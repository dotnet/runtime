// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace System.IO.Pipes
{
    // TODO: consolidate logic between PipeStreamCompletionSource and ConnectionCompletionSource
    internal sealed unsafe class PipeStreamCompletionSource : TaskCompletionSource<int>
    {
        private const int NoResult = 0;
        private const int ResultSuccess = 1;
        private const int ResultError = 2;
        private const int RegisteringCancellation = 4;
        private const int CompletedCallback = 8;

        private readonly bool _isWrite;
        private readonly PipeStream _pipeStream;
        private readonly ThreadPoolBoundHandle _threadPoolBinding;

        private CancellationTokenRegistration _cancellationRegistration;
        private CancellationToken _cancellationToken;
        private int _errorCode;
        private bool _isMessageComplete;
        private int _numBytes; // number of buffer read OR written
        private NativeOverlapped* _overlapped;
        private int _state;

#if DEBUG
        private bool _cancellationHasBeenRegistered;
#endif

        internal PipeStreamCompletionSource(PipeStream stream, byte[] buffer, CancellationToken cancellationToken, bool isWrite)
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            Debug.Assert(stream != null, "server is null");
            Debug.Assert(stream._threadPoolBinding != null, "server._threadPoolBinding is null");
            Debug.Assert(buffer != null, "buffer is null");

            _pipeStream = stream;
            _cancellationToken = cancellationToken;
            _isWrite = isWrite;
            _isMessageComplete = true;
            _threadPoolBinding = _pipeStream._threadPoolBinding;
            _state = NoResult;

            _overlapped = _threadPoolBinding.AllocateNativeOverlapped((errorCode, numBytes, pOverlapped) =>
            {
                var completionSource = (PipeStreamCompletionSource)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
                Debug.Assert(completionSource.Overlapped == pOverlapped);

                completionSource.AsyncCallback(errorCode, numBytes);
            }, this, buffer);
        }

        internal NativeOverlapped* Overlapped
        {
            [SecurityCritical]get { return _overlapped; }
        }

        internal void RegisterForCancellation()
        {
#if DEBUG
            Debug.Assert(!_cancellationHasBeenRegistered, "Cannot register for cancellation twice");
            _cancellationHasBeenRegistered = true;
#endif

            // Quick check to make sure that the cancellation token supports cancellation, and that the IO hasn't completed
            if (_cancellationToken.CanBeCanceled && Overlapped != null)
            {
                // Register the cancellation only if the IO hasn't completed
                int state = Interlocked.CompareExchange(ref _state, RegisteringCancellation, NoResult);
                if (state == NoResult)
                {
                    // Register the cancellation
                    _cancellationRegistration = _cancellationToken.Register(thisRef => ((PipeStreamCompletionSource)thisRef).Cancel(), this);

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
                if (state == ResultSuccess || state == ResultError)
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
            if (Overlapped != null)
            {
                _threadPoolBinding.FreeNativeOverlapped(Overlapped);
                _overlapped = null;
            }
        }

        internal void SetCompletedSynchronously(int numBytes = default(int))
        {
            if (!_isWrite)
            {
                _pipeStream.UpdateMessageCompletion(_isMessageComplete);
            }

            TrySetResult(numBytes);
        }

        private void AsyncCallback(uint errorCode, uint numBytes)
        {
            _numBytes = (int)numBytes;

            // Allow async read to finish
            if (!_isWrite)
            {
                switch (errorCode)
                {
                    case Interop.mincore.Errors.ERROR_BROKEN_PIPE:
                    case Interop.mincore.Errors.ERROR_PIPE_NOT_CONNECTED:
                    case Interop.mincore.Errors.ERROR_NO_DATA:
                        errorCode = 0;
                        numBytes = 0;
                        break;
                }
            }

            // For message type buffer.
            if (errorCode == Interop.mincore.Errors.ERROR_MORE_DATA)
            {
                errorCode = 0;
                _isMessageComplete = false;
            }
            else
            {
                _isMessageComplete = true;
            }

            _errorCode = (int)errorCode;

            int resultState = errorCode == 0 ? ResultSuccess : ResultError;

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

        private void Cancel()
        {
            // Storing to locals to avoid data races
            SafeHandle handle = _threadPoolBinding.Handle;
            NativeOverlapped* overlapped = Overlapped;

            Debug.Assert(overlapped != null && !Task.IsCompleted, "IO should not have completed yet");

            // If the handle is still valid, attempt to cancel the IO
            if (!handle.IsInvalid)
            {
                if (!Interop.mincore.CancelIoEx(handle, overlapped))
                {
                    // This case should not have any consequences although
                    // it will be easier to debug if there exists any special case
                    // we are not aware of.
                    int errorCode = Marshal.GetLastWin32Error();
                    Debug.WriteLine("CancelIoEx finished with error code {0}.", errorCode);
                }
            }
        }

        private void CompleteCallback(int resultState)
        {
            Debug.Assert(resultState == ResultSuccess || resultState == ResultError, "Unexpected result state " + resultState);

            ReleaseResources();

            if (resultState == ResultError)
            {
                if (_errorCode == Interop.mincore.Errors.ERROR_OPERATION_ABORTED)
                {
                    if (_cancellationToken.CanBeCanceled && !_cancellationToken.IsCancellationRequested)
                    {
                        // If this is unexpected abortion
                        TrySetException(__Error.GetOperationAborted());
                    }
                    else
                    {
                        // otherwise set canceled
                        TrySetCanceled(_cancellationToken);
                    }
                }
                else
                {
                    TrySetException(_pipeStream.WinIOError(_errorCode));
                }
            }
            else
            {
                SetCompletedSynchronously(_numBytes);
            }
        }
    }
}
