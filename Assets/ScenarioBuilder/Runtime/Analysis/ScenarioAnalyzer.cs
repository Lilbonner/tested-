using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Анализатор оценивает готовый журнал. Он не связан с процессом прохождения:
    /// тот же журнал можно оценить заново любым анализатором (в игре или в окне редактора).
    /// Новый анализатор: наследник + [CreateAssetMenu], реализовать <see cref="Evaluate"/>.
    /// </summary>
    public abstract class ScenarioAnalyzer : ScriptableObject
    {
        [SerializeField] string displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public AnalysisReport Analyze(RunLog log)
        {
            var facts = RunFacts.From(log);
            var report = new AnalysisReport
            {
                AnalyzerName = DisplayName,
                RunTitle = log != null ? log.Title : "—",
            };
            report.Warnings.AddRange(facts.Warnings);
            if (log?.entries != null) Evaluate(facts, report);
            return report;
        }

        protected abstract void Evaluate(RunFacts facts, AnalysisReport report);

        internal void SetDisplayName(string value) => displayName = value;
    }

    public sealed class ReportLine
    {
        public string StepId;
        public string Title;
        public string Group;
        public string Verdict;
        public float Points;
    }

    /// <summary>Результат анализа: итог, построчная разбивка, заметки и диагностические предупреждения.</summary>
    public sealed class AnalysisReport
    {
        public string AnalyzerName;
        public string RunTitle;
        public float Score;
        public float MaxScore;
        public string PointsLabel = "баллы";
        public List<ReportLine> Lines { get; } = new List<ReportLine>();
        public List<string> Notes { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>Текст для uGUI/IMGUI с rich text.</summary>
        public string ToRichText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{AnalyzerName}</b>   ·   {RunTitle}");
            sb.AppendLine($"<size=30><b>Итог: {Score:0.#} / {MaxScore:0.#}</b></size>");
            foreach (var warning in Warnings)
                sb.AppendLine($"<color=#E0A030>(!) {warning}</color>");
            foreach (var note in Notes)
                sb.AppendLine(note);
            sb.AppendLine();
            foreach (var line in Lines)
            {
                var group = string.IsNullOrEmpty(line.Group) ? "" : $"<color=#8899AA>[{line.Group}]</color> ";
                sb.AppendLine($"{group}<b>{line.StepId}</b> {line.Title} — {line.Verdict}  <b>{line.Points:+0.#;-0.#;0}</b>");
            }
            return sb.ToString();
        }
    }
}
