namespace Constellation.Gameplay
{
    using Constellation.Data;

    /// <summary>
    /// A target that accepts a dragged tile.
    /// </summary>
    public interface IDropTarget
    {
        /// <summary>The verse this target belongs to.</summary>
        string VerseId { get; }

        /// <summary>The chunk this target expects, or null for a bridge junction.</summary>
        PhraseChunk ExpectedChunk { get; }

        /// <summary>True once a correct chunk has been placed here.</summary>
        bool IsFilled { get; }

        /// <summary>Places a chunk when it matches, returning whether it was accepted.</summary>
        bool TryAccept(PhraseChunk chunk);

        /// <summary>Pulses the target to draw the player's eye.</summary>
        void Pulse();
    }
}
