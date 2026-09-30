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
        // Reuses native zlib states via deflateReset() instead of allocating/freeing one per Deflater,
        // avoiding the page-fault/heap-contention regression from zlib-ng's larger single allocation
        // per deflate state (see https://github.com/dotnet/runtime/issues/134700).
        // Sixteen maximum-sized states cap idle native memory at roughly 5.2 MiB.
        private const int MaxPooledDeflateStates = 16;
        private static readonly object s_poolLock = new();
        private static readonly DeflaterState?[] s_pool = new DeflaterState?[MaxPooledDeflateStates];
        private static int s_nextEvictionIndex;
        private DeflaterState _state;
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

        private Deflater(DeflaterState state)
        {
            _state = state;
        }

        private ZLibNative.ZLibStreamHandle ZLibStream =>
            _state.Stream;

        ~Deflater()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            try
            {
                Dispose(true);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (!disposing)
            {
                try
                {
                    // Unpin the input buffer, but avoid modifying the ZLibStreamHandle (which may have been disposed of).
                    DeallocateInputBufferHandle(resetStreamHandle: false);
                }
                catch
                {
                    // Finalization must not allow exceptions from a custom memory manager to escape.
                }

                _state = default;
                _isDisposed = true;
                return;
            }

            ZLibNative.ZLibStreamHandle? zlibStream = null;
            DeflaterState state;
            try
            {
                lock (SyncLock)
                {
                    // Re-check under the lock: a racing Dispose() may have already won and detached
                    // _state, in which case this call must be a no-op rather than observe ObjectDisposedException.
                    if (_isDisposed)
                    {
                        return;
                    }

                    zlibStream = ZLibStream;
                    zlibStream.NextOut = ZLibNative.ZNullPtr;
                    zlibStream.AvailOut = 0;
                    DeallocateInputBufferHandleCore(resetStreamHandle: true);
                    state = _state;
                    _state = default; // Detach native state before transferring it to the pool.
                    _isDisposed = true;
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    _state = default;
                    _isDisposed = true;
                }
                zlibStream?.Dispose();
                throw;
            }

            ReturnToPool(state);
        }

        public bool NeedsInput() => 0 == ZLibStream.AvailIn;

        internal unsafe void SetInput(ReadOnlyMemory<byte> inputBuffer)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(!inputBuffer.IsEmpty);

            lock (SyncLock)
            {
                _inputBufferHandle = inputBuffer.Pin();

                ZLibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
                ZLibStream.AvailIn = (uint)inputBuffer.Length;
            }
        }

        internal unsafe void SetInput(byte* inputBufferPtr, int count)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(inputBufferPtr != null);
            Debug.Assert(count > 0);

            lock (SyncLock)
            {
                ZLibStream.NextIn = (IntPtr)inputBufferPtr;
                ZLibStream.AvailIn = (uint)count;
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
                if (0 == ZLibStream.AvailIn)
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
                    ZLibStream.NextOut = (IntPtr)bufPtr;
                    ZLibStream.AvailOut = (uint)outputBuffer.Length;

                    ZErrorCode errC = Deflate(flushCode);
                    bytesRead = outputBuffer.Length - (int)ZLibStream.AvailOut;

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


            // Note: we require that NeedsInput() == true, i.e. that 0 == ZLibStream.AvailIn.
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
                ZLibStream.AvailIn = 0;
                ZLibStream.NextIn = ZLibNative.ZNullPtr;
            }

            try
            {
                _inputBufferHandle.Dispose();
            }
            finally
            {
                _inputBufferHandle = default;
            }
        }

        private ZErrorCode Deflate(ZFlushCode flushCode)
        {
            ZErrorCode errC;
            try
            {
                errC = ZLibStream.Deflate(flushCode);
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
                    throw new ZLibException(SR.ZLibErrorInconsistentStream, "deflate", (int)errC, ZLibStream.GetErrorMessage());

                default:
                    throw new ZLibException(SR.ZLibErrorUnexpected, "deflate", (int)errC, ZLibStream.GetErrorMessage());
            }
        }

        public static Deflater CreateDeflater(ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel)
        {
            Debug.Assert(windowBits >= minWindowBits && windowBits <= maxWindowBits);

            // zlib-ng treats DefaultCompression (-1) as an alias for level 6 internally. Normalize it here so the
            // pool doesn't split equivalent configurations across two slots and needlessly reduce the hit rate.
            if (compressionLevel == ZLibNative.CompressionLevel.DefaultCompression)
            {
                compressionLevel = (ZLibNative.CompressionLevel)6;
            }

            DeflaterState? pooledState = RentDeflateState(compressionLevel, strategy, windowBits, memLevel);
            if (pooledState is DeflaterState state)
            {
                try
                {
                    if (state.Reset())
                    {
                        return new Deflater(state);
                    }
                }
                catch
                {
                    state.Dispose();
                    throw;
                }

                state.Dispose();
            }

            ZLibNative.ZLibStreamHandle zlibStream = ZLibNative.ZLibStreamHandle.CreateForDeflate(compressionLevel, windowBits, memLevel, strategy);

            return new Deflater(new DeflaterState(zlibStream, compressionLevel, strategy, windowBits, memLevel));
        }

        private static DeflaterState? RentDeflateState(ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel)
        {
            lock (s_poolLock)
            {
                for (int i = 0; i < s_pool.Length; i++)
                {
                    if (s_pool[i] is DeflaterState state && state.Matches(compressionLevel, strategy, windowBits, memLevel))
                    {
                        s_pool[i] = null;
                        return state;
                    }
                }
            }

            return null;
        }

        private static void ReturnToPool(DeflaterState state)
        {
            DeflaterState? evictedState = null;
            lock (s_poolLock)
            {
                for (int i = 0; i < s_pool.Length; i++)
                {
                    if (s_pool[i] is null)
                    {
                        s_pool[i] = state;
                        return;
                    }
                }

                // When full, evict entries in round-robin slot order so new configurations displace old ones.
                int evictionIndex = s_nextEvictionIndex;
                evictedState = s_pool[evictionIndex];
                s_pool[evictionIndex] = state;
                s_nextEvictionIndex = (evictionIndex + 1) % s_pool.Length;
            }

            evictedState?.Dispose();
        }

        private readonly struct DeflaterState
        {
            private readonly ZLibNative.ZLibStreamHandle? _stream;
            private readonly ZLibNative.CompressionLevel _compressionLevel;
            private readonly ZLibNative.CompressionStrategy _strategy;
            private readonly int _windowBits;
            private readonly int _memLevel;

            internal ZLibNative.ZLibStreamHandle Stream =>
                _stream ?? throw new ObjectDisposedException(nameof(Deflater));

            internal DeflaterState(ZLibNative.ZLibStreamHandle stream, ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel)
            {
                _stream = stream;
                _compressionLevel = compressionLevel;
                _strategy = strategy;
                _windowBits = windowBits;
                _memLevel = memLevel;
            }

            internal bool Matches(ZLibNative.CompressionLevel compressionLevel, ZLibNative.CompressionStrategy strategy, int windowBits, int memLevel) =>
                _compressionLevel == compressionLevel &&
                _strategy == strategy &&
                _windowBits == windowBits &&
                _memLevel == memLevel;

            internal bool Reset() => Stream.DeflateReset() == ZErrorCode.Ok;

            internal void Dispose() => Stream.Dispose();
        }
    }
}
