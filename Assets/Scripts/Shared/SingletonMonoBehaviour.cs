using UnityEngine;

namespace YashGame.Shared
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Provides thread-safe lazy initialization and optional DontDestroyOnLoad persistence.
    /// </summary>
    /// <typeparam name="T">The concrete MonoBehaviour type that inherits this singleton.</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Fields

        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting;

        [Header("Singleton Settings")]
        [SerializeField, Tooltip("If true, this object will persist across scene loads.")]
        private bool _dontDestroyOnLoad = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the singleton instance. Creates one if it doesn't exist.
        /// Thread-safe via double-checked locking.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning(
                        $"[SingletonMonoBehaviour] Instance of {typeof(T)} requested after application quit. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            var singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                            _instance = singletonObject.AddComponent<T>();
                            Debug.Log(
                                $"[SingletonMonoBehaviour] Created new instance of {typeof(T).Name}.");
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Returns true if a singleton instance currently exists (without creating one).
        /// </summary>
        public static bool HasInstance => _instance != null;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Enforces singleton pattern and optionally marks as DontDestroyOnLoad.
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this as T)
            {
                Debug.LogWarning(
                    $"[SingletonMonoBehaviour] Duplicate instance of {typeof(T).Name} detected. Destroying duplicate on '{gameObject.name}'.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            if (_dontDestroyOnLoad)
            {
                // Ensure we are a root object before calling DontDestroyOnLoad
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Called when the MonoBehaviour will be destroyed.
        /// Cleans up the static instance reference.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this as T)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Called when the application is quitting.
        /// Prevents creation of ghost singleton instances during shutdown.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        #endregion
    }
}
