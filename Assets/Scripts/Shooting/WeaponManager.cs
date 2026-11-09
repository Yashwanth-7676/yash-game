using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Master manager for the 4 weapons (Assault Rifle, Pistol, Shotgun, Sniper Rifle).
    /// Handles equipped weapon slots, independent ammo tracking, procedural 3D weapon models,
    /// smooth switching feedback, raycasting, pellet spreading, and reload mechanics.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        #region Structs & Classes

        [System.Serializable]
        public class WeaponSlot
        {
            public WeaponData data;
            public int currentMag;
            public int reserveAmmo;
            public GameObject modelObject;
            public Transform muzzleTransform;
            public Light muzzleLight;
            public GameObject muzzleFlashMesh;
            public AudioClip gunshotClip;
            public AudioClip reloadClip;
        }

        #endregion

        #region Serialized Settings

        [Header("Weapon Slots")]
        [SerializeField] private List<WeaponSlot> _weapons = new List<WeaponSlot>();
        [SerializeField] private int _activeSlotIndex = 0;

        [Header("Switching Settings")]
        [SerializeField] private float _switchDuration = 0.25f;

        #endregion

        #region Public Properties

        public WeaponSlot ActiveWeapon => (_weapons != null && _weapons.Count > _activeSlotIndex) ? _weapons[_activeSlotIndex] : null;
        public int ActiveSlotIndex => _activeSlotIndex;
        public int TotalWeaponsCount => _weapons != null ? _weapons.Count : 0;
        public bool IsSwitching => _isSwitching;
        public bool IsReloading => _isReloading;
        public bool CanShoot => !_isSwitching && !_isReloading && ActiveWeapon != null && ActiveWeapon.currentMag > 0 && Time.time >= _nextFireTime;

        #endregion

        #region Events

        public event System.Action<int, WeaponData> OnWeaponSwitched; // (slotIndex, data)
        public event System.Action<int, int> OnAmmoUpdated; // (currentMag, reserve)
        public event System.Action OnReloadStarted;
        public event System.Action OnReloadFinished;
        public event System.Action OnShotFired;

        #endregion

        #region Private Fields

        private bool _isSwitching;
        private bool _isReloading;
        private float _nextFireTime;
        private float _shotInterval;

        private Transform _mountSocket;
        private Camera _mainCamera;
        private MobileInputController _input;
        private PlayerMovement _playerMovement;
        private ThirdPersonCamera _cameraController;
        private AudioSource _audioSource;

        private bool _hasShotThisPress;

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

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _mainCamera = Camera.main;
            _playerMovement = GetComponentInParent<PlayerMovement>();

            EnsureMountSocket();
            InitializeWeapons();
        }

        private void Start()
        {
            _input = MobileInputController.Instance;
            if (_mainCamera != null)
            {
                _cameraController = _mainCamera.GetComponent<ThirdPersonCamera>();
            }

            EquipWeapon(_activeSlotIndex, true);
        }

        private void Update()
        {
            if (_input == null)
            {
                _input = MobileInputController.Instance;
                if (_input == null) return;
            }

            HandleWeaponSwitchInput();
            HandleShootingInput();
            HandleReloadInput();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Weapons Initialization

        private void EnsureMountSocket()
        {
            if (_playerMovement == null) _playerMovement = GetComponentInParent<PlayerMovement>();
            _mountSocket = (_playerMovement != null && _playerMovement.WeaponSocket != null) ? _playerMovement.WeaponSocket : transform;
        }

        private void InitializeWeapons()
        {
            _weapons.Clear();

            // 1. Assault Rifle
            AddWeaponSlot(WeaponData.CreateAssaultRifle());

            // 2. Pistol
            AddWeaponSlot(WeaponData.CreatePistol());

            // 3. Shotgun
            AddWeaponSlot(WeaponData.CreateShotgun());

            // 4. Sniper Rifle
            AddWeaponSlot(WeaponData.CreateSniperRifle());
        }

        private void AddWeaponSlot(WeaponData data)
        {
            WeaponSlot slot = new WeaponSlot
            {
                data = data,
                currentMag = data.magazineSize,
                reserveAmmo = data.maxReserveAmmo
            };

            // Audio clips
            slot.gunshotClip = Resources.Load<AudioClip>(data.gunshotAudioResource);
            slot.reloadClip = Resources.Load<AudioClip>(data.reloadAudioResource);

            // Construct 3D weapon model
            slot.modelObject = BuildWeaponModel(data, _mountSocket, out Transform muzzleT, out Light muzzleL, out GameObject muzzleMesh);
            slot.muzzleTransform = muzzleT;
            slot.muzzleLight = muzzleL;
            slot.muzzleFlashMesh = muzzleMesh;
            slot.modelObject.SetActive(false);

            _weapons.Add(slot);
        }

        #endregion

        #region Weapon Switching

        public void SwitchToSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _weapons.Count) return;
            if (slotIndex == _activeSlotIndex && !_isReloading) return;
            if (_isSwitching) return;

            StopAllCoroutines();
            StartCoroutine(SwitchWeaponRoutine(slotIndex));
        }

        private void HandleWeaponSwitchInput()
        {
            // Keyboard 1, 2, 3, 4
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSlot(3);

            // Q key toggles between primary and secondary
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int next = (_activeSlotIndex == 0) ? 1 : 0;
                SwitchToSlot(next);
            }

            // Mouse scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.05f)
            {
                int next = (_activeSlotIndex + 1) % _weapons.Count;
                SwitchToSlot(next);
            }
            else if (scroll < -0.05f)
            {
                int prev = (_activeSlotIndex - 1 + _weapons.Count) % _weapons.Count;
                SwitchToSlot(prev);
            }
        }

        private IEnumerator SwitchWeaponRoutine(int targetSlot)
        {
            _isSwitching = true;
            _isReloading = false;

            // Lower current weapon model
            if (ActiveWeapon != null && ActiveWeapon.modelObject != null)
            {
                Vector3 startPos = ActiveWeapon.modelObject.transform.localPosition;
                Vector3 downPos = startPos + new Vector3(0f, -0.2f, -0.1f);

                float elapsed = 0f;
                float halfTime = _switchDuration * 0.5f;

                while (elapsed < halfTime)
                {
                    elapsed += Time.deltaTime;
                    ActiveWeapon.modelObject.transform.localPosition = Vector3.Lerp(startPos, downPos, elapsed / halfTime);
                    yield return null;
                }

                ActiveWeapon.modelObject.SetActive(false);
                ActiveWeapon.modelObject.transform.localPosition = startPos;
            }

            // Switch to new slot
            EquipWeapon(targetSlot, false);

            // Raise new weapon model
            if (ActiveWeapon != null && ActiveWeapon.modelObject != null)
            {
                ActiveWeapon.modelObject.SetActive(true);
                Vector3 finalPos = Vector3.zero;
                Vector3 startPos = finalPos + new Vector3(0f, -0.2f, -0.1f);
                ActiveWeapon.modelObject.transform.localPosition = startPos;

                float elapsed = 0f;
                float halfTime = _switchDuration * 0.5f;

                while (elapsed < halfTime)
                {
                    elapsed += Time.deltaTime;
                    ActiveWeapon.modelObject.transform.localPosition = Vector3.Lerp(startPos, finalPos, elapsed / halfTime);
                    yield return null;
                }

                ActiveWeapon.modelObject.transform.localPosition = finalPos;
            }

            _isSwitching = false;
        }

        private void EquipWeapon(int index, bool instant)
        {
            _activeSlotIndex = Mathf.Clamp(index, 0, _weapons.Count - 1);
            WeaponSlot slot = ActiveWeapon;
            if (slot == null) return;

            _shotInterval = 60f / slot.data.fireRateRPM;

            // Hide all other weapon models
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i].modelObject != null)
                {
                    _weapons[i].modelObject.SetActive(i == _activeSlotIndex);
                }
            }

            OnWeaponSwitched?.Invoke(_activeSlotIndex, slot.data);
            OnAmmoUpdated?.Invoke(slot.currentMag, slot.reserveAmmo);
        }

        public WeaponSlot GetSlot(int index)
        {
            if (index >= 0 && index < _weapons.Count) return _weapons[index];
            return null;
        }

        #endregion

        #region Shooting Mechanics

        private void HandleShootingInput()
        {
            if (_input == null) return;

            bool isFireHeld = _input.IsFirePressed;

            if (ActiveWeapon != null && !ActiveWeapon.data.isAutomatic)
            {
                // Semi-auto (Pistol, Shotgun, Sniper): only fire once per press
                if (isFireHeld)
                {
                    if (!_hasShotThisPress)
                    {
                        _hasShotThisPress = true;
                        TryShoot();
                    }
                }
                else
                {
                    _hasShotThisPress = false;
                }
            }
            else
            {
                // Full-auto (Assault Rifle): continuous fire while held
                if (isFireHeld)
                {
                    TryShoot();
                }
            }
        }

        public bool TryShoot()
        {
            if (_isSwitching || _isReloading) return false;

            WeaponSlot slot = ActiveWeapon;
            if (slot == null) return false;

            if (slot.currentMag <= 0)
            {
                // Play empty dry-fire click
                if (Time.time >= _nextFireTime)
                {
                    _nextFireTime = Time.time + 0.25f;
                    if (_audioSource != null && slot.reloadClip != null)
                    {
                        _audioSource.pitch = 2.0f;
                        _audioSource.PlayOneShot(slot.reloadClip, 0.4f);
                    }
                }

                // Auto reload if reserve available
                if (slot.reserveAmmo > 0)
                {
                    StartCoroutine(ReloadRoutine());
                }
                return false;
            }

            if (Time.time < _nextFireTime) return false;

            _nextFireTime = Time.time + _shotInterval;
            slot.currentMag--;

            OnAmmoUpdated?.Invoke(slot.currentMag, slot.reserveAmmo);
            OnShotFired?.Invoke();

            ExecuteRaycastShot(slot);
            PlayMuzzleEffects(slot);
            PlayGunshotAudio(slot);

            // Screen shake & Camera Recoil
            if (CombatEffects.Instance != null)
            {
                CombatEffects.Instance.TriggerScreenShake(slot.data.screenShakeIntensity, 0.12f);
            }

            if (_cameraController != null)
            {
                _cameraController.ApplyRecoilKick(slot.data.cameraRecoilKick);
            }

            return true;
        }

        private void ExecuteRaycastShot(WeaponSlot slot)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            float baseSpread = slot.data.spreadAngle;
            if (_playerMovement != null)
            {
                if (_playerMovement.IsAiming) baseSpread = slot.data.aimSpreadAngle;
                else if (_playerMovement.CurrentSpeedRatio > 0.5f) baseSpread = slot.data.runningSpreadAngle;
            }

            Vector3 muzzlePos = slot.muzzleTransform != null ? slot.muzzleTransform.position : transform.position + transform.forward * 0.5f;

            // Fire each pellet (1 for AR/Pistol/Sniper, 8 for Shotgun)
            int pellets = Mathf.Max(1, slot.data.pelletCount);

            for (int p = 0; p < pellets; p++)
            {
                Vector2 spreadOffset = Random.insideUnitCircle * baseSpread;
                Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f + spreadOffset.x, 0.5f + spreadOffset.y, 0f));

                Vector3 hitPoint = ray.origin + ray.direction * slot.data.range;
                Vector3 hitNormal = -ray.direction;
                bool isEnemyHit = false;

                if (Physics.Raycast(ray, out RaycastHit hit, slot.data.range, ~0, QueryTriggerInteraction.Ignore))
                {
                    hitPoint = hit.point;
                    hitNormal = hit.normal;

                    EnemyHealth eh = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (eh != null && eh.IsAlive)
                    {
                        isEnemyHit = true;
                        eh.TakeDamage(slot.data.damage, hit.point, ray.direction);

                        if (CombatEffects.Instance != null)
                        {
                            CombatEffects.Instance.PlayHitMarkerChime();
                        }

                        ShootingHUD hud = FindObjectOfType<ShootingHUD>();
                        if (hud != null) hud.TriggerHitMarker();
                    }
                }

                // Tracers and Impacts via CombatEffects pooling
                if (CombatEffects.Instance != null)
                {
                    CombatEffects.Instance.SpawnBulletTracer(muzzlePos, hitPoint, slot.data.themeColor);
                    CombatEffects.Instance.SpawnImpact(hitPoint, hitNormal, isEnemyHit);
                }
            }
        }

        #endregion

        #region Reloading

        private void HandleReloadInput()
        {
            if (_input != null && _input.ReloadTriggered && !_isReloading && !_isSwitching)
            {
                WeaponSlot slot = ActiveWeapon;
                if (slot != null && slot.currentMag < slot.data.magazineSize && slot.reserveAmmo > 0)
                {
                    StartCoroutine(ReloadRoutine());
                }
            }
        }

        public void TriggerReload()
        {
            if (!_isReloading && !_isSwitching && ActiveWeapon != null && ActiveWeapon.currentMag < ActiveWeapon.data.magazineSize && ActiveWeapon.reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            WeaponSlot slot = ActiveWeapon;
            if (slot == null) yield break;

            OnReloadStarted?.Invoke();

            if (_audioSource != null && slot.reloadClip != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(slot.reloadClip, 0.85f);
            }

            // Lower weapon slightly during reload
            Vector3 origPos = Vector3.zero;
            if (slot.modelObject != null)
            {
                slot.modelObject.transform.localPosition = new Vector3(0f, -0.15f, -0.08f);
            }

            yield return new WaitForSeconds(slot.data.reloadDuration);

            if (slot.modelObject != null)
            {
                slot.modelObject.transform.localPosition = origPos;
            }

            int needed = slot.data.magazineSize - slot.currentMag;
            int toFill = Mathf.Min(needed, slot.reserveAmmo);
            slot.currentMag += toFill;
            slot.reserveAmmo -= toFill;

            _isReloading = false;
            OnAmmoUpdated?.Invoke(slot.currentMag, slot.reserveAmmo);
            OnReloadFinished?.Invoke();
        }

        #endregion

        #region Audio & Visual Feedback

        private void PlayMuzzleEffects(WeaponSlot slot)
        {
            if (slot.muzzleLight != null)
            {
                StopCoroutine(nameof(MuzzleLightFlashRoutine));
                StartCoroutine(MuzzleLightFlashRoutine(slot.muzzleLight, slot.muzzleFlashMesh));
            }
        }

        private IEnumerator MuzzleLightFlashRoutine(Light l, GameObject mesh)
        {
            if (l != null) l.enabled = true;
            if (mesh != null) mesh.SetActive(true);

            yield return new WaitForSeconds(0.045f);

            if (l != null) l.enabled = false;
            if (mesh != null) mesh.SetActive(false);
        }

        private void PlayGunshotAudio(WeaponSlot slot)
        {
            if (slot.gunshotClip != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.96f, 1.04f);
                _audioSource.PlayOneShot(slot.gunshotClip, 0.9f);
            }
        }

        #endregion

        #region Procedural 3D Weapon Models

        private GameObject BuildWeaponModel(WeaponData data, Transform parent, out Transform muzzleT, out Light muzzleL, out GameObject muzzleMesh)
        {
            GameObject model = new GameObject($"WeaponModel_{data.abbreviation}");
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Material gunMetal = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            gunMetal.color = new Color(0.12f, 0.14f, 0.17f);

            Material carbonDark = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            carbonDark.color = new Color(0.06f, 0.07f, 0.09f);

            Material accentMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            accentMat.color = data.themeColor;
            accentMat.EnableKeyword("_EMISSION");
            accentMat.SetColor("_EmissionColor", data.themeColor * 1.5f);

            float muzzleZ = 0.5f;

            switch (data.weaponType)
            {
                case WeaponType.AssaultRifle:
                    // Assault Rifle Body
                    CreateBox(model.transform, "Receiver", new Vector3(0f, 0.04f, 0.05f), new Vector3(0.06f, 0.09f, 0.32f), gunMetal);
                    CreateCylinder(model.transform, "Barrel", new Vector3(0f, 0.05f, 0.32f), new Vector3(0.025f, 0.14f, 0.025f), 90f, carbonDark);
                    CreateBox(model.transform, "Handguard", new Vector3(0f, 0.04f, 0.24f), new Vector3(0.055f, 0.075f, 0.18f), carbonDark);
                    CreateBox(model.transform, "Mag", new Vector3(0f, -0.08f, 0.1f), new Vector3(0.04f, 0.14f, 0.07f), carbonDark);
                    CreateBox(model.transform, "Stock", new Vector3(0f, 0.02f, -0.2f), new Vector3(0.045f, 0.11f, 0.18f), carbonDark);
                    CreateBox(model.transform, "LaserSight", new Vector3(0.04f, 0.07f, 0.18f), new Vector3(0.02f, 0.02f, 0.06f), accentMat);
                    muzzleZ = 0.48f;
                    break;

                case WeaponType.Pistol:
                    // Compact Combat Pistol
                    CreateBox(model.transform, "Slide", new Vector3(0f, 0.05f, 0.05f), new Vector3(0.045f, 0.05f, 0.20f), gunMetal);
                    CreateBox(model.transform, "Grip", new Vector3(0f, -0.06f, -0.03f), new Vector3(0.04f, 0.12f, 0.06f), carbonDark);
                    CreateBox(model.transform, "Laser", new Vector3(0f, 0.01f, 0.1f), new Vector3(0.025f, 0.02f, 0.06f), accentMat);
                    muzzleZ = 0.16f;
                    break;

                case WeaponType.Shotgun:
                    // Heavy 12-Gauge Pump Shotgun
                    CreateBox(model.transform, "Receiver", new Vector3(0f, 0.04f, 0.02f), new Vector3(0.07f, 0.10f, 0.28f), carbonDark);
                    CreateCylinder(model.transform, "WideBarrel", new Vector3(0f, 0.06f, 0.32f), new Vector3(0.045f, 0.18f, 0.045f), 90f, gunMetal);
                    CreateCylinder(model.transform, "MagTube", new Vector3(0f, 0.02f, 0.30f), new Vector3(0.035f, 0.16f, 0.035f), 90f, carbonDark);
                    CreateBox(model.transform, "PumpSlide", new Vector3(0f, 0.02f, 0.24f), new Vector3(0.06f, 0.06f, 0.12f), accentMat);
                    CreateBox(model.transform, "Stock", new Vector3(0f, 0.01f, -0.22f), new Vector3(0.05f, 0.12f, 0.22f), carbonDark);
                    muzzleZ = 0.52f;
                    break;

                case WeaponType.SniperRifle:
                    // Long-Barreled Vanguard .50 Cal Sniper
                    CreateBox(model.transform, "LongReceiver", new Vector3(0f, 0.05f, 0.02f), new Vector3(0.065f, 0.09f, 0.36f), carbonDark);
                    CreateCylinder(model.transform, "LongBarrel", new Vector3(0f, 0.06f, 0.50f), new Vector3(0.03f, 0.32f, 0.03f), 90f, gunMetal);
                    CreateBox(model.transform, "MuzzleBrake", new Vector3(0f, 0.06f, 0.82f), new Vector3(0.06f, 0.05f, 0.08f), gunMetal);
                    // High-Magnification Scope
                    CreateCylinder(model.transform, "HighScope", new Vector3(0f, 0.13f, 0.05f), new Vector3(0.04f, 0.14f, 0.04f), 90f, carbonDark);
                    CreateBox(model.transform, "ScopeMount", new Vector3(0f, 0.09f, 0.05f), new Vector3(0.025f, 0.04f, 0.16f), gunMetal);
                    CreateBox(model.transform, "ScopeLens", new Vector3(0f, 0.13f, 0.19f), new Vector3(0.035f, 0.035f, 0.01f), accentMat);
                    CreateBox(model.transform, "HeavyStock", new Vector3(0f, 0.02f, -0.26f), new Vector3(0.05f, 0.13f, 0.24f), carbonDark);
                    muzzleZ = 0.86f;
                    break;
            }

            // Muzzle Transform
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(model.transform, false);
            muzzleObj.transform.localPosition = new Vector3(0f, 0.05f, muzzleZ);
            muzzleT = muzzleObj.transform;

            // Muzzle Light
            muzzleL = muzzleObj.AddComponent<Light>();
            muzzleL.type = LightType.Point;
            muzzleL.range = 6.5f;
            muzzleL.intensity = 3.5f;
            muzzleL.color = new Color(1f, 0.85f, 0.45f);
            muzzleL.enabled = false;

            // Muzzle Flare
            muzzleMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleMesh.transform.SetParent(muzzleObj.transform, false);
            muzzleMesh.transform.localScale = new Vector3(0.12f, 0.12f, 0.18f);
            Material flMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            flMat.color = new Color(1f, 0.9f, 0.5f);
            muzzleMesh.GetComponent<Renderer>().material = flMat;
            Destroy(muzzleMesh.GetComponent<Collider>());
            muzzleMesh.SetActive(false);

            return model;
        }

        private void CreateBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = pos;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().material = mat;
            Destroy(box.GetComponent<Collider>());
        }

        private void CreateCylinder(Transform parent, string name, Vector3 pos, Vector3 size, float rotX, Material mat)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            cyl.transform.SetParent(parent, false);
            cyl.transform.localPosition = pos;
            cyl.transform.localScale = size;
            cyl.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
            cyl.GetComponent<Renderer>().material = mat;
            Destroy(cyl.GetComponent<Collider>());
        }

        #endregion
    }
}
