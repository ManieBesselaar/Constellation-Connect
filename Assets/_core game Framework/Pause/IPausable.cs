namespace Lifestories.Pause
{
    /// <summary>
    /// Implemented by components that must react to a pause or resume request.
    /// Register with <see cref="PauseManager.Register"/> in OnEnable and unregister with
    /// <see cref="PauseManager.Unregister"/> in OnDisable.
    /// </summary>
    public interface IPausable
    {
        /// <summary>
        /// Called when the game is paused.
        /// </summary>
        void Pause();

        /// <summary>
        /// Called when the game is resumed.
        /// </summary>
        void Resume();
    }
}
