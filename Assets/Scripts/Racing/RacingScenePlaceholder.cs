using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.Racing
{
    /// <summary>
    /// Placeholder controller for the Racing Game scene.
    /// Displays "Coming Soon" messaging and provides a back button to the main menu.
    /// </summary>
    public class RacingScenePlaceholder : MonoBehaviour
    {
        #region UI References

        [Header("UI Text Elements")]
        [SerializeField, Tooltip("Header text displaying 'RACING GAME'.")]
        private TMP_Text _headerText;

        [SerializeField, Tooltip("Subtitle text displaying 'Coming Soon'.")]
        private TMP_Text _subtitleText;

        [SerializeField, Tooltip("Environment description text.")]
        private TMP_Text _descriptionText;

        [Header("Buttons")]
        [SerializeField, Tooltip("Button to return to the main menu.")]
        private Button _backButton;

        [Header("Animation Settings")]
        [SerializeField, Tooltip("CanvasGroup for the entire placeholder UI.")]
        private CanvasGroup _contentCanvasGroup;

        [SerializeField, Tooltip("Duration of the entrance fade animation.")]
        private float _fadeInDuration = 0.6f;

        #endregion

        #region Content

        private const string HEADER_CONTENT = "RACING GAME";
        private const string SUBTITLE_CONTENT = "Coming Soon";
        private const string DESCRIPTION_CONTENT =
            "Get ready for high-speed thrills!\n\n" +
            "• Multiple tracks and environments\n" +
            "• Unlockable cars and customizations\n" +
            "• Competitive leaderboards\n\n" +
            "Stay tuned for updates!";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureHUD();
        }

        private void Start()
        {
            // Update game state
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.RacingGame);
            }

            SetupContent();
            BindButtons();
            StartCoroutine(AnimateEntrance());
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles the back button press. Returns to the main menu scene.
        /// </summary>
        public void OnBackPressed()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            StartCoroutine(BackToMenu());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets up text content for the placeholder UI.
        /// </summary>
        private void SetupContent()
        {
            if (_headerText != null) _headerText.text = HEADER_CONTENT;
            if (_subtitleText != null) _subtitleText.text = SUBTITLE_CONTENT;
            if (_descriptionText != null) _descriptionText.text = DESCRIPTION_CONTENT;

            // Start invisible for animation
            if (_contentCanvasGroup != null)
            {
                _contentCanvasGroup.alpha = 0f;
                _contentCanvasGroup.interactable = false;
                _contentCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Binds button click listeners.
        /// </summary>
        private void BindButtons()
        {
            _backButton?.onClick.AddListener(OnBackPressed);
        }

        /// <summary>
        /// Removes button click listeners.
        /// </summary>
        private void UnbindButtons()
        {
            _backButton?.onClick.RemoveListener(OnBackPressed);
        }

        /// <summary>
        /// Plays the entrance animation sequence: fade in content, then typewriter subtitle.
        /// </summary>
        private IEnumerator AnimateEntrance()
        {
            yield return new WaitForSecondsRealtime(0.2f);

            // Fade in the content
            if (_contentCanvasGroup != null)
            {
                yield return UIAnimationHelper.FadeIn(_contentCanvasGroup, _fadeInDuration);
            }

            // Typewriter effect on subtitle
            if (_subtitleText != null)
            {
                yield return UIAnimationHelper.TypewriterText(_subtitleText, SUBTITLE_CONTENT, 0.05f);
            }

            // Start a gentle pulse on the header
            if (_headerText != null)
            {
                StartCoroutine(UIAnimationHelper.PulseAnimation(_headerText.transform, 1.03f, 2.0f));
            }
        }

        /// <summary>
        /// Animates back button press, then loads the main menu.
        /// </summary>
        private IEnumerator BackToMenu()
        {
            if (_backButton != null)
            {
                yield return UIAnimationHelper.ButtonPressAnimation(_backButton.transform);
            }

            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(SceneLoader.MAIN_MENU);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.MAIN_MENU);
            }
        }

        /// <summary>
        /// Procedurally constructs a racing telemetry HUD and Return button if not in the scene.
        /// </summary>
        private void EnsureHUD()
        {
            if (GetComponentInChildren<Canvas>() != null || FindObjectOfType<Canvas>() != null) return;

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            GameObject canvasObj = new GameObject("RacingHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bg.GetComponent<Image>().color = new Color(0.06f, 0.02f, 0.04f, 1f);

            // Top Status Bar
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image), typeof(Outline));
            topBar.transform.SetParent(canvasObj.transform, false);
            RectTransform tbRt = topBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 1f); tbRt.anchorMax = new Vector2(0.5f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = new Vector2(0f, -40f);
            tbRt.sizeDelta = new Vector2(980f, 180f);
            topBar.GetComponent<Image>().color = new Color(0.14f, 0.04f, 0.08f, 0.95f);
            topBar.GetComponent<Outline>().effectColor = new Color(1f, 0.18f, 0.35f, 1f);

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(topBar.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 40f); tRt.sizeDelta = new Vector2(940f, 60f);
            _headerText = titleObj.GetComponent<TextMeshProUGUI>();
            _headerText.text = "NEO-TOKYO: MIDNIGHT HIGHWAY CIRCUIT";
            _headerText.fontSize = 38;
            _headerText.alignment = TextAlignmentOptions.Center;
            _headerText.color = Color.white;

            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(topBar.transform, false);
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -25f); sRt.sizeDelta = new Vector2(940f, 50f);
            _subtitleText = subObj.GetComponent<TextMeshProUGUI>();
            _subtitleText.text = "<color=#FF2E59>POSITION: 1ST / 8  •  LAP: 1 / 3  •  SPEED: 248 KM/H (GEAR 5)</color>";
            _subtitleText.fontSize = 26;
            _subtitleText.alignment = TextAlignmentOptions.Center;

            // Center Telemetry Box
            GameObject boxObj = new GameObject("TelemetryBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            boxObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bRt = boxObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 80f);
            bRt.sizeDelta = new Vector2(960f, 600f);
            boxObj.GetComponent<Image>().color = new Color(0.10f, 0.03f, 0.06f, 0.95f);
            boxObj.GetComponent<Outline>().effectColor = new Color(1f, 0.18f, 0.35f, 0.5f);

            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(boxObj.transform, false);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchoredPosition = Vector2.zero; dRt.sizeDelta = new Vector2(880f, 520f);
            _descriptionText = descObj.GetComponent<TextMeshProUGUI>();
            _descriptionText.text =
                "<size=36><color=#FF2E59>RACING GRID STANDBY</color></size>\n\n" +
                "• Universal Render Pipeline Active\n" +
                "• Sports Car Physics & Drift System Prepared\n" +
                "• AI Competitor Navigation & Speedway Route Configured\n" +
                "• Nitro Boost & Engine Audio Bus Ready\n\n" +
                "<color=#8FA0C0>Phase 1 Launcher & Navigation milestone complete.\n" +
                "Press button below to return to Main Menu.</color>";
            _descriptionText.fontSize = 28;
            _descriptionText.alignment = TextAlignmentOptions.Center;

            // Return Button
            GameObject btnObj = new GameObject("ReturnButton", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
            btnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f); btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 80f);
            btnRt.sizeDelta = new Vector2(940f, 130f);
            btnObj.GetComponent<Image>().color = new Color(0.55f, 0.08f, 0.16f, 1f);
            btnObj.GetComponent<Outline>().effectColor = new Color(1f, 0.18f, 0.35f, 1f);
            btnObj.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            _backButton = btnObj.GetComponent<Button>();

            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = btnTextObj.GetComponent<RectTransform>();
            btRt.anchoredPosition = Vector2.zero; btRt.sizeDelta = new Vector2(900f, 90f);
            TMP_Text bt = btnTextObj.GetComponent<TextMeshProUGUI>();
            bt.text = "◀ RETURN TO YASH GAME HUB";
            bt.fontSize = 36;
            bt.alignment = TextAlignmentOptions.Center;
            bt.color = Color.white;
        }

        #endregion
    }
}
