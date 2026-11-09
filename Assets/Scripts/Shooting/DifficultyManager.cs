using System;
using UnityEngine;

namespace YashGame.Shooting
{
    public enum DifficultyLevel
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>
    /// Manages game combat difficulty settings and gameplay multipliers:
    /// Adjusts enemy damage, health, wave spawn quotas, player survivability, and coin multipliers.
    /// Persists player choice locally across game sessions.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        #region Constants & PlayerPrefs Keys

        private const string KEY_SELECTED_DIFFICULTY = "YashGame_SelectedDifficulty";

        #endregion

        #region Public Properties

        public DifficultyLevel CurrentDifficulty => _currentDifficulty;

        // Combat Multipliers
        public float EnemyDamageMultiplier
        {
            get
            {
                switch (_currentDifficulty)
                {
                    case DifficultyLevel.Easy: return 0.75f;
                    case DifficultyLevel.Normal: return 1.0f;
                    case DifficultyLevel.Hard: return 1.40f;
                    default: return 1.0f;
                }
            }
        }

        public float EnemyHealthMultiplier
        {
            get
            {
                switch (_currentDifficulty)
                {
                    case DifficultyLevel.Easy: return 0.85f;
                    case DifficultyLevel.Normal: return 1.0f;
                    case DifficultyLevel.Hard: return 1.25f;
                    default: return 1.0f;
                }
            }
        }

        public float EnemyCountMultiplier
        {
            get
            {
                switch (_currentDifficulty)
                {
                    case DifficultyLevel.Easy: return 0.80f;
                    case DifficultyLevel.Normal: return 1.0f;
                    case DifficultyLevel.Hard: return 1.35f;
                    default: return 1.0f;
                }
            }
        }

        public float PlayerHealthMultiplier
        {
            get
            {
                switch (_currentDifficulty)
                {
                    case DifficultyLevel.Easy: return 1.25f;
                    case DifficultyLevel.Normal: return 1.0f;
                    case DifficultyLevel.Hard: return 0.85f;
                    default: return 1.0f;
                }
            }
        }

        public float CoinRewardMultiplier
        {
            get
            {
                switch (_currentDifficulty)
                {
                    case DifficultyLevel.Easy: return 1.0f;
                    case DifficultyLevel.Normal: return 1.25f;
                    case DifficultyLevel.Hard: return 1.75f;
                    default: return 1.0f;
                }
            }
        }

        #endregion

        #region Events

        public event Action<DifficultyLevel> OnDifficultyChanged;

        #endregion

        #region Private Fields

        [SerializeField] private DifficultyLevel _currentDifficulty = DifficultyLevel.Normal;

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
            DontDestroyOnLoad(gameObject);

            LoadDifficulty();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        public void SetDifficulty(DifficultyLevel level)
        {
            _currentDifficulty = level;
            PlayerPrefs.SetInt(KEY_SELECTED_DIFFICULTY, (int)level);
            PlayerPrefs.Save();

            Debug.Log($"[DifficultyManager] Combat difficulty set to: {level}");
            OnDifficultyChanged?.Invoke(level);
        }

        public void LoadDifficulty()
        {
            int saved = PlayerPrefs.GetInt(KEY_SELECTED_DIFFICULTY, (int)DifficultyLevel.Normal);
            _currentDifficulty = (DifficultyLevel)Mathf.Clamp(saved, 0, 2);
        }

        public static Color GetDifficultyColor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Easy: return new Color(0.2f, 0.9f, 0.4f);       // Bright Green
                case DifficultyLevel.Normal: return new Color(0.0f, 0.85f, 1.0f);    // Cyan Neon
                case DifficultyLevel.Hard: return new Color(1.0f, 0.25f, 0.35f);     // Crimson Red
                default: return Color.white;
            }
        }

        public static string GetDifficultyName(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Easy: return "RECRUIT (EASY)";
                case DifficultyLevel.Normal: return "OPERATIVE (NORMAL)";
                case DifficultyLevel.Hard: return "VETERAN (HARD)";
                default: return "NORMAL";
            }
        }

        #endregion
    }
}
