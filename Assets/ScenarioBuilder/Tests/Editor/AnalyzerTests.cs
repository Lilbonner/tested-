using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static ScenarioBuilder.Tests.ScenarioFixture;

namespace ScenarioBuilder.Tests
{
    public class AnalyzerTests
    {
        ScenarioFixture _f;

        [SetUp] public void SetUp() => _f = new ScenarioFixture();
        [TearDown] public void TearDown() => _f.Dispose();

        RunLog TrainingRunWithOneRetry()
        {
            var s = _f.Steps(2);
            var mode = _f.Mode(new TrainingRules(), OutOfTurnPolicy.Violation, Group("Все", Together(s.ToArray())));
            var session = _f.Start(mode);
            session.HandleEvent(DoEvent(s[0]));
            session.HandleEvent(BadEvent(s[1]));
            session.HandleEvent(DoEvent(s[1]));
            return session.Log;
        }

        [Test]
        public void ExamAnalyzer_OnTrainingLog_TreatsViolatedStepAsFailed()
        {
            var report = _f.Analyzer<ExamPenaltyAnalyzer>().Analyze(TrainingRunWithOneRetry());

            Assert.AreEqual(new[] { "Completed", "Failed" }, report.Lines.Select(l => l.Verdict));
            Assert.AreEqual(90, report.Score);
        }

        [Test]
        public void Reanalysis_FromJson_GivesSameResult()
        {
            var log = TrainingRunWithOneRetry();
            var restored = JsonUtility.FromJson<RunLog>(RunLogStorage.ToJson(log));
            var analyzer = _f.Analyzer<TrainingScoreAnalyzer>();

            Assert.AreEqual(analyzer.Analyze(log).Score, analyzer.Analyze(restored).Score);
            Assert.AreEqual(15, analyzer.Analyze(restored).Score);
        }

        [Test]
        public void UnknownEntryTypesAndFields_AreIgnored()
        {
            var log = TrainingRunWithOneRetry();
            log.entries.Insert(3, new LogEntry(1, "HeartRate").With("bpm", 90));
            log.entries[4].With("customField", "x");

            var report = _f.Analyzer<TrainingScoreAnalyzer>().Analyze(log);

            Assert.AreEqual(15, report.Score);
            CollectionAssert.IsEmpty(report.Warnings);
        }

        [Test]
        public void MissingOptionalData_ProducesWarnings_NotExceptions()
        {
            var log = new RunLog { scenario = "Hand-made", mode = "?" };
            log.entries.Add(new LogEntry(1, LogTypes.StepCompleted, "S01", LogResults.Success));
            log.entries.Add(new LogEntry(2, LogTypes.Violation, null, LogResults.Violation));

            var exam = _f.Analyzer<ExamPenaltyAnalyzer>().Analyze(log);
            var training = _f.Analyzer<TrainingScoreAnalyzer>().Analyze(log);

            Assert.AreEqual(100, exam.Score);
            Assert.AreEqual(10, training.Score);
            Assert.IsTrue(exam.Warnings.Any(w => w.Contains("StepDeclared")));
            Assert.IsTrue(exam.Warnings.Any(w => w.Contains("RunFinished")));
            Assert.IsTrue(exam.Warnings.Any(w => w.Contains("группе")));
            Assert.IsTrue(training.Warnings.Any(w => w.Contains("без идентификатора")));
        }

        [Test]
        public void NullOrBrokenLog_DoesNotThrow()
        {
            var report = _f.Analyzer<ExamPenaltyAnalyzer>().Analyze(null);
            Assert.IsNotEmpty(report.Warnings);

            var broken = new RunLog { entries = null };
            Assert.DoesNotThrow(() => _f.Analyzer<TrainingScoreAnalyzer>().Analyze(broken));
        }
    }
}
