using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Master coordinator for YASH GAME Shooting Mode (Phases 2, 3 & 4).
    /// Orchestrates:
    /// - 4 Selectable 3D Battlefield Maps (MapManager)
    /// - Player character with 4-weapon arsenal (WeaponManager)
    /// - 3 Enemy archetypes with difficulty scaling (EnemyAI, EnemySpawner)
    /// - 3 Playable missions: Sector Purge, Last Stand, Cyber Warlord (MissionManager)
    /// - Persistent rewards, star progression & map unlocks (RewardManager, MissionProgressManager, MapUnlockManager)
    /// - Difficulty system (DifficultyManager)
    /// - Mobile landscape HUD, controls & modals (ShootingHUD)
    /// </summary>
    public class ShootingGameManager : MonoBehaviour
    {
        public static ShootingGameManager Instance { get; private set; }

        #region Serialized Settings

        [Header("Audio")]
        [SerializeField] private AudioClip _victoryClip;

        #endregion

        #region Public Properties

        public GameObject PlayerObject { get; private set; }
        public WeaponManager PlayerWeaponManager { get; private set; }
        public bool IsGameOver => _isGameOver;
        public string ActiveMapId => _activeMapId;

        #endregion

        #region Private Fields

        private bool _isGameOver = false;
        private string _activeMapId = "desert";

        private MapManager _mapManager;
        private ShootingHUD _hud;
        private MobileInputController _input;
        private ThirdPersonCamera _cameraController;
        private AudioSource _audioSource;

        private EnemySpawner _enemySpawner;
        private MissionManager _missionManager;
        private RewardManager _rewardManager;
        private DifficultyManager _difficultyManager;
        private MapUnlockManager _mapUnlockManager;
        private MissionProgressManager _missionProgressManager;

        private PlayerHealth _playerHealth;

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

            // Enforce landscape orientation for shooting gameplay
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _victoryClip = Resources.Load<AudioClip>("Audio/Shooting/Victory_Fanfare");

            SetupShootingSystems();
        }

        private void Start()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.ShootingGame);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // Restore portrait orientation when leaving shooting game
            Screen.orientation = ScreenOrientation.Portrait;
        }

        #endregion

        #region Runtime Auto-Bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoadedRuntime()
        {
            CheckAndBootstrap();
            SceneManager.sceneLoaded += (scene, mode) => CheckAndBootstrap();
        }

        private static void CheckAndBootstrap()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.name == SceneLoader.SHOOTING || active.name == "ShootingGame")
            {
                if (Instance == null && FindObjectOfType<ShootingGameManager>() == null)
                {
                    GameObject managerObj = new GameObject("ShootingGameManager");
                    managerObj.AddComponent<ShootingGameManager>();
                }
            }
        }

        #endregion

        #region Core Systems Orchestration

        private void SetupShootingSystems()
        {
            // 1. Mobile Input Controller
            EnsureMobileInput();

            // 2. Persistent Reward & Economy Managers
            EnsureProgressionManagers();

            // 3. Combat Effects Pool
            EnsureCombatEffects();

            // 4. Build Selected 3D Battlefield Map (Desert, City, Military, Forest)
            EnsureBattlefieldMap();

            // 5. Setup Camera & Lighting
            SetupCamera();

            // 6. Spawn Player Character with WeaponManager & Health Multipliers
            SpawnPlayer();

            // 7. Setup Enemy Spawner
            EnsureEnemySpawner();

            // 8. Setup Mission Manager (Elimination, Survival, Assassination)
            EnsureMissionManager();

            // 9. Setup Shooting HUD
            SetupHUD();
        }

        private void EnsureMobileInput()
        {
            _input = FindObjectOfType<MobileInputController>();
            if (_input == null)
            {
                GameObject inputObj = new GameObject("MobileInputController");
                _input = inputObj.AddComponent<MobileInputController>();
            }
        }

        private void EnsureProgressionManagers()
        {
            _rewardManager = RewardManager.Instance ?? FindObjectOfType<RewardManager>();
            if (_rewardManager == null)
            {
                GameObject rewardObj = new GameObject("RewardManager");
                _rewardManager = rewardObj.AddComponent<RewardManager>();
            }

            _difficultyManager = DifficultyManager.Instance ?? FindObjectOfType<DifficultyManager>();
            if (_difficultyManager == null)
            {
                GameObject diffObj = new GameObject("DifficultyManager");
                _difficultyManager = diffObj.AddComponent<DifficultyManager>();
            }

            _mapUnlockManager = MapUnlockManager.Instance ?? FindObjectOfType<MapUnlockManager>();
            if (_mapUnlockManager == null)
            {
                GameObject unlockObj = new GameObject("MapUnlockManager");
                _mapUnlockManager = unlockObj.AddComponent<MapUnlockManager>();
            }

            _missionProgressManager = MissionProgressManager.Instance ?? FindObjectOfType<MissionProgressManager>();
            if (_missionProgressManager == null)
            {
                GameObject progObj = new GameObject("MissionProgressManager");
                _missionProgressManager = progObj.AddComponent<MissionProgressManager>();
            }
        }

        private void EnsureCombatEffects()
        {
            if (CombatEffects.Instance == null && FindObjectOfType<CombatEffects>() == null)
            {
                GameObject fxObj = new GameObject("CombatEffects");
                fxObj.AddComponent<CombatEffects>();
            }
        }

        private void EnsureBattlefieldMap()
        {
            _activeMapId = PlayerPrefs.GetString("YashGame_SelectedMap", "desert");

            _mapManager = GetComponent<MapManager>();
            if (_mapManager == null)
            {
                _mapManager = gameObject.AddComponent<MapManager>();
            }

            _mapManager.BuildMap(_activeMapId);
        }

        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            _cameraController = mainCam.GetComponent<ThirdPersonCamera>();
            if (_cameraController == null)
            {
                _cameraController = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
            }
        }

        private void SpawnPlayer()
        {
            Vector3 spawnPos = new Vector3(0f, 0.5f, -42f);
            if (_mapManager != null && _mapManager.PlayerSpawnPoint != null)
            {
                spawnPos = _mapManager.PlayerSpawnPoint.position;
            }

            PlayerObject = new GameObject("PlayerCharacter");
            PlayerObject.tag = "Player";
            PlayerObject.transform.position = spawnPos;

            // Add Locomotion & Health
            PlayerMovement pm = PlayerObject.AddComponent<PlayerMovement>();
            _playerHealth = PlayerObject.AddComponent<PlayerHealth>();

            // Phase 3 Master 4-Weapon Manager
            PlayerWeaponManager = PlayerObject.AddComponent<WeaponManager>();

            _playerHealth.OnDied += HandlePlayerDied;

            // Connect Camera
            if (_cameraController != null)
            {
                _cameraController.SetTarget(PlayerObject.transform);
            }
        }

        private void EnsureEnemySpawner()
        {
            _enemySpawner = EnemySpawner.Instance;
            if (_enemySpawner == null)
            {
                GameObject spawnerObj = new GameObject("EnemySpawner");
                _enemySpawner = spawnerObj.AddComponent<EnemySpawner>();
            }
        }

        private void EnsureMissionManager()
        {
            _missionManager = MissionManager.Instance;
            if (_missionManager == null)
            {
                GameObject missionObj = new GameObject("MissionManager");
                _missionManager = missionObj.AddComponent<MissionManager>();
            }

            _missionManager.OnMissionCompleted += HandleMissionCompleted;
        }

        private void SetupHUD()
        {
            _hud = FindObjectOfType<ShootingHUD>();
            if (_hud == null)
            {
                GameObject hudObj = new GameObject("ShootingHUD");
                _hud = hudObj.AddComponent<ShootingHUD>();
            }

            _hud.HookPlayerComponents();
        }

        #endregion

        #region Game Progression & Event Handlers

        private void HandlePlayerDied()
        {
            if (_isGameOver) return;
            _isGameOver = true;

            Debug.Log("[ShootingGameManager] Player eliminated. Mission compromised.");
            if (_hud != null)
            {
                _hud.ShowDefeatModal(true);
            }
        }

        private void HandleMissionCompleted(MissionData mission, bool victory, int coinsEarned, float duration)
        {
            if (victory)
            {
                // Play victory fanfare sound
                if (_victoryClip != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(_victoryClip, 0.9f);
                }

                // Record map mission progression & unlock next map
                if (_missionProgressManager != null)
                {
                    float healthPercent = (_playerHealth != null && _playerHealth.MaxHealth > 0f)
                        ? (_playerHealth.CurrentHealth / _playerHealth.MaxHealth)
                        : 0.5f;

                    DifficultyLevel diff = (_difficultyManager != null)
                        ? _difficultyManager.CurrentDifficulty
                        : DifficultyLevel.Normal;

                    _missionProgressManager.RecordMissionCompletion(_activeMapId, duration, healthPercent, diff);
                }
            }
        }

        #endregion

        #region Navigation Helpers

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            Screen.orientation = ScreenOrientation.Portrait;

            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(SceneLoader.MAIN_MENU);
            }
            else
            {
                SceneManager.LoadScene(SceneLoader.MAIN_MENU);
            }
        }

        #endregion
    }
}
