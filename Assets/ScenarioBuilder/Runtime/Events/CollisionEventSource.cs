using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>Физическое столкновение → событие «Hit».</summary>
    [AddComponentMenu("Scenario/Sources/Collision Source")]
    [RequireComponent(typeof(Collider))]
    public sealed class CollisionEventSource : ScenarioEventSource
    {
        public const string Hit = "Hit";
        static readonly string[] Names = { Hit };

        [Tooltip("Реагировать только на объекты с этим тегом. Пусто — на любые.")]
        [SerializeField] string requiredTag = "Player";

        public override IReadOnlyList<string> EventNames => Names;

        void OnCollisionEnter(Collision collision)
        {
            var other = collision.gameObject;
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
            Emit(Hit, other);
        }
    }
}
