using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder.Tests
{
    /// <summary>Сборка сценариев из кода для тестов. Все созданные объекты удаляются в Dispose.</summary>
    sealed class ScenarioFixture : IDisposable
    {
        public const string Do = "Do";
        public const string Bad = "Bad";

        readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        float _time;

        public UnityEventSource Source(string id)
        {
            var go = Track(new GameObject(id));
            var source = go.AddComponent<UnityEventSource>();
            source.SetEventNames(Do, Bad);
            return source;
        }

        /// <summary>Шаг «выполнить Do на своём источнике»; Bad на том же источнике — запрещённое действие.</summary>
        public SingleActionStep Step(string id)
        {
            var source = Source($"src_{id}");
            var step = Track(new GameObject(id)).AddComponent<SingleActionStep>();
            step.Setup(id, $"Шаг {id}", null);
            step.SetAction(new EventMatcher(source, Do));
            step.AddForbidden(new EventMatcher(source, Bad));
            return step;
        }

        public List<SingleActionStep> Steps(int count, int firstNumber = 1)
        {
            var list = new List<SingleActionStep>();
            for (var i = 0; i < count; i++) list.Add(Step($"S{firstNumber + i:00}"));
            return list;
        }

        public ScenarioMode Mode(ExecutionRules rules, OutOfTurnPolicy policy, params StepGroup[] groups)
        {
            var mode = Track(new GameObject("mode")).AddComponent<ScenarioMode>();
            mode.Setup("Тест", rules, policy, null, groups);
            return mode;
        }

        public ScenarioSession Start(ScenarioMode mode)
        {
            var session = new ScenarioSession(mode, "Test", null, () => _time += 0.5f);
            session.Start();
            return session;
        }

        public T Analyzer<T>() where T : ScenarioAnalyzer => Track(ScriptableObject.CreateInstance<T>());

        public static StepGroup Group(string name, params ActivationBatch[] batches) =>
            new StepGroup { name = name, batches = new List<ActivationBatch>(batches) };

        public static StepGroup GroupWithPrevious(string name, params ActivationBatch[] batches)
        {
            var group = Group(name, batches);
            group.start = GroupStart.WithPrevious;
            return group;
        }

        public static ActivationBatch Together(params ScenarioStep[] steps) =>
            new ActivationBatch { activation = BatchActivation.Together, steps = new List<ScenarioStep>(steps) };

        public static ActivationBatch OneByOne(params ScenarioStep[] steps) =>
            new ActivationBatch { activation = BatchActivation.OneByOne, steps = new List<ScenarioStep>(steps) };

        public static ScenarioEvent DoEvent(ScenarioStep step) => new ScenarioEvent(SourceOf(step), Do);
        public static ScenarioEvent BadEvent(ScenarioStep step) => new ScenarioEvent(SourceOf(step), Bad);

        static ScenarioEventSource SourceOf(ScenarioStep step) => step.ForbiddenActions[0].source;

        T Track<T>(T obj) where T : UnityEngine.Object
        {
            _created.Add(obj);
            return obj;
        }

        public void Dispose()
        {
            foreach (var obj in _created)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
