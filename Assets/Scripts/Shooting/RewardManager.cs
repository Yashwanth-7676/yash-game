using System;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Manages player currency and combat rewards.
    /// Saves and loads accumulated coins persistently using PlayerPrefs,
    /// awards coins on enemy eliminations, bonus coins on mission victory,
    /// and triggers audio chimes.
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        public static RewardManager Instance { get; private set; }

        #region Constants & PlayerPrefs Keys

        private const string KEY_PLAYER_COINS = "YashGame_PlayerCoins";
        private const int DEFAULT_COINS = 250;

        #endregion

        #region Public Properties

        public int TotalCoins { get; private set; }
        public int CurrentCoins => TotalCoins;
        public int SessionEarnedCoins { get; private set; }

        #endregion

        #region Events

        public event Action<int, int> OnCoinsUpdated; // (totalCoins, sessionEarned)
        public event Action<int, int> OnCoinsChanged; // Alias for HUD compatibility

        #endregion

        #region Private Fields

        private AudioSource _audioSource;
        private AudioClip _coinClip;

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

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _coinClip = Resources.Load<AudioClip>("Audio/Shooting/Coin_Pickup");

            LoadCoins();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Coin Operations

        public void LoadCoins()
        {
            TotalCoins = PlayerPrefs.GetInt(KEY_PLAYER_COINS, DEFAULT_COINS);
            SessionEarnedCoins = 0;
            OnCoinsUpdated?.Invoke(TotalCoins, SessionEarnedCoins);
            OnCoinsChanged?.Invoke(TotalCoins, SessionEarnedCoins);
        }

        public void SaveCoins()
        {
            PlayerPrefs.SetInt(KEY_PLAYER_COINS, TotalCoins);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Awards coins for defeating an enemy target or completing an objective.
        /// </summary>
        public void AddCoins(int amount, bool playSound = true)
        {
            if (amount <= 0) return;

            if (DifficultyManager.Instance != null)
            {
                amount = Mathf.RoundToInt(amount * DifficultyManager.Instance.CoinRewardMultiplier);
            }

            TotalCoins += amount;
            SessionEarnedCoins += amount;
            SaveCoins();

            OnCoinsUpdated?.Invoke(TotalCoins, SessionEarnedCoins);
            OnCoinsChanged?.Invoke(TotalCoins, SessionEarnedCoins);

            if (playSound && _coinClip != null && _audioSource != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                _audioSource.PlayOneShot(_coinClip, 0.85f);
            }
        }

        /// <summary>
        /// Resets session counter for fresh mission start.
        /// </summary>
        public void ResetSessionEarnings()
        {
            SessionEarnedCoins = 0;
            OnCoinsUpdated?.Invoke(TotalCoins, SessionEarnedCoins);
            OnCoinsChanged?.Invoke(TotalCoins, SessionEarnedCoins);
        }

        #endregion
    }
}
