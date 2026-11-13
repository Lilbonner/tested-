using System.Collections.Generic;

namespace ScenarioBuilder
{
    /// <summary>Что известно о шаге по журналу.</summary>
    public sealed class StepFacts
    {
        public string Id;
        public string Title;
        /// <summary>Имя группы; null, если в журнале нет этих данных.</summary>
        public string Group;
        public bool Declared;
        public bool Activated;
        public bool Completed;
        public int Violations;
        public int ViolationsBeforeCompletion;
        public bool FailedExplicitly;
        public bool SkippedExplicitly;

        /// <summary>Номер успешной попытки (1 — с первого раза).</summary>
        public int SuccessfulAttempt => ViolationsBeforeCompletion + 1;
    }

    /// <summary>
    /// Общий разбор журнала для всех анализаторов: читает только известные типы записей
    /// и игнорирует остальные. Анализаторы работают с этим представлением, а не с режимом
    /// исполнения, поэтому любой анализатор применим к журналу любого режима.
    /// </summary>
    public sealed class RunFacts
    {
        readonly Dictionary<string, StepFacts> _byId = new Dictionary<string, StepFacts>();

        public List<StepFacts> Steps { get; } = new List<StepFacts>();
        public List<string> Warnings { get; } = new List<string>();
        public bool Finished { get; private set; }
        public bool HasDeclarations { get; private set; }

        public static RunFacts From(RunLog log)
        {
            var facts = new RunFacts();
            if (log?.entries == null)
            {
                facts.Warnings.Add("Журнал пуст или повреждён — оценивать нечего.");
                return facts;
            }

            var withoutStep = 0;
            foreach (var entry in log.entries)
            {
                if (entry == null) continue;
                switch (entry.type)
                {
                    case LogTypes.RunFinished:
                        facts.Finished = true;
                        continue;
                    case LogTypes.StepDeclared:
                    case LogTypes.StepActivated:
                    case LogTypes.StepCompleted:
                    case LogTypes.Violation:
                    case LogTypes.StepFailed:
                    case LogTypes.StepSkipped:
                        break;
                    default:
                        continue; // неизвестные и служебные записи не мешают анализу
                }

                if (string.IsNullOrEmpty(entry.stepId))
                {
                    withoutStep++;
                    continue;
                }

                var step = facts.Get(entry.stepId);
                step.Group ??= entry.Get(LogKeys.Group);
                step.Title ??= entry.Get(LogKeys.Title);

                switch (entry.type)
                {
                    case LogTypes.StepDeclared:
                        step.Declared = true;
                        facts.HasDeclarations = true;
                        break;
                    case LogTypes.StepActivated:
                        step.Activated = true;
                        break;
                    case LogTypes.StepCompleted:
                        step.Completed = true;
                        break;
                    case LogTypes.Violation:
                        step.Violations++;
                        if (!step.Completed) step.ViolationsBeforeCompletion++;
                        break;
                    case LogTypes.StepFailed:
                        step.FailedExplicitly = true;
                        break;
                    case LogTypes.StepSkipped:
                        step.SkippedExplicitly = true;
                        break;
                }
            }

            if (!facts.HasDeclarations)
                facts.Warnings.Add("В журнале нет записей StepDeclared: шаги, по которым не было событий, не учтены — " +
                                   "максимум и итог могут быть неточными.");
            if (!facts.Finished)
                facts.Warnings.Add("Прохождение не завершено (нет записи RunFinished) — оценка по имеющимся данным.");
            if (withoutStep > 0)
                facts.Warnings.Add($"Записей шагов без идентификатора: {withoutStep} — они пропущены.");
            return facts;
        }

        StepFacts Get(string id)
        {
            if (_byId.TryGetValue(id, out var step)) return step;
            step = new StepFacts { Id = id };
            _byId.Add(id, step);
            Steps.Add(step);
            return step;
        }
    }
}
