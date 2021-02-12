// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;

using ZErrorCode = System.IO.Compression.ZLibNative.ErrorCode;
using ZFlushCode = System.IO.Compression.ZLibNative.FlushCode;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib compression API.
    /// </summary>
    internal sealed class Deflater : IDisposable
    {
        private readonly ZLibNative.ZLibStreamHandle _handle;
        private bool _isDisposed;

        // Note, DeflateStream or the deflater do not try to be thread safe.
        // The lock is just used to make writing to unmanaged structures atomic to make sure
        // that they do not get inconsistent fields that may lead to an unmanaged memory violation.
        // To prevent *managed* buffer corruption or other weird behaviour users need to synchronise
        // on the stream explicitly.
        private object SyncLock => this;

        internal Deflater(int windowBits)
        {
            var compressionLevel = ZLibNative.CompressionLevel.DefaultCompression;
            var memLevel = ZLibNative.Deflate_DefaultMemLevel;
            var strategy = ZLibNative.CompressionStrategy.DefaultStrategy;

            ZErrorCode errorCode;
            try
            {
                errorCode = ZLibNative.CreateZLibStreamForDeflate(out _handle, compressionLevel, windowBits, memLevel, strategy);
            }
            catch (Exception cause)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errorCode)
            {
                case ZErrorCode.Ok:
                    return;

                case ZErrorCode.MemError:
                    throw new WebSocketException(SR.ZLibErrorNotEnoughMemory);

                case ZErrorCode.VersionError:
                    throw new WebSocketException(SR.ZLibErrorVersionMismatch);

                case ZErrorCode.StreamError:
                    throw new WebSocketException(SR.ZLibErrorIncorrectInitParameters);

                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)errorCode));
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _handle.Dispose();
                _isDisposed = true;
            }
        }

        public unsafe void Deflate(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written)
        {
            fixed (byte* fixedInput = input)
            fixed (byte* fixedOutput = output)
            {
                _handle.NextIn = (IntPtr)fixedInput;
                _handle.AvailIn = (uint)input.Length;

                _handle.NextOut = (IntPtr)fixedOutput;
                _handle.AvailOut = (uint)output.Length;

                Deflate(ZFlushCode.NoFlush);

                consumed = input.Length - (int)_handle.AvailIn;
                written = output.Length - (int)_handle.AvailOut;
            }
        }

        public unsafe int Finish(Span<byte> output, out bool completed)
        {
            fixed (byte* fixedOutput = output)
            {
                _handle.NextIn = IntPtr.Zero;
                _handle.AvailIn = 0;

                _handle.NextOut = (IntPtr)fixedOutput;
                _handle.AvailOut = (uint)output.Length;

                var errorCode = Deflate(ZFlushCode.SyncFlush);
                var writtenBytes = output.Length - (int)_handle.AvailOut;

                completed = errorCode == ZErrorCode.Ok && writtenBytes < output.Length;

                return writtenBytes;
            }
        }

        private ZErrorCode Deflate(ZFlushCode flushCode)
        {
            ZErrorCode errorCode;
            try
            {
                errorCode = _handle.Deflate(flushCode);
            }
            catch (Exception cause)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errorCode)
            {
                case ZErrorCode.Ok:
                case ZErrorCode.StreamEnd:
                    return errorCode;

                case ZErrorCode.BufError:
                    return errorCode;  // This is a recoverable error

                case ZErrorCode.StreamError:
                    throw new WebSocketException(SR.ZLibErrorInconsistentStream);

                default:
                    throw new WebSocketException(string.Format(SR.ZLibErrorUnexpected, (int)errorCode));
            }
        }
    }
}
