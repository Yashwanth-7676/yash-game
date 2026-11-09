using UnityEngine;
using UnityEngine.SceneManagement;
using YashGame.MainMenu;
using YashGame.Shooting;
using YashGame.Racing;

namespace YashGame.Shared
{
    /// <summary>
    /// Global runtime bootstrapper for YASH GAME.
    /// Runs automatically on scene load to guarantee that all required singletons,
    /// UI builders, and controllers exist in every scene without requiring manual hierarchy setup.
    /// </summary>
    public static class YashGameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // Apply frame rate preference immediately on boot
            SettingsManager.InitializeDefaults();
            SettingsManager.ApplyTargetFPS();
            SettingsManager.ApplyGraphicsQuality();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeActiveScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeActiveScene(scene);
        }

        private static void InitializeActiveScene(Scene scene)
        {
            string sceneName = scene.name;
            Debug.Log($"[YashGameBootstrapper] Initializing scene: {sceneName}");

            // Ensure Core Singletons
            EnsureCoreSingletons();

            // Scene-specific auto-wiring
            switch (sceneName)
            {
                case SceneLoader.SPLASH:
                    EnsureSplashScene();
                    break;

                case SceneLoader.MAIN_MENU:
                    EnsureMainMenuScene();
                    break;

                case SceneLoader.SHOOTING:
                    EnsureShootingScene();
                    break;

                case SceneLoader.RACING:
                    EnsureRacingScene();
                    break;
            }
        }

        private static void EnsureCoreSingletons()
        {
            // GameManager
            if (!GameManager.HasInstance && Object.FindObjectOfType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager", typeof(GameManager));
                Object.DontDestroyOnLoad(gmObj);
            }

            // SceneLoader
            if (!SceneLoader.HasInstance && Object.FindObjectOfType<SceneLoader>() == null)
            {
                GameObject slObj = new GameObject("SceneLoader", typeof(SceneLoader), typeof(Canvas), typeof(CanvasGroup));
                Canvas c = slObj.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 999; // Always on top for transitions
                Object.DontDestroyOnLoad(slObj);
            }

            // AudioManager
            if (!AudioManager.HasInstance && Object.FindObjectOfType<AudioManager>() == null)
            {
                GameObject amObj = new GameObject("AudioManager", typeof(AudioManager));
                Object.DontDestroyOnLoad(amObj);
            }
        }

        private static void EnsureSplashScene()
        {
            if (Object.FindObjectOfType<SplashScreenController>() == null)
            {
                GameObject obj = new GameObject("SplashSetup", typeof(SplashScreenUIBuilder), typeof(SplashScreenController));
            }
        }

        private static void EnsureMainMenuScene()
        {
            if (Object.FindObjectOfType<MainMenuController>() == null)
            {
                GameObject obj = new GameObject("MainMenuSetup", typeof(MainMenuUIBuilder), typeof(MainMenuController));
            }
        }

        private static void EnsureShootingScene()
        {
            if (Object.FindObjectOfType<ShootingGameManager>() == null)
            {
                GameObject obj = new GameObject("ShootingGameManager", typeof(ShootingGameManager));
            }
        }

        private static void EnsureRacingScene()
        {
            if (Object.FindObjectOfType<RacingScenePlaceholder>() == null)
            {
                GameObject obj = new GameObject("RacingSceneSetup", typeof(RacingScenePlaceholder));
            }
        }
    }
}
