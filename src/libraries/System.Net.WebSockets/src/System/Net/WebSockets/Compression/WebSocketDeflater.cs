// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.IO.Compression.ZLibNative;

namespace System.Net.WebSockets.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib compression API.
    /// </summary>
    internal sealed class WebSocketDeflater : IDisposable
    {
        private ZLibStreamHandle? _stream;
        private readonly int _windowBits;
        private readonly bool _persisted;

        private byte[]? _buffer;

        internal WebSocketDeflater(int windowBits, bool persisted)
        {
            Debug.Assert(windowBits >= 9 && windowBits <= 15);

            // We use negative window bits in order to produce raw deflate data
            _windowBits = -windowBits;
            _persisted = persisted;
        }

        public void Dispose() => _stream?.Dispose();

        public void ReleaseBuffer()
        {
            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }
        }

        public ReadOnlySpan<byte> Deflate(ReadOnlySpan<byte> payload, bool continuation, bool endOfMessage)
        {
            Debug.Assert(_buffer is null, "Invalid state, ReleaseBuffer not called.");

            // Do not try to rent more than 1MB initially, because it will actually allocate
            // instead of renting. Be optimistic that what we're sending is actually going to fit.
            const int MaxInitialBufferLength = 1024 * 1024;

            // For small payloads there might actually be overhead in the compression and the resulting
            // output might be larger than the payload. This is why we rent at least 4KB initially.
            const int MinInitialBufferLength = 4 * 1024;

            _buffer = ArrayPool<byte>.Shared.Rent(Math.Clamp(payload.Length, MinInitialBufferLength, MaxInitialBufferLength));
            int position = 0;

            while (true)
            {
                DeflatePrivate(payload, _buffer.AsSpan(position), continuation, endOfMessage,
                    out int consumed, out int written, out bool needsMoreOutput);
                position += written;

                if (!needsMoreOutput)
                {
                    break;
                }

                payload = payload.Slice(consumed);

                // Rent a 30% bigger buffer
                byte[] newBuffer = ArrayPool<byte>.Shared.Rent((int)(_buffer.Length * 1.3));
                _buffer.AsSpan(0, position).CopyTo(newBuffer);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }

            return new ReadOnlySpan<byte>(_buffer, 0, position);
        }

        private void DeflatePrivate(ReadOnlySpan<byte> payload, Span<byte> output, bool continuation, bool endOfMessage,
            out int consumed, out int written, out bool needsMoreOutput)
        {
            Debug.Assert(!continuation || _stream is not null, "Invalid state. The stream should not be null in continuations.");

            if (_stream is null)
            {
                Initialize();
            }

            if (payload.IsEmpty)
            {
                consumed = 0;
                written = 0;
            }
            else
            {
                UnsafeDeflate(payload, output, out consumed, out written, out needsMoreOutput);

                if (needsMoreOutput)
                {
                    Debug.Assert(written == output.Length);
                    return;
                }
            }

            written += UnsafeFlush(output.Slice(written), out needsMoreOutput);

            if (needsMoreOutput)
            {
                Debug.Assert(written == output.Length);
                return;
            }
            Debug.Assert(output.Slice(written - WebSocketInflater.FlushMarkerLength, WebSocketInflater.FlushMarkerLength)
                               .EndsWith(WebSocketInflater.FlushMarker), "The deflated block must always end with a flush marker.");

            if (endOfMessage)
            {
                // As per RFC we need to remove the flush markers
                written -= WebSocketInflater.FlushMarkerLength;
            }

            if (endOfMessage && !_persisted)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        private unsafe void UnsafeDeflate(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written, out bool needsMoreBuffer)
        {
            Debug.Assert(_stream is not null);

            fixed (byte* fixedInput = input)
            fixed (byte* fixedOutput = output)
            {
                _stream.NextIn = (IntPtr)fixedInput;
                _stream.AvailIn = (uint)input.Length;

                _stream.NextOut = (IntPtr)fixedOutput;
                _stream.AvailOut = (uint)output.Length;

                // The flush is set to Z_NO_FLUSH, which allows deflate to decide
                // how much data to accumulate before producing output,
                // in order to maximize compression.
                var errorCode = Deflate(_stream, FlushCode.NoFlush);

                consumed = input.Length - (int)_stream.AvailIn;
                written = output.Length - (int)_stream.AvailOut;

                needsMoreBuffer = errorCode == ErrorCode.BufError;
            }
        }

        private unsafe int UnsafeFlush(Span<byte> output, out bool needsMoreBuffer)
        {
            Debug.Assert(_stream is not null);
            Debug.Assert(_stream.AvailIn == 0);
            Debug.Assert(output.Length >= 6, "We neede at least 6 bytes guarantee the completion of the deflate block.");

            fixed (byte* fixedOutput = output)
            {
                _stream.NextIn = IntPtr.Zero;
                _stream.AvailIn = 0;

                _stream.NextOut = (IntPtr)fixedOutput;
                _stream.AvailOut = (uint)output.Length;

                // The flush is set to Z_SYNC_FLUSH, all pending output is flushed
                // to the output buffer and the output is aligned on a byte boundary,
                // so that the decompressor can get all input data available so far.
                // This completes the current deflate block and follows it with an empty
                // stored block that is three bits plus filler bits to the next byte,
                // followed by four bytes (00 00 ff ff).
                ErrorCode errorCode = Deflate(_stream, FlushCode.SyncFlush);
                Debug.Assert(errorCode is ErrorCode.Ok or ErrorCode.BufError);

                needsMoreBuffer = errorCode == ErrorCode.BufError;
                return output.Length - (int)_stream.AvailOut;
            }
        }

        private static ErrorCode Deflate(ZLibStreamHandle stream, FlushCode flushCode)
        {
            ErrorCode errorCode;
            try
            {
                errorCode = stream.Deflate(flushCode);
            }
            catch (Exception cause)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errorCode)
            {
                case ErrorCode.Ok:
                case ErrorCode.StreamEnd:
                case ErrorCode.BufError:
                    return errorCode;

                case ErrorCode.StreamError:
                    throw new WebSocketException(SR.ZLibErrorInconsistentStream);

                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)errorCode));
            }
        }

        [MemberNotNull(nameof(_stream))]
        private void Initialize()
        {
            Debug.Assert(_stream is null);

            var compressionLevel = CompressionLevel.DefaultCompression;
            var memLevel = Deflate_DefaultMemLevel;
            var strategy = CompressionStrategy.DefaultStrategy;

            ErrorCode errorCode;
            try
            {
                errorCode = CreateZLibStreamForDeflate(out _stream, compressionLevel, _windowBits, memLevel, strategy);
            }
            catch (Exception cause)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errorCode)
            {
                case ErrorCode.Ok:
                    return;
                case ErrorCode.MemError:
                    throw new WebSocketException(SR.ZLibErrorNotEnoughMemory);
                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)errorCode));
            }
        }
    }
}
