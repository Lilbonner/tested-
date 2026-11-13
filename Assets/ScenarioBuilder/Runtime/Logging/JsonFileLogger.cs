using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Сохраняет журнал в JSON-файл (папка <see cref="RunLogStorage.Folder"/>).
    /// Сохранённые прохождения можно заново оценить другим анализатором — в игре или в окне редактора.
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario/Loggers/JSON File Logger", fileName = "JsonFileLogger")]
    public sealed class JsonFileLogger : ScenarioLogger
    {
        [Tooltip("Перезаписывать файл после каждой записи — прерванное прохождение тоже сохранится.")]
        [SerializeField] bool saveEveryEntry = true;

        public override void BeginRun(RunLog run) => RunLogStorage.Save(run);

        public override void Write(RunLog run, LogEntry entry)
        {
            if (saveEveryEntry) RunLogStorage.Save(run);
        }

        public override void EndRun(RunLog run)
        {
            var path = RunLogStorage.Save(run);
            Debug.Log($"[Scenario] Журнал сохранён: {path}");
        }
    }
}
