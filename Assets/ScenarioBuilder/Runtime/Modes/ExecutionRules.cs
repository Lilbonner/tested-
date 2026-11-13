using System;
using UnityEngine;

namespace ScenarioBuilder
{
    public enum ViolationOutcome
    {
        /// <summary>Шаг остаётся активным, пользователь пробует ещё раз.</summary>
        Retry,
        /// <summary>Шаг получает статус Failed, повтора нет.</summary>
        Fail,
    }

    /// <summary>
    /// Правила исполнения режима: что происходит при нарушении и когда группа закрывается досрочно.
    /// Активация шагов, журнал и оценка сюда не входят. Новый режим — новый наследник.
    /// </summary>
    [Serializable]
    public abstract class ExecutionRules
    {
        public abstract ViolationOutcome OnViolation { get; }

        /// <summary>Закрыть группу досрочно (незавершённые шаги → Skipped)?</summary>
        public abstract bool ShouldCloseGroup(int violationsInGroup);

        public abstract string Describe();
    }

    [Serializable, DisplayName("Тренировка: повтор шага после нарушения")]
    public sealed class TrainingRules : ExecutionRules
    {
        public override ViolationOutcome OnViolation => ViolationOutcome.Retry;
        public override bool ShouldCloseGroup(int violationsInGroup) => false;
        public override string Describe() => "Тренировка: при нарушении шаг повторяется, пропусков нет";
    }

    [Serializable, DisplayName("Экзамен: Failed и порог нарушений в группе")]
    public sealed class ExamRules : ExecutionRules
    {
        [Min(1)]
        [Tooltip("Сколько нарушений в группе допускается. При достижении порога незавершённые шаги группы пропускаются.")]
        public int violationThreshold = 2;

        public override ViolationOutcome OnViolation => ViolationOutcome.Fail;
        public override bool ShouldCloseGroup(int violationsInGroup) => violationsInGroup >= violationThreshold;
        public override string Describe() => $"Экзамен: ошибка → Failed, порог группы — {violationThreshold} нарушения";
    }
}
