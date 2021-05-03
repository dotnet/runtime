// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : WindowsFileStreamStrategy
    {
        /// <summary>Reusable IValueTaskSource for FileStream ValueTask-returning async operations.</summary>
        private sealed unsafe class ValueTaskSource : IValueTaskSource<int>, IValueTaskSource
        {
            internal static readonly IOCompletionCallback s_ioCallback = IOCallback;

            internal readonly PreAllocatedOverlapped _preallocatedOverlapped;
            private readonly AsyncWindowsFileStreamStrategy _strategy;
            internal MemoryHandle _memoryHandle;
            internal ManualResetValueTaskSourceCore<int> _source; // mutable struct; do not make this readonly
            private NativeOverlapped* _overlapped;
            private CancellationTokenRegistration _cancellationRegistration;
            /// <summary>
            /// 0 when the operation hasn't been scheduled, non-zero when either the operation has completed,
            /// in which case its value is a packed combination of the error code and number of bytes, or when
            /// the read/write call has finished scheduling the async operation.
            /// </summary>
            internal ulong _result;

            internal ValueTaskSource(AsyncWindowsFileStreamStrategy strategy)
            {
                _strategy = strategy;
                _source.RunContinuationsAsynchronously = true;
                _preallocatedOverlapped = new PreAllocatedOverlapped(s_ioCallback, this, null);
            }

            internal void Dispose()
            {
                ReleaseResources();
                _preallocatedOverlapped.Dispose();
            }

            internal NativeOverlapped* PrepareForOperation(ReadOnlyMemory<byte> memory)
            {
                _result = 0;
                _memoryHandle = memory.Pin();
                _overlapped = _strategy._fileHandle.ThreadPoolBinding!.AllocateNativeOverlapped(_preallocatedOverlapped);
                return _overlapped;
            }

            public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _source.OnCompleted(continuation, state, token, flags);
            void IValueTaskSource.GetResult(short token) => GetResultAndRelease(token);
            int IValueTaskSource<int>.GetResult(short token) => GetResultAndRelease(token);

            private int GetResultAndRelease(short token)
            {
                try
                {
                    return _source.GetResult(token);
                }
                finally
                {
                    // The instance is ready to be reused
                    _strategy.TryToReuse(this);
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
                            ValueTaskSource vts = (ValueTaskSource)s!;
                            if (!vts._strategy._fileHandle.IsInvalid)
                            {
                                try
                                {
                                    Interop.Kernel32.CancelIoEx(vts._strategy._fileHandle, vts._overlapped);
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

            internal void ReleaseResources()
            {
                // Unpin any pinned buffer.
                _memoryHandle.Dispose();

                // Ensure that any cancellation callback has either completed or will never run,
                // so that we don't try to access an overlapped for this operation after it's already
                // been freed.
                _cancellationRegistration.Dispose();

                // Free the overlapped.
                if (_overlapped != null)
                {
                    _strategy._fileHandle.ThreadPoolBinding!.FreeNativeOverlapped(_overlapped);
                    _overlapped = null;
                }
            }

            // After calling Read/WriteFile to start the asynchronous operation, the caller may configure cancellation,
            // and only after that should we allow for completing the operation, as completion needs to factor in work
            // done by that cancellation registration, e.g. unregistering.  As such, we use _result to both track who's
            // responsible for calling Complete and for passing the necessary data between parties.

            /// <summary>Invoked when AsyncWindowsFileStreamStrategy has finished scheduling the async operation.</summary>
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
                ValueTaskSource? vts = (ValueTaskSource?)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
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
                ReleaseResources();

                switch (errorCode)
                {
                    case 0:
                    case Interop.Errors.ERROR_BROKEN_PIPE:
                    case Interop.Errors.ERROR_NO_DATA:
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
