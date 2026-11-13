using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Составной шаг: действия должны произойти строго по порядку
    /// (например, «сначала войти в зону A, затем в зону B»).
    /// </summary>
    [AddComponentMenu("Scenario/Steps/Sequence Step")]
    public sealed class SequenceStep : ScenarioStep
    {
        [Tooltip("Действия в нужном порядке. Порядок меняется перетаскиванием.")]
        [SerializeField] List<EventMatcher> sequence = new List<EventMatcher>();

        [Tooltip("Действие из последовательности не по порядку — нарушение (иначе просто игнорируется).")]
        [SerializeField] bool wrongOrderIsViolation = true;

        int _progress;

        public int Progress => _progress;

        public override bool IsOwnAction(in ScenarioEvent e)
        {
            foreach (var m in sequence)
                if (m.Matches(e)) return true;
            return false;
        }

        protected override StepReaction ReactToAction(in ScenarioEvent e)
        {
            if (sequence.Count == 0) return StepReaction.Ignored;

            if (sequence[_progress].Matches(e))
            {
                _progress++;
                if (_progress < sequence.Count)
                    return StepReaction.Progress($"{_progress}/{sequence.Count}");
                _progress = 0;
                return StepReaction.Completed;
            }

            // Повтор уже выполненного действия — не ошибка.
            for (var i = 0; i < _progress; i++)
                if (sequence[i].Matches(e)) return StepReaction.Ignored;

            // Действие из будущей части последовательности — нарушение порядка.
            for (var i = _progress + 1; i < sequence.Count; i++)
            {
                if (!sequence[i].Matches(e)) continue;
                if (!wrongOrderIsViolation) return StepReaction.Ignored;
                var expected = sequence[_progress];
                _progress = 0;
                return StepReaction.Violation($"Нарушен порядок: сначала нужно {expected}");
            }
            return StepReaction.Ignored;
        }

        public override void ResetProgress() => _progress = 0;

        public override string DescribeAction() => string.Join(" → ", sequence.Select(m => m.ToString()));

        internal void SetSequence(bool violationOnWrongOrder, params EventMatcher[] matchers)
        {
            sequence = new List<EventMatcher>(matchers);
            wrongOrderIsViolation = violationOnWrongOrder;
        }
    }
}
