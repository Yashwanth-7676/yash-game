using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Controls the Splash Screen: animated logo entrance, dual blue/red accent glow,
    /// dynamic loading progress bar, simulated system warmups, and smooth transition to Main Menu.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Logo & Branding")]
        [SerializeField, Tooltip("Image component displaying the YASH GAME logo.")]
        private Image _logoImage;

        [SerializeField, Tooltip("Tagline text ('TWO WORLDS. ONE GAME.').")]
        private TMP_Text _taglineText;

        [SerializeField, Tooltip("CanvasGroup controlling entire splash UI transparency.")]
        private CanvasGroup _splashCanvasGroup;

        [Header("Progress Bar & Status")]
        [SerializeField, Tooltip("Slider representing loading progression (0 to 1).")]
        private Slider _progressBar;

        [SerializeField, Tooltip("Text showing current loading status message.")]
        private TMP_Text _statusText;

        [SerializeField, Tooltip("Text showing percentage readout ('75%').")]
        private TMP_Text _percentageText;

        [SerializeField, Tooltip("Tap to skip/continue button.")]
        private Button _skipButton;

        [Header("Timing")]
        [SerializeField, Tooltip("Minimum duration the splash screen is displayed (seconds).")]
        private float _splashDuration = 2.8f;

        [SerializeField, Tooltip("Next scene name to load after splash completes.")]
        private string _nextSceneName = SceneLoader.MAIN_MENU;

        #endregion

        #region Private Fields

        private bool _isTransitioning = false;

        private readonly string[] _statusSteps = new string[]
        {
            "INITIALIZING URP RENDER PIPELINE...",
            "CONFIGURING SHADER CACHE & TEXTURES...",
            "CALIBRATING AUDIO MIXERS...",
            "LOADING DUAL GAME SUBSYSTEMS...",
            "SYNCHRONIZING PLAYER PROFILE...",
            "LAUNCHING YASH GAME HUB..."
        };

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Set 60 FPS target early for smooth splash animations
            Application.targetFrameRate = 60;

            // Initialize default settings early
            SettingsManager.InitializeDefaults();

            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(OnSkipPressed);
            }

            StartCoroutine(RunSplashSequence());
        }

        private void OnDestroy()
        {
            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(OnSkipPressed);
            }
        }

        #endregion

        #region Sequence Logic

        /// <summary>
        /// Orchestrates the entire splash screen timeline.
        /// </summary>
        private IEnumerator RunSplashSequence()
        {
            // Initial state: start transparent
            if (_splashCanvasGroup != null)
            {
                _splashCanvasGroup.alpha = 0f;
            }

            if (_progressBar != null)
            {
                _progressBar.minValue = 0f;
                _progressBar.maxValue = 1f;
                _progressBar.value = 0f;
            }

            // 1. Smooth Fade-in
            float fadeElapsed = 0f;
            float fadeInDuration = 0.6f;
            while (fadeElapsed < fadeInDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeInDuration);
                if (_splashCanvasGroup != null)
                {
                    _splashCanvasGroup.alpha = t;
                }
                yield return null;
            }

            if (_splashCanvasGroup != null) _splashCanvasGroup.alpha = 1f;

            // 2. Animate logo pulse & fill progress bar
            float elapsed = 0f;
            int lastStepIndex = -1;

            while (elapsed < _splashDuration && !_isTransitioning)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _splashDuration);

                // Update progress bar
                if (_progressBar != null)
                {
                    _progressBar.value = progress;
                }

                // Update percentage text
                if (_percentageText != null)
                {
                    _percentageText.text = $"{(int)(progress * 100)}%";
                }

                // Update status step message
                int currentStep = Mathf.Min((int)(progress * _statusSteps.Length), _statusSteps.Length - 1);
                if (currentStep != lastStepIndex)
                {
                    lastStepIndex = currentStep;
                    if (_statusText != null)
                    {
                        _statusText.text = _statusSteps[currentStep];
                    }
                }

                // Gentle logo breathing pulse
                if (_logoImage != null)
                {
                    float scale = 1.0f + 0.03f * Mathf.Sin(elapsed * 4.0f);
                    _logoImage.transform.localScale = new Vector3(scale, scale, 1.0f);
                }

                yield return null;
            }

            // 3. Complete and transition
            if (!_isTransitioning)
            {
                yield return TransitionToMainMenu();
            }
        }

        /// <summary>
        /// Handles skip button click for instant entry.
        /// </summary>
        public void OnSkipPressed()
        {
            if (_isTransitioning) return;
            StopAllCoroutines();
            StartCoroutine(TransitionToMainMenu());
        }

        /// <summary>
        /// Fades out splash screen and navigates to Main Menu.
        /// </summary>
        private IEnumerator TransitionToMainMenu()
        {
            _isTransitioning = true;

            if (_progressBar != null) _progressBar.value = 1f;
            if (_percentageText != null) _percentageText.text = "100%";
            if (_statusText != null) _statusText.text = "READY!";

            yield return new WaitForSecondsRealtime(0.25f);

            // Fade out
            float fadeElapsed = 0f;
            float fadeOutDuration = 0.5f;

            while (fadeElapsed < fadeOutDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeOutDuration);
                if (_splashCanvasGroup != null)
                {
                    _splashCanvasGroup.alpha = 1f - t;
                }
                yield return null;
            }

            if (_splashCanvasGroup != null) _splashCanvasGroup.alpha = 0f;

            // Load MainMenu scene
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(_nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(_nextSceneName);
            }
        }

        #endregion
    }
}
