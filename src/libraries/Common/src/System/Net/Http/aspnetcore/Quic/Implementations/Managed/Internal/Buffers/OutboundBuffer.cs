using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal.Buffers
{
    /// <summary>
    ///     Structure for containing outbound stream data, represents sending direction of the stream.
    /// </summary>
    internal sealed class OutboundBuffer
    {
        private const int PreferredChunkSize = 32 * 1024;
        // TODO-RZ: tie this to control flow limits
        private const int MaximumHeldChunks = 8;

        private object SyncObject => _toSendChannel;

        /// <summary>
        ///     Current state of the stream.
        /// </summary>
        internal SendStreamState StreamState { get; private set; }

        /// <summary>
        ///     Ranges of bytes acked by the peer.
        /// </summary>
        private readonly RangeSet _acked = new RangeSet();

        /// <summary>
        ///     Ranges of bytes currently in-flight.
        /// </summary>
        private readonly RangeSet _checkedOut = new RangeSet();

        /// <summary>
        ///     Ranges of bytes awaiting to be sent.
        /// </summary>
        private readonly RangeSet _pending = new RangeSet();

        /// <summary>
        ///     True if the peer has acked a frame specifying the final size of the stream.
        /// </summary>
        private bool _finAcked;

        /// <summary>
        ///     Chunk to be filled from user data.
        /// </summary>
        private StreamChunk _toBeQueuedChunk = new StreamChunk(0, ReadOnlyMemory<byte>.Empty,
            ArrayPool<byte>.Shared.Rent(PreferredChunkSize));

        /// <summary>
        ///     Channel of incoming chunks of memory from the user.
        /// </summary>
        private readonly Channel<StreamChunk> _toSendChannel =
            Channel.CreateUnbounded<StreamChunk>(new UnboundedChannelOptions
            {
                SingleReader = true, SingleWriter = true
            });

        /// <summary>
        ///     Individual chunks of the stream to be sent.
        /// </summary>
        private readonly List<StreamChunk> _chunks = new List<StreamChunk>();

        /// <summary>
        ///     Number of bytes dequeued from the <see cref="_toSendChannel"/>.
        /// </summary>
        private long _dequedBytes;

        /// <summary>
        ///     Error code if the stream was aborted.
        /// </summary>
        internal long? Error { get; private set; }

        public OutboundBuffer(long maxData)
        {
            UpdateMaxData(maxData);
        }

        /// <summary>
        ///     Total number of bytes written into this stream.
        /// </summary>
        /// <remarks>
        ///     This property is updated by the user-code thread.
        /// </remarks>
        internal long WrittenBytes { get; private set; }

        /// <summary>
        ///     Number of bytes present in <see cref="_toSendChannel" />
        /// </summary>
        private long BytesInChannel => WrittenBytes - _dequedBytes;

        /// <summary>
        ///     Total number of bytes allowed to transport in this stream.
        /// </summary>
        internal long MaxData { get; private set; }

        /// <summary>
        ///     Number of bytes from the beginning of the stream which were sent (and not necessarily acknowledged).
        /// </summary>
        internal long SentBytes { get; private set; }

        /// <summary>
        ///     Synchronization for avoiding overfilling the buffer.
        /// </summary>
        private readonly SemaphoreSlim _bufferLimitSemaphore = new SemaphoreSlim(MaximumHeldChunks - 1);

        /// <summary>
        ///     True if the stream is closed for further writing (no more data can be added).
        /// </summary>
        internal bool SizeKnown { get; private set; }

        /// <summary>
        ///     Returns true if buffer contains any sendable data below <see cref="MaxData" /> limit.
        /// </summary>
        internal bool IsFlushable => _pending.Count > 0 && _pending[0].Start < MaxData ||
                                     _dequedBytes < MaxData && BytesInChannel > 0;

        /// <summary>
        ///     Aborts the outbound stream with given error code.
        /// </summary>
        /// <param name="errorCode"></param>
        internal void Abort(long errorCode)
        {
            // TODO-RZ: should we throw if already aborted?

            // TODO-RZ: this is the only situation when state is set from user thread, maybe we can
            // find a way to remove the need for the lock
            if (StreamState < SendStreamState.WantReset)
            {
                lock (SyncObject)
                {
                    if (StreamState < SendStreamState.WantReset)
                    {
                        Debug.Assert(Error == null);
                        Error = errorCode;
                        StreamState = SendStreamState.WantReset;
                    }
                }
            }

            // TODO-RZ: Can we drop all buffered data?
        }

        internal void OnResetSent()
        {
            // we are past WantReset, no synchronization needed
            Debug.Assert(StreamState == SendStreamState.WantReset);
            StreamState = SendStreamState.ResetSent;
        }

        internal void OnResetAcked()
        {
            // we are past WantReset, no synchronization needed
            Debug.Assert(StreamState == SendStreamState.ResetSent);
            StreamState = SendStreamState.ResetReceived;
        }

        internal void OnResetLost()
        {
            // we are past WantReset, no synchronization needed
            Debug.Assert(StreamState == SendStreamState.ResetSent);
            StreamState = SendStreamState.WantReset;
        }

        /// <summary>
        ///     Queues the not yet full chunk of stream into flush queue, blocking when control flow limit is not
        ///     sufficient.
        /// </summary>
        internal async ValueTask FlushChunkAsync(CancellationToken cancellationToken = default)
        {
            if (_toBeQueuedChunk.Length == 0)
            {
                // nothing to do
                return;
            }

            var buffer = await RentBufferAsync(cancellationToken).ConfigureAwait(false);
            var tmp = _toBeQueuedChunk;
            _toBeQueuedChunk = new StreamChunk(WrittenBytes, Memory<byte>.Empty, buffer);

            await _toSendChannel.Writer.WriteAsync(tmp, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Queues the not yet full chunk of stream into flush queue, blocking when control flow limit is not
        ///     sufficient.
        /// </summary>
        internal void FlushChunk()
        {
            if (_toBeQueuedChunk.Length == 0)
            {
                // nothing to do
                return;
            }

            var buffer = RentBuffer();
            var tmp = _toBeQueuedChunk;
            _toBeQueuedChunk = new StreamChunk(WrittenBytes, Memory<byte>.Empty, buffer);

            _toSendChannel.Writer.TryWrite(tmp);
        }

        /// <summary>
        ///     Flushes partially full chunk into sending queue, regardless of <see cref="MaxData"/> limit.
        /// </summary>
        internal void ForceFlushPartialChunk()
        {
            _toSendChannel.Writer.TryWrite(_toBeQueuedChunk);
            var buffer = ArrayPool<byte>.Shared.Rent(PreferredChunkSize);
            _toBeQueuedChunk = new StreamChunk(WrittenBytes, Memory<byte>.Empty, buffer);
        }

        /// <summary>
        ///     Updates the <see cref="MaxData" /> parameter.
        /// </summary>
        /// <param name="value">Value of the parameter.</param>
        internal void UpdateMaxData(long value)
        {
            MaxData = Math.Max(MaxData, value);
        }

        internal async ValueTask EnqueueAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(!SizeKnown);

            while (buffer.Length > 0)
            {
                int toWrite = Math.Min(_toBeQueuedChunk.Buffer!.Length - _toBeQueuedChunk.Length, buffer.Length);
                buffer.Span.Slice(0, toWrite).CopyTo(_toBeQueuedChunk.Buffer!.AsSpan(_toBeQueuedChunk.Length, toWrite));
                WrittenBytes += toWrite;
                _toBeQueuedChunk = new StreamChunk(_toBeQueuedChunk.StreamOffset,
                    _toBeQueuedChunk.Buffer!.AsMemory(0, _toBeQueuedChunk.Length + toWrite), _toBeQueuedChunk.Buffer);

                if (_toBeQueuedChunk.Length == _toBeQueuedChunk.Buffer!.Length)
                {
                    await FlushChunkAsync(cancellationToken).ConfigureAwait(false);
                }

                buffer = buffer.Slice(toWrite);
            }
        }

        /// <summary>
        ///     Copies given memory to the outbound stream to be sent.
        /// </summary>
        /// <param name="data">Data to be sent.</param>
        internal void Enqueue(ReadOnlySpan<byte> data)
        {
            Debug.Assert(!SizeKnown);

            while (data.Length > 0)
            {
                int toWrite = Math.Min(_toBeQueuedChunk.Buffer!.Length - _toBeQueuedChunk.Length, data.Length);
                data.Slice(0, toWrite).CopyTo(_toBeQueuedChunk.Buffer!.AsSpan(_toBeQueuedChunk.Length, toWrite));
                WrittenBytes += toWrite;
                _toBeQueuedChunk = new StreamChunk(_toBeQueuedChunk.StreamOffset,
                    _toBeQueuedChunk.Buffer!.AsMemory(0, _toBeQueuedChunk.Length + toWrite), _toBeQueuedChunk.Buffer);

                if (_toBeQueuedChunk.Length == _toBeQueuedChunk.Buffer!.Length)
                {
                    FlushChunk();
                }

                data = data.Slice(toWrite);
            }
        }

        private void DrainIncomingChunks()
        {
            var reader = _toSendChannel.Reader;
            while (reader.TryRead(out var chunk))
            {
                Debug.Assert(_dequedBytes == chunk.StreamOffset);
                _pending.Add(chunk.StreamOffset, chunk.StreamOffset + chunk.Length - 1);
                _chunks.Add(chunk);
                _dequedBytes += chunk.Length;
            }
        }

        /// <summary>
        ///     Returns length of the next contiguous range of data that can be checked out, respecting the
        ///     <see cref="MaxData" /> parameter.
        /// </summary>
        /// <returns></returns>
        internal (long offset, long count) GetNextSendableRange()
        {
            DrainIncomingChunks();
            if (_pending.Count == 0) return (WrittenBytes, 0);

            long sendableLength = MaxData - _pending[0].Start;
            long count = Math.Min(sendableLength, _pending[0].Length);
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

            if (StreamState == SendStreamState.Ready)
            {
                lock (SyncObject)
                {
                    if (StreamState == SendStreamState.Ready)
                    {
                        StreamState = SendStreamState.Send;
                    }
                }
            }

            DrainIncomingChunks();
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
                int inChunkCount = Math.Min(_chunks[i].Length - inChunkStart, destination.Length - copied);
                _chunks[i].Memory.Span.Slice(inChunkStart, inChunkCount).CopyTo(destination.Slice(copied));

                copied += inChunkCount;
                i++;
            }

            SentBytes = Math.Max(SentBytes, end + 1);

            if (SizeKnown && StreamState == SendStreamState.Send && SentBytes == WrittenBytes)
            {
                lock (SyncObject)
                {
                    if (StreamState == SendStreamState.Send)
                    {
                        StreamState = SendStreamState.DataSent;
                    }
                }
            }
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
        /// <param name="fin">Whether the sent frame contained the FIN bit.</param>
        internal void OnAck(long offset, long count, bool fin = false)
        {
            if (fin)
            {
                Debug.Assert(offset + count == WrittenBytes);
                _finAcked = true;
            }

            if (count == 0)
            {
                return;
            }

            long end = offset + count - 1;

            Debug.Assert(_checkedOut.Includes(offset, end));

            _checkedOut.Remove(offset, end);
            _acked.Add(offset, end);

            if (_acked[0].Start != 0)
            {
                // do not discard data yet, as the very first data to be discared were not acked
                return;
            }

            // release unneeded data
            long processed = _acked[0].End + 1;

            // index of first chunk with unsent data is the same as count of unneeded chunks that are before
            int toRemove = _chunks.FindIndex(c => c.StreamOffset + c.Length > processed);
            if (toRemove == -1)
            {
                toRemove = _chunks.Count;
            }

            for (int i = 0; i < toRemove; i++)
            {
                if (_chunks[i].Buffer != null)
                {
                    ReturnBuffer(_chunks[i].Buffer!);
                }
            }

            _chunks.RemoveRange(0, toRemove);

            if (_finAcked && _acked[0].Length == WrittenBytes && StreamState == SendStreamState.DataSent)
            {
                lock (SyncObject)
                {
                    if (StreamState == SendStreamState.DataSent)
                    {
                        StreamState = SendStreamState.DataReceived;
                    }
                }
            }
        }

        private byte[] RentBuffer()
        {
            // TODO-RZ: we need to be able to cancel this
            _bufferLimitSemaphore.Wait();
            return ArrayPool<byte>.Shared.Rent(PreferredChunkSize);
        }

        private async ValueTask<byte[]> RentBufferAsync(CancellationToken cancellationToken)
        {
            await _bufferLimitSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return ArrayPool<byte>.Shared.Rent(PreferredChunkSize);
        }

        private void ReturnBuffer(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _bufferLimitSemaphore.Release();
        }
    }
}
