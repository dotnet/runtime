// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Threading;

namespace System.IO.Pipes
{
    internal sealed class PipeStreamAsyncResult : IAsyncResult
    {
        internal AsyncCallback _userCallback;   // User callback
        internal Object _userStateObject;
        internal ManualResetEvent _waitHandle;
        [SecurityCritical]
        internal SafePipeHandle _handle;        // For cancellation support.
        [SecurityCritical]
        internal unsafe NativeOverlapped* _overlapped;

        internal int _EndXxxCalled;             // Whether we've called EndXxx already.
        internal int _numBytes;                 // number of buffer read OR written
        internal int _errorCode;

        internal bool _isMessageComplete;
        internal bool _isWrite;                 // Whether this is a read or a write
        internal bool _isComplete;              // Value for IsCompleted property        
        internal bool _completedSynchronously;  // Which thread called callback

        public Object AsyncState
        {
            get { return _userStateObject; }
        }

        public bool IsCompleted
        {
            get { return _isComplete; }
        }

        public unsafe WaitHandle AsyncWaitHandle
        {
            [SecurityCritical]
            get
            {
                if (_waitHandle == null)
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    if (_overlapped != null && _overlapped->EventHandle != IntPtr.Zero)
                    {
                        mre.SetSafeWaitHandle(new SafeWaitHandle(_overlapped->EventHandle, true));
                    }
                    if (_isComplete)
                    {
                        mre.Set();
                    }
                    _waitHandle = mre;
                }
                return _waitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get { return _completedSynchronously; }
        }

        internal void CallUserCallback()
        {
            if (_userCallback != null)
            {
                _completedSynchronously = false;
                ThreadPool.QueueUserWorkItem(s =>
                {
                    PipeStreamAsyncResult ar = (PipeStreamAsyncResult)s;
                    ar._isComplete = true;
                    if (ar._waitHandle != null)
                    {
                        ar._waitHandle.Set();
                    }
                    ar._userCallback(this);
                }, this);
            }
            else
            {
                _isComplete = true;
                if (_waitHandle != null)
                {
                    _waitHandle.Set();
                }
            }
        }
    }
}


