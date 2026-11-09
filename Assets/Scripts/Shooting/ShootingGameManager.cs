using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Master coordinator for the 3D third-person shooting game.
    /// Builds the battlefield map, spawns player and enemy bots, tracks elimination objectives,
    /// manages victory and defeat states, and routes scene returns back to the YASH GAME main menu.
    /// </summary>
    public class ShootingGameManager : MonoBehaviour
    {
        #region Static Singleton

        public static ShootingGameManager Instance { get; private set; }

        #endregion

        #region Serialized Settings

        [Header("Mission Objectives")]
        [SerializeField, Tooltip("Target enemies to eliminate.")]
        private int _totalTargets = 5;

        [Header("Audio")]
        [SerializeField, Tooltip("Victory fanfare sound.")]
        private AudioClip _victoryClip;

        #endregion

        #region Public Properties

        public int TotalTargets => _totalTargets;
        public int EliminatedTargets => _eliminatedTargets;
        public bool IsGameOver => _isGameOver;
        public GameObject PlayerObject { get; private set; }

        #endregion

        #region Private Fields

        private int _eliminatedTargets = 0;
        private bool _isGameOver = false;

        private BattlefieldBuilder _battlefieldBuilder;
        private ShootingHUD _hud;
        private MobileInputController _input;
        private ThirdPersonCamera _cameraController;
        private AudioSource _audioSource;

        private List<GameObject> _activeEnemies = new List<GameObject>();

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

            SetupEnvironmentAndGameplay();
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

        #region Game Setup & Spawning

        private void SetupEnvironmentAndGameplay()
        {
            // 1. Input Controller
            if (_input == null)
            {
                _input = FindObjectOfType<MobileInputController>();
                if (_input == null)
                {
                    GameObject inputObj = new GameObject("MobileInputController");
                    _input = inputObj.AddComponent<MobileInputController>();
                }
            }

            // 2. Build 3D Battlefield Map
            _battlefieldBuilder = GetComponent<BattlefieldBuilder>();
            if (_battlefieldBuilder == null)
            {
                _battlefieldBuilder = gameObject.AddComponent<BattlefieldBuilder>();
            }
            _battlefieldBuilder.BuildBattlefield();

            // 3. Setup Camera
            SetupCamera();

            // 4. Spawn Player Character
            SpawnPlayer();

            // 5. Spawn Enemy Combat Bots
            SpawnEnemyBots();

            // 6. Setup Shooting HUD
            SetupHUD();
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

            // Directional Light
            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightObj = new GameObject("SunLight");
                Light sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.95f, 0.88f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                lightObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }

            _cameraController = mainCam.GetComponent<ThirdPersonCamera>();
            if (_cameraController == null)
            {
                _cameraController = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
            }
        }

        private void SpawnPlayer()
        {
            Vector3 spawnPos = new Vector3(0f, 0.5f, -38f);
            if (_battlefieldBuilder != null && _battlefieldBuilder.PlayerSpawnPoint != null)
            {
                spawnPos = _battlefieldBuilder.PlayerSpawnPoint.position;
            }

            PlayerObject = new GameObject("PlayerCharacter");
            PlayerObject.tag = "Player";
            PlayerObject.transform.position = spawnPos;

            // Add Components
            PlayerMovement pm = PlayerObject.AddComponent<PlayerMovement>();
            PlayerHealth ph = PlayerObject.AddComponent<PlayerHealth>();
            WeaponController weapon = PlayerObject.AddComponent<WeaponController>();

            // Connect Camera
            if (_cameraController != null)
            {
                _cameraController.SetTarget(PlayerObject.transform);
            }
        }

        private void SpawnEnemyBots()
        {
            _activeEnemies.Clear();
            _eliminatedTargets = 0;

            if (_battlefieldBuilder == null || _battlefieldBuilder.EnemySpawnPoints.Count == 0) return;

            int count = Mathf.Min(_totalTargets, _battlefieldBuilder.EnemySpawnPoints.Count);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = _battlefieldBuilder.EnemySpawnPoints[i];
                GameObject bot = new GameObject($"EnemyBot_{i + 1}");
                bot.tag = "Enemy";
                bot.transform.position = spawnPos + Vector3.up * 0.2f;

                EnemyHealth eh = bot.AddComponent<EnemyHealth>();
                EnemyController ec = bot.AddComponent<EnemyController>();

                if (i < _battlefieldBuilder.EnemyPatrolRoutes.Count)
                {
                    ec.SetWaypoints(_battlefieldBuilder.EnemyPatrolRoutes[i]);
                }

                _activeEnemies.Add(bot);
            }
        }

        private void SetupHUD()
        {
            _hud = FindObjectOfType<ShootingHUD>();
            if (_hud == null)
            {
                GameObject hudObj = new GameObject("ShootingHUD");
                _hud = hudObj.AddComponent<ShootingHUD>();
            }

            _hud.UpdateObjectiveUI(_eliminatedTargets, _totalTargets);
            _hud.HookPlayerComponents();
        }

        #endregion

        #region Match Progression & Objectives

        public void OnTargetEliminated(GameObject enemy, int scoreValue)
        {
            if (_isGameOver) return;

            _eliminatedTargets++;
            if (_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Remove(enemy);
            }

            if (_hud != null)
            {
                _hud.UpdateObjectiveUI(_eliminatedTargets, _totalTargets);
            }

            Debug.Log($"[ShootingGameManager] Target eliminated! ({_eliminatedTargets}/{_totalTargets})");

            if (_eliminatedTargets >= _totalTargets)
            {
                StartCoroutine(VictorySequenceRoutine());
            }
        }

        public void OnPlayerDefeated()
        {
            if (_isGameOver) return;
            _isGameOver = true;

            Debug.Log("[ShootingGameManager] Mission Failed: Player operative eliminated.");
            StartCoroutine(DefeatSequenceRoutine());
        }

        private IEnumerator VictorySequenceRoutine()
        {
            _isGameOver = true;
            Debug.Log("[ShootingGameManager] ALL TARGETS ELIMINATED! MISSION ACCOMPLISHED!");

            if (_victoryClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_victoryClip, 0.9f);
            }

            yield return new WaitForSeconds(1.2f);

            if (_hud != null)
            {
                _hud.ShowVictoryModal(true);
            }
        }

        private IEnumerator DefeatSequenceRoutine()
        {
            yield return new WaitForSeconds(1.2f);

            if (_hud != null)
            {
                _hud.ShowDefeatModal(true);
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
