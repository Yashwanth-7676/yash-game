using System;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Tracks detailed mission completion metrics per map:
    /// Star ratings (1-3 stars), personal best clear times, highest difficulty conquered,
    /// and triggers map unlock checks via MapUnlockManager.
    /// </summary>
    public class MissionProgressManager : MonoBehaviour
    {
        public static MissionProgressManager Instance { get; private set; }

        #region Constants & PlayerPrefs Keys

        private const string KEY_MAP_STARS = "YashGame_MapStars_";
        private const string KEY_MAP_BEST_TIME = "YashGame_MapBestTime_";
        private const string KEY_MAP_HIGHEST_DIFF = "YashGame_MapHighestDiff_";

        #endregion

        #region Events

        public event Action<string, int, float, bool> OnMissionRecorded; // (mapId, stars, clearTime, isNewBest)

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Progression Record & Retrieval

        /// <summary>
        /// Records a completed mission run and calculates stars earned.
        /// </summary>
        public int RecordMissionCompletion(string mapId, float durationSeconds, float playerHealthPercent, DifficultyLevel difficulty)
        {
            if (string.IsNullOrEmpty(mapId)) mapId = "desert";

            // 1. Star calculation:
            // 1 Star: Victory
            // 2 Stars: Health >= 50%
            // 3 Stars: Time <= 80 seconds
            int stars = 1;
            if (playerHealthPercent >= 0.50f) stars++;
            if (durationSeconds <= 80f) stars++;

            // Save highest stars
            int currentStars = GetMapStars(mapId);
            if (stars > currentStars)
            {
                PlayerPrefs.SetInt(KEY_MAP_STARS + mapId, stars);
            }

            // Save best time
            float currentBest = GetMapBestTime(mapId);
            bool isNewBest = false;
            if (currentBest <= 0.01f || durationSeconds < currentBest)
            {
                PlayerPrefs.SetFloat(KEY_MAP_BEST_TIME + mapId, durationSeconds);
                isNewBest = true;
            }

            // Save highest difficulty
            int highestDiff = PlayerPrefs.GetInt(KEY_MAP_HIGHEST_DIFF + mapId, -1);
            if ((int)difficulty > highestDiff)
            {
                PlayerPrefs.SetInt(KEY_MAP_HIGHEST_DIFF + mapId, (int)difficulty);
            }

            PlayerPrefs.Save();

            // Mark map cleared in MapUnlockManager
            if (MapUnlockManager.Instance != null)
            {
                MapUnlockManager.Instance.MarkMapCleared(mapId);
            }

            Debug.Log($"[MissionProgressManager] Map {mapId.ToUpper()} recorded! Stars: {stars}/3, Time: {durationSeconds:F1}s");
            OnMissionRecorded?.Invoke(mapId, stars, durationSeconds, isNewBest);

            return stars;
        }

        public int GetMapStars(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return 0;
            return PlayerPrefs.GetInt(KEY_MAP_STARS + mapId, 0);
        }

        public float GetMapBestTime(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return 0f;
            return PlayerPrefs.GetFloat(KEY_MAP_BEST_TIME + mapId, 0f);
        }

        public DifficultyLevel GetMapHighestDifficulty(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return DifficultyLevel.Easy;
            int diff = PlayerPrefs.GetInt(KEY_MAP_HIGHEST_DIFF + mapId, (int)DifficultyLevel.Normal);
            return (DifficultyLevel)Mathf.Clamp(diff, 0, 2);
        }

        public int GetTotalStars()
        {
            int total = 0;
            foreach (string mapId in MapUnlockManager.MAP_ORDER)
            {
                total += GetMapStars(mapId);
            }
            return total;
        }

        #endregion
    }
}
