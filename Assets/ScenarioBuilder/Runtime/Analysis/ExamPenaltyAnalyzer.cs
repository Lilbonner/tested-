using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Оценка экзамена: каждый шаг — Completed, Failed или Skipped.
    /// Итог = max(0, 100 − 10 × Failed − 5 × Skipped) (значения настраиваются).
    /// Статус выводится из журнала: было нарушение → Failed; выполнен → Completed; иначе → Skipped.
    /// Поэтому анализатор корректно работает и на журнале тренировки.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario/Analyzers/Exam Penalty", fileName = "ExamPenaltyAnalyzer")]
    public sealed class ExamPenaltyAnalyzer : ScenarioAnalyzer
    {
        public enum Outcome { Completed, Failed, Skipped }

        [SerializeField] int baseScore = 100;
        [SerializeField] int failedPenalty = 10;
        [SerializeField] int skippedPenalty = 5;

        public static Outcome Classify(StepFacts step) =>
            step.Violations > 0 || step.FailedExplicitly ? Outcome.Failed
            : step.Completed ? Outcome.Completed
            : Outcome.Skipped;

        protected override void Evaluate(RunFacts facts, AnalysisReport report)
        {
            var penalties = 0;
            var perGroup = new Dictionary<string, (int completed, int failed, int skipped, int penalty)>();
            var missingGroups = 0;

            foreach (var step in facts.Steps)
            {
                var outcome = Classify(step);
                var penalty = outcome == Outcome.Failed ? failedPenalty : outcome == Outcome.Skipped ? skippedPenalty : 0;
                penalties += penalty;
                report.Lines.Add(new ReportLine
                {
                    StepId = step.Id, Title = step.Title, Group = step.Group,
                    Verdict = outcome.ToString(), Points = -penalty,
                });

                if (step.Group == null)
                {
                    missingGroups++;
                    continue;
                }
                perGroup.TryGetValue(step.Group, out var g);
                perGroup[step.Group] = (
                    g.completed + (outcome == Outcome.Completed ? 1 : 0),
                    g.failed + (outcome == Outcome.Failed ? 1 : 0),
                    g.skipped + (outcome == Outcome.Skipped ? 1 : 0),
                    g.penalty + penalty);
            }

            report.MaxScore = baseScore;
            report.Score = Mathf.Max(0, baseScore - penalties);

            var lines = report.Lines;
            report.Notes.Add($"Completed: {lines.Count(l => l.Verdict == nameof(Outcome.Completed))}, " +
                             $"Failed: {lines.Count(l => l.Verdict == nameof(Outcome.Failed))}, " +
                             $"Skipped: {lines.Count(l => l.Verdict == nameof(Outcome.Skipped))}. Штраф: {penalties}.");
            foreach (var pair in perGroup)
                report.Notes.Add($"  Группа «{pair.Key}»: Completed {pair.Value.completed}, Failed {pair.Value.failed}, " +
                                 $"Skipped {pair.Value.skipped} — штраф {pair.Value.penalty}");
            if (missingGroups > 0)
                report.Warnings.Add($"Нет данных о группе у {missingGroups} шаг(ов) — разбивка по группам неполная.");
        }
    }
}
