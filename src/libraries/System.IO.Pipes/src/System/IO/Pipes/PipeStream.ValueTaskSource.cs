// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        internal abstract unsafe class PipeValueTaskSource<TResult> : IValueTaskSource<TResult>, IValueTaskSource
        {
            private const int NoResult = 0;
            private const int ResultSuccess = 1;
            private const int ResultError = 2;
            private const int RegisteringCancellation = 4;
            private const int CompletedCallback = 8;

            internal static readonly IOCompletionCallback s_ioCallback = IOCallback;

            internal readonly PreAllocatedOverlapped _preallocatedOverlapped;
            private readonly PipeStream _pipeStream;
            private CancellationTokenRegistration _cancellationRegistration;
            private int _errorCode;
            private NativeOverlapped* _overlapped;
            private MemoryHandle _pinnedMemory;
            private int _state;

            protected internal ManualResetValueTaskSourceCore<TResult> _source; // mutable struct; do not make this readonly
            public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _source.OnCompleted(continuation, state, token, flags);
            void IValueTaskSource.GetResult(short token) => GetResultAndRelease(token);
            public TResult GetResult(short token) => GetResultAndRelease(token);

            private TResult GetResultAndRelease(short token)
            {
                try
                {
                    return _source.GetResult(token);
                }
                finally
                {
                    // The instance is ready to be reused
                    _pipeStream.TryToReuse(this);
                }
            }

            internal short Version => _source.Version;

            protected PipeValueTaskSource(PipeStream pipeStream)
            {
                Debug.Assert(pipeStream != null, "pipeStream is null");

                _pipeStream = pipeStream;
                // Using RunContinuationsAsynchronously for compat reasons (old API used ThreadPool.QueueUserWorkItem for continuations)
                _source.RunContinuationsAsynchronously = true;
                _preallocatedOverlapped = new PreAllocatedOverlapped(s_ioCallback, this, null);
            }

            internal void Dispose()
            {
                ReleaseResources();
                _preallocatedOverlapped.Dispose();
            }

            internal NativeOverlapped* Overlapped => _overlapped;

            internal void PrepareForOperation(ReadOnlyMemory<byte> memory = default)
            {
                _state = NoResult;
                _pinnedMemory = memory.Pin();
                _overlapped = _pipeStream._threadPoolBinding!.AllocateNativeOverlapped(_preallocatedOverlapped);
            }

            internal void RegisterForCancellation(CancellationToken cancellationToken)
            {
                // Quick check to make sure that the cancellation token supports cancellation, and that the IO hasn't completed
                if (cancellationToken.CanBeCanceled && _overlapped != null)
                {
                    // Register the cancellation only if the IO hasn't completed
                    int state = Interlocked.CompareExchange(ref _state, RegisteringCancellation, NoResult);
                    if (state == NoResult)
                    {
                        // Register the cancellation
                        _cancellationRegistration = cancellationToken.UnsafeRegister(thisRef => ((PipeValueTaskSource<TResult>)thisRef!).Cancel(), this);

                        // Grab the state for case if IO completed while we were setting the registration.
                        state = Interlocked.Exchange(ref _state, NoResult);
                    }
                    else if (state != CompletedCallback)
                    {
                        // IO already completed and we have grabbed result state.
                        // Set NoResult to prevent invocation of CompleteCallback(result state) from AsyncCallback(...)
                        state = Interlocked.Exchange(ref _state, NoResult);
                    }

                    // If we have the result state of completed IO call CompleteCallback(result).
                    // Otherwise IO not completed.
                    if ((state & (ResultSuccess | ResultError)) != 0)
                    {
                        CompleteCallback(state);
                    }
                }
            }

            internal void ReleaseResources()
            {
                _cancellationRegistration.Dispose();

                // NOTE: The cancellation must *NOT* be running at this point, or it may observe freed memory
                // (this is why we disposed the registration above)
                if (_overlapped != null)
                {
                    _pipeStream._threadPoolBinding!.FreeNativeOverlapped(_overlapped);
                    _overlapped = null;
                }

                _pinnedMemory.Dispose();
            }

            internal abstract void SetCompletedSynchronously();

            private static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                var valueTaskSource = (PipeValueTaskSource<TResult>?)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
                Debug.Assert(valueTaskSource is not null);
                Debug.Assert(valueTaskSource._overlapped == pOverlapped);

                valueTaskSource.AsyncCallback(errorCode, numBytes);
            }

            protected virtual void AsyncCallback(uint errorCode, uint numBytes)
            {
                int resultState;
                if (errorCode == 0)
                {
                    resultState = ResultSuccess;
                }
                else
                {
                    resultState = ResultError;
                    _errorCode = (int)errorCode;
                }

                // Store the result so that other threads can observe it
                // and if no other thread is registering cancellation, continue.
                // Otherwise CompleteCallback(resultState) will be invoked by RegisterForCancellation().
                if (Interlocked.Exchange(ref _state, resultState) == NoResult)
                {
                    // Now try to prevent invocation of CompleteCallback(resultState) from RegisterForCancellation().
                    // Otherwise, thread responsible for registering cancellation stole the result and it will invoke CompleteCallback(resultState).
                    if (Interlocked.Exchange(ref _state, CompletedCallback) != NoResult)
                    {
                        CompleteCallback(resultState);
                    }
                }
            }

            protected abstract void HandleError(int errorCode);

            private void Cancel()
            {
                SafeHandle handle = _pipeStream._threadPoolBinding!.Handle;
                NativeOverlapped* overlapped = _overlapped;

                if (!handle.IsInvalid)
                {
                    try
                    {
                        // If the handle is still valid, attempt to cancel the IO
                        if (!Interop.Kernel32.CancelIoEx(handle, overlapped))
                        {
                            // This case should not have any consequences although
                            // it will be easier to debug if there exists any special case
                            // we are not aware of.
                            int errorCode = Marshal.GetLastPInvokeError();
                            Debug.WriteLine("CancelIoEx finished with error code {0}.", errorCode);
                        }
                    }
                    catch (ObjectDisposedException) { } // in case the SafeHandle is (erroneously) closed concurrently
                }
            }

            protected virtual void HandleUnexpectedCancellation() => SetCanceled();

            private void CompleteCallback(int resultState)
            {
                Debug.Assert(resultState == ResultSuccess || resultState == ResultError, "Unexpected result state " + resultState);
                CancellationToken cancellationToken = _cancellationRegistration.Token;

                ReleaseResources();

                if (resultState == ResultError)
                {
                    if (_errorCode == Interop.Errors.ERROR_OPERATION_ABORTED)
                    {
                        if (cancellationToken.CanBeCanceled && !cancellationToken.IsCancellationRequested)
                        {
                            HandleUnexpectedCancellation();
                        }
                        else
                        {
                            // otherwise set canceled
                            SetCanceled(cancellationToken);
                        }
                    }
                    else
                    {
                        HandleError(_errorCode);
                    }
                }
                else
                {
                    SetCompletedSynchronously();
                }
            }

            protected void SetResult(TResult result) => _source.SetResult(result);
            protected void SetException(Exception exception) => _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(exception));
            protected void SetCanceled(CancellationToken cancellationToken = default) => SetException(new OperationCanceledException(cancellationToken));
        }
    }
}
