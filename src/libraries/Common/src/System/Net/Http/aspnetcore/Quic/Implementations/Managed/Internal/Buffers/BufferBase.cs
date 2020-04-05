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
        ///     Optimized code for the case when data are added without duplication and at the end of the buffer.
        /// </summary>
        /// <param name="data">Data segment to be added.</param>
        protected void EnqueueAtEnd(ulong offset, ReadOnlySpan<byte> data)
        {
            Debug.Assert(_chunks.Count == 0 || _chunks[^1].StreamOffset + _chunks[^1].Length <= offset);

            // utilize unused space in the last chunk if data perfectly matches the end
            if (_chunks.Count > 0 && _chunks[^1].Length < (ulong) _chunks[^1].Buffer.Length &&
                _chunks[^1].StreamOffset + _chunks[^1].Length == offset)
            {
                var last = _chunks[^1];
                int copied = Math.Min(last.Buffer.Length - (int)last.Length, data.Length);
                data.Slice(0, copied).CopyTo(last.Buffer.AsSpan((int)last.Length));
                _chunks[^1] = new StreamChunk(last.StreamOffset, last.Buffer, last.Length + (ulong) copied);

                data = data.Slice(copied);
                offset += (ulong) copied;

                // avoid renting zero array
                if (data.IsEmpty) return;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            data.CopyTo(buffer);

            _chunks.Add(new StreamChunk(offset, buffer, (ulong) data.Length));
        }
    }
}
