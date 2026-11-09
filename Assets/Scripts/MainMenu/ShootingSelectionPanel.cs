using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Controls the Shooting Game Selection Screen / Briefing Modal.
    /// Displays high-impact battlefield artwork, mission intel, loadout stats,
    /// a DEPLOY/PLAY button to launch the ShootingGame scene, and a BACK button.
    /// </summary>
    public class ShootingSelectionPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Root & Panels")]
        [SerializeField, Tooltip("CanvasGroup of this selection panel for smooth transitions.")]
        private CanvasGroup _canvasGroup;

        [Header("Artwork & Visuals")]
        [SerializeField, Tooltip("Image component displaying the tactical shooting artwork banner.")]
        private Image _artworkImage;

        [SerializeField, Tooltip("Header text displaying the mission/game title.")]
        private TMP_Text _titleText;

        [SerializeField, Tooltip("Subtitle displaying the battlefield location.")]
        private TMP_Text _subtitleText;

        [SerializeField, Tooltip("Intel briefing / description text.")]
        private TMP_Text _briefingText;

        [Header("Buttons")]
        [SerializeField, Tooltip("Button that launches the ShootingGame scene.")]
        private Button _playButton;

        [SerializeField, Tooltip("Button that returns to the Main Menu.")]
        private Button _backButton;

        [Header("Controller Reference")]
        [SerializeField, Tooltip("Optional reference to the parent MainMenuController.")]
        private MainMenuController _mainMenuController;

        #endregion

        #region Constants

        private const string DEFAULT_TITLE = "SECTOR 7: URBAN WARFARE";
        private const string DEFAULT_SUBTITLE = "THIRD-PERSON ADVANCED TACTICAL COMBAT";
        private const string DEFAULT_BRIEFING =
            "• Arsenal: M4A1 Rifle, 9mm Pistol, SG-12 Shotgun, V-50 Sniper\n" +
            "• Hostiles: Tactical Soldiers, Heavy Juggernauts & Fast Skirmishers\n" +
            "• Missions: Sector Purge, Last Stand (Survival), Cyber Warlord\n" +
            "• Rewards: Gold Coins for Eliminations & Mission Completion\n" +
            "• Controls: Dual-Stick Touch / WASD + Mouse + 1-4 Weapon Switching";

        private MapSelectionController _mapSelectionController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // Phase 4 Map Selection Controller integration
            _mapSelectionController = GetComponent<MapSelectionController>();
            if (_mapSelectionController == null)
            {
                _mapSelectionController = gameObject.AddComponent<MapSelectionController>();
            }

            // Set default texts if assigned
            if (_titleText != null && string.IsNullOrEmpty(_titleText.text))
            {
                _titleText.text = DEFAULT_TITLE;
            }

            if (_subtitleText != null && string.IsNullOrEmpty(_subtitleText.text))
            {
                _subtitleText.text = DEFAULT_SUBTITLE;
            }

            if (_briefingText != null && string.IsNullOrEmpty(_briefingText.text))
            {
                _briefingText.text = DEFAULT_BRIEFING;
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
        /// Shows the shooting selection screen with smooth fade and scale.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _mapSelectionController?.UpdateDisplay();
            StopAllCoroutines();
            StartCoroutine(AnimateShow());
        }

        /// <summary>
        /// Hides the shooting selection screen and returns to main menu.
        /// </summary>
        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateHide());
        }

        /// <summary>
        /// Handles Play / Deploy button click: loads the actual ShootingGame scene.
        /// </summary>
        public void OnPlayPressed()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            Debug.Log("[ShootingSelectionPanel] Deploying to Shooting Game scene...");

            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(SceneLoader.SHOOTING);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.SHOOTING);
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
                _mainMenuController.CloseShootingSelection();
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
