using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Class for receiving and buffering inbound stream data.
    /// </summary>
    internal class InboundBuffer
    {
        /// <summary>
        ///     Chunks of data awaiting delivery, kept sorted by stream offset.
        /// </summary>
        private List<StreamChunk> _chunks = new List<StreamChunk>();

        /// <summary>
        ///     Ranges of data which are received, but not delivered.
        /// </summary>
        private RangeSet _undelivered = new RangeSet();

        /// <summary>
        ///     Total number of bytes delivered.
        /// </summary>
        internal ulong BytesRead { get; private set; }

        /// <summary>
        ///     Number of bytes ready to be read from the stream.
        /// </summary>
        internal ulong BytesAvailable => _undelivered.Count > 0 && BytesRead == _undelivered.GetMin()
            ? _undelivered[0].Length
            : 0;

        /// <summary>
        ///     Receives a chunk of data and buffers it for delivery.
        /// </summary>
        /// <param name="offset">Offset on the stream of the received data.</param>
        /// <param name="data">The data to be received.</param>
        internal void Receive(ulong offset, ReadOnlySpan<byte> data)
        {
            ulong deliverable = BytesRead + BytesAvailable;
            if (offset + (ulong)data.Length < deliverable)
            {
                return; // entirely duplicate
            }

            if (offset < deliverable)
            {
                // drop duplicate prefix;
                data = data.Slice((int)(deliverable - offset));
                offset = deliverable;
            }

            _undelivered.Add(offset, offset + (ulong)data.Length - 1);

            // find place to insert the new chunk
            // TODO-RZ: use binary search
            ulong ulongLength = (ulong)data.Length;
            int index = _chunks.FindIndex(c => c.StreamOffset >= offset + ulongLength);

            if (index == -1)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
                data.CopyTo(buffer);
                // we are done
                _chunks.Add(new StreamChunk(offset, buffer, ulongLength));
                return;
            }

            // TODO-RZ: manage duplicate and out of order data
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Delivers the buffered data to the target destination span.
        /// </summary>
        /// <param name="destination"></param>
        internal void Deliver(Span<byte> destination)
        {
            Debug.Assert(BytesAvailable >= (ulong) destination.Length);
            _undelivered.Remove(BytesRead, (ulong) destination.Length);

            int written = 0;
            while (destination.Length > 0)
            {
                int inChunkOffset = (int) (BytesRead - _chunks[0].StreamOffset);
                int inChunkLength = Math.Min((int) _chunks[0].Length, destination.Length);

                _chunks[0].Buffer.AsSpan(inChunkOffset, inChunkLength).CopyTo(destination);

                destination = destination.Slice(inChunkLength);

                // TODO-RZ: remove chunks all at once for better efficiency
                if (_chunks[0].Length == (ulong) inChunkLength)
                {
                    ArrayPool<byte>.Shared.Return(_chunks[0].Buffer);
                    _chunks.RemoveAt(0);
                }

                BytesRead += (ulong) inChunkLength;
            }
        }

        /// <summary>
        ///     Skips given amount of data. Useful if the data was delivered by different means (without buffering).
        /// </summary>
        /// <param name="length">Number of bytes from the payload to be skipped.</param>
        public void Skip(ulong length)
        {
            BytesRead += length;
            // TODO-RZ: drop obsolete chunks
        }
    }
}
