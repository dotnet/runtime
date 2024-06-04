// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Security;

using ZErrorCode = System.IO.Compression.ZLibNative.ErrorCode;
using ZFlushCode = System.IO.Compression.ZLibNative.FlushCode;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib compression API.
    /// </summary>
    internal sealed class Deflater : IDisposable
    {
        private readonly ZLibNative.ZLibStreamHandle _zlibStream;
        private MemoryHandle _inputBufferHandle;
        private bool _isDisposed;
        private const int minWindowBits = -15;  // WindowBits must be between -8..-15 to write no header, 8..15 for a
        private const int maxWindowBits = 31;   // zlib header, or 24..31 for a GZip header

        // Note, DeflateStream or the deflater do not try to be thread safe.
        // The lock is just used to make writing to unmanaged structures atomic to make sure
        // that they do not get inconsistent fields that may lead to an unmanaged memory violation.
        // To prevent *managed* buffer corruption or other weird behaviour users need to synchronise
        // on the stream explicitly.
        private object SyncLock => this;

        internal Deflater(CompressionLevel compressionLevel, int windowBits)
        {
            Debug.Assert(windowBits >= minWindowBits && windowBits <= maxWindowBits);
            ZLibNative.CompressionLevel zlibCompressionLevel;
            int memLevel;

            switch (compressionLevel)
            {
                // See the note in ZLibNative.CompressionLevel for the recommended combinations.

                case CompressionLevel.Optimal:
                    zlibCompressionLevel = ZLibNative.CompressionLevel.DefaultCompression;
                    memLevel = ZLibNative.Deflate_DefaultMemLevel;
                    break;

                case CompressionLevel.Fastest:
                    zlibCompressionLevel = ZLibNative.CompressionLevel.BestSpeed;
                    memLevel = ZLibNative.Deflate_DefaultMemLevel;
                    break;

                case CompressionLevel.NoCompression:
                    zlibCompressionLevel = ZLibNative.CompressionLevel.NoCompression;
                    memLevel = ZLibNative.Deflate_NoCompressionMemLevel;
                    break;

                case CompressionLevel.SmallestSize:
                    zlibCompressionLevel = ZLibNative.CompressionLevel.BestCompression;
                    memLevel = ZLibNative.Deflate_DefaultMemLevel;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(compressionLevel));
            }

            ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;

            ZErrorCode errC;
            try
            {
                errC = ZLibNative.CreateZLibStreamForDeflate(out _zlibStream, zlibCompressionLevel,
                                                             windowBits, memLevel, strategy);
            }
            catch (Exception cause)
            {
                throw new ZLibException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errC)
            {
                case ZErrorCode.Ok:
                    return;

                case ZErrorCode.MemError:
                    throw new ZLibException(SR.ZLibErrorNotEnoughMemory, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

                case ZErrorCode.VersionError:
                    throw new ZLibException(SR.ZLibErrorVersionMismatch, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

                case ZErrorCode.StreamError:
                    throw new ZLibException(SR.ZLibErrorIncorrectInitParameters, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());

                default:
                    throw new ZLibException(SR.ZLibErrorUnexpected, "deflateInit2_", (int)errC, _zlibStream.GetErrorMessage());
            }
        }

        ~Deflater()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _zlibStream.Dispose();

                DeallocateInputBufferHandle();
                _isDisposed = true;
            }
        }

        public bool NeedsInput() => 0 == _zlibStream.AvailIn;

        internal unsafe void SetInput(ReadOnlyMemory<byte> inputBuffer)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(!inputBuffer.IsEmpty);

            lock (SyncLock)
            {
                _inputBufferHandle = inputBuffer.Pin();

                _zlibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
                _zlibStream.AvailIn = (uint)inputBuffer.Length;
            }
        }

        internal unsafe void SetInput(byte* inputBufferPtr, int count)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(inputBufferPtr != null);
            Debug.Assert(count > 0);

            lock (SyncLock)
            {
                _zlibStream.NextIn = (IntPtr)inputBufferPtr;
                _zlibStream.AvailIn = (uint)count;
            }
        }

        internal int GetDeflateOutput(byte[] outputBuffer)
        {
            Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
            Debug.Assert(!NeedsInput(), "GetDeflateOutput should only be called after providing input");

            try
            {
                int bytesRead;
                ReadDeflateOutput(outputBuffer, ZFlushCode.NoFlush, out bytesRead);
                return bytesRead;
            }
            finally
            {
                // Before returning, make sure to release input buffer if necessary:
                if (0 == _zlibStream.AvailIn)
                {
                    DeallocateInputBufferHandle();
                }
            }
        }

        private unsafe ZErrorCode ReadDeflateOutput(byte[] outputBuffer, ZFlushCode flushCode, out int bytesRead)
        {
            Debug.Assert(outputBuffer?.Length > 0);

            lock (SyncLock)
            {
                fixed (byte* bufPtr = &outputBuffer[0])
                {
                    _zlibStream.NextOut = (IntPtr)bufPtr;
                    _zlibStream.AvailOut = (uint)outputBuffer.Length;

                    ZErrorCode errC = Deflate(flushCode);
                    bytesRead = outputBuffer.Length - (int)_zlibStream.AvailOut;

                    return errC;
                }
            }
        }

        internal bool Finish(byte[] outputBuffer, out int bytesRead)
        {
            Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
            Debug.Assert(outputBuffer.Length > 0, "Can't pass in an empty output buffer!");

            ZErrorCode errC = ReadDeflateOutput(outputBuffer, ZFlushCode.Finish, out bytesRead);
            return errC == ZErrorCode.StreamEnd;
        }

        /// <summary>
        /// Returns true if there was something to flush. Otherwise False.
        /// </summary>
        internal bool Flush(byte[] outputBuffer, out int bytesRead)
        {
            Debug.Assert(null != outputBuffer, "Can't pass in a null output buffer!");
            Debug.Assert(outputBuffer.Length > 0, "Can't pass in an empty output buffer!");
            Debug.Assert(NeedsInput(), "We have something left in previous input!");


            // Note: we require that NeedsInput() == true, i.e. that 0 == _zlibStream.AvailIn.
            // If there is still input left we should never be getting here; instead we
            // should be calling GetDeflateOutput.

            return ReadDeflateOutput(outputBuffer, ZFlushCode.SyncFlush, out bytesRead) == ZErrorCode.Ok;
        }

        private void DeallocateInputBufferHandle()
        {
            lock (SyncLock)
            {
                _zlibStream.AvailIn = 0;
                _zlibStream.NextIn = ZLibNative.ZNullPtr;
                _inputBufferHandle.Dispose();
            }
        }

        private ZErrorCode Deflate(ZFlushCode flushCode)
        {
            ZErrorCode errC;
            try
            {
                errC = _zlibStream.Deflate(flushCode);
            }
            catch (Exception cause)
            {
                throw new ZLibException(SR.ZLibErrorDLLLoadError, cause);
            }

            switch (errC)
            {
                case ZErrorCode.Ok:
                case ZErrorCode.StreamEnd:
                    return errC;

                case ZErrorCode.BufError:
                    return errC;  // This is a recoverable error

                case ZErrorCode.StreamError:
                    throw new ZLibException(SR.ZLibErrorInconsistentStream, "deflate", (int)errC, _zlibStream.GetErrorMessage());

                default:
                    throw new ZLibException(SR.ZLibErrorUnexpected, "deflate", (int)errC, _zlibStream.GetErrorMessage());
            }
        }
    }
}
