using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Controls the Settings UI panel: graphics quality dropdown, volume sliders,
    /// sensitivity slider, apply/reset/close buttons.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        #region UI References

        [Header("Graphics & Performance")]
        [SerializeField, Tooltip("Dropdown for selecting graphics quality (Low/Medium/High).")]
        private TMP_Dropdown _graphicsDropdown;

        [SerializeField, Tooltip("Dropdown for selecting target FPS preference (30/60/90/Max).")]
        private TMP_Dropdown _fpsDropdown;

        [Header("Volume Sliders")]
        [SerializeField, Tooltip("Slider for master volume (0-1).")]
        private Slider _masterVolumeSlider;

        [SerializeField, Tooltip("Slider for music volume (0-1).")]
        private Slider _musicVolumeSlider;

        [SerializeField, Tooltip("Slider for SFX volume (0-1).")]
        private Slider _sfxVolumeSlider;

        [Header("Sensitivity")]
        [SerializeField, Tooltip("Slider for controls sensitivity (0.1-2.0).")]
        private Slider _sensitivitySlider;

        [SerializeField, Tooltip("Label displaying the current sensitivity value.")]
        private TMP_Text _sensitivityValueLabel;

        [Header("Volume Labels")]
        [SerializeField, Tooltip("Label displaying master volume percentage.")]
        private TMP_Text _masterVolumeLabel;

        [SerializeField, Tooltip("Label displaying music volume percentage.")]
        private TMP_Text _musicVolumeLabel;

        [SerializeField, Tooltip("Label displaying SFX volume percentage.")]
        private TMP_Text _sfxVolumeLabel;

        [Header("Buttons")]
        [SerializeField, Tooltip("Button to apply current settings.")]
        private Button _applyButton;

        [SerializeField, Tooltip("Button to reset all settings to defaults.")]
        private Button _resetButton;

        [SerializeField, Tooltip("Button to close the settings panel.")]
        private Button _closeButton;

        #endregion

        #region References

        [Header("Panel")]
        [SerializeField, Tooltip("Reference to the MainMenuController to notify on close.")]
        private MainMenuController _menuController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSliderRanges();
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
            BindListeners();
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies the current UI values to the game settings and saves them.
        /// </summary>
        public void ApplySettings()
        {
            // Write values to SettingsManager
            if (_graphicsDropdown != null)
            {
                SettingsManager.GraphicsQuality = _graphicsDropdown.value;
            }

            if (_fpsDropdown != null)
            {
                SettingsManager.TargetFPSIndex = _fpsDropdown.value;
            }

            if (_masterVolumeSlider != null)
            {
                SettingsManager.MasterVolume = _masterVolumeSlider.value;
            }

            if (_musicVolumeSlider != null)
            {
                SettingsManager.MusicVolume = _musicVolumeSlider.value;
            }

            if (_sfxVolumeSlider != null)
            {
                SettingsManager.SFXVolume = _sfxVolumeSlider.value;
            }

            if (_sensitivitySlider != null)
            {
                SettingsManager.Sensitivity = _sensitivitySlider.value;
            }

            // Save and apply
            SettingsManager.Save();
            SettingsManager.LoadAndApply();

            Debug.Log("[SettingsPanel] Settings applied and saved.");
        }

        /// <summary>
        /// Resets all settings to their default values and refreshes the UI.
        /// </summary>
        public void ResetToDefaults()
        {
            SettingsManager.ResetToDefaults();
            LoadCurrentSettings();
            Debug.Log("[SettingsPanel] Settings reset to defaults.");
        }

        /// <summary>
        /// Closes this settings panel by notifying the MainMenuController.
        /// </summary>
        public void Close()
        {
            if (_menuController != null)
            {
                _menuController.CloseSettingsPanel();
            }
            else
            {
                // Fallback: just hide ourselves
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures slider min/max ranges and dropdown options.
        /// </summary>
        private void SetupSliderRanges()
        {
            // Volume sliders: 0 to 1
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.minValue = 0f;
                _masterVolumeSlider.maxValue = 1f;
                _masterVolumeSlider.wholeNumbers = false;
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.minValue = 0f;
                _musicVolumeSlider.maxValue = 1f;
                _musicVolumeSlider.wholeNumbers = false;
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.minValue = 0f;
                _sfxVolumeSlider.maxValue = 1f;
                _sfxVolumeSlider.wholeNumbers = false;
            }

            // Sensitivity slider: 0.1 to 2.0
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.minValue = SettingsManager.MIN_SENSITIVITY;
                _sensitivitySlider.maxValue = SettingsManager.MAX_SENSITIVITY;
                _sensitivitySlider.wholeNumbers = false;
            }

            // Graphics dropdown
            if (_graphicsDropdown != null)
            {
                _graphicsDropdown.ClearOptions();
                _graphicsDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Low (Battery Saver)", "Medium (Balanced)", "High (Ultra Quality)"
                });
            }

            // Target FPS dropdown
            if (_fpsDropdown != null)
            {
                _fpsDropdown.ClearOptions();
                _fpsDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "30 FPS (Saver)", "60 FPS (Smooth)", "90 FPS (High)", "Max / Uncapped"
                });
            }
        }

        /// <summary>
        /// Loads current settings values into the UI controls.
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (_graphicsDropdown != null)
            {
                _graphicsDropdown.SetValueWithoutNotify(SettingsManager.GraphicsQuality);
            }

            if (_fpsDropdown != null)
            {
                _fpsDropdown.SetValueWithoutNotify(SettingsManager.TargetFPSIndex);
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetValueWithoutNotify(SettingsManager.MasterVolume);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.SetValueWithoutNotify(SettingsManager.MusicVolume);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.SetValueWithoutNotify(SettingsManager.SFXVolume);
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.SetValueWithoutNotify(SettingsManager.Sensitivity);
            }

            // Update all labels
            UpdateMasterVolumeLabel(SettingsManager.MasterVolume);
            UpdateMusicVolumeLabel(SettingsManager.MusicVolume);
            UpdateSFXVolumeLabel(SettingsManager.SFXVolume);
            UpdateSensitivityLabel(SettingsManager.Sensitivity);
        }

        /// <summary>
        /// Binds slider and button event listeners.
        /// </summary>
        private void BindListeners()
        {
            _masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            _musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            _sensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);

            _applyButton?.onClick.AddListener(ApplySettings);
            _resetButton?.onClick.AddListener(ResetToDefaults);
            _closeButton?.onClick.AddListener(Close);
        }

        /// <summary>
        /// Removes slider and button event listeners.
        /// </summary>
        private void UnbindListeners()
        {
            _masterVolumeSlider?.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            _musicVolumeSlider?.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            _sensitivitySlider?.onValueChanged.RemoveListener(OnSensitivityChanged);

            _applyButton?.onClick.RemoveListener(ApplySettings);
            _resetButton?.onClick.RemoveListener(ResetToDefaults);
            _closeButton?.onClick.RemoveListener(Close);
        }

        #endregion

        #region Slider Callbacks (Real-time preview)

        /// <summary>
        /// Called when the master volume slider value changes. Provides real-time audio preview.
        /// </summary>
        private void OnMasterVolumeChanged(float value)
        {
            UpdateMasterVolumeLabel(value);

            // Real-time preview: apply audio immediately
            if (AudioManager.HasInstance)
            {
                float musicVol = _musicVolumeSlider != null ? _musicVolumeSlider.value : SettingsManager.MusicVolume;
                float sfxVol = _sfxVolumeSlider != null ? _sfxVolumeSlider.value : SettingsManager.SFXVolume;

                AudioManager.Instance.SetMusicVolume(musicVol * value);
                AudioManager.Instance.SetSFXVolume(sfxVol * value);
            }
        }

        /// <summary>
        /// Called when the music volume slider value changes. Provides real-time audio preview.
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            UpdateMusicVolumeLabel(value);

            // Real-time preview
            if (AudioManager.HasInstance)
            {
                float master = _masterVolumeSlider != null ? _masterVolumeSlider.value : SettingsManager.MasterVolume;
                AudioManager.Instance.SetMusicVolume(value * master);
            }
        }

        /// <summary>
        /// Called when the SFX volume slider value changes. Provides real-time audio preview.
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            UpdateSFXVolumeLabel(value);

            // Real-time preview
            if (AudioManager.HasInstance)
            {
                float master = _masterVolumeSlider != null ? _masterVolumeSlider.value : SettingsManager.MasterVolume;
                AudioManager.Instance.SetSFXVolume(value * master);
            }
        }

        /// <summary>
        /// Called when the sensitivity slider value changes.
        /// </summary>
        private void OnSensitivityChanged(float value)
        {
            UpdateSensitivityLabel(value);
        }

        #endregion

        #region Label Updates

        private void UpdateMasterVolumeLabel(float value)
        {
            if (_masterVolumeLabel != null)
            {
                _masterVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void UpdateMusicVolumeLabel(float value)
        {
            if (_musicVolumeLabel != null)
            {
                _musicVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void UpdateSFXVolumeLabel(float value)
        {
            if (_sfxVolumeLabel != null)
            {
                _sfxVolumeLabel.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (_sensitivityValueLabel != null)
            {
                _sensitivityValueLabel.text = $"{value:F1}";
            }
        }

        #endregion
    }
}
