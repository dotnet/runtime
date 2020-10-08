using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Holds data from a particular contiguous segment of a stream.
    /// </summary>
    internal readonly struct StreamChunk
    {
        /// <summary>
        ///     Comparer for sorting instances of <see cref="StreamChunk"/> by their <see cref="StreamOffset"/>.
        /// </summary>
        internal static Comparer<StreamChunk> OffsetComparer =
            Comparer<StreamChunk>.Create((left, right) => left.StreamOffset.CompareTo(right.StreamOffset));

        /// <summary>
        ///     Offset in the stream where the data start.
        /// </summary>
        internal readonly long StreamOffset;

        /// <summary>
        ///     Memory block to be sent.
        /// </summary>
        internal readonly ReadOnlyMemory<byte> Memory;

        /// <summary>
        ///     Source buffer for <see cref="Memory"/>, if the backing array was pooled. Null if the memory came from user.
        /// </summary>
        internal readonly byte[]? Buffer;

        /// <summary>
        ///     Length of the chunk and number of bytes actually stored in the <see cref="Buffer"/>.
        /// </summary>
        internal int Length => Memory.Length;

        public StreamChunk(long streamOffset, ReadOnlyMemory<byte> memory, byte[]? buffer = null)
        {
            StreamOffset = streamOffset;
            Memory = memory;
            Buffer = buffer;
        }
    }
}
