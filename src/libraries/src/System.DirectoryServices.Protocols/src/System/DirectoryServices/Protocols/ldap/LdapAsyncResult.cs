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

    internal class LdapAsyncResult : IAsyncResult
    {
        private LdapAsyncWaitHandle _asyncWaitHandle = null;
        internal AsyncCallback callback = null;
        internal bool completed = false;
        private bool _completedSynchronously = false;
        internal ManualResetEvent manualResetEvent = null;
        private object _stateObject = null;
        internal LdapRequestState resultObject = null;
        internal bool partialResults = false;

        public LdapAsyncResult(AsyncCallback callbackRoutine, object state, bool partialResults)
        {
            _stateObject = state;
            callback = callbackRoutine;
            manualResetEvent = new ManualResetEvent(false);
            this.partialResults = partialResults;
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
                    _asyncWaitHandle = new LdapAsyncWaitHandle(manualResetEvent.SafeWaitHandle);
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
            if ((!(o is LdapAsyncResult)) || (o == null))
            {
                return false;
            }

            return (this == (LdapAsyncResult)o);
        }

        sealed internal class LdapAsyncWaitHandle : WaitHandle
        {
            public LdapAsyncWaitHandle(SafeWaitHandle handle) : base()
            {
                this.SafeWaitHandle = handle;
            }

            ~LdapAsyncWaitHandle()
            {
                this.SafeWaitHandle = null;
            }
        }
    }

    internal class LdapRequestState
    {
        internal DirectoryResponse response = null;
        internal LdapAsyncResult ldapAsync = null;
        internal Exception exception = null;
        internal bool abortCalled = false;

        public LdapRequestState() { }
    }

    internal enum ResultsStatus
    {
        PartialResult = 0,
        CompleteResult = 1,
        Done = 2
    }

    internal class LdapPartialAsyncResult : LdapAsyncResult
    {
        internal LdapConnection con;
        internal int messageID = -1;
        internal bool partialCallback;
        internal ResultsStatus resultStatus = ResultsStatus.PartialResult;
        internal TimeSpan requestTimeout;

        internal SearchResponse response = null;
        internal Exception exception = null;
        internal DateTime startTime;

        public LdapPartialAsyncResult(int messageID, AsyncCallback callbackRoutine, object state, bool partialResults, LdapConnection con, bool partialCallback, TimeSpan requestTimeout) : base(callbackRoutine, state, partialResults)
        {
            this.messageID = messageID;
            this.con = con;
            this.partialResults = true;
            this.partialCallback = partialCallback;
            this.requestTimeout = requestTimeout;
            this.startTime = DateTime.Now;
        }
    }
}

