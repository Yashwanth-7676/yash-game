using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Auxiliary weapon switching controller.
    /// Handles swipe gestures, mouse scroll wheel, and quick-switch hotkeys (Q, Number Keys),
    /// forwarding selection events directly to the active WeaponManager.
    /// </summary>
    public class WeaponSwitching : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Swipe Detection")]
        [SerializeField] private float _swipeThreshold = 50f;
        [SerializeField] private bool _enableSwipeSwitch = true;

        #endregion

        #region Private Fields

        private WeaponManager _weaponManager;
        private Vector2 _touchStartPos;
        private bool _isSwipeCandidate;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _weaponManager = GetComponent<WeaponManager>();
            if (_weaponManager == null)
            {
                _weaponManager = WeaponManager.Instance;
            }
        }

        private void Update()
        {
            if (_weaponManager == null)
            {
                _weaponManager = WeaponManager.Instance;
                if (_weaponManager == null) return;
            }

            HandleQuickKeys();
            HandleScrollWheel();
            if (_enableSwipeSwitch)
            {
                HandleMobileSwipe();
            }
        }

        #endregion

        #region Input Handling

        private void HandleQuickKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) _weaponManager.SwitchToSlot(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) _weaponManager.SwitchToSlot(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) _weaponManager.SwitchToSlot(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) _weaponManager.SwitchToSlot(3);

            // Q toggles between primary and secondary
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int next = (_weaponManager.ActiveSlotIndex == 0) ? 1 : 0;
                _weaponManager.SwitchToSlot(next);
            }
        }

        private void HandleScrollWheel()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.05f)
            {
                int count = _weaponManager.TotalWeaponsCount;
                if (count > 0)
                {
                    int next = (_weaponManager.ActiveSlotIndex + 1) % count;
                    _weaponManager.SwitchToSlot(next);
                }
            }
            else if (scroll < -0.05f)
            {
                int count = _weaponManager.TotalWeaponsCount;
                if (count > 0)
                {
                    int prev = (_weaponManager.ActiveSlotIndex - 1 + count) % count;
                    _weaponManager.SwitchToSlot(prev);
                }
            }
        }

        private void HandleMobileSwipe()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Check if touch is on upper screen area (not obstructing joystick or fire buttons)
                if (touch.position.y > Screen.height * 0.4f && touch.position.y < Screen.height * 0.85f)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        _touchStartPos = touch.position;
                        _isSwipeCandidate = true;
                    }
                    else if (touch.phase == TouchPhase.Ended && _isSwipeCandidate)
                    {
                        Vector2 delta = touch.position - _touchStartPos;
                        if (Mathf.Abs(delta.x) > _swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.5f)
                        {
                            int count = _weaponManager.TotalWeaponsCount;
                            if (count > 0)
                            {
                                if (delta.x > 0)
                                {
                                    // Swipe Right -> Next Weapon
                                    int next = (_weaponManager.ActiveSlotIndex + 1) % count;
                                    _weaponManager.SwitchToSlot(next);
                                }
                                else
                                {
                                    // Swipe Left -> Previous Weapon
                                    int prev = (_weaponManager.ActiveSlotIndex - 1 + count) % count;
                                    _weaponManager.SwitchToSlot(prev);
                                }
                            }
                        }
                        _isSwipeCandidate = false;
                    }
                }
            }
            else
            {
                _isSwipeCandidate = false;
            }
        }

        #endregion
    }
}
