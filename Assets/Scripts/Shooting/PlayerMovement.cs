using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Third-person player movement controller using CharacterController.
    /// Handles walking, running, jumping, camera-relative movement vectors,
    /// smooth rotation towards move direction or camera forward (when aiming),
    /// and procedural limb animations for mobile efficiency.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Movement Speeds")]
        [SerializeField, Tooltip("Standard walking speed in m/s.")]
        private float _walkSpeed = 4.5f;

        [SerializeField, Tooltip("Running speed when moving forward.")]
        private float _runSpeed = 7.5f;

        [SerializeField, Tooltip("Movement speed while aiming down sights.")]
        private float _aimSpeed = 2.8f;

        [SerializeField, Tooltip("Rate of acceleration/deceleration.")]
        private float _acceleration = 12f;

        [SerializeField, Tooltip("Character turn speed in degrees per second.")]
        private float _rotationSpeed = 720f;

        [Header("Jumping & Gravity")]
        [SerializeField, Tooltip("Jump height in meters.")]
        private float _jumpHeight = 1.4f;

        [SerializeField, Tooltip("Gravity acceleration.")]
        private float _gravity = 20.0f;

        [Header("References")]
        [SerializeField, Tooltip("Reference to the main game camera.")]
        private Transform _cameraTransform;

        [SerializeField, Tooltip("Weapon attachment transform socket.")]
        private Transform _weaponSocket;

        #endregion

        #region Public Properties

        public bool IsGrounded => _characterController.isGrounded;
        public bool IsAiming { get; private set; }
        public Vector3 Velocity => _characterController.velocity;
        public float CurrentSpeedRatio => _currentSpeed / _runSpeed;
        public Transform WeaponSocket => _weaponSocket;

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private MobileInputController _input;
        private float _currentSpeed;
        private float _verticalVelocity;

        // Procedural Animation Parts
        private Transform _modelRoot;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _torso;
        private float _walkCycleTimer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _characterController.center = new Vector3(0f, 1.0f, 0f);
            _characterController.height = 2.0f;
            _characterController.radius = 0.45f;
            _characterController.stepOffset = 0.35f;

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            EnsureProceduralModel();
        }

        private void Start()
        {
            _input = MobileInputController.Instance;
            if (_input == null)
            {
                _input = FindObjectOfType<MobileInputController>();
            }
        }

        private void Update()
        {
            if (_input == null)
            {
                _input = MobileInputController.Instance;
                if (_input == null) return;
            }

            IsAiming = _input.IsAiming;

            HandleMovement();
            HandleRotation();
            AnimateProceduralLimbs();
        }

        #endregion

        #region Movement & Physics

        private void HandleMovement()
        {
            Vector2 inputDir = _input.MoveInput;
            bool hasInput = inputDir.sqrMagnitude > 0.01f;

            // Target speed based on state
            float targetSpeed = _walkSpeed;
            if (IsAiming)
            {
                targetSpeed = _aimSpeed;
            }
            else if (hasInput && inputDir.y > 0.5f && Mathf.Abs(inputDir.x) < 0.5f)
            {
                targetSpeed = _runSpeed;
            }

            if (!hasInput)
            {
                targetSpeed = 0f;
            }

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);

            // Calculate camera-relative movement direction
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (_cameraTransform != null)
            {
                forward = _cameraTransform.forward;
                right = _cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            Vector3 desiredMoveDir = (forward * inputDir.y + right * inputDir.x);
            if (desiredMoveDir.sqrMagnitude > 1f)
            {
                desiredMoveDir.Normalize();
            }

            // Ground check & Jumping
            if (_characterController.isGrounded)
            {
                // Small downward force to stay sticked to slopes
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2.0f;
                }

                if (_input.JumpTriggered)
                {
                    _verticalVelocity = Mathf.Sqrt(2f * _jumpHeight * _gravity);
                }
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            // Move the character
            Vector3 horizontalVelocity = desiredMoveDir * _currentSpeed;
            Vector3 finalMovement = (horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;

            _characterController.Move(finalMovement);
        }

        private void HandleRotation()
        {
            if (IsAiming)
            {
                // When aiming, face camera forward direction directly for precise crosshair alignment
                if (_cameraTransform != null)
                {
                    Vector3 camForward = _cameraTransform.forward;
                    camForward.y = 0f;
                    if (camForward.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(camForward);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                // When not aiming, rotate smoothly toward movement vector
                Vector2 inputDir = _input.MoveInput;
                if (inputDir.sqrMagnitude > 0.01f && _cameraTransform != null)
                {
                    Vector3 forward = _cameraTransform.forward;
                    Vector3 right = _cameraTransform.right;
                    forward.y = 0f;
                    right.y = 0f;
                    forward.Normalize();
                    right.Normalize();

                    Vector3 moveDir = forward * inputDir.y + right * inputDir.x;
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(moveDir);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
                    }
                }
            }
        }

        #endregion

        #region Procedural Visuals & Animation

        private void AnimateProceduralLimbs()
        {
            if (_leftLeg == null || _rightLeg == null) return;

            float speedRatio = _currentSpeed / _runSpeed;

            if (speedRatio > 0.05f && _characterController.isGrounded)
            {
                _walkCycleTimer += Time.deltaTime * (_currentSpeed * 2.8f);

                float legSwing = Mathf.Sin(_walkCycleTimer) * 32f * speedRatio;
                _leftLeg.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
                _rightLeg.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);

                // Arm swing (only when not aiming)
                if (!IsAiming && _leftArm != null && _rightArm != null)
                {
                    float armSwing = Mathf.Cos(_walkCycleTimer) * 25f * speedRatio;
                    _leftArm.localRotation = Quaternion.Euler(-armSwing + 15f, 0f, 0f);
                    _rightArm.localRotation = Quaternion.Euler(armSwing + 25f, 0f, 0f);
                }

                // Subtle torso bounce
                if (_torso != null)
                {
                    float bob = Mathf.Abs(Mathf.Sin(_walkCycleTimer * 2f)) * 0.04f;
                    _torso.localPosition = new Vector3(0f, 1.0f + bob, 0f);
                }
            }
            else
            {
                // Return to idle stance smoothly
                _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation, Quaternion.identity, Time.deltaTime * 10f);
                _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, Quaternion.identity, Time.deltaTime * 10f);

                if (_torso != null)
                {
                    _torso.localPosition = Vector3.Lerp(_torso.localPosition, new Vector3(0f, 1.0f, 0f), Time.deltaTime * 10f);
                }
            }

            // Aiming pose for arms
            if (IsAiming && _rightArm != null && _leftArm != null)
            {
                _rightArm.localRotation = Quaternion.Euler(-65f, -15f, 0f);
                _leftArm.localRotation = Quaternion.Euler(-55f, 35f, 0f);
            }
        }

        private void EnsureProceduralModel()
        {
            if (transform.Find("ModelRoot") != null)
            {
                _modelRoot = transform.Find("ModelRoot");
                return;
            }

            // Create Model Root
            GameObject modelRootObj = new GameObject("ModelRoot");
            modelRootObj.transform.SetParent(transform, false);
            _modelRoot = modelRootObj.transform;

            // Shared Tactical Materials
            Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bodyMat.color = new Color(0.12f, 0.16f, 0.24f); // Tactical navy

            Material armorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            armorMat.color = new Color(0.05f, 0.07f, 0.11f); // Dark carbon armor

            Material visorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            visorMat.color = new Color(0f, 0.85f, 1f); // Glowing cyan visor
            visorMat.EnableKeyword("_EMISSION");
            visorMat.SetColor("_EmissionColor", new Color(0f, 0.85f, 1f) * 1.5f);

            // Torso (Chest + Tactical Vest)
            GameObject torsoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torsoObj.name = "Torso";
            torsoObj.transform.SetParent(_modelRoot, false);
            torsoObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torsoObj.transform.localScale = new Vector3(0.55f, 0.65f, 0.35f);
            torsoObj.GetComponent<Renderer>().material = bodyMat;
            Destroy(torsoObj.GetComponent<Collider>());
            _torso = torsoObj.transform;

            // Tactical Vest Plate
            GameObject vestObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vestObj.name = "TacticalVest";
            vestObj.transform.SetParent(torsoObj.transform, false);
            vestObj.transform.localPosition = new Vector3(0f, 0.05f, 0.1f);
            vestObj.transform.localScale = new Vector3(0.9f, 0.8f, 1.05f);
            vestObj.GetComponent<Renderer>().material = armorMat;
            Destroy(vestObj.GetComponent<Collider>());

            // Head & Helmet
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "Head";
            headObj.transform.SetParent(torsoObj.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            headObj.transform.localScale = new Vector3(0.6f, 0.5f, 0.65f);
            headObj.GetComponent<Renderer>().material = armorMat;
            Destroy(headObj.GetComponent<Collider>());

            // Visor (Cyan Glow)
            GameObject visorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visorObj.name = "Visor";
            visorObj.transform.SetParent(headObj.transform, false);
            visorObj.transform.localPosition = new Vector3(0f, 0.02f, 0.36f);
            visorObj.transform.localScale = new Vector3(0.6f, 0.18f, 0.3f);
            visorObj.GetComponent<Renderer>().material = visorMat;
            Destroy(visorObj.GetComponent<Collider>());

            // Left Arm
            GameObject lArmObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lArmObj.name = "LeftArm";
            lArmObj.transform.SetParent(torsoObj.transform, false);
            lArmObj.transform.localPosition = new Vector3(-0.38f, 0.1f, 0.1f);
            lArmObj.transform.localScale = new Vector3(0.24f, 0.45f, 0.24f);
            lArmObj.transform.localRotation = Quaternion.Euler(20f, 0f, -10f);
            lArmObj.GetComponent<Renderer>().material = bodyMat;
            Destroy(lArmObj.GetComponent<Collider>());
            _leftArm = lArmObj.transform;

            // Right Arm (Weapon Hand)
            GameObject rArmObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rArmObj.name = "RightArm";
            rArmObj.transform.SetParent(torsoObj.transform, false);
            rArmObj.transform.localPosition = new Vector3(0.38f, 0.1f, 0.1f);
            rArmObj.transform.localScale = new Vector3(0.24f, 0.45f, 0.24f);
            rArmObj.transform.localRotation = Quaternion.Euler(25f, 0f, 10f);
            rArmObj.GetComponent<Renderer>().material = bodyMat;
            Destroy(rArmObj.GetComponent<Collider>());
            _rightArm = rArmObj.transform;

            // Weapon Socket
            GameObject socketObj = new GameObject("WeaponSocket");
            socketObj.transform.SetParent(rArmObj.transform, false);
            socketObj.transform.localPosition = new Vector3(0f, -0.35f, 0.2f);
            socketObj.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            _weaponSocket = socketObj.transform;

            // Left Leg
            GameObject lLegPivot = new GameObject("LeftLegPivot");
            lLegPivot.transform.SetParent(_modelRoot, false);
            lLegPivot.transform.localPosition = new Vector3(-0.16f, 0.75f, 0f);
            _leftLeg = lLegPivot.transform;

            GameObject lLegMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lLegMesh.transform.SetParent(lLegPivot.transform, false);
            lLegMesh.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            lLegMesh.transform.localScale = new Vector3(0.22f, 0.45f, 0.22f);
            lLegMesh.GetComponent<Renderer>().material = armorMat;
            Destroy(lLegMesh.GetComponent<Collider>());

            // Right Leg
            GameObject rLegPivot = new GameObject("RightLegPivot");
            rLegPivot.transform.SetParent(_modelRoot, false);
            rLegPivot.transform.localPosition = new Vector3(0.16f, 0.75f, 0f);
            _rightLeg = rLegPivot.transform;

            GameObject rLegMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rLegMesh.transform.SetParent(rLegPivot.transform, false);
            rLegMesh.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            rLegMesh.transform.localScale = new Vector3(0.22f, 0.45f, 0.22f);
            rLegMesh.GetComponent<Renderer>().material = armorMat;
            Destroy(rLegMesh.GetComponent<Collider>());
        }

        #endregion
    }
}
