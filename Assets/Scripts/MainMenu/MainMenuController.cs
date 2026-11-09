using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Controls the main menu scene: button interactions, panel visibility, and UI animations.
    /// Attach to a root GameObject in the MainMenu scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region UI Button References

        [Header("Navigation Buttons")]
        [SerializeField, Tooltip("Button to launch the Shooting Game.")]
        private Button _shootingButton;

        [SerializeField, Tooltip("Button to launch the Racing Game.")]
        private Button _racingButton;

        [SerializeField, Tooltip("Button to open the Settings panel.")]
        private Button _settingsButton;

        [SerializeField, Tooltip("Button to open the Player Profile panel.")]
        private Button _profileButton;

        [SerializeField, Tooltip("Button to exit the application.")]
        private Button _exitButton;

        [SerializeField, Tooltip("Button to toggle sound on/off.")]
        private Button _soundToggleButton;

        [SerializeField, Tooltip("Icon or text on the sound toggle button.")]
        private TMP_Text _soundToggleLabel;

        #endregion

        #region Panel References

        [Header("Panels")]
        [SerializeField, Tooltip("The main menu panel containing all primary buttons.")]
        private CanvasGroup _mainPanel;

        [SerializeField, Tooltip("The settings panel (controlled by SettingsPanel script).")]
        private CanvasGroup _settingsPanel;

        [SerializeField, Tooltip("The player profile panel (controlled by PlayerProfilePanel script).")]
        private CanvasGroup _profilePanel;

        [SerializeField, Tooltip("The shooting game selection screen.")]
        private CanvasGroup _shootingSelectionPanel;

        [SerializeField, Tooltip("The racing game selection screen.")]
        private CanvasGroup _racingSelectionPanel;

        #endregion

        #region Animation Settings

        [Header("Animation Settings")]
        [SerializeField, Tooltip("Delay between each button's entrance animation.")]
        private float _staggerDelay = 0.15f;

        [SerializeField, Tooltip("Duration of each element's fade-in animation.")]
        private float _fadeInDuration = 0.4f;

        [SerializeField, Tooltip("List of CanvasGroups to animate in staggered order on scene start.")]
        private CanvasGroup[] _staggeredElements;

        #endregion

        #region References

        [Header("Components")]
        [SerializeField, Tooltip("Optional MainMenuUIAnimator for visual effects.")]
        private MainMenuUIAnimator _uiAnimator;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Set game state
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.MainMenu);
            }

            // Initialize settings on first run
            SettingsManager.InitializeDefaults();

            BindButtons();
            InitializePanels();
            StartCoroutine(AnimateEntrance());
            UpdateSoundToggleLabel();

            // Trigger the UI animator if assigned
            if (_uiAnimator != null)
            {
                _uiAnimator.PlayEntranceAnimations();
            }
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// Handles the Shooting Game button press. Opens the Shooting Selection screen (or loads scene).
        /// </summary>
        public void OnShootingPressed()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayButtonClick();

            if (_shootingSelectionPanel != null)
            {
                StartCoroutine(ShowSubPanel(_shootingSelectionPanel));
            }
            else if (SceneLoader.HasInstance)
            {
                StartCoroutine(ButtonPressAndLoad(_shootingButton?.transform, SceneLoader.SHOOTING));
            }
            else
            {
                Debug.LogError("[MainMenuController] SceneLoader instance not found.");
            }
        }

        /// <summary>
        /// Handles the Racing Game button press. Opens the Racing Selection screen (or loads scene).
        /// </summary>
        public void OnRacingPressed()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayButtonClick();

            if (_racingSelectionPanel != null)
            {
                StartCoroutine(ShowSubPanel(_racingSelectionPanel));
            }
            else if (SceneLoader.HasInstance)
            {
                StartCoroutine(ButtonPressAndLoad(_racingButton?.transform, SceneLoader.RACING));
            }
            else
            {
                Debug.LogError("[MainMenuController] SceneLoader instance not found.");
            }
        }

        /// <summary>
        /// Handles the Settings button press. Shows the settings panel with animation.
        /// </summary>
        public void OnSettingsPressed()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayButtonClick();
            StartCoroutine(ShowSettingsPanel());
        }

        /// <summary>
        /// Handles the Player Profile button press. Shows the profile panel with animation.
        /// </summary>
        public void OnProfilePressed()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayButtonClick();
            if (_profilePanel != null)
            {
                StartCoroutine(ShowSubPanel(_profilePanel));
            }
        }

        /// <summary>
        /// Handles the Exit button press. Quits the application.
        /// </summary>
        public void OnExitPressed()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlayButtonClick();
            Debug.Log("[MainMenuController] Exit requested.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Toggles game audio mute state.
        /// </summary>
        public void OnSoundTogglePressed()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.ToggleMusicMute();
                AudioManager.Instance.ToggleSFXMute();
                UpdateSoundToggleLabel();
            }
        }

        /// <summary>
        /// Closes the settings panel and returns to the main panel.
        /// </summary>
        public void CloseSettingsPanel()
        {
            StartCoroutine(HideSettingsPanel());
        }

        /// <summary>
        /// Closes the shooting selection panel and returns to the main menu.
        /// </summary>
        public void CloseShootingSelection()
        {
            if (_shootingSelectionPanel != null)
            {
                StartCoroutine(HideSubPanel(_shootingSelectionPanel));
            }
        }

        /// <summary>
        /// Closes the racing selection panel and returns to the main menu.
        /// </summary>
        public void CloseRacingSelection()
        {
            if (_racingSelectionPanel != null)
            {
                StartCoroutine(HideSubPanel(_racingSelectionPanel));
            }
        }

        /// <summary>
        /// Closes the player profile panel and returns to the main menu.
        /// </summary>
        public void CloseProfilePanel()
        {
            if (_profilePanel != null)
            {
                StartCoroutine(HideSubPanel(_profilePanel));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Binds click handlers to all buttons.
        /// </summary>
        private void BindButtons()
        {
            _shootingButton?.onClick.AddListener(OnShootingPressed);
            _racingButton?.onClick.AddListener(OnRacingPressed);
            _settingsButton?.onClick.AddListener(OnSettingsPressed);
            _profileButton?.onClick.AddListener(OnProfilePressed);
            _exitButton?.onClick.AddListener(OnExitPressed);
            _soundToggleButton?.onClick.AddListener(OnSoundTogglePressed);
        }

        /// <summary>
        /// Removes click handlers from all buttons.
        /// </summary>
        private void UnbindButtons()
        {
            _shootingButton?.onClick.RemoveListener(OnShootingPressed);
            _racingButton?.onClick.RemoveListener(OnRacingPressed);
            _settingsButton?.onClick.RemoveListener(OnSettingsPressed);
            _profileButton?.onClick.RemoveListener(OnProfilePressed);
            _exitButton?.onClick.RemoveListener(OnExitPressed);
            _soundToggleButton?.onClick.RemoveListener(OnSoundTogglePressed);
        }

        /// <summary>
        /// Sets initial panel visibility states.
        /// </summary>
        private void InitializePanels()
        {
            // Settings panel starts hidden
            if (_settingsPanel != null)
            {
                _settingsPanel.alpha = 0f;
                _settingsPanel.interactable = false;
                _settingsPanel.blocksRaycasts = false;
                _settingsPanel.gameObject.SetActive(false);
            }

            // Profile panel starts hidden
            if (_profilePanel != null)
            {
                _profilePanel.alpha = 0f;
                _profilePanel.interactable = false;
                _profilePanel.blocksRaycasts = false;
                _profilePanel.gameObject.SetActive(false);
            }

            // Shooting selection panel starts hidden
            if (_shootingSelectionPanel != null)
            {
                _shootingSelectionPanel.alpha = 0f;
                _shootingSelectionPanel.interactable = false;
                _shootingSelectionPanel.blocksRaycasts = false;
                _shootingSelectionPanel.gameObject.SetActive(false);
            }

            // Racing selection panel starts hidden
            if (_racingSelectionPanel != null)
            {
                _racingSelectionPanel.alpha = 0f;
                _racingSelectionPanel.interactable = false;
                _racingSelectionPanel.blocksRaycasts = false;
                _racingSelectionPanel.gameObject.SetActive(false);
            }

            // Main panel starts transparent for entrance animation
            if (_mainPanel != null)
            {
                _mainPanel.alpha = 0f;
                _mainPanel.interactable = false;
                _mainPanel.blocksRaycasts = false;
            }

            // Staggered elements start invisible
            if (_staggeredElements != null)
            {
                foreach (var element in _staggeredElements)
                {
                    if (element != null)
                    {
                        element.alpha = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Plays staggered entrance animations for all UI elements.
        /// </summary>
        private IEnumerator AnimateEntrance()
        {
            // Small initial delay for scene to settle
            yield return new WaitForSecondsRealtime(0.2f);

            // Fade in the main panel
            if (_mainPanel != null)
            {
                StartCoroutine(UIAnimationHelper.FadeIn(_mainPanel, _fadeInDuration));
                yield return new WaitForSecondsRealtime(_fadeInDuration * 0.5f);
            }

            // Staggered fade-in for individual elements
            if (_staggeredElements != null)
            {
                foreach (var element in _staggeredElements)
                {
                    if (element != null)
                    {
                        StartCoroutine(UIAnimationHelper.FadeIn(element, _fadeInDuration));
                        yield return new WaitForSecondsRealtime(_staggerDelay);
                    }
                }
            }
        }

        /// <summary>
        /// Shows the settings panel with a fade-in animation.
        /// </summary>
        private IEnumerator ShowSettingsPanel()
        {
            if (_settingsPanel == null) yield break;

            // Update game state
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.Settings);
            }

            _settingsPanel.gameObject.SetActive(true);

            // Fade out main panel
            if (_mainPanel != null)
            {
                yield return StartCoroutine(UIAnimationHelper.FadeOut(_mainPanel, _fadeInDuration * 0.5f));
            }

            // Fade in settings panel
            yield return StartCoroutine(UIAnimationHelper.FadeIn(_settingsPanel, _fadeInDuration));
        }

        /// <summary>
        /// Hides the settings panel and returns to the main panel.
        /// </summary>
        private IEnumerator HideSettingsPanel()
        {
            if (_settingsPanel == null) yield break;

            // Fade out settings panel
            yield return StartCoroutine(UIAnimationHelper.FadeOut(_settingsPanel, _fadeInDuration * 0.5f));
            _settingsPanel.gameObject.SetActive(false);

            // Fade in main panel
            if (_mainPanel != null)
            {
                yield return StartCoroutine(UIAnimationHelper.FadeIn(_mainPanel, _fadeInDuration));
            }

            // Return game state
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.MainMenu);
            }
        }

        /// <summary>
        /// Shows any sub-panel (ShootingSelection, RacingSelection, Profile) with smooth transition.
        /// </summary>
        private IEnumerator ShowSubPanel(CanvasGroup subPanel)
        {
            if (subPanel == null) yield break;

            subPanel.gameObject.SetActive(true);

            if (_mainPanel != null)
            {
                yield return StartCoroutine(UIAnimationHelper.FadeOut(_mainPanel, _fadeInDuration * 0.5f));
            }

            yield return StartCoroutine(UIAnimationHelper.FadeIn(subPanel, _fadeInDuration));
        }

        /// <summary>
        /// Hides any sub-panel and restores the main menu view.
        /// </summary>
        private IEnumerator HideSubPanel(CanvasGroup subPanel)
        {
            if (subPanel == null) yield break;

            yield return StartCoroutine(UIAnimationHelper.FadeOut(subPanel, _fadeInDuration * 0.5f));
            subPanel.gameObject.SetActive(false);

            if (_mainPanel != null)
            {
                yield return StartCoroutine(UIAnimationHelper.FadeIn(_mainPanel, _fadeInDuration));
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.MainMenu);
            }
        }

        /// <summary>
        /// Plays button press animation, then loads a scene.
        /// </summary>
        private IEnumerator ButtonPressAndLoad(Transform buttonTransform, string sceneName)
        {
            if (buttonTransform != null)
            {
                yield return StartCoroutine(UIAnimationHelper.ButtonPressAnimation(buttonTransform));
            }

            SceneLoader.Instance.LoadScene(sceneName);
        }

        /// <summary>
        /// Updates the sound toggle button label based on current mute state.
        /// </summary>
        private void UpdateSoundToggleLabel()
        {
            if (_soundToggleLabel == null) return;

            bool isMuted = AudioManager.HasInstance && AudioManager.Instance.IsMusicMuted;
            _soundToggleLabel.text = isMuted ? "🔇" : "🔊";
        }

        #endregion
    }
}
