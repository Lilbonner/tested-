using System;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioBuilder
{
    /// <summary>
    /// Контекстное условие шага: проверяется в момент, когда действие шага выполнено.
    /// Новое условие — наследник с [Serializable]; оно автоматически появится в выпадающем списке инспектора.
    /// </summary>
    [Serializable]
    public abstract class StepCondition
    {
        [Tooltip("Сообщение пользователю при невыполненном условии. Пусто — стандартное.")]
        [SerializeField] string failMessage;

        public bool Check(IStepContext context, out string reason)
        {
            if (Evaluate(context))
            {
                reason = null;
                return true;
            }
            reason = string.IsNullOrWhiteSpace(failMessage) ? DefaultFailMessage : failMessage;
            return false;
        }

        protected abstract bool Evaluate(IStepContext context);
        protected abstract string DefaultFailMessage { get; }

        internal StepCondition WithMessage(string message)
        {
            failMessage = message;
            return this;
        }
    }

    [Serializable, DisplayName("Toggle включён / выключен")]
    public sealed class ToggleStateCondition : StepCondition
    {
        public Toggle toggle;
        public bool mustBeOn = true;

        protected override bool Evaluate(IStepContext context) => toggle != null && toggle.isOn == mustBeOn;

        protected override string DefaultFailMessage =>
            $"«{(toggle ? toggle.name : "?")}» должен быть {(mustBeOn ? "включён" : "выключен")}";
    }

    [Serializable, DisplayName("Объект активен / неактивен")]
    public sealed class ObjectActiveCondition : StepCondition
    {
        public GameObject target;
        public bool mustBeActive = true;

        protected override bool Evaluate(IStepContext context) =>
            target != null && target.activeInHierarchy == mustBeActive;

        protected override string DefaultFailMessage =>
            $"«{(target ? target.name : "?")}» должен быть {(mustBeActive ? "активен" : "неактивен")}";
    }

    [Serializable, DisplayName("Игрок в зоне / вне зоны")]
    public sealed class InsideZoneCondition : StepCondition
    {
        public TriggerEventSource zone;
        public bool mustBeInside = true;

        protected override bool Evaluate(IStepContext context) => zone != null && zone.IsOccupied == mustBeInside;

        protected override string DefaultFailMessage =>
            $"Нужно находиться {(mustBeInside ? "в зоне" : "вне зоны")} «{(zone ? zone.SourceId : "?")}»";
    }

    [Serializable, DisplayName("Другой шаг в нужном статусе")]
    public sealed class StepStatusCondition : StepCondition
    {
        public ScenarioStep step;
        public StepStatus requiredStatus = StepStatus.Completed;

        protected override bool Evaluate(IStepContext context) =>
            step != null && context != null && context.GetStatus(step) == requiredStatus;

        protected override string DefaultFailMessage =>
            $"Сначала: «{(step ? step.Title : "?")}» ({requiredStatus})";
    }
}
