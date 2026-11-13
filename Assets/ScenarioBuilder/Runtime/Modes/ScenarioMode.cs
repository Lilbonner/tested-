using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Режим прохождения: какие шаги в каких группах и пакетах, правила исполнения,
    /// реакция на действия вне очереди и анализатор по умолчанию.
    /// Шаги сюда только ссылаются — один шаг может участвовать в любом числе режимов.
    /// </summary>
    [AddComponentMenu("Scenario/Scenario Mode")]
    public sealed class ScenarioMode : MonoBehaviour
    {
        [SerializeField] string displayName = "Режим";

        [Tooltip("Правила исполнения: что происходит при нарушении и когда группа закрывается.")]
        [SerializeReference, SubclassPicker]
        ExecutionRules rules = new TrainingRules();

        [Tooltip("Действие шага до его очереди: нарушение или игнорирование. Шаг может переопределить.")]
        [SerializeField] OutOfTurnPolicy outOfTurn = OutOfTurnPolicy.Violation;

        [Tooltip("Группы выполняются по порядку. Внутри группы пакеты активируются по порядку.")]
        [SerializeField] List<StepGroup> groups = new List<StepGroup>();

        [Tooltip("Анализатор, который показывается после прохождения. Можно сменить на экране результатов.")]
        [SerializeField] ScenarioAnalyzer defaultAnalyzer;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ExecutionRules Rules => rules ?? (rules = new TrainingRules());
        public OutOfTurnPolicy OutOfTurn => outOfTurn;
        public IReadOnlyList<StepGroup> Groups => groups;
        public ScenarioAnalyzer DefaultAnalyzer => defaultAnalyzer;

        public IEnumerable<ScenarioStep> AllSteps => groups.SelectMany(g => g.Steps);

        internal void Setup(string modeName, ExecutionRules executionRules, OutOfTurnPolicy policy,
            ScenarioAnalyzer analyzer, params StepGroup[] stepGroups)
        {
            displayName = modeName;
            rules = executionRules;
            outOfTurn = policy;
            defaultAnalyzer = analyzer;
            groups = new List<StepGroup>(stepGroups);
        }

        internal void AddGroup(StepGroup group) => groups.Add(group);

        /// <summary>Проблемы конфигурации, понятные человеку (показываются в инспекторе и при запуске).</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (groups.Count == 0) problems.Add("Нет ни одной группы.");
            var seen = new HashSet<ScenarioStep>();
            var ids = new HashSet<string>();
            foreach (var group in groups)
            {
                foreach (var batch in group.batches)
                {
                    if (batch.steps.Any(s => s == null))
                        problems.Add($"Группа «{group.name}»: в пакете есть пустая ссылка на шаг.");
                    foreach (var step in batch.steps.Where(s => s != null))
                    {
                        if (!seen.Add(step))
                            problems.Add($"Шаг «{step.Id}» встречается в режиме несколько раз — учитывается первое вхождение.");
                        else if (!ids.Add(step.Id))
                            problems.Add($"Несколько шагов с одинаковым идентификатором «{step.Id}».");
                    }
                }
                if (!group.Steps.Any()) problems.Add($"Группа «{group.name}» пустая.");
            }
            return problems;
        }
    }
}
