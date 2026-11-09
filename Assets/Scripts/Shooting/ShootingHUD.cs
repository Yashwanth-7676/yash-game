using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Landscape Mobile Shooting HUD & Control Interface.
    /// Upgraded for Phase 3:
    /// - 4-Weapon Switching Touch Bar (Assault Rifle, Pistol, Shotgun, Sniper Rifle)
    /// - Persistent Coin Counter & Rewards HUD
    /// - Dynamic Mission Objective Bar (Elimination, Survival, Assassination)
    /// - Directional Damage Indicator Container
    /// - Virtual Movement Joystick, Touch Action Buttons (Fire, Aim, Jump, Reload)
    /// - Dynamic Bloom Crosshair & Hitmarker
    /// - Upgraded Pause, Victory & Defeat modals with mission statistics
    /// </summary>
    public class ShootingHUD : MonoBehaviour
    {
        public static ShootingHUD Instance { get; private set; }

        #region Palette Constants

        private static readonly Color ColorCyanNeon = new Color(0f, 0.85f, 1f, 1f);
        private static readonly Color ColorRedNeon = new Color(1f, 0.18f, 0.35f, 1f);
        private static readonly Color ColorGoldNeon = new Color(1f, 0.84f, 0f, 1f);
        private static readonly Color ColorCardDark = new Color(0.04f, 0.06f, 0.12f, 0.88f);
        private static readonly Color ColorTabInactive = new Color(0.08f, 0.11f, 0.18f, 0.85f);
        private static readonly Color ColorTabActive = new Color(0f, 0.45f, 0.75f, 0.95f);
        private static readonly Color ColorTextWhite = new Color(0.96f, 0.97f, 1f, 1f);

        #endregion

        #region Serialized UI References

        [Header("Player Status")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Image _damageVignette;

        [Header("Weapon & Ammo")]
        [SerializeField] private TMP_Text _weaponNameText;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private TMP_Text _reloadStatusText;
        [SerializeField] private Image[] _weaponTabBgs;
        [SerializeField] private TMP_Text[] _weaponTabTexts;

        [Header("Mission & Objective")]
        [SerializeField] private TMP_Text _missionTitleText;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private Slider _objectiveSlider;

        [Header("Crosshair & Hitmarker")]
        [SerializeField] private RectTransform _crosshairRoot;
        [SerializeField] private RectTransform[] _crosshairTicks;
        [SerializeField] private TMP_Text _hitMarkerText;

        [Header("Virtual Joystick")]
        [SerializeField] private RectTransform _joystickBase;
        [SerializeField] private RectTransform _joystickKnob;

        [Header("Action Buttons")]
        [SerializeField] private Button _fireButton;
        [SerializeField] private Button _aimButton;
        [SerializeField] private Button _jumpButton;
        [SerializeField] private Button _reloadButton;
        [SerializeField] private Button _pauseButton;

        [Header("Modals")]
        [SerializeField] private GameObject _pauseModal;
        [SerializeField] private GameObject _victoryModal;
        [SerializeField] private GameObject _defeatModal;

        [Header("Modal Dynamic Text")]
        [SerializeField] private TMP_Text _victoryStatsText;
        [SerializeField] private TMP_Text _defeatStatsText;

        #endregion

        #region Private Fields

        private MobileInputController _input;
        private WeaponManager _weaponManager;
        private WeaponController _legacyWeapon;
        private PlayerHealth _playerHealth;
        private PlayerMovement _playerMovement;
        private RewardManager _rewardManager;
        private MissionManager _missionManager;

        private bool _isDraggingJoystick;
        private Vector2 _joystickPointerStart;
        private float _joystickRadius = 75f;
        private float _crosshairSpread = 18f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureHUDHierarchy();
        }

        private void Start()
        {
            _input = MobileInputController.Instance;
            HookPlayerComponents();
            HookRewardManager();
            HookMissionManager();
        }

        private void Update()
        {
            UpdateCrosshairDynamics();
            HandleTouchJoystickInput();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_rewardManager != null)
            {
                _rewardManager.OnCoinsChanged -= UpdateCoinsUI;
            }

            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponSwitched -= HandleWeaponSwitched;
                _weaponManager.OnAmmoUpdated -= UpdateAmmoUI;
            }

            if (_missionManager != null)
            {
                _missionManager.OnMissionStarted -= HandleMissionStarted;
                _missionManager.OnMissionProgressUpdated -= HandleMissionProgress;
                _missionManager.OnMissionCompleted -= HandleMissionCompleted;
            }
        }

        #endregion

        #region Component Hooking

        public void HookPlayerComponents()
        {
            if (_playerHealth == null)
            {
                _playerHealth = FindObjectOfType<PlayerHealth>();
                if (_playerHealth != null)
                {
                    _playerHealth.OnHealthChanged += UpdateHealthUI;
                    _playerHealth.OnPlayerDamaged += TriggerDamageFlash;
                    UpdateHealthUI(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
                }
            }

            if (_weaponManager == null)
            {
                _weaponManager = FindObjectOfType<WeaponManager>();
                if (_weaponManager != null)
                {
                    _weaponManager.OnWeaponSwitched += HandleWeaponSwitched;
                    _weaponManager.OnAmmoUpdated += UpdateAmmoUI;
                    _weaponManager.OnReloadStarted += () => SetReloadStatus(true, "RELOADING...");
                    _weaponManager.OnReloadFinished += () => SetReloadStatus(false, "");

                    if (_weaponManager.ActiveWeapon != null)
                    {
                        HandleWeaponSwitched(_weaponManager.ActiveSlotIndex, _weaponManager.ActiveWeapon.data);
                        UpdateAmmoUI(_weaponManager.ActiveWeapon.currentMag, _weaponManager.ActiveWeapon.reserveAmmo);
                    }
                }
            }

            // Fallback for Phase 2 legacy weapon controller
            if (_weaponManager == null && _legacyWeapon == null)
            {
                _legacyWeapon = FindObjectOfType<WeaponController>();
                if (_legacyWeapon != null)
                {
                    _legacyWeapon.OnAmmoChanged += UpdateAmmoUI;
                    _legacyWeapon.OnEnemyHit += TriggerHitMarker;
                    _legacyWeapon.OnReloadStarted += () => SetReloadStatus(true, "RELOADING...");
                    _legacyWeapon.OnReloadFinished += () => SetReloadStatus(false, "");

                    if (_weaponNameText != null) _weaponNameText.text = _legacyWeapon.WeaponName;
                    UpdateAmmoUI(_legacyWeapon.CurrentAmmo, _legacyWeapon.ReserveAmmo);
                }
            }

            if (_playerMovement == null)
            {
                _playerMovement = FindObjectOfType<PlayerMovement>();
            }
        }

        private void HookRewardManager()
        {
            _rewardManager = RewardManager.Instance;
            if (_rewardManager == null) _rewardManager = FindObjectOfType<RewardManager>();

            if (_rewardManager != null)
            {
                _rewardManager.OnCoinsChanged += UpdateCoinsUI;
                UpdateCoinsUI(_rewardManager.CurrentCoins, 0);
            }
        }

        private void HookMissionManager()
        {
            _missionManager = MissionManager.Instance;
            if (_missionManager == null) _missionManager = FindObjectOfType<MissionManager>();

            if (_missionManager != null)
            {
                _missionManager.OnMissionStarted += HandleMissionStarted;
                _missionManager.OnMissionProgressUpdated += HandleMissionProgress;
                _missionManager.OnMissionCompleted += HandleMissionCompleted;

                if (_missionManager.CurrentMission != null)
                {
                    HandleMissionStarted(_missionManager.CurrentMission);
                }
            }
        }

        #endregion

        #region Weapon Switching & UI

        public void SelectWeaponSlot(int slotIndex)
        {
            if (_weaponManager != null)
            {
                _weaponManager.SwitchToSlot(slotIndex);
            }
        }

        private void HandleWeaponSwitched(int slotIndex, WeaponData data)
        {
            if (data != null && _weaponNameText != null)
            {
                _weaponNameText.text = data.weaponName.ToUpper();
            }

            HighlightActiveWeaponTab(slotIndex);
        }

        private void HighlightActiveWeaponTab(int activeIndex)
        {
            if (_weaponTabBgs == null) return;

            for (int i = 0; i < _weaponTabBgs.Length; i++)
            {
                if (_weaponTabBgs[i] != null)
                {
                    bool isActive = (i == activeIndex);
                    _weaponTabBgs[i].color = isActive ? ColorTabActive : ColorTabInactive;

                    Outline outline = _weaponTabBgs[i].GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = isActive ? ColorCyanNeon : new Color(0.2f, 0.3f, 0.4f, 0.5f);
                    }
                }
            }
        }

        #endregion

        #region HUD Updates

        public void UpdateHealthUI(float current, float max)
        {
            if (_healthSlider != null)
            {
                _healthSlider.minValue = 0f;
                _healthSlider.maxValue = max;
                _healthSlider.value = current;
            }

            if (_healthText != null)
            {
                _healthText.text = $"HP {(int)current} / {(int)max}";
            }
        }

        public void UpdateCoinsUI(int totalCoins, int delta)
        {
            if (_coinsText != null)
            {
                _coinsText.text = $"🪙 <color=#FFD700>{totalCoins:N0}</color>";
            }
        }

        public void UpdateAmmoUI(int current, int reserve)
        {
            if (_ammoText != null)
            {
                _ammoText.text = $"<size=46><color=#00D9FF>{current}</color></size> <color=#8FA0C0>/ {reserve}</color>";
            }
        }

        public void UpdateObjectiveUI(int current, int total)
        {
            if (_objectiveText != null)
            {
                _objectiveText.text = $"🎯 TARGETS: <color=#00D9FF>{current}</color> / {total}";
            }
        }

        private void SetReloadStatus(bool isBusy, string message)
        {
            if (_reloadStatusText != null)
            {
                _reloadStatusText.gameObject.SetActive(isBusy);
                _reloadStatusText.text = message;
            }
        }

        public void TriggerDamageFlash()
        {
            if (_damageVignette == null) return;
            StopCoroutine(nameof(DamageFlashRoutine));
            StartCoroutine(nameof(DamageFlashRoutine));
        }

        private IEnumerator DamageFlashRoutine()
        {
            _damageVignette.color = new Color(1f, 0f, 0.15f, 0.4f);
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.25f;
                _damageVignette.color = new Color(1f, 0f, 0.15f, Mathf.Lerp(0.4f, 0f, t));
                yield return null;
            }
            _damageVignette.color = new Color(1f, 0f, 0.15f, 0f);
        }

        public void TriggerHitMarker()
        {
            if (_hitMarkerText == null) return;
            StopCoroutine(nameof(HitMarkerRoutine));
            StartCoroutine(nameof(HitMarkerRoutine));
        }

        private IEnumerator HitMarkerRoutine()
        {
            _hitMarkerText.gameObject.SetActive(true);
            _hitMarkerText.color = Color.white;
            _hitMarkerText.rectTransform.localScale = Vector3.one * 1.4f;

            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.12f;
                _hitMarkerText.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.05f);
            _hitMarkerText.gameObject.SetActive(false);
        }

        private void UpdateCrosshairDynamics()
        {
            if (_crosshairTicks == null || _crosshairTicks.Length < 4) return;

            float targetSpread = 18f;
            if (_playerMovement != null)
            {
                if (_playerMovement.IsAiming) targetSpread = 8f;
                else if (_playerMovement.CurrentSpeedRatio > 0.5f) targetSpread = 36f;
                else if (_playerMovement.CurrentSpeedRatio > 0.05f) targetSpread = 24f;
            }

            _crosshairSpread = Mathf.Lerp(_crosshairSpread, targetSpread, Time.deltaTime * 14f);

            // Ticks: 0:Top, 1:Bottom, 2:Left, 3:Right
            if (_crosshairTicks[0] != null) _crosshairTicks[0].anchoredPosition = new Vector2(0f, _crosshairSpread);
            if (_crosshairTicks[1] != null) _crosshairTicks[1].anchoredPosition = new Vector2(0f, -_crosshairSpread);
            if (_crosshairTicks[2] != null) _crosshairTicks[2].anchoredPosition = new Vector2(-_crosshairSpread, 0f);
            if (_crosshairTicks[3] != null) _crosshairTicks[3].anchoredPosition = new Vector2(_crosshairSpread, 0f);
        }

        #endregion

        #region Mission Events Handling

        private void HandleMissionStarted(MissionData mission)
        {
            if (mission == null) return;

            string mapName = (MapManager.Instance != null && MapManager.Instance.CurrentMapData != null)
                ? MapManager.Instance.CurrentMapData.displayName
                : "BATTLEFIELD";

            string diffName = (DifficultyManager.Instance != null)
                ? DifficultyManager.GetDifficultyName(DifficultyManager.Instance.CurrentDifficulty)
                : "NORMAL";

            if (_missionTitleText != null)
            {
                _missionTitleText.text = $"{mapName} • <size=16><color=#FFD700>[{diffName}]</color></size>";
            }

            if (_objectiveText != null)
            {
                _objectiveText.text = $"{mission.title}: {mission.subtitle}";
            }

            if (_objectiveSlider != null)
            {
                _objectiveSlider.value = 0f;
            }
        }

        private void HandleMissionProgress(string progressText, float ratio)
        {
            if (_objectiveText != null)
            {
                _objectiveText.text = progressText;
            }

            if (_objectiveSlider != null)
            {
                _objectiveSlider.value = Mathf.Clamp01(ratio);
            }
        }

        private void HandleMissionCompleted(MissionData mission, bool victory, int coinsEarned, float duration)
        {
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            string timeFormatted = $"{minutes:00}:{seconds:00}";

            if (victory)
            {
                if (_victoryStatsText != null)
                {
                    _victoryStatsText.text = $"TIME: <color=#00D9FF>{timeFormatted}</color>   |   REWARD: <color=#FFD700>+{coinsEarned} COINS</color>\n<size=22><color=#8FA0C0>OUTPOST SECURED AND REPORTED TO HQ</color></size>";
                }
                ShowVictoryModal(true);
            }
            else
            {
                if (_defeatStatsText != null)
                {
                    _defeatStatsText.text = $"TIME SURVIVED: <color=#FF2E59>{timeFormatted}</color>\n<size=22><color=#8FA0C0>CRITICAL VITAL SIGNS FAILURE</color></size>";
                }
                ShowDefeatModal(true);
            }
        }

        #endregion

        #region Touch Joystick Implementation

        private void HandleTouchJoystickInput()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    // Left half of screen controls movement joystick
                    if (touch.position.x < Screen.width * 0.45f && touch.position.y < Screen.height * 0.55f)
                    {
                        ProcessTouchMovement(touch);
                        return;
                    }
                }
            }

            if (_isDraggingJoystick && Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                ResetJoystick();
            }
        }

        private void ProcessTouchMovement(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                _isDraggingJoystick = true;
                _joystickPointerStart = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 diff = touch.position - _joystickPointerStart;
                Vector2 clamped = Vector2.ClampMagnitude(diff, _joystickRadius);

                if (_joystickKnob != null)
                {
                    _joystickKnob.anchoredPosition = clamped;
                }

                Vector2 normalizedInput = clamped / _joystickRadius;
                if (_input != null)
                {
                    _input.SetMovement(normalizedInput);
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                ResetJoystick();
            }
        }

        private void ResetJoystick()
        {
            _isDraggingJoystick = false;
            if (_joystickKnob != null)
            {
                _joystickKnob.anchoredPosition = Vector2.zero;
            }
            if (_input != null)
            {
                _input.SetMovement(Vector2.zero);
            }
        }

        #endregion

        #region Modals & Flow

        public void ShowPauseMenu(bool show)
        {
            if (_pauseModal != null) _pauseModal.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        public void ShowVictoryModal(bool show)
        {
            if (_victoryModal != null) _victoryModal.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        public void ShowDefeatModal(bool show)
        {
            if (_defeatModal != null) _defeatModal.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        public void OnResumeClicked() => ShowPauseMenu(false);

        public void OnNextMissionClicked()
        {
            Time.timeScale = 1f;
            ShowVictoryModal(false);
            if (_missionManager != null)
            {
                _missionManager.NextMission();
            }
            else
            {
                OnRestartClicked();
            }
        }

        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            ShowVictoryModal(false);
            ShowDefeatModal(false);

            if (_missionManager != null)
            {
                _missionManager.RestartCurrentMission();
            }
            else
            {
                if (SceneLoader.HasInstance) SceneLoader.Instance.LoadScene(SceneLoader.SHOOTING);
                else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.SHOOTING);
            }
        }

        public void OnReturnToMenuClicked()
        {
            Time.timeScale = 1f;
            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadScene(SceneLoader.MAIN_MENU);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.MAIN_MENU);
        }

        #endregion

        #region Procedural UI Hierarchy Assembly

        private void EnsureHUDHierarchy()
        {
            if (transform.Find("LandscapeCanvas") != null) return;

            Debug.Log("[ShootingHUD] Procedurally constructing Landscape Mobile Shooting HUD (Phase 3)...");

            GameObject canvasObj = new GameObject("LandscapeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Damage Vignette (fullscreen red overlay)
            GameObject vignObj = new GameObject("DamageVignette", typeof(RectTransform), typeof(Image));
            vignObj.transform.SetParent(canvasObj.transform, false);
            RectTransform vRt = vignObj.GetComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one; vRt.sizeDelta = Vector2.zero;
            _damageVignette = vignObj.GetComponent<Image>();
            _damageVignette.color = new Color(1f, 0f, 0f, 0f);
            _damageVignette.raycastTarget = false;

            // Directional Damage Indicator Container
            GameObject dmgIndObj = new GameObject("DamageIndicatorRoot", typeof(RectTransform), typeof(DamageIndicator));
            dmgIndObj.transform.SetParent(canvasObj.transform, false);
            RectTransform diRt = dmgIndObj.GetComponent<RectTransform>();
            diRt.anchorMin = new Vector2(0.5f, 0.5f); diRt.anchorMax = new Vector2(0.5f, 0.5f);
            diRt.sizeDelta = Vector2.zero;

            // 1. Top Bar: Health Bar, Coins, Mission Objective & Pause
            BuildTopStatusBars(canvasObj.transform);

            // 2. Crosshair & HitMarker (Center)
            BuildCrosshairAndHitMarker(canvasObj.transform);

            // 3. Virtual Joystick (Bottom-Left)
            BuildMovementJoystick(canvasObj.transform);

            // 4. Weapon Switching Tabs (Bottom-Center)
            BuildWeaponSwitchingTabs(canvasObj.transform);

            // 5. Action Buttons (Bottom-Right: Fire, Aim, Jump, Reload)
            BuildActionButtons(canvasObj.transform);

            // 6. Ammo Box & Reload Notification
            BuildAmmoInfoBox(canvasObj.transform);

            // 7. Modals (Pause, Upgraded Victory & Defeat)
            BuildModals(canvasObj.transform);
        }

        private void BuildTopStatusBars(Transform parent)
        {
            // Health Bar (Top-left)
            GameObject hpObj = new GameObject("HealthBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            hpObj.transform.SetParent(parent, false);
            RectTransform hpRt = hpObj.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0f, 1f); hpRt.anchorMax = new Vector2(0f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.anchoredPosition = new Vector2(40f, -40f);
            hpRt.sizeDelta = new Vector2(320f, 55f);
            hpObj.GetComponent<Image>().color = ColorCardDark;
            hpObj.GetComponent<Outline>().effectColor = ColorCyanNeon;

            // Slider inside HealthBox
            _healthSlider = hpObj.AddComponent<Slider>();
            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(hpObj.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = new Vector2(-10f, -10f); faRt.anchoredPosition = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0f, 0.85f, 1f, 0.9f);
            _healthSlider.fillRect = fillRt;

            GameObject hpTextObj = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hpTextObj.transform.SetParent(hpObj.transform, false);
            RectTransform hptRt = hpTextObj.GetComponent<RectTransform>();
            hptRt.anchoredPosition = Vector2.zero; hptRt.sizeDelta = new Vector2(300f, 45f);
            _healthText = hpTextObj.GetComponent<TextMeshProUGUI>();
            _healthText.text = "HP 100 / 100";
            _healthText.fontSize = 24;
            _healthText.alignment = TextAlignmentOptions.Center;
            _healthText.color = ColorTextWhite;

            // Coins Display Badge (Next to Health)
            GameObject coinBadge = new GameObject("CoinBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
            coinBadge.transform.SetParent(parent, false);
            RectTransform cbRt = coinBadge.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 1f); cbRt.anchorMax = new Vector2(0f, 1f);
            cbRt.pivot = new Vector2(0f, 1f);
            cbRt.anchoredPosition = new Vector2(380f, -40f);
            cbRt.sizeDelta = new Vector2(170f, 55f);
            coinBadge.GetComponent<Image>().color = ColorCardDark;
            coinBadge.GetComponent<Outline>().effectColor = ColorGoldNeon;

            GameObject coinTextObj = new GameObject("CoinText", typeof(RectTransform), typeof(TextMeshProUGUI));
            coinTextObj.transform.SetParent(coinBadge.transform, false);
            RectTransform ctRt = coinTextObj.GetComponent<RectTransform>();
            ctRt.anchoredPosition = Vector2.zero; ctRt.sizeDelta = new Vector2(160f, 45f);
            _coinsText = coinTextObj.GetComponent<TextMeshProUGUI>();
            _coinsText.text = "🪙 <color=#FFD700>0</color>";
            _coinsText.fontSize = 24;
            _coinsText.alignment = TextAlignmentOptions.Center;
            _coinsText.color = ColorTextWhite;

            // Mission Objective Box (Top-center)
            GameObject objBadge = new GameObject("MissionBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
            objBadge.transform.SetParent(parent, false);
            RectTransform obRt = objBadge.GetComponent<RectTransform>();
            obRt.anchorMin = new Vector2(0.5f, 1f); obRt.anchorMax = new Vector2(0.5f, 1f);
            obRt.pivot = new Vector2(0.5f, 1f);
            obRt.anchoredPosition = new Vector2(0f, -30f);
            obRt.sizeDelta = new Vector2(560f, 75f);
            objBadge.GetComponent<Image>().color = ColorCardDark;
            objBadge.GetComponent<Outline>().effectColor = ColorCyanNeon;

            GameObject mTitleObj = new GameObject("MissionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            mTitleObj.transform.SetParent(objBadge.transform, false);
            RectTransform mtRt = mTitleObj.GetComponent<RectTransform>();
            mtRt.anchoredPosition = new Vector2(0f, 16f); mtRt.sizeDelta = new Vector2(540f, 32f);
            _missionTitleText = mTitleObj.GetComponent<TextMeshProUGUI>();
            _missionTitleText.text = "OPERATION: SECTOR PURGE";
            _missionTitleText.fontSize = 20;
            _missionTitleText.alignment = TextAlignmentOptions.Center;
            _missionTitleText.color = ColorCyanNeon;

            GameObject mSubObj = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
            mSubObj.transform.SetParent(objBadge.transform, false);
            RectTransform msRt = mSubObj.GetComponent<RectTransform>();
            msRt.anchoredPosition = new Vector2(0f, -14f); msRt.sizeDelta = new Vector2(540f, 28f);
            _objectiveText = mSubObj.GetComponent<TextMeshProUGUI>();
            _objectiveText.text = "🎯 TARGETS: 0 / 8";
            _objectiveText.fontSize = 18;
            _objectiveText.alignment = TextAlignmentOptions.Center;
            _objectiveText.color = ColorTextWhite;

            // Pause Button (Top-right)
            GameObject pauseObj = CreateActionButton("PauseButton", parent, new Vector2(-40f, -40f), new Vector2(70f, 70f),
                new Vector2(1f, 1f), "⏸", 36, ColorCardDark, ColorCyanNeon);
            _pauseButton = pauseObj.GetComponent<Button>();
            _pauseButton.onClick.AddListener(() => ShowPauseMenu(true));
        }

        private void BuildCrosshairAndHitMarker(Transform parent)
        {
            GameObject crossObj = new GameObject("CrosshairRoot", typeof(RectTransform));
            crossObj.transform.SetParent(parent, false);
            _crosshairRoot = crossObj.GetComponent<RectTransform>();
            _crosshairRoot.anchoredPosition = Vector2.zero;

            _crosshairTicks = new RectTransform[4];
            _crosshairTicks[0] = CreateCrosshairTick("Tick_Top", _crosshairRoot, new Vector2(3f, 14f), new Vector2(0f, 20f));
            _crosshairTicks[1] = CreateCrosshairTick("Tick_Bottom", _crosshairRoot, new Vector2(3f, 14f), new Vector2(0f, -20f));
            _crosshairTicks[2] = CreateCrosshairTick("Tick_Left", _crosshairRoot, new Vector2(14f, 3f), new Vector2(-20f, 0f));
            _crosshairTicks[3] = CreateCrosshairTick("Tick_Right", _crosshairRoot, new Vector2(14f, 3f), new Vector2(20f, 0f));

            // Hitmarker Text ("✖")
            GameObject hmObj = new GameObject("HitMarker", typeof(RectTransform), typeof(TextMeshProUGUI));
            hmObj.transform.SetParent(crossObj.transform, false);
            RectTransform hmRt = hmObj.GetComponent<RectTransform>();
            hmRt.anchoredPosition = Vector2.zero; hmRt.sizeDelta = new Vector2(60f, 60f);
            _hitMarkerText = hmObj.GetComponent<TextMeshProUGUI>();
            _hitMarkerText.text = "✖";
            _hitMarkerText.fontSize = 36;
            _hitMarkerText.color = Color.white;
            _hitMarkerText.alignment = TextAlignmentOptions.Center;
            hmObj.SetActive(false);
        }

        private void BuildMovementJoystick(Transform parent)
        {
            GameObject joyBaseObj = new GameObject("JoystickBase", typeof(RectTransform), typeof(Image), typeof(Outline));
            joyBaseObj.transform.SetParent(parent, false);
            _joystickBase = joyBaseObj.GetComponent<RectTransform>();
            _joystickBase.anchorMin = new Vector2(0f, 0f); _joystickBase.anchorMax = new Vector2(0f, 0f);
            _joystickBase.pivot = new Vector2(0.5f, 0.5f);
            _joystickBase.anchoredPosition = new Vector2(180f, 180f);
            _joystickBase.sizeDelta = new Vector2(160f, 160f);
            joyBaseObj.GetComponent<Image>().color = new Color(0.06f, 0.1f, 0.2f, 0.45f);
            joyBaseObj.GetComponent<Outline>().effectColor = new Color(0f, 0.85f, 1f, 0.4f);

            GameObject joyKnobObj = new GameObject("JoystickKnob", typeof(RectTransform), typeof(Image));
            joyKnobObj.transform.SetParent(joyBaseObj.transform, false);
            _joystickKnob = joyKnobObj.GetComponent<RectTransform>();
            _joystickKnob.anchoredPosition = Vector2.zero;
            _joystickKnob.sizeDelta = new Vector2(70f, 70f);
            joyKnobObj.GetComponent<Image>().color = new Color(0f, 0.85f, 1f, 0.85f);
        }

        private void BuildWeaponSwitchingTabs(Transform parent)
        {
            GameObject barObj = new GameObject("WeaponSwitchBar", typeof(RectTransform));
            barObj.transform.SetParent(parent, false);
            RectTransform bRt = barObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f); bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(-60f, 25f);
            bRt.sizeDelta = new Vector2(460f, 65f);

            string[] weaponLabels = new string[] { "[1] M4A1", "[2] 9MM", "[3] SG-12", "[4] V-50" };
            _weaponTabBgs = new Image[4];
            _weaponTabTexts = new TMP_Text[4];

            float tabWidth = 105f;
            float spacing = 10f;
            float startX = -((4 * tabWidth + 3 * spacing) * 0.5f) + (tabWidth * 0.5f);

            for (int i = 0; i < 4; i++)
            {
                int slotIndex = i;
                GameObject tabObj = new GameObject($"Tab_{i}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
                tabObj.transform.SetParent(barObj.transform, false);

                RectTransform tRt = tabObj.GetComponent<RectTransform>();
                tRt.anchoredPosition = new Vector2(startX + i * (tabWidth + spacing), 0f);
                tRt.sizeDelta = new Vector2(tabWidth, 55f);

                Image bg = tabObj.GetComponent<Image>();
                bg.color = (i == 0) ? ColorTabActive : ColorTabInactive;
                _weaponTabBgs[i] = bg;

                Outline outline = tabObj.GetComponent<Outline>();
                outline.effectColor = (i == 0) ? ColorCyanNeon : new Color(0.2f, 0.3f, 0.4f, 0.5f);
                outline.effectDistance = new Vector2(2f, -2f);

                Button btn = tabObj.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectWeaponSlot(slotIndex));

                GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(tabObj.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchoredPosition = Vector2.zero; txtRt.sizeDelta = new Vector2(tabWidth - 6f, 50f);

                TMP_Text txt = txtObj.GetComponent<TextMeshProUGUI>();
                txt.text = weaponLabels[i];
                txt.fontSize = 18;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = ColorTextWhite;
                _weaponTabTexts[i] = txt;
            }
        }

        private void BuildActionButtons(Transform parent)
        {
            // Fire Button (Large Red circle)
            GameObject fireObj = CreateActionButton("FireButton", parent, new Vector2(-120f, 130f), new Vector2(140f, 140f),
                new Vector2(1f, 0f), "🔥\nFIRE", 26, ColorCardDark, ColorRedNeon);
            _fireButton = fireObj.GetComponent<Button>();
            AddPointerListeners(fireObj, (held) => { if (_input != null) _input.SetFireButtonHeld(held); });

            // Aim Button (Cyan circle)
            GameObject aimObj = CreateActionButton("AimButton", parent, new Vector2(-280f, 90f), new Vector2(100f, 100f),
                new Vector2(1f, 0f), "🎯\nAIM", 22, ColorCardDark, ColorCyanNeon);
            _aimButton = aimObj.GetComponent<Button>();
            _aimButton.onClick.AddListener(() => { if (_input != null) _input.ToggleAim(); });

            // Jump Button
            GameObject jumpObj = CreateActionButton("JumpButton", parent, new Vector2(-100f, 300f), new Vector2(90f, 90f),
                new Vector2(1f, 0f), "⤊\nJUMP", 20, ColorCardDark, ColorCyanNeon);
            _jumpButton = jumpObj.GetComponent<Button>();
            _jumpButton.onClick.AddListener(() => { if (_input != null) _input.TriggerJump(); });

            // Reload Button
            GameObject reloadObj = CreateActionButton("ReloadButton", parent, new Vector2(-240f, 220f), new Vector2(90f, 90f),
                new Vector2(1f, 0f), "🔄\nRELOAD", 18, ColorCardDark, ColorCyanNeon);
            _reloadButton = reloadObj.GetComponent<Button>();
            _reloadButton.onClick.AddListener(() => { if (_input != null) _input.TriggerReload(); });
        }

        private void BuildAmmoInfoBox(Transform parent)
        {
            GameObject ammoBox = new GameObject("AmmoBox", typeof(RectTransform), typeof(Image));
            ammoBox.transform.SetParent(parent, false);
            RectTransform abRt = ammoBox.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(1f, 0f); abRt.anchorMax = new Vector2(1f, 0f);
            abRt.pivot = new Vector2(1f, 0f);
            abRt.anchoredPosition = new Vector2(-400f, 30f);
            abRt.sizeDelta = new Vector2(320f, 110f);
            ammoBox.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            GameObject wNameObj = new GameObject("WeaponName", typeof(RectTransform), typeof(TextMeshProUGUI));
            wNameObj.transform.SetParent(ammoBox.transform, false);
            RectTransform wnRt = wNameObj.GetComponent<RectTransform>();
            wnRt.anchoredPosition = new Vector2(0f, 30f); wnRt.sizeDelta = new Vector2(300f, 36f);
            _weaponNameText = wNameObj.GetComponent<TextMeshProUGUI>();
            _weaponNameText.text = "M4A1 TACTICAL RIFLE";
            _weaponNameText.fontSize = 20;
            _weaponNameText.alignment = TextAlignmentOptions.Right;
            _weaponNameText.color = new Color(0.7f, 0.85f, 1f);

            GameObject aCountObj = new GameObject("AmmoCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            aCountObj.transform.SetParent(ammoBox.transform, false);
            RectTransform acRt = aCountObj.GetComponent<RectTransform>();
            acRt.anchoredPosition = new Vector2(0f, -12f); acRt.sizeDelta = new Vector2(300f, 50f);
            _ammoText = aCountObj.GetComponent<TextMeshProUGUI>();
            _ammoText.text = "<size=46><color=#00D9FF>30</color></size> / 120";
            _ammoText.fontSize = 30;
            _ammoText.alignment = TextAlignmentOptions.Right;

            GameObject relStatusObj = new GameObject("ReloadStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            relStatusObj.transform.SetParent(ammoBox.transform, false);
            RectTransform rsRt = relStatusObj.GetComponent<RectTransform>();
            rsRt.anchoredPosition = new Vector2(0f, -44f); rsRt.sizeDelta = new Vector2(300f, 25f);
            _reloadStatusText = relStatusObj.GetComponent<TextMeshProUGUI>();
            _reloadStatusText.text = "";
            _reloadStatusText.fontSize = 18;
            _reloadStatusText.color = ColorGoldNeon;
            _reloadStatusText.alignment = TextAlignmentOptions.Right;
        }

        private void BuildModals(Transform parent)
        {
            // Pause Modal
            _pauseModal = CreateModalDialog("PauseModal", parent, "TACTICAL PAUSE", "RESUME", OnResumeClicked, "RESTART", OnRestartClicked, "EXIT TO HUB", OnReturnToMenuClicked);

            // Upgraded Victory Modal
            _victoryModal = CreateMissionResultModal("VictoryModal", parent,
                "MISSION ACCOMPLISHED",
                "OUTPOST SECURED",
                ColorCyanNeon,
                "NEXT MISSION", OnNextMissionClicked,
                "REPLAY", OnRestartClicked,
                "MAIN MENU", OnReturnToMenuClicked,
                out _victoryStatsText);

            // Upgraded Defeat Modal
            _defeatModal = CreateMissionResultModal("DefeatModal", parent,
                "OPERATIVE COMPROMISED",
                "CRITICAL SYSTEM FAILURE",
                ColorRedNeon,
                "RESPAWN / RETRY", OnRestartClicked,
                "", null,
                "MAIN MENU", OnReturnToMenuClicked,
                out _defeatStatsText);

            _pauseModal.SetActive(false);
            _victoryModal.SetActive(false);
            _defeatModal.SetActive(false);
        }

        private GameObject CreateMissionResultModal(string name, Transform parent, string title, string sub, Color accentColor, string btn1Text, UnityEngine.Events.UnityAction btn1Action, string btn2Text, UnityEngine.Events.UnityAction btn2Action, string exitText, UnityEngine.Events.UnityAction exitAction, out TMP_Text statsOut)
        {
            GameObject modal = new GameObject(name, typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(parent, false);
            RectTransform mRt = modal.GetComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one; mRt.sizeDelta = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.95f);

            GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(Outline));
            box.transform.SetParent(modal.transform, false);
            RectTransform bRt = box.GetComponent<RectTransform>();
            bRt.anchoredPosition = Vector2.zero; bRt.sizeDelta = new Vector2(760f, 540f);
            box.GetComponent<Image>().color = ColorCardDark;
            box.GetComponent<Outline>().effectColor = accentColor;
            box.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(box.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 180f); tRt.sizeDelta = new Vector2(700f, 60f);
            TMP_Text tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = title;
            tTmp.fontSize = 38;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = ColorTextWhite;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(box.transform, false);
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 130f); sRt.sizeDelta = new Vector2(700f, 40f);
            TMP_Text sTmp = subObj.GetComponent<TextMeshProUGUI>();
            sTmp.text = sub;
            sTmp.fontSize = 24;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = accentColor;

            // Stats Block
            GameObject statsObj = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(box.transform, false);
            RectTransform stRt = statsObj.GetComponent<RectTransform>();
            stRt.anchoredPosition = new Vector2(0f, 60f); stRt.sizeDelta = new Vector2(680f, 80f);
            statsOut = statsObj.GetComponent<TextMeshProUGUI>();
            statsOut.text = "TIME: 00:00   |   REWARD: +0 COINS";
            statsOut.fontSize = 26;
            statsOut.alignment = TextAlignmentOptions.Center;
            statsOut.color = ColorTextWhite;

            float yOffset = -30f;
            if (!string.IsNullOrEmpty(btn1Text) && btn1Action != null)
            {
                GameObject b1 = CreateActionButton("Btn1", box.transform, new Vector2(0f, yOffset), new Vector2(500f, 70f), new Vector2(0.5f, 0.5f), btn1Text, 26, new Color(0f, 0.4f, 0.65f), ColorCyanNeon);
                b1.GetComponent<Button>().onClick.AddListener(btn1Action);
                yOffset -= 85f;
            }

            if (!string.IsNullOrEmpty(btn2Text) && btn2Action != null)
            {
                GameObject b2 = CreateActionButton("Btn2", box.transform, new Vector2(0f, yOffset), new Vector2(500f, 70f), new Vector2(0.5f, 0.5f), btn2Text, 26, ColorCardDark, ColorCyanNeon);
                b2.GetComponent<Button>().onClick.AddListener(btn2Action);
                yOffset -= 85f;
            }

            if (!string.IsNullOrEmpty(exitText) && exitAction != null)
            {
                GameObject be = CreateActionButton("ExitBtn", box.transform, new Vector2(0f, yOffset), new Vector2(500f, 70f), new Vector2(0.5f, 0.5f), exitText, 26, ColorCardDark, ColorRedNeon);
                be.GetComponent<Button>().onClick.AddListener(exitAction);
            }

            return modal;
        }

        private RectTransform CreateCrosshairTick(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            GameObject tickObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            tickObj.transform.SetParent(parent, false);
            RectTransform rt = tickObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            tickObj.GetComponent<Image>().color = new Color(0f, 0.85f, 1f, 0.85f);
            return rt;
        }

        private GameObject CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor, string text, float fontSize, Color bgColor, Color outlineColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            btnObj.GetComponent<Image>().color = bgColor;
            btnObj.GetComponent<Outline>().effectColor = outlineColor;
            btnObj.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchoredPosition = Vector2.zero; trt.sizeDelta = size;
            TMP_Text tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ColorTextWhite;

            return btnObj;
        }

        private void AddPointerListeners(GameObject target, System.Action<bool> onStateChanged)
        {
            EventTrigger trigger = target.AddComponent<EventTrigger>();

            EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((_) => onStateChanged(true));
            trigger.triggers.Add(down);

            EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((_) => onStateChanged(false));
            trigger.triggers.Add(up);
        }

        private GameObject CreateModalDialog(string name, Transform parent, string title, string btn1Text, UnityEngine.Events.UnityAction btn1Action, string btn2Text, UnityEngine.Events.UnityAction btn2Action, string exitText, UnityEngine.Events.UnityAction exitAction)
        {
            GameObject modal = new GameObject(name, typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(parent, false);
            RectTransform mRt = modal.GetComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one; mRt.sizeDelta = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.95f);

            GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(Outline));
            box.transform.SetParent(modal.transform, false);
            RectTransform bRt = box.GetComponent<RectTransform>();
            bRt.anchoredPosition = Vector2.zero; bRt.sizeDelta = new Vector2(700f, 480f);
            box.GetComponent<Image>().color = ColorCardDark;
            box.GetComponent<Outline>().effectColor = ColorCyanNeon;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(box.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 150f); tRt.sizeDelta = new Vector2(660f, 100f);
            TMP_Text tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = title;
            tTmp.fontSize = 38;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = ColorTextWhite;

            float yOffset = 40f;
            if (!string.IsNullOrEmpty(btn1Text) && btn1Action != null)
            {
                GameObject b1 = CreateActionButton("Btn1", box.transform, new Vector2(0f, yOffset), new Vector2(500f, 75f), new Vector2(0.5f, 0.5f), btn1Text, 28, new Color(0f, 0.35f, 0.55f), ColorCyanNeon);
                b1.GetComponent<Button>().onClick.AddListener(btn1Action);
                yOffset -= 95f;
            }

            if (!string.IsNullOrEmpty(btn2Text) && btn2Action != null)
            {
                GameObject b2 = CreateActionButton("Btn2", box.transform, new Vector2(0f, yOffset), new Vector2(500f, 75f), new Vector2(0.5f, 0.5f), btn2Text, 28, ColorCardDark, ColorCyanNeon);
                b2.GetComponent<Button>().onClick.AddListener(btn2Action);
                yOffset -= 95f;
            }

            if (!string.IsNullOrEmpty(exitText) && exitAction != null)
            {
                GameObject be = CreateActionButton("ExitBtn", box.transform, new Vector2(0f, yOffset), new Vector2(500f, 75f), new Vector2(0.5f, 0.5f), exitText, 28, ColorCardDark, ColorRedNeon);
                be.GetComponent<Button>().onClick.AddListener(exitAction);
            }

            return modal;
        }

        #endregion
    }
}
