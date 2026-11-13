using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Базовый класс источника событий. Чтобы добавить свой источник, унаследуйтесь,
    /// перечислите имена событий в <see cref="EventNames"/> и вызывайте <see cref="Emit"/>.
    /// </summary>
    public abstract class ScenarioEventSource : MonoBehaviour
    {
        [Tooltip("Имя источника в журнале. Если пусто — используется имя объекта.")]
        [SerializeField] string sourceId;

        public string SourceId => string.IsNullOrEmpty(sourceId) ? name : sourceId;

        /// <summary>Какие события умеет публиковать источник (для выпадающего списка в инспекторе).</summary>
        public abstract IReadOnlyList<string> EventNames { get; }

        protected void Emit(string eventName, GameObject other = null) =>
            ScenarioEventBus.Raise(new ScenarioEvent(this, eventName, other));

        /// <summary>Ручной вызов события (тесты, отладка, собственные скрипты).</summary>
        public void EmitManually(string eventName, GameObject other = null) => Emit(eventName, other);
    }
}
