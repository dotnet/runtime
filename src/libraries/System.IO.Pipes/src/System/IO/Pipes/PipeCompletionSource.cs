// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    internal abstract unsafe class PipeCompletionSource<TResult> : TaskCompletionSource<TResult>
    {
        private const int NoResult = 0;
        private const int ResultSuccess = 1;
        private const int ResultError = 2;
        private const int RegisteringCancellation = 4;
        private const int CompletedCallback = 8;

        private readonly ThreadPoolBoundHandle _threadPoolBinding;

        private CancellationTokenRegistration _cancellationRegistration;
        private int _errorCode;
        private NativeOverlapped* _overlapped;
        private MemoryHandle _pinnedMemory;
        private int _state;

#if DEBUG
        private bool _cancellationHasBeenRegistered;
#endif

        // Using RunContinuationsAsynchronously for compat reasons (old API used ThreadPool.QueueUserWorkItem for continuations)
        protected PipeCompletionSource(ThreadPoolBoundHandle handle, ReadOnlyMemory<byte> bufferToPin)
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            Debug.Assert(handle != null, "handle is null");

            _threadPoolBinding = handle;
            _state = NoResult;

            _pinnedMemory = bufferToPin.Pin();
            _overlapped = _threadPoolBinding.AllocateNativeOverlapped((errorCode, numBytes, pOverlapped) =>
            {
                var completionSource = (PipeCompletionSource<TResult>)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped)!;
                Debug.Assert(completionSource.Overlapped == pOverlapped);

                completionSource.AsyncCallback(errorCode, numBytes);
            }, this, null);
        }

        internal NativeOverlapped* Overlapped
        {
            get { return _overlapped; }
        }

        internal void RegisterForCancellation(CancellationToken cancellationToken)
        {
#if DEBUG
            Debug.Assert(!_cancellationHasBeenRegistered, "Cannot register for cancellation twice");
            _cancellationHasBeenRegistered = true;
#endif

            // Quick check to make sure that the cancellation token supports cancellation, and that the IO hasn't completed
            if (cancellationToken.CanBeCanceled && Overlapped != null)
            {
                // Register the cancellation only if the IO hasn't completed
                int state = Interlocked.CompareExchange(ref _state, RegisteringCancellation, NoResult);
                if (state == NoResult)
                {
                    // Register the cancellation
                    _cancellationRegistration = cancellationToken.UnsafeRegister(thisRef => ((PipeCompletionSource<TResult>)thisRef!).Cancel(), this);

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
                _threadPoolBinding.FreeNativeOverlapped(Overlapped);
                _overlapped = null;
            }

            _pinnedMemory.Dispose();
        }

        internal abstract void SetCompletedSynchronously();

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
            SafeHandle handle = _threadPoolBinding.Handle;
            NativeOverlapped* overlapped = Overlapped;

            // If the handle is still valid, attempt to cancel the IO
            if (!handle.IsInvalid && !Interop.Kernel32.CancelIoEx(handle, overlapped))
            {
                // This case should not have any consequences although
                // it will be easier to debug if there exists any special case
                // we are not aware of.
                int errorCode = Marshal.GetLastPInvokeError();
                Debug.WriteLine("CancelIoEx finished with error code {0}.", errorCode);
            }
        }

        protected virtual void HandleUnexpectedCancellation() => TrySetCanceled();

        private void CompleteCallback(int resultState)
        {
            Debug.Assert(resultState == ResultSuccess || resultState == ResultError, $"Unexpected result state {resultState}");
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
                        TrySetCanceled(cancellationToken);
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
    }
}
