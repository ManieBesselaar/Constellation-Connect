namespace Constellation.Gameplay
{
    using Constellation.Data;

    /// <summary>
    /// A tile that can be picked up and dragged onto a drop target.
    /// </summary>
    public interface IDraggableTile
    {
        /// <summary>The chunk this tile carries.</summary>
        PhraseChunk Chunk { get; }

        /// <summary>True while the tile is held by the pointer.</summary>
        bool IsDragging { get; }

        /// <summary>Returns the tile to its resting position.</summary>
        void ReturnToOrigin();
    }
}
