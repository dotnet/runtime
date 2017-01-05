// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Threading;
    using System.Net;
    using System.Text;
    using System.IO;
    using Microsoft.Win32.SafeHandles;

    internal class DsmlAsyncResult : IAsyncResult
    {
        private DsmlAsyncWaitHandle _asyncWaitHandle = null;
        internal AsyncCallback callback = null;
        internal bool completed = false;
        private bool _completedSynchronously = false;
        internal ManualResetEvent manualResetEvent = null;
        private object _stateObject = null;
        internal RequestState resultObject = null;
        internal bool hasValidRequest = false;

        public DsmlAsyncResult(AsyncCallback callbackRoutine, object state)
        {
            _stateObject = state;
            callback = callbackRoutine;
            manualResetEvent = new ManualResetEvent(false);
        }

        object IAsyncResult.AsyncState
        {
            get { return _stateObject; }
        }

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get
            {
                if (null == _asyncWaitHandle)
                {
                    _asyncWaitHandle = new DsmlAsyncWaitHandle(manualResetEvent.SafeWaitHandle);
                }

                return (WaitHandle)_asyncWaitHandle;
            }
        }

        bool IAsyncResult.CompletedSynchronously
        {
            get { return _completedSynchronously; }
        }

        bool IAsyncResult.IsCompleted
        {
            get { return completed; }
        }

        public override int GetHashCode()
        {
            return manualResetEvent.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if ((!(o is DsmlAsyncResult)) || (o == null))
            {
                return false;
            }

            return (this == (DsmlAsyncResult)o);
        }

        sealed internal class DsmlAsyncWaitHandle : WaitHandle
        {
            public DsmlAsyncWaitHandle(SafeWaitHandle handle) : base()
            {
                this.SafeWaitHandle = handle;
            }

            ~DsmlAsyncWaitHandle()
            {
                this.SafeWaitHandle = null;
            }
        }
    }

    internal class RequestState
    {
        public const int bufferSize = 1024;
        public StringBuilder responseString = new StringBuilder(1024);
        public string requestString = null;
        public HttpWebRequest request = null;
        public Stream requestStream = null;
        public Stream responseStream = null;
        public byte[] bufferRead = null;
        public UTF8Encoding encoder = new UTF8Encoding();
        public DsmlAsyncResult dsmlAsync = null;
        internal bool abortCalled = false;
        internal Exception exception = null;

        public RequestState()
        {
            bufferRead = new byte[bufferSize];
        }
    }
}
