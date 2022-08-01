// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using static System.IO.Compression.ZLibNative;

namespace System.Net.WebSockets.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib compression API.
    /// </summary>
    internal sealed class WebSocketDeflater : IDisposable
    {
        private readonly int _windowBits;
        private ZLibStreamHandle? _stream;
        private readonly bool _persisted;

        private byte[]? _buffer;

        internal WebSocketDeflater(int windowBits, bool persisted)
        {
            _windowBits = -windowBits; // Negative for raw deflate
            _persisted = persisted;
        }

        public void Dispose()
        {
            if (_stream is not null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public void ReleaseBuffer()
        {
            if (_buffer is byte[] toReturn)
            {
                _buffer = null;
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }

        public ReadOnlySpan<byte> Deflate(ReadOnlySpan<byte> payload, bool endOfMessage)
        {
            Debug.Assert(_buffer is null, "Invalid state, ReleaseBuffer not called.");

            // For small payloads there might actually be overhead in the compression and the resulting
            // output might be larger than the payload. This is why we rent at least 4KB initially.
            const int MinInitialBufferLength = 4 * 1024;

            _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(payload.Length, MinInitialBufferLength));
            int position = 0;

            while (true)
            {
                DeflatePrivate(payload, _buffer.AsSpan(position), endOfMessage,
                    out int consumed, out int written, out bool needsMoreOutput);
                position += written;

                if (!needsMoreOutput)
                {
                    Debug.Assert(consumed == payload.Length);
                    break;
                }

                payload = payload.Slice(consumed);

                // Rent a 30% bigger buffer
                byte[] newBuffer = ArrayPool<byte>.Shared.Rent((int)(_buffer.Length * 1.3));
                _buffer.AsSpan(0, position).CopyTo(newBuffer);

                byte[] toReturn = _buffer;
                _buffer = newBuffer;

                ArrayPool<byte>.Shared.Return(toReturn);
            }

            return new ReadOnlySpan<byte>(_buffer, 0, position);
        }

        private void DeflatePrivate(ReadOnlySpan<byte> payload, Span<byte> output, bool endOfMessage,
            out int consumed, out int written, out bool needsMoreOutput)
        {
            _stream ??= CreateDeflater();

            if (payload.Length == 0)
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

                // It is important here to also check that we haven't
                // exhausted the output buffer because after deflating we're
                // always going to issue a flush and a flush with empty output
                // is going to throw.
                needsMoreBuffer = errorCode == ErrorCode.BufError
                    || _stream.AvailIn > 0
                    || written == output.Length;
            }
        }

        private unsafe int UnsafeFlush(Span<byte> output, out bool needsMoreBuffer)
        {
            Debug.Assert(_stream is not null);
            Debug.Assert(_stream.AvailIn == 0);
            Debug.Assert(output.Length > 0);

            fixed (byte* fixedOutput = output)
            {
                _stream.NextIn = IntPtr.Zero;
                _stream.AvailIn = 0;

                _stream.NextOut = (IntPtr)fixedOutput;
                _stream.AvailOut = (uint)output.Length;

                // We need to use Z_BLOCK_FLUSH to instruct the zlib to flush all outstanding
                // data but also not to emit a deflate block boundary. After we know that there is no
                // more data, we can safely proceed to instruct the library to emit the boundary markers.
                ErrorCode errorCode = Deflate(_stream, FlushCode.Block);
                Debug.Assert(errorCode is ErrorCode.Ok or ErrorCode.BufError);

                // We need at least 6 bytes to guarantee that we can emit a deflate block boundary.
                needsMoreBuffer = _stream.AvailOut < 6;

                if (!needsMoreBuffer)
                {
                    // The flush is set to Z_SYNC_FLUSH, all pending output is flushed
                    // to the output buffer and the output is aligned on a byte boundary,
                    // so that the decompressor can get all input data available so far.
                    // This completes the current deflate block and follows it with an empty
                    // stored block that is three bits plus filler bits to the next byte,
                    // followed by four bytes (00 00 ff ff).
                    errorCode = Deflate(_stream, FlushCode.SyncFlush);
                    Debug.Assert(errorCode == ErrorCode.Ok);
                }

                return output.Length - (int)_stream.AvailOut;
            }
        }

        private static ErrorCode Deflate(ZLibStreamHandle stream, FlushCode flushCode)
        {
            ErrorCode errorCode = stream.Deflate(flushCode);

            if (errorCode is ErrorCode.Ok or ErrorCode.StreamEnd or ErrorCode.BufError)
            {
                return errorCode;
            }

            string message = errorCode == ErrorCode.StreamError
                ? SR.ZLibErrorInconsistentStream
                : string.Format(SR.ZLibErrorUnexpected, (int)errorCode);
            throw new WebSocketException(message);
        }

        private ZLibStreamHandle CreateDeflater()
        {
            ZLibStreamHandle? stream = null;
            ErrorCode errorCode;
            try
            {
                errorCode = CreateZLibStreamForDeflate(out stream,
                    level: CompressionLevel.DefaultCompression,
                    windowBits: _windowBits,
                    memLevel: Deflate_DefaultMemLevel,
                    strategy: CompressionStrategy.DefaultStrategy);
            }
            catch (Exception cause)
            {
                stream?.Dispose();
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
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
