using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Controls the Racing Game Selection Screen / Vehicle Specs Modal.
    /// Displays high-speed night track artwork, vehicle stats (top speed, acceleration, nitro),
    /// a START RACE/PLAY button to launch the RacingGame scene, and a BACK button.
    /// </summary>
    public class RacingSelectionPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Root & Panels")]
        [SerializeField, Tooltip("CanvasGroup of this selection panel for smooth transitions.")]
        private CanvasGroup _canvasGroup;

        [Header("Artwork & Visuals")]
        [SerializeField, Tooltip("Image component displaying the hyper racing artwork banner.")]
        private Image _artworkImage;

        [SerializeField, Tooltip("Header text displaying the race title.")]
        private TMP_Text _titleText;

        [SerializeField, Tooltip("Subtitle displaying track name & conditions.")]
        private TMP_Text _subtitleText;

        [SerializeField, Tooltip("Vehicle intel / track specs text.")]
        private TMP_Text _specsText;

        [Header("Buttons")]
        [SerializeField, Tooltip("Button that launches the RacingGame scene.")]
        private Button _playButton;

        [SerializeField, Tooltip("Button that returns to the Main Menu.")]
        private Button _backButton;

        [Header("Controller Reference")]
        [SerializeField, Tooltip("Optional reference to the parent MainMenuController.")]
        private MainMenuController _mainMenuController;

        #endregion

        #region Constants

        private const string DEFAULT_TITLE = "NEO-TOKYO: MIDNIGHT VELOCITY";
        private const string DEFAULT_SUBTITLE = "HIGH-OCTANE ASPHALT CIRCUIT";
        private const string DEFAULT_SPECS =
            "• Vehicle: Apex GT-R Hypercar (Cyber Red Edition)\n" +
            "• Engine: Twin-Turbo V8 Hybrid • 920 HP\n" +
            "• Top Speed: 345 KM/H • 0-100: 2.3s\n" +
            "• Nitro System: Dual Stage Cryo-Boost\n" +
            "• Opponents: 7 AI Competitors in Dynamic Grid\n" +
            "• Controls: Mobile Touch Steering / Acceleration / Brake";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_titleText != null && string.IsNullOrEmpty(_titleText.text))
            {
                _titleText.text = DEFAULT_TITLE;
            }

            if (_subtitleText != null && string.IsNullOrEmpty(_subtitleText.text))
            {
                _subtitleText.text = DEFAULT_SUBTITLE;
            }

            if (_specsText != null && string.IsNullOrEmpty(_specsText.text))
            {
                _specsText.text = DEFAULT_SPECS;
            }
        }

        private void OnEnable()
        {
            _playButton?.onClick.AddListener(OnPlayPressed);
            _backButton?.onClick.AddListener(OnBackPressed);
        }

        private void OnDisable()
        {
            _playButton?.onClick.RemoveListener(OnPlayPressed);
            _backButton?.onClick.RemoveListener(OnBackPressed);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the racing selection screen with smooth fade and scale.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateShow());
        }

        /// <summary>
        /// Hides the racing selection screen and returns to main menu.
        /// </summary>
        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateHide());
        }

        /// <summary>
        /// Handles Play / Start Race button click: loads the actual RacingGame scene.
        /// </summary>
        public void OnPlayPressed()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            Debug.Log("[RacingSelectionPanel] Starting Race: Loading Racing Game scene...");

            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(SceneLoader.RACING);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.RACING);
            }
        }

        /// <summary>
        /// Handles Back button click: returns to the main menu view.
        /// </summary>
        public void OnBackPressed()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            if (_mainMenuController != null)
            {
                _mainMenuController.CloseRacingSelection();
            }
            else
            {
                Hide();
            }
        }

        #endregion

        #region Private Animations

        private IEnumerator AnimateShow()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = true;

                transform.localScale = Vector3.one * 0.95f;

                float elapsed = 0f;
                float duration = 0.3f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _canvasGroup.alpha = t;
                    transform.localScale = Vector3.Lerp(Vector3.one * 0.95f, Vector3.one, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }

                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                transform.localScale = Vector3.one;
            }
        }

        private IEnumerator AnimateHide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;

                float elapsed = 0f;
                float duration = 0.25f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _canvasGroup.alpha = 1f - t;
                    transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.95f, t);
                    yield return null;
                }

                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        #endregion
    }
}
