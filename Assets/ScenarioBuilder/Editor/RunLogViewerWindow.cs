using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// Окно редактора: список сохранённых прохождений (из Play Mode и из билда),
    /// повторная оценка любым анализатором и просмотр записей журнала. Сцена не запускается.
    /// </summary>
    public sealed class RunLogViewerWindow : EditorWindow
    {
        readonly List<(string path, RunLog log)> _runs = new List<(string, RunLog)>();
        List<ScenarioAnalyzer> _analyzers = new List<ScenarioAnalyzer>();
        RunLog _selected;
        string _error;
        ScenarioAnalyzer _analyzer;
        bool _showEntries;
        Vector2 _listScroll, _reportScroll;
        GUIStyle _rich, _mono;

        [MenuItem("Tools/Scenario/Журналы прохождений")]
        public static void Open() => GetWindow<RunLogViewerWindow>("Журналы прохождений");

        void OnEnable() => Refresh();

        void Refresh()
        {
            _runs.Clear();
            foreach (var path in RunLogStorage.List())
            {
                var log = RunLogStorage.Load(path, out _);
                _runs.Add((path, log));
            }
            _analyzers = AssetDatabase.FindAssets("t:ScenarioAnalyzer")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ScenarioAnalyzer>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null).ToList();
            if (_analyzer == null) _analyzer = _analyzers.FirstOrDefault();
        }

        void OnGUI()
        {
            _rich ??= new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            _mono ??= new GUIStyle(EditorStyles.label) { font = EditorStyles.miniFont, wordWrap = false, richText = false };

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(80))) Refresh();
                if (GUILayout.Button("Открыть файл…", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    var path = EditorUtility.OpenFilePanel("Журнал прохождения", RunLogStorage.Folder, "json");
                    if (!string.IsNullOrEmpty(path)) Select(RunLogStorage.Load(path, out _error));
                }
                if (GUILayout.Button("Папка журналов", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    Directory.CreateDirectory(RunLogStorage.Folder);
                    EditorUtility.RevealInFinder(RunLogStorage.Folder);
                }
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRunList();
                DrawReport();
            }
        }

        void DrawRunList()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.Width(260));
            _listScroll = scroll.scrollPosition;
            if (_runs.Count == 0)
                EditorGUILayout.HelpBox("Сохранённых прохождений нет. Пройдите сценарий с JsonFileLogger.", MessageType.Info);
            foreach (var (path, log) in _runs)
            {
                var caption = log != null ? $"{log.mode}  ·  {log.LocalStartTime}" : $"(повреждён) {Path.GetFileName(path)}";
                var style = log != null && log == _selected ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(caption, style)) Select(log);
            }
        }

        void DrawReport()
        {
            using var _ = new EditorGUILayout.VerticalScope();
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Выберите прохождение слева.", MessageType.None);
                return;
            }

            _analyzer = (ScenarioAnalyzer)EditorGUILayout.ObjectField("Анализатор", _analyzer, typeof(ScenarioAnalyzer), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var analyzer in _analyzers)
                    if (GUILayout.Toggle(_analyzer == analyzer, analyzer.DisplayName, EditorStyles.miniButton))
                        _analyzer = analyzer;
            }
            _showEntries = EditorGUILayout.ToggleLeft("Показать записи журнала", _showEntries);

            using var scroll = new EditorGUILayout.ScrollViewScope(_reportScroll);
            _reportScroll = scroll.scrollPosition;
            if (_analyzer != null)
                GUILayout.Label(_analyzer.Analyze(_selected).ToRichText(), _rich);
            else
                EditorGUILayout.HelpBox("Нет ни одного ассета анализатора (Create → Scenario → Analyzers).", MessageType.Warning);

            if (!_showEntries) return;
            EditorGUILayout.Space();
            foreach (var entry in _selected.entries)
                GUILayout.Label(entry.ToString(), _mono);
        }

        void Select(RunLog log)
        {
            _selected = log;
            if (log != null) _error = null;
        }
    }
}
