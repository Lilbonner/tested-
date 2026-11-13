using System.Collections.Generic;
using System.IO;
using ScenarioBuilder.Demo;
using ScenarioBuilder.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScenarioBuilder.Editor
{
    /// <summary>
    /// Собирает демонстрационную сцену «Запуск насосной станции» с нуля: окружение, источники событий,
    /// 12 шагов и два режима (тренировка / экзамен). Нужен только для воспроизводимости демо —
    /// дальше сцена настраивается вручную в инспекторе.
    /// </summary>
    public static class DemoSceneBuilder
    {
        public const string Root = "Assets/ScenarioBuilder/Demo";
        public const string ScenePath = Root + "/ScenarioDemo.unity";
        const string SettingsFolder = Root + "/Settings";
        const string MaterialsFolder = Root + "/Materials";

        [MenuItem("Tools/Scenario/Пересобрать демо-сцену")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Демо-сцена",
                    "Сцена ScenarioDemo будет пересоздана, ручные изменения в ней пропадут. Продолжить?", "Пересоздать", "Отмена"))
                return;
            Build();
        }

        public static void Build()
        {
            EnsureFolder(Root);
            EnsureFolder(SettingsFolder);
            EnsureFolder(MaterialsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var ctx = new Context();

            SetupCamera();
            BuildEnvironment(ctx);
            BuildInteractiveObjects(ctx);
            BuildControlPanel(ctx);
            BuildScenario(ctx);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Scenario] Демо-сцена собрана: {ScenePath}");
        }

        /// <summary>Объекты сцены, на которые ссылаются шаги.</summary>
        sealed class Context
        {
            public TriggerEventSource Extinguisher, ZoneA, ZoneB, SafeZone, DangerZone, Gauge;
            public CollisionEventSource Terminal, ExitDoor;
            public UnityEventSource Valve;
            public ButtonEventSource Helmet, Gloves, PumpStart, Radio, Report, EmergencyStop;
            public ToggleEventSource Power;
        }

        // ---------- окружение ----------

        static void SetupCamera()
        {
            var camera = Camera.main;
            camera.orthographic = true;
            camera.orthographicSize = 9.4f;
            camera.transform.rotation = Quaternion.Euler(60, 0, 0);
            var target = new Vector3(-5.4f, 0, -0.6f);
            camera.transform.position = target - camera.transform.forward * 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        }

        static void BuildEnvironment(Context ctx)
        {
            var root = new GameObject("Environment").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root);
            floor.transform.localScale = new Vector3(20, 0.2f, 16);
            floor.transform.position = new Vector3(0, -0.1f, 0);
            Paint(floor, Mat("Floor", new Color(0.33f, 0.36f, 0.4f)));

            var wall = Mat("Wall", new Color(0.55f, 0.58f, 0.62f));
            Box(root, "Wall North", new Vector3(0, 0.75f, 8.2f), new Vector3(20.8f, 1.5f, 0.4f), wall);
            Box(root, "Wall South", new Vector3(0, 0.75f, -8.2f), new Vector3(20.8f, 1.5f, 0.4f), wall);
            Box(root, "Wall West", new Vector3(-10.2f, 0.75f, 0), new Vector3(0.4f, 1.5f, 16), wall);
            Box(root, "Wall East", new Vector3(10.2f, 0.75f, 1.25f), new Vector3(0.4f, 1.5f, 13.5f), wall);

            var pump = Box(root, "Pump (visual)", new Vector3(6.5f, 0.6f, 6.9f), new Vector3(3, 1.2f, 1.2f), Mat("Pump", new Color(0.25f, 0.45f, 0.75f)));
            Object.DestroyImmediate(pump.GetComponent<Collider>());
            Label(root, "НАСОС", new Vector3(6.5f, 1.4f, 6.9f));

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1, -6);
            Paint(player, Mat("Player", new Color(0.95f, 0.55f, 0.2f)));
            var body = player.AddComponent<Rigidbody>();
            body.mass = 70;
            player.AddComponent<PlayerMover>();
            Label(player.transform, "ВЫ", new Vector3(0, 1.5f, 0), 1.3f);
        }

        static void BuildInteractiveObjects(Context ctx)
        {
            var root = new GameObject("Interactive").transform;

            ctx.Extinguisher = Zone(root, "Огнетушитель", new Vector3(-4, 0, -3), new Vector2(2.4f, 2.4f), new Color(0.9f, 0.2f, 0.2f, 0.35f));
            var extinguisher = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            extinguisher.name = "Extinguisher (visual)";
            extinguisher.transform.SetParent(ctx.Extinguisher.transform, false);
            extinguisher.transform.localPosition = new Vector3(0, 0.5f, 0.6f);
            extinguisher.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
            Object.DestroyImmediate(extinguisher.GetComponent<Collider>());
            Paint(extinguisher, Mat("Extinguisher", new Color(0.85f, 0.1f, 0.1f)));

            ctx.ZoneA = Zone(root, "Зона A", new Vector3(-8.3f, 0, 5.6f), new Vector2(2.6f, 2.6f), new Color(1f, 0.85f, 0.2f, 0.35f));
            ctx.ZoneB = Zone(root, "Зона B", new Vector3(-3.5f, 0, 6.4f), new Vector2(2.6f, 2.4f), new Color(0.2f, 0.85f, 0.95f, 0.35f));
            ctx.SafeZone = Zone(root, "Безопасная зона", new Vector3(-1, 0, 0.5f), new Vector2(2.6f, 2.6f), new Color(0.2f, 0.9f, 0.3f, 0.35f));
            ctx.DangerZone = Zone(root, "Опасная зона", new Vector3(6.5f, 0, 4.2f), new Vector2(3.2f, 3f), new Color(1f, 0.1f, 0.1f, 0.45f));
            ctx.Gauge = Zone(root, "Манометр", new Vector3(8.4f, 0, -1.6f), new Vector2(2.2f, 2.2f), new Color(0.75f, 0.4f, 0.95f, 0.35f));

            ctx.Terminal = Solid(root, "Терминал", new Vector3(4, 0.75f, -3), new Vector3(1.4f, 1.5f, 0.7f), new Color(0.2f, 0.4f, 0.9f));
            ctx.ExitDoor = Solid(root, "Дверь выхода", new Vector3(10.15f, 1f, -6.3f), new Vector3(0.3f, 2f, 2.6f), new Color(0.45f, 0.3f, 0.15f));

            // Вентиль: E рядом → Interactable.onInteract (UnityEvent) → UnityEventSource.Raise()
            var valve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            valve.name = "Вентиль";
            valve.transform.SetParent(root);
            valve.transform.position = new Vector3(1.5f, 0.5f, 3.6f);
            valve.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
            Paint(valve, Mat("Valve", new Color(0.8f, 0.8f, 0.2f)));
            ctx.Valve = valve.AddComponent<UnityEventSource>();
            ctx.Valve.SetEventNames("Opened");
            var interactable = valve.AddComponent<Interactable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("highlight").objectReferenceValue = valve.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(interactable.onInteract, ctx.Valve.Raise);
            Label(root, "Вентиль [E]", valve.transform.position + new Vector3(0, 1.0f, 0));
        }

        static TriggerEventSource Zone(Transform parent, string name, Vector3 position, Vector2 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position + new Vector3(0, 0.5f, 0);
            go.transform.localScale = new Vector3(size.x, 1f, size.y);
            go.GetComponent<Collider>().isTrigger = true;
            Paint(go, Mat($"Zone_{name}", color, transparent: true));
            Label(parent, name, position + new Vector3(0, 1.2f, 0));
            return go.AddComponent<TriggerEventSource>();
        }

        static CollisionEventSource Solid(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var go = Box(parent, name, position, size, Mat($"Solid_{name}", color));
            Label(parent, name, position + new Vector3(0, size.y * 0.5f + 0.5f, 0));
            return go.AddComponent<CollisionEventSource>();
        }

        // ---------- пульт (UI Button / Toggle) ----------

        static void BuildControlPanel(Context ctx)
        {
            var canvas = UiFactory.Canvas("Control Panel (UI)", 0).transform;
            var bar = UiFactory.Panel(canvas, "Panel", UiFactory.PanelColor)
                .Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(170, 12), new Vector2(1500, 96));
            var layout = UiFactory.Horizontal(bar, 10);
            layout.padding = new RectOffset(14, 14, 14, 14);

            var title = UiFactory.Label(bar, "<b>ПУЛЬТ</b>", 22, TextAnchor.MiddleCenter);
            title.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;

            ctx.Helmet = PanelButton(bar, "Каска", UiFactory.ButtonColor);
            ctx.Gloves = PanelButton(bar, "Перчатки", UiFactory.ButtonColor);
            ctx.Power = PanelToggle(bar, "Питание");
            ctx.PumpStart = PanelButton(bar, "Пуск насоса", new Color(0.2f, 0.5f, 0.3f));
            ctx.Radio = PanelButton(bar, "Связь", UiFactory.ButtonColor);
            ctx.Report = PanelButton(bar, "Доклад", UiFactory.ButtonColor);
            ctx.EmergencyStop = PanelButton(bar, "Аварийный стоп", new Color(0.7f, 0.15f, 0.15f));

            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Control panel");
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        static ButtonEventSource PanelButton(Transform parent, string caption, Color color)
        {
            var button = UiFactory.Button(parent, caption, null, color, 20);
            button.navigation = new Navigation { mode = Navigation.Mode.None }; // стрелки двигают игрока, а не фокус UI
            return button.gameObject.AddComponent<ButtonEventSource>();
        }

        static ToggleEventSource PanelToggle(Transform parent, string caption)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 48;
            var background = go.GetComponent<Image>();
            background.color = new Color(0.3f, 0.3f, 0.34f);

            var check = new GameObject("On", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(go.transform, false);
            var checkImage = check.GetComponent<Image>();
            checkImage.color = new Color(0.95f, 0.75f, 0.2f);
            ((RectTransform)check.transform).Stretch(3);

            var label = UiFactory.Label(go.transform, caption, 20, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch(4);
            label.color = new Color(0.1f, 0.1f, 0.1f);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkImage;
            toggle.isOn = false;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            return go.AddComponent<ToggleEventSource>();
        }

        // ---------- сценарий: шаги и режимы ----------

        static void BuildScenario(Context c)
        {
            var root = new GameObject("Scenario");
            var stepsRoot = new GameObject("Steps").transform;
            stepsRoot.SetParent(root.transform);
            var modesRoot = new GameObject("Modes").transform;
            modesRoot.SetParent(root.transform);

            var s01 = Single(stepsRoot, "S01", "Надеть каску", "Нажмите «Каска» на пульте.", c.Helmet, ButtonEventSource.Click);
            var s02 = Single(stepsRoot, "S02", "Надеть перчатки", "Нажмите «Перчатки» на пульте.", c.Gloves, ButtonEventSource.Click);
            var s03 = Single(stepsRoot, "S03", "Осмотреть огнетушитель", "Подойдите к огнетушителю (красная зона). Не заходите в опасную зону.",
                c.Extinguisher, TriggerEventSource.Enter);
            s03.AddForbidden(new EventMatcher(c.DangerZone, TriggerEventSource.Enter));

            var s04 = Single(stepsRoot, "S04", "Отметиться у терминала", "Коснитесь синего терминала. Только в каске!",
                c.Terminal, CollisionEventSource.Hit);
            s04.AddCondition(new StepStatusCondition { step = s01 }.WithMessage("К терминалу допускают только в каске"));

            var s05 = Single(stepsRoot, "S05", "Включить питание", "Включите переключатель «Питание».", c.Power, ToggleEventSource.On);

            var s06 = Step<SequenceStep>(stepsRoot, "S06", "Обход: зона A, затем зона B", "Сначала жёлтая зона A, потом голубая зона B.");
            s06.SetSequence(true,
                new EventMatcher(c.ZoneA, TriggerEventSource.Enter),
                new EventMatcher(c.ZoneB, TriggerEventSource.Enter));

            var s07 = Single(stepsRoot, "S07", "Открыть вентиль", "Подойдите к вентилю и нажмите E. Питание должно быть включено.",
                c.Valve, "Opened");
            s07.AddCondition(new ToggleStateCondition { toggle = c.Power.GetComponent<Toggle>(), mustBeOn = true }
                .WithMessage("Вентиль открывают только при включённом питании"));

            var s08 = Single(stepsRoot, "S08", "Запустить насос из безопасной зоны",
                "Встаньте в зелёную зону и нажмите «Пуск насоса». Не заходите в опасную зону.", c.PumpStart, ButtonEventSource.Click);
            s08.AddCondition(new InsideZoneCondition { zone = c.SafeZone, mustBeInside = true }
                .WithMessage("Пуск — только из безопасной зоны"));
            s08.AddForbidden(new EventMatcher(c.DangerZone, TriggerEventSource.Enter));

            var s09 = Single(stepsRoot, "S09", "Проверить манометр", "Подойдите к манометру (фиолетовая зона).", c.Gauge, TriggerEventSource.Enter);
            var s10 = Single(stepsRoot, "S10", "Проверка связи", "Нажмите «Связь».", c.Radio, ButtonEventSource.Click);
            var s11 = Single(stepsRoot, "S11", "Доложить о запуске", "Нажмите «Доклад». Аварийный стоп без причины — нарушение.",
                c.Report, ButtonEventSource.Click);
            s11.AddForbidden(new EventMatcher(c.EmergencyStop, ButtonEventSource.Click));
            var s12 = Single(stepsRoot, "S12", "Покинуть помещение", "Подойдите к двери выхода (коричневая, справа внизу).",
                c.ExitDoor, CollisionEventSource.Hit);

            var trainingAnalyzer = Asset<TrainingScoreAnalyzer>("TrainingScoreAnalyzer", "Тренировка: баллы за попытки");
            var examAnalyzer = Asset<ExamPenaltyAnalyzer>("ExamPenaltyAnalyzer", "Экзамен: штрафы");
            var consoleLogger = Asset<ConsoleLogger>("ConsoleLogger", null);
            var jsonLogger = Asset<JsonFileLogger>("JsonFileLogger", null);

            var training = new GameObject("Тренировка").AddComponent<ScenarioMode>();
            training.transform.SetParent(modesRoot);
            training.Setup("Тренировка", new TrainingRules(), OutOfTurnPolicy.Violation, trainingAnalyzer,
                Group("Все шаги",
                    Batch(BatchActivation.Together, "Шаги 1–4 в любом порядке", s01, s02, s03, s04),
                    Batch(BatchActivation.OneByOne, "Шаги 5–8 по очереди", s05, s06, s07, s08),
                    Batch(BatchActivation.Together, "Шаги 9 и 10", s09, s10),
                    Batch(BatchActivation.Together, "Шаги 11 и 12", s11, s12)));

            var exam = new GameObject("Экзамен").AddComponent<ScenarioMode>();
            exam.transform.SetParent(modesRoot);
            exam.Setup("Экзамен", new ExamRules { violationThreshold = 2 }, OutOfTurnPolicy.Violation, examAnalyzer,
                Group("Подготовка", Batch(BatchActivation.Together, "", s01, s02, s03, s04)),
                Group("Запуск", Batch(BatchActivation.OneByOne, "", s05, s06, s07, s08)),
                Group("Завершение",
                    Batch(BatchActivation.Together, "", s09, s10),
                    Batch(BatchActivation.Together, "", s11, s12)));

            var runner = root.AddComponent<ScenarioRunner>();
            runner.Setup("Запуск насосной станции",
                new List<ScenarioMode> { training, exam },
                new List<ScenarioLogger> { consoleLogger, jsonLogger },
                new List<ScenarioAnalyzer> { trainingAnalyzer, examAnalyzer });
            root.AddComponent<ScenarioHud>().SetRunner(runner);
        }

        static SingleActionStep Single(Transform parent, string id, string title, string hint, ScenarioEventSource source, string eventName)
        {
            var step = Step<SingleActionStep>(parent, id, title, hint);
            step.SetAction(new EventMatcher(source, eventName));
            return step;
        }

        static T Step<T>(Transform parent, string id, string title, string hint) where T : ScenarioStep
        {
            var go = new GameObject($"{id} {title}");
            go.transform.SetParent(parent);
            var step = go.AddComponent<T>();
            step.Setup(id, title, hint);
            return step;
        }

        static StepGroup Group(string name, params ActivationBatch[] batches) =>
            new StepGroup { name = name, batches = new List<ActivationBatch>(batches) };

        static ActivationBatch Batch(BatchActivation activation, string label, params ScenarioStep[] steps) =>
            new ActivationBatch { label = label, activation = activation, steps = new List<ScenarioStep>(steps) };

        // ---------- ассеты и примитивы ----------

        static T Asset<T>(string fileName, string displayName) where T : ScriptableObject
        {
            var path = $"{SettingsFolder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            if (asset is ScenarioAnalyzer analyzer && displayName != null) analyzer.SetDisplayName(displayName);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Material Mat(string name, Color color, bool transparent = false)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (transparent)
            {
                material.SetFloat("_Mode", 2);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = size;
            Paint(go, material);
            return go;
        }

        static void Paint(GameObject go, Material material) => go.GetComponent<Renderer>().sharedMaterial = material;

        static void Label(Transform parent, string text, Vector3 position, float scale = 1f)
        {
            var go = new GameObject($"Label {text}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.rotation = Quaternion.Euler(60, 0, 0);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = UiFactory.DefaultFont;
            mesh.fontSize = 48;
            mesh.characterSize = 0.06f * scale;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            go.GetComponent<MeshRenderer>().sharedMaterial = UiFactory.DefaultFont.material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
