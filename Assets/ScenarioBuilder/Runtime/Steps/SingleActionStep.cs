using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>Шаг из одного действия: нажать кнопку, войти в зону, столкнуться с объектом…</summary>
    [AddComponentMenu("Scenario/Steps/Single Action Step")]
    public sealed class SingleActionStep : ScenarioStep
    {
        [Tooltip("Действие, которое выполняет шаг.")]
        [SerializeField] EventMatcher action = new EventMatcher();

        public override bool IsOwnAction(in ScenarioEvent e) => action.Matches(e);

        protected override StepReaction ReactToAction(in ScenarioEvent e) =>
            action.Matches(e) ? StepReaction.Completed : StepReaction.Ignored;

        public override void ResetProgress() { }

        public override string DescribeAction() => action.ToString();

        internal void SetAction(EventMatcher matcher) => action = matcher;
    }
}
