using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Мост из любого UnityEvent (OnClick, OnValueChanged, собственные UnityEvent в скриптах) в сценарий.
    /// В UnityEvent перетащите этот компонент и выберите метод Raise() или Raise(string).
    /// </summary>
    [AddComponentMenu("Scenario/Sources/UnityEvent Source")]
    public sealed class UnityEventSource : ScenarioEventSource
    {
        [Tooltip("Имена событий, которые публикует источник. Raise() без параметра публикует первое.")]
        [SerializeField] List<string> eventNames = new List<string> { "Raised" };

        public override IReadOnlyList<string> EventNames => eventNames;

        public void Raise() => Emit(eventNames.Count > 0 ? eventNames[0] : "Raised");

        public void Raise(string eventName) => Emit(eventName);

        internal void SetEventNames(params string[] names) => eventNames = new List<string>(names);
    }
}
