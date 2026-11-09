using System;
using UnityEngine;

namespace YashGame.Shared
{
    /// <summary>
    /// Central game manager that tracks game state and player profile data.
    /// Persists across scene loads as a singleton.
    /// </summary>
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        #region Enums

        /// <summary>
        /// All possible game states.
        /// </summary>
        public enum GameState
        {
            MainMenu,
            Loading,
            ShootingGame,
            RacingGame,
            Settings,
            Paused
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever the game state changes. Provides the previous and new states.
        /// </summary>
        public event Action<GameState, GameState> OnGameStateChanged;

        #endregion

        #region Fields

        [Header("Player Profile")]
        [SerializeField, Tooltip("Default player name if none is saved.")]
        private string _defaultPlayerName = "Player";

        private GameState _currentState = GameState.MainMenu;
        private GameState _previousState = GameState.MainMenu;

        // PlayerPrefs keys
        private const string PREF_PLAYER_NAME = "YashGame_PlayerName";
        private const string PREF_PLAYER_LEVEL = "YashGame_PlayerLevel";

        #endregion

        #region Properties

        /// <summary>
        /// The current game state.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// The previous game state (before the last transition).
        /// </summary>
        public GameState PreviousState => _previousState;

        /// <summary>
        /// The player's display name. Saved to and loaded from PlayerPrefs.
        /// </summary>
        public string PlayerName
        {
            get => PlayerPrefs.GetString(PREF_PLAYER_NAME, _defaultPlayerName);
            set
            {
                PlayerPrefs.SetString(PREF_PLAYER_NAME, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// The player's current level. Saved to and loaded from PlayerPrefs.
        /// </summary>
        public int PlayerLevel
        {
            get => PlayerPrefs.GetInt(PREF_PLAYER_LEVEL, 1);
            set
            {
                PlayerPrefs.SetInt(PREF_PLAYER_LEVEL, Mathf.Max(1, value));
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region Unity Lifecycle

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Changes the current game state and fires the <see cref="OnGameStateChanged"/> event.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void SetGameState(GameState newState)
        {
            if (_currentState == newState)
            {
                Debug.Log($"[GameManager] Already in state {newState}. Ignoring.");
                return;
            }

            _previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] State changed: {_previousState} -> {_currentState}");
            OnGameStateChanged?.Invoke(_previousState, _currentState);
        }

        /// <summary>
        /// Resets the player profile to default values.
        /// </summary>
        public void ResetPlayerProfile()
        {
            PlayerName = _defaultPlayerName;
            PlayerLevel = 1;
            Debug.Log("[GameManager] Player profile reset to defaults.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the game manager on startup.
        /// Ensures default player profile values are in place.
        /// </summary>
        private void Initialize()
        {
            // Ensure a player name exists
            if (!PlayerPrefs.HasKey(PREF_PLAYER_NAME))
            {
                PlayerName = _defaultPlayerName;
            }

            if (!PlayerPrefs.HasKey(PREF_PLAYER_LEVEL))
            {
                PlayerLevel = 1;
            }

            _currentState = GameState.MainMenu;
            _previousState = GameState.MainMenu;

            Debug.Log($"[GameManager] Initialized. Player: {PlayerName}, Level: {PlayerLevel}");
        }

        #endregion
    }
}
