using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Landscape Mobile Shooting HUD & Control Interface.
    /// Manages the virtual movement joystick, touch action buttons (Fire, Aim, Jump, Reload),
    /// dynamic crosshair, hitmarker, health bar, ammo display, pause menu, and victory/defeat screens.
    /// </summary>
    public class ShootingHUD : MonoBehaviour
    {
        #region Palette Constants

        private static readonly Color ColorCyanNeon = new Color(0f, 0.85f, 1f, 1f);
        private static readonly Color ColorRedNeon = new Color(1f, 0.18f, 0.35f, 1f);
        private static readonly Color ColorCardDark = new Color(0.04f, 0.06f, 0.12f, 0.85f);
        private static readonly Color ColorTextWhite = new Color(0.96f, 0.97f, 1f, 1f);

        #endregion

        #region Serialized UI References

        [Header("Player Status")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private Image _damageVignette;

        [Header("Weapon & Ammo")]
        [SerializeField] private TMP_Text _weaponNameText;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private TMP_Text _reloadStatusText;

        [Header("Objective")]
        [SerializeField] private TMP_Text _objectiveText;

        [Header("Crosshair & Hitmarker")]
        [SerializeField] private RectTransform _crosshairRoot;
        [SerializeField] private RectTransform[] _crosshairTicks; // Top, Bottom, Left, Right
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

        #endregion

        #region Private Fields

        private MobileInputController _input;
        private WeaponController _weapon;
        private PlayerHealth _playerHealth;
        private PlayerMovement _playerMovement;

        private bool _isDraggingJoystick;
        private Vector2 _joystickPointerStart;
        private float _joystickRadius = 75f;
        private float _crosshairSpread = 18f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureHUDHierarchy();
        }

        private void Start()
        {
            _input = MobileInputController.Instance;
            HookPlayerComponents();
        }

        private void Update()
        {
            UpdateCrosshairDynamics();
            HandleTouchJoystickInput();
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

            if (_weapon == null)
            {
                _weapon = FindObjectOfType<WeaponController>();
                if (_weapon != null)
                {
                    _weapon.OnAmmoChanged += UpdateAmmoUI;
                    _weapon.OnEnemyHit += TriggerHitMarker;
                    _weapon.OnReloadStarted += () => SetReloadStatus(true);
                    _weapon.OnReloadFinished += () => SetReloadStatus(false);

                    if (_weaponNameText != null) _weaponNameText.text = _weapon.WeaponName;
                    UpdateAmmoUI(_weapon.CurrentAmmo, _weapon.ReserveAmmo);
                }
            }

            if (_playerMovement == null)
            {
                _playerMovement = FindObjectOfType<PlayerMovement>();
            }
        }

        #endregion

        #region UI Updates

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

        public void UpdateAmmoUI(int current, int reserve)
        {
            if (_ammoText != null)
            {
                _ammoText.text = $"<size=48><color=#00D9FF>{current}</color></size> <color=#8FA0C0>/ {reserve}</color>";
            }
        }

        public void UpdateObjectiveUI(int current, int total)
        {
            if (_objectiveText != null)
            {
                _objectiveText.text = $"🎯 TARGETS ELIMINATED: <color=#00D9FF>{current}</color> / {total}";
            }
        }

        private void SetReloadStatus(bool isReloading)
        {
            if (_reloadStatusText != null)
            {
                _reloadStatusText.gameObject.SetActive(isReloading);
                _reloadStatusText.text = isReloading ? "RELOADING..." : "";
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
            _damageVignette.color = new Color(1f, 0f, 0.15f, 0.35f);
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.25f;
                _damageVignette.color = new Color(1f, 0f, 0.15f, Mathf.Lerp(0.35f, 0f, t));
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
            _hitMarkerText.rectTransform.localScale = Vector3.one * 1.35f;

            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.12f;
                _hitMarkerText.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.35f, Vector3.one, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.04f);
            _hitMarkerText.gameObject.SetActive(false);
        }

        private void UpdateCrosshairDynamics()
        {
            if (_crosshairTicks == null || _crosshairTicks.Length < 4) return;

            float targetSpread = 20f;
            if (_playerMovement != null)
            {
                if (_playerMovement.IsAiming) targetSpread = 10f;
                else if (_playerMovement.CurrentSpeedRatio > 0.5f) targetSpread = 38f;
                else if (_playerMovement.CurrentSpeedRatio > 0.05f) targetSpread = 26f;
            }

            _crosshairSpread = Mathf.Lerp(_crosshairSpread, targetSpread, Time.deltaTime * 14f);

            // Ticks: 0:Top, 1:Bottom, 2:Left, 3:Right
            if (_crosshairTicks[0] != null) _crosshairTicks[0].anchoredPosition = new Vector2(0f, _crosshairSpread);
            if (_crosshairTicks[1] != null) _crosshairTicks[1].anchoredPosition = new Vector2(0f, -_crosshairSpread);
            if (_crosshairTicks[2] != null) _crosshairTicks[2].anchoredPosition = new Vector2(-_crosshairSpread, 0f);
            if (_crosshairTicks[3] != null) _crosshairTicks[3].anchoredPosition = new Vector2(_crosshairSpread, 0f);
        }

        #endregion

        #region Touch Joystick Implementation

        private void HandleTouchJoystickInput()
        {
            if (_joystickBase == null || _joystickKnob == null) return;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    // Left side of screen (X < 40%)
                    if (touch.position.x < Screen.width * 0.4f)
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            _isDraggingJoystick = true;
                            _joystickPointerStart = touch.position;
                            _joystickBase.position = touch.position;
                            _joystickKnob.localPosition = Vector2.zero;
                        }
                        else if (_isDraggingJoystick && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                        {
                            Vector2 dragDelta = touch.position - _joystickPointerStart;
                            Vector2 clamped = Vector2.ClampMagnitude(dragDelta, _joystickRadius);
                            _joystickKnob.localPosition = clamped;

                            Vector2 normalized = clamped / _joystickRadius;
                            if (_input != null) _input.SetJoystickInput(normalized);
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            ResetJoystick();
                        }
                    }
                }
            }
            else if (_isDraggingJoystick)
            {
                ResetJoystick();
            }
        }

        private void ResetJoystick()
        {
            _isDraggingJoystick = false;
            if (_joystickKnob != null) _joystickKnob.localPosition = Vector2.zero;
            if (_input != null) _input.SetJoystickInput(Vector2.zero);
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

        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            if (SceneLoader.HasInstance) SceneLoader.Instance.LoadScene(SceneLoader.SHOOTING);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.SHOOTING);
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

            Debug.Log("[ShootingHUD] Procedurally constructing Landscape Mobile Shooting HUD...");

            GameObject canvasObj = new GameObject("LandscapeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // Landscape mobile
            scaler.matchWidthOrHeight = 0.5f;

            // Damage Vignette (fullscreen red overlay)
            GameObject vignObj = new GameObject("DamageVignette", typeof(RectTransform), typeof(Image));
            vignObj.transform.SetParent(canvasObj.transform, false);
            RectTransform vRt = vignObj.GetComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one; vRt.sizeDelta = Vector2.zero;
            _damageVignette = vignObj.GetComponent<Image>();
            _damageVignette.color = new Color(1f, 0f, 0f, 0f);
            _damageVignette.raycastTarget = false;

            // 1. Top Bar: Health Bar & Objective & Pause Button
            // Health Bar (Top-left)
            GameObject hpObj = new GameObject("HealthBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            hpObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hpRt = hpObj.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0f, 1f); hpRt.anchorMax = new Vector2(0f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.anchoredPosition = new Vector2(40f, -40f);
            hpRt.sizeDelta = new Vector2(380f, 60f);
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
            hptRt.anchoredPosition = Vector2.zero; hptRt.sizeDelta = new Vector2(360f, 50f);
            _healthText = hpTextObj.GetComponent<TextMeshProUGUI>();
            _healthText.text = "HP 100 / 100";
            _healthText.fontSize = 26;
            _healthText.alignment = TextAlignmentOptions.Center;
            _healthText.color = ColorTextWhite;

            // Objective Badge (Top-center)
            GameObject objBadge = new GameObject("ObjectiveBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
            objBadge.transform.SetParent(canvasObj.transform, false);
            RectTransform obRt = objBadge.GetComponent<RectTransform>();
            obRt.anchorMin = new Vector2(0.5f, 1f); obRt.anchorMax = new Vector2(0.5f, 1f);
            obRt.pivot = new Vector2(0.5f, 1f);
            obRt.anchoredPosition = new Vector2(0f, -40f);
            obRt.sizeDelta = new Vector2(440f, 60f);
            objBadge.GetComponent<Image>().color = ColorCardDark;
            objBadge.GetComponent<Outline>().effectColor = ColorCyanNeon;

            GameObject objTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            objTextObj.transform.SetParent(objBadge.transform, false);
            RectTransform otRt = objTextObj.GetComponent<RectTransform>();
            otRt.anchoredPosition = Vector2.zero; otRt.sizeDelta = new Vector2(420f, 50f);
            _objectiveText = objTextObj.GetComponent<TextMeshProUGUI>();
            _objectiveText.text = "🎯 TARGETS ELIMINATED: 0 / 5";
            _objectiveText.fontSize = 24;
            _objectiveText.alignment = TextAlignmentOptions.Center;
            _objectiveText.color = ColorTextWhite;

            // Pause Button (Top-right)
            GameObject pauseObj = CreateActionButton("PauseButton", canvasObj.transform, new Vector2(-40f, -40f), new Vector2(70f, 70f),
                new Vector2(1f, 1f), "⏸", 36, ColorCardDark, ColorCyanNeon);
            _pauseButton = pauseObj.GetComponent<Button>();
            _pauseButton.onClick.AddListener(() => ShowPauseMenu(true));

            // 2. Crosshair & HitMarker (Center)
            GameObject crossObj = new GameObject("CrosshairRoot", typeof(RectTransform));
            crossObj.transform.SetParent(canvasObj.transform, false);
            _crosshairRoot = crossObj.GetComponent<RectTransform>();
            _crosshairRoot.anchoredPosition = Vector2.zero;

            _crosshairTicks = new RectTransform[4];
            // Top tick
            _crosshairTicks[0] = CreateCrosshairTick("Tick_Top", _crosshairRoot, new Vector2(3f, 14f), new Vector2(0f, 20f));
            // Bottom tick
            _crosshairTicks[1] = CreateCrosshairTick("Tick_Bottom", _crosshairRoot, new Vector2(3f, 14f), new Vector2(0f, -20f));
            // Left tick
            _crosshairTicks[2] = CreateCrosshairTick("Tick_Left", _crosshairRoot, new Vector2(14f, 3f), new Vector2(-20f, 0f));
            // Right tick
            _crosshairTicks[3] = CreateCrosshairTick("Tick_Right", _crosshairRoot, new Vector2(14f, 3f), new Vector2(20f, 0f));

            // Hitmarker Text ("X")
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

            // 3. Virtual Joystick (Bottom-Left)
            GameObject joyBaseObj = new GameObject("JoystickBase", typeof(RectTransform), typeof(Image), typeof(Outline));
            joyBaseObj.transform.SetParent(canvasObj.transform, false);
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

            // 4. Action Buttons (Bottom-Right)
            // Fire Button (Large Red circle)
            GameObject fireObj = CreateActionButton("FireButton", canvasObj.transform, new Vector2(-120f, 130f), new Vector2(140f, 140f),
                new Vector2(1f, 0f), "🔥\nFIRE", 26, ColorCardDark, ColorRedNeon);
            _fireButton = fireObj.GetComponent<Button>();
            AddPointerListeners(fireObj, (held) => { if (_input != null) _input.SetFireButtonHeld(held); });

            // Aim Button (Cyan circle)
            GameObject aimObj = CreateActionButton("AimButton", canvasObj.transform, new Vector2(-280f, 90f), new Vector2(100f, 100f),
                new Vector2(1f, 0f), "🎯\nAIM", 22, ColorCardDark, ColorCyanNeon);
            _aimButton = aimObj.GetComponent<Button>();
            _aimButton.onClick.AddListener(() => { if (_input != null) _input.ToggleAim(); });

            // Jump Button
            GameObject jumpObj = CreateActionButton("JumpButton", canvasObj.transform, new Vector2(-100f, 300f), new Vector2(90f, 90f),
                new Vector2(1f, 0f), "⤊\nJUMP", 20, ColorCardDark, ColorCyanNeon);
            _jumpButton = jumpObj.GetComponent<Button>();
            _jumpButton.onClick.AddListener(() => { if (_input != null) _input.TriggerJump(); });

            // Reload Button
            GameObject reloadObj = CreateActionButton("ReloadButton", canvasObj.transform, new Vector2(-240f, 220f), new Vector2(90f, 90f),
                new Vector2(1f, 0f), "🔄\nRELOAD", 18, ColorCardDark, ColorCyanNeon);
            _reloadButton = reloadObj.GetComponent<Button>();
            _reloadButton.onClick.AddListener(() => { if (_input != null) _input.TriggerReload(); });

            // 5. Ammo Box & Weapon Info (Bottom-Right corner)
            GameObject ammoBox = new GameObject("AmmoBox", typeof(RectTransform), typeof(Image));
            ammoBox.transform.SetParent(canvasObj.transform, false);
            RectTransform abRt = ammoBox.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(1f, 0f); abRt.anchorMax = new Vector2(1f, 0f);
            abRt.pivot = new Vector2(1f, 0f);
            abRt.anchoredPosition = new Vector2(-400f, 30f);
            abRt.sizeDelta = new Vector2(300f, 100f);
            ammoBox.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);

            GameObject wNameObj = new GameObject("WeaponName", typeof(RectTransform), typeof(TextMeshProUGUI));
            wNameObj.transform.SetParent(ammoBox.transform, false);
            RectTransform wnRt = wNameObj.GetComponent<RectTransform>();
            wnRt.anchoredPosition = new Vector2(0f, 25f); wnRt.sizeDelta = new Vector2(280f, 40f);
            _weaponNameText = wNameObj.GetComponent<TextMeshProUGUI>();
            _weaponNameText.text = "M4A1 TACTICAL RIFLE";
            _weaponNameText.fontSize = 20;
            _weaponNameText.alignment = TextAlignmentOptions.Right;
            _weaponNameText.color = new Color(0.7f, 0.8f, 1f);

            GameObject aCountObj = new GameObject("AmmoCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            aCountObj.transform.SetParent(ammoBox.transform, false);
            RectTransform acRt = aCountObj.GetComponent<RectTransform>();
            acRt.anchoredPosition = new Vector2(0f, -15f); acRt.sizeDelta = new Vector2(280f, 50f);
            _ammoText = aCountObj.GetComponent<TextMeshProUGUI>();
            _ammoText.text = "<size=48><color=#00D9FF>30</color></size> / 120";
            _ammoText.fontSize = 32;
            _ammoText.alignment = TextAlignmentOptions.Right;

            // 6. Modals (Pause, Victory, Defeat)
            _pauseModal = CreateModalDialog("PauseModal", canvasObj.transform, "MISSION PAUSED", "RESUME", OnResumeClicked, "RESTART", OnRestartClicked, "EXIT TO HUB", OnReturnToMenuClicked);
            _victoryModal = CreateModalDialog("VictoryModal", canvasObj.transform, "MISSION ACCOMPLISHED!\n<size=28><color=#00D9FF>ALL THREATS ELIMINATED</color></size>", "REPLAY MISSION", OnRestartClicked, "", null, "RETURN TO HUB", OnReturnToMenuClicked);
            _defeatModal = CreateModalDialog("DefeatModal", canvasObj.transform, "OPERATIVE COMPROMISED\n<size=28><color=#FF2E59>CRITICAL HEALTH FAILURE</color></size>", "RESPAWN / RETRY", OnRestartClicked, "", null, "RETURN TO HUB", OnReturnToMenuClicked);

            _pauseModal.SetActive(false);
            _victoryModal.SetActive(false);
            _defeatModal.SetActive(false);
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
