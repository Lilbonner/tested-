using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    public enum BatchActivation
    {
        /// <summary>Все шаги пакета активируются одновременно, выполнять можно в любом порядке.</summary>
        Together,
        /// <summary>Шаги пакета активируются по одному, следующий — после завершения предыдущего.</summary>
        OneByOne,
    }

    public enum GroupStart
    {
        /// <summary>Группа начинается, когда закончены все предыдущие группы.</summary>
        AfterPrevious,
        /// <summary>Группа стартует одновременно с предыдущей (перекрывающиеся группы).</summary>
        WithPrevious,
    }

    /// <summary>Пакет активации: набор шагов, которые открываются вместе или по очереди.</summary>
    [Serializable]
    public sealed class ActivationBatch
    {
        [Tooltip("Необязательная подпись пакета (видна в инспекторе).")]
        public string label;
        public BatchActivation activation = BatchActivation.Together;
        [Tooltip("Шаги пакета. Перетащите сюда объекты шагов из Hierarchy, порядок меняется перетаскиванием.")]
        public List<ScenarioStep> steps = new List<ScenarioStep>();

        /// <summary>Разворачивает пакет в «волны» — наборы одновременно активных шагов.</summary>
        public IEnumerable<List<ScenarioStep>> Waves()
        {
            var valid = steps.Where(s => s != null).ToList();
            if (valid.Count == 0) yield break;
            if (activation == BatchActivation.Together)
                yield return valid;
            else
                foreach (var step in valid) yield return new List<ScenarioStep> { step };
        }
    }

    /// <summary>Группа шагов. В тренировке обычно одна, на экзамене — несколько со своим порогом нарушений.</summary>
    [Serializable]
    public sealed class StepGroup
    {
        public string name = "Группа";
        [Tooltip("Когда группа начинается относительно предыдущей.")]
        public GroupStart start = GroupStart.AfterPrevious;
        [Tooltip("Пакеты активируются по порядку: следующий — после завершения всех шагов предыдущего.")]
        public List<ActivationBatch> batches = new List<ActivationBatch>();

        public IEnumerable<ScenarioStep> Steps => batches.SelectMany(b => b.steps).Where(s => s != null);
    }
}
