using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Master coordinator for the 3 playable shooting missions:
    /// 1. Operation: Sector Purge (Elimination)
    /// 2. Operation: Last Stand (Survival)
    /// 3. Operation: Cyber Warlord (Assassination)
    /// Manages briefings, objectives, real-time timers, completion triggers, and coin rewards.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        #region Serialized Settings

        [Header("Missions List")]
        [SerializeField] private List<MissionData> _missions = new List<MissionData>();
        [SerializeField] private int _currentMissionIndex = 0;

        #endregion

        #region Public Properties

        public MissionData CurrentMission => (_missions != null && _currentMissionIndex < _missions.Count) ? _missions[_currentMissionIndex] : null;
        public int CurrentMissionIndex => _currentMissionIndex;
        public int TotalMissionsCount => _missions.Count;
        public bool IsMissionActive => _isMissionActive;
        public float MissionTimeElapsed => _missionTimeElapsed;
        public float TimeRemaining => Mathf.Max(0f, _currentMissionTimeLimit - _missionTimeElapsed);
        public int KillsCount => _killsCount;

        #endregion

        #region Events

        public event Action<MissionData> OnMissionStarted;
        public event Action<string, float> OnMissionProgressUpdated; // (progressText, 0..1 ratio)
        public event Action<MissionData, bool, int, float> OnMissionCompleted; // (mission, victory, coinsEarned, elapsedSeconds)

        #endregion

        #region Private Fields

        private bool _isMissionActive = false;
        private float _missionTimeElapsed = 0f;
        private float _currentMissionTimeLimit = 0f;
        private int _killsCount = 0;

        private EnemySpawner _spawner;
        private PlayerHealth _playerHealth;
        private ShootingHUD _hud;

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

            InitializeDefaultMissions();
        }

        private void Start()
        {
            _spawner = EnemySpawner.Instance;
            if (_spawner == null) _spawner = FindObjectOfType<EnemySpawner>();

            _hud = ShootingHUD.Instance;
            if (_hud == null) _hud = FindObjectOfType<ShootingHUD>();

            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                _playerHealth = pm.GetComponent<PlayerHealth>();
                if (_playerHealth != null)
                {
                    _playerHealth.OnDied += HandlePlayerDied;
                }
            }

            if (_spawner != null)
            {
                _spawner.OnEnemyDefeated += HandleEnemyDefeated;
                _spawner.OnAllMissionEnemiesDefeated += HandleAllEliminationTargetsDefeated;
            }

            // Start first mission by default
            StartMission(_currentMissionIndex);
        }

        private void Update()
        {
            if (!_isMissionActive) return;

            _missionTimeElapsed += Time.deltaTime;

            if (CurrentMission != null)
            {
                if (CurrentMission.type == MissionType.Survival)
                {
                    float remaining = TimeRemaining;
                    float ratio = 1f - Mathf.Clamp01(remaining / CurrentMission.timeLimitSeconds);
                    string text = $"SURVIVE: {Mathf.CeilToInt(remaining)}s | KILLS: {_killsCount}";

                    OnMissionProgressUpdated?.Invoke(text, ratio);

                    if (remaining <= 0f)
                    {
                        EndMission(true);
                    }
                }
                else if (CurrentMission.type == MissionType.Elimination)
                {
                    float ratio = Mathf.Clamp01((float)_killsCount / CurrentMission.targetCount);
                    string text = $"TARGETS ELIMINATED: {_killsCount}/{CurrentMission.targetCount}";
                    OnMissionProgressUpdated?.Invoke(text, ratio);
                }
                else if (CurrentMission.type == MissionType.Assassination)
                {
                    string text = $"LOCATE & ELIMINATE: {CurrentMission.hvtName.ToUpper()}";
                    OnMissionProgressUpdated?.Invoke(text, 0.5f);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_playerHealth != null)
            {
                _playerHealth.OnDied -= HandlePlayerDied;
            }

            if (_spawner != null)
            {
                _spawner.OnEnemyDefeated -= HandleEnemyDefeated;
                _spawner.OnAllMissionEnemiesDefeated -= HandleAllEliminationTargetsDefeated;
            }
        }

        #endregion

        #region Mission Initialization

        private void InitializeDefaultMissions()
        {
            _missions.Clear();
            _missions.Add(MissionData.CreateSectorPurge());    // Mission 1: Elimination
            _missions.Add(MissionData.CreateLastStand());      // Mission 2: Survival
            _missions.Add(MissionData.CreateAssassination());  // Mission 3: Assassination
        }

        #endregion

        #region Mission Control API

        public void StartMission(int missionIndex)
        {
            if (missionIndex < 0 || missionIndex >= _missions.Count)
            {
                missionIndex = 0;
            }

            _currentMissionIndex = missionIndex;
            MissionData mission = CurrentMission;
            if (mission == null) return;

            _isMissionActive = true;
            _missionTimeElapsed = 0f;
            _currentMissionTimeLimit = mission.timeLimitSeconds;
            _killsCount = 0;

            if (_spawner == null) _spawner = FindObjectOfType<EnemySpawner>();

            if (_spawner != null)
            {
                switch (mission.type)
                {
                    case MissionType.Elimination:
                        _spawner.StartEliminationSpawning(mission.targetCount);
                        break;
                    case MissionType.Survival:
                        _spawner.StartSurvivalSpawning();
                        break;
                    case MissionType.Assassination:
                        _spawner.StartAssassinationSpawning(mission.hvtName);
                        break;
                }
            }

            OnMissionStarted?.Invoke(mission);
        }

        public void NextMission()
        {
            int nextIndex = (_currentMissionIndex + 1) % _missions.Count;
            StartMission(nextIndex);
        }

        public void RestartCurrentMission()
        {
            StartMission(_currentMissionIndex);
        }

        public void EndMission(bool victory)
        {
            if (!_isMissionActive) return;
            _isMissionActive = false;

            if (_spawner != null)
            {
                _spawner.StopSpawning();
            }

            int earnedCoins = 0;
            if (victory && CurrentMission != null)
            {
                earnedCoins = CurrentMission.rewardCoins;
                if (RewardManager.Instance != null)
                {
                    RewardManager.Instance.AddCoins(earnedCoins);
                }
            }

            OnMissionCompleted?.Invoke(CurrentMission, victory, earnedCoins, _missionTimeElapsed);
        }

        #endregion

        #region Event Handlers

        private void HandleEnemyDefeated(EnemyAI enemy)
        {
            if (!_isMissionActive) return;

            _killsCount++;

            if (CurrentMission != null && CurrentMission.type == MissionType.Assassination)
            {
                // Check if defeated enemy was the HVT
                if (enemy != null && enemy.gameObject.name.Contains("HVT"))
                {
                    EndMission(true);
                }
            }
        }

        private void HandleAllEliminationTargetsDefeated()
        {
            if (!_isMissionActive) return;

            if (CurrentMission != null && CurrentMission.type == MissionType.Elimination)
            {
                EndMission(true);
            }
        }

        private void HandlePlayerDied()
        {
            if (!_isMissionActive) return;

            EndMission(false);
        }

        #endregion
    }
}
