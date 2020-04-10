using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Buffers
{
    /// <summary>
    ///     Class for receiving and buffering inbound stream data.
    /// </summary>
    internal sealed class InboundBuffer : BufferBase
    {
        public InboundBuffer(long maxData)
        {
            UpdateMaxData(maxData);
        }

        /// <summary>
        ///     Ranges of data which are received, but not delivered.
        /// </summary>
        private RangeSet _undelivered = new RangeSet();

        /// <summary>
        ///     Total number of bytes delivered.
        /// </summary>
        internal long BytesRead { get; private set; }

        /// <summary>
        ///     Final size of the stream. Null if final size is not known yet.
        /// </summary>
        internal long? FinalSize { get; private set; }

        /// <summary>
        ///     Number of bytes ready to be read from the stream.
        /// </summary>
        internal long BytesAvailable => _undelivered.Count > 0 && BytesRead == _undelivered.GetMin()
            ? _undelivered[0].Length
            : 0;

        /// <summary>
        ///     Receives a chunk of data and buffers it for delivery.
        /// </summary>
        /// <param name="offset">Offset on the stream of the received data.</param>
        /// <param name="data">The data to be received.</param>
        internal void Receive(long offset, ReadOnlySpan<byte> data)
        {
            long deliverable = BytesRead + BytesAvailable;
            if (offset + data.Length < deliverable)
            {
                return; // entirely duplicate
            }

            if (offset < deliverable)
            {
                // drop duplicate prefix;
                data = data.Slice((int)(deliverable - offset));
                offset = deliverable;
            }


            // optimized hot path - in-order delivery
            if (_chunks.Count == 0 || _chunks[^1].StreamOffset + _chunks[^1].Length <= offset)
            {
                EnqueueAtEnd(offset, data);
                _undelivered.Add(offset, offset + data.Length - 1);
                return;
            }

            // out of order delivery, use ranges to remove duplicate data
            RangeSet toDeliver = new RangeSet();
            toDeliver.Add(offset, offset + data.Length - 1);
            foreach (var range in _undelivered)
            {
                toDeliver.Remove(range.Start, range.End);
            }

            foreach (var range in toDeliver)
            {
                // find appropriate index
                int index = _chunks.FindLastIndex(c => c.StreamOffset < range.Start) + 1;
                var buffer = ArrayPool<byte>.Shared.Rent((int)range.Length);
                data.Slice((int) (range.Start - offset), (int) range.Length).CopyTo(buffer);

                _chunks.Insert(index, new StreamChunk(range.Start, buffer, range.Length));
            }

            _undelivered.Add(offset, offset + data.Length - 1);
        }

        /// <summary>
        ///     Delivers the buffered data to the target destination span.
        /// </summary>
        /// <param name="destination"></param>
        internal int Deliver(Span<byte> destination)
        {
            if (BytesAvailable < destination.Length)
            {
                destination = destination.Slice(0, (int) BytesAvailable);
            }

            int delivered = destination.Length;
            _undelivered.Remove(BytesRead, destination.Length);

            int index = 0;
            while (destination.Length > 0)
            {
                int inChunkOffset = (int) (BytesRead - _chunks[index].StreamOffset);
                int inChunkLength = Math.Min((int) _chunks[index].Length, destination.Length);

                _chunks[index].Buffer.AsSpan(inChunkOffset, inChunkLength).CopyTo(destination);

                destination = destination.Slice(inChunkLength);

                BytesRead += inChunkLength;
                index++;
            }

            DiscardDataUntil(BytesRead);
            return delivered;
        }

        /// <summary>
        ///     Processes all buffered readable data using provided callback.
        /// </summary>
        /// <param name="process"></param>
        internal void Deliver(Action<ArraySegment<byte>> process)
        {
            if (BytesAvailable == 0) return;

            long deliverable = _undelivered[0].End;
            int index = 0;
            while (_chunks[index].StreamOffset < deliverable)
            {
                process(new ArraySegment<byte>(_chunks[index].Buffer, 0, (int) _chunks[index].Length));
                index++;
            }

            BytesRead = deliverable;
            DiscardDataUntil(BytesRead);
        }

        /// <summary>
        ///     Drops buffers containing data lesser than given offset.
        /// </summary>
        /// <param name="offset">Minimum offset to be kept.</param>
        private void DiscardDataUntil(long offset)
        {
            int toRemove = _chunks.FindIndex(c => c.StreamOffset + c.Length > offset);
            if (toRemove < 0)
                toRemove = _chunks.Count;

            for (int i = 0; i < toRemove; i++)
            {
                ArrayPool<byte>.Shared.Return(_chunks[i].Buffer);
            }

            _chunks.RemoveRange(0, toRemove);
        }

        /// <summary>
        ///     Skips given amount of data. Useful if the data was delivered by different means (without buffering).
        /// </summary>
        /// <param name="length">Number of bytes from the payload to be skipped.</param>
        public void Skip(long length)
        {
            _undelivered.Remove(BytesRead, BytesRead + length - 1);
            BytesRead += length;
            DiscardDataUntil(BytesRead);
        }
    }
}
