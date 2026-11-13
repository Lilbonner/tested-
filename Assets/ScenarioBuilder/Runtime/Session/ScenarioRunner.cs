using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScenarioBuilder
{
    /// <summary>
    /// Точка сборки сценария в сцене: список режимов, подключённые логгеры и доступные анализаторы.
    /// Создаёт сессию для выбранного режима и передаёт ей события из шины.
    /// </summary>
    [AddComponentMenu("Scenario/Scenario Runner")]
    public sealed class ScenarioRunner : MonoBehaviour
    {
        [SerializeField] string scenarioName = "Сценарий";

        [Tooltip("Режимы прохождения. Шаги в режимах общие — режимы только ссылаются на них.")]
        [SerializeField] List<ScenarioMode> modes = new List<ScenarioMode>();

        [Tooltip("Логгеры (ассеты). Можно подключить несколько: консоль, JSON-файл, свой.")]
        [SerializeField] List<ScenarioLogger> loggers = new List<ScenarioLogger>();

        [Tooltip("Анализаторы (ассеты), между которыми можно переключаться после прохождения.")]
        [SerializeField] List<ScenarioAnalyzer> analyzers = new List<ScenarioAnalyzer>();

        [Tooltip("Индекс режима для автозапуска при старте сцены. -1 — показать меню выбора.")]
        [SerializeField] int autoStartMode = -1;

        static int s_pendingMode = -1;

        public string ScenarioName => scenarioName;
        public IReadOnlyList<ScenarioMode> Modes => modes;
        public IReadOnlyList<ScenarioAnalyzer> Analyzers => analyzers;
        public ScenarioSession Session { get; private set; }

        public event Action<ScenarioSession> SessionStarted;
        public event Action<ScenarioSession> SessionFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_pendingMode = -1;

        void OnEnable() => ScenarioEventBus.Raised += OnScenarioEvent;
        void OnDisable() => ScenarioEventBus.Raised -= OnScenarioEvent;

        void Start()
        {
            var mode = s_pendingMode >= 0 ? s_pendingMode : autoStartMode;
            s_pendingMode = -1;
            if (mode >= 0 && mode < modes.Count) StartMode(mode);
        }

        void OnDestroy() => Session?.Abort();
        void OnApplicationQuit() => Session?.Abort();

        public void StartMode(int index)
        {
            if (index < 0 || index >= modes.Count || modes[index] == null)
            {
                Debug.LogError($"[Scenario] Режим с индексом {index} не настроен.", this);
                return;
            }
            Session?.Abort();
            Session = new ScenarioSession(modes[index], scenarioName, loggers);
            foreach (var problem in Session.ConfigProblems)
                Debug.LogWarning($"[Scenario] {modes[index].DisplayName}: {problem}", modes[index]);
            Session.Finished += s => SessionFinished?.Invoke(s);
            SessionStarted?.Invoke(Session);
            Session.Start();
        }

        /// <summary>Перезагрузить сцену (сбросить объекты) и сразу запустить режим. -1 — в меню.</summary>
        public void ReloadAndStart(int modeIndex)
        {
            Session?.Abort();
            s_pendingMode = modeIndex;
            SceneManager.LoadScene(gameObject.scene.path);
        }

        public int IndexOf(ScenarioMode mode) => modes.IndexOf(mode);

        public ScenarioAnalyzer AnalyzerFor(ScenarioMode mode)
        {
            if (mode != null && mode.DefaultAnalyzer != null) return mode.DefaultAnalyzer;
            return analyzers.Count > 0 ? analyzers[0] : null;
        }

        void OnScenarioEvent(ScenarioEvent e) => Session?.HandleEvent(e);

        internal void Setup(string name, List<ScenarioMode> modeList, List<ScenarioLogger> loggerList,
            List<ScenarioAnalyzer> analyzerList)
        {
            scenarioName = name;
            modes = modeList;
            loggers = loggerList;
            analyzers = analyzerList;
        }
    }
}
