// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.Protocols
{
    internal class LdapAsyncResult : IAsyncResult
    {
        private LdapAsyncWaitHandle _asyncWaitHandle;
        internal AsyncCallback _callback;
        internal bool _completed;
        internal ManualResetEvent _manualResetEvent;
        private readonly object _stateObject;
        internal LdapRequestState _resultObject;
        internal bool _partialResults;

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
            get => _asyncWaitHandle ??= new LdapAsyncWaitHandle(_manualResetEvent.SafeWaitHandle);
        }

        bool IAsyncResult.CompletedSynchronously => false;

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

    internal sealed class LdapRequestState
    {
        internal DirectoryResponse _response;
        internal LdapAsyncResult _ldapAsync;
        internal Exception _exception;
        internal bool _abortCalled;

        public LdapRequestState() { }
    }

    internal enum ResultsStatus
    {
        PartialResult = 0,
        CompleteResult = 1,
        Done = 2
    }

    internal sealed class LdapPartialAsyncResult : LdapAsyncResult
    {
        internal LdapConnection _con;
        internal int _messageID = -1;
        internal bool _partialCallback;
        internal ResultsStatus _resultStatus = ResultsStatus.PartialResult;
        internal TimeSpan _requestTimeout;

        internal SearchResponse _response;
        internal Exception _exception;
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
