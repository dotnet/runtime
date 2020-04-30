using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed.Internal.Buffers
{
    /// <summary>
    ///     Class for receiving and buffering inbound stream data.
    /// </summary>
    internal sealed class InboundBuffer
    {
        /// <summary>
        ///     Chunk containing leftover data from the last delivery.
        /// </summary>
        private StreamChunk _deliveryLeftoverChunk;

        /// <summary>
        ///     Channel for producing chunks for the user to read.
        /// </summary>
        private readonly Channel<StreamChunk> _deliverableChannel =
            Channel.CreateUnbounded<StreamChunk>(new UnboundedChannelOptions
            {
                SingleReader = true, SingleWriter = true
            });

        /// <summary>
        ///     Received stream chunks which cannot be delivered yet because of out of order delivery of frames.
        /// </summary>
        private readonly SortedSet<StreamChunk> _outOfOrderChunks = new SortedSet<StreamChunk>(StreamChunk.OffsetComparer);

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
            Debug.Assert(value <= MaxData);
            MaxData = Math.Max(RemoteMaxData, value);
        }

        public InboundBuffer(long maxData)
        {
            UpdateMaxData(maxData);
        }

        /// <summary>
        ///     Ranges of data which are received, but not delivered.
        /// </summary>
        private readonly RangeSet _undelivered = new RangeSet();

        /// <summary>
        ///     Number of bytes streamed through the <see cref="_deliverableChannel"/>.
        /// </summary>
        private long _bytesDeliverable;

        /// <summary>
        ///     Total number of bytes delivered.
        /// </summary>
        internal long BytesRead { get; private set; }

        /// <summary>
        ///     Final size of the stream. Null if final size is not known yet.
        /// </summary>
        internal long? FinalSize { get; private set; }

        /// <summary>
        ///     Estimated size of the stream. Final size may not be lower than this value.
        /// </summary>
        internal long EstimatedSize => _undelivered.Count > 0 ? _undelivered.GetMax() : BytesRead;

        /// <summary>
        ///     Number of bytes ready to be read from the stream.
        /// </summary>
        internal long BytesAvailable => _bytesDeliverable - BytesRead;

        /// <summary>
        ///     Receives a chunk of data and buffers it for delivery.
        /// </summary>
        /// <param name="offset">Offset on the stream of the received data.</param>
        /// <param name="data">The data to be received.</param>
        /// <param name="fin">True if this is the last segment of the stream.</param>
        internal void Receive(long offset, ReadOnlySpan<byte> data, bool fin = false)
        {
            Debug.Assert(FinalSize == null || offset + data.Length <= FinalSize, "Writing after final size");

            if (fin)
            {
                Debug.Assert(FinalSize == null || FinalSize == offset + data.Length);
                FinalSize = offset + data.Length;
            }

            // deliver new data if present
            if (!data.IsEmpty && offset + data.Length > _bytesDeliverable)
            {
                if (offset < _bytesDeliverable)
                {
                    // drop duplicate prefix;
                    data = data.Slice((int)(_bytesDeliverable - offset));
                    offset = _bytesDeliverable;
                }

                // optimized hot path - in-order delivery
                if (_outOfOrderChunks.Count == 0 && _bytesDeliverable == offset)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
                    data.CopyTo(buffer);
                    _bytesDeliverable += data.Length;
                    _deliverableChannel.Writer.TryWrite(new StreamChunk(offset, buffer.AsMemory(0, data.Length), buffer));
                }
                else
                {
                    // out of order delivery, use ranges to remove duplicate data
                    RangeSet toDeliver = new RangeSet {{offset, offset + data.Length - 1}};
                    foreach (var range in _undelivered)
                    {
                        toDeliver.Remove(range.Start, range.End);
                    }

                    foreach (var range in toDeliver)
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent((int)range.Length);
                        data.Slice((int) (range.Start - offset), (int) range.Length).CopyTo(buffer);
                        _outOfOrderChunks.Add(new StreamChunk(range.Start, buffer.AsMemory(0, (int) range.Length), buffer));
                    }

                    _undelivered.Add(offset, offset + data.Length - 1);
                    DrainDeliverableOutOfOrderChunks();
                }
            }

            if (FinalSize == _bytesDeliverable)
            {
                _deliverableChannel.Writer.TryComplete();
            }
        }

        private void DrainDeliverableOutOfOrderChunks()
        {
            while (_outOfOrderChunks.Count > 0 && _outOfOrderChunks.Min.StreamOffset == _bytesDeliverable)
            {
                var chunk = _outOfOrderChunks.Min;
                _undelivered.Remove(chunk.StreamOffset, chunk.StreamOffset + chunk.Length - 1);
                _bytesDeliverable += chunk.Length;
                _deliverableChannel.Writer.TryWrite(chunk);
                _outOfOrderChunks.Remove(chunk);
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

            if (await _deliverableChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
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

            ArrayPool<byte>.Shared.Return(chunk.Buffer);
        }

        /// <summary>
        ///     Skips given amount of data. Useful if the data was delivered by different means (without buffering).
        /// </summary>
        /// <param name="length">Number of bytes from the payload to be skipped.</param>
        public void Skip(long length)
        {
            // TODO-RZ: test if this is robust against crypto stream out of order receipt
            Debug.Assert(BytesAvailable == 0, "Can skip only if the data has not been received.");
            _undelivered.Remove(BytesRead, BytesRead + length - 1);
            BytesRead += length;
            _bytesDeliverable += length;
            DrainDeliverableOutOfOrderChunks();
        }
    }
}
