using System;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Player health system. Manages current HP, damage intake,
    /// audio feedback, and death notification to the ShootingGameManager.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        #region Serialized Settings

        [Header("Health Settings")]
        [SerializeField, Tooltip("Maximum hit points.")]
        private float _maxHealth = 100f;

        [SerializeField, Tooltip("Invulnerability grace period in seconds after taking hit.")]
        private float _invulnerabilityDuration = 0.35f;

        #endregion

        #region Public Properties

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float HealthRatio => Mathf.Clamp01(_currentHealth / _maxHealth);
        public bool IsAlive => _currentHealth > 0f;

        #endregion

        #region Events

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action OnPlayerDamaged;
        public event Action OnPlayerDied;

        #endregion

        #region Private Fields

        private float _currentHealth;
        private float _lastDamageTime = -99f;
        private AudioSource _audioSource;
        private AudioClip _hurtClip;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (DifficultyManager.Instance != null)
            {
                _maxHealth *= DifficultyManager.Instance.PlayerHealthMultiplier;
            }

            _currentHealth = _maxHealth;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _hurtClip = Resources.Load<AudioClip>("Audio/Shooting/Player_Hurt");
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        #endregion

        #region Damage & Healing

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            if (Time.time < _lastDamageTime + _invulnerabilityDuration) return;

            _lastDamageTime = Time.time;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnPlayerDamaged?.Invoke();

            if (_hurtClip != null && _audioSource != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                _audioSource.PlayOneShot(_hurtClip, 0.85f);
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            Debug.Log("[PlayerHealth] Player operative eliminated!");
            OnPlayerDied?.Invoke();

            if (ShootingGameManager.Instance != null)
            {
                ShootingGameManager.Instance.OnPlayerDefeated();
            }
        }

        #endregion
    }
}
