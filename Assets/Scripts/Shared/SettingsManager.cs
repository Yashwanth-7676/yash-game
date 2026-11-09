using UnityEngine;

namespace YashGame.Shared
{
    /// <summary>
    /// Static settings manager that handles persistent game settings via PlayerPrefs.
    /// Does not require a MonoBehaviour or GameObject — call directly from any script.
    /// </summary>
    public static class SettingsManager
    {
        #region PlayerPrefs Key Constants

        /// <summary>PlayerPrefs key for graphics quality level (int 0-2).</summary>
        public const string KEY_GRAPHICS_QUALITY = "YashGame_GraphicsQuality";

        /// <summary>PlayerPrefs key for master volume (float 0-1).</summary>
        public const string KEY_MASTER_VOLUME = "YashGame_MasterVolume";

        /// <summary>PlayerPrefs key for music volume (float 0-1).</summary>
        public const string KEY_MUSIC_VOLUME = "YashGame_MusicVolume";

        /// <summary>PlayerPrefs key for SFX volume (float 0-1).</summary>
        public const string KEY_SFX_VOLUME = "YashGame_SFXVolume";

        /// <summary>PlayerPrefs key for controls sensitivity (float 0.1-2.0).</summary>
        public const string KEY_SENSITIVITY = "YashGame_Sensitivity";

        /// <summary>PlayerPrefs key for target FPS index (0 = 30 FPS, 1 = 60 FPS, 2 = 90 FPS, 3 = Max/Uncapped).</summary>
        public const string KEY_TARGET_FPS = "YashGame_TargetFPS";

        /// <summary>PlayerPrefs key indicating settings have been initialized at least once.</summary>
        public const string KEY_INITIALIZED = "YashGame_SettingsInitialized";

        #endregion

        #region Default Values

        /// <summary>Default graphics quality level (1 = Medium).</summary>
        public const int DEFAULT_GRAPHICS_QUALITY = 1;

        /// <summary>Default target FPS index (1 = 60 FPS for smooth mobile gameplay).</summary>
        public const int DEFAULT_TARGET_FPS = 1;

        /// <summary>Default master volume.</summary>
        public const float DEFAULT_MASTER_VOLUME = 1.0f;

        /// <summary>Default music volume.</summary>
        public const float DEFAULT_MUSIC_VOLUME = 0.8f;

        /// <summary>Default SFX volume.</summary>
        public const float DEFAULT_SFX_VOLUME = 1.0f;

        /// <summary>Default controls sensitivity.</summary>
        public const float DEFAULT_SENSITIVITY = 1.0f;

        /// <summary>Minimum allowed sensitivity value.</summary>
        public const float MIN_SENSITIVITY = 0.1f;

        /// <summary>Maximum allowed sensitivity value.</summary>
        public const float MAX_SENSITIVITY = 2.0f;

        #endregion

        #region Properties

        /// <summary>
        /// Graphics quality level: 0 = Low, 1 = Medium, 2 = High.
        /// </summary>
        public static int GraphicsQuality
        {
            get => PlayerPrefs.GetInt(KEY_GRAPHICS_QUALITY, DEFAULT_GRAPHICS_QUALITY);
            set
            {
                int clamped = Mathf.Clamp(value, 0, 2);
                PlayerPrefs.SetInt(KEY_GRAPHICS_QUALITY, clamped);
            }
        }

        /// <summary>
        /// Master volume (0.0 – 1.0).
        /// </summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, DEFAULT_MASTER_VOLUME);
            set => PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, Mathf.Clamp01(value));
        }

        /// <summary>
        /// Music volume (0.0 – 1.0).
        /// </summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
            set => PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, Mathf.Clamp01(value));
        }

        /// <summary>
        /// SFX volume (0.0 – 1.0).
        /// </summary>
        public static float SFXVolume
        {
            get => PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
            set => PlayerPrefs.SetFloat(KEY_SFX_VOLUME, Mathf.Clamp01(value));
        }

        /// <summary>
        /// Controls sensitivity (0.1 – 2.0).
        /// </summary>
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);
            set => PlayerPrefs.SetFloat(KEY_SENSITIVITY, Mathf.Clamp(value, MIN_SENSITIVITY, MAX_SENSITIVITY));
        }

        /// <summary>
        /// Target FPS preference index: 0 = 30 FPS, 1 = 60 FPS, 2 = 90 FPS, 3 = Max/Uncapped (-1).
        /// </summary>
        public static int TargetFPSIndex
        {
            get => PlayerPrefs.GetInt(KEY_TARGET_FPS, DEFAULT_TARGET_FPS);
            set
            {
                int clamped = Mathf.Clamp(value, 0, 3);
                PlayerPrefs.SetInt(KEY_TARGET_FPS, clamped);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes settings with default values if this is the first run.
        /// Call this early (e.g., from GameManager.Awake).
        /// </summary>
        public static void InitializeDefaults()
        {
            if (PlayerPrefs.GetInt(KEY_INITIALIZED, 0) == 1)
            {
                Debug.Log("[SettingsManager] Settings already initialized. Loading existing values.");
                ApplyGraphicsQuality();
                return;
            }

            Debug.Log("[SettingsManager] First run detected. Applying default settings.");
            ResetToDefaults();
            PlayerPrefs.SetInt(KEY_INITIALIZED, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets all settings to their default values and saves immediately.
        /// </summary>
        public static void ResetToDefaults()
        {
            GraphicsQuality = DEFAULT_GRAPHICS_QUALITY;
            TargetFPSIndex = DEFAULT_TARGET_FPS;
            MasterVolume = DEFAULT_MASTER_VOLUME;
            MusicVolume = DEFAULT_MUSIC_VOLUME;
            SFXVolume = DEFAULT_SFX_VOLUME;
            Sensitivity = DEFAULT_SENSITIVITY;

            Save();
            ApplyGraphicsQuality();
            ApplyTargetFPS();

            Debug.Log("[SettingsManager] All settings reset to defaults.");
        }

        /// <summary>
        /// Saves all pending PlayerPrefs changes to disk.
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
            Debug.Log("[SettingsManager] Settings saved.");
        }

        /// <summary>
        /// Loads and applies all settings. Call after modifying values if immediate
        /// application is needed beyond just the PlayerPrefs write.
        /// </summary>
        public static void LoadAndApply()
        {
            ApplyGraphicsQuality();
            ApplyTargetFPS();

            // Apply audio settings if AudioManager exists
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetMusicVolume(MusicVolume * MasterVolume);
                AudioManager.Instance.SetSFXVolume(SFXVolume * MasterVolume);
            }

            Debug.Log($"[SettingsManager] Settings applied – Quality: {GraphicsQuality}, FPS: {GetTargetFPSValue(TargetFPSIndex)}, " +
                      $"Master: {MasterVolume:F2}, Music: {MusicVolume:F2}, " +
                      $"SFX: {SFXVolume:F2}, Sensitivity: {Sensitivity:F2}");
        }

        /// <summary>
        /// Applies the target frame rate preference.
        /// </summary>
        public static void ApplyTargetFPS()
        {
            int fps = GetTargetFPSValue(TargetFPSIndex);
            QualitySettings.vSyncCount = 0; // Disable VSync so targetFrameRate takes effect on mobile & PC
            Application.targetFrameRate = fps;
            Debug.Log($"[SettingsManager] Target FPS set to {fps} (Index: {TargetFPSIndex}).");
        }

        /// <summary>
        /// Returns integer frame rate value for a given FPS index.
        /// </summary>
        public static int GetTargetFPSValue(int index)
        {
            return index switch
            {
                0 => 30,
                1 => 60,
                2 => 90,
                3 => -1, // Uncapped/Device refresh rate
                _ => 60
            };
        }

        /// <summary>
        /// Returns display string for FPS index.
        /// </summary>
        public static string GetTargetFPSName(int index)
        {
            return index switch
            {
                0 => "30 FPS (Battery Saver)",
                1 => "60 FPS (Balanced)",
                2 => "90 FPS (High Performance)",
                3 => "Max / Uncapped",
                _ => "60 FPS"
            };
        }

        /// <summary>
        /// Applies the current graphics quality setting to Unity's QualitySettings.
        /// </summary>
        public static void ApplyGraphicsQuality()
        {
            int quality = GraphicsQuality;
            QualitySettings.SetQualityLevel(quality, true);
            Debug.Log($"[SettingsManager] Graphics quality set to level {quality} ({QualityLevelName(quality)}).");
        }

        /// <summary>
        /// Returns the human-readable name for a graphics quality level.
        /// </summary>
        /// <param name="level">Quality level (0-2).</param>
        /// <returns>Display name string.</returns>
        public static string QualityLevelName(int level)
        {
            return level switch
            {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                _ => "Unknown"
            };
        }

        #endregion
    }
}
