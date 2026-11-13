using System;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>Действие пользователя, пришедшее от источника событий.</summary>
    public readonly struct ScenarioEvent
    {
        public readonly ScenarioEventSource Source;
        public readonly string EventName;
        /// <summary>Второй участник (кто вошёл в триггер, с кем столкнулись). Может быть null.</summary>
        public readonly GameObject Other;

        public ScenarioEvent(ScenarioEventSource source, string eventName, GameObject other = null)
        {
            Source = source;
            EventName = eventName;
            Other = other;
        }

        public string SourceId => Source != null ? Source.SourceId : "<none>";

        public override string ToString() => $"{SourceId}.{EventName}";
    }

    /// <summary>
    /// Общая шина событий сценария. Источники ничего не знают о шагах и режимах —
    /// они только публикуют события сюда, а исполняющая сессия на них подписана.
    /// </summary>
    public static class ScenarioEventBus
    {
        public static event Action<ScenarioEvent> Raised;

        public static void Raise(in ScenarioEvent e) => Raised?.Invoke(e);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Raised = null; // на случай отключённой перезагрузки домена
    }
}
