using System;
using UnityEngine;
using UnityEngine.EventSystems;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Dual input controller for mobile touch controls and Unity Editor PC controls.
    /// Handles left virtual joystick (movement), right touch surface (camera look),
    /// and action buttons (Fire, Aim, Jump, Reload, Pause).
    /// </summary>
    public class MobileInputController : MonoBehaviour
    {
        #region Static Instance

        public static MobileInputController Instance { get; private set; }

        #endregion

        #region Input State Properties

        /// <summary>Horizontal and Vertical movement vector (-1 to +1).</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>Horizontal (yaw) and Vertical (pitch) camera look delta.</summary>
        public Vector2 LookInput { get; private set; }

        /// <summary>True on frame fire button is pressed or held down.</summary>
        public bool IsFirePressed { get; private set; }

        /// <summary>True if aiming down sights (toggle or hold).</summary>
        public bool IsAiming { get; private set; }

        /// <summary>True on frame jump button was triggered.</summary>
        public bool JumpTriggered { get; private set; }

        /// <summary>True on frame reload button was triggered.</summary>
        public bool ReloadTriggered { get; private set; }

        /// <summary>True on frame pause button was triggered.</summary>
        public bool PauseTriggered { get; private set; }

        #endregion

        #region Serialized Settings

        [Header("Sensitivity")]
        [SerializeField, Tooltip("Base camera touch sensitivity multiplier.")]
        private float _touchLookSensitivity = 0.15f;

        [SerializeField, Tooltip("Mouse sensitivity in Editor / PC mode.")]
        private float _mouseLookSensitivity = 2.0f;

        [SerializeField, Tooltip("Look smoothing factor.")]
        private float _lookSmoothing = 18f;

        #endregion

        #region Private Fields

        private Vector2 _rawTouchLookDelta;
        private Vector2 _smoothedLookInput;
        private int _lookFingerId = -1;
        private Vector2 _lastTouchPosition;

        // Virtual Joystick State
        private bool _isJoystickActive;
        private Vector2 _joystickCenter;
        private Vector2 _joystickCurrent;
        private float _joystickRadius = 80f;

        // Button Touch States
        private bool _fireButtonHeld;
        private bool _aimButtonHeld;
        private bool _jumpQueued;
        private bool _reloadQueued;
        private bool _pauseQueued;

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

            // Load user sensitivity preference if available
            float savedSens = SettingsManager.Sensitivity;
            if (savedSens > 0.05f)
            {
                _touchLookSensitivity = 0.15f * savedSens;
                _mouseLookSensitivity = 2.0f * savedSens;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            ProcessInput();
        }

        #endregion

        #region Input Processing

        private void ProcessInput()
        {
            // 1. Reset single-frame triggers
            JumpTriggered = false;
            ReloadTriggered = false;
            PauseTriggered = false;

            // 2. Editor & PC Keyboard/Mouse Controls
            Vector2 keyboardMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 mouseLook = Vector2.zero;

            // In Editor or standalone PC, allow mouse look when Right Click or Left Click is used or cursor locked
            if (Input.GetMouseButton(1) || Input.GetMouseButton(0) || Cursor.lockState == CursorLockMode.Locked)
            {
                mouseLook = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * _mouseLookSensitivity;
            }

            // Keyboard action triggers
            bool pcFire = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Mouse0);
            bool pcAim = Input.GetButton("Fire2") || Input.GetKey(KeyCode.Mouse1);
            bool pcJump = Input.GetKeyDown(KeyCode.Space);
            bool pcReload = Input.GetKeyDown(KeyCode.R);
            bool pcPause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);

            // 3. Process Mobile Touch Events
            ProcessTouches();

            // 4. Combine Movement (Mobile Joystick + Keyboard)
            Vector2 combinedMove = _isJoystickActive ? _joystickCurrent : keyboardMove;
            if (combinedMove.sqrMagnitude > 1f) combinedMove.Normalize();
            MoveInput = combinedMove;

            // 5. Combine Look (Mobile Swipe + Mouse)
            Vector2 targetLook = mouseLook + (_rawTouchLookDelta * _touchLookSensitivity);
            _smoothedLookInput = Vector2.Lerp(_smoothedLookInput, targetLook, Time.unscaledDeltaTime * _lookSmoothing);
            LookInput = _smoothedLookInput;
            _rawTouchLookDelta = Vector2.zero;

            // 6. Combine Actions
            IsFirePressed = _fireButtonHeld || pcFire;
            IsAiming = _aimButtonHeld || pcAim;

            if (_jumpQueued || pcJump)
            {
                JumpTriggered = true;
                _jumpQueued = false;
            }

            if (_reloadQueued || pcReload)
            {
                ReloadTriggered = true;
                _reloadQueued = false;
            }

            if (_pauseQueued || pcPause)
            {
                PauseTriggered = true;
                _pauseQueued = false;
            }
        }

        private void ProcessTouches()
        {
            if (Input.touchCount == 0)
            {
                _lookFingerId = -1;
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 pos = touch.position;

                // Right side of screen (X > Screen.width * 0.4f) is dedicated look surface
                if (pos.x > Screen.width * 0.35f)
                {
                    // Ignore touch if hovering over UI buttons
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        continue;
                    }

                    if (touch.phase == TouchPhase.Began)
                    {
                        if (_lookFingerId == -1)
                        {
                            _lookFingerId = touch.fingerId;
                            _lastTouchPosition = touch.position;
                        }
                    }
                    else if (touch.fingerId == _lookFingerId)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            Vector2 delta = touch.position - _lastTouchPosition;
                            _rawTouchLookDelta += delta;
                            _lastTouchPosition = touch.position;
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            _lookFingerId = -1;
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Methods called by UI HUD Buttons

        /// <summary>
        /// Updates the virtual joystick position from the HUD joystick script.
        /// </summary>
        public void SetJoystickInput(Vector2 normalizedVector)
        {
            _isJoystickActive = normalizedVector.sqrMagnitude > 0.001f;
            _joystickCurrent = normalizedVector;
        }

        public void SetFireButtonHeld(bool held) => _fireButtonHeld = held;
        public void SetAimButtonHeld(bool held) => _aimButtonHeld = held;
        public void ToggleAim() => _aimButtonHeld = !_aimButtonHeld;
        public void TriggerJump() => _jumpQueued = true;
        public void TriggerReload() => _reloadQueued = true;
        public void TriggerPause() => _pauseQueued = true;

        #endregion
    }
}
