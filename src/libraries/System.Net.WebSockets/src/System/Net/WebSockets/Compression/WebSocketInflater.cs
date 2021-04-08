// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using static System.IO.Compression.ZLibNative;

namespace System.Net.WebSockets.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib decompression API.
    /// </summary>
    internal sealed class WebSocketInflater : IDisposable
    {
        internal const int FlushMarkerLength = 4;
        internal static ReadOnlySpan<byte> FlushMarker => new byte[] { 0x00, 0x00, 0xFF, 0xFF };

        private readonly ZLibStreamPool _streamPool;
        private ZLibStreamHandle? _stream;
        private readonly bool _persisted;

        /// <summary>
        /// There is no way of knowing, when decoding data, if the underlying inflater
        /// has flushed all outstanding data to consumer other than to provide a buffer
        /// and see whether any bytes are written. There are cases when the consumers
        /// provide a buffer exactly the size of the uncompressed data and in this case
        /// to avoid requiring another read we will use this field.
        /// </summary>
        private byte? _remainingByte;

        /// <summary>
        /// When the inflater is persisted we need to manually append the flush marker
        /// before finishing the decoding.
        /// </summary>
        private bool _needsFlushMarker;

        private byte[]? _buffer;

        /// <summary>
        /// The position for the next unconsumed byte in the inflate buffer.
        /// </summary>
        private int _position;

        /// <summary>
        /// How many unconsumed bytes are left in the inflate buffer.
        /// </summary>
        private int _available;

        internal WebSocketInflater(int windowBits, bool persisted)
        {
            _streamPool = ZLibStreamPool.GetOrCreate(windowBits);
            _persisted = persisted;
        }

        /// <summary>
        /// Indicates that there is nothing left for inflating.
        /// </summary>
        public bool Finished { get; private set; } = true;

        public Memory<byte> Memory => _buffer.AsMemory(_position + _available);

        public Span<byte> Span => _buffer.AsSpan(_position + _available);

        public void Dispose()
        {
            if (_stream is not null)
            {
                _streamPool.ReturnInflater(_stream);
                _stream = null;
            }
            ReleaseBuffer();
        }

        /// <summary>
        /// Initializes the inflater by allocating a buffer so the websocket can receive directly onto it.
        /// </summary>
        /// <param name="payloadLength">the length of the message payload</param>
        /// <param name="userBufferLength">the length of the buffer where the payload will be inflated</param>
        public void Prepare(long payloadLength, int userBufferLength)
        {
            if (_buffer is not null)
            {
                Debug.Assert(_available > 0);

                _buffer.AsSpan(_position, _available).CopyTo(_buffer);
                _position = 0;
            }
            else
            {
                // Rent a buffer as close to the size of the user buffer as possible,
                // but not try to rent anything above 1MB because the array pool will allocate.
                // If the payload is smaller than the user buffer, rent only as much as we need.
                _buffer = ArrayPool<byte>.Shared.Rent(Math.Min(userBufferLength, (int)Math.Min(payloadLength, 1_000_000)));
            }
        }

        /// <summary>
        /// Inflates the last receive payload into the provided buffer.
        /// </summary>
        public unsafe void Inflate(int totalBytesReceived, Span<byte> output, bool flush, out int written)
        {
            if (totalBytesReceived > 0)
            {
                Debug.Assert(_buffer is not null, "Prepare must be called.");
                _available += totalBytesReceived;
            }

            _stream ??= _streamPool.GetInflater();

            if (_available > 0 && output.Length > 0)
            {
                int consumed;

                fixed (byte* bufferPtr = _buffer)
                {
                    _stream.NextIn = (IntPtr)(bufferPtr + _position);
                    _stream.AvailIn = (uint)_available;

                    written = Inflate(_stream, output);
                    consumed = _available - (int)_stream.AvailIn;
                }

                _position += consumed;
                _available -= consumed;
                _needsFlushMarker = _persisted;
            }
            else
            {
                written = 0;
            }

            if (_available == 0)
            {
                ReleaseBuffer();
                Finished = flush ? Flush(output, ref written) : true;
            }
            else
            {
                Finished = false;
            }
        }

        /// <summary>
        /// Finishes the decoding by flushing any outstanding data to the output.
        /// </summary>
        /// <returns>true if the flush completed, false to indicate that there is more outstanding data.</returns>
        private unsafe bool Flush(Span<byte> output, ref int written)
        {
            Debug.Assert(_stream is not null);
            Debug.Assert(_available == 0);

            if (_needsFlushMarker)
            {
                _needsFlushMarker = false;

                // It's OK to use the flush marker like this, because it's pointer is unmovable.
                fixed (byte* flushMarkerPtr = FlushMarker)
                {
                    _stream.NextIn = (IntPtr)flushMarkerPtr;
                    _stream.AvailIn = FlushMarkerLength;
                }
            }

            if (_remainingByte is not null)
            {
                if (output.Length == written)
                {
                    return false;
                }
                output[written] = _remainingByte.GetValueOrDefault();
                _remainingByte = null;
                written += 1;
            }

            // If we have more space in the output, try to inflate
            if (output.Length > written)
            {
                written += Inflate(_stream, output[written..]);
            }

            // After inflate, if we have more space in the output then it means that we
            // have finished. Otherwise we need to manually check for more data.
            if (written < output.Length || IsFinished(_stream, out _remainingByte))
            {
                if (!_persisted)
                {
                    _streamPool.ReturnInflater(_stream);
                    _stream = null;
                }
                return true;
            }

            return false;
        }

        private void ReleaseBuffer()
        {
            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
                _available = 0;
                _position = 0;
            }
        }

        private static unsafe bool IsFinished(ZLibStreamHandle stream, out byte? remainingByte)
        {
            // There is no other way to make sure that we'e consumed all data
            // but to try to inflate again with at least one byte of output buffer.
            byte b;
            if (Inflate(stream, new Span<byte>(&b, 1)) == 0)
            {
                remainingByte = null;
                return true;
            }

            remainingByte = b;
            return false;
        }

        private static unsafe int Inflate(ZLibStreamHandle stream, Span<byte> destination)
        {
            Debug.Assert(destination.Length > 0);
            ErrorCode errorCode;

            fixed (byte* bufPtr = destination)
            {
                stream.NextOut = (IntPtr)bufPtr;
                stream.AvailOut = (uint)destination.Length;

                errorCode = stream.Inflate(FlushCode.NoFlush);

                if (errorCode is ErrorCode.Ok or ErrorCode.StreamEnd or ErrorCode.BufError)
                {
                    return destination.Length - (int)stream.AvailOut;
                }
            }

            string message = errorCode switch
            {
                ErrorCode.MemError => SR.ZLibErrorNotEnoughMemory,
                ErrorCode.DataError => SR.ZLibUnsupportedCompression,
                ErrorCode.StreamError => SR.ZLibErrorInconsistentStream,
                _ => string.Format(SR.ZLibErrorUnexpected, (int)errorCode)
            };
            throw new WebSocketException(message);
        }
    }
}
