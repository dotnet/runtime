// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private OverlappedValueTaskSource? _reusableOverlappedValueTaskSource; // reusable OverlappedValueTaskSource that is currently NOT being used

        // Rent the reusable OverlappedValueTaskSource, or create a new one to use if we couldn't get one (which
        // should only happen on first use or if the SafeFileHandle is being used concurrently).
        internal OverlappedValueTaskSource GetOverlappedValueTaskSource() =>
            Interlocked.Exchange(ref _reusableOverlappedValueTaskSource, null) ?? new OverlappedValueTaskSource(this);

        protected override bool ReleaseHandle()
        {
            bool result = Interop.Kernel32.CloseHandle(handle);

            Interlocked.Exchange(ref _reusableOverlappedValueTaskSource, null)?.Dispose();

            return result;
        }

        private void TryToReuse(OverlappedValueTaskSource source)
        {
            source._source.Reset();

            if (Interlocked.CompareExchange(ref _reusableOverlappedValueTaskSource, source, null) is not null)
            {
                source._preallocatedOverlapped.Dispose();
            }
        }

        /// <summary>Reusable IValueTaskSource for RandomAccess async operations based on Overlapped I/O.</summary>
        internal sealed unsafe class OverlappedValueTaskSource : IValueTaskSource<int>, IValueTaskSource
        {
            internal static readonly IOCompletionCallback s_ioCallback = IOCallback;

            internal readonly PreAllocatedOverlapped _preallocatedOverlapped;
            internal readonly SafeFileHandle _fileHandle;
            private OSFileStreamStrategy? _strategy;
            internal MemoryHandle _memoryHandle;
            private int _bufferSize;
            internal ManualResetValueTaskSourceCore<int> _source; // mutable struct; do not make this readonly
            private NativeOverlapped* _overlapped;
            private CancellationTokenRegistration _cancellationRegistration;
            /// <summary>
            /// 0 when the operation hasn't been scheduled, non-zero when either the operation has completed,
            /// in which case its value is a packed combination of the error code and number of bytes, or when
            /// the read/write call has finished scheduling the async operation.
            /// </summary>
            internal ulong _result;

            internal OverlappedValueTaskSource(SafeFileHandle fileHandle)
            {
                _fileHandle = fileHandle;
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

            internal NativeOverlapped* PrepareForOperation(ReadOnlyMemory<byte> memory, long fileOffset, OSFileStreamStrategy? strategy = null)
            {
                Debug.Assert(strategy is null || strategy is AsyncWindowsFileStreamStrategy, $"Strategy was expected to be null or async, got {strategy}.");

                _result = 0;
                _strategy = strategy;
                _bufferSize = memory.Length;
                _memoryHandle = memory.Pin();
                _overlapped = _fileHandle.ThreadPoolBinding!.AllocateNativeOverlapped(_preallocatedOverlapped);
                _overlapped->OffsetLow = (int)fileOffset;
                _overlapped->OffsetHigh = (int)(fileOffset >> 32);
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
                    _fileHandle.TryToReuse(this);
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
                _strategy = null;
                // Unpin any pinned buffer.
                _memoryHandle.Dispose();

                // Ensure that any cancellation callback has either completed or will never run,
                // so that we don't try to access an overlapped for this operation after it's already
                // been freed.
                _cancellationRegistration.Dispose();

                // Free the overlapped.
                if (_overlapped != null)
                {
                    _fileHandle.ThreadPoolBinding!.FreeNativeOverlapped(_overlapped);
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
                Debug.Assert(errorCode == Interop.Errors.ERROR_SUCCESS || numBytes == 0, $"Callback returned {errorCode} error and {numBytes} bytes");

                OSFileStreamStrategy? strategy = _strategy;
                ReleaseResources();

                if (strategy is not null && _bufferSize != numBytes) // true only for incomplete operations
                {
                    strategy.OnIncompleteOperation(_bufferSize, (int)numBytes);
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
                        // Cancellation
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
}
