// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal.Streams
{
    /// <summary>
    ///     Class for receiving and buffering inbound stream data.
    /// </summary>
    internal sealed class ReceiveStream
    {
        private int ReorderBuffersSize => QuicBufferPool.BufferSize;

        private object SyncObject => _deliverableChannel;

        /// <summary>
        ///     Chunk containing leftover data from the last delivery.
        /// </summary>
        private StreamChunk _deliveryLeftoverChunk;

        /// <summary>
        ///     Current state of the stream.
        /// </summary>
        internal RecvStreamState StreamState { get; private set; }

        /// <summary>
        ///     Channel for producing chunks for the user to read.
        /// </summary>
        private readonly Channel<StreamChunk> _deliverableChannel =
            Channel.CreateUnbounded<StreamChunk>(new UnboundedChannelOptions
            {
                SingleReader = true, SingleWriter = true
            });

        /// <summary>
        ///     List of buffers which hold out-of-order data which cannot be delivered yet because a prior data were not
        ///     received yet.
        /// </summary>
        private readonly List<byte[]> _receivingBuffers = new List<byte[]>();

        /// <summary>
        ///     Total number of bytes allowed to transport in this stream. Receiving data at this offset or higher
        ///     implies protocol violation by the sender.
        /// </summary>
        internal long MaxData { get; private set; }

        /// <summary>
        ///     Value of <see cref="MaxData"/> that the peer is confirmed to have received. Used to determine whether an
        ///     update should be sent.
        /// </summary>
        internal long RemoteMaxData { get; private set; }

        /// <summary>
        ///     Updates the <see cref="MaxData"/> parameter to the maximum of current and new values.
        /// </summary>
        /// <param name="value">Value of the parameter.</param>
        internal void UpdateMaxData(long value)
        {
            MaxData = Math.Max(MaxData, value);
        }

        /// <summary>
        ///     Updates <see cref="RemoteMaxData"/> to the maximum of current and new values.
        /// </summary>
        /// <param name="value">value of the parameter.</param>
        internal void UpdateRemoteMaxData(long value)
        {
            // the peer cannot receive greater max data than we have locally
            Debug.Assert(value <= MaxData);
            RemoteMaxData = Math.Max(RemoteMaxData, value);
        }

        public ReceiveStream(long maxData)
        {
            MaxData = maxData;
            RemoteMaxData = maxData;
        }

        /// <summary>
        ///     Ranges of data which are received, but weren't queued for delivery in the delivery channel because they
        ///     were received out-of-order.
        /// </summary>
        private readonly RangeSet _toBeQueuedRanges = new RangeSet();

        /// <summary>
        ///     Number of bytes streamed through the <see cref="_deliverableChannel"/>.
        /// </summary>
        private long _bytesQueued;

        /// <summary>
        ///     Total number of bytes delivered.
        /// </summary>
        internal long BytesRead { get; private set; }

        /// <summary>
        ///     True if Fin has been received. Also, if true, <see cref="Size"/> holds the final size of the stream.
        /// </summary>
        internal bool FinalSizeKnown { get; private set; }

        /// <summary>
        ///     Size of the stream. May grow while FinalSizeKnown is false.
        /// </summary>
        internal long Size { get; private set; }

        /// <summary>
        ///     Number of bytes ready to be read from the stream.
        /// </summary>
        internal long BytesAvailable => _bytesQueued - BytesRead;

        /// <summary>
        ///     Error code if the inbound buffer was aborted.
        /// </summary>
        public long? Error { get; private set; }

        /// <summary>
        ///     Returns true if MaxStreamData frame should be sent to the peer.
        /// </summary>
        /// <returns></returns>
        internal bool ShouldUpdateMaxData()
        {
            return MaxData - RemoteMaxData >= RemoteMaxData - Size;
        }

        /// <summary>
        ///     Request that the stream be aborted by the sender with specified error code.
        /// </summary>
        /// <param name="errorCode">Application provided error code.</param>
        /// <remarks>Called from user thread.</remarks>
        internal void RequestAbort(long errorCode)
        {
            // requesting that STOP_SENDING be sent for streams in states other than
            // Receive and SizeKnown does not make sense. Also, there is a data race with incoming
            // RESET_STREAM frames sent on peers own behalf.
            if (StreamState >= RecvStreamState.DataReceived)
            {
                return;
            }

            lock (SyncObject)
            {
                if (StreamState >= RecvStreamState.DataReceived)
                {
                    return;
                }

                StreamState = RecvStreamState.WantStopSending;
                Debug.Assert(Error == null);
                Error = errorCode;
            }

            SetStreamAborted(errorCode);
            // we cannot drop all buffered data now, since we are on user thread. The data will be discarded
            // in OnStopSendingSent.
        }

        /// <summary>
        ///     Performs state transition when RESET_FRAME was received.
        /// </summary>
        /// <param name="errorCode">Error code from the frame.</param>
        internal void OnResetStream(long errorCode)
        {
            if (StreamState >= RecvStreamState.ResetReceived)
            {
                return;
            }

            lock (SyncObject)
            {
                if (StreamState >= RecvStreamState.ResetReceived)
                {
                    return;
                }

                StreamState = RecvStreamState.ResetReceived;

                // Error can be set if application requested that STOP_SENDING frame be sent.
                // RFC allows use to ignore incoming error code in such case.
                Error ??= errorCode;
            }

            Debug.Assert(StreamState == RecvStreamState.DataRead || StreamState >= RecvStreamState.ResetReceived);

            SetStreamAborted(errorCode);
            DropAllBufferedData();
        }

        internal void OnStopSendingSent()
        {
            if (StreamState == RecvStreamState.WantStopSending)
            {
                lock (SyncObject)
                {
                    if (StreamState == RecvStreamState.WantStopSending)
                    {
                        StreamState = RecvStreamState.StopSendingSent;
                    }
                }
            }

            // we are on socket thread, we can safely discard buffers
            DropAllBufferedData();
        }

        internal void OnStopSendingLost()
        {
            if (StreamState == RecvStreamState.StopSendingSent)
            {
                lock (SyncObject)
                {
                    if (StreamState == RecvStreamState.StopSendingSent)
                    {
                        StreamState = RecvStreamState.WantStopSending;
                    }
                }
            }
        }

        private void OnFinalSize(long finalSize)
        {
            Debug.Assert(!FinalSizeKnown || Size == finalSize);

            if (StreamState == RecvStreamState.Receive)
            {
                lock (SyncObject)
                {
                    if (StreamState == RecvStreamState.Receive)
                    {
                        // TODO-RZ: manage leftover flow control credit
                        StreamState = RecvStreamState.SizeKnown;
                        Size = finalSize;
                        FinalSizeKnown = true;
                    }
                }
            }
        }

        private void OnAllReceived()
        {
            if (StreamState == RecvStreamState.SizeKnown)
            {
                lock (SyncObject)
                {
                    if (StreamState == RecvStreamState.SizeKnown)
                    {
                        StreamState = RecvStreamState.DataReceived;
                        _deliverableChannel.Writer.TryComplete();
                    }
                }
            }
        }

        private void OnAllRead()
        {
            // no lock necessary here, race condition with incoming reset frame does not affect user.
            StreamState = RecvStreamState.DataRead;
        }


        /// <summary>
        ///     Receives a chunk of data and buffers it for delivery.
        /// </summary>
        /// <param name="offset">Offset on the stream of the received data.</param>
        /// <param name="data">The data to be received.</param>
        /// <param name="fin">True if this is the last segment of the stream.</param>
        internal void Receive(long offset, ReadOnlySpan<byte> data, bool fin = false)
        {
            Debug.Assert(!FinalSizeKnown || offset + data.Length <= Size, "Writing after final size");

            if (fin)
            {
                OnFinalSize(offset + data.Length);
            }

            Size = Math.Max(Size, offset + data.Length);

            // deliver new data if present
            if (!data.IsEmpty && offset + data.Length > _bytesQueued)
            {
                if (offset < _bytesQueued)
                {
                    // drop duplicate prefix;
                    data = data.Slice((int)(_bytesQueued - offset));
                    offset = _bytesQueued;
                }

                long recvBufStart = _bytesQueued - _bytesQueued % ReorderBuffersSize;
                while (!data.IsEmpty)
                {
                    int recvBufIndex = (int) ((offset - recvBufStart) / ReorderBuffersSize);
                    int recvBufOffset = (int) (offset % ReorderBuffersSize);

                    // make sure we have enough buffers, note that the protocol's Flow Control feature should
                    // limit maximum number of buffers we have allocated at any moment.
                    while (_receivingBuffers.Count <= recvBufIndex)
                    {
                        _receivingBuffers.Add(QuicBufferPool.Rent());
                    }

                    int written = Math.Min(data.Length, ReorderBuffersSize - recvBufOffset);
                    data.Slice(0, written).CopyTo(_receivingBuffers[recvBufIndex].AsSpan().Slice(recvBufOffset));
                    _toBeQueuedRanges.Add(offset, offset + written - 1);

                    data = data.Slice(written);
                    offset += written;
                }

                // queue data for delivery
                int buffersProcessed = 0;
                while (_toBeQueuedRanges.Count > 0 && _toBeQueuedRanges.GetMin() == _bytesQueued)
                {
                    Debug.Assert(_receivingBuffers.Count > buffersProcessed);

                    var buffer = _receivingBuffers[buffersProcessed];

                    long lastStreamOffsetInBuffer = recvBufStart + buffersProcessed * ReorderBuffersSize + ReorderBuffersSize - 1;
                    long deliveryEnd = Math.Min(_toBeQueuedRanges[0].End, lastStreamOffsetInBuffer);

                    int startOffset = (int)(_bytesQueued % ReorderBuffersSize);
                    int endOffset = (int)(deliveryEnd % ReorderBuffersSize);
                    int delivered = endOffset - startOffset + 1;

                    var memory = buffer.AsMemory(startOffset, delivered);

                    StreamChunk chunk;
                    if (deliveryEnd == lastStreamOffsetInBuffer)
                    {
                        // entire buffer is filled, pass it in the chunk so that it gets returned and reused
                        chunk = new StreamChunk(_bytesQueued, memory, buffer);
                        buffersProcessed++;
                    }
                    else
                    {
                        chunk = new StreamChunk(_bytesQueued, memory, null);
                    }

                    _deliverableChannel.Writer.TryWrite(chunk);
                    _toBeQueuedRanges.Remove(_bytesQueued, deliveryEnd);
                    _bytesQueued += delivered;
                }

                // remove buffers which were passed to the deliverable channel
                _receivingBuffers.RemoveRange(0, buffersProcessed);
            }

            if (FinalSizeKnown && Size == _bytesQueued)
            {
                OnAllReceived();
            }
        }

        /// <summary>
        ///     Delivers the buffered data by copying to the provided memory. If no data available, this method blocks
        ///     until more data arrive or the stream is closed.
        /// </summary>
        /// <param name="destination">Destination memory.</param>
        /// <param name="token">Cancellation token for the operation.</param>
        /// <returns></returns>
        internal async ValueTask<int> DeliverAsync(Memory<byte> destination, CancellationToken token)
        {
            int delivered = Deliver(destination.Span);

            if (delivered > 0)
                return delivered;

            if (StreamState != RecvStreamState.DataRead && await _deliverableChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                return Deliver(destination.Span);
            }

            return 0;
        }

        /// <summary>
        ///     Delivers the buffered data to the target destination span.
        /// </summary>
        /// <param name="destination"></param>
        internal int Deliver(Span<byte> destination)
        {
            int delivered = 0;

            do
            {
                if (_deliveryLeftoverChunk.Memory.IsEmpty)
                {
                    ReturnMemory(_deliveryLeftoverChunk);

                    if (!_deliverableChannel.Reader.TryRead(out _deliveryLeftoverChunk))
                    {
                        break;
                    }
                }

                int len = Math.Min(destination.Length, _deliveryLeftoverChunk.Memory.Length);
                _deliveryLeftoverChunk.Memory.Span.Slice(0, len).CopyTo(destination.Slice(0, len));

                _deliveryLeftoverChunk = new StreamChunk(
                    _deliveryLeftoverChunk.StreamOffset + len,
                    _deliveryLeftoverChunk.Memory.Slice(len),
                    _deliveryLeftoverChunk.Buffer);

                destination = destination.Slice(len);
                delivered += len;

                // allow sender send more data
            } while (destination.Length > 0);

            UpdateMaxData(MaxData + delivered);
            BytesRead += delivered;
            if (FinalSizeKnown && BytesRead == Size)
            {
                OnAllRead();
            }

            return delivered;
        }

        /// <summary>
        ///     Processes all deliverable data using provided callback.
        /// </summary>
        /// <param name="process"></param>
        internal void Deliver(Action<ReadOnlyMemory<byte>> process)
        {
            if (!_deliveryLeftoverChunk.Memory.IsEmpty)
            {
                process(_deliveryLeftoverChunk.Memory);
                BytesRead += _deliveryLeftoverChunk.Memory.Length;
                ReturnMemory(_deliveryLeftoverChunk);
                _deliveryLeftoverChunk = default;
            }

            while (_deliverableChannel.Reader.TryRead(out var chunk))
            {
                process(chunk.Memory);
                BytesRead += chunk.Memory.Length;
                ReturnMemory(chunk);
            }
        }

        private void ReturnMemory(in StreamChunk chunk)
        {
            if (chunk.Buffer == null)
            {
                return;
            }

            ReturnBuffer(chunk.Buffer);
        }

        public void OnFatalException(Exception e)
        {
            if (e is QuicConnectionAbortedException abortedException && abortedException.ErrorCode == 0)
            {
                _deliverableChannel.Writer.TryComplete(null);
            }
            else
            {
                _deliverableChannel.Writer.TryComplete(e);
            }
        }

        private void ReturnBuffer(byte[] buffer)
        {
            QuicBufferPool.Return(buffer);
        }

        internal void DropAllBufferedData()
        {
            for (int i = 0; i < _receivingBuffers.Count; i++)
            {
                ReturnBuffer(_receivingBuffers[i]);
            }

            _receivingBuffers.Clear();
        }

        private void SetStreamAborted(long errorCode)
        {
            _deliverableChannel.Writer.TryComplete(new QuicStreamAbortedException(errorCode));
        }
    }
}
