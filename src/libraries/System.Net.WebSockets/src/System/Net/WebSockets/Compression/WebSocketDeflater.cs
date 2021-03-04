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

        internal WebSocketDeflater(int windowBits, bool persisted)
        {
            Debug.Assert(windowBits >= 9 && windowBits <= 15);

            // We use negative window bits in order to produce raw deflate data
            _windowBits = -windowBits;
            _persisted = persisted;
        }

        public void Dispose() => _stream?.Dispose();

        public void Deflate( ReadOnlySpan<byte> payload, Span<byte> output, bool continuation, bool endOfMessage,
            out int consumed, out int written, out bool needsMoreOutput)
        {
            Debug.Assert(!continuation || _stream is not null, "Invalid state. The stream should not be null in continuations.");

            if (_stream is null)
            {
                Initialize();
            }

            Deflate(payload, output, out consumed, out written, out needsMoreOutput);
            if (needsMoreOutput)
            {
                return;
            }

            // See comment by Mark Adler https://github.com/madler/zlib/issues/149#issuecomment-225237457
            // At that point there will be at most a few bits left to write.
            // Then call deflate() with Z_FULL_FLUSH and no more input and at least six bytes of available output.
            written += Flush(output.Slice(written), out needsMoreOutput);

            if (!needsMoreOutput)
            {
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
        }

        private unsafe void Deflate(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written, out bool needsMoreBuffer)
        {
            Debug.Assert(_stream is not null);

            fixed (byte* fixedInput = input)
            fixed (byte* fixedOutput = output)
            {
                _stream.NextIn = (IntPtr)fixedInput;
                _stream.AvailIn = (uint)input.Length;

                _stream.NextOut = (IntPtr)fixedOutput;
                _stream.AvailOut = (uint)output.Length;

                // If flush is set to Z_BLOCK, a deflate block is completed
                // and emitted, as for Z_SYNC_FLUSH, but the output
                // is not aligned on a byte boundary, and up to seven bits
                // of the current block are held to be written as the next byte after
                // the next deflate block is completed.
                var errorCode = Deflate(_stream, (FlushCode)5/*Z_BLOCK*/);

                consumed = input.Length - (int)_stream.AvailIn;
                written = output.Length - (int)_stream.AvailOut;

                needsMoreBuffer = errorCode == ErrorCode.BufError;
            }
        }

        private unsafe int Flush(Span<byte> output, out bool needsMoreBuffer)
        {
            Debug.Assert(_stream is not null);
            Debug.Assert(_stream.AvailIn == 0);
            Debug.Assert(output.Length >= 6);

            fixed (byte* fixedOutput = output)
            {
                _stream.NextIn = IntPtr.Zero;
                _stream.AvailIn = 0;

                _stream.NextOut = (IntPtr)fixedOutput;
                _stream.AvailOut = (uint)output.Length;

                ErrorCode errorCode = Deflate(_stream, (FlushCode)3/*Z_FULL_FLUSH*/);
                int writtenBytes = output.Length - (int)_stream.AvailOut;

                needsMoreBuffer = errorCode == ErrorCode.BufError;
                return writtenBytes;
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
                    return errorCode;

                case ErrorCode.BufError:
                    return errorCode;  // This is a recoverable error

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
