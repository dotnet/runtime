// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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

        private ZLibStreamHandle? _stream;
        private readonly int _windowBits;
        private readonly bool _persisted;

        /// <summary>
        /// There is no way of knowing, when decoding data, if the underlying deflater
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
            Debug.Assert(windowBits >= 9 && windowBits <= 15);

            // We use negative window bits to instruct deflater to expect raw deflate data
            _windowBits = -windowBits;
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
            _stream?.Dispose();
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

            if (_available > 0)
            {
                if (_stream is null)
                {
                    Initialize();
                }

                int consumed;

                fixed (byte* fixedInput = _buffer)
                fixed (byte* fixedOutput = &MemoryMarshal.GetReference(output))
                {
                    _stream.NextIn = (IntPtr)(fixedInput + _position);
                    _stream.AvailIn = (uint)_available;

                    _stream.NextOut = (IntPtr)fixedOutput;
                    _stream.AvailOut = (uint)output.Length;

                    Inflate(_stream);

                    consumed = _available - (int)_stream.AvailIn;
                    written = output.Length - (int)_stream.AvailOut;
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

                if (flush)
                {
                    Finished = Flush(output.Slice(written), out int byteCount);
                    written += byteCount;
                }
                else
                {
                    Finished = true;
                }
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
        private unsafe bool Flush(Span<byte> output, out int written)
        {
            Debug.Assert(_stream is not null);
            Debug.Assert(_available == 0);

            if (_needsFlushMarker)
            {
                fixed (byte* fixedInput = &MemoryMarshal.GetReference(FlushMarker))
                fixed (byte* fixedOutput = &MemoryMarshal.GetReference(output))
                {
                    _stream.NextIn = (IntPtr)fixedInput;
                    _stream.AvailIn = (uint)FlushMarkerLength;

                    _stream.NextOut = (IntPtr)fixedOutput;
                    _stream.AvailOut = (uint)output.Length;

                    Inflate(_stream);

                    written = output.Length - (int)_stream.AvailOut;
                }

                _needsFlushMarker = false;

                if (written < output.Length || IsFinished(_stream, out _remainingByte))
                {
                    OnFinished();
                    return true;
                }
            }

            written = 0;

            if (output.IsEmpty)
            {
                if (_remainingByte is not null)
                {
                    return false;
                }
                if (IsFinished(_stream, out _remainingByte))
                {
                    OnFinished();
                    return true;
                }
            }
            else
            {
                if (_remainingByte is not null)
                {
                    output[0] = _remainingByte.GetValueOrDefault();
                    written = 1;
                    _remainingByte = null;
                }

                written += Inflate(_stream, output[written..]);

                if (written < output.Length || IsFinished(_stream, out _remainingByte))
                {
                    OnFinished();
                    return true;
                }
            }

            return false;
        }

        private void OnFinished()
        {
            Debug.Assert(_stream is not null);

            if (!_persisted)
            {
                _stream.Dispose();
                _stream = null;
            }
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
            if (stream.AvailIn > 0)
            {
                remainingByte = null;
                return false;
            }

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
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(destination))
            {
                stream.NextOut = (IntPtr)bufPtr;
                stream.AvailOut = (uint)destination.Length;

                Inflate(stream);
                return destination.Length - (int)stream.AvailOut;
            }
        }

        private static void Inflate(ZLibStreamHandle stream)
        {
            ErrorCode errorCode;
            try
            {
                errorCode = stream.Inflate(FlushCode.NoFlush);
            }
            catch (Exception cause) // could not load the Zlib DLL correctly
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }
            switch (errorCode)
            {
                case ErrorCode.Ok:           // progress has been made inflating
                case ErrorCode.StreamEnd:    // The end of the input stream has been reached
                case ErrorCode.BufError:     // No room in the output buffer - inflate() can be called again with more space to continue
                    break;

                case ErrorCode.MemError:     // Not enough memory to complete the operation
                    throw new WebSocketException(SR.ZLibErrorNotEnoughMemory);

                case ErrorCode.DataError:    // The input data was corrupted (input stream not conforming to the zlib format or incorrect check value)
                    throw new WebSocketException(SR.ZLibUnsupportedCompression);

                case ErrorCode.StreamError:  //the stream structure was inconsistent (for example if next_in or next_out was NULL),
                    throw new WebSocketException(SR.ZLibErrorInconsistentStream);

                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)errorCode));
            }
        }

        [MemberNotNull(nameof(_stream))]
        private void Initialize()
        {
            Debug.Assert(_stream is null);

            ErrorCode error;
            try
            {
                error = CreateZLibStreamForInflate(out _stream, _windowBits);
            }
            catch (Exception exception)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, exception);
            }

            switch (error)
            {
                case ErrorCode.Ok:
                    return;
                case ErrorCode.MemError:
                    throw new WebSocketException(SR.ZLibErrorNotEnoughMemory);
                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)error));
            }
        }
    }
}
