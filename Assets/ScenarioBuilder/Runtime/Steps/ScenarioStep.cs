using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Переиспользуемый шаг сценария. Знает только, какие действия к нему относятся и
    /// когда он выполнен/нарушен. Ничего не знает о режимах, группах, очках и журнале —
    /// поэтому один и тот же шаг используется в любом режиме без дублирования.
    ///
    /// Новый тип шага: унаследуйтесь и реализуйте <see cref="IsOwnAction"/>,
    /// <see cref="ReactToAction"/> и <see cref="ResetProgress"/>.
    /// </summary>
    public abstract class ScenarioStep : MonoBehaviour
    {
        [Tooltip("Уникальный идентификатор шага (попадает в журнал).")]
        [SerializeField] string stepId;
        [Tooltip("Короткое название задания для пользователя.")]
        [SerializeField] string title;
        [TextArea(2, 4)]
        [Tooltip("Подсказка, показывается пока шаг активен.")]
        [SerializeField] string instruction;

        [Tooltip("Контекст: условия, которые должны выполняться в момент завершения шага. " +
                 "Если условие не выполнено — это нарушение.")]
        [SerializeReference, SubclassPicker]
        List<StepCondition> conditions = new List<StepCondition>();

        [Tooltip("Действия, запрещённые, пока шаг активен. Каждое — нарушение.")]
        [SerializeField] List<EventMatcher> forbiddenActions = new List<EventMatcher>();

        [Tooltip("Реакция на действие этого шага до его очереди.")]
        [SerializeField] OutOfTurnOverride outOfTurn = OutOfTurnOverride.UseModeSetting;

        public string Id => string.IsNullOrWhiteSpace(stepId) ? name : stepId;
        public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
        public string Instruction => instruction;
        public OutOfTurnOverride OutOfTurn => outOfTurn;
        public IReadOnlyList<StepCondition> Conditions => conditions;
        public IReadOnlyList<EventMatcher> ForbiddenActions => forbiddenActions;

        /// <summary>Действие является «своим» действием шага (используется для очереди шагов).</summary>
        public abstract bool IsOwnAction(in ScenarioEvent e);

        /// <summary>Реакция на действие, пока шаг активен.</summary>
        public StepReaction React(in ScenarioEvent e, IStepContext context)
        {
            foreach (var forbidden in forbiddenActions)
                if (forbidden != null && forbidden.Matches(e))
                    return StepReaction.Violation($"Запрещённое действие: {forbidden}");

            var reaction = ReactToAction(e);
            if (reaction.Kind != ReactionKind.Completed) return reaction;

            foreach (var condition in conditions)
            {
                if (condition == null || condition.Check(context, out var reason)) continue;
                ResetProgress();
                return StepReaction.Violation(reason);
            }
            return reaction;
        }

        /// <summary>Собственная логика шага: прогресс, завершение или нарушение порядка.</summary>
        protected abstract StepReaction ReactToAction(in ScenarioEvent e);

        /// <summary>Сбросить внутренний прогресс (при активации и повторной попытке).</summary>
        public abstract void ResetProgress();

        /// <summary>Короткое описание ожидаемого действия для инспектора/журнала.</summary>
        public abstract string DescribeAction();

        internal void Setup(string id, string stepTitle, string stepInstruction)
        {
            stepId = id;
            title = stepTitle;
            instruction = stepInstruction;
        }

        internal void AddCondition(StepCondition condition) => conditions.Add(condition);
        internal void AddForbidden(EventMatcher matcher) => forbiddenActions.Add(matcher);
        internal void SetOutOfTurn(OutOfTurnOverride value) => outOfTurn = value;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(stepId)) stepId = name;
        }
    }
}
