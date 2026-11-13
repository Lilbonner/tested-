using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScenarioBuilder
{
    /// <summary>Дополнительное поле записи журнала. Набор полей открыт — новые ключи ничего не ломают.</summary>
    [Serializable]
    public sealed class LogField
    {
        public string key;
        public string value;
    }

    /// <summary>
    /// Минимальный контракт записи журнала: время, шаг, тип, результат + произвольные поля.
    /// Обработчики читают только известные им типы и поля, остальное игнорируют.
    /// </summary>
    [Serializable]
    public sealed class LogEntry
    {
        /// <summary>Секунды от начала прохождения.</summary>
        public float time;
        public string stepId;
        public string type;
        public string result;
        public List<LogField> data = new List<LogField>();

        public LogEntry() { }

        public LogEntry(float time, string type, string stepId = null, string result = null)
        {
            this.time = time;
            this.type = type;
            this.stepId = stepId;
            this.result = result;
        }

        public LogEntry With(string key, object value)
        {
            if (value == null) return this;
            var text = value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
            data.Add(new LogField { key = key, value = text });
            return this;
        }

        public string Get(string key)
        {
            if (data == null) return null;
            foreach (var field in data)
                if (field.key == key) return field.value;
            return null;
        }

        public bool Has(string key) => Get(key) != null;

        public override string ToString()
        {
            var text = $"[{time,7:0.00}] {type,-14} {stepId,-5} {result}";
            if (data != null && data.Count > 0)
            {
                var parts = new List<string>();
                foreach (var field in data) parts.Add($"{field.key}={field.value}");
                text += "  {" + string.Join(", ", parts) + "}";
            }
            return text;
        }
    }

    /// <summary>Журнал одного прохождения. Сериализуется в JSON и анализируется независимо от сессии.</summary>
    [Serializable]
    public sealed class RunLog
    {
        public int formatVersion = 1;
        public string runId;
        public string scenario;
        public string mode;
        public string startedUtc;
        public List<LogEntry> entries = new List<LogEntry>();

        public static RunLog Create(string scenario, string mode)
        {
            var now = DateTime.UtcNow;
            return new RunLog
            {
                runId = now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture),
                scenario = scenario,
                mode = mode,
                startedUtc = now.ToString("o", CultureInfo.InvariantCulture),
            };
        }

        public string Title => $"{scenario} — {mode} — {LocalStartTime}";

        public string LocalStartTime =>
            DateTime.TryParse(startedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t)
                ? t.ToLocalTime().ToString("dd.MM HH:mm:ss", CultureInfo.InvariantCulture)
                : startedUtc;
    }

    /// <summary>Известные типы записей. Список открыт: можно писать и свои типы.</summary>
    public static class LogTypes
    {
        public const string RunStarted = "RunStarted";
        public const string RunFinished = "RunFinished";
        /// <summary>Объявление шага режима в начале прохождения (нужно, чтобы учесть шаги без событий).</summary>
        public const string StepDeclared = "StepDeclared";
        public const string GroupStarted = "GroupStarted";
        public const string GroupFinished = "GroupFinished";
        public const string StepActivated = "StepActivated";
        public const string StepProgress = "StepProgress";
        public const string StepCompleted = "StepCompleted";
        public const string Violation = "Violation";
        public const string StepFailed = "StepFailed";
        public const string StepSkipped = "StepSkipped";
    }

    public static class LogResults
    {
        public const string Success = "Success";
        public const string Violation = "Violation";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
    }

    public static class LogKeys
    {
        public const string Group = "group";
        public const string Title = "title";
        public const string Reason = "reason";
        public const string Attempt = "attempt";
        public const string OutOfTurn = "outOfTurn";
        public const string Event = "event";
        public const string Rules = "rules";
        public const string GroupViolations = "groupViolations";
    }
}
