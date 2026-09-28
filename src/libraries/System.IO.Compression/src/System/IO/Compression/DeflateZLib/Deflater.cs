// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

using ZErrorCode = System.IO.Compression.ZLibNative.ErrorCode;
using ZFlushCode = System.IO.Compression.ZLibNative.FlushCode;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib compression API.
    /// </summary>
    internal sealed class Deflater : IDisposable
    {
        private const int MaxPooledDeflaters = 16;
        private static readonly object s_poolLock = new();
        private static readonly Deflater?[] s_pool = new Deflater?[MaxPooledDeflaters];
        private readonly ZLibNative.ZLibStreamHandle _zlibStream;
        private readonly ZLibNative.CompressionLevel _compressionLevel;
        private readonly ZLibNative.CompressionStrategy _strategy;
        private readonly int _windowBits;
        private readonly int _memLevel;
        private MemoryHandle _inputBufferHandle;
        private bool _isDisposed;
        private const int minWindowBits = -15;  // WindowBits must be between -8..-15 to write no header, 8..15 for a
        private const int maxWindowBits = 31;   // zlib header, or 24..31 for a GZip header

        // Note, DeflateStream or the deflater do not try to be thread safe.
        // The lock is just used to make writing to unmanaged structures atomic to make sure
        // that they do not get inconsistent fields that may lead to an unmanaged memory violation.
        // To prevent *managed* buffer corruption or other weird behavior users need to synchronize
        // on the stream explicitly.
        private object SyncLock => this;

        private Deflater(ZLibNative.ZLibStreamHandle zlibStream, ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel)
        {
            _zlibStream = zlibStream;
            _compressionLevel = compressionLevel;
            _strategy = strategy;
            _windowBits = windowBits;
            _memLevel = memLevel;
        }

        ~Deflater()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    lock (SyncLock)
                    {
                        DeallocateInputBufferHandleCore(resetStreamHandle: true);
                        _zlibStream.NextOut = ZLibNative.ZNullPtr;
                        _zlibStream.AvailOut = 0;
                        _isDisposed = true;
                    }

                    if (!TryReturnToPool(this))
                    {
                        _zlibStream.Dispose();
                    }
                }
                else
                {
                    // Unpin the input buffer, but avoid modifying the ZLibStreamHandle (which may have been disposed of).
                    DeallocateInputBufferHandle(resetStreamHandle: false);
                    _isDisposed = true;
                }
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
                    DeallocateInputBufferHandle(resetStreamHandle: true);
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

        /// <summary>
        /// Discards any unconsumed input previously set via SetInput, releasing the pinned reference (if any).
        /// Must be called if an in-progress operation is abandoned (e.g. due to an exception or cancellation) so
        /// the deflater doesn't retain a dangling reference to a buffer the caller may have since reused or freed.
        /// </summary>
        internal void UnsetInput()
        {
            lock (SyncLock)
            {
                // On the common success path all input has already been consumed (NeedsInput() is true),
                // so there is nothing to abandon and no cleanup is necessary. Only when an operation is
                // abandoned mid-input do we need to reset state and release any pinned reference. This check
                // must happen under the lock so it can't race with a concurrent SetInput/Dispose call.
                if (!NeedsInput())
                {
                    DeallocateInputBufferHandleCore(resetStreamHandle: true);
                }
            }
        }

        private void DeallocateInputBufferHandle(bool resetStreamHandle)
        {
            lock (SyncLock)
            {
                DeallocateInputBufferHandleCore(resetStreamHandle);
            }
        }

        // Must be called while holding SyncLock.
        private void DeallocateInputBufferHandleCore(bool resetStreamHandle)
        {
            if (resetStreamHandle)
            {
                _zlibStream.AvailIn = 0;
                _zlibStream.NextIn = ZLibNative.ZNullPtr;
            }

            _inputBufferHandle.Dispose();
            _inputBufferHandle = default;
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

        public static Deflater CreateDeflater(ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel)
        {
            Debug.Assert(windowBits >= minWindowBits && windowBits <= maxWindowBits);

            Deflater? deflater = RentDeflater(compressionLevel, strategy, windowBits, memLevel);
            if (deflater is not null)
            {
                if (deflater.Reset())
                {
                    return deflater;
                }

                deflater._zlibStream.Dispose();
            }

            ZLibNative.ZLibStreamHandle zlibStream = ZLibNative.ZLibStreamHandle.CreateForDeflate(compressionLevel, windowBits, memLevel, strategy);

            return new Deflater(zlibStream, compressionLevel, strategy, windowBits, memLevel);
        }

        private bool Reset()
        {
            lock (SyncLock)
            {
                Debug.Assert(_isDisposed);

                DeallocateInputBufferHandleCore(resetStreamHandle: true);
                _zlibStream.NextOut = ZLibNative.ZNullPtr;
                _zlibStream.AvailOut = 0;

                if (_zlibStream.DeflateReset() != ZErrorCode.Ok)
                {
                    return false;
                }

                _isDisposed = false;
                GC.ReRegisterForFinalize(this);
                return true;
            }
        }

        private bool Matches(ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel) =>
            _compressionLevel == compressionLevel &&
            _strategy == strategy &&
            _windowBits == windowBits &&
            _memLevel == memLevel;

        private static Deflater? RentDeflater(ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel)
        {
            lock (s_poolLock)
            {
                for (int i = 0; i < s_pool.Length; i++)
                {
                    Deflater? deflater = s_pool[i];
                    if (deflater is not null && deflater.Matches(compressionLevel, strategy, windowBits, memLevel))
                    {
                        s_pool[i] = null;
                        return deflater;
                    }
                }
            }

            return null;
        }

        private static bool TryReturnToPool(Deflater deflater)
        {
            Deflater? evictedDeflater = null;
            lock (s_poolLock)
            {
                for (int i = 0; i < s_pool.Length; i++)
                {
                    if (s_pool[i] is null)
                    {
                        s_pool[i] = deflater;
                        return true;
                    }
                }

                evictedDeflater = s_pool[^1];
                s_pool[^1] = deflater;
            }

            evictedDeflater!._zlibStream.Dispose();
            return true;
        }
    }
}
