namespace ScenarioBuilder
{
    public enum StepStatus
    {
        /// <summary>Ещё не дошла очередь.</summary>
        Pending,
        /// <summary>Шаг доступен и принимает действия.</summary>
        Active,
        Completed,
        Failed,
        Skipped,
    }

    /// <summary>Что шаги и условия могут узнать о текущем прохождении.</summary>
    public interface IStepContext
    {
        StepStatus GetStatus(ScenarioStep step);
    }

    public enum ReactionKind { Ignored, Progress, Completed, Violation }

    /// <summary>Ответ шага на событие.</summary>
    public readonly struct StepReaction
    {
        public readonly ReactionKind Kind;
        /// <summary>Причина нарушения или описание прогресса.</summary>
        public readonly string Message;

        StepReaction(ReactionKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }

        public static readonly StepReaction Ignored = new StepReaction(ReactionKind.Ignored, null);
        public static StepReaction Progress(string message) => new StepReaction(ReactionKind.Progress, message);
        public static readonly StepReaction Completed = new StepReaction(ReactionKind.Completed, null);
        public static StepReaction Violation(string reason) => new StepReaction(ReactionKind.Violation, reason);
    }

    /// <summary>Что делать с действием шага, который ещё не активирован.</summary>
    public enum OutOfTurnPolicy
    {
        /// <summary>Зафиксировать нарушение (по умолчанию).</summary>
        Violation,
        /// <summary>Шаг полностью неактивен до своей очереди — действие игнорируется.</summary>
        Ignore,
    }

    public enum OutOfTurnOverride
    {
        /// <summary>Как настроено в режиме.</summary>
        UseModeSetting,
        Violation,
        Ignore,
    }
}
