// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>Reusable IValueTaskSource for RandomAccess async operations based on Overlapped I/O.</summary>
    internal abstract unsafe class OverlappedValueTaskSource : IValueTaskSource<int>, IValueTaskSource
    {
        internal static readonly IOCompletionCallback s_ioCallback = IOCallback;

        internal readonly PreAllocatedOverlapped _preallocatedOverlapped;
        internal readonly SafeHandle _fileHandle;
        private readonly ThreadPoolBoundHandle _threadPoolBinding;
        private readonly bool _canSeek;
        protected Stream? _owner;
        internal MemoryHandle _memoryHandle;
        protected int _bufferSize;
        internal ManualResetValueTaskSourceCore<int> _source; // mutable struct; do not make this readonly
        private NativeOverlapped* _overlapped;
        private CancellationTokenRegistration _cancellationRegistration;
        /// <summary>
        /// 0 when the operation hasn't been scheduled, non-zero when either the operation has completed,
        /// in which case its value is a packed combination of the error code and number of bytes, or when
        /// the read/write call has finished scheduling the async operation.
        /// </summary>
        internal ulong _result;

        internal OverlappedValueTaskSource(SafeHandle fileHandle, ThreadPoolBoundHandle threadPoolBinding, bool canSeek)
        {
            _fileHandle = fileHandle;
            _threadPoolBinding = threadPoolBinding;
            _canSeek = canSeek;
            _source.RunContinuationsAsynchronously = true;
            _preallocatedOverlapped = PreAllocatedOverlapped.UnsafeCreate(s_ioCallback, this, null);
        }

        internal void Dispose()
        {
            ReleaseResources();
            _preallocatedOverlapped.Dispose();
        }

        internal static Exception GetIOError(int errorCode, string? path)
            => errorCode == Interop.Errors.ERROR_HANDLE_EOF
                ? ThrowHelper.CreateEndOfFileException()
                : Win32Marshal.GetExceptionForWin32Error(errorCode, path);

        protected abstract void TryToReuse();

        protected abstract void HandleIncomplete(Stream owner, uint errorCode, uint byteCount);

        internal NativeOverlapped* PrepareForOperation(ReadOnlyMemory<byte> memory, long fileOffset, Stream? owner = null)
        {
            _result = 0;
            _owner = owner;
            _bufferSize = memory.Length;
            _memoryHandle = memory.Pin();
            _overlapped = _threadPoolBinding.AllocateNativeOverlapped(_preallocatedOverlapped);
            if (_canSeek)
            {
                _overlapped->OffsetLow = (int)fileOffset;
                _overlapped->OffsetHigh = (int)(fileOffset >> 32);
            }
            return _overlapped;
        }

        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _source.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.GetResult(short token) => GetResult(token);
        public int GetResult(short token)
        {
            try
            {
                return _source.GetResult(token);
            }
            finally
            {
                // The instance is ready to be reused
                TryToReuse();
            }
        }

        internal short Version => _source.Version;

        internal void RegisterForCancellation(CancellationToken cancellationToken)
        {
            Debug.Assert(_overlapped != null);
            if (cancellationToken.CanBeCanceled)
            {
                try
                {
                    _cancellationRegistration = cancellationToken.UnsafeRegister(static (s, token) =>
                    {
                        OverlappedValueTaskSource vts = (OverlappedValueTaskSource)s!;
                        if (!vts._fileHandle.IsInvalid)
                        {
                            try
                            {
                                Interop.Kernel32.CancelIoEx(vts._fileHandle, vts._overlapped);
                                // Ignore all failures: no matter whether it succeeds or fails, completion is handled via the IOCallback.
                            }
                            catch (ObjectDisposedException) { } // in case the SafeHandle is (erroneously) closed concurrently
                        }
                    }, this);
                }
                catch (OutOfMemoryException)
                {
                    // Just in case trying to register OOMs, we ignore it in order to
                    // protect the higher-level calling code that would proceed to unpin
                    // memory that might be actively used by an in-flight async operation.
                }
            }
        }

        private void ReleaseResources()
        {
            _owner = null;

            // Ensure that any cancellation callback has either completed or will never run,
            // so that we don't try to access an overlapped for this operation after it's already
            // been freed.
            _cancellationRegistration.Dispose();

            // Unpin any pinned buffer.
            _memoryHandle.Dispose();

            // Free the overlapped.
            if (_overlapped != null)
            {
                _threadPoolBinding.FreeNativeOverlapped(_overlapped);
                _overlapped = null;
            }
        }

        // After calling Read/WriteFile to start the asynchronous operation, the caller may configure cancellation,
        // and only after that should we allow for completing the operation, as completion needs to factor in work
        // done by that cancellation registration, e.g. unregistering.  As such, we use _result to both track who's
        // responsible for calling Complete and for passing the necessary data between parties.

        /// <summary>Invoked when the async operation finished being scheduled.</summary>
        internal void FinishedScheduling()
        {
            // Set the value to 1.  If it was already non-0, then the asynchronous operation already completed but
            // didn't call Complete, so we call Complete here.  The read result value is the data (packed) necessary
            // to make the call.
            ulong result = Interlocked.Exchange(ref _result, 1);
            if (result != 0)
            {
                Complete(errorCode: (uint)result, numBytes: (uint)(result >> 32) & 0x7FFFFFFF);
            }
        }

        /// <summary>Invoked when the asynchronous operation has completed asynchronously.</summary>
        private static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            OverlappedValueTaskSource? vts = (OverlappedValueTaskSource?)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
            Debug.Assert(vts is not null);
            Debug.Assert(vts._overlapped == pOverlapped, "Overlaps don't match");

            // Set the value to a packed combination of the error code and number of bytes (plus a high-bit 1
            // to ensure the value we're setting is non-zero).  If it was already non-0 (the common case), then
            // the call site already finished scheduling the async operation, in which case we're ready to complete.
            Debug.Assert(numBytes < int.MaxValue);
            if (Interlocked.Exchange(ref vts._result, (1ul << 63) | ((ulong)numBytes << 32) | errorCode) != 0)
            {
                vts.Complete(errorCode, numBytes);
            }
        }

        internal void Complete(uint errorCode, uint numBytes)
        {
            Stream? owner = _owner;
            ReleaseResources(); // sets _owner to null

            if (owner is not null)
            {
                HandleIncomplete(owner, errorCode, numBytes);
            }

            switch (errorCode)
            {
                case Interop.Errors.ERROR_SUCCESS:
                case Interop.Errors.ERROR_BROKEN_PIPE:
                case Interop.Errors.ERROR_NO_DATA:
                case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                    // Success
                    _source.SetResult((int)numBytes);
                    break;

                case Interop.Errors.ERROR_OPERATION_ABORTED:
                    CancellationToken ct = _cancellationRegistration.Token;
                    _source.SetException(ct.IsCancellationRequested ? new OperationCanceledException(ct) : new OperationCanceledException());
                    break;

                default:
                    // Failure
                    _source.SetException(Win32Marshal.GetExceptionForWin32Error((int)errorCode));
                    break;
            }
        }
    }
}
