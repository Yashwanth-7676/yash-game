using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Autonomous enemy controller for test targets on the battlefield.
    /// Handles waypoint patrolling, player detection, turning to face player,
    /// and periodic burst attacks when in range.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyController : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Movement & Navigation")]
        [SerializeField, Tooltip("Patrol movement speed.")]
        private float _patrolSpeed = 2.4f;

        [SerializeField, Tooltip("Chase movement speed.")]
        private float _chaseSpeed = 3.6f;

        [SerializeField, Tooltip("Rotation turn speed in deg/s.")]
        private float _turnSpeed = 240f;

        [SerializeField, Tooltip("Waypoints for patrolling.")]
        private Transform[] _waypoints;

        [Header("Combat & Detection")]
        [SerializeField, Tooltip("Distance at which enemy notices player.")]
        private float _detectionRadius = 20f;

        [SerializeField, Tooltip("Distance at which enemy stops chasing and starts shooting.")]
        private float _attackRange = 12f;

        [SerializeField, Tooltip("Delay in seconds between shots.")]
        private float _attackInterval = 1.6f;

        [SerializeField, Tooltip("Damage dealt per enemy shot.")]
        private float _attackDamage = 8f;

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private EnemyHealth _health;
        private Transform _playerTransform;
        private PlayerHealth _playerHealth;

        private int _currentWaypointIndex = 0;
        private float _nextAttackTime;
        private float _walkCycleTimer;

        // Procedural Visuals
        private Transform _modelRoot;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _gunArm;
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

            EnsureProceduralBotModel();
        }

        private void Start()
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                _playerTransform = pm.transform;
                _playerHealth = pm.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            if (_health != null && !_health.IsAlive) return;

            if (_playerTransform == null)
            {
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    _playerTransform = pm.transform;
                    _playerHealth = pm.GetComponent<PlayerHealth>();
                }
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distToPlayer <= _detectionRadius && _playerHealth != null && _playerHealth.IsAlive)
            {
                EngagePlayer(distToPlayer);
            }
            else
            {
                PatrolBehavior();
            }

            ApplyGravity();
            AnimateProceduralBot();
        }

        #endregion

        #region Behavior Modes

        private void PatrolBehavior()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;

            Transform targetWp = _waypoints[_currentWaypointIndex];
            if (targetWp == null) return;

            Vector3 toWp = targetWp.position - transform.position;
            toWp.y = 0f;

            if (toWp.sqrMagnitude < 1.5f)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                return;
            }

            // Rotate toward waypoint
            Quaternion targetRot = Quaternion.LookRotation(toWp.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * Time.deltaTime);

            // Move forward
            Vector3 move = transform.forward * _patrolSpeed * Time.deltaTime;
            _characterController.Move(move);
        }

        private void EngagePlayer(float distToPlayer)
        {
            Vector3 toPlayer = _playerTransform.position - transform.position;
            toPlayer.y = 0f;

            // Rotate toward player
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * 1.5f * Time.deltaTime);
            }

            // Move closer if further than attack range
            if (distToPlayer > _attackRange)
            {
                Vector3 move = transform.forward * _chaseSpeed * Time.deltaTime;
                _characterController.Move(move);
            }

            // Attack player if line of sight clear
            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + _attackInterval;
                TryAttackPlayer();
            }
        }

        private void TryAttackPlayer()
        {
            if (_playerHealth == null || !_playerHealth.IsAlive) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = _playerTransform.position + Vector3.up * 1.2f;
            Vector3 dir = (targetPos - eyePos).normalized;

            // Simple raycast to check if player is visible
            if (Physics.Raycast(eyePos, dir, out RaycastHit hit, _detectionRadius))
            {
                if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
                {
                    _playerHealth.TakeDamage(_attackDamage);

                    // Muzzle flash on enemy
                    if (_muzzleLight != null)
                    {
                        StartCoroutine(FlashMuzzleRoutine());
                    }
                }
            }
        }

        private IEnumerator FlashMuzzleRoutine()
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

        #region Procedural Visuals

        private void AnimateProceduralBot()
        {
            if (_leftLeg == null || _rightLeg == null) return;

            float speed = _characterController.velocity.magnitude;
            if (speed > 0.1f)
            {
                _walkCycleTimer += Time.deltaTime * speed * 3.2f;
                float swing = Mathf.Sin(_walkCycleTimer) * 28f;
                _leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
                _rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            }
            else
            {
                _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }

        private void EnsureProceduralBotModel()
        {
            if (transform.Find("BotModel") != null)
            {
                _modelRoot = transform.Find("BotModel");
                return;
            }

            GameObject modelObj = new GameObject("BotModel");
            modelObj.transform.SetParent(transform, false);
            _modelRoot = modelObj.transform;

            Material armorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            armorMat.color = new Color(0.18f, 0.2f, 0.24f); // Dark tactical steel

            Material eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            eyeMat.color = new Color(1f, 0.18f, 0.35f); // Threat Crimson
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", new Color(1f, 0.18f, 0.35f) * 2.0f);

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "BotTorso";
            torso.transform.SetParent(_modelRoot, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.6f, 0.7f, 0.4f);
            torso.GetComponent<Renderer>().material = armorMat;
            Destroy(torso.GetComponent<Collider>());

            // Head with red eye visor
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "BotHead";
            head.transform.SetParent(torso.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            head.GetComponent<Renderer>().material = armorMat;
            Destroy(head.GetComponent<Collider>());

            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.name = "RedEyeVisor";
            eye.transform.SetParent(head.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.05f, 0.38f);
            eye.transform.localScale = new Vector3(0.65f, 0.22f, 0.35f);
            eye.GetComponent<Renderer>().material = eyeMat;
            Destroy(eye.GetComponent<Collider>());

            // Gun Arm (Right)
            GameObject gunArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gunArm.name = "GunArm";
            gunArm.transform.SetParent(torso.transform, false);
            gunArm.transform.localPosition = new Vector3(0.42f, 0.05f, 0.35f);
            gunArm.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            gunArm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gunArm.GetComponent<Renderer>().material = armorMat;
            Destroy(gunArm.GetComponent<Collider>());
            _gunArm = gunArm.transform;

            // Gun Muzzle Light
            GameObject lightObj = new GameObject("MuzzleLight");
            lightObj.transform.SetParent(gunArm.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            _muzzleLight = lightObj.AddComponent<Light>();
            _muzzleLight.type = LightType.Point;
            _muzzleLight.color = new Color(1f, 0.2f, 0.3f);
            _muzzleLight.intensity = 3f;
            _muzzleLight.range = 5f;
            _muzzleLight.enabled = false;

            // Left Arm
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(torso.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.4f, 0.05f, 0f);
            leftArm.transform.localScale = new Vector3(0.18f, 0.55f, 0.2f);
            leftArm.GetComponent<Renderer>().material = armorMat;
            Destroy(leftArm.GetComponent<Collider>());

            // Left Leg
            GameObject lLeg = new GameObject("LeftLeg");
            lLeg.transform.SetParent(_modelRoot, false);
            lLeg.transform.localPosition = new Vector3(-0.18f, 0.7f, 0f);
            _leftLeg = lLeg.transform;

            GameObject lMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lMesh.transform.SetParent(lLeg.transform, false);
            lMesh.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            lMesh.transform.localScale = new Vector3(0.2f, 0.65f, 0.25f);
            lMesh.GetComponent<Renderer>().material = armorMat;
            Destroy(lMesh.GetComponent<Collider>());

            // Right Leg
            GameObject rLeg = new GameObject("RightLeg");
            rLeg.transform.SetParent(_modelRoot, false);
            rLeg.transform.localPosition = new Vector3(0.18f, 0.7f, 0f);
            _rightLeg = rLeg.transform;

            GameObject rMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rMesh.transform.SetParent(rLeg.transform, false);
            rMesh.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            rMesh.transform.localScale = new Vector3(0.2f, 0.65f, 0.25f);
            rMesh.GetComponent<Renderer>().material = armorMat;
            Destroy(rMesh.GetComponent<Collider>());
        }

        #endregion
    }
}
