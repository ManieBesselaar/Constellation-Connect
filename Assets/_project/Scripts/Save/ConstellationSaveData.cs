namespace Constellation.Save
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Per-theme progression record persisted inside the framework save payload.
    /// </summary>
    [Serializable]
    public class ThemeProgress
    {
        /// <summary>Identifier of the constellation this record belongs to.</summary>
        public string ConstellationId;

        /// <summary>Best star rating earned, 0-3.</summary>
        public int Stars;

        /// <summary>Best score achieved on this constellation.</summary>
        public int BestScore;

        /// <summary>Fastest recorded completion time in seconds, 0 when never completed.</summary>
        public float BestTimeSeconds;

        /// <summary>True once the constellation has been completed at least once.</summary>
        public bool Completed;
    }

    /// <summary>
    /// The game-owned payload stored opaquely inside <c>SaveData.GameplayPayloadJson</c>.
    /// </summary>
    [Serializable]
    public class ConstellationSaveData
    {
        /// <summary>Selected study timer duration in seconds. Ignored when <see cref="ZenMode"/> is set.</summary>
        public int StudyDurationSeconds = 60;

        /// <summary>True when study mode has no countdown.</summary>
        public bool ZenMode;

        /// <summary>True when the player has muted the music channel without losing its volume setting.</summary>
        public bool MusicMuted;

        /// <summary>One progression record per constellation theme.</summary>
        public List<ThemeProgress> Themes = new List<ThemeProgress>();
    }
}
