using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Инспектор шага: показывает, в каких режимах и группах шаг используется.</summary>
    [CustomEditor(typeof(ScenarioStep), true)]
    public sealed class ScenarioStepEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length > 1) return;
            var step = (ScenarioStep)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Действие", step.DescribeAction());

            var sameId = FindObjectsByType<ScenarioStep>(FindObjectsInactive.Include)
                .Count(s => s != step && s.Id == step.Id);
            if (sameId > 0)
                EditorGUILayout.HelpBox($"Идентификатор «{step.Id}» не уникален — журнал перепутает шаги.", MessageType.Error);

            var usage = FindObjectsByType<ScenarioMode>(FindObjectsInactive.Include)
                .SelectMany(m => m.Groups.Where(g => g.Steps.Contains(step)).Select(g => $"{m.DisplayName} → «{g.name}»"))
                .ToList();
            EditorGUILayout.HelpBox(usage.Count == 0
                    ? "Шаг не используется ни в одном режиме."
                    : "Используется: " + string.Join("; ", usage),
                usage.Count == 0 ? MessageType.Warning : MessageType.Info);
        }
    }
}
