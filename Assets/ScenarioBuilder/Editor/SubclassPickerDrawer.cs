using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// Выпадающий список типов для полей [SerializeReference, SubclassPicker]:
    /// условия шагов, правила режимов. Новые наследники появляются в списке автоматически.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
    public sealed class SubclassPickerDrawer : PropertyDrawer
    {
        const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (property.propertyType != SerializedPropertyType.ManagedReference) return height;
            if (property.isExpanded && property.managedReferenceValue != null)
                foreach (var child in Children(property))
                    height += EditorGUI.GetPropertyHeight(child, true) + Spacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "SubclassPicker требует [SerializeReference]");
                return;
            }

            DetachSharedReference(property);
            var value = property.managedReferenceValue;
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(line.x, line.y, EditorGUIUtility.labelWidth, line.height);
            var buttonRect = new Rect(labelRect.xMax + 2, line.y, line.width - labelRect.width - 2, line.height);

            if (IsArrayElement(property, out var index)) label = new GUIContent($"#{index + 1}");

            EditorGUI.BeginProperty(position, label, property);
            if (value != null)
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
            else
                EditorGUI.LabelField(labelRect, label);

            var caption = value == null ? "— выберите тип —" : NiceName(value.GetType());
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(caption), FocusType.Keyboard))
                ShowMenu(property, value?.GetType());

            if (value != null && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var y = line.yMax + Spacing;
                foreach (var child in Children(property))
                {
                    var h = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                    y += h + Spacing;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        void ShowMenu(SerializedProperty property, Type current)
        {
            var serializedObject = property.serializedObject;
            var path = property.propertyPath;
            var menu = new GenericMenu();

            void Assign(object instance)
            {
                serializedObject.Update();
                var target = serializedObject.FindProperty(path);
                target.managedReferenceValue = instance;
                target.isExpanded = instance != null;
                serializedObject.ApplyModifiedProperties();
            }

            menu.AddItem(new GUIContent("— нет —"), current == null, () => Assign(null));
            foreach (var type in CandidateTypes(BaseType()))
                menu.AddItem(new GUIContent(NiceName(type)), type == current, () => Assign(Activator.CreateInstance(type)));
            menu.ShowAsContext();
        }

        Type BaseType()
        {
            var type = fieldInfo.FieldType;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
            return type;
        }

        static IEnumerable<Type> CandidateTypes(Type baseType) =>
            TypeCache.GetTypesDerivedFrom(baseType).Append(baseType)
                .Where(t => !t.IsAbstract && !t.IsGenericType && !t.IsInterface
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .Distinct()
                .OrderBy(NiceName);

        static string NiceName(Type type) =>
            type.GetCustomAttribute<ScenarioBuilder.DisplayNameAttribute>()?.Name
            ?? ObjectNames.NicifyVariableName(type.Name);

        static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            var it = property.Copy();
            var end = property.GetEndProperty();
            if (!it.NextVisible(true)) yield break;
            while (!SerializedProperty.EqualContents(it, end))
            {
                yield return it.Copy();
                if (!it.NextVisible(false)) yield break;
            }
        }

        static bool IsArrayElement(SerializedProperty property, out int index)
        {
            index = -1;
            var path = property.propertyPath;
            if (!path.EndsWith("]")) return false;
            var open = path.LastIndexOf('[');
            return int.TryParse(path.Substring(open + 1, path.Length - open - 2), out index);
        }

        /// <summary>
        /// При добавлении элемента «+» Unity копирует ссылку на тот же объект — оба элемента
        /// редактировались бы одновременно. Делаем копии независимыми.
        /// </summary>
        static void DetachSharedReference(SerializedProperty property)
        {
            if (property.managedReferenceValue == null || !IsArrayElement(property, out var index) || index == 0) return;
            var path = property.propertyPath;
            var array = property.serializedObject.FindProperty(path.Substring(0, path.LastIndexOf(".Array.data[", StringComparison.Ordinal)));
            if (array == null || !array.isArray) return;

            for (var i = 0; i < index; i++)
            {
                if (array.GetArrayElementAtIndex(i).managedReferenceId != property.managedReferenceId) continue;
                var original = property.managedReferenceValue;
                var copy = Activator.CreateInstance(original.GetType());
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(original), copy);
                property.managedReferenceValue = copy;
                property.serializedObject.ApplyModifiedProperties();
                return;
            }
        }
    }
}
