// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        // This is an internal object extending TaskCompletionSource with fields
        // for all of the relevant data necessary to complete the IO operation.
        // This is used by IOCallback and all of the async methods.
        unsafe private sealed class FileStreamCompletionSource : TaskCompletionSource<int>
        {
            private const long NoResult = 0;
            private const long ResultSuccess = (long)1 << 32;
            private const long ResultError = (long)2 << 32;
            private const long RegisteringCancellation = (long)4 << 32;
            private const long CompletedCallback = (long)8 << 32;
            private const ulong ResultMask = ((ulong)uint.MaxValue) << 32;

            private static Action<object> s_cancelCallback;

            private readonly FileStream _stream;
            private readonly int _numBufferedBytes;
            private readonly CancellationToken _cancellationToken;
            private CancellationTokenRegistration _cancellationRegistration;
#if DEBUG
            private bool _cancellationHasBeenRegistered;
#endif
            private NativeOverlapped* _overlapped; // Overlapped class responsible for operations in progress when an appdomain unload occurs
            private long _result; // Using long since this needs to be used in Interlocked APIs

            // Using RunContinuationsAsynchronously for compat reasons (old API used Task.Factory.StartNew for continuations)
            internal FileStreamCompletionSource(FileStream stream, int numBufferedBytes, byte[] bytes, CancellationToken cancellationToken)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _numBufferedBytes = numBufferedBytes;
                _stream = stream;
                _result = NoResult;
                _cancellationToken = cancellationToken;

                // Create the native overlapped. We try to use the preallocated overlapped if possible: 
                // it's possible if the byte buffer is the same one that's associated with the preallocated overlapped 
                // and if no one else is currently using the preallocated overlapped.  This is the fast-path for cases 
                // where the user-provided buffer is smaller than the FileStream's buffer (such that the FileStream's 
                // buffer is used) and where operations on the FileStream are not being performed concurrently.
                _overlapped = ReferenceEquals(bytes, _stream._buffer) && _stream.CompareExchangeCurrentOverlappedOwner(this, null) == null ?
                    _stream._fileHandle.ThreadPoolBinding.AllocateNativeOverlapped(_stream._preallocatedOverlapped) :
                    _stream._fileHandle.ThreadPoolBinding.AllocateNativeOverlapped(s_ioCallback, this, bytes);
                Debug.Assert(_overlapped != null, "AllocateNativeOverlapped returned null");
            }

            internal NativeOverlapped* Overlapped
            {
                [SecurityCritical]get { return _overlapped; }
            }

            public void SetCompletedSynchronously(int numBytes)
            {
                ReleaseNativeResource();
                TrySetResult(numBytes + _numBufferedBytes);
            }

            public void RegisterForCancellation()
            {
#if DEBUG
                Debug.Assert(!_cancellationHasBeenRegistered, "Cannot register for cancellation twice");
                _cancellationHasBeenRegistered = true;
#endif

                // Quick check to make sure that the cancellation token supports cancellation, and that the IO hasn't completed
                if ((_cancellationToken.CanBeCanceled) && (_overlapped != null))
                {
                    var cancelCallback = s_cancelCallback;
                    if (cancelCallback == null) s_cancelCallback = cancelCallback = Cancel;

                    // Register the cancellation only if the IO hasn't completed
                    long packedResult = Interlocked.CompareExchange(ref _result, RegisteringCancellation, NoResult);
                    if (packedResult == NoResult)
                    {
                        _cancellationRegistration = _cancellationToken.Register(cancelCallback, this);

                        // Switch the result, just in case IO completed while we were setting the registration
                        packedResult = Interlocked.Exchange(ref _result, NoResult);
                    }
                    else if (packedResult != CompletedCallback)
                    {
                        // Failed to set the result, IO is in the process of completing
                        // Attempt to take the packed result
                        packedResult = Interlocked.Exchange(ref _result, NoResult);
                    }

                    // If we have a callback that needs to be completed
                    if ((packedResult != NoResult) && (packedResult != CompletedCallback) && (packedResult != RegisteringCancellation))
                    {
                        CompleteCallback((ulong)packedResult);
                    }
                }
            }

            internal void ReleaseNativeResource()
            {
                // Ensure that cancellation has been completed and cleaned up.
                _cancellationRegistration.Dispose();

                // Free the overlapped.
                // NOTE: The cancellation must *NOT* be running at this point, or it may observe freed memory
                // (this is why we disposed the registration above).
                if (_overlapped != null)
                {
                    _stream._fileHandle.ThreadPoolBinding.FreeNativeOverlapped(_overlapped);
                    _overlapped = null;
                }

                // Ensure we're no longer set as the current completion source (we may not have been to begin with).
                // Only one operation at a time is eligible to use the preallocated overlapped, 
                _stream.CompareExchangeCurrentOverlappedOwner(null, this);
            }

            // When doing IO asynchronously (i.e. _isAsync==true), this callback is 
            // called by a free thread in the threadpool when the IO operation 
            // completes.  
            internal static unsafe void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                // Extract the completion source from the overlapped.  The state in the overlapped
                // will either be a Win32FileStream (in the case where the preallocated overlapped was used),
                // in which case the operation being completed is its _currentOverlappedOwner, or it'll
                // be directly the FileStreamCompletion that's completing (in the case where the preallocated
                // overlapped was already in use by another operation).
                object state = ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
                FileStream fs = state as FileStream;
                FileStreamCompletionSource completionSource = fs != null ? 
                    fs._currentOverlappedOwner : 
                    (FileStreamCompletionSource)state;
                Debug.Assert(completionSource._overlapped == pOverlapped, "Overlaps don't match");

                // Handle reading from & writing to closed pipes.  While I'm not sure
                // this is entirely necessary anymore, maybe it's possible for 
                // an async read on a pipe to be issued and then the pipe is closed, 
                // returning this error.  This may very well be necessary.
                ulong packedResult;
                if (errorCode != 0 && errorCode != ERROR_BROKEN_PIPE && errorCode != ERROR_NO_DATA)
                {
                    packedResult = ((ulong)ResultError | errorCode);
                }
                else
                {
                    packedResult = ((ulong)ResultSuccess | numBytes);
                }

                // Stow the result so that other threads can observe it
                // And, if no other thread is registering cancellation, continue
                if (NoResult == Interlocked.Exchange(ref completionSource._result, (long)packedResult))
                {
                    // Successfully set the state, attempt to take back the callback
                    if (Interlocked.Exchange(ref completionSource._result, CompletedCallback) != NoResult)
                    {
                        // Successfully got the callback, finish the callback
                        completionSource.CompleteCallback(packedResult);
                    }
                    // else: Some other thread stole the result, so now it is responsible to finish the callback
                }
                // else: Some other thread is registering a cancellation, so it *must* finish the callback
            }

            private void CompleteCallback(ulong packedResult) {
                // Free up the native resource and cancellation registration
                ReleaseNativeResource();

                // Unpack the result and send it to the user
                long result = (long)(packedResult & ResultMask);
                if (result == ResultError)
                {
                    int errorCode = unchecked((int)(packedResult & uint.MaxValue));
                    if (errorCode == Interop.Errors.ERROR_OPERATION_ABORTED)
                    {
                        TrySetCanceled(_cancellationToken.IsCancellationRequested ? _cancellationToken : new CancellationToken(true));
                    }
                    else
                    {
                        TrySetException(Win32Marshal.GetExceptionForWin32Error(errorCode));
                    }
                }
                else
                {
                    Debug.Assert(result == ResultSuccess, "Unknown result");
                    TrySetResult((int)(packedResult & uint.MaxValue) + _numBufferedBytes);
                }
            }

            private static void Cancel(object state)
            {
                // WARNING: This may potentially be called under a lock (during cancellation registration)

                FileStreamCompletionSource completionSource = state as FileStreamCompletionSource;
                Debug.Assert(completionSource != null, "Unknown state passed to cancellation");
                Debug.Assert(completionSource._overlapped != null && !completionSource.Task.IsCompleted, "IO should not have completed yet");

                // If the handle is still valid, attempt to cancel the IO
                if (!completionSource._stream._fileHandle.IsInvalid && 
                    !Interop.Kernel32.CancelIoEx(completionSource._stream._fileHandle, completionSource._overlapped))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // ERROR_NOT_FOUND is returned if CancelIoEx cannot find the request to cancel.
                    // This probably means that the IO operation has completed.
                    if (errorCode != Interop.Errors.ERROR_NOT_FOUND)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }
                }
            }
        } 
    }
}
