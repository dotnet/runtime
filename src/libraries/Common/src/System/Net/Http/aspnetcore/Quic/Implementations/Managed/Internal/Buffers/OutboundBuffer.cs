using System.Buffers;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Buffers
{
    /// <summary>
    ///     Structure for containing outbound stream data.
    /// </summary>
    internal sealed class OutboundBuffer : BufferBase
    {
        public OutboundBuffer(long maxData)
        {
            UpdateMaxData(maxData);
        }

        /// <summary>
        ///     Ranges of bytes awaiting to be sent.
        /// </summary>
        private RangeSet _pending = new RangeSet();

        /// <summary>
        ///     Ranges of bytes currently in-flight.
        /// </summary>
        private RangeSet _checkedOut = new RangeSet();

        /// <summary>
        ///     Total number of bytes written into this stream.
        /// </summary>
        internal long WrittenBytes { get; private set; }

        /// <summary>
        ///     True if the stream is closed for further writing (no more data can be added).
        /// </summary>
        internal bool SizeKnown { get; private set; }

        /// <summary>
        ///     True if all data has been transmitted and acknowledged.
        /// </summary>
        internal bool Finished => SizeKnown && !HasUnackedData;

        /// <summary>
        ///     Returns true if buffer contains any readable data.
        /// </summary>
        internal bool HasPendingData => _pending.Count > 0 && _pending[0].Start < MaxData;

        /// <summary>
        ///     True if there is data that has not been confirmed received.
        /// </summary>
        internal bool HasUnackedData => _pending.Count + _checkedOut.Count != 0;

        /// <summary>
        ///     Returns length of the next contiguous range of data that can be checked out, respecting the <see cref="BufferBase.MaxData"/> parameter.
        /// </summary>
        /// <returns></returns>
        internal (long offset, long count) GetNextSendableRange()
        {
            if (_pending.Count == 0) return (WrittenBytes, 0);

            long sendableLength = MaxData - _pending[0].Start;
            long count = Math.Min(sendableLength, _pending[0].Length);
            Debug.Assert(count > 0);
            return (_pending[0].Start, count);
        }

        /// <summary>
        ///     Reads data from the stream into provided span.
        /// </summary>
        /// <param name="destination">Destination memory for the data.</param>
        internal void CheckOut(Span<byte> destination)
        {
            if (destination.IsEmpty)
                return;

            Debug.Assert(destination.Length <= GetNextSendableRange().count);

            long start = _pending.GetMin();
            long end = start + destination.Length - 1;

            _pending.Remove(start, end);
            _checkedOut.Add(start, end);

            int copied = 0;
            int i = _chunks.FindIndex(c => c.StreamOffset + c.Length >= start);
            while (copied < destination.Length)
            {
                int inChunkStart = (int)(start - _chunks[i].StreamOffset) + copied;
                int inChunkCount = Math.Min((int)_chunks[i].Length - inChunkStart, destination.Length - copied);
                _chunks[i].Buffer.AsSpan(inChunkStart, inChunkCount).CopyTo(destination.Slice(copied));

                copied += inChunkCount;
                i++;
            }
        }

        /// <summary>
        ///     Adds data to the stream.
        /// </summary>
        /// <param name="data">Data to be sent.</param>
        internal void Enqueue(ReadOnlySpan<byte> data)
        {
            Debug.Assert(!SizeKnown, "Trying to add data to finished OutboundBuffer");

            _pending.Add(WrittenBytes, WrittenBytes + data.Length - 1);
            EnqueueAtEnd(WrittenBytes, data);
            WrittenBytes += data.Length;
        }

        /// <summary>
        ///     Marks the stream as finished, no more data can be added to the stream.
        /// </summary>
        internal void MarkEndOfData()
        {
            SizeKnown = true;
        }

        /// <summary>
        ///     Called to inform the buffer that transmission of given range was not successful.
        /// </summary>
        /// <param name="offset">Start of the range.</param>
        /// <param name="count">Number of bytes lost.</param>
        internal void OnLost(long offset, long count)
        {
            long end = offset + count - 1;

            Debug.Assert(_checkedOut.Includes(offset, end));
            Debug.Assert(!_pending.Includes(offset, end));

            _checkedOut.Remove(offset, end);
            _pending.Add(offset, end);
        }

        /// <summary>
        ///     Called to inform the buffer that transmission of given range was successful.
        /// </summary>
        /// <param name="offset">Start of the range.</param>
        /// <param name="count">Number of bytes acked.</param>
        internal void OnAck(long offset, long count)
        {
            long end = offset + count - 1;
            Debug.Assert(_checkedOut.Includes(offset, end));

            _checkedOut.Remove(offset, end);

            // release unneeded data
            long processed = (_pending.Count, _checkedOut.Count) switch
            {
                (0, 0) => WrittenBytes,
                (0, _) => _checkedOut.GetMin(),
                (_, 0) => _pending.GetMin(),
                _ => Math.Min(_pending.GetMin(), _checkedOut.GetMin())
            };

            // index of first chunk with unsent data is the same as count of unneeded chunks that are before
            int toRemove = _chunks.FindIndex(c => c.StreamOffset + c.Length > processed);
            if (toRemove == -1)
            {
                toRemove = _chunks.Count;
            }

            for (int i = 0; i < toRemove; i++)
            {
                ArrayPool<byte>.Shared.Return(_chunks[i].Buffer);
            }

            _chunks.RemoveRange(0, toRemove);
        }
    }
}
