namespace Constellation.Save
{
    using System.Collections.Generic;
    using Lifestories.SaveSystem;
    using UnityEngine;

    /// <summary>
    /// Bridges the game's own progression payload to the framework save system.
    /// The framework stores and returns an opaque JSON string so the game owns its schema.
    /// </summary>
    public static class SaveService
    {
        private static ConstellationSaveData _data;

        /// <summary>
        /// The in-memory progression payload. Never null after <see cref="Initialize"/>.
        /// </summary>
        public static ConstellationSaveData Data
        {
            get
            {
                if (_data == null) Initialize();
                return _data;
            }
        }

        /// <summary>
        /// Loads the framework save data and deserializes the game payload from it.
        /// </summary>
        public static void Initialize()
        {
            SaveData frameworkData = SaveSystemManager.Load();

            if (string.IsNullOrEmpty(frameworkData.GameplayPayloadJson))
            {
                _data = new ConstellationSaveData();
                Debug.Log("[SaveService] No existing payload found; starting from defaults.");
                return;
            }

            _data = JsonUtility.FromJson<ConstellationSaveData>(frameworkData.GameplayPayloadJson);

            if (_data == null)
            {
                _data = new ConstellationSaveData();
                Debug.LogWarning("[SaveService] Stored payload could not be read; starting from defaults.");
                return;
            }

            if (_data.Themes == null)
            {
                _data.Themes = new List<ThemeProgress>();
            }

            Debug.Log($"[SaveService] Loaded payload with {_data.Themes.Count} theme records.");
        }

        /// <summary>
        /// Returns the progression record for a constellation, creating it when absent.
        /// </summary>
        /// <param name="constellationId">Identifier of the constellation theme.</param>
        public static ThemeProgress GetTheme(string constellationId)
        {
            if (string.IsNullOrEmpty(constellationId)) return null;

            foreach (ThemeProgress progress in Data.Themes)
            {
                if (progress.ConstellationId == constellationId) return progress;
            }

            var created = new ThemeProgress { ConstellationId = constellationId };
            Data.Themes.Add(created);
            return created;
        }

        /// <summary>
        /// Records a finished run, keeping only the best stars, score and time.
        /// </summary>
        /// <param name="constellationId">Identifier of the constellation theme.</param>
        /// <param name="stars">Stars earned this run, 1-3.</param>
        /// <param name="score">Score achieved this run.</param>
        /// <param name="timeSeconds">Time taken this run, in seconds.</param>
        public static void RecordResult(string constellationId, int stars, int score, float timeSeconds)
        {
            ThemeProgress progress = GetTheme(constellationId);
            if (progress == null) return;

            if (stars > progress.Stars) progress.Stars = Mathf.Clamp(stars, 0, 3);
            if (score > progress.BestScore) progress.BestScore = score;

            if (!progress.Completed || timeSeconds < progress.BestTimeSeconds || progress.BestTimeSeconds <= 0f)
            {
                progress.BestTimeSeconds = timeSeconds;
            }

            progress.Completed = true;

            Debug.Log($"[SaveService] Recorded {constellationId}: stars={progress.Stars} bestScore={progress.BestScore} bestTime={progress.BestTimeSeconds:0.0}s");
            Save();
        }

        /// <summary>
        /// Stores the chosen study timer duration.
        /// </summary>
        /// <param name="seconds">Duration in seconds, or 0 for Zen mode.</param>
        public static void SetStudyDuration(int seconds)
        {
            if (seconds <= 0)
            {
                Data.ZenMode = true;
                Data.StudyDurationSeconds = 0;
            }
            else
            {
                Data.ZenMode = false;
                Data.StudyDurationSeconds = seconds;
            }

            Debug.Log($"[SaveService] Study timer set to {(Data.ZenMode ? "Zen" : Data.StudyDurationSeconds + "s")}.");
            Save();
        }

        /// <summary>
        /// Persists the current payload and audio settings through the framework save system.
        /// </summary>
        public static void Save()
        {
            SaveData frameworkData = SaveSystemManager.CurrentData ?? SaveSystemManager.Load();
            frameworkData.GameplayPayloadJson = JsonUtility.ToJson(Data);
            SaveSystemManager.Save(frameworkData);
        }

        /// <summary>Master output volume, 0-1.</summary>
        public static float MasterVolume
        {
            get => (SaveSystemManager.CurrentData ?? SaveSystemManager.Load()).MasterVolume;
            set
            {
                var data = SaveSystemManager.CurrentData ?? SaveSystemManager.Load();
                data.MasterVolume = Mathf.Clamp01(value);
                SaveSystemManager.Save(data);
            }
        }

        /// <summary>Background music volume, 0-1.</summary>
        public static float MusicVolume
        {
            get => (SaveSystemManager.CurrentData ?? SaveSystemManager.Load()).MusicVolume;
            set
            {
                var data = SaveSystemManager.CurrentData ?? SaveSystemManager.Load();
                data.MusicVolume = Mathf.Clamp01(value);
                SaveSystemManager.Save(data);
            }
        }

        /// <summary>Sound effect volume, 0-1.</summary>
        public static float SfxVolume
        {
            get => (SaveSystemManager.CurrentData ?? SaveSystemManager.Load()).SfxVolume;
            set
            {
                var data = SaveSystemManager.CurrentData ?? SaveSystemManager.Load();
                data.SfxVolume = Mathf.Clamp01(value);
                SaveSystemManager.Save(data);
            }
        }
    }
}
