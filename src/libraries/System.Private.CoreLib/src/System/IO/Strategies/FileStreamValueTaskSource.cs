// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : WindowsFileStreamStrategy
    {
        /// <summary>
        /// Type that helps reduce allocations for FileStream.ReadAsync and FileStream.WriteAsync.
        /// </summary>
        private unsafe class FileStreamValueTaskSource : IValueTaskSource<int>, IValueTaskSource
        {
            internal static readonly IOCompletionCallback s_ioCallback = IOCallback;

            private readonly AsyncWindowsFileStreamStrategy _strategy;
            private readonly int _numBufferedBytes;

            private ManualResetValueTaskSourceCore<int> _source; // mutable struct; do not make this readonly
            private NativeOverlapped* _overlapped;
            private CancellationTokenRegistration _cancellationRegistration;
            private long _result; // Using long since this needs to be used in Interlocked APIs
#if DEBUG
            private bool _cancellationHasBeenRegistered;
#endif

            public static FileStreamValueTaskSource Create(
                AsyncWindowsFileStreamStrategy strategy,
                PreAllocatedOverlapped? preallocatedOverlapped,
                int numBufferedBytes,
                ReadOnlyMemory<byte> memory)
            {
                // If the memory passed in is the strategy's internal buffer, we can use the base AwaitableProvider,
                // which has a PreAllocatedOverlapped with the memory already pinned.  Otherwise, we use the derived
                // MemoryAwaitableProvider, which Retains the memory, which will result in less pinning in the case
                // where the underlying memory is backed by pre-pinned buffers.
                return preallocatedOverlapped != null &&
                       MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer) &&
                       preallocatedOverlapped.IsUserObject(buffer.Array) ?
                            new FileStreamValueTaskSource(strategy, preallocatedOverlapped, numBufferedBytes, buffer.Array) :
                            new MemoryFileStreamValueTaskSource(strategy, numBufferedBytes, memory);
            }

            protected FileStreamValueTaskSource(
                AsyncWindowsFileStreamStrategy strategy,
                PreAllocatedOverlapped? preallocatedOverlapped,
                int numBufferedBytes,
                byte[]? bytes)
            {
                _strategy = strategy;
                _numBufferedBytes = numBufferedBytes;

                _result = FileStreamHelpers.NoResult;

                _source = default;
                // Using RunContinuationsAsynchronously for compat reasons (old API used Task.Factory.StartNew for continuations)
                _source.RunContinuationsAsynchronously = true;

                _overlapped = bytes != null &&
                              _strategy.CompareExchangeCurrentOverlappedOwner(this, null) == null ?
                              _strategy._fileHandle.ThreadPoolBinding!.AllocateNativeOverlapped(preallocatedOverlapped!) : // allocated when buffer was created, and buffer is non-null
                              strategy._fileHandle.ThreadPoolBinding!.AllocateNativeOverlapped(s_ioCallback, this, bytes);

                Debug.Assert(_overlapped != null, "AllocateNativeOverlapped returned null");
            }

            internal NativeOverlapped* Overlapped => _overlapped;
            internal int NumBufferedBytes => _numBufferedBytes;
            public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _source.OnCompleted(continuation, state, token, flags);
            void IValueTaskSource.GetResult(short token) => _source.GetResult(token);
            int IValueTaskSource<int>.GetResult(short token) => _source.GetResult(token);
            internal short Version => _source.Version;

            internal void RegisterForCancellation(CancellationToken cancellationToken)
            {
#if DEBUG
                Debug.Assert(cancellationToken.CanBeCanceled);
                Debug.Assert(!_cancellationHasBeenRegistered, "Cannot register for cancellation twice");
                _cancellationHasBeenRegistered = true;
#endif

                // Quick check to make sure the IO hasn't completed
                if (_overlapped != null)
                {
                    // Register the cancellation only if the IO hasn't completed
                    long packedResult = Interlocked.CompareExchange(ref _result, FileStreamHelpers.RegisteringCancellation, FileStreamHelpers.NoResult);
                    if (packedResult == FileStreamHelpers.NoResult)
                    {
                        _cancellationRegistration = cancellationToken.UnsafeRegister(static (s, token) => ((FileStreamValueTaskSource)s!).Cancel(token), this);

                        // Switch the result, just in case IO completed while we were setting the registration
                        packedResult = Interlocked.Exchange(ref _result, FileStreamHelpers.NoResult);
                    }
                    else if (packedResult != FileStreamHelpers.CompletedCallback)
                    {
                        // Failed to set the result, IO is in the process of completing
                        // Attempt to take the packed result
                        packedResult = Interlocked.Exchange(ref _result, FileStreamHelpers.NoResult);
                    }

                    // If we have a callback that needs to be completed
                    if ((packedResult != FileStreamHelpers.NoResult) && (packedResult != FileStreamHelpers.CompletedCallback) && (packedResult != FileStreamHelpers.RegisteringCancellation))
                    {
                        CompleteCallback((ulong)packedResult);
                    }
                }
            }

            internal virtual void ReleaseNativeResource()
            {
                // Ensure that cancellation has been completed and cleaned up.
                _cancellationRegistration.Dispose();

                // Free the overlapped.
                // NOTE: The cancellation must *NOT* be running at this point, or it may observe freed memory
                // (this is why we disposed the registration above).
                if (_overlapped != null)
                {
                    _strategy._fileHandle.ThreadPoolBinding!.FreeNativeOverlapped(_overlapped);
                    _overlapped = null;
                }

                // Ensure we're no longer set as the current AwaitableProvider (we may not have been to begin with).
                // Only one operation at a time is eligible to use the preallocated overlapped
                _strategy.CompareExchangeCurrentOverlappedOwner(null, this);
            }

            private static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                // Extract the AwaitableProvider from the overlapped.  The state in the overlapped
                // will either be a AsyncWindowsFileStreamStrategy (in the case where the preallocated overlapped was used),
                // in which case the operation being completed is its _currentOverlappedOwner, or it'll
                // be directly the AwaitableProvider that's completing (in the case where the preallocated
                // overlapped was already in use by another operation).
                object? state = ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
                Debug.Assert(state is (AsyncWindowsFileStreamStrategy or FileStreamValueTaskSource));
                FileStreamValueTaskSource valueTaskSource = state switch
                {
                    AsyncWindowsFileStreamStrategy strategy => strategy._currentOverlappedOwner!, // must be owned
                    _ => (FileStreamValueTaskSource)state
                };
                Debug.Assert(valueTaskSource != null);
                Debug.Assert(valueTaskSource._overlapped == pOverlapped, "Overlaps don't match");

                // Handle reading from & writing to closed pipes.  While I'm not sure
                // this is entirely necessary anymore, maybe it's possible for
                // an async read on a pipe to be issued and then the pipe is closed,
                // returning this error.  This may very well be necessary.
                ulong packedResult;
                if (errorCode != 0 && errorCode != Interop.Errors.ERROR_BROKEN_PIPE && errorCode != Interop.Errors.ERROR_NO_DATA)
                {
                    packedResult = ((ulong)FileStreamHelpers.ResultError | errorCode);
                }
                else
                {
                    packedResult = ((ulong)FileStreamHelpers.ResultSuccess | numBytes);
                }

                // Stow the result so that other threads can observe it
                // And, if no other thread is registering cancellation, continue
                if (Interlocked.Exchange(ref valueTaskSource._result, (long)packedResult) == FileStreamHelpers.NoResult)
                {
                    // Successfully set the state, attempt to take back the callback
                    if (Interlocked.Exchange(ref valueTaskSource._result, FileStreamHelpers.CompletedCallback) != FileStreamHelpers.NoResult)
                    {
                        // Successfully got the callback, finish the callback
                        valueTaskSource.CompleteCallback(packedResult);
                    }
                    // else: Some other thread stole the result, so now it is responsible to finish the callback
                }
                // else: Some other thread is registering a cancellation, so it *must* finish the callback
            }

            private void CompleteCallback(ulong packedResult)
            {
                // Free up the native resource and cancellation registration
                CancellationToken cancellationToken = _cancellationRegistration.Token; // access before disposing registration
                ReleaseNativeResource();

                // Unpack the result and send it to the user
                long result = (long)(packedResult & FileStreamHelpers.ResultMask);
                if (result == FileStreamHelpers.ResultError)
                {
                    int errorCode = unchecked((int)(packedResult & uint.MaxValue));
                    if (errorCode == Interop.Errors.ERROR_OPERATION_ABORTED)
                    {
                        _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(canceled: true))));
                    }
                    else
                    {
                        Exception e = Win32Marshal.GetExceptionForWin32Error(errorCode);
                        e.SetCurrentStackTrace();
                        _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(e));
                    }
                }
                else
                {
                    Debug.Assert(result == FileStreamHelpers.ResultSuccess, "Unknown result");
                    _source.SetResult((int)(packedResult & uint.MaxValue) + _numBufferedBytes);
                }
            }

            private void Cancel(CancellationToken token)
            {
                // If the handle is still valid, attempt to cancel the IO
                if (!_strategy._fileHandle.IsInvalid &&
                    !Interop.Kernel32.CancelIoEx(_strategy._fileHandle, _overlapped))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // ERROR_NOT_FOUND is returned if CancelIoEx cannot find the request to cancel.
                    // This probably means that the IO operation has completed.
                    if (errorCode != Interop.Errors.ERROR_NOT_FOUND)
                    {
                        _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace( // TODO: Resource string for exception
                            new OperationCanceledException("IO operation cancelled.", Win32Marshal.GetExceptionForWin32Error(errorCode), token)));
                    }
                }
            }
        }

        /// <summary>
        /// Extends <see cref="FileStreamValueTaskSource"/> with to support disposing of a
        /// <see cref="MemoryHandle"/> when the operation has completed.  This should only be used
        /// when memory doesn't wrap a byte[].
        /// </summary>
        private sealed class MemoryFileStreamValueTaskSource : FileStreamValueTaskSource
        {
            private MemoryHandle _handle; // mutable struct; do not make this readonly

            // this type handles the pinning, so bytes are null
            internal unsafe MemoryFileStreamValueTaskSource(AsyncWindowsFileStreamStrategy strategy, int numBufferedBytes, ReadOnlyMemory<byte> memory)
                : base(strategy, null, numBufferedBytes, null) // this type handles the pinning, so null is passed for bytes to the base
            {
                _handle = memory.Pin();
            }

            internal override void ReleaseNativeResource()
            {
                _handle.Dispose();
                base.ReleaseNativeResource();
            }
        }
    }
}
