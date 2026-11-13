using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioBuilder
{
    /// <summary>Нажатие UI Button → событие «Click».</summary>
    [AddComponentMenu("Scenario/Sources/Button Source")]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonEventSource : ScenarioEventSource
    {
        public const string Click = "Click";
        static readonly string[] Names = { Click };

        Button _button;

        public override IReadOnlyList<string> EventNames => Names;

        void OnEnable()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        void OnDisable() => _button.onClick.RemoveListener(OnClick);

        void OnClick() => Emit(Click);
    }
}
