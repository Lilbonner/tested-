using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScenarioBuilder.UI
{
    /// <summary>Небольшой помощник для сборки uGUI из кода (стандартные компоненты Unity UI).</summary>
    public static class UiFactory
    {
        static Font s_font;

        public static Font DefaultFont => s_font ? s_font : s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static readonly Color PanelColor = new Color(0.09f, 0.11f, 0.14f, 0.92f);
        public static readonly Color ButtonColor = new Color(0.22f, 0.32f, 0.45f, 1f);
        public static readonly Color AccentColor = new Color(0.20f, 0.55f, 0.35f, 1f);

        public static Canvas Canvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float padding = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        public static VerticalLayoutGroup Vertical(RectTransform rt, int padding, float spacing, bool fitHeight)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (fitHeight)
                rt.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static RectTransform Row(Transform parent, float height, float spacing = 8)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Horizontal(rt, spacing);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleHeight = 0; // иначе группа с forceExpand заберёт свободное место у соседей
            return rt;
        }

        public static Text Label(Transform parent, string text, int size = 24, TextAnchor align = TextAnchor.UpperLeft,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.color = Color.white;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Transform parent, string caption, Action onClick, Color? color = null, int fontSize = 24)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color ?? ButtonColor;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            var label = Label(go.transform, caption, fontSize, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch(4);
            go.AddComponent<LayoutElement>().minHeight = 48;
            return button;
        }

        public static void SetCaption(this Button button, string caption) =>
            button.GetComponentInChildren<Text>().text = caption;

        /// <summary>Прокручиваемый текст (для отчёта и журнала).</summary>
        public static Text ScrollText(Transform parent, int fontSize, out ScrollRect scroll)
        {
            var root = Panel(parent, "Scroll", new Color(0, 0, 0, 0.35f));
            root.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(root, false);
            viewport.Stretch(12);

            var text = Label(viewport, "", fontSize);
            var content = text.rectTransform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return text;
        }
    }
}
