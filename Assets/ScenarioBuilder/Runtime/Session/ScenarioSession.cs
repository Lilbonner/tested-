using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    public enum MessageKind { Info, Success, Warning, Error }

    public readonly struct SessionMessage
    {
        public readonly MessageKind Kind;
        public readonly string Text;

        public SessionMessage(MessageKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }

    public readonly struct StepSnapshot
    {
        public readonly ScenarioStep Step;
        public readonly StepStatus Status;
        public readonly int Attempt;
        public readonly string Group;

        public StepSnapshot(ScenarioStep step, StepStatus status, int attempt, string group)
        {
            Step = step;
            Status = status;
            Attempt = attempt;
            Group = group;
        }
    }

    public readonly struct GroupSnapshot
    {
        public readonly string Name;
        public readonly bool Started;
        public readonly bool Finished;
        public readonly int Violations;

        public GroupSnapshot(string name, bool started, bool finished, int violations)
        {
            Name = name;
            Started = started;
            Finished = finished;
            Violations = violations;
        }
    }

    /// <summary>
    /// Одно прохождение выбранного режима. Отвечает за очередь активации (группы → пакеты → волны),
    /// статусы шагов и запись журнала. Что делать при нарушении — решают <see cref="ExecutionRules"/>,
    /// как реагировать на действие — сами шаги, как оценивать — анализаторы (по журналу, после).
    /// Чистый C#-класс: тестируется без сцены и Play Mode.
    /// </summary>
    public sealed class ScenarioSession : IStepContext
    {
        sealed class StepState
        {
            public ScenarioStep Step;
            public GroupState Group;
            public StepStatus Status = StepStatus.Pending;
            public int Attempt = 1;
        }

        sealed class GroupState
        {
            public StepGroup Config;
            public readonly List<List<StepState>> Waves = new List<List<StepState>>();
            public int Wave = -1;
            public bool Started;
            public bool Finished;
            public int Violations;

            public IEnumerable<StepState> Steps => Waves.SelectMany(w => w);
        }

        readonly ScenarioMode _mode;
        readonly IReadOnlyList<ScenarioLogger> _loggers;
        readonly Func<float> _clock;
        readonly List<GroupState> _groups = new List<GroupState>();
        readonly List<StepState> _ordered = new List<StepState>();
        readonly Dictionary<ScenarioStep, StepState> _byStep = new Dictionary<ScenarioStep, StepState>();
        float _startTime;
        int _blockStart, _blockEnd;

        public ScenarioMode Mode => _mode;
        public RunLog Log { get; }
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }
        public bool IsRunning => IsStarted && !IsFinished;
        public IReadOnlyList<string> ConfigProblems { get; }

        public event Action Changed;
        public event Action<SessionMessage> Message;
        public event Action<ScenarioSession> Finished;

        public ScenarioSession(ScenarioMode mode, string scenarioName, IReadOnlyList<ScenarioLogger> loggers = null,
            Func<float> clock = null)
        {
            _mode = mode ? mode : throw new ArgumentNullException(nameof(mode));
            _loggers = loggers ?? Array.Empty<ScenarioLogger>();
            _clock = clock ?? (() => Time.realtimeSinceStartup);
            Log = RunLog.Create(scenarioName, mode.DisplayName);
            ConfigProblems = mode.Validate();

            foreach (var config in mode.Groups)
            {
                var group = new GroupState { Config = config };
                foreach (var batch in config.batches)
                foreach (var wave in batch.Waves())
                {
                    var states = new List<StepState>();
                    foreach (var step in wave)
                    {
                        if (_byStep.ContainsKey(step)) continue; // повтор шага в режиме — берём первое вхождение
                        var state = new StepState { Step = step, Group = group };
                        _byStep.Add(step, state);
                        _ordered.Add(state);
                        states.Add(state);
                    }
                    if (states.Count > 0) group.Waves.Add(states);
                }
                _groups.Add(group);
            }
        }

        // ---------- состояние для UI ----------

        public StepStatus GetStatus(ScenarioStep step) =>
            step != null && _byStep.TryGetValue(step, out var s) ? s.Status : StepStatus.Pending;

        public IEnumerable<StepSnapshot> Steps =>
            _ordered.Select(s => new StepSnapshot(s.Step, s.Status, s.Attempt, s.Group.Config.name));

        public IEnumerable<GroupSnapshot> Groups =>
            _groups.Select(g => new GroupSnapshot(g.Config.name, g.Started, g.Finished, g.Violations));

        // ---------- жизненный цикл ----------

        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
            _startTime = _clock();

            foreach (var logger in _loggers) Safe(() => logger.BeginRun(Log), logger);
            Write(new LogEntry(0, LogTypes.RunStarted).With(LogKeys.Rules, _mode.Rules.Describe()));
            foreach (var problem in ConfigProblems)
                Notify(MessageKind.Warning, $"Настройка режима: {problem}");
            foreach (var s in _ordered)
                Write(Entry(LogTypes.StepDeclared, s).With(LogKeys.Title, s.Step.Title));

            StartBlockFrom(0);
            Changed?.Invoke();
        }

        /// <summary>Прервать прохождение (выход в меню, перезапуск). Журнал помечается как незавершённый.</summary>
        public void Abort()
        {
            if (!IsRunning) return;
            IsFinished = true;
            Write(new LogEntry(Now, "RunAborted"));
            foreach (var logger in _loggers) Safe(() => logger.EndRun(Log), logger);
        }

        public void HandleEvent(in ScenarioEvent e)
        {
            if (!IsRunning) return;

            var handled = false;
            foreach (var s in _ordered.Where(s => s.Status == StepStatus.Active).ToList())
            {
                if (s.Status != StepStatus.Active) continue; // мог измениться при обработке предыдущего шага
                var reaction = s.Step.React(e, this);
                switch (reaction.Kind)
                {
                    case ReactionKind.Progress:
                        handled = true;
                        Write(Entry(LogTypes.StepProgress, s, reaction.Message).With(LogKeys.Event, e.ToString()));
                        Notify(MessageKind.Info, $"«{s.Step.Title}»: {reaction.Message}");
                        break;
                    case ReactionKind.Completed:
                        handled = true;
                        Complete(s, e);
                        break;
                    case ReactionKind.Violation:
                        handled = true;
                        RegisterViolation(s, reaction.Message, false, e);
                        break;
                }
                if (IsFinished) break;
            }

            if (!handled && !IsFinished) HandleOutOfTurn(e);
            Changed?.Invoke();
        }

        // ---------- очередь активации ----------

        // Блок = группа «после предыдущих» + следующие за ней группы «вместе с предыдущей».
        void StartBlockFrom(int index)
        {
            if (index >= _groups.Count)
            {
                Finish();
                return;
            }
            var end = index + 1;
            while (end < _groups.Count && _groups[end].Config.start == GroupStart.WithPrevious) end++;
            _blockStart = index;
            _blockEnd = end;
            for (var i = index; i < end; i++) StartGroup(_groups[i]);
        }

        void StartGroup(GroupState g)
        {
            g.Started = true;
            Write(new LogEntry(Now, LogTypes.GroupStarted).With(LogKeys.Group, g.Config.name));
            if (g.Waves.Count > 0 && _mode.Rules.ShouldCloseGroup(g.Violations))
                CloseGroup(g); // нарушения «вне очереди» накопились ещё до старта группы
            else
                AdvanceWave(g);
        }

        void AdvanceWave(GroupState g)
        {
            while (true)
            {
                g.Wave++;
                if (g.Wave >= g.Waves.Count)
                {
                    FinishGroup(g);
                    return;
                }
                var activated = false;
                foreach (var s in g.Waves[g.Wave].Where(s => s.Status == StepStatus.Pending))
                {
                    s.Status = StepStatus.Active;
                    s.Step.ResetProgress();
                    Write(Entry(LogTypes.StepActivated, s));
                    activated = true;
                }
                if (activated) return;
            }
        }

        void TryAdvance(GroupState g)
        {
            if (!g.Started || g.Finished) return;
            if (g.Waves[g.Wave].Any(s => s.Status == StepStatus.Active)) return;
            AdvanceWave(g);
        }

        void FinishGroup(GroupState g)
        {
            if (g.Finished) return;
            g.Finished = true;
            Write(new LogEntry(Now, LogTypes.GroupFinished)
                .With(LogKeys.Group, g.Config.name)
                .With(LogKeys.GroupViolations, g.Violations));

            for (var i = _blockStart; i < _blockEnd; i++)
                if (!_groups[i].Finished) return;
            StartBlockFrom(_blockEnd);
        }

        void CloseGroup(GroupState g)
        {
            var skipped = 0;
            foreach (var s in g.Steps.Where(s => s.Status is StepStatus.Pending or StepStatus.Active))
            {
                s.Status = StepStatus.Skipped;
                skipped++;
                Write(Entry(LogTypes.StepSkipped, s, LogResults.Skipped)
                    .With(LogKeys.Reason, "достигнут порог нарушений группы"));
            }
            Notify(MessageKind.Error,
                $"Группа «{g.Config.name}» закрыта: нарушений {g.Violations}. Пропущено шагов: {skipped}.");
            FinishGroup(g);
        }

        void Finish()
        {
            if (IsFinished) return;
            IsFinished = true;
            Write(new LogEntry(Now, LogTypes.RunFinished));
            foreach (var logger in _loggers) Safe(() => logger.EndRun(Log), logger);
            Notify(MessageKind.Success, "Сценарий завершён.");
            Finished?.Invoke(this);
        }

        // ---------- результаты шагов ----------

        void Complete(StepState s, in ScenarioEvent e)
        {
            s.Status = StepStatus.Completed;
            Write(Entry(LogTypes.StepCompleted, s, LogResults.Success)
                .With(LogKeys.Attempt, s.Attempt)
                .With(LogKeys.Event, e.ToString()));
            Notify(MessageKind.Success, $"Выполнено: «{s.Step.Title}»");
            TryAdvance(s.Group);
        }

        void HandleOutOfTurn(in ScenarioEvent e)
        {
            foreach (var s in _ordered)
            {
                if (s.Status != StepStatus.Pending || !s.Step.IsOwnAction(e)) continue;
                var isViolation = s.Step.OutOfTurn switch
                {
                    OutOfTurnOverride.Violation => true,
                    OutOfTurnOverride.Ignore => false,
                    _ => _mode.OutOfTurn == OutOfTurnPolicy.Violation,
                };
                if (!isViolation) continue; // шаг неактивен до своей очереди и не принимает действия
                RegisterViolation(s, "действие вне очереди — шаг ещё не доступен", true, e);
                return;
            }
        }

        void RegisterViolation(StepState s, string reason, bool outOfTurn, in ScenarioEvent e)
        {
            var g = s.Group;
            g.Violations++;
            Write(Entry(LogTypes.Violation, s, LogResults.Violation)
                .With(LogKeys.Reason, reason)
                .With(LogKeys.Attempt, s.Attempt)
                .With(LogKeys.OutOfTurn, outOfTurn ? "true" : null)
                .With(LogKeys.Event, e.ToString())
                .With(LogKeys.GroupViolations, g.Violations));

            if (_mode.Rules.OnViolation == ViolationOutcome.Retry)
            {
                s.Attempt++;
                if (s.Status == StepStatus.Active) s.Step.ResetProgress();
                Notify(MessageKind.Warning, outOfTurn
                    ? $"«{s.Step.Title}»: {reason}"
                    : $"Нарушение в «{s.Step.Title}»: {reason}. Повторите шаг.");
            }
            else
            {
                s.Status = StepStatus.Failed;
                Write(Entry(LogTypes.StepFailed, s, LogResults.Failed).With(LogKeys.Reason, reason));
                Notify(MessageKind.Error, $"Шаг «{s.Step.Title}» провален: {reason}");
            }

            if (g.Started && !g.Finished && _mode.Rules.ShouldCloseGroup(g.Violations))
                CloseGroup(g);
            else if (s.Status == StepStatus.Failed)
                TryAdvance(g);
        }

        // ---------- журнал ----------

        float Now => _clock() - _startTime;

        LogEntry Entry(string type, StepState s, string result = null) =>
            new LogEntry(Now, type, s.Step.Id, result).With(LogKeys.Group, s.Group.Config.name);

        void Write(LogEntry entry)
        {
            Log.entries.Add(entry);
            foreach (var logger in _loggers) Safe(() => logger.Write(Log, entry), logger);
        }

        void Notify(MessageKind kind, string text) => Message?.Invoke(new SessionMessage(kind, text));

        // Сбой логгера не должен ломать прохождение.
        static void Safe(Action action, UnityEngine.Object context)
        {
            try { action(); }
            catch (Exception ex) { Debug.LogException(ex, context); }
        }
    }
}
