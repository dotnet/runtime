// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Disable CS0162: Unreachable code detected
//
// There is unreachable code in this file due to the use of SocketPal.SupportsMultipleConnectAttempts.
#pragma warning disable 162

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // This object is used to wrap a bunch of ConnectAsync operations
    // on behalf of a single user call to ConnectAsync with a DnsEndPoint
    internal abstract class MultipleConnectAsync
    {
        protected SocketAsyncEventArgs _userArgs;
        protected SocketAsyncEventArgs _internalArgs;

        protected DnsEndPoint _endPoint;
        protected IPAddress[] _addressList;
        protected int _nextAddress;

        private Socket _lastAttemptSocket;

        private enum State
        {
            NotStarted,
            DnsQuery,
            ConnectAttempt,
            UserConnectAttempt,
            Completed,
            Canceled,
        }

        private State _state;

        private object _lockObject = new object();

        protected abstract bool RequiresUserConnectAttempt { get; }

        protected abstract Socket UserSocket { get; }

        // Called by Socket to kick off the ConnectAsync process.  We'll complete the user's SAEA
        // when it's done.  Returns true if the operation will be asynchronous, false if it has failed synchronously
        public bool StartConnectAsync(SocketAsyncEventArgs args, DnsEndPoint endPoint)
        {
            lock (_lockObject)
            {
                if (endPoint.AddressFamily != AddressFamily.Unspecified &&
                    endPoint.AddressFamily != AddressFamily.InterNetwork &&
                    endPoint.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    NetEventSource.Fail(this, $"Unexpected endpoint address family: {endPoint.AddressFamily}");
                }

                _userArgs = args;
                _endPoint = endPoint;

                // If Cancel() was called before we got the lock, it only set the state to Canceled: we need to
                // fail synchronously from here.  Once State.DnsQuery is set, the Cancel() call will handle calling AsyncFail.
                if (_state == State.Canceled)
                {
                    SyncFail(new SocketException((int)SocketError.OperationAborted));
                    return false;
                }

                if (_state != State.NotStarted)
                {
                    NetEventSource.Fail(this, "MultipleConnectAsync.StartConnectAsync(): Unexpected object state");
                }

                _state = State.DnsQuery;

                IAsyncResult result = DnsAPMExtensions.BeginGetHostAddresses(endPoint.Host, new AsyncCallback(DnsCallback), null);
                if (result.CompletedSynchronously)
                {
                    return DoDnsCallback(result, true);
                }
                else
                {
                    return true;
                }
            }
        }

        // Callback which fires when the Dns Resolve is complete
        private void DnsCallback(IAsyncResult result)
        {
            if (!result.CompletedSynchronously)
            {
                DoDnsCallback(result, false);
            }
        }

        // Called when the DNS query completes (either synchronously or asynchronously).  Checks for failure and
        // starts the first connection attempt if it succeeded.  Returns true if the operation will be asynchronous,
        // false if it has failed synchronously.
        private bool DoDnsCallback(IAsyncResult result, bool sync)
        {
            Exception exception = null;

            lock (_lockObject)
            {
                // If the connection attempt was canceled during the dns query, the user's callback has already been
                // called asynchronously and we simply need to return.
                if (_state == State.Canceled)
                {
                    return true;
                }

                if (_state != State.DnsQuery)
                {
                    NetEventSource.Fail(this, "MultipleConnectAsync.DoDnsCallback(): Unexpected object state");
                }

                try
                {
                    _addressList = DnsAPMExtensions.EndGetHostAddresses(result);
                    if (_addressList == null)
                    {
                        NetEventSource.Fail(this, "MultipleConnectAsync.DoDnsCallback(): EndGetHostAddresses returned null!");
                    }
                }
                catch (Exception e)
                {
                    _state = State.Completed;
                    exception = e;
                }

                // If the dns query succeeded, try to connect to the first address
                if (exception == null)
                {
                    _state = State.ConnectAttempt;

                    _internalArgs = new SocketAsyncEventArgs();
                    _internalArgs.Completed += InternalConnectCallback;

                    if (!RequiresUserConnectAttempt)
                    {
                        _internalArgs.SetBuffer(_userArgs.Buffer, _userArgs.Offset, _userArgs.Count);
                    }

                    exception = AttemptConnection();

                    if (exception != null)
                    {
                        // There was a synchronous error while connecting
                        _state = State.Completed;
                    }
                }
            }

            // Call this outside of the lock because it might call the user's callback.
            if (exception != null)
            {
                return Fail(sync, exception);
            }
            else
            {
                return true;
            }
        }

        // Callback which fires when an internal connection attempt completes.
        // If it failed and there are more addresses to try, do it.
        private void InternalConnectCallback(object sender, SocketAsyncEventArgs args)
        {
            Exception exception = null;

            lock (_lockObject)
            {
                if (_state == State.Canceled)
                {
                    // If Cancel was called before we got the lock, the Socket will be closed soon.  We need to report
                    // OperationAborted (even though the connection actually completed), or the user will try to use a 
                    // closed Socket.
                    exception = new SocketException((int)SocketError.OperationAborted);
                }
                else if (_state == State.ConnectAttempt)
                {
                    if (args.SocketError == SocketError.Success)
                    {
                        if (RequiresUserConnectAttempt)
                        {
                            exception = AttemptUserConnection();

                            if (exception == null)
                            {
                                // Don't call the callback; we've started a connection attempt on the
                                // user's socket.
                                _state = State.UserConnectAttempt;
                                return;
                            }
                        }

                        // The connection attempt succeeded or the user connect attempt failed synchronously; go to the
                        // completed state. The callback will be called outside the lock.
                        _state = State.Completed;
                    }
                    else if (args.SocketError == SocketError.OperationAborted)
                    {
                        // The socket was closed while the connect was in progress.  This can happen if the user
                        // closes the socket, and is equivalent to a call to CancelConnectAsync
                        exception = new SocketException((int)SocketError.OperationAborted);
                        _state = State.Canceled;
                    }
                    else
                    {
                        // Try again, if there are more IPAddresses to be had.

                        // If the underlying OS does not support multiple connect attempts
                        // on the same socket, dispose the socket used for the last attempt.
                        if (!SocketPal.SupportsMultipleConnectAttempts)
                        {
                            _lastAttemptSocket.Dispose();
                        }

                        // Keep track of this because it will be overwritten by AttemptConnection
                        SocketError currentFailure = args.SocketError;
                        Exception connectException = AttemptConnection();

                        if (connectException == null)
                        {
                            // don't call the callback, another connection attempt is successfully started
                            return;
                        }
                        else
                        {
                            SocketException socketException = connectException as SocketException;
                            if (socketException != null && socketException.SocketErrorCode == SocketError.NoData)
                            {
                                // If the error is NoData, that means there are no more IPAddresses to attempt
                                // a connection to.  Return the last error from an actual connection instead.
                                exception = new SocketException((int)currentFailure);
                            }
                            else
                            {
                                exception = connectException;
                            }

                            _state = State.Completed;
                        }
                    }
                }
                else
                {
                    if (_state != State.UserConnectAttempt)
                    {
                        NetEventSource.Fail(this, "Unexpected object state");
                    }
                    if (!RequiresUserConnectAttempt)
                    {
                        NetEventSource.Fail(this, "State.UserConnectAttempt without RequiresUserConnectAttempt");
                    }

                    if (args.SocketError == SocketError.Success)
                    {
                        _state = State.Completed;
                    }
                    else if (args.SocketError == SocketError.OperationAborted)
                    {
                        // The socket was closed while the connect was in progress.  This can happen if the user
                        // closes the socket, and is equivalent to a call to CancelConnectAsync
                        exception = new SocketException((int)SocketError.OperationAborted);
                        _state = State.Canceled;
                    }
                    else
                    {
                        // The connect attempt on the user's socket failed. Return the corresponding error.
                        exception = new SocketException((int)args.SocketError);
                        _state = State.Completed;
                    }
                }
            }

            if (exception == null)
            {
                Succeed();
            }
            else
            {
                AsyncFail(exception);
            }
        }

        // Called to initiate a connection attempt to the next address in the list.  Returns an exception
        // if the attempt failed synchronously, or null if it was successfully initiated.
        private Exception AttemptConnection()
        {
            try
            {
                IPAddress attemptAddress = GetNextAddress(out _lastAttemptSocket);

                if (attemptAddress == null)
                {
                    return new SocketException((int)SocketError.NoData);
                }

                if (attemptAddress == null)
                {
                    NetEventSource.Fail(this, "attemptAddress is null!");
                }

                _internalArgs.RemoteEndPoint = new IPEndPoint(attemptAddress, _endPoint.Port);

                return AttemptConnection(_lastAttemptSocket, _internalArgs);
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException)
                {
                    NetEventSource.Fail(this, "unexpected ObjectDisposedException");
                }
                return e;
            }
        }

        private Exception AttemptUserConnection()
        {
            Debug.Assert(!SocketPal.SupportsMultipleConnectAttempts);
            Debug.Assert(_lastAttemptSocket != null);

            try
            {
                _lastAttemptSocket.Dispose();

                // Setup the internal args. RemoteEndpoint should already be correct.
                _internalArgs.SetBuffer(_userArgs.Buffer, _userArgs.Offset, _userArgs.Count);

                return AttemptConnection(UserSocket, _internalArgs);
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException)
                {
                    NetEventSource.Fail(this, "unexpected ObjectDisposedException");
                }
                return e;
            }
        }

        private static Exception AttemptConnection(Socket attemptSocket, SocketAsyncEventArgs args)
        {
            try
            {
                if (attemptSocket == null)
                {
                    NetEventSource.Fail(null, "attemptSocket is null!");
                }

                if (!attemptSocket.ConnectAsync(args))
                {
                    return new SocketException((int)args.SocketError);
                }
            }
            catch (ObjectDisposedException)
            {
                // This can happen if the user closes the socket, and is equivalent to a call 
                // to CancelConnectAsync
                return new SocketException((int)SocketError.OperationAborted);
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }

        protected abstract void OnSucceed();

        private void Succeed()
        {
            OnSucceed();
            _userArgs.FinishWrapperConnectSuccess(_internalArgs.ConnectSocket, _internalArgs.BytesTransferred, _internalArgs.SocketFlags);
            _internalArgs.Dispose();
        }

        protected abstract void OnFail(bool abortive);

        private void OnFailOuter(bool abortive)
        {
            OnFail(abortive);

            if (!SocketPal.SupportsMultipleConnectAttempts && _lastAttemptSocket != null)
            {
                _lastAttemptSocket.Dispose();
                _lastAttemptSocket = null;
            }
        }

        private bool Fail(bool sync, Exception e)
        {
            if (sync)
            {
                SyncFail(e);
                return false;
            }
            else
            {
                AsyncFail(e);
                return true;
            }
        }

        private void SyncFail(Exception e)
        {
            OnFailOuter(false);

            if (_internalArgs != null)
            {
                _internalArgs.Dispose();
            }

            SocketException socketException = e as SocketException;
            if (socketException != null)
            {
                _userArgs.FinishConnectByNameSyncFailure(socketException, 0, SocketFlags.None);
            }
            else
            {
                throw e;
            }
        }

        private void AsyncFail(Exception e)
        {
            OnFailOuter(false);

            if (_internalArgs != null)
            {
                _internalArgs.Dispose();
            }

            _userArgs.FinishOperationAsyncFailure(e, 0, SocketFlags.None);
        }

        public void Cancel()
        {
            bool callOnFail = false;

            lock (_lockObject)
            {
                switch (_state)
                {
                    case State.NotStarted:
                        // Cancel was called before the Dns query was started.  The dns query won't be started
                        // and the connection attempt will fail synchronously after the state change to DnsQuery.
                        // All we need to do here is close all the sockets.
                        callOnFail = true;
                        break;

                    case State.DnsQuery:
                        // Cancel was called after the Dns query was started, but before it finished.  We can't
                        // actually cancel the Dns query, but we'll fake it by failing the connect attempt asynchronously
                        // from here, and silently dropping the connection attempt when the Dns query finishes.
                        Task.Factory.StartNew(
                            s => CallAsyncFail(s),
                            null,
                            CancellationToken.None,
                            TaskCreationOptions.DenyChildAttach,
                            TaskScheduler.Default);

                        callOnFail = true;
                        break;

                    case State.ConnectAttempt:
                    case State.UserConnectAttempt:
                        // Cancel was called after the Dns query completed, but before we had a connection result to give
                        // to the user.  Closing the sockets will cause any in-progress ConnectAsync call to fail immediately
                        // with OperationAborted, and will cause ObjectDisposedException from any new calls to ConnectAsync
                        // (which will be translated to OperationAborted by AttemptConnection).
                        callOnFail = true;
                        break;

                    case State.Completed:
                        // Cancel was called after we locked in a result to give to the user.  Ignore it and give the user
                        // the real completion.
                        break;

                    default:
                        NetEventSource.Fail(this, "Unexpected object state");
                        break;
                }

                _state = State.Canceled;
            }

            // Call this outside the lock because Socket.Close may block
            if (callOnFail)
            {
                OnFailOuter(true);
            }
        }

        // Call AsyncFail on a threadpool thread so it's asynchronous with respect to Cancel().
        private void CallAsyncFail(object ignored)
        {
            AsyncFail(new SocketException((int)SocketError.OperationAborted));
        }

        protected abstract IPAddress GetNextAddress(out Socket attemptSocket);
    }

    // Used when the instance ConnectAsync method is called, or when the DnsEndPoint specified
    // an AddressFamily.  There's only one Socket, and we only try addresses that match its
    // AddressFamily
    internal sealed class SingleSocketMultipleConnectAsync : MultipleConnectAsync
    {
        private Socket _socket;
        private bool _userSocket;

        protected override bool RequiresUserConnectAttempt { get { return !SocketPal.SupportsMultipleConnectAttempts; } }

        protected override Socket UserSocket
        {
            get
            {
                Debug.Assert(!SocketPal.SupportsMultipleConnectAttempts);
                return _socket;
            }
        }

        public SingleSocketMultipleConnectAsync(Socket socket, bool userSocket)
        {
            _socket = socket;
            _userSocket = userSocket;
        }

        protected override IPAddress GetNextAddress(out Socket attemptSocket)
        {
            IPAddress rval = null;
            do
            {
                if (_nextAddress >= _addressList.Length)
                {
                    attemptSocket = null;
                    return null;
                }

                rval = _addressList[_nextAddress];
                ++_nextAddress;
            }
            while (!_socket.CanTryAddressFamily(rval.AddressFamily));

            if (SocketPal.SupportsMultipleConnectAttempts)
            {
                attemptSocket = _socket;
            }
            else
            {
                attemptSocket = new Socket(_socket.AddressFamily, _socket.SocketType, _socket.ProtocolType);
                if (_socket.AddressFamily == AddressFamily.InterNetworkV6 && _socket.DualMode)
                {
                    attemptSocket.DualMode = true;
                }
            }

            return rval;
        }

        protected override void OnFail(bool abortive)
        {
            // Close the socket if this is an abortive failure (CancelConnectAsync) 
            // or if we created it internally
            if (abortive || !_userSocket)
            {
                _socket.Dispose();
            }
        }

        // nothing to do on success
        protected override void OnSucceed() { }
    }

    // This is used when the static ConnectAsync method is called.  We don't know the address family
    // ahead of time, so we create both IPv4 and IPv6 sockets.
    internal sealed class DualSocketMultipleConnectAsync : MultipleConnectAsync
    {
        private Socket _socket4;
        private Socket _socket6;

        protected override bool RequiresUserConnectAttempt { get { return false; } }

        protected override Socket UserSocket
        {
            get
            {
                Debug.Fail("Should never get here");
                throw new NotSupportedException();
            }
        }

        public DualSocketMultipleConnectAsync(SocketType socketType, ProtocolType protocolType)
        {
            Debug.Assert(SocketPal.SupportsMultipleConnectAttempts);

            if (Socket.OSSupportsIPv4)
            {
                _socket4 = new Socket(AddressFamily.InterNetwork, socketType, protocolType);
            }
            if (Socket.OSSupportsIPv6)
            {
                _socket6 = new Socket(AddressFamily.InterNetworkV6, socketType, protocolType);
            }
        }

        protected override IPAddress GetNextAddress(out Socket attemptSocket)
        {
            IPAddress rval = null;
            attemptSocket = null;

            while (attemptSocket == null)
            {
                if (_nextAddress >= _addressList.Length)
                {
                    return null;
                }

                rval = _addressList[_nextAddress];
                ++_nextAddress;

                if (rval.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    attemptSocket = _socket6;
                }
                else if (rval.AddressFamily == AddressFamily.InterNetwork)
                {
                    attemptSocket = _socket4;
                }
            }

            return rval;
        }

        // on success, close the socket that wasn't used
        protected override void OnSucceed()
        {
            if (_socket4 != null && !_socket4.Connected)
            {
                _socket4.Dispose();
            }
            if (_socket6 != null && !_socket6.Connected)
            {
                _socket6.Dispose();
            }
        }

        // close both sockets whether its abortive or not - we always create them internally
        protected override void OnFail(bool abortive)
        {
            if (_socket4 != null)
            {
                _socket4.Dispose();
            }
            if (_socket6 != null)
            {
                _socket6.Dispose();
            }
        }
    }

    internal sealed class MultipleSocketMultipleConnectAsync : MultipleConnectAsync
    {
        private readonly SocketType _socketType;
        private readonly ProtocolType _protocolType;
        private readonly bool _supportsIPv4;
        private readonly bool _supportsIPv6;

        protected override bool RequiresUserConnectAttempt { get { return false; } }

        protected override Socket UserSocket
        {
            get
            {
                Debug.Fail("Should never get here");
                throw new NotSupportedException();
            }
        }

        public MultipleSocketMultipleConnectAsync(SocketType socketType, ProtocolType protocolType)
        {
            Debug.Assert(!SocketPal.SupportsMultipleConnectAttempts);

            _socketType = socketType;
            _protocolType = protocolType;
            _supportsIPv4 = Socket.OSSupportsIPv4;
            _supportsIPv6 = Socket.OSSupportsIPv6;
        }

        protected override IPAddress GetNextAddress(out Socket attemptSocket)
        {
            if (_supportsIPv4 || _supportsIPv6)
            {
                while (_nextAddress < _addressList.Length)
                {
                    IPAddress rval = _addressList[_nextAddress];
                    ++_nextAddress;

                    if (_supportsIPv6 && rval.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        attemptSocket = new Socket(AddressFamily.InterNetworkV6, _socketType, _protocolType);
                        return rval;
                    }
                    else if (_supportsIPv4 && rval.AddressFamily == AddressFamily.InterNetwork)
                    {
                        attemptSocket = new Socket(AddressFamily.InterNetwork, _socketType, _protocolType);
                        return rval;
                    }
                }
            }

            attemptSocket = null;
            return null;
        }

        // nothing to do on failure
        protected override void OnFail(bool abortive) { }

        // nothing to do on success
        protected override void OnSucceed() { }
    }
}
