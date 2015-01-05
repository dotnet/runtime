// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Threading;

namespace System.IO.Pipes
{
    internal unsafe sealed class PipeAsyncResult : IAsyncResult
    {
        internal AsyncCallback _userCallback;   // User code callback
        internal Object _userStateObject;
        internal ManualResetEvent _waitHandle;
        [SecurityCritical]
        internal SafePipeHandle _handle;
        [SecurityCritical]
        internal NativeOverlapped* _overlapped;

        internal int _EndXxxCalled;             // Whether we've called EndXxx already.
        internal int _errorCode;

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

        public WaitHandle AsyncWaitHandle
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
                    PipeAsyncResult ar = (PipeAsyncResult)s;
                    ar._isComplete = true;
                    if (ar._waitHandle != null)
                    {
                        ar._waitHandle.Set();
                    }
                    ar._userCallback(ar);
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
