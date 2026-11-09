using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Handles intelligent dynamic spawning and wave management of the 3 enemy archetypes:
    /// Soldier, Heavy Soldier, and Fast Soldier, plus High-Value Targets.
    /// Strictly limits simultaneous active enemies (default max 6) to preserve solid 60 FPS on mobile.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        #region Serialized Settings

        [Header("Spawn Configuration")]
        [SerializeField] private int _maxSimultaneousEnemies = 6;
        [SerializeField] private float _spawnInterval = 3.5f;

        #endregion

        #region Public Properties

        public int ActiveEnemyCount => _activeEnemies.Count;
        public int TotalEnemiesSpawned => _totalEnemiesSpawned;
        public int TotalEnemiesDefeated => _totalEnemiesDefeated;
        public IReadOnlyList<EnemyAI> ActiveEnemies => _activeEnemies;

        #endregion

        #region Events

        public event Action<EnemyAI> OnEnemySpawned;
        public event Action<EnemyAI> OnEnemyDefeated;
        public event Action OnAllMissionEnemiesDefeated;

        #endregion

        #region Private Fields

        private readonly List<EnemyAI> _activeEnemies = new List<EnemyAI>();
        private int _totalEnemiesSpawned = 0;
        private int _totalEnemiesDefeated = 0;

        private BattlefieldBuilder _battlefieldBuilder;
        private Transform _playerTransform;
        private Coroutine _spawnRoutine;

        private int _targetTotalToSpawn = 0;
        private bool _isSpawningActive = false;

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

            _battlefieldBuilder = FindObjectOfType<BattlefieldBuilder>();
        }

        private void Start()
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                _playerTransform = pm.transform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Spawning API

        /// <summary>
        /// Starts spawning enemies for an Elimination mission until totalQuota is reached.
        /// </summary>
        public void StartEliminationSpawning(int totalQuota)
        {
            ResetSpawner();

            if (DifficultyManager.Instance != null)
            {
                totalQuota = Mathf.RoundToInt(totalQuota * DifficultyManager.Instance.EnemyCountMultiplier);
            }

            _targetTotalToSpawn = totalQuota;
            _isSpawningActive = true;

            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(EliminationSpawnRoutine());
        }

        /// <summary>
        /// Starts continuous wave spawning for a Survival mission.
        /// </summary>
        public void StartSurvivalSpawning()
        {
            ResetSpawner();
            _targetTotalToSpawn = int.MaxValue;
            _isSpawningActive = true;

            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SurvivalSpawnRoutine());
        }

        /// <summary>
        /// Spawns the High-Value Target boss along with escort guards for Assassination mission.
        /// </summary>
        public void StartAssassinationSpawning(string hvtName)
        {
            ResetSpawner();
            _isSpawningActive = false;

            // Spawn Commander (HVT)
            Vector3 bossPos = GetSpawnPosition(0);
            Transform[] bossWaypoints = GetPatrolWaypoints(0);
            EnemyAI boss = SpawnEnemy(EnemyType.HeavySoldier, bossPos, bossWaypoints);
            if (boss != null)
            {
                boss.gameObject.name = "HVT_Commander";
                // Scale boss slightly to look formidable
                boss.transform.localScale = Vector3.one * 1.35f;
            }

            // Spawn 2 Soldier escorts
            Vector3 escortPos1 = bossPos + new Vector3(-3f, 0f, -2f);
            Vector3 escortPos2 = bossPos + new Vector3(3f, 0f, -2f);
            SpawnEnemy(EnemyType.Soldier, escortPos1, bossWaypoints);
            SpawnEnemy(EnemyType.FastSoldier, escortPos2, bossWaypoints);
        }

        /// <summary>
        /// Instantiates and configures an enemy of the specified archetype.
        /// </summary>
        public EnemyAI SpawnEnemy(EnemyType type, Vector3 position, Transform[] waypoints = null)
        {
            GameObject enemyObj = new GameObject($"Enemy_{type}_{_totalEnemiesSpawned + 1}");
            enemyObj.transform.position = position;

            // Add required components
            EnemyHealth health = enemyObj.AddComponent<EnemyHealth>();
            EnemyAI ai = enemyObj.AddComponent<EnemyAI>();

            ai.ConfigureArchetype(type);

            _activeEnemies.Add(ai);
            _totalEnemiesSpawned++;

            health.OnDied += () => HandleEnemyDefeated(ai);

            OnEnemySpawned?.Invoke(ai);
            return ai;
        }

        public void StopSpawning()
        {
            _isSpawningActive = false;
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

        public void ResetSpawner()
        {
            StopSpawning();

            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i] != null)
                {
                    Destroy(_activeEnemies[i].gameObject);
                }
            }
            _activeEnemies.Clear();
            _totalEnemiesSpawned = 0;
            _totalEnemiesDefeated = 0;
        }

        #endregion

        #region Internal Spawn Coroutines

        private IEnumerator EliminationSpawnRoutine()
        {
            // Initial burst: spawn 3 enemies
            int initialCount = Mathf.Min(3, _targetTotalToSpawn);
            for (int i = 0; i < initialCount; i++)
            {
                SpawnNextWaveEnemy();
                yield return new WaitForSeconds(0.6f);
            }

            while (_isSpawningActive && _totalEnemiesSpawned < _targetTotalToSpawn)
            {
                yield return new WaitForSeconds(_spawnInterval);

                if (_activeEnemies.Count < _maxSimultaneousEnemies && _totalEnemiesSpawned < _targetTotalToSpawn)
                {
                    SpawnNextWaveEnemy();
                }
            }
        }

        private IEnumerator SurvivalSpawnRoutine()
        {
            // Initial spawn
            for (int i = 0; i < 3; i++)
            {
                SpawnNextWaveEnemy();
                yield return new WaitForSeconds(0.8f);
            }

            while (_isSpawningActive)
            {
                yield return new WaitForSeconds(_spawnInterval);

                if (_activeEnemies.Count < _maxSimultaneousEnemies)
                {
                    SpawnNextWaveEnemy();
                }
            }
        }

        private void SpawnNextWaveEnemy()
        {
            EnemyType type = SelectRandomEnemyType();
            int routeIndex = UnityEngine.Random.Range(0, 5);

            Vector3 spawnPos = GetSpawnPosition(routeIndex);
            Transform[] waypoints = GetPatrolWaypoints(routeIndex);

            SpawnEnemy(type, spawnPos, waypoints);
        }

        private EnemyType SelectRandomEnemyType()
        {
            // Weighted distribution: 50% Standard Soldier, 30% Fast Skirmisher, 20% Heavy Juggernaut
            float roll = UnityEngine.Random.value;
            if (roll < 0.50f) return EnemyType.Soldier;
            if (roll < 0.80f) return EnemyType.FastSoldier;
            return EnemyType.HeavySoldier;
        }

        private Vector3 GetSpawnPosition(int routeIndex)
        {
            if (MapManager.Instance != null && MapManager.Instance.EnemySpawnPoints.Count > routeIndex)
            {
                return MapManager.Instance.EnemySpawnPoints[routeIndex];
            }

            if (_battlefieldBuilder != null && _battlefieldBuilder.EnemySpawnPoints.Count > routeIndex)
            {
                return _battlefieldBuilder.EnemySpawnPoints[routeIndex];
            }

            // Fallback perimeter spawn positions
            Vector3[] fallbacks = new Vector3[]
            {
                new Vector3(-25f, 0f, 25f),
                new Vector3(25f, 0f, 25f),
                new Vector3(-25f, 0f, -25f),
                new Vector3(25f, 0f, -25f),
                new Vector3(0f, 0f, 30f)
            };
            return fallbacks[routeIndex % fallbacks.Length];
        }

        private Transform[] GetPatrolWaypoints(int routeIndex)
        {
            if (MapManager.Instance != null && MapManager.Instance.EnemyPatrolRoutes.Count > routeIndex)
            {
                return MapManager.Instance.EnemyPatrolRoutes[routeIndex];
            }

            if (_battlefieldBuilder != null && _battlefieldBuilder.EnemyPatrolRoutes.Count > routeIndex)
            {
                return _battlefieldBuilder.EnemyPatrolRoutes[routeIndex];
            }
            return null;
        }

        private void HandleEnemyDefeated(EnemyAI enemy)
        {
            if (_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Remove(enemy);
            }

            _totalEnemiesDefeated++;
            OnEnemyDefeated?.Invoke(enemy);

            // If elimination target reached
            if (_targetTotalToSpawn > 0 && _totalEnemiesDefeated >= _targetTotalToSpawn)
            {
                OnAllMissionEnemiesDefeated?.Invoke();
            }
        }

        #endregion
    }
}
