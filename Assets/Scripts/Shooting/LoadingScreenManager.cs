using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Smooth asynchronous scene & map loading screen manager.
    /// Displays map name, tactical briefing, rotating combat tips,
    /// dynamic progress bar, and animated spinner during scene transitions.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        public static LoadingScreenManager Instance { get; private set; }

        #region Serialized UI References

        [Header("UI Canvas & Panels")]
        [SerializeField] private GameObject _loadingCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Map Details Display")]
        [SerializeField] private TMP_Text _mapTitleText;
        [SerializeField] private TMP_Text _mapSubtitleText;
        [SerializeField] private Image _mapThemedAccentBar;

        [Header("Progress Elements")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _progressPercentText;
        [SerializeField] private RectTransform _spinnerIcon;

        [Header("Combat Tips")]
        [SerializeField] private TMP_Text _tacticalTipText;

        #endregion

        #region Tactical Tips Collection

        private static readonly string[] COMBAT_TIPS = new string[]
        {
            "💡 TIP: Shotgun deals massive burst damage at close quarters, but has wider pellet spread at distance.",
            "💡 TIP: Heavy Juggernauts explode on defeat! Keep your distance to avoid fatal area shockwaves.",
            "💡 TIP: Aiming down sights significantly tightens your weapon crosshair and recoil kick.",
            "💡 TIP: Quick-switch between weapons using hotkeys 1-4, Q, or swipe left/right across the upper screen.",
            "💡 TIP: Fast Skirmishers will flank aggressively through cover. Listen for footstep audio.",
            "💡 TIP: Clearing maps on Veteran (Hard) difficulty yields a 1.75× gold coin multiplier bonus!"
        };

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureLoadingUIHierarchy();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public Transition API

        /// <summary>
        /// Loads the target scene asynchronously while displaying custom map details and rotating tips.
        /// </summary>
        public void LoadBattlefieldAsync(string sceneName, MapData mapData)
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneRoutine(sceneName, mapData));
        }

        #endregion

        #region Asynchronous Loading Routine

        private IEnumerator LoadSceneRoutine(string sceneName, MapData mapData)
        {
            if (_loadingCanvas != null) _loadingCanvas.SetActive(true);

            // Populate Map Details
            if (mapData != null)
            {
                if (_mapTitleText != null) _mapTitleText.text = mapData.displayName.ToUpper();
                if (_mapSubtitleText != null) _mapSubtitleText.text = mapData.subtitle;
                if (_mapThemedAccentBar != null) _mapThemedAccentBar.color = mapData.themeAccentColor;
            }

            // Pick a random tip
            if (_tacticalTipText != null)
            {
                int tipIndex = UnityEngine.Random.Range(0, COMBAT_TIPS.Length);
                _tacticalTipText.text = COMBAT_TIPS[tipIndex];
            }

            // Fade In Loading Screen
            if (_canvasGroup != null)
            {
                float fadeIn = 0f;
                while (fadeIn < 0.25f)
                {
                    fadeIn += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeIn / 0.25f);
                    yield return null;
                }
                _canvasGroup.alpha = 1f;
            }

            // Begin Async Scene Load
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            float displayedProgress = 0f;

            while (!asyncOp.isDone)
            {
                // asyncOp.progress goes from 0.0 to 0.9 while loading
                float targetProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);

                while (displayedProgress < targetProgress)
                {
                    displayedProgress += Time.unscaledDeltaTime * 1.8f;
                    UpdateProgressUI(displayedProgress);

                    if (_spinnerIcon != null)
                    {
                        _spinnerIcon.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime * 1.5f);
                    }
                    yield return null;
                }

                if (asyncOp.progress >= 0.9f && displayedProgress >= 0.98f)
                {
                    UpdateProgressUI(1.0f);
                    yield return new WaitForSecondsRealtime(0.25f);
                    asyncOp.allowSceneActivation = true;
                }

                yield return null;
            }

            // Fade Out Loading Screen
            if (_canvasGroup != null)
            {
                float fadeOut = 0f;
                while (fadeOut < 0.35f)
                {
                    fadeOut += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeOut / 0.35f);
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }

            if (_loadingCanvas != null) _loadingCanvas.SetActive(false);
        }

        private void UpdateProgressUI(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = progress;
            }

            if (_progressPercentText != null)
            {
                _progressPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }
        }

        #endregion

        #region Procedural Loading UI

        private void EnsureLoadingUIHierarchy()
        {
            if (transform.Find("LoadingCanvas") != null) return;

            GameObject canvasObj = new GameObject("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Topmost overlay

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = canvasObj.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            _loadingCanvas = canvasObj;

            // Background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.98f);

            // Center Panel
            GameObject centerPanel = new GameObject("CenterPanel", typeof(RectTransform));
            centerPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform cpRt = centerPanel.GetComponent<RectTransform>();
            cpRt.anchoredPosition = Vector2.zero; cpRt.sizeDelta = new Vector2(1100f, 600f);

            // Title
            GameObject titleObj = new GameObject("MapTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(centerPanel.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 140f); tRt.sizeDelta = new Vector2(1000f, 70f);
            _mapTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            _mapTitleText.text = "DEPLOYING TO BATTLEFIELD";
            _mapTitleText.fontSize = 44;
            _mapTitleText.alignment = TextAlignmentOptions.Center;
            _mapTitleText.color = Color.white;

            // Subtitle
            GameObject subObj = new GameObject("MapSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(centerPanel.transform, false);
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 80f); sRt.sizeDelta = new Vector2(1000f, 40f);
            _mapSubtitleText = subObj.GetComponent<TextMeshProUGUI>();
            _mapSubtitleText.text = "OPERATION INITIALIZING...";
            _mapSubtitleText.fontSize = 26;
            _mapSubtitleText.alignment = TextAlignmentOptions.Center;
            _mapSubtitleText.color = new Color(0.0f, 0.85f, 1.0f);

            // Themed Accent Divider
            GameObject barObj = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(centerPanel.transform, false);
            RectTransform bRt = barObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 40f); bRt.sizeDelta = new Vector2(600f, 4f);
            _mapThemedAccentBar = barObj.GetComponent<Image>();
            _mapThemedAccentBar.color = new Color(0f, 0.85f, 1f);

            // Progress Bar (Slider)
            GameObject sliderObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(centerPanel.transform, false);
            RectTransform pbRt = sliderObj.GetComponent<RectTransform>();
            pbRt.anchoredPosition = new Vector2(0f, -40f); pbRt.sizeDelta = new Vector2(760f, 24f);

            _progressBar = sliderObj.GetComponent<Slider>();
            _progressBar.minValue = 0f; _progressBar.maxValue = 1f;

            // Slider Fill Area
            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.GetComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            Image fImg = fill.GetComponent<Image>();
            fImg.color = new Color(0f, 0.85f, 1f, 0.95f);
            _progressBar.fillRect = fRt;

            // Percent Text
            GameObject pctObj = new GameObject("PercentText", typeof(RectTransform), typeof(TextMeshProUGUI));
            pctObj.transform.SetParent(centerPanel.transform, false);
            RectTransform pctRt = pctObj.GetComponent<RectTransform>();
            pctRt.anchoredPosition = new Vector2(0f, -80f); pctRt.sizeDelta = new Vector2(200f, 35f);
            _progressPercentText = pctObj.GetComponent<TextMeshProUGUI>();
            _progressPercentText.text = "0%";
            _progressPercentText.fontSize = 24;
            _progressPercentText.alignment = TextAlignmentOptions.Center;
            _progressPercentText.color = Color.white;

            // Tactical Tip Box
            GameObject tipObj = new GameObject("TacticalTip", typeof(RectTransform), typeof(TextMeshProUGUI));
            tipObj.transform.SetParent(centerPanel.transform, false);
            RectTransform tipRt = tipObj.GetComponent<RectTransform>();
            tipRt.anchoredPosition = new Vector2(0f, -170f); tipRt.sizeDelta = new Vector2(960f, 70f);
            _tacticalTipText = tipObj.GetComponent<TextMeshProUGUI>();
            _tacticalTipText.text = COMBAT_TIPS[0];
            _tacticalTipText.fontSize = 22;
            _tacticalTipText.alignment = TextAlignmentOptions.Center;
            _tacticalTipText.color = new Color(0.75f, 0.85f, 0.95f);

            _loadingCanvas.SetActive(false);
        }

        #endregion
    }
}
