namespace Lifestories.Pause
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Central pause coordinator. Pausable components register here and receive
    /// <see cref="IPausable.Pause"/> and <see cref="IPausable.Resume"/> callbacks, and the manager
    /// freezes <see cref="Time.timeScale"/> for the duration of the pause.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        [Tooltip("Time scale applied while the game is paused. Zero freezes physics, animation and delta-time driven logic.")]
        [SerializeField] private float _pausedTimeScale = 0f;

        private static readonly List<IPausable> _pausables = new List<IPausable>();
        private static readonly List<IPausable> _dispatchBuffer = new List<IPausable>();
        private static PauseManager _instance;
        private static float _timeScaleBeforePause = 1f;

        /// <summary>
        /// True while the game is paused.
        /// </summary>
        public static bool IsPaused { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Only a root object can be marked persistent. When the manager sits under the
            // PersistentManager root it already survives scene loads, and the call would log a warning.
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _instance = null;

            if (IsPaused)
            {
                ResumeGame();
            }
        }

        // Static state survives play sessions when domain reload is disabled, so it must be reset explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void resetStatics()
        {
            _pausables.Clear();
            _dispatchBuffer.Clear();
            IsPaused = false;
            _timeScaleBeforePause = 1f;
        }

        /// <summary>
        /// Registers a pausable component so it receives pause and resume callbacks.
        /// </summary>
        /// <param name="pausable">The component to register.</param>
        public static void Register(IPausable pausable)
        {
            if (pausable == null || _pausables.Contains(pausable))
            {
                return;
            }

            _pausables.Add(pausable);
        }

        /// <summary>
        /// Removes a pausable component from the pause notification list.
        /// </summary>
        /// <param name="pausable">The component to unregister.</param>
        public static void Unregister(IPausable pausable)
        {
            if (pausable == null)
            {
                return;
            }

            _pausables.Remove(pausable);
        }

        /// <summary>
        /// Toggles between the paused and running states.
        /// </summary>
        public static void TogglePause()
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /// <summary>
        /// Pauses the game, freezes time and notifies every registered pausable component.
        /// </summary>
        public static void PauseGame()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = _instance != null ? _instance._pausedTimeScale : 0f;
            dispatchToPausables(true);
        }

        /// <summary>
        /// Resumes the game, restores the previous time scale and notifies every registered pausable component.
        /// </summary>
        public static void ResumeGame()
        {
            bool wasPaused = IsPaused;
            IsPaused = false;

            if (wasPaused)
            {
                Time.timeScale = _timeScaleBeforePause;
            }

            dispatchToPausables(false);
        }

        // Iterates a snapshot so a Pause or Resume implementation may safely register or unregister itself.
        private static void dispatchToPausables(bool shouldPause)
        {
            _dispatchBuffer.Clear();
            _dispatchBuffer.AddRange(_pausables);

            for (int index = 0; index < _dispatchBuffer.Count; index++)
            {
                IPausable pausable = _dispatchBuffer[index];

                // A destroyed MonoBehaviour still answers the interface, so check it as a Unity object.
                if (pausable is Object unityObject && unityObject == null)
                {
                    _pausables.Remove(pausable);
                    continue;
                }

                if (shouldPause)
                {
                    pausable.Pause();
                }
                else
                {
                    pausable.Resume();
                }
            }

            _dispatchBuffer.Clear();
        }
    }
}
