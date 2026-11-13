using System.Linq;
using NUnit.Framework;
using static ScenarioBuilder.Tests.ScenarioFixture;

namespace ScenarioBuilder.Tests
{
    /// <summary>Контрольные примеры из раздела 7 задания.</summary>
    public class ControlExampleTests
    {
        ScenarioFixture _f;

        [SetUp] public void SetUp() => _f = new ScenarioFixture();
        [TearDown] public void TearDown() => _f.Dispose();

        [Test]
        public void Training_FirstSecondThirdAttempt_Gives15Points()
        {
            var s = _f.Steps(3);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("Все", Together(s.ToArray())));
            var session = _f.Start(mode);

            session.HandleEvent(DoEvent(s[0]));                                   // 1-я попытка
            session.HandleEvent(BadEvent(s[1])); session.HandleEvent(DoEvent(s[1])); // 2-я
            session.HandleEvent(BadEvent(s[2])); session.HandleEvent(BadEvent(s[2]));
            session.HandleEvent(DoEvent(s[2]));                                   // 3-я

            Assert.IsTrue(session.IsFinished);
            var report = _f.Analyzer<TrainingScoreAnalyzer>().Analyze(session.Log);
            Assert.AreEqual(15, report.Score);
            Assert.AreEqual(30, report.MaxScore);
            CollectionAssert.AreEqual(new[] { 10f, 5f, 0f }, report.Lines.Select(l => l.Points));
            CollectionAssert.IsEmpty(report.Warnings);
        }

        [Test]
        public void Training_ViolationKeepsStepAndOthersActive()
        {
            var s = _f.Steps(2);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("Все", Together(s.ToArray())));
            var session = _f.Start(mode);

            session.HandleEvent(BadEvent(s[0]));

            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[0]));
            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[1]));
        }

        [Test]
        public void Exam_TwoViolationsInGroupOfFour_SkipsRest_Score70()
        {
            var s = _f.Steps(8);
            var mode = _f.Mode(new ExamRules { violationThreshold = 2 }, OutOfTurnPolicy.Violation,
                Group("G1", Together(s[0], s[1], s[2], s[3])),
                Group("G2", OneByOne(s[4], s[5])),
                Group("G3", Together(s[6], s[7])));
            var session = _f.Start(mode);

            session.HandleEvent(BadEvent(s[0]));
            Assert.AreEqual(StepStatus.Failed, session.GetStatus(s[0]));
            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[1]), "до порога исполнение продолжается");

            session.HandleEvent(BadEvent(s[1]));
            Assert.AreEqual(StepStatus.Failed, session.GetStatus(s[1]));
            Assert.AreEqual(StepStatus.Skipped, session.GetStatus(s[2]));
            Assert.AreEqual(StepStatus.Skipped, session.GetStatus(s[3]));
            Assert.AreEqual(StepStatus.Active, session.GetStatus(s[4]), "началась следующая группа");

            foreach (var step in s.Skip(4)) session.HandleEvent(DoEvent(step));
            Assert.IsTrue(session.IsFinished);

            var report = _f.Analyzer<ExamPenaltyAnalyzer>().Analyze(session.Log);
            Assert.AreEqual(70, report.Score);
            Assert.AreEqual(new[] { "Failed", "Failed", "Skipped", "Skipped" }, report.Lines.Take(4).Select(l => l.Verdict));
        }

        [Test]
        public void Exam_FailedStepHasNoRetry()
        {
            var s = _f.Steps(2);
            var mode = _f.Mode(new ExamRules(), OutOfTurnPolicy.Violation, Group("G", Together(s.ToArray())));
            var session = _f.Start(mode);

            session.HandleEvent(BadEvent(s[0]));
            session.HandleEvent(DoEvent(s[0]));

            Assert.AreEqual(StepStatus.Failed, session.GetStatus(s[0]));
        }

        [Test]
        public void MixedActivation_FollowsBatchesAndWaves()
        {
            var s = _f.Steps(12);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("Все",
                Together(s[0], s[1], s[2], s[3]),
                OneByOne(s[4], s[5], s[6], s[7]),
                Together(s[8], s[9]),
                Together(s[10], s[11])));
            var session = _f.Start(mode);

            AssertActive(session, s, 0, 1, 2, 3);
            foreach (var i in new[] { 3, 1, 0 }) session.HandleEvent(DoEvent(s[i])); // любой порядок
            AssertActive(session, s, 2);
            session.HandleEvent(DoEvent(s[2]));

            for (var i = 4; i < 8; i++)
            {
                AssertActive(session, s, i);
                session.HandleEvent(DoEvent(s[i]));
            }

            AssertActive(session, s, 8, 9);
            session.HandleEvent(DoEvent(s[9]));
            session.HandleEvent(DoEvent(s[8]));
            AssertActive(session, s, 10, 11);
            session.HandleEvent(DoEvent(s[11]));
            session.HandleEvent(DoEvent(s[10]));
            Assert.IsTrue(session.IsFinished);
        }

        static void AssertActive(ScenarioSession session, System.Collections.Generic.List<SingleActionStep> steps,
            params int[] indices)
        {
            var active = steps.Select((step, i) => (step, i))
                .Where(p => session.GetStatus(p.step) == StepStatus.Active)
                .Select(p => p.i).ToArray();
            CollectionAssert.AreEquivalent(indices, active);
        }
    }
}
