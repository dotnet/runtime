using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;

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
        ///     Channel for producing chunks of from the user to read.
        /// </summary>
        private readonly Channel<StreamChunk> _boundaryChannel =
            Channel.CreateUnbounded<StreamChunk>(new UnboundedChannelOptions
            {
                SingleReader = true, SingleWriter = true
            });

        /// <summary>
        ///     Individual, deduplicated parts of the stream, ordered by stream offset.
        /// </summary>
        internal List<StreamChunk> _chunks = new List<StreamChunk>();

        /// <summary>
        ///     Total number of bytes allowed to transport in this stream.
        /// </summary>
        internal long MaxData { get; private set; }

        /// <summary>
        ///     Updates the <see cref="MaxData"/> parameter.
        /// </summary>
        /// <param name="value">Value of the parameter.</param>
        internal void UpdateMaxData(long value)
        {
            MaxData = Math.Max(MaxData, value);
        }

        public InboundBuffer(long maxData)
        {
            UpdateMaxData(maxData);
        }

        /// <summary>
        ///     Ranges of data which are received, but not delivered.
        /// </summary>
        private RangeSet _undelivered = new RangeSet();

        /// <summary>
        ///     Number of bytes streamed through the <see cref="_boundaryChannel"/>.
        /// </summary>
        internal long _bytesChanneled;

        /// <summary>
        ///     Total number of bytes delivered.
        /// </summary>
        internal long BytesRead { get; private set; }

        /// <summary>
        ///     Final size of the stream. Null if final size is not known yet.
        /// </summary>
        internal long? FinalSize { get; set; }

        /// <summary>
        ///     Estimated size of the stream. Final size may not be lower than this value.
        /// </summary>
        internal long EstimatedSize => _undelivered.Count > 0 ? _undelivered.GetMax() : BytesRead;

        /// <summary>
        ///     Number of bytes ready to be read from the stream.
        /// </summary>
        internal long BytesAvailable => _bytesChanneled - BytesRead;
        // internal long BytesAvailable => _undelivered.Count > 0 && BytesRead == _undelivered.GetMin()
            // ? _undelivered[0].Length
            // : 0;

        /// <summary>
        ///     Receives a chunk of data and buffers it for delivery.
        /// </summary>
        /// <param name="offset">Offset on the stream of the received data.</param>
        /// <param name="data">The data to be received.</param>
        internal void Receive(long offset, ReadOnlySpan<byte> data)
        {
            Debug.Assert(FinalSize == null || offset + data.Length <= FinalSize, "Writing after final size");

            if (data.IsEmpty)
            {
                return;
            }

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
                var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
                data.CopyTo(buffer);

                _chunks.Add(new StreamChunk(offset, buffer.AsMemory(0, data.Length), buffer));
            }
            else
            {
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

                    _chunks.Insert(index, new StreamChunk(range.Start, buffer.AsMemory(0, (int) range.Length), buffer));
                }
            }

            _undelivered.Add(offset, offset + data.Length - 1);
            FlushChunksToUser();
        }

        private void FlushChunksToUser()
        {
            // push chunks to the output channel for the user thread to read.
            int pushed = 0;
            var channelWriter = _boundaryChannel.Writer;
            while (pushed < _chunks.Count && _chunks[pushed].StreamOffset == _bytesChanneled)
            {
                var chunk = _chunks[pushed];
                _undelivered.Remove(chunk.StreamOffset, chunk.StreamOffset + chunk.Length - 1);
                channelWriter.TryWrite(chunk);
                _bytesChanneled += chunk.Length;
                pushed++;
            }

            _chunks.RemoveRange(0, pushed);
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

                    if (!_boundaryChannel.Reader.TryRead(out _deliveryLeftoverChunk))
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
            } while (destination.Length > 0);

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

            while (_boundaryChannel.Reader.TryRead(out var chunk))
            {
                process(chunk.Memory);
                BytesRead += chunk.Memory.Length;
                ReturnMemory(chunk);
            }
        }

        private void ReturnMemory(in StreamChunk chunk)
        {
            if (chunk.Buffer != null)
            {
                ArrayPool<byte>.Shared.Return(chunk.Buffer);
            }
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
            _bytesChanneled += length;
            FlushChunksToUser();
        }
    }
}
