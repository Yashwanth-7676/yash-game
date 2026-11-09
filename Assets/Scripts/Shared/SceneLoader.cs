using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YashGame.Shared
{
    /// <summary>
    /// Handles async scene loading with fade-to-black transitions and a progress bar.
    /// Attach to a GameObject with a CanvasGroup for the fade overlay and a Slider for progress.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class SceneLoader : SingletonMonoBehaviour<SceneLoader>
    {
        #region Scene Name Constants

        /// <summary>Splash screen scene name.</summary>
        public const string SPLASH = "SplashScreen";

        /// <summary>Main menu scene name.</summary>
        public const string MAIN_MENU = "MainMenu";

        /// <summary>Shooting game scene name.</summary>
        public const string SHOOTING = "ShootingGame";

        /// <summary>Racing game scene name.</summary>
        public const string RACING = "RacingGame";

        #endregion

        #region Fields

        [Header("Transition UI")]
        [SerializeField, Tooltip("CanvasGroup used for the fade-to-black overlay.")]
        private CanvasGroup _fadeCanvasGroup;

        [SerializeField, Tooltip("Slider that displays scene loading progress.")]
        private Slider _progressBar;

        [SerializeField, Tooltip("Optional loading status text.")]
        private TMPro.TMP_Text _loadingText;

        [Header("Transition Settings")]
        [SerializeField, Tooltip("Duration of the fade-in/out transition in seconds.")]
        private float _fadeDuration = 0.5f;

        [SerializeField, Tooltip("Minimum time the loading screen is displayed (seconds).")]
        private float _minimumLoadingDisplayTime = 1.0f;

        private bool _isLoading;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if a scene is currently being loaded.
        /// </summary>
        public bool IsLoading => _isLoading;

        #endregion

        #region Unity Lifecycle

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            // Auto-create fade overlay if not assigned
            if (_fadeCanvasGroup == null)
            {
                _fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
                if (_fadeCanvasGroup == null)
                {
                    GameObject overlay = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                    overlay.transform.SetParent(transform, false);
                    RectTransform rt = overlay.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    Image img = overlay.GetComponent<Image>();
                    img.color = Color.black;
                    _fadeCanvasGroup = overlay.GetComponent<CanvasGroup>();
                }
            }

            // Start with fade overlay hidden
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
                _fadeCanvasGroup.interactable = false;
            }

            // Hide progress bar initially
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(false);
            }

            if (_loadingText != null)
            {
                _loadingText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a scene by name with a fade transition and progress display.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] A scene is already being loaded. Ignoring request.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, null));
        }

        /// <summary>
        /// Loads a scene by name with a fade transition and reports progress via callback.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="onProgress">Callback invoked with progress value (0-1).</param>
        public void LoadScene(string sceneName, Action<float> onProgress)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] A scene is already being loaded. Ignoring request.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, onProgress));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine that handles the full scene loading sequence:
        /// fade out → load async → fade in.
        /// </summary>
        private IEnumerator LoadSceneRoutine(string sceneName, Action<float> onProgress)
        {
            _isLoading = true;

            // Notify GameManager
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.Loading);
            }

            // Show loading UI
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(true);
                _progressBar.value = 0f;
            }

            if (_loadingText != null)
            {
                _loadingText.gameObject.SetActive(true);
                _loadingText.text = "Loading...";
            }

            // Fade to black
            yield return StartCoroutine(Fade(0f, 1f));

            // Begin async loading
            float elapsedTime = 0f;
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            if (asyncLoad == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene '{sceneName}'. Does it exist in Build Settings?");
                _isLoading = false;
                yield return StartCoroutine(Fade(1f, 0f));
                yield break;
            }

            // Don't allow scene activation until we're ready
            asyncLoad.allowSceneActivation = false;

            // Wait for loading (Unity reports up to 0.9 before activation)
            while (!asyncLoad.isDone)
            {
                elapsedTime += Time.unscaledDeltaTime;

                // Unity's progress goes from 0 to 0.9, then jumps to 1.0 on activation
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                if (_progressBar != null)
                {
                    _progressBar.value = progress;
                }

                if (_loadingText != null)
                {
                    _loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
                }

                onProgress?.Invoke(progress);

                // When loading is essentially done (0.9), check minimum display time
                if (asyncLoad.progress >= 0.9f)
                {
                    if (elapsedTime >= _minimumLoadingDisplayTime)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            // Final progress update
            if (_progressBar != null)
            {
                _progressBar.value = 1f;
            }

            onProgress?.Invoke(1f);

            // Hide loading UI
            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(false);
            }

            if (_loadingText != null)
            {
                _loadingText.gameObject.SetActive(false);
            }

            // Fade back in
            yield return StartCoroutine(Fade(1f, 0f));

            _isLoading = false;
        }

        /// <summary>
        /// Smoothly fades the overlay CanvasGroup between two alpha values.
        /// </summary>
        /// <param name="from">Starting alpha value.</param>
        /// <param name="to">Ending alpha value.</param>
        private IEnumerator Fade(float from, float to)
        {
            if (_fadeCanvasGroup == null)
            {
                yield break;
            }

            _fadeCanvasGroup.blocksRaycasts = true;
            _fadeCanvasGroup.interactable = true;

            float elapsed = 0f;
            _fadeCanvasGroup.alpha = from;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                // Smooth step for nicer easing
                t = t * t * (3f - 2f * t);
                _fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _fadeCanvasGroup.alpha = to;

            // If fully transparent, stop blocking raycasts
            if (Mathf.Approximately(to, 0f))
            {
                _fadeCanvasGroup.blocksRaycasts = false;
                _fadeCanvasGroup.interactable = false;
            }
        }

        #endregion
    }
}
