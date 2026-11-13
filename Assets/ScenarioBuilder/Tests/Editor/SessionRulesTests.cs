using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static ScenarioBuilder.Tests.ScenarioFixture;

namespace ScenarioBuilder.Tests
{
    public class SessionRulesTests
    {
        ScenarioFixture _f;

        [SetUp] public void SetUp() => _f = new ScenarioFixture();
        [TearDown] public void TearDown() => _f.Dispose();

        [Test]
        public void OutOfTurn_ByDefault_IsViolation()
        {
            var s = _f.Steps(2);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("G", OneByOne(s[0], s[1])));
            var session = _f.Start(mode);

            session.HandleEvent(DoEvent(s[1]));

            Assert.AreEqual(StepStatus.Pending, session.GetStatus(s[1]));
            var violation = session.Log.entries.Single(e => e.type == LogTypes.Violation);
            Assert.AreEqual("S02", violation.stepId);
            Assert.AreEqual("true", violation.Get(LogKeys.OutOfTurn));
        }

        [Test]
        public void OutOfTurn_Ignore_StepDoesNotAcceptActions()
        {
            var s = _f.Steps(2);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Ignore, Group("G", OneByOne(s[0], s[1])));
            var session = _f.Start(mode);

            session.HandleEvent(DoEvent(s[1]));
            Assert.IsFalse(session.Log.entries.Any(e => e.type == LogTypes.Violation));
            Assert.AreEqual(StepStatus.Pending, session.GetStatus(s[1]));

            session.HandleEvent(DoEvent(s[0]));
            session.HandleEvent(DoEvent(s[1]));
            Assert.IsTrue(session.IsFinished);
        }

        [Test]
        public void OutOfTurn_StepOverride_WinsOverMode()
        {
            var s = _f.Steps(2);
            s[1].SetOutOfTurn(OutOfTurnOverride.Ignore);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("G", OneByOne(s[0], s[1])));
            var session = _f.Start(mode);

            session.HandleEvent(DoEvent(s[1]));

            Assert.IsFalse(session.Log.entries.Any(e => e.type == LogTypes.Violation));
        }

        [Test]
        public void SequenceStep_WrongOrderIsViolation_RightOrderCompletes()
        {
            var a = _f.Source("ZoneA");
            var b = _f.Source("ZoneB");
            var step = new GameObject("seq").AddComponent<SequenceStep>();
            step.Setup("SEQ", "A затем B", null);
            step.SetSequence(true, new EventMatcher(a, Do), new EventMatcher(b, Do));
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("G", Together(step)));
            var session = _f.Start(mode);

            session.HandleEvent(new ScenarioEvent(b, Do));
            Assert.AreEqual(1, session.Log.entries.Count(e => e.type == LogTypes.Violation));

            session.HandleEvent(new ScenarioEvent(a, Do));
            session.HandleEvent(new ScenarioEvent(a, Do)); // повтор уже сделанного — не ошибка
            session.HandleEvent(new ScenarioEvent(b, Do));

            Assert.AreEqual(StepStatus.Completed, session.GetStatus(step));
            Assert.AreEqual(1, session.Log.entries.Count(e => e.type == LogTypes.Violation));
            Object.DestroyImmediate(step.gameObject);
        }

        [Test]
        public void ContextCondition_StepStatus_ViolationUntilOtherStepCompleted()
        {
            var s = _f.Steps(2);
            s[1].AddCondition(new StepStatusCondition { step = s[0] });
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("G", Together(s[0], s[1])));
            var session = _f.Start(mode);

            session.HandleEvent(DoEvent(s[1]));
            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[1]));
            Assert.AreEqual(1, session.Log.entries.Count(e => e.type == LogTypes.Violation));

            session.HandleEvent(DoEvent(s[0]));
            session.HandleEvent(DoEvent(s[1]));
            Assert.AreEqual(StepStatus.Completed, session.GetStatus(s[1]));
        }

        [Test]
        public void Groups_WithPrevious_StartTogether()
        {
            var s = _f.Steps(3);
            var mode = _f.Mode(new ExamRules(), OutOfTurnPolicy.Violation,
                Group("G1", Together(s[0])),
                GroupWithPrevious("G2", Together(s[1])),
                Group("G3", Together(s[2])));
            var session = _f.Start(mode);

            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[0]));
            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[1]));
            Assert.AreEqual(StepStatus.Pending, session.GetStatus(s[2]));

            session.HandleEvent(DoEvent(s[1]));
            Assert.AreEqual(StepStatus.Pending, session.GetStatus(s[2]), "G3 ждёт окончания всего блока");
            session.HandleEvent(DoEvent(s[0]));
            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[2]));
        }

        [Test]
        public void SameSteps_DifferentModes_NoDuplication()
        {
            var s = _f.Steps(4);
            var training = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("Все", Together(s.ToArray())));
            var exam = _f.Mode(new ExamRules(), OutOfTurnPolicy.Violation,
                Group("G1", Together(s[0], s[1])), Group("G2", OneByOne(s[2], s[3])));

            var t = _f.Start(training);
            var e = _f.Start(exam);

            Assert.AreEqual(4, t.Steps.Count(x => x.Status == StepStatus.Active));
            Assert.AreEqual(2, e.Steps.Count(x => x.Status == StepStatus.Active));
        }
    }
}
