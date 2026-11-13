using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioBuilder.UI
{
    /// <summary>
    /// Интерфейс прохождения: выбор режима, активные задания, сообщения о нарушениях,
    /// итоговый отчёт с переключением анализатора, журнал и сохранённые прохождения.
    /// Только отображает состояние — логики сценария здесь нет.
    /// </summary>
    [AddComponentMenu("Scenario/Scenario HUD")]
    public sealed class ScenarioHud : MonoBehaviour
    {
        [SerializeField] ScenarioRunner runner;
        [SerializeField] float messageSeconds = 4f;
        [Tooltip("Сколько последних записей журнала показывать в окне результатов.")]
        [SerializeField] int maxLogLines = 200;

        RectTransform _backdrop, _menu, _tasksPanel, _toastPanel, _results, _savedRuns, _savedList;
        Text _tasks, _toast, _resultsTitle, _resultsBody;
        ScrollRect _resultsScroll;
        RectTransform _analyzerRow;
        Button _restartButton;
        float _toastUntil;

        RunLog _shownLog;
        ScenarioAnalyzer _shownAnalyzer;
        bool _showLogTab;

        void Awake()
        {
            if (runner == null) runner = FindAnyObjectByType<ScenarioRunner>();
            UiFactory.EnsureEventSystem();
            Build();
        }

        void OnEnable()
        {
            if (runner == null) return;
            runner.SessionStarted += OnSessionStarted;
            runner.SessionFinished += OnSessionFinished;
        }

        void OnDisable()
        {
            if (runner == null) return;
            runner.SessionStarted -= OnSessionStarted;
            runner.SessionFinished -= OnSessionFinished;
        }

        void Start()
        {
            if (runner == null)
            {
                ShowToast(MessageKind.Error, "На сцене нет ScenarioRunner.");
                return;
            }
            if (runner.Session == null) ShowMenu();
        }

        void Update()
        {
            if (_toastPanel.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toastPanel.gameObject.SetActive(false);
        }

        // ---------- построение ----------

        void Build()
        {
            var canvas = UiFactory.Canvas("ScenarioHUD", 10, transform).transform;
            _backdrop = UiFactory.Panel(canvas, "Backdrop", new Color(0, 0, 0, 0.55f)).Stretch();

            _tasksPanel = UiFactory.Panel(canvas, "Tasks", UiFactory.PanelColor)
                .Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(620, 0));
            UiFactory.Vertical(_tasksPanel, 16, 6, true);
            _tasks = UiFactory.Label(_tasksPanel, "", 21);
            var menuRow = UiFactory.Row(_tasksPanel, 40);
            UiFactory.Button(menuRow, "Прервать и выйти в меню", () => runner.ReloadAndStart(-1),
                new Color(0.35f, 0.25f, 0.25f), 18);

            _toastPanel = UiFactory.Panel(canvas, "Toast", new Color(0, 0, 0, 0.8f))
                .Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(820, 0));
            UiFactory.Vertical(_toastPanel, 14, 0, true);
            _toast = UiFactory.Label(_toastPanel, "", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
            _toastPanel.gameObject.SetActive(false);

            BuildMenu(canvas);
            BuildResults(canvas);
            BuildSavedRuns(canvas);
        }

        void BuildMenu(Transform canvas)
        {
            _menu = UiFactory.Panel(canvas, "Menu", UiFactory.PanelColor)
                .Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 0));
            UiFactory.Vertical(_menu, 28, 14, true);
            UiFactory.Label(_menu, runner ? runner.ScenarioName : "Сценарий", 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(_menu,
                "WASD — движение, E — взаимодействие, мышь — кнопки пульта.\nВыберите режим прохождения:",
                20, TextAnchor.MiddleCenter);
            if (runner != null)
            {
                for (var i = 0; i < runner.Modes.Count; i++)
                {
                    var index = i;
                    var mode = runner.Modes[i];
                    if (mode == null) continue;
                    UiFactory.Button(_menu, mode.DisplayName, () => StartMode(index), UiFactory.AccentColor, 28);
                    UiFactory.Label(_menu, $"<color=#9AA8B8>{mode.Rules.Describe()}</color>", 18, TextAnchor.MiddleCenter);
                }
            }
            UiFactory.Button(_menu, "Сохранённые прохождения и повторная оценка", ShowSavedRuns);
        }

        void BuildResults(Transform canvas)
        {
            _results = UiFactory.Panel(canvas, "Results", UiFactory.PanelColor)
                .Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 940));
            UiFactory.Vertical(_results, 24, 10, false);
            _resultsTitle = UiFactory.Label(_results, "", 28, TextAnchor.MiddleLeft, FontStyle.Bold);

            UiFactory.Label(_results, "Анализатор (оценка пересчитывается по журналу без повторного прохождения):", 18);
            _analyzerRow = UiFactory.Row(_results, 44);

            var tabs = UiFactory.Row(_results, 40);
            UiFactory.Button(tabs, "Отчёт", () => { _showLogTab = false; RenderResults(); }, null, 20);
            UiFactory.Button(tabs, "Журнал событий", () => { _showLogTab = true; RenderResults(); }, null, 20);

            _resultsBody = UiFactory.ScrollText(_results, 20, out _resultsScroll);

            var bottom = UiFactory.Row(_results, 54);
            _restartButton = UiFactory.Button(bottom, "Пройти заново", RestartShownMode, UiFactory.AccentColor);
            UiFactory.Button(bottom, "Сохранённые прохождения", ShowSavedRuns);
            UiFactory.Button(bottom, "В меню", () => runner.ReloadAndStart(-1));
            _results.gameObject.SetActive(false);
        }

        void BuildSavedRuns(Transform canvas)
        {
            _savedRuns = UiFactory.Panel(canvas, "SavedRuns", UiFactory.PanelColor)
                .Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 0));
            UiFactory.Vertical(_savedRuns, 24, 10, true);
            UiFactory.Label(_savedRuns, "Сохранённые прохождения", 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Label(_savedRuns, $"<color=#9AA8B8>{RunLogStorage.Folder}</color>", 16, TextAnchor.MiddleCenter);
            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(_savedRuns, false);
            _savedList = (RectTransform)list.transform;
            UiFactory.Vertical(_savedList, 0, 6, false);
            UiFactory.Button(_savedRuns, "Назад", () =>
            {
                _savedRuns.gameObject.SetActive(false);
                if (_shownLog != null) _results.gameObject.SetActive(true);
                else ShowMenu();
            });
            _savedRuns.gameObject.SetActive(false);
        }

        // ---------- состояния экрана ----------

        void ShowMenu()
        {
            HideAll();
            _menu.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            // Затемнение под модальными окнами (меню, результаты, список прохождений).
            var modal = _menu.gameObject.activeSelf || _results.gameObject.activeSelf || _savedRuns.gameObject.activeSelf;
            if (_backdrop.gameObject.activeSelf != modal) _backdrop.gameObject.SetActive(modal);
        }

        void HideAll()
        {
            _menu.gameObject.SetActive(false);
            _tasksPanel.gameObject.SetActive(false);
            _results.gameObject.SetActive(false);
            _savedRuns.gameObject.SetActive(false);
        }

        void StartMode(int index)
        {
            // Первый запуск — сразу; повторный — через перезагрузку сцены, чтобы сбросить объекты.
            if (runner.Session == null) runner.StartMode(index);
            else runner.ReloadAndStart(index);
        }

        void RestartShownMode()
        {
            var mode = runner.Modes.FirstOrDefault(m => m != null && m.DisplayName == _shownLog?.mode);
            runner.ReloadAndStart(mode != null ? runner.IndexOf(mode) : -1);
        }

        void OnSessionStarted(ScenarioSession session)
        {
            HideAll();
            _tasksPanel.gameObject.SetActive(true);
            session.Changed += () => RenderTasks(session);
            session.Message += m => ShowToast(m.Kind, m.Text);
            RenderTasks(session);
        }

        void OnSessionFinished(ScenarioSession session)
        {
            RenderTasks(session);
            ShowResults(session.Log, runner.AnalyzerFor(session.Mode));
        }

        void ShowSavedRuns()
        {
            HideAll();
            foreach (Transform child in _savedList) Destroy(child.gameObject);
            var files = RunLogStorage.List().Take(10).ToList();
            if (files.Count == 0) UiFactory.Label(_savedList, "Пока нет сохранённых прохождений.", 20, TextAnchor.MiddleCenter);
            foreach (var path in files)
            {
                var log = RunLogStorage.Load(path, out var error);
                var caption = log != null
                    ? $"{log.mode} — {log.LocalStartTime}{(log.entries.Any(e => e.type == LogTypes.RunFinished) ? "" : "  (не завершено)")}"
                    : error;
                var button = UiFactory.Button(_savedList, caption, () => ShowResults(log, null), null, 20);
                button.interactable = log != null;
            }
            _savedRuns.gameObject.SetActive(true);
        }

        void ShowResults(RunLog log, ScenarioAnalyzer analyzer)
        {
            HideAll();
            _shownLog = log;
            _shownAnalyzer = analyzer ? analyzer : DefaultAnalyzerFor(log);
            _showLogTab = false;

            foreach (Transform child in _analyzerRow) Destroy(child.gameObject);
            foreach (var a in runner.Analyzers.Where(a => a != null))
            {
                var current = a;
                UiFactory.Button(_analyzerRow, a.DisplayName, () =>
                {
                    _shownAnalyzer = current;
                    _showLogTab = false;
                    RenderResults();
                }, null, 20);
            }
            _restartButton.SetCaption($"Пройти заново: {log.mode}");
            _results.gameObject.SetActive(true);
            RenderResults();
        }

        ScenarioAnalyzer DefaultAnalyzerFor(RunLog log)
        {
            var mode = runner.Modes.FirstOrDefault(m => m != null && m.DisplayName == log?.mode);
            return runner.AnalyzerFor(mode);
        }

        // ---------- отрисовка ----------

        void RenderResults()
        {
            if (_shownLog == null) return;
            foreach (Transform child in _analyzerRow)
            {
                var button = child.GetComponent<Button>();
                var isCurrent = _shownAnalyzer != null && child.name == _shownAnalyzer.DisplayName;
                button.image.color = isCurrent ? UiFactory.AccentColor : UiFactory.ButtonColor;
            }

            _resultsTitle.text = $"Результаты: {_shownLog.Title}";
            if (_showLogTab)
            {
                var entries = _shownLog.entries;
                var skip = Mathf.Max(0, entries.Count - maxLogLines);
                var sb = new StringBuilder();
                if (skip > 0) sb.AppendLine($"<color=#9AA8B8>… первые {skip} записей скрыты (полный журнал — в JSON)</color>");
                foreach (var entry in entries.Skip(skip)) sb.AppendLine(ColorEntry(entry));
                _resultsBody.text = sb.ToString();
            }
            else
            {
                _resultsBody.text = _shownAnalyzer != null
                    ? _shownAnalyzer.Analyze(_shownLog).ToRichText()
                    : "<color=#E0A030>Анализаторы не подключены к ScenarioRunner.</color>";
            }
            _resultsScroll.verticalNormalizedPosition = 1;
        }

        static string ColorEntry(LogEntry entry)
        {
            var text = entry.ToString();
            return entry.type switch
            {
                LogTypes.Violation or LogTypes.StepFailed => $"<color=#FF7A6E>{text}</color>",
                LogTypes.StepSkipped => $"<color=#B0B0B0>{text}</color>",
                LogTypes.StepCompleted => $"<color=#7CE38B>{text}</color>",
                LogTypes.GroupStarted or LogTypes.GroupFinished or LogTypes.RunStarted or LogTypes.RunFinished
                    => $"<b>{text}</b>",
                _ => text,
            };
        }

        void RenderTasks(ScenarioSession session)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=26><b>{session.Mode.DisplayName}</b></size>");
            var exam = session.Mode.Rules as ExamRules;
            foreach (var g in session.Groups.Where(g => g.Started && !g.Finished))
                sb.AppendLine(exam != null
                    ? $"Группа «{g.Name}» — нарушений {g.Violations} из {exam.violationThreshold}"
                    : $"Группа «{g.Name}»");

            var steps = session.Steps.ToList();
            var active = steps.Where(s => s.Status == StepStatus.Active).ToList();
            sb.AppendLine();
            sb.AppendLine(active.Count > 1 ? "<b>Задания (в любом порядке):</b>" : "<b>Задание:</b>");
            if (active.Count == 0) sb.AppendLine(session.IsFinished ? "Все задания завершены." : "—");
            foreach (var s in active)
            {
                var attempt = s.Attempt > 1 ? $"  <color=#E0A030>(попытка {s.Attempt})</color>" : "";
                sb.AppendLine($"<color=#FFE680>» {s.Step.Title}</color>{attempt}");
                if (!string.IsNullOrWhiteSpace(s.Step.Instruction))
                    sb.AppendLine($"   <size=18><color=#B8C4D0>{s.Step.Instruction}</color></size>");
            }

            sb.AppendLine();
            sb.Append("<size=18>");
            foreach (var group in steps.GroupBy(s => s.Group))
            {
                sb.Append($"<color=#8899AA>{group.Key}:</color> ");
                sb.Append(string.Join(" ", group.Select(s => $"<color={StatusColor(s.Status)}>{s.Step.Id}</color>")));
                sb.AppendLine();
            }
            sb.Append("<color=#7CE38B>выполнен</color>  <color=#FF7A6E>провален</color>  " +
                      "<color=#909090>пропущен</color>  <color=#FFE680>активен</color>  <color=#5A6470>ожидает</color></size>");
            _tasks.text = sb.ToString();
        }

        static string StatusColor(StepStatus status) => status switch
        {
            StepStatus.Completed => "#7CE38B",
            StepStatus.Failed => "#FF7A6E",
            StepStatus.Skipped => "#909090",
            StepStatus.Active => "#FFE680",
            _ => "#5A6470",
        };

        void ShowToast(MessageKind kind, string text)
        {
            var color = kind switch
            {
                MessageKind.Success => "#7CE38B",
                MessageKind.Warning => "#FFC857",
                MessageKind.Error => "#FF7A6E",
                _ => "#FFFFFF",
            };
            _toast.text = $"<color={color}>{text}</color>";
            _toastPanel.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + messageSeconds;
        }

        internal void SetRunner(ScenarioRunner value) => runner = value;
    }
}
