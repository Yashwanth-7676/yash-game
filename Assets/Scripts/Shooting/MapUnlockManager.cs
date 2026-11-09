using System;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Manages persistent battlefield map unlocks and progression.
    /// Unlocks Map 1 (Desert Warfare) by default, and unlocks subsequent maps
    /// sequentially upon completing operations.
    /// </summary>
    public class MapUnlockManager : MonoBehaviour
    {
        public static MapUnlockManager Instance { get; private set; }

        #region Constants & PlayerPrefs Keys

        private const string KEY_UNLOCKED_PREFIX = "YashGame_MapUnlocked_";
        private const string KEY_CLEARED_PREFIX = "YashGame_MapCleared_";

        public static readonly string[] MAP_ORDER = new string[]
        {
            "desert",    // Map 1: Desert Warfare
            "city",      // Map 2: Modern City
            "military",  // Map 3: Military Base
            "forest"     // Map 4: Forest Survival
        };

        #endregion

        #region Events

        public event Action<string> OnMapUnlocked;
        public event Action<string> OnMapCleared;

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

            EnsureDefaultUnlocks();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Unlock Logic

        private void EnsureDefaultUnlocks()
        {
            // Map 1 (Desert Warfare) is always unlocked from the start
            if (!IsMapUnlocked("desert"))
            {
                UnlockMap("desert", false);
            }
        }

        public bool IsMapUnlocked(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return false;
            if (mapId == "desert") return true;

            return PlayerPrefs.GetInt(KEY_UNLOCKED_PREFIX + mapId, 0) == 1;
        }

        public void UnlockMap(string mapId, bool fireEvent = true)
        {
            if (string.IsNullOrEmpty(mapId)) return;

            PlayerPrefs.SetInt(KEY_UNLOCKED_PREFIX + mapId, 1);
            PlayerPrefs.Save();

            Debug.Log($"[MapUnlockManager] Battlefield map unlocked: {mapId.ToUpper()}");

            if (fireEvent)
            {
                OnMapUnlocked?.Invoke(mapId);
            }
        }

        public bool IsMapCleared(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return false;
            return PlayerPrefs.GetInt(KEY_CLEARED_PREFIX + mapId, 0) == 1;
        }

        public void MarkMapCleared(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;

            PlayerPrefs.SetInt(KEY_CLEARED_PREFIX + mapId, 1);
            PlayerPrefs.Save();

            Debug.Log($"[MapUnlockManager] Battlefield map marked cleared: {mapId.ToUpper()}");
            OnMapCleared?.Invoke(mapId);

            // Automatically check and unlock next sequential map
            CheckAndUnlockNextMap(mapId);
        }

        private void CheckAndUnlockNextMap(string completedMapId)
        {
            for (int i = 0; i < MAP_ORDER.Length; i++)
            {
                if (MAP_ORDER[i] == completedMapId)
                {
                    if (i + 1 < MAP_ORDER.Length)
                    {
                        string nextMap = MAP_ORDER[i + 1];
                        if (!IsMapUnlocked(nextMap))
                        {
                            UnlockMap(nextMap, true);
                        }
                    }
                    break;
                }
            }
        }

        public int GetClearedMapCount()
        {
            int count = 0;
            for (int i = 0; i < MAP_ORDER.Length; i++)
            {
                if (IsMapCleared(MAP_ORDER[i])) count++;
            }
            return count;
        }

        public string GetNextLockedMapId()
        {
            for (int i = 0; i < MAP_ORDER.Length; i++)
            {
                if (!IsMapUnlocked(MAP_ORDER[i])) return MAP_ORDER[i];
            }
            return null; // All unlocked!
        }

        public void ResetAllProgression()
        {
            for (int i = 0; i < MAP_ORDER.Length; i++)
            {
                PlayerPrefs.DeleteKey(KEY_UNLOCKED_PREFIX + MAP_ORDER[i]);
                PlayerPrefs.DeleteKey(KEY_CLEARED_PREFIX + MAP_ORDER[i]);
            }
            PlayerPrefs.Save();
            EnsureDefaultUnlocks();
        }

        #endregion
    }
}
