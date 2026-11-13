using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Одна строка: [источник] [событие ▼]. Список событий берётся у выбранного источника.</summary>
    [CustomPropertyDrawer(typeof(EventMatcher))]
    public sealed class EventMatcherDrawer : PropertyDrawer
    {
        const string AnyEvent = "(любое)";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sourceProp = property.FindPropertyRelative(nameof(EventMatcher.source));
            var eventProp = property.FindPropertyRelative(nameof(EventMatcher.eventName));

            EditorGUI.BeginProperty(position, label, property);
            if (!label.text.StartsWith("Element"))
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var sourceRect = new Rect(position.x, position.y, position.width * 0.6f - 2, position.height);
            var eventRect = new Rect(sourceRect.xMax + 4, position.y, position.width - sourceRect.width - 4, position.height);

            EditorGUI.PropertyField(sourceRect, sourceProp, GUIContent.none);

            var source = sourceProp.objectReferenceValue as ScenarioEventSource;
            using (new EditorGUI.DisabledScope(source == null))
            {
                var names = source != null ? source.EventNames.ToList() : new System.Collections.Generic.List<string>();
                var current = eventProp.stringValue;
                if (!string.IsNullOrEmpty(current) && !names.Contains(current)) names.Add(current);
                names.Insert(0, AnyEvent);

                var index = string.IsNullOrEmpty(current) ? 0 : names.IndexOf(current);
                var picked = EditorGUI.Popup(eventRect, index, names.ToArray());
                if (picked != index) eventProp.stringValue = picked == 0 ? "" : names[picked];
            }
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
