using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Weapon controller for the assault rifle (M4A1).
    /// Handles raycast shooting, hit detection, magazine & reserve ammo,
    /// fire rate pacing, reloading, muzzle flash light, bullet tracers,
    /// audio feedback, and object pooling for mobile performance.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Weapon Stats")]
        [SerializeField, Tooltip("Weapon display name.")]
        private string _weaponName = "M4A1 TACTICAL RIFLE";

        [SerializeField, Tooltip("Magazine capacity.")]
        private int _magazineSize = 30;

        [SerializeField, Tooltip("Maximum reserve ammo.")]
        private int _maxReserveAmmo = 120;

        [SerializeField, Tooltip("Damage dealt per bullet.")]
        private float _damage = 25f;

        [SerializeField, Tooltip("Rounds per minute (e.g. 600 RPM = 0.1s shot interval).")]
        private float _fireRateRpm = 600f;

        [SerializeField, Tooltip("Maximum effective raycast range.")]
        private float _range = 150f;

        [SerializeField, Tooltip("Duration of weapon reload in seconds.")]
        private float _reloadDuration = 1.8f;

        [Header("Spread & Recoil")]
        [SerializeField, Tooltip("Bullet spread angle when aiming.")]
        private float _aimSpread = 0.005f;

        [SerializeField, Tooltip("Bullet spread angle when standing/walking.")]
        private float _normalSpread = 0.02f;

        [SerializeField, Tooltip("Bullet spread angle when running.")]
        private float _runningSpread = 0.045f;

        [SerializeField, Tooltip("Recoil kick angle on camera.")]
        private float _cameraRecoilKick = 0.8f;

        [Header("Visual References")]
        [SerializeField, Tooltip("Socket / Transform where weapon model resides.")]
        private Transform _mountSocket;

        [SerializeField, Tooltip("Tip of barrel where muzzle flash and tracers originate.")]
        private Transform _muzzleTransform;

        #endregion

        #region Public Properties

        public string WeaponName => _weaponName;
        public int CurrentAmmo => _currentAmmo;
        public int MagazineSize => _magazineSize;
        public int ReserveAmmo => _reserveAmmo;
        public bool IsReloading => _isReloading;
        public bool CanShoot => !_isReloading && _currentAmmo > 0 && Time.time >= _nextFireTime;

        #endregion

        #region Events

        public event System.Action<int, int> OnAmmoChanged; // (current, reserve)
        public event System.Action OnReloadStarted;
        public event System.Action OnReloadFinished;
        public event System.Action OnShotFired;
        public event System.Action OnEnemyHit;

        #endregion

        #region Private Fields

        private int _currentAmmo;
        private int _reserveAmmo;
        private bool _isReloading;
        private float _nextFireTime;
        private float _shotInterval;

        private Camera _mainCamera;
        private MobileInputController _input;
        private PlayerMovement _playerMovement;
        private AudioSource _audioSource;

        // Visual Effects
        private Light _muzzleLight;
        private GameObject _muzzleFlashMesh;
        private Transform _rifleModel;

        // Audio Clips
        private AudioClip _gunshotClip;
        private AudioClip _reloadClip;
        private AudioClip _hitMarkerClip;

        // Object Pools
        private Queue<LineRenderer> _tracerPool = new Queue<LineRenderer>();
        private Queue<GameObject> _impactPool = new Queue<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _shotInterval = 60f / _fireRateRpm;
            _currentAmmo = _magazineSize;
            _reserveAmmo = _maxReserveAmmo;

            _mainCamera = Camera.main;
            _playerMovement = GetComponentInParent<PlayerMovement>();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D player audio

            LoadAudioClips();
            EnsureRifleModel();
            InitializeObjectPools();
        }

        private void Start()
        {
            _input = MobileInputController.Instance;
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
        }

        private void Update()
        {
            if (_input == null)
            {
                _input = MobileInputController.Instance;
                if (_input == null) return;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // Handle Fire Trigger
            if (_input.IsFirePressed)
            {
                TryShoot();
            }

            // Handle Reload Trigger
            if (_input.ReloadTriggered && !_isReloading && _currentAmmo < _magazineSize && _reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }

            // Auto-reload on empty fire
            if (_input.IsFirePressed && _currentAmmo == 0 && !_isReloading && _reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        #endregion

        #region Shooting Mechanics

        public bool TryShoot()
        {
            if (_isReloading) return false;

            if (_currentAmmo <= 0)
            {
                // Play dry-fire click
                if (Time.time >= _nextFireTime)
                {
                    _nextFireTime = Time.time + 0.25f;
                    PlaySound(_reloadClip, 0.4f, 2.0f);
                }
                return false;
            }

            if (Time.time < _nextFireTime) return false;

            _nextFireTime = Time.time + _shotInterval;
            _currentAmmo--;
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
            OnShotFired?.Invoke();

            ExecuteRaycastShot();
            PlayMuzzleEffects();
            PlayGunshotAudio();

            return true;
        }

        private void ExecuteRaycastShot()
        {
            if (_mainCamera == null) return;

            // Calculate Spread based on player movement state
            float spread = _normalSpread;
            if (_playerMovement != null)
            {
                if (_playerMovement.IsAiming) spread = _aimSpread;
                else if (_playerMovement.CurrentSpeedRatio > 0.6f) spread = _runningSpread;
            }

            Vector2 randomCircle = Random.insideUnitCircle * spread;
            Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f + randomCircle.x, 0.5f + randomCircle.y, 0f));

            Vector3 hitPoint = ray.origin + ray.direction * _range;
            Vector3 hitNormal = -ray.direction;
            bool hitSomething = false;

            if (Physics.Raycast(ray, out RaycastHit hit, _range, ~0, QueryTriggerInteraction.Ignore))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
                hitSomething = true;

                // Check for Enemy Health
                EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth.IsAlive)
                {
                    enemyHealth.TakeDamage(_damage, hit.point, ray.direction);
                    PlayHitFeedback();
                    SpawnImpactEffect(hit.point, hit.normal, true);
                }
                else
                {
                    SpawnImpactEffect(hit.point, hit.normal, false);
                }
            }

            // Spawn tracer line from muzzle to hit point
            Vector3 muzzlePos = _muzzleTransform != null ? _muzzleTransform.position : transform.position + transform.forward * 0.5f;
            SpawnBulletTracer(muzzlePos, hitPoint);
        }

        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            OnReloadStarted?.Invoke();
            PlaySound(_reloadClip, 0.9f, 1.0f);

            // Procedural weapon lowering animation during reload
            Vector3 origLocalPos = _mountSocket != null ? _mountSocket.localPosition : Vector3.zero;
            if (_mountSocket != null)
            {
                _mountSocket.localPosition = origLocalPos + new Vector3(0f, -0.15f, -0.08f);
            }

            yield return new WaitForSeconds(_reloadDuration);

            if (_mountSocket != null)
            {
                _mountSocket.localPosition = origLocalPos;
            }

            int needed = _magazineSize - _currentAmmo;
            int toLoad = Mathf.Min(needed, _reserveAmmo);
            _currentAmmo += toLoad;
            _reserveAmmo -= toLoad;

            _isReloading = false;
            OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
            OnReloadFinished?.Invoke();
        }

        #endregion

        #region Visual & Audio Feedback

        private void PlayMuzzleEffects()
        {
            StopCoroutine(nameof(MuzzleFlashRoutine));
            StartCoroutine(nameof(MuzzleFlashRoutine));

            // Subtle procedural weapon recoil kick
            if (_rifleModel != null)
            {
                StopCoroutine(nameof(WeaponRecoilRoutine));
                StartCoroutine(nameof(WeaponRecoilRoutine));
            }
        }

        private IEnumerator MuzzleFlashRoutine()
        {
            if (_muzzleLight != null) _muzzleLight.enabled = true;
            if (_muzzleFlashMesh != null) _muzzleFlashMesh.SetActive(true);

            yield return new WaitForSeconds(0.04f);

            if (_muzzleLight != null) _muzzleLight.enabled = false;
            if (_muzzleFlashMesh != null) _muzzleFlashMesh.SetActive(false);
        }

        private IEnumerator WeaponRecoilRoutine()
        {
            Vector3 basePos = Vector3.zero;
            Vector3 kickPos = new Vector3(0f, 0.02f, -0.06f);

            _rifleModel.localPosition = kickPos;
            float elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                _rifleModel.localPosition = Vector3.Lerp(kickPos, basePos, elapsed / 0.08f);
                yield return null;
            }
            _rifleModel.localPosition = basePos;
        }

        private void PlayGunshotAudio()
        {
            float pitchVariation = Random.Range(0.96f, 1.04f);
            PlaySound(_gunshotClip, 0.85f, pitchVariation);
        }

        private void PlayHitFeedback()
        {
            PlaySound(_hitMarkerClip, 0.8f, 1.0f);
            OnEnemyHit?.Invoke();
        }

        private void PlaySound(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume);
        }

        private void LoadAudioClips()
        {
            _gunshotClip = Resources.Load<AudioClip>("Audio/Shooting/Gunshot_Rifle");
            _reloadClip = Resources.Load<AudioClip>("Audio/Shooting/Weapon_Reload");
            _hitMarkerClip = Resources.Load<AudioClip>("Audio/Shooting/HitMarker");
        }

        #endregion

        #region Visual Effects Pooling (Tracers & Impacts)

        private void InitializeObjectPools()
        {
            // Tracers Pool
            for (int i = 0; i < 10; i++)
            {
                GameObject tracerObj = new GameObject($"Tracer_{i}");
                tracerObj.transform.SetParent(transform);
                LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
                lr.startWidth = 0.04f;
                lr.endWidth = 0.015f;
                lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                lr.material.color = new Color(1f, 0.85f, 0.4f, 0.9f);
                lr.enabled = false;
                _tracerPool.Enqueue(lr);
            }

            // Impact Particles Pool
            for (int i = 0; i < 12; i++)
            {
                GameObject impactObj = new GameObject($"Impact_{i}");
                impactObj.transform.SetParent(transform);

                // Small spark quad
                GameObject sparkQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sparkQuad.transform.SetParent(impactObj.transform, false);
                sparkQuad.transform.localScale = Vector3.one * 0.25f;
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                m.color = new Color(1f, 0.8f, 0.3f);
                sparkQuad.GetComponent<Renderer>().material = m;
                Destroy(sparkQuad.GetComponent<Collider>());

                impactObj.SetActive(false);
                _impactPool.Enqueue(impactObj);
            }
        }

        private void SpawnBulletTracer(Vector3 start, Vector3 end)
        {
            if (_tracerPool.Count == 0) return;

            LineRenderer lr = _tracerPool.Dequeue();
            _tracerPool.Enqueue(lr);

            StopCoroutine(nameof(AnimateTracer));
            StartCoroutine(AnimateTracer(lr, start, end));
        }

        private IEnumerator AnimateTracer(LineRenderer lr, Vector3 start, Vector3 end)
        {
            lr.enabled = true;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            yield return new WaitForSeconds(0.04f);

            lr.enabled = false;
        }

        private void SpawnImpactEffect(Vector3 pos, Vector3 normal, bool isEnemy)
        {
            if (_impactPool.Count == 0) return;

            GameObject impact = _impactPool.Dequeue();
            _impactPool.Enqueue(impact);

            impact.transform.position = pos + normal * 0.02f;
            impact.transform.rotation = Quaternion.LookRotation(normal);

            // Change color: Red for enemy, Yellow/Grey for concrete
            Renderer rend = impact.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = isEnemy ? new Color(1f, 0.15f, 0.25f) : new Color(1f, 0.85f, 0.4f);
            }

            impact.SetActive(true);
            StartCoroutine(DisableAfterSeconds(impact, 0.15f));
        }

        private IEnumerator DisableAfterSeconds(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) obj.SetActive(false);
        }

        #endregion

        #region Procedural M4A1 3D Model Generation

        private void EnsureRifleModel()
        {
            if (transform.Find("RifleModel") != null)
            {
                _rifleModel = transform.Find("RifleModel");
                _muzzleTransform = _rifleModel.Find("Muzzle");
                return;
            }

            GameObject modelObj = new GameObject("RifleModel");
            modelObj.transform.SetParent(transform, false);
            _rifleModel = modelObj.transform;

            Material gunMetal = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            gunMetal.color = new Color(0.12f, 0.14f, 0.16f); // Matte gunmetal

            Material carbonDark = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            carbonDark.color = new Color(0.06f, 0.07f, 0.09f);

            Material sightGlass = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            sightGlass.color = new Color(0f, 0.9f, 1f, 0.8f);

            // Receiver Body
            GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Receiver";
            receiver.transform.SetParent(_rifleModel, false);
            receiver.transform.localPosition = new Vector3(0f, 0.04f, 0.05f);
            receiver.transform.localScale = new Vector3(0.06f, 0.09f, 0.32f);
            receiver.GetComponent<Renderer>().material = gunMetal;
            Destroy(receiver.GetComponent<Collider>());

            // Barrel
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(_rifleModel, false);
            barrel.transform.localPosition = new Vector3(0f, 0.05f, 0.32f);
            barrel.transform.localScale = new Vector3(0.025f, 0.14f, 0.025f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.GetComponent<Renderer>().material = carbonDark;
            Destroy(barrel.GetComponent<Collider>());

            // Handguard
            GameObject handguard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handguard.name = "Handguard";
            handguard.transform.SetParent(_rifleModel, false);
            handguard.transform.localPosition = new Vector3(0f, 0.04f, 0.24f);
            handguard.transform.localScale = new Vector3(0.055f, 0.075f, 0.18f);
            handguard.GetComponent<Renderer>().material = carbonDark;
            Destroy(handguard.GetComponent<Collider>());

            // Magazine
            GameObject mag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mag.name = "Magazine";
            mag.transform.SetParent(_rifleModel, false);
            mag.transform.localPosition = new Vector3(0f, -0.08f, 0.1f);
            mag.transform.localScale = new Vector3(0.04f, 0.14f, 0.07f);
            mag.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            mag.GetComponent<Renderer>().material = carbonDark;
            Destroy(mag.GetComponent<Collider>());

            // Stock
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(_rifleModel, false);
            stock.transform.localPosition = new Vector3(0f, 0.02f, -0.2f);
            stock.transform.localScale = new Vector3(0.045f, 0.11f, 0.18f);
            stock.GetComponent<Renderer>().material = carbonDark;
            Destroy(stock.GetComponent<Collider>());

            // Red Dot Sight
            GameObject sight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sight.name = "Sight";
            sight.transform.SetParent(_rifleModel, false);
            sight.transform.localPosition = new Vector3(0f, 0.11f, 0.05f);
            sight.transform.localScale = new Vector3(0.04f, 0.05f, 0.09f);
            sight.GetComponent<Renderer>().material = sightGlass;
            Destroy(sight.GetComponent<Collider>());

            // Muzzle Tip
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(_rifleModel, false);
            muzzleObj.transform.localPosition = new Vector3(0f, 0.05f, 0.48f);
            _muzzleTransform = muzzleObj.transform;

            // Muzzle Light
            _muzzleLight = muzzleObj.AddComponent<Light>();
            _muzzleLight.type = LightType.Point;
            _muzzleLight.range = 6f;
            _muzzleLight.intensity = 3.5f;
            _muzzleLight.color = new Color(1f, 0.85f, 0.4f);
            _muzzleLight.enabled = false;

            // Muzzle Flash Mesh
            _muzzleFlashMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _muzzleFlashMesh.transform.SetParent(muzzleObj.transform, false);
            _muzzleFlashMesh.transform.localScale = new Vector3(0.12f, 0.12f, 0.2f);
            Material flashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            flashMat.color = new Color(1f, 0.9f, 0.5f);
            _muzzleFlashMesh.GetComponent<Renderer>().material = flashMat;
            Destroy(_muzzleFlashMesh.GetComponent<Collider>());
            _muzzleFlashMesh.SetActive(false);
        }

        #endregion
    }
}
