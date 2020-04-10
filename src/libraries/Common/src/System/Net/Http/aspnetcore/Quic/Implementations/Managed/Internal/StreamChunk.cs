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
        ///     Buffer holding the data, only first <see cref="Length"/> bytes are used.
        /// </summary>
        internal readonly byte[] Buffer;

        /// <summary>
        ///     Length of the chunk and number of bytes actually stored in the <see cref="Buffer"/>.
        /// </summary>
        internal readonly long Length;

        public StreamChunk(long streamOffset, byte[] buffer, long length)
        {
            StreamOffset = streamOffset;
            Buffer = buffer;
            Length = length;
        }
    }
}
