using UnityEngine;

namespace ScenarioBuilder
{
    /// <summary>
    /// Логгер — «куда выводить» журнал. Сам журнал (<see cref="RunLog"/>) собирает сессия,
    /// а логгеры получают каждую запись. Логгеров можно подключить несколько, заменить
    /// или добавить новые, не трогая исполнение и анализ.
    /// Новый логгер: наследник + [CreateAssetMenu], затем ассет добавляется в ScenarioRunner.
    /// </summary>
    public abstract class ScenarioLogger : ScriptableObject
    {
        public virtual void BeginRun(RunLog run) { }
        public abstract void Write(RunLog run, LogEntry entry);
        public virtual void EndRun(RunLog run) { }
    }
}
