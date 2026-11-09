using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Controls the Player Profile modal dialog.
    /// Displays avatar, player username, level, experience bar,
    /// career shooting and racing statistics, and a close button.
    /// </summary>
    public class PlayerProfilePanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Root")]
        [SerializeField, Tooltip("CanvasGroup of the profile modal.")]
        private CanvasGroup _canvasGroup;

        [Header("Profile Info")]
        [SerializeField, Tooltip("Avatar portrait image.")]
        private Image _avatarImage;

        [SerializeField, Tooltip("Player name text.")]
        private TMP_Text _playerNameText;

        [SerializeField, Tooltip("Player level text.")]
        private TMP_Text _playerLevelText;

        [SerializeField, Tooltip("Player rank badge text.")]
        private TMP_Text _playerRankText;

        [SerializeField, Tooltip("XP progress slider.")]
        private Slider _xpSlider;

        [SerializeField, Tooltip("XP progress numbers text.")]
        private TMP_Text _xpText;

        [Header("Career Stats")]
        [SerializeField, Tooltip("Shooting combat stats summary text.")]
        private TMP_Text _shootingStatsText;

        [SerializeField, Tooltip("Racing career stats summary text.")]
        private TMP_Text _racingStatsText;

        [Header("Buttons")]
        [SerializeField, Tooltip("Close button.")]
        private Button _closeButton;

        [Header("Controller Reference")]
        [SerializeField, Tooltip("Reference to parent MainMenuController.")]
        private MainMenuController _mainMenuController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            _closeButton?.onClick.AddListener(OnClosePressed);
            RefreshProfileData();
        }

        private void OnDisable()
        {
            _closeButton?.onClick.RemoveListener(OnClosePressed);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes display values using saved PlayerPrefs or GameManager.
        /// </summary>
        public void RefreshProfileData()
        {
            string playerName = PlayerPrefs.GetString("YashGame_PlayerName", "Player_Yash");
            int playerLevel = PlayerPrefs.GetInt("YashGame_PlayerLevel", 24);
            int currentXP = PlayerPrefs.GetInt("YashGame_PlayerXP", 3850);
            int targetXP = 5000;

            if (_playerNameText != null) _playerNameText.text = playerName;
            if (_playerLevelText != null) _playerLevelText.text = $"LEVEL {playerLevel}";
            if (_playerRankText != null) _playerRankText.text = "APEX WARRIOR ★★★";

            if (_xpSlider != null)
            {
                _xpSlider.minValue = 0f;
                _xpSlider.maxValue = targetXP;
                _xpSlider.value = currentXP;
            }

            if (_xpText != null)
            {
                _xpText.text = $"{currentXP:N0} / {targetXP:N0} XP";
            }

            if (_shootingStatsText != null)
            {
                _shootingStatsText.text =
                    "COMBAT DOSSIER:\n" +
                    "• Matches Played: 48\n" +
                    "• Confirmed Kills: 136\n" +
                    "• Precision Accuracy: 68.4%\n" +
                    "• Win Rate: 72.9%";
            }

            if (_racingStatsText != null)
            {
                _racingStatsText.text =
                    "RACING TELEMETRY:\n" +
                    "• Races Entered: 56\n" +
                    "• 1st Place Podiums: 38\n" +
                    "• Top Speed Record: 345 KM/H\n" +
                    "• Total Drift Points: 184,200";
            }
        }

        /// <summary>
        /// Shows the profile dialog with scale and fade animation.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            RefreshProfileData();
            StopAllCoroutines();
            StartCoroutine(AnimateShow());
        }

        /// <summary>
        /// Hides the profile dialog.
        /// </summary>
        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateHide());
        }

        /// <summary>
        /// Close button handler.
        /// </summary>
        public void OnClosePressed()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            if (_mainMenuController != null)
            {
                _mainMenuController.CloseProfilePanel();
            }
            else
            {
                Hide();
            }
        }

        #endregion

        #region Animations

        private IEnumerator AnimateShow()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = true;

                transform.localScale = Vector3.one * 0.95f;

                float elapsed = 0f;
                float duration = 0.25f;

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
                float duration = 0.2f;

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
