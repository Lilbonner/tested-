using System;

namespace ScenarioBuilder
{
    /// <summary>«Какое действие ждём»: источник + имя события. Пустое имя = любое событие источника.</summary>
    [Serializable]
    public sealed class EventMatcher
    {
        public ScenarioEventSource source;
        public string eventName;

        public EventMatcher() { }

        public EventMatcher(ScenarioEventSource source, string eventName)
        {
            this.source = source;
            this.eventName = eventName;
        }

        public bool Matches(in ScenarioEvent e) =>
            source != null && e.Source == source &&
            (string.IsNullOrEmpty(eventName) || e.EventName == eventName);

        public override string ToString() =>
            source == null ? "<не задано>" : $"{source.SourceId}.{(string.IsNullOrEmpty(eventName) ? "*" : eventName)}";
    }
}
