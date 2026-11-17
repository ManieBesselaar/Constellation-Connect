namespace Lifestories.SaveSystem
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Manages saving and loading game data across different platforms using JSON serialization.
    /// </summary>
    public static class SaveSystemManager
    {
        private const string SAVE_PREF_KEY = "Lifestories_SaveData_JSON";

        /// <summary>
        /// The save data currently in memory. Null until <see cref="Load"/> or <see cref="Save(SaveData)"/> runs.
        /// </summary>
        public static SaveData CurrentData { get; private set; }

        // Static state survives play sessions when domain reload is disabled, so it must be reset explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void resetStatics()
        {
            CurrentData = null;
        }

        /// <summary>
        /// Serializes and saves the active in-memory data container to persistent storage.
        /// </summary>
        public static void Save()
        {
            Save(CurrentData);
        }

        /// <summary>
        /// Serializes and saves the supplied data container to persistent storage.
        /// </summary>
        /// <param name="data">The save data instance to store.</param>
        public static void Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveSystemManager] Save ignored: the supplied SaveData instance is null. Call Load() first.");
                return;
            }

            CurrentData = data;
            string json = JsonUtility.ToJson(data, true);

            // WebGL and cross-platform safe string storage
            PlayerPrefs.SetString(SAVE_PREF_KEY, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads existing save data from persistent storage, or creates a default instance if none exists or the stored JSON is unreadable.
        /// </summary>
        /// <returns>The loaded or newly initialized SaveData instance, also cached in <see cref="CurrentData"/>.</returns>
        public static SaveData Load()
        {
            if (!PlayerPrefs.HasKey(SAVE_PREF_KEY))
            {
                CurrentData = new SaveData();
                return CurrentData;
            }

            string json = PlayerPrefs.GetString(SAVE_PREF_KEY, string.Empty);
            SaveData data = null;

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    data = JsonUtility.FromJson<SaveData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[SaveSystemManager] Stored save data could not be read and was replaced with defaults: {exception.Message}");
                }
            }

            CurrentData = data ?? new SaveData();
            return CurrentData;
        }

        /// <summary>
        /// Clears all stored save data and resets the in-memory container to factory defaults.
        /// </summary>
        public static void DeleteSaveData()
        {
            PlayerPrefs.DeleteKey(SAVE_PREF_KEY);
            PlayerPrefs.Save();
            CurrentData = new SaveData();
        }
    }
}
