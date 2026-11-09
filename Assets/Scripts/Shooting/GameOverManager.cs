using System;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Manages game-over state transitions, rating evaluations (S, A, B, C ranks),
    /// coin bonus calculations, and clean scene restarts / hub routing.
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        public static GameOverManager Instance { get; private set; }

        #region Public Properties

        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }
        public string LastRankEarned { get; private set; } = "B";
        public int LastTotalCoinsEarned { get; private set; } = 0;

        #endregion

        #region Events

        public event Action<bool, string, int> OnGameOverTriggered; // (victory, rank, totalCoins)

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Evaluation & Triggers

        /// <summary>
        /// Evaluates match performance and yields rank + bonus coin rewards.
        /// </summary>
        public void TriggerGameOver(bool victory, float durationSeconds, int enemiesKilled, int baseReward)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            IsVictory = victory;

            string rank = EvaluateRank(victory, durationSeconds, enemiesKilled);
            LastRankEarned = rank;

            int rankBonus = 0;
            if (victory)
            {
                switch (rank)
                {
                    case "S": rankBonus = 100; break;
                    case "A": rankBonus = 60; break;
                    case "B": rankBonus = 30; break;
                    default: rankBonus = 10; break;
                }
            }

            LastTotalCoinsEarned = victory ? (baseReward + rankBonus) : 0;

            if (victory && LastTotalCoinsEarned > 0 && RewardManager.Instance != null)
            {
                RewardManager.Instance.AddCoins(LastTotalCoinsEarned);
            }

            OnGameOverTriggered?.Invoke(victory, rank, LastTotalCoinsEarned);
        }

        private string EvaluateRank(bool victory, float duration, int kills)
        {
            if (!victory) return "F";

            // Speedy clears and high kills get S rank
            if (duration <= 45f && kills >= 5) return "S";
            if (duration <= 75f && kills >= 4) return "A";
            if (duration <= 120f) return "B";
            return "C";
        }

        public void ResetState()
        {
            IsGameOver = false;
            IsVictory = false;
        }

        #endregion
    }
}
