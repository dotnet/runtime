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

        private readonly int _windowBits;
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
        /// The last added bytes to the inflater were part of the final
        /// payload for the message being sent.
        /// </summary>
        private bool _endOfMessage;

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
            _windowBits = -windowBits; // Negative for raw deflate
            _persisted = persisted;
        }

        public Memory<byte> Memory => _buffer.AsMemory(_position + _available);

        public Span<byte> Span => _buffer.AsSpan(_position + _available);

        public void Dispose()
        {
            if (_stream is not null)
            {
                _stream.Dispose();
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
                // Rent a buffer as close to the size of the user buffer as possible.
                // If the payload is smaller than the user buffer, rent only as much as we need.
                _buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(userBufferLength, payloadLength));
            }
        }

        public void AddBytes(int totalBytesReceived, bool endOfMessage)
        {
            Debug.Assert(totalBytesReceived == 0 || _buffer is not null, "Prepare must be called.");

            _available += totalBytesReceived;
            _endOfMessage = endOfMessage;

            if (endOfMessage)
            {
                if (_buffer is null)
                {
                    Debug.Assert(_available == 0);

                    _buffer = ArrayPool<byte>.Shared.Rent(FlushMarkerLength);
                    _available = FlushMarkerLength;
                    FlushMarker.CopyTo(_buffer);
                }
                else
                {
                    if (_buffer.Length < _available + FlushMarkerLength)
                    {
                        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(_available + FlushMarkerLength);
                        _buffer.AsSpan(0, _available).CopyTo(newBuffer);

                        byte[] toReturn = _buffer;
                        _buffer = newBuffer;

                        ArrayPool<byte>.Shared.Return(toReturn);
                    }

                    FlushMarker.CopyTo(_buffer.AsSpan(_available));
                    _available += FlushMarkerLength;
                }
            }
        }

        /// <summary>
        /// Inflates the last receive payload into the provided buffer.
        /// </summary>
        public unsafe bool Inflate(Span<byte> output, out int written)
        {
            _stream ??= CreateInflater();

            if (_available > 0 && output.Length > 0)
            {
                int consumed;

                fixed (byte* bufferPtr = _buffer)
                {
                    _stream.NextIn = (IntPtr)(bufferPtr + _position);
                    _stream.AvailIn = (uint)_available;

                    written = Inflate(_stream, output, FlushCode.NoFlush);
                    consumed = _available - (int)_stream.AvailIn;
                }

                _position += consumed;
                _available -= consumed;
            }
            else
            {
                written = 0;
            }

            if (_available == 0)
            {
                ReleaseBuffer();
                return _endOfMessage ? Finish(output, ref written) : true;
            }

            return false;
        }

        /// <summary>
        /// Finishes the decoding by flushing any outstanding data to the output.
        /// </summary>
        /// <returns>true if the flush completed, false to indicate that there is more outstanding data.</returns>
        private unsafe bool Finish(Span<byte> output, ref int written)
        {
            Debug.Assert(_stream is not null && _stream.AvailIn == 0);
            Debug.Assert(_available == 0);

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
                written += Inflate(_stream, output[written..], FlushCode.SyncFlush);
            }

            // After inflate, if we have more space in the output then it means that we
            // have finished. Otherwise we need to manually check for more data.
            if (written < output.Length || IsFinished(_stream, out _remainingByte))
            {
                if (!_persisted)
                {
                    _stream.Dispose();
                    _stream = null;
                }
                return true;
            }

            return false;
        }

        private void ReleaseBuffer()
        {
            if (_buffer is byte[] toReturn)
            {
                _buffer = null;
                _available = 0;
                _position = 0;

                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }

        private static unsafe bool IsFinished(ZLibStreamHandle stream, out byte? remainingByte)
        {
            // There is no other way to make sure that we've consumed all data
            // but to try to inflate again with at least one byte of output buffer.
            byte b;
            if (Inflate(stream, new Span<byte>(&b, 1), FlushCode.SyncFlush) == 0)
            {
                remainingByte = null;
                return true;
            }

            remainingByte = b;
            return false;
        }

        private static unsafe int Inflate(ZLibStreamHandle stream, Span<byte> destination, FlushCode flushCode)
        {
            Debug.Assert(destination.Length > 0);
            ErrorCode errorCode;

            fixed (byte* bufPtr = destination)
            {
                stream.NextOut = (IntPtr)bufPtr;
                stream.AvailOut = (uint)destination.Length;

                errorCode = stream.Inflate(flushCode);

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

        private ZLibStreamHandle CreateInflater()
        {
            ZLibStreamHandle? stream = null;
            ErrorCode errorCode;

            try
            {
                errorCode = CreateZLibStreamForInflate(out stream, _windowBits);
            }
            catch (Exception exception)
            {
                stream?.Dispose();
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, exception);
            }

            if (errorCode == ErrorCode.Ok)
            {
                return stream;
            }

            stream.Dispose();

            string message = errorCode == ErrorCode.MemError
                ? SR.ZLibErrorNotEnoughMemory
                : string.Format(SR.ZLibErrorUnexpected, (int)errorCode);
            throw new WebSocketException(message);
        }
    }
}
