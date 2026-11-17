namespace Lifestories.SaveSystem
{
    /// <summary>
    /// Interface implemented by components requiring state persistence.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Populates the shared save data instance with current component state.
        /// </summary>
        /// <param name="data">The active save data container.</param>
        public void PopulateSaveData(SaveData data);

        /// <summary>
        /// Restores component state from the loaded save data instance.
        /// </summary>
        /// <param name="data">The loaded save data container.</param>
        public void LoadSaveData(SaveData data);
    }
}