using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>Пишет журнал в консоль Unity (и в Player.log билда).</summary>
    [CreateAssetMenu(menuName = "Scenario/Loggers/Console Logger", fileName = "ConsoleLogger")]
    public sealed class ConsoleLogger : ScenarioLogger
    {
        [Tooltip("Писать только нарушения, пропуски и итоги (без активаций и прогресса).")]
        [SerializeField] bool onlyImportant;

        public override void Write(RunLog run, LogEntry entry)
        {
            if (onlyImportant && entry.type is LogTypes.StepActivated or LogTypes.StepProgress or LogTypes.StepDeclared)
                return;
            Debug.Log($"[Scenario] {entry}");
        }
    }
}
