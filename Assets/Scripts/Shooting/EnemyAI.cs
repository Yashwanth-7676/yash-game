using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YashGame.Shooting
{
    public enum EnemyType
    {
        Soldier,
        HeavySoldier,
        FastSoldier
    }

    /// <summary>
    /// Advanced Enemy AI controlling the 3 enemy archetypes (Soldier, Heavy Soldier, Fast Soldier).
    /// Features floating billboard health bars, patrol & combat states, directional damage reporting,
    /// defeat explosions for heavy units, and persistent coin rewards.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Enemy Archetype")]
        [SerializeField] private EnemyType _enemyType = EnemyType.Soldier;
        [SerializeField] private string _enemyDisplayName = "Combat Sentinel";

        [Header("Stats Override")]
        [SerializeField] private float _moveSpeed = 3.2f;
        [SerializeField] private float _attackDamage = 8f;
        [SerializeField] private float _attackInterval = 1.6f;
        [SerializeField] private float _attackRange = 12f;
        [SerializeField] private float _detectionRadius = 22f;
        [SerializeField] private int _coinReward = 15;

        [Header("Navigation")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _turnSpeed = 260f;

        #endregion

        #region Public Properties

        public EnemyType Type => _enemyType;
        public string DisplayName => _enemyDisplayName;
        public int CoinReward => _coinReward;

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private EnemyHealth _health;
        private Transform _playerTransform;
        private PlayerHealth _playerHealth;

        private int _currentWpIndex = 0;
        private float _nextAttackTime;
        private float _walkCycleTimer;

        // Floating Health Bar
        private GameObject _healthBarCanvas;
        private Slider _hpSlider;
        private Camera _mainCamera;

        // Visuals
        private Transform _modelRoot;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Light _muzzleLight;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _characterController.height = 2.0f;
            _characterController.radius = 0.5f;
            _characterController.center = new Vector3(0f, 1.0f, 0f);

            _health = GetComponent<EnemyHealth>();
            _mainCamera = Camera.main;

            ApplyArchetypeStats();
            BuildProceduralChassis();
            CreateFloatingHealthBar();
        }

        private void Start()
        {
            FindPlayer();

            if (_health != null)
            {
                _health.OnHealthChanged += HandleHealthChanged;
                _health.OnDied += HandleDeath;
            }
        }

        private void Update()
        {
            if (_health != null && !_health.IsAlive) return;

            if (_playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distToPlayer <= _detectionRadius && _playerHealth != null && _playerHealth.IsAlive)
            {
                EngagePlayer(distToPlayer);
            }
            else
            {
                Patrol();
            }

            ApplyGravity();
            AnimateLimbOscillation();
            UpdateHealthBarBillboard();
        }

        #endregion

        #region Archetype Configuration

        public void ConfigureArchetype(EnemyType type)
        {
            _enemyType = type;
            ApplyArchetypeStats();
        }

        private void ApplyArchetypeStats()
        {
            float baseHp = 100f;

            switch (_enemyType)
            {
                case EnemyType.Soldier:
                    _enemyDisplayName = "Tactical Soldier";
                    _moveSpeed = 3.2f;
                    _attackDamage = 8f;
                    _attackInterval = 1.6f;
                    _attackRange = 12f;
                    _detectionRadius = 22f;
                    _coinReward = 15;
                    baseHp = 100f;
                    break;

                case EnemyType.HeavySoldier:
                    _enemyDisplayName = "Heavy Juggernaut";
                    _moveSpeed = 2.0f; // Slower
                    _attackDamage = 18f; // Heavy hits
                    _attackInterval = 2.2f;
                    _attackRange = 14f;
                    _detectionRadius = 24f;
                    _coinReward = 35;
                    baseHp = 180f;
                    break;

                case EnemyType.FastSoldier:
                    _enemyDisplayName = "Fast Skirmisher";
                    _moveSpeed = 5.2f; // Fast flanker
                    _attackDamage = 6f;
                    _attackInterval = 0.95f; // Rapid fire
                    _attackRange = 10f;
                    _detectionRadius = 26f;
                    _coinReward = 20;
                    baseHp = 60f;
                    break;
            }

            // Apply difficulty multipliers if DifficultyManager is present
            if (DifficultyManager.Instance != null)
            {
                _attackDamage *= DifficultyManager.Instance.EnemyDamageMultiplier;
                baseHp *= DifficultyManager.Instance.EnemyHealthMultiplier;
            }

            if (_health != null)
            {
                _health.SetMaxHealth(baseHp);
            }
        }

        #endregion

        #region AI Logic

        private void FindPlayer()
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                _playerTransform = pm.transform;
                _playerHealth = pm.GetComponent<PlayerHealth>();
            }
        }

        private void Patrol()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;

            Transform targetWp = _waypoints[_currentWpIndex];
            if (targetWp == null) return;

            Vector3 toWp = targetWp.position - transform.position;
            toWp.y = 0f;

            if (toWp.sqrMagnitude < 1.8f)
            {
                _currentWpIndex = (_currentWpIndex + 1) % _waypoints.Length;
                return;
            }

            Quaternion targetRot = Quaternion.LookRotation(toWp.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * Time.deltaTime);

            Vector3 move = transform.forward * _moveSpeed * Time.deltaTime;
            _characterController.Move(move);
        }

        private void EngagePlayer(float distToPlayer)
        {
            Vector3 toPlayer = _playerTransform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * 1.5f * Time.deltaTime);
            }

            // Move into attack range
            if (distToPlayer > _attackRange)
            {
                Vector3 move = transform.forward * _moveSpeed * 1.15f * Time.deltaTime;
                _characterController.Move(move);
            }

            // Attack on interval
            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + _attackInterval;
                ExecuteAttack();
            }
        }

        private void ExecuteAttack()
        {
            if (_playerHealth == null || !_playerHealth.IsAlive) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = _playerTransform.position + Vector3.up * 1.2f;
            Vector3 dir = (targetPos - eyePos).normalized;

            if (Physics.Raycast(eyePos, dir, out RaycastHit hit, _detectionRadius))
            {
                if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
                {
                    _playerHealth.TakeDamage(_attackDamage);

                    // Show directional damage indicator on player screen!
                    if (DamageIndicator.Instance != null)
                    {
                        DamageIndicator.Instance.ShowDamageFrom(transform.position);
                    }

                    // Screen shake on hit
                    if (CombatEffects.Instance != null)
                    {
                        CombatEffects.Instance.TriggerScreenShake(0.35f, 0.15f);
                    }

                    if (_muzzleLight != null)
                    {
                        StartCoroutine(MuzzleLightFlash());
                    }
                }
            }
        }

        private IEnumerator MuzzleLightFlash()
        {
            _muzzleLight.enabled = true;
            yield return new WaitForSeconds(0.06f);
            if (_muzzleLight != null) _muzzleLight.enabled = false;
        }

        private void ApplyGravity()
        {
            if (!_characterController.isGrounded)
            {
                _characterController.Move(Vector3.down * 15f * Time.deltaTime);
            }
        }

        public void SetWaypoints(Transform[] points)
        {
            _waypoints = points;
        }

        #endregion

        #region Death & Rewards

        private void HandleHealthChanged(float cur, float max)
        {
            if (_hpSlider != null)
            {
                _hpSlider.maxValue = max;
                _hpSlider.value = cur;
                _healthBarCanvas.SetActive(true);
            }
        }

        private void HandleDeath()
        {
            // Award Coins
            if (RewardManager.Instance != null)
            {
                RewardManager.Instance.AddCoins(_coinReward);
            }

            // If Heavy Soldier, trigger death explosion!
            if (_enemyType == EnemyType.HeavySoldier && CombatEffects.Instance != null)
            {
                CombatEffects.Instance.SpawnExplosion(transform.position + Vector3.up * 1.0f, 7f, 35f);
            }

            // Notify Mission Manager
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.RegisterEnemyDefeated(this);
            }

            if (_healthBarCanvas != null)
            {
                Destroy(_healthBarCanvas);
            }
        }

        #endregion

        #region Floating Health Bar

        private void CreateFloatingHealthBar()
        {
            _healthBarCanvas = new GameObject("FloatingHPBar", typeof(RectTransform), typeof(Canvas));
            _healthBarCanvas.transform.SetParent(transform, false);
            _healthBarCanvas.transform.localPosition = new Vector3(0f, 2.3f, 0f);

            Canvas c = _healthBarCanvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            RectTransform cRt = _healthBarCanvas.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(1.2f, 0.2f);
            cRt.localScale = Vector3.one * 0.8f;

            // Background
            GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_healthBarCanvas.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(1.2f, 0.2f);
            bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.75f);

            // Slider
            _hpSlider = bg.AddComponent<Slider>();

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bg.transform, false);
            RectTransform fRt = fill.GetComponent<RectTransform>();
            fRt.sizeDelta = new Vector2(1.15f, 0.16f);
            Image fImg = fill.GetComponent<Image>();
            fImg.color = (_enemyType == EnemyType.HeavySoldier) ? new Color(1f, 0.45f, 0.1f) : new Color(1f, 0.18f, 0.35f);

            _hpSlider.fillRect = fRt;
            _hpSlider.minValue = 0f;
            _hpSlider.maxValue = 100f;
            _hpSlider.value = 100f;
        }

        private void UpdateHealthBarBillboard()
        {
            if (_healthBarCanvas == null || !_healthBarCanvas.activeSelf) return;

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _healthBarCanvas.transform.rotation = Quaternion.LookRotation(_healthBarCanvas.transform.position - _mainCamera.transform.position);
            }
        }

        #endregion

        #region Procedural 3D Model

        private void AnimateLimbOscillation()
        {
            if (_leftLeg == null || _rightLeg == null) return;

            float speed = _characterController.velocity.magnitude;
            if (speed > 0.1f)
            {
                _walkCycleTimer += Time.deltaTime * speed * 3.4f;
                float swing = Mathf.Sin(_walkCycleTimer) * 30f;
                _leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
                _rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            }
            else
            {
                _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }

        private void BuildProceduralChassis()
        {
            GameObject model = new GameObject("BotChassis");
            model.transform.SetParent(transform, false);
            _modelRoot = model.transform;

            float scaleMult = 1.0f;
            Color mainColor = new Color(0.18f, 0.20f, 0.24f); // Steel
            Color accentColor = new Color(1f, 0.18f, 0.35f); // Crimson

            if (_enemyType == EnemyType.HeavySoldier)
            {
                scaleMult = 1.25f;
                mainColor = new Color(0.10f, 0.12f, 0.15f); // Heavy Obsidian
                accentColor = new Color(1f, 0.45f, 0.1f); // Molten Orange
            }
            else if (_enemyType == EnemyType.FastSoldier)
            {
                scaleMult = 0.85f;
                mainColor = new Color(0.24f, 0.12f, 0.18f); // Agile Reddish Steel
                accentColor = new Color(1f, 0.1f, 0.4f);
            }

            model.transform.localScale = Vector3.one * scaleMult;

            Material armorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = mainColor };
            Material eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = accentColor };
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", accentColor * 2.0f);

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(model.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.6f, 0.7f, 0.4f);
            torso.GetComponent<Renderer>().material = armorMat;
            Destroy(torso.GetComponent<Collider>());

            // Head & Eye
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(torso.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            head.GetComponent<Renderer>().material = armorMat;
            Destroy(head.GetComponent<Collider>());

            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.name = "Visor";
            eye.transform.SetParent(head.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.05f, 0.38f);
            eye.transform.localScale = new Vector3(0.65f, 0.22f, 0.35f);
            eye.GetComponent<Renderer>().material = eyeMat;
            Destroy(eye.GetComponent<Collider>());

            // Gun Arm
            GameObject gunArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gunArm.name = "GunArm";
            gunArm.transform.SetParent(torso.transform, false);
            gunArm.transform.localPosition = new Vector3(0.42f, 0.05f, 0.35f);
            gunArm.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            gunArm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gunArm.GetComponent<Renderer>().material = armorMat;
            Destroy(gunArm.GetComponent<Collider>());

            GameObject lightObj = new GameObject("MuzzleLight");
            lightObj.transform.SetParent(gunArm.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            _muzzleLight = lightObj.AddComponent<Light>();
            _muzzleLight.type = LightType.Point;
            _muzzleLight.color = accentColor;
            _muzzleLight.intensity = 3.5f;
            _muzzleLight.range = 5.5f;
            _muzzleLight.enabled = false;

            // Left Arm
            GameObject lArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lArm.name = "LeftArm";
            lArm.transform.SetParent(torso.transform, false);
            lArm.transform.localPosition = new Vector3(-0.4f, 0.05f, 0f);
            lArm.transform.localScale = new Vector3(0.18f, 0.55f, 0.2f);
            lArm.GetComponent<Renderer>().material = armorMat;
            Destroy(lArm.GetComponent<Collider>());

            // Legs
            GameObject lLegPivot = new GameObject("LeftLegPivot");
            lLegPivot.transform.SetParent(model.transform, false);
            lLegPivot.transform.localPosition = new Vector3(-0.18f, 0.7f, 0f);
            _leftLeg = lLegPivot.transform;
            GameObject lMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lMesh.transform.SetParent(lLegPivot.transform, false);
            lMesh.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            lMesh.transform.localScale = new Vector3(0.2f, 0.65f, 0.25f);
            lMesh.GetComponent<Renderer>().material = armorMat;
            Destroy(lMesh.GetComponent<Collider>());

            GameObject rLegPivot = new GameObject("RightLegPivot");
            rLegPivot.transform.SetParent(model.transform, false);
            rLegPivot.transform.localPosition = new Vector3(0.18f, 0.7f, 0f);
            _rightLeg = rLegPivot.transform;
            GameObject rMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rMesh.transform.SetParent(rLegPivot.transform, false);
            rMesh.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            rMesh.transform.localScale = new Vector3(0.2f, 0.65f, 0.25f);
            rMesh.GetComponent<Renderer>().material = armorMat;
            Destroy(rMesh.GetComponent<Collider>());
        }

        #endregion
    }
}
