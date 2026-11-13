using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>Инспектор режима: обычные настройки + читаемая схема активации и проверка ошибок.</summary>
    [CustomEditor(typeof(ScenarioMode))]
    public sealed class ScenarioModeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var mode = (ScenarioMode)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Схема прохождения", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Describe(mode), MessageType.None);

            foreach (var problem in mode.Validate())
                EditorGUILayout.HelpBox(problem, MessageType.Warning);

            var unused = SceneSteps(mode).Except(mode.AllSteps).ToList();
            if (unused.Count == 0) return;
            EditorGUILayout.HelpBox("Шаги сцены, не участвующие в режиме: " +
                                    string.Join(", ", unused.Select(s => s.Id)), MessageType.Info);
            if (GUILayout.Button("Добавить их пакетом в последнюю группу"))
            {
                Undo.RecordObject(mode, "Add unused steps");
                if (mode.Groups.Count == 0) mode.AddGroup(new StepGroup { name = "Группа 1" });
                mode.Groups[mode.Groups.Count - 1].batches.Add(new ActivationBatch { steps = unused });
                EditorUtility.SetDirty(mode);
            }
        }

        internal static string Describe(ScenarioMode mode)
        {
            var sb = new StringBuilder();
            sb.AppendLine(mode.Rules.Describe());
            sb.AppendLine(mode.OutOfTurn == OutOfTurnPolicy.Violation
                ? "Действие шага до его очереди — нарушение."
                : "Шаг неактивен до своей очереди и не принимает действия.");

            for (var g = 0; g < mode.Groups.Count; g++)
            {
                var group = mode.Groups[g];
                var start = g == 0 ? "" : group.start == GroupStart.WithPrevious ? " (вместе с предыдущей)" : " (после предыдущих)";
                sb.AppendLine();
                sb.AppendLine($"Группа {g + 1} «{group.name}»{start}:");
                for (var b = 0; b < group.batches.Count; b++)
                {
                    var batch = group.batches[b];
                    var ids = batch.steps.Select(s => s ? s.Id : "<пусто>").ToList();
                    var body = batch.activation == BatchActivation.Together
                        ? $"[{string.Join(", ", ids)}] — одновременно, в любом порядке"
                        : $"{string.Join(" → ", ids)} — по очереди";
                    sb.AppendLine($"   {b + 1}. {body}");
                }
            }
            return sb.ToString().TrimEnd();
        }

        static IEnumerable<ScenarioStep> SceneSteps(ScenarioMode mode) =>
            FindObjectsByType<ScenarioStep>(FindObjectsInactive.Include)
                .Where(s => s.gameObject.scene == mode.gameObject.scene)
                .OrderBy(s => s.Id);
    }
}
