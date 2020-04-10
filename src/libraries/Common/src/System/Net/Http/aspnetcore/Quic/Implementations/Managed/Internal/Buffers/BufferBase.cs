using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Buffers
{
    /// <summary>
    ///     Base Shared logic for <see cref="InboundBuffer"/> and <see cref="OutboundBuffer"/>.
    /// </summary>
    internal class BufferBase
    {
        /// <summary>
        ///     Individual, deduplicated parts of the stream, ordered by stream offset.
        /// </summary>
        protected List<StreamChunk> _chunks = new List<StreamChunk>();

        /// <summary>
        ///     Total number of bytes allowed to transport in this stream.
        /// </summary>
        protected long MaxData { get; private set; }

        /// <summary>
        ///     Updates the <see cref="MaxData"/> parameter.
        /// </summary>
        /// <param name="value">Value of the parameter.</param>
        internal void UpdateMaxData(long value)
        {
            MaxData = Math.Max(MaxData, value);
        }

        /// <summary>
        ///     Optimized code for the case when data are added without duplication and at the end of the buffer.
        /// </summary>
        /// <param name="offset">Offset at which the data is enqueued</param>
        /// <param name="data">Data segment to be added.</param>
        protected void EnqueueAtEnd(long offset, ReadOnlySpan<byte> data)
        {
            Debug.Assert(_chunks.Count == 0 || _chunks[^1].StreamOffset + _chunks[^1].Length <= offset);

            // utilize unused space in the last chunk if data perfectly matches the end
            if (_chunks.Count > 0 && _chunks[^1].Length < _chunks[^1].Buffer.Length &&
                _chunks[^1].StreamOffset + _chunks[^1].Length == offset)
            {
                var last = _chunks[^1];
                int copied = Math.Min(last.Buffer.Length - (int)last.Length, data.Length);
                data.Slice(0, copied).CopyTo(last.Buffer.AsSpan((int)last.Length));
                _chunks[^1] = new StreamChunk(last.StreamOffset, last.Buffer, last.Length + copied);

                data = data.Slice(copied);
                offset += copied;

                // avoid renting zero array
                if (data.IsEmpty) return;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            data.CopyTo(buffer);

            _chunks.Add(new StreamChunk(offset, buffer, data.Length));
        }
    }
}
