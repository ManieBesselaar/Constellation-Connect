namespace Lifestories.SaveSystem
{
    using System;

    /// <summary>
    /// Serializable container holding all persistent player settings and gameplay progression state.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string SelectedLanguageCode = "en";
        public float MasterVolume = 1.0f;
        public float MusicVolume = 1.0f;
        public float DialogueVolume = 1.0f;
        public float SfxVolume = 1.0f;
        public int CurrentCheckpointIndex = 0;

        /// <summary>
        /// Opaque JSON payload owned by the consuming game. The framework never inspects or
        /// mutates it, so a game can persist its own progression without forking this type.
        /// </summary>
        public string GameplayPayloadJson = string.Empty;

        /// <summary>
        /// Resets data to default factory settings.
        /// </summary>
        public void ResetToDefaults()
        {
            SelectedLanguageCode = "en";
            MasterVolume = 1.0f;
            MusicVolume = 1.0f;
            DialogueVolume = 1.0f;
            SfxVolume = 1.0f;
            CurrentCheckpointIndex = 0;
            GameplayPayloadJson = string.Empty;
        }
    }
}