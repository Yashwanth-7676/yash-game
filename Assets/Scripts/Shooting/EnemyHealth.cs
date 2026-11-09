using System.Collections;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Health system for enemy targets. Handles hit reactions (color flash, hit sounds),
    /// health depletion, death collapse animation, and objective notification.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Health Settings")]
        [SerializeField, Tooltip("Maximum hit points.")]
        private float _maxHealth = 100f;

        [SerializeField, Tooltip("Points awarded when defeated.")]
        private int _scoreValue = 100;

        #endregion

        #region Public Properties

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0f;

        #endregion

        #region Events

        public event System.Action<float, float> OnHealthChanged;
        public event System.Action OnHit;
        public event System.Action OnDied;

        #endregion

        #region Private Fields

        private float _currentHealth;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private AudioSource _audioSource;
        private AudioClip _hitClip;
        private AudioClip _defeatClip;
        private bool _isDying;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _renderers = GetComponentsInChildren<Renderer>();

            if (_renderers != null && _renderers.Length > 0)
            {
                _originalColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null && _renderers[i].material != null)
                    {
                        _originalColors[i] = _renderers[i].material.color;
                    }
                }
            }

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.8f; // 3D sound
            _audioSource.minDistance = 2f;
            _audioSource.maxDistance = 35f;

            _hitClip = Resources.Load<AudioClip>("Audio/Shooting/Enemy_Hit");
            _defeatClip = Resources.Load<AudioClip>("Audio/Shooting/Enemy_Defeated");
        }

        #endregion

        #region Damage Handling

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (!IsAlive || _isDying) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnHit?.Invoke();

            // Hit feedback
            PlayHitReaction();

            if (_currentHealth <= 0f)
            {
                Die(hitDirection);
            }
        }

        private void PlayHitReaction()
        {
            if (_hitClip != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.PlayOneShot(_hitClip, 0.75f);
            }

            StopCoroutine(nameof(FlashWhiteRoutine));
            StartCoroutine(nameof(FlashWhiteRoutine));
        }

        private IEnumerator FlashWhiteRoutine()
        {
            if (_renderers == null) yield break;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].material.color = Color.white;
                }
            }

            yield return new WaitForSeconds(0.08f);

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _originalColors != null && i < _originalColors.Length)
                {
                    _renderers[i].material.color = _originalColors[i];
                }
            }
        }

        private void Die(Vector3 hitDirection)
        {
            _isDying = true;
            OnDied?.Invoke();

            if (_defeatClip != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.95f, 1.05f);
                _audioSource.PlayOneShot(_defeatClip, 1.0f);
            }

            // Notify Game Manager
            if (ShootingGameManager.Instance != null)
            {
                ShootingGameManager.Instance.OnTargetEliminated(gameObject, _scoreValue);
            }

            // Disable enemy controller and colliders
            EnemyController controller = GetComponent<EnemyController>();
            if (controller != null) controller.enabled = false;

            Collider[] cols = GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;

            // Simple procedural tumble/collapse animation
            StartCoroutine(DeathAnimationRoutine(hitDirection));
        }

        private IEnumerator DeathAnimationRoutine(Vector3 hitDirection)
        {
            float elapsed = 0f;
            float duration = 1.0f;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.Euler(75f, startRot.eulerAngles.y, startRot.eulerAngles.z);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t * 1.5f);
                transform.position = startPos + Vector3.down * (t * 0.4f);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        #endregion
    }
}
