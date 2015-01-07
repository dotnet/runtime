// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO
{
    internal partial class Win32FileStream
    {
        // This is an internal object implementing IAsyncResult with fields
        // for all of the relevant data necessary to complete the IO operation.
        // This is used by AsyncFSCallback and all of the async methods.
        unsafe private sealed class FileStreamAsyncResult : IAsyncResult
        {
            // User code callback
            private AsyncCallback _userCallback;
            private Object _userStateObject;
            private ManualResetEvent _waitHandle;
#if USE_OVERLAPPED
            private SafeFileHandle _handle;      // For cancellation support.
            private NativeOverlapped* _overlapped;
            internal NativeOverlapped* OverLapped {[SecurityCritical]get { return _overlapped; } }
            internal bool IsAsync {[SecuritySafeCritical]get { return _overlapped != null; } }

            internal int _EndXxxCalled;   // Whether we've called EndXxx already.
            private int _numBytes;     // number of bytes read OR written

            private int _errorCode;
            internal int ErrorCode { get { return _errorCode; } }
#endif

            private int _numBufferedBytes;

#if USE_OVERLAPPED
            internal int NumBytesRead { get { return _numBytes + _numBufferedBytes; } }
#else
            internal int NumBytesRead { get { return _numBufferedBytes; } }
#endif

            private bool _isWrite;     // Whether this is a read or a write
            internal bool IsWrite { get { return _isWrite; } }

            private bool _isComplete;  // Value for IsCompleted property        
            private bool _completedSynchronously;  // Which thread called callback

#if USE_OVERLAPPED
            // The NativeOverlapped struct keeps a GCHandle to this IAsyncResult object.
            // So if the user doesn't call EndRead/EndWrite, a finalizer won't help because
            // it'll never get called. 

            // Overlapped class will take care of the async IO operations in progress 
            // when an appdomain unload occurs.

            private unsafe static IOCompletionCallback s_IOCallback;
#endif

            internal FileStreamAsyncResult(
                int numBufferedBytes,
                byte[] bytes,
#if USE_OVERLAPPED
                SafeFileHandle handle,
#endif
                AsyncCallback userCallback,
                Object userStateObject,
                bool isWrite)
            {
                _userCallback = userCallback;
                _userStateObject = userStateObject;
                _isWrite = isWrite;
                _numBufferedBytes = numBufferedBytes;
#if USE_OVERLAPPED
                _handle = handle;
#endif

                // For Synchronous IO, I could go with either a callback and using
                // the managed Monitor class, or I could create a handle and wait on it.
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                _waitHandle = waitHandle;
#if USE_OVERLAPPED
                // Create a managed overlapped class
                // We will set the file offsets later
                Overlapped overlapped = new Overlapped();
                overlapped.AsyncResult = this;

                // Pack the Overlapped class, and store it in the async result
                var ioCallback = s_IOCallback; // cached static delegate; delay initialized due to it being SecurityCritical
                if (ioCallback == null) s_IOCallback = ioCallback = new IOCompletionCallback(AsyncFSCallback);
                _overlapped = overlapped.Pack(ioCallback, bytes);

                Contract.Assert(_overlapped != null, "Did Overlapped.Pack or Overlapped.UnsafePack just return a null?");
#endif
            }

            internal static FileStreamAsyncResult CreateBufferedReadResult(int numBufferedBytes, AsyncCallback userCallback, Object userStateObject, bool isWrite)
            {
                FileStreamAsyncResult asyncResult = new FileStreamAsyncResult(numBufferedBytes, userCallback, userStateObject, isWrite);
                asyncResult.CallUserCallback();
                return asyncResult;
            }

            // This creates a synchronous Async Result. We should consider making this a separate class and maybe merge it with 
            // System.IO.Stream.SynchronousAsyncResult
            private FileStreamAsyncResult(int numBufferedBytes, AsyncCallback userCallback, Object userStateObject, bool isWrite)
            {
                _userCallback = userCallback;
                _userStateObject = userStateObject;
                _isWrite = isWrite;
                _numBufferedBytes = numBufferedBytes;
            }

            public Object AsyncState
            {
                get { return _userStateObject; }
            }

            public bool IsCompleted
            {
                get { return _isComplete; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    // Consider uncommenting this someday soon - the EventHandle 
                    // in the Overlapped struct is really useless half of the 
                    // time today since the OS doesn't signal it.  If users call
                    // EndXxx after the OS call happened to complete, there's no
                    // reason to create a synchronization primitive here.  Fixing
                    // this will save us some perf, assuming we can correctly
                    // initialize the ManualResetEvent.  
                    if (_waitHandle == null)
                    {
                        ManualResetEvent mre = new ManualResetEvent(false);
#if USE_OVERLAPPED
                        if (_overlapped != null && _overlapped->EventHandle != IntPtr.Zero)
                        {
                            mre.SetSafeWaitHandle(new SafeWaitHandle(_overlapped->EventHandle, true));
                        }
#endif

                        // make sure only one thread sets _waitHandle
                        if (Interlocked.CompareExchange<ManualResetEvent>(ref _waitHandle, mre, null) == null)
                        {
                            if (_isComplete)
                                _waitHandle.Set();
                        }
                        else
                        {
                            // There's a slight but acceptable race condition if 
                            // we weren't the thread that set _waitHandle and 
                            // this code path returns before the code in the if 
                            // statement  executes (on the other thread). 
                            // However, the caller is waiting for the wait 
                            // handle to be set, which will still happen.
                            mre.Dispose();
                        }
                    }
                    return _waitHandle;
                }
            }

            // Returns true iff the user callback was called by the thread that 
            // called BeginRead or BeginWrite.  If we use an async delegate or
            // threadpool thread internally, this will be false.  This is used
            // by code to determine whether a successive call to BeginRead needs 
            // to be done on their main thread or in their callback to avoid a
            // stack overflow on many reads or writes.
            public bool CompletedSynchronously
            {
                get { return _completedSynchronously; }
            }

            private void CallUserCallbackWorker()
            {
                _isComplete = true;

                Interlocked.MemoryBarrier();

                // ensure _isComplete is set before reading _waitHandle
                if (_waitHandle != null)
                    _waitHandle.Set();

                _userCallback(this);
            }

            internal void CallUserCallback()
            {
                // Convenience method for me, since I have to do this in a number 
                // of places in the buffering code for fake IAsyncResults.   
                // AsyncFSCallback intentionally does not use this method.

                if (_userCallback != null)
                {
                    // Call user's callback on a threadpool thread.  
                    // Set completedSynchronously to false, since it's on another 
                    // thread, not the main thread.
                    _completedSynchronously = false;
                    Task.Factory.StartNew(state => ((FileStreamAsyncResult)state).CallUserCallbackWorker(),
                        this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                else
                {
                    _isComplete = true;

                    Interlocked.MemoryBarrier();

                    // ensure _isComplete is set before reading _waitHandle
                    if (_waitHandle != null)
                        _waitHandle.Set();
                }
            }

#if USE_OVERLAPPED
            internal void ReleaseNativeResource()
            {
                // Free memory & GC handles.
                if (this._overlapped != null)
                    Overlapped.Free(_overlapped);
            }
#endif

            internal void Wait()
            {
                if (_waitHandle != null)
                {
                    // We must block to ensure that AsyncFSCallback has completed,
                    // and we should close the WaitHandle in here.  AsyncFSCallback
                    // and the hand-ported imitation version in COMThreadPool.cpp 
                    // are the only places that set this event.
                    try
                    {
                        _waitHandle.WaitOne();
                        Contract.Assert(_isComplete == true, "FileStreamAsyncResult::Wait - AsyncFSCallback  didn't set _isComplete to true!");
                    }
                    finally
                    {
                        _waitHandle.Dispose();
                    }
                }
            }
#if USE_OVERLAPPED
            // When doing IO asynchronously (ie, _isAsync==true), this callback is 
            // called by a free thread in the threadpool when the IO operation 
            // completes.  
            unsafe private static void AsyncFSCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                // Unpack overlapped
                Overlapped overlapped = Overlapped.Unpack(pOverlapped);
                // Free the overlapped struct in EndRead/EndWrite.

                // Extract async result from overlapped 
                FileStreamAsyncResult asyncResult =
                    (FileStreamAsyncResult)overlapped.AsyncResult;
                asyncResult._numBytes = (int)numBytes;

                // Handle reading from & writing to closed pipes.  While I'm not sure
                // this is entirely necessary anymore, maybe it's possible for 
                // an async read on a pipe to be issued and then the pipe is closed, 
                // returning this error.  This may very well be necessary.
                if (errorCode == Win32FileStream.ERROR_BROKEN_PIPE || errorCode == Win32FileStream.ERROR_NO_DATA)
                    errorCode = 0;

                asyncResult._errorCode = (int)errorCode;

                // Call the user-provided callback.  It can and often should
                // call EndRead or EndWrite.  There's no reason to use an async 
                // delegate here - we're already on a threadpool thread.  
                // IAsyncResult's completedSynchronously property must return
                // false here, saying the user callback was called on another thread.
                asyncResult._completedSynchronously = false;
                asyncResult._isComplete = true;

                Interlocked.MemoryBarrier();

                // ensure _isComplete is set before reading _waitHandle
                // The OS does not signal this event.  We must do it ourselves.
                ManualResetEvent wh = asyncResult._waitHandle;
                if (wh != null)
                {
                    Contract.Assert(!wh.GetSafeWaitHandle().IsClosed, "ManualResetEvent already closed!");
                    bool r = wh.Set();
                    Contract.Assert(r, "ManualResetEvent::Set failed!");
                    if (!r) throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                AsyncCallback userCallback = asyncResult._userCallback;
                if (userCallback != null)
                    userCallback(asyncResult);
            }

            internal void Cancel()
            {
                Contract.Assert(_handle != null, "_handle should not be null.");
                Contract.Assert(_overlapped != null, "Cancel should only be called on true asynchronous FileStreamAsyncResult, i.e. _overlapped is not null");

                if (IsCompleted)
                    return;

                if (_handle.IsInvalid)
                    return;

                bool r = Interop.mincore.CancelIoEx(_handle, _overlapped);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // ERROR_NOT_FOUND is returned if CancelIoEx cannot find the request to cancel.
                    // This probably means that the IO operation has completed.
                    if (errorCode != Interop.ERROR_NOT_FOUND)
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }
#endif
        }
    }
}