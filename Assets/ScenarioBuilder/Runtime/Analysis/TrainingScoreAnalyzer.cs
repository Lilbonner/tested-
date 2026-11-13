using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Оценка тренировки: баллы за шаг зависят от номера успешной попытки
    /// (по умолчанию 10 — с первой, 5 — со второй, 0 — с третьей и далее).
    /// Попытки считаются по нарушениям до завершения шага.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario/Analyzers/Training Score", fileName = "TrainingScoreAnalyzer")]
    public sealed class TrainingScoreAnalyzer : ScenarioAnalyzer
    {
        [Tooltip("Баллы за успех с 1-й, 2-й, … попытки. Максимум за шаг — первый элемент.")]
        [SerializeField] int[] pointsByAttempt = { 10, 5 };
        [Tooltip("Баллы за успех с попытки, для которой нет значения в списке выше.")]
        [SerializeField] int pointsForLaterAttempts;

        int MaxPerStep => pointsByAttempt is { Length: > 0 } ? pointsByAttempt[0] : 0;

        protected override void Evaluate(RunFacts facts, AnalysisReport report)
        {
            foreach (var step in facts.Steps)
            {
                var line = new ReportLine { StepId = step.Id, Title = step.Title, Group = step.Group };
                if (step.Completed)
                {
                    var attempt = step.SuccessfulAttempt;
                    line.Points = attempt <= pointsByAttempt.Length ? pointsByAttempt[attempt - 1] : pointsForLaterAttempts;
                    line.Verdict = $"выполнен с {attempt}-й попытки";
                }
                else
                {
                    line.Verdict = step.SkippedExplicitly ? "пропущен" : step.FailedExplicitly ? "провален" : "не выполнен";
                }
                report.Lines.Add(line);
                report.Score += line.Points;
            }

            report.MaxScore = MaxPerStep * facts.Steps.Count;
            report.Notes.Add($"Шагов: {facts.Steps.Count}. Нарушений всего: {Sum(facts, s => s.Violations)}.");
        }

        static int Sum(RunFacts facts, System.Func<StepFacts, int> selector)
        {
            var total = 0;
            foreach (var step in facts.Steps) total += selector(step);
            return total;
        }
    }
}
