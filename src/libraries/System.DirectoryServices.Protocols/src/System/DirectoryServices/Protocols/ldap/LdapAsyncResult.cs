// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.Protocols
{
    internal class LdapAsyncResult : IAsyncResult
    {
        private LdapAsyncWaitHandle _asyncWaitHandle = null;
        internal AsyncCallback _callback = null;
        internal bool _completed = false;
        private bool _completedSynchronously = false;
        internal ManualResetEvent _manualResetEvent = null;
        private object _stateObject = null;
        internal LdapRequestState _resultObject = null;
        internal bool _partialResults = false;

        public LdapAsyncResult(AsyncCallback callbackRoutine, object state, bool partialResults)
        {
            _stateObject = state;
            _callback = callbackRoutine;
            _manualResetEvent = new ManualResetEvent(false);
            _partialResults = partialResults;
        }

        object IAsyncResult.AsyncState => _stateObject;

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get => _asyncWaitHandle ?? (_asyncWaitHandle = new LdapAsyncWaitHandle(_manualResetEvent.SafeWaitHandle));
        }

        bool IAsyncResult.CompletedSynchronously => _completedSynchronously;

        bool IAsyncResult.IsCompleted => _completed;

        public override int GetHashCode() => _manualResetEvent.GetHashCode();

        public override bool Equals(object obj)
        {
            if (!(obj is LdapAsyncResult otherAsyncResult))
            {
                return false;
            }

            return this == otherAsyncResult;
        }

        private sealed class LdapAsyncWaitHandle : WaitHandle
        {
            public LdapAsyncWaitHandle(SafeWaitHandle handle) : base()
            {
                SafeWaitHandle = handle;
            }

            ~LdapAsyncWaitHandle() => SafeWaitHandle = null;
        }
    }

    internal class LdapRequestState
    {
        internal DirectoryResponse _response = null;
        internal LdapAsyncResult _ldapAsync = null;
        internal Exception _exception = null;
        internal bool _abortCalled = false;

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
        internal LdapConnection _con;
        internal int _messageID = -1;
        internal bool _partialCallback;
        internal ResultsStatus _resultStatus = ResultsStatus.PartialResult;
        internal TimeSpan _requestTimeout;

        internal SearchResponse _response = null;
        internal Exception _exception = null;
        internal DateTime _startTime;

        public LdapPartialAsyncResult(int messageID, AsyncCallback callbackRoutine, object state, bool partialResults, LdapConnection con, bool partialCallback, TimeSpan requestTimeout) : base(callbackRoutine, state, partialResults)
        {
            _messageID = messageID;
            _con = con;
            _partialResults = true;
            _partialCallback = partialCallback;
            _requestTimeout = requestTimeout;
            _startTime = DateTime.Now;
        }
    }
}
