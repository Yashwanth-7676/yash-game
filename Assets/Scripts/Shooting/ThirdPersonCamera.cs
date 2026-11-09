using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Over-the-shoulder third-person camera controller with collision detection,
    /// smooth damping, spherical look rotation, and Aim-Down-Sights (ADS) zoom transitions.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Target Tracking")]
        [SerializeField, Tooltip("Target transform to follow (the player).")]
        private Transform _target;

        [SerializeField, Tooltip("Height offset above target root to look at.")]
        private float _targetHeight = 1.6f;

        [Header("Camera Offsets")]
        [SerializeField, Tooltip("Default over-the-shoulder camera offset.")]
        private Vector3 _normalOffset = new Vector3(0.55f, 0.2f, -3.0f);

        [SerializeField, Tooltip("Camera offset when aiming down sights.")]
        private Vector3 _aimOffset = new Vector3(0.40f, 0.1f, -1.6f);

        [Header("Rotation & Pitch Limits")]
        [SerializeField, Tooltip("Minimum vertical pitch angle in degrees.")]
        private float _minPitch = -40f;

        [SerializeField, Tooltip("Maximum vertical pitch angle in degrees.")]
        private float _maxPitch = 70f;

        [Header("Smooth Damping")]
        [SerializeField, Tooltip("Smooth time for camera position tracking.")]
        private float _positionSmoothTime = 0.04f;

        [SerializeField, Tooltip("Smooth time for ADS transitions.")]
        private float _zoomTransitionSpeed = 12f;

        [Header("Obstacle Collision")]
        [SerializeField, Tooltip("Sphere radius for obstacle collision testing.")]
        private float _collisionRadius = 0.25f;

        [SerializeField, Tooltip("Layers that block camera line of sight.")]
        private LayerMask _collisionLayers = ~0; // default everything except player

        #endregion

        #region Public Properties

        public Camera UnityCamera { get; private set; }
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        #endregion

        #region Private Fields

        private float _yaw;
        private float _pitch;
        private Vector3 _currentOffset;
        private Vector3 _smoothVelocity;
        private MobileInputController _input;
        private PlayerMovement _playerMovement;
        private float _targetFov = 60f;
        private const float NORMAL_FOV = 60f;
        private const float AIM_FOV = 40f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            UnityCamera = GetComponent<Camera>();
            if (UnityCamera == null)
            {
                UnityCamera = gameObject.AddComponent<Camera>();
            }

            _currentOffset = _normalOffset;
            _collisionLayers &= ~(1 << 8); // Ignore Player layer if layer 8
        }

        private void Start()
        {
            _input = MobileInputController.Instance;
            if (_target == null)
            {
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    SetTarget(pm.transform);
                }
            }

            // Initialize angles from current rotation
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_input == null)
            {
                _input = MobileInputController.Instance;
                if (_input == null) return;
            }

            HandleLookRotation();
            UpdateCameraPosition();
            UpdateFOV();
        }

        #endregion

        #region Camera Positioning & Collision

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            _playerMovement = _target.GetComponent<PlayerMovement>();
        }

        /// <summary>
        /// Applies procedural vertical recoil kick to camera pitch.
        /// </summary>
        public void ApplyRecoilKick(float verticalKick)
        {
            _pitch -= verticalKick;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void HandleLookRotation()
        {
            Vector2 lookInput = _input.LookInput;

            _yaw += lookInput.x;
            _pitch -= lookInput.y;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            // Wrap yaw
            if (_yaw > 360f) _yaw -= 360f;
            else if (_yaw < -360f) _yaw += 360f;
        }

        private void UpdateCameraPosition()
        {
            // Transition offset based on Aiming state
            bool isAiming = _playerMovement != null ? _playerMovement.IsAiming : _input.IsAiming;
            Vector3 desiredLocalOffset = isAiming ? _aimOffset : _normalOffset;
            _currentOffset = Vector3.Lerp(_currentOffset, desiredLocalOffset, Time.deltaTime * _zoomTransitionSpeed);

            // Pivot point on player
            Vector3 pivotPoint = _target.position + Vector3.up * _targetHeight;

            // Compute ideal camera position in world space
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 idealPosition = pivotPoint + rotation * _currentOffset;

            // Camera collision raycast to prevent wall clipping
            Vector3 direction = (idealPosition - pivotPoint);
            float maxDistance = direction.magnitude;

            if (maxDistance > 0.001f)
            {
                direction.Normalize();

                if (Physics.SphereCast(pivotPoint, _collisionRadius, direction, out RaycastHit hit, maxDistance, _collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    // Push camera forward slightly in front of hit surface
                    float safeDistance = Mathf.Max(0.2f, hit.distance - 0.1f);
                    idealPosition = pivotPoint + direction * safeDistance;
                }
            }

            // Smooth position update
            transform.position = Vector3.SmoothDamp(transform.position, idealPosition, ref _smoothVelocity, _positionSmoothTime);
            transform.rotation = rotation;
        }

        private void UpdateFOV()
        {
            if (UnityCamera == null) return;

            bool isAiming = _playerMovement != null ? _playerMovement.IsAiming : _input.IsAiming;
            _targetFov = isAiming ? AIM_FOV : NORMAL_FOV;

            UnityCamera.fieldOfView = Mathf.Lerp(UnityCamera.fieldOfView, _targetFov, Time.deltaTime * _zoomTransitionSpeed);
        }

        #endregion
    }
}
