using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Trigger-зона → события «Enter» / «Exit». Также помнит, кто сейчас внутри,
    /// чтобы условия шагов могли проверять «игрок находится в зоне».
    /// </summary>
    [AddComponentMenu("Scenario/Sources/Trigger Source")]
    [RequireComponent(typeof(Collider))]
    public sealed class TriggerEventSource : ScenarioEventSource
    {
        public const string Enter = "Enter";
        public const string Exit = "Exit";
        static readonly string[] Names = { Enter, Exit };

        [Tooltip("Реагировать только на объекты с этим тегом. Пусто — на любые.")]
        [SerializeField] string requiredTag = "Player";

        readonly HashSet<GameObject> _inside = new HashSet<GameObject>();

        public override IReadOnlyList<string> EventNames => Names;
        public bool IsOccupied => _inside.Count > 0;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        bool Accepts(Component other) => string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);

        void OnTriggerEnter(Collider other)
        {
            if (!Accepts(other)) return;
            _inside.Add(other.gameObject);
            Emit(Enter, other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            if (!Accepts(other)) return;
            _inside.Remove(other.gameObject);
            Emit(Exit, other.gameObject);
        }

        internal void SetOccupied(GameObject who, bool inside)
        {
            if (inside) _inside.Add(who); else _inside.Remove(who);
        }
    }
}
