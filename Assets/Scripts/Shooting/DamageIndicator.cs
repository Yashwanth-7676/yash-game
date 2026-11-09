using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YashGame.Shooting
{
    /// <summary>
    /// Displays directional damage indicator arcs on the player's screen
    /// pointing towards the world-space location of the attacker who hit them.
    /// </summary>
    public class DamageIndicator : MonoBehaviour
    {
        public static DamageIndicator Instance { get; private set; }

        #region Serialized Settings

        [Header("Settings")]
        [SerializeField] private float _indicatorDuration = 1.4f;
        [SerializeField] private float _indicatorRadius = 260f;

        #endregion

        #region Private Fields

        private Transform _playerTransform;
        private Transform _cameraTransform;
        private RectTransform _container;
        private List<IndicatorInstance> _activeIndicators = new List<IndicatorInstance>();

        private class IndicatorInstance
        {
            public RectTransform rectTransform;
            public Image image;
            public Vector3 attackerPosition;
            public float timeRemaining;
        }

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

            _container = GetComponent<RectTransform>();
            if (_container == null)
            {
                _container = gameObject.AddComponent<RectTransform>();
            }

            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void Start()
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) _playerTransform = pm.transform;
            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null) _playerTransform = pm.transform;
                return;
            }

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            UpdateIndicators();
        }

        #endregion

        #region Indicator Logic

        /// <summary>
        /// Registers a hit from an attacker position and spawns/updates a directional indicator.
        /// </summary>
        public void ShowDamageFrom(Vector3 attackerWorldPos)
        {
            IndicatorInstance ind = CreateIndicator();
            ind.attackerPosition = attackerWorldPos;
            ind.timeRemaining = _indicatorDuration;
            _activeIndicators.Add(ind);
        }

        private void UpdateIndicators()
        {
            if (_activeIndicators.Count == 0 || _cameraTransform == null) return;

            for (int i = _activeIndicators.Count - 1; i >= 0; i--)
            {
                IndicatorInstance ind = _activeIndicators[i];
                ind.timeRemaining -= Time.deltaTime;

                if (ind.timeRemaining <= 0f)
                {
                    Destroy(ind.rectTransform.gameObject);
                    _activeIndicators.RemoveAt(i);
                    continue;
                }

                // Compute angle relative to camera forward
                Vector3 toAttacker = (ind.attackerPosition - _playerTransform.position);
                toAttacker.y = 0f;

                Vector3 camFwd = _cameraTransform.forward;
                camFwd.y = 0f;

                float angle = Vector3.SignedAngle(camFwd, toAttacker, Vector3.up);

                // Position on screen radius
                float rad = -angle * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Sin(rad) * _indicatorRadius, Mathf.Cos(rad) * _indicatorRadius);

                ind.rectTransform.anchoredPosition = pos;
                ind.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

                // Fade alpha over time
                float alpha = Mathf.Clamp01(ind.timeRemaining / _indicatorDuration);
                ind.image.color = new Color(1f, 0.15f, 0.25f, alpha * 0.85f);
            }
        }

        private IndicatorInstance CreateIndicator()
        {
            GameObject obj = new GameObject("DamageArc", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(transform, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 60f);

            Image img = obj.GetComponent<Image>();
            img.color = new Color(1f, 0.15f, 0.25f, 0.85f);
            img.raycastTarget = false;

            return new IndicatorInstance
            {
                rectTransform = rt,
                image = img,
                timeRemaining = _indicatorDuration
            };
        }

        #endregion
    }
}
