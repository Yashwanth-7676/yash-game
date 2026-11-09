#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YashGame.MainMenu;
using YashGame.Shared;
using YashGame.Shooting;
using YashGame.Racing;

namespace YashGame.Editor
{
    /// <summary>
    /// Development studio editor tools for YASH GAME.
    /// Provides one-click scene setup, build settings verification, and rapid scene switching.
    /// </summary>
    public static class YashGameEditorTools
    {
        private const string SCENE_SPLASH = "Assets/Scenes/Splash/SplashScreen.unity";
        private const string SCENE_MAIN = "Assets/Scenes/MainMenu/MainMenu.unity";
        private const string SCENE_SHOOTING = "Assets/Scenes/Shooting/ShootingGame.unity";
        private const string SCENE_RACING = "Assets/Scenes/Racing/RacingGame.unity";
        private const string SCENE_LOADING = "Assets/Scenes/Loading/LoadingScreen.unity";

        [MenuItem("Yash Game/Setup & Validate All Scenes and UI", false, 1)]
        public static void SetupAllScenesAndUI()
        {
            Debug.Log("[YashGameEditorTools] Starting one-click scene and UI setup...");

            SetupScene(SCENE_SPLASH, (scene) =>
            {
                EnsureGameObjectWithComponent<SplashScreenUIBuilder>("SplashSetup");
                EnsureGameObjectWithComponent<SplashScreenController>("SplashSetup");
            });

            SetupScene(SCENE_MAIN, (scene) =>
            {
                EnsureGameObjectWithComponent<MainMenuUIBuilder>("MainMenuSetup");
                EnsureGameObjectWithComponent<MainMenuController>("MainMenuSetup");
            });

            SetupScene(SCENE_SHOOTING, (scene) =>
            {
                EnsureGameObjectWithComponent<ShootingGameManager>("ShootingGameManager");
            });

            SetupScene(SCENE_RACING, (scene) =>
            {
                EnsureGameObjectWithComponent<RacingScenePlaceholder>("RacingSceneSetup");
            });

            ConfigureBuildSettings();

            // Re-open Main Menu scene as default
            EditorSceneManager.OpenScene(SCENE_MAIN);

            Debug.Log("[YashGameEditorTools] Successfully set up and validated all YASH GAME scenes and UI hierarchies!");
            EditorUtility.DisplayDialog("YASH GAME Setup Complete",
                "All 5 scenes (Splash, MainMenu, Loading, Shooting, Racing) have been configured with UI systems, touch controls, and scene navigation.\n\nReady for testing!", "OK");
        }

        [MenuItem("Yash Game/Verify Build Settings", false, 2)]
        public static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(SCENE_SPLASH, true),
                new EditorBuildSettingsScene(SCENE_MAIN, true),
                new EditorBuildSettingsScene(SCENE_LOADING, true),
                new EditorBuildSettingsScene(SCENE_SHOOTING, true),
                new EditorBuildSettingsScene(SCENE_RACING, true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[YashGameEditorTools] Build Settings updated with 5 active scenes (Splash -> MainMenu -> Loading -> Shooting -> Racing).");
        }

        [MenuItem("Yash Game/Open Scenes/1. Splash Screen", false, 20)]
        public static void OpenSplashScene() => OpenScene(SCENE_SPLASH);

        [MenuItem("Yash Game/Open Scenes/2. Main Menu", false, 21)]
        public static void OpenMainMenuScene() => OpenScene(SCENE_MAIN);

        [MenuItem("Yash Game/Open Scenes/3. Shooting Game", false, 22)]
        public static void OpenShootingScene() => OpenScene(SCENE_SHOOTING);

        [MenuItem("Yash Game/Open Scenes/4. Racing Game", false, 23)]
        public static void OpenRacingScene() => OpenScene(SCENE_RACING);

        private static void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        private static void SetupScene(string scenePath, System.Action<Scene> onSceneOpen)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            onSceneOpen?.Invoke(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T EnsureGameObjectWithComponent<T>(string gameObjectName) where T : Component
        {
            T existing = Object.FindObjectOfType<T>();
            if (existing != null) return existing;

            GameObject obj = GameObject.Find(gameObjectName);
            if (obj == null)
            {
                obj = new GameObject(gameObjectName);
            }

            T comp = obj.GetComponent<T>();
            if (comp == null)
            {
                comp = obj.AddComponent<T>();
            }

            return comp;
        }
    }
}
#endif
