using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Procedurally builds, styles, and links the Splash Screen UI hierarchy
    /// if not already baked in the scene. Ensures 100% immediate playability,
    /// responsive portrait layout (1080x1920), glowing logo, animated progress bar,
    /// and smooth transition to the Main Menu.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SplashScreenUIBuilder : MonoBehaviour
    {
        private static readonly Color ColorDarkBg = new Color(0.02f, 0.028f, 0.065f, 1f); // #050710
        private static readonly Color ColorCyanNeon = new Color(0f, 0.85f, 1f, 1f);
        private static readonly Color ColorRedNeon = new Color(1f, 0.18f, 0.35f, 1f);
        private static readonly Color ColorCardDark = new Color(0.05f, 0.07f, 0.15f, 0.95f);
        private static readonly Color ColorTextWhite = new Color(0.96f, 0.97f, 1f, 1f);
        private static readonly Color ColorTextMuted = new Color(0.65f, 0.72f, 0.85f, 1f);

        private void Awake()
        {
            EnsureEventSystem();
            BuildUIHierarchy();
        }

        public void BuildUIHierarchy()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null && canvas.transform.Find("SplashContent") != null)
            {
                Debug.Log("[SplashScreenUIBuilder] Splash Canvas already exists. Skipping.");
                return;
            }

            Debug.Log("[SplashScreenUIBuilder] Building Splash Screen UI...");

            // 1. Root Canvas
            GameObject canvasObj = new GameObject("SplashCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);

            Canvas rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 2. Fullscreen Background
            GameObject bgObj = CreateUIElement("Background", canvasObj.transform);
            SetStretchFull(bgObj.GetComponent<RectTransform>());
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = ColorDarkBg;

            // 3. Splash Content Group
            GameObject contentObj = CreateUIElement("SplashContent", canvasObj.transform);
            SetStretchFull(contentObj.GetComponent<RectTransform>());
            CanvasGroup cg = contentObj.AddComponent<CanvasGroup>();

            // Setup Controller
            SplashScreenController controller = gameObject.GetComponent<SplashScreenController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<SplashScreenController>();
            }

            // 4. Logo & Branding
            Sprite logoSprite = LoadSprite("Textures/YashGame_Logo");
            Image logoImg = null;

            if (logoSprite != null)
            {
                GameObject logoObj = CreateUIElement("SplashLogo", contentObj.transform);
                RectTransform logoRt = logoObj.GetComponent<RectTransform>();
                logoRt.anchoredPosition = new Vector2(0f, 180f);
                logoRt.sizeDelta = new Vector2(920f, 460f);

                logoImg = logoObj.AddComponent<Image>();
                logoImg.sprite = logoSprite;
                logoImg.preserveAspect = true;
            }
            else
            {
                CreateText("LogoText", contentObj.transform, "<color=#00D9FF>YASH</color> <color=#FF2E59>GAME</color>",
                    new Vector2(0f, 220f), new Vector2(900f, 140f), 88, TextAlignmentOptions.Center);
                CreateText("TaglineText", contentObj.transform, "— TWO WORLDS. ONE GAME. —",
                    new Vector2(0f, 120f), new Vector2(900f, 50f), 32, TextAlignmentOptions.Center);
            }

            // 5. Progress Bar Box
            GameObject barFrameObj = CreateUIElement("ProgressFrame", contentObj.transform);
            RectTransform frameRt = barFrameObj.GetComponent<RectTransform>();
            frameRt.anchoredPosition = new Vector2(0f, -320f);
            frameRt.sizeDelta = new Vector2(860f, 40f);

            Image frameImg = barFrameObj.AddComponent<Image>();
            frameImg.color = new Color(0.04f, 0.06f, 0.12f, 1f);

            Outline frameOutline = barFrameObj.AddComponent<Outline>();
            frameOutline.effectColor = ColorCyanNeon;
            frameOutline.effectDistance = new Vector2(2f, -2f);

            // Slider
            Slider slider = barFrameObj.AddComponent<Slider>();

            // Fill Area
            GameObject fillArea = CreateUIElement("FillArea", barFrameObj.transform);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0f);
            faRt.anchorMax = new Vector2(1f, 1f);
            faRt.sizeDelta = new Vector2(-8f, -8f);
            faRt.anchoredPosition = Vector2.zero;

            GameObject fill = CreateUIElement("Fill", fillArea.transform);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = ColorCyanNeon;

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            // Percentage Text
            TMP_Text pctText = CreateText("PercentageText", contentObj.transform, "0%",
                new Vector2(0f, -260f), new Vector2(400f, 60f), 38, TextAlignmentOptions.Center);

            // Status Message Text
            TMP_Text statusText = CreateText("StatusText", contentObj.transform, "INITIALIZING SYSTEM...",
                new Vector2(0f, -390f), new Vector2(860f, 50f), 26, TextAlignmentOptions.Center);
            statusText.color = ColorTextMuted;

            // 6. Skip Button (Fullscreen transparent click surface)
            GameObject skipObj = CreateUIElement("SkipButton", contentObj.transform);
            SetStretchFull(skipObj.GetComponent<RectTransform>());
            Button skipBtn = skipObj.AddComponent<Button>();

            Image skipImg = skipObj.AddComponent<Image>();
            skipImg.color = new Color(0f, 0f, 0f, 0f); // invisible touch target

            CreateText("SkipHint", contentObj.transform, "<size=22><color=#607090>TAP ANYWHERE TO SKIP</color></size>",
                new Vector2(0f, -600f), new Vector2(800f, 40f), 24, TextAlignmentOptions.Center);

            // Wire into controller
            SetPrivateField(controller, "_splashCanvasGroup", cg);
            SetPrivateField(controller, "_logoImage", logoImg);
            SetPrivateField(controller, "_progressBar", slider);
            SetPrivateField(controller, "_percentageText", pctText);
            SetPrivateField(controller, "_statusText", statusText);
            SetPrivateField(controller, "_skipButton", skipBtn);

            Debug.Log("[SplashScreenUIBuilder] Splash Screen UI built successfully!");
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetStretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            GameObject textObj = CreateUIElement(name, parent);
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = ColorTextWhite;
            return tmp;
        }

        private Sprite LoadSprite(string resourcePath)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
