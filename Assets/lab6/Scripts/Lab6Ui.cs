using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lab6
{
    public static class Lab6Ui
    {
        private static Font _defaultFont;
        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont != null) return _defaultFont;
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_defaultFont == null) _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _defaultFont;
            }
        }

        public static GameObject Panel(Transform parent, string name, Color bg)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        public static Text Label(Transform parent, string text, int size = 14, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = DefaultFont;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.fontStyle = style;
            t.supportRichText = true;
            return t;
        }

        public static Button Btn(Transform parent, string label, Action onClick, Color? color = null)
        {
            GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color ?? new Color(0.25f, 0.28f, 0.40f, 1f);
            Button b = go.GetComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(0.35f, 0.40f, 0.55f);
            cb.pressedColor = new Color(0.20f, 0.22f, 0.32f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => onClick());

            Text t = Label(go.transform, label, 14, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform rtt = t.GetComponent<RectTransform>();
            rtt.anchorMin = Vector2.zero;
            rtt.anchorMax = Vector2.one;
            rtt.offsetMin = Vector2.zero;
            rtt.offsetMax = Vector2.zero;
            return b;
        }

        public static InputField Input(Transform parent, string placeholder)
        {
            GameObject go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.16f);
            InputField ifd = go.GetComponent<InputField>();

            Text textComp = Label(go.transform, "", 12, TextAnchor.MiddleLeft);
            textComp.color = Color.white;
            RectTransform rtt = textComp.GetComponent<RectTransform>();
            rtt.anchorMin = Vector2.zero;
            rtt.anchorMax = Vector2.one;
            rtt.offsetMin = new Vector2(6, 2);
            rtt.offsetMax = new Vector2(-6, -2);

            Text plc = Label(go.transform, placeholder, 12, TextAnchor.MiddleLeft);
            plc.color = new Color(1f, 1f, 1f, 0.35f);
            plc.fontStyle = FontStyle.Italic;
            RectTransform rtp = plc.GetComponent<RectTransform>();
            rtp.anchorMin = Vector2.zero;
            rtp.anchorMax = Vector2.one;
            rtp.offsetMin = new Vector2(6, 2);
            rtp.offsetMax = new Vector2(-6, -2);

            ifd.textComponent = textComp;
            ifd.placeholder = plc;
            return ifd;
        }

        public static ScrollRect ScrollView(Transform parent, out RectTransform content, Color? bg = null)
        {
            GameObject sv = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            sv.transform.SetParent(parent, false);
            Image bgImg = sv.GetComponent<Image>();
            bgImg.color = bg ?? new Color(0.08f, 0.09f, 0.13f);

            ScrollRect sr = sv.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(sv.transform, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(4, 4);
            vpRt.offsetMax = new Vector2(-12, -4);
            sr.viewport = vpRt;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport.transform, false);
            content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 4;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            ContentSizeFitter csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.content = content;

            // Vertical scrollbar
            GameObject sb = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sb.transform.SetParent(sv.transform, false);
            sb.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.18f);
            RectTransform sbRt = sb.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = new Vector2(1, 1);
            sbRt.pivot = new Vector2(1, 0.5f);
            sbRt.sizeDelta = new Vector2(8, 0);
            sbRt.anchoredPosition = Vector2.zero;

            GameObject sliderArea = new GameObject("SlidingArea", typeof(RectTransform));
            sliderArea.transform.SetParent(sb.transform, false);
            RectTransform saRt = sliderArea.GetComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = Vector2.zero;
            saRt.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(sliderArea.transform, false);
            handle.GetComponent<Image>().color = new Color(0.35f, 0.40f, 0.55f);
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            Scrollbar sbar = sb.GetComponent<Scrollbar>();
            sbar.handleRect = handleRt;
            sbar.direction = Scrollbar.Direction.BottomToTop;
            sbar.targetGraphic = handle.GetComponent<Image>();

            sr.verticalScrollbar = sbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            sr.movementType = ScrollRect.MovementType.Clamped;

            return sr;
        }

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Stretch(GameObject go, float pad = 0f)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void SetSize(GameObject go, float w, float h)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
