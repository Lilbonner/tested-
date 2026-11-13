using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioBuilder
{
    /// <summary>
    /// Пример собственного источника: UI Toggle → «On» / «Off».
    /// Шаблон для новых источников: подписаться на что угодно и вызвать Emit.
    /// </summary>
    [AddComponentMenu("Scenario/Sources/Toggle Source")]
    [RequireComponent(typeof(Toggle))]
    public sealed class ToggleEventSource : ScenarioEventSource
    {
        public const string On = "On";
        public const string Off = "Off";
        static readonly string[] Names = { On, Off };

        Toggle _toggle;

        public override IReadOnlyList<string> EventNames => Names;

        void OnEnable()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnChanged);
        }

        void OnDisable() => _toggle.onValueChanged.RemoveListener(OnChanged);

        void OnChanged(bool isOn) => Emit(isOn ? On : Off);
    }
}
