// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        // AcceptSocket property variables.
        internal Socket _acceptSocket;
        private Socket _connectSocket;

        // Buffer,Offset,Count property variables.
        internal byte[] _buffer;
        internal int _count;
        internal int _offset;

        // BufferList property variables.
        internal IList<ArraySegment<byte>> _bufferList;

        // BytesTransferred property variables.
        private int _bytesTransferred;

        // Completed event property variables.
        private event EventHandler<SocketAsyncEventArgs> _completed;
        private bool _completedChanged;

        // DisconnectReuseSocket propery variables.
        private bool _disconnectReuseSocket;

        // LastOperation property variables.
        private SocketAsyncOperation _completedOperation;

        // ReceiveMessageFromPacketInfo property variables.
        private IPPacketInformation _receiveMessageFromPacketInfo;

        // RemoteEndPoint property variables.
        private EndPoint _remoteEndPoint;

        // SendPacketsSendSize property variable.
        internal int _sendPacketsSendSize;

        // SendPacketsElements property variables.
        internal SendPacketsElement[] _sendPacketsElements;

        // SocketError property variables.
        private SocketError _socketError;
        private Exception _connectByNameError;

        // SocketFlags property variables.
        internal SocketFlags _socketFlags;

        // UserToken property variables.
        private object _userToken;

        // Internal buffer for AcceptEx when Buffer not supplied.
        internal byte[] _acceptBuffer;
        internal int _acceptAddressBufferCount;

        // Internal SocketAddress buffer.
        internal Internals.SocketAddress _socketAddress;

        // Misc state variables.
        private ExecutionContext _context;
        private static readonly ContextCallback s_executionCallback = ExecutionCallback;
        private Socket _currentSocket;
        private bool _disposeCalled;

        // Controls thread safety via Interlocked.
        private const int Configuring = -1;
        private const int Free = 0;
        private const int InProgress = 1;
        private const int Disposed = 2;
        private int _operating;

        private MultipleConnectAsync _multipleConnect;

        public SocketAsyncEventArgs()
        {
            InitializeInternals();
        }

        public Socket AcceptSocket
        {
            get { return _acceptSocket; }
            set { _acceptSocket = value; }
        }

        public Socket ConnectSocket
        {
            get { return _connectSocket; }
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Count
        {
            get { return _count; }
        }

        // NOTE: this property is mutually exclusive with Buffer.
        // Setting this property with an existing non-null Buffer will throw.    
        public IList<ArraySegment<byte>> BufferList
        {
            get { return _bufferList; }
            set
            {
                StartConfiguring();
                try
                {
                    if (value != null && _buffer != null)
                    {
                        throw new ArgumentException(SR.Format(SR.net_ambiguousbuffers, "Buffer"));
                    }
                    _bufferList = value;
                    SetupMultipleBuffers();
                }
                finally
                {
                    Complete();
                }
            }
        }

        public int BytesTransferred
        {
            get { return _bytesTransferred; }
        }

        public event EventHandler<SocketAsyncEventArgs> Completed
        {
            add
            {
                _completed += value;
                _completedChanged = true;
            }
            remove
            {
                _completed -= value;
                _completedChanged = true;
            }
        }

        protected virtual void OnCompleted(SocketAsyncEventArgs e)
        {
            EventHandler<SocketAsyncEventArgs> handler = _completed;
            if (handler != null)
            {
                handler(e._currentSocket, e);
            }
        }

        // DisconnectResuseSocket property.
        public bool DisconnectReuseSocket
        {
            get { return _disconnectReuseSocket; }
            set { _disconnectReuseSocket = value; }
        }

        public SocketAsyncOperation LastOperation
        {
            get { return _completedOperation; }
        }

        public IPPacketInformation ReceiveMessageFromPacketInfo
        {
            get { return _receiveMessageFromPacketInfo; }
        }

        public EndPoint RemoteEndPoint
        {
            get { return _remoteEndPoint; }
            set { _remoteEndPoint = value; }
        }

        public SendPacketsElement[] SendPacketsElements
        {
            get { return _sendPacketsElements; }
            set
            {
                StartConfiguring();
                try
                {
                    _sendPacketsElements = value;
                    SetupSendPacketsElements();
                }
                finally
                {
                    Complete();
                }
            }
        }

        public int SendPacketsSendSize
        {
            get { return _sendPacketsSendSize; }
            set { _sendPacketsSendSize = value; }
        }

        public SocketError SocketError
        {
            get { return _socketError; }
            set { _socketError = value; }
        }

        public Exception ConnectByNameError
        {
            get { return _connectByNameError; }
        }

        public SocketFlags SocketFlags
        {
            get { return _socketFlags; }
            set { _socketFlags = value; }
        }

        public object UserToken
        {
            get { return _userToken; }
            set { _userToken = value; }
        }

        public void SetBuffer(byte[] buffer, int offset, int count)
        {
            SetBufferInternal(buffer, offset, count);
        }

        public void SetBuffer(int offset, int count)
        {
            SetBufferInternal(_buffer, offset, count);
        }

        private void SetBufferInternal(byte[] buffer, int offset, int count)
        {
            StartConfiguring();
            try
            {
                if (buffer == null)
                {
                    // Clear out existing buffer.
                    _buffer = null;
                    _offset = 0;
                    _count = 0;
                }
                else
                {
                    // Can't have both Buffer and BufferList.
                    if (_bufferList != null)
                    {
                        throw new ArgumentException(SR.Format(SR.net_ambiguousbuffers, "BufferList"));
                    }

                    // Offset and count can't be negative and the 
                    // combination must be in bounds of the array.
                    if (offset < 0 || offset > buffer.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    }
                    if (count < 0 || count > (buffer.Length - offset))
                    {
                        throw new ArgumentOutOfRangeException(nameof(count));
                    }

                    _buffer = buffer;
                    _offset = offset;
                    _count = count;
                }

                // Pin new or unpin old buffer if necessary.
                SetupSingleBuffer();
            }
            finally
            {
                Complete();
            }
        }

        internal void SetResults(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            _socketError = socketError;
            _connectByNameError = null;
            _bytesTransferred = bytesTransferred;
            _socketFlags = flags;
        }

        internal void SetResults(Exception exception, int bytesTransferred, SocketFlags flags)
        {
            _connectByNameError = exception;
            _bytesTransferred = bytesTransferred;
            _socketFlags = flags;

            if (exception == null)
            {
                _socketError = SocketError.Success;
            }
            else
            {
                SocketException socketException = exception as SocketException;
                if (socketException != null)
                {
                    _socketError = socketException.SocketErrorCode;
                }
                else
                {
                    _socketError = SocketError.SocketError;
                }
            }
        }

        private static void ExecutionCallback(object state)
        {
            var thisRef = (SocketAsyncEventArgs)state;
            thisRef.OnCompleted(thisRef);
        }

        // Marks this object as no longer "in-use". Will also execute a Dispose deferred
        // because I/O was in progress.  
        internal void Complete()
        {
            // Mark as not in-use.
            _operating = Free;

            InnerComplete();

            // Check for deferred Dispose().
            // The deferred Dispose is not guaranteed if Dispose is called while an operation is in progress. 
            // The _disposeCalled variable is not managed in a thread-safe manner on purpose for performance.
            if (_disposeCalled)
            {
                Dispose();
            }
        }

        // Dispose call to implement IDisposable.
        public void Dispose()
        {
            // Remember that Dispose was called.
            _disposeCalled = true;

            // Check if this object is in-use for an async socket operation.
            if (Interlocked.CompareExchange(ref _operating, Disposed, Free) != Free)
            {
                // Either already disposed or will be disposed when current operation completes.
                return;
            }

            // OK to dispose now.
            FreeInternals(false);

            // Don't bother finalizing later.
            GC.SuppressFinalize(this);
        }

        ~SocketAsyncEventArgs()
        {
            FreeInternals(true);
        }

        // NOTE: Use a try/finally to make sure Complete is called when you're done
        private void StartConfiguring()
        {
            int status = Interlocked.CompareExchange(ref _operating, Configuring, Free);
            if (status == InProgress || status == Configuring)
            {
                throw new InvalidOperationException(SR.net_socketopinprogress);
            }
            else if (status == Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        // Prepares for a native async socket call.
        // This method performs the tasks common to all socket operations.
        internal void StartOperationCommon(Socket socket)
        {
            // Change status to "in-use".
            if (Interlocked.CompareExchange(ref _operating, InProgress, Free) != Free)
            {
                // If it was already "in-use" check if Dispose was called.
                if (_disposeCalled)
                {
                    // Dispose was called - throw ObjectDisposed.
                    throw new ObjectDisposedException(GetType().FullName);
                }

                // Only one at a time.
                throw new InvalidOperationException(SR.net_socketopinprogress);
            }

            // Prepare execution context for callback.
            // If event delegates have changed or socket has changed
            // then discard any existing context.
            if (_completedChanged || socket != _currentSocket)
            {
                _completedChanged = false;
                _context = null;
            }

            // Capture execution context if none already.
            if (_context == null)
            {
                _context = ExecutionContext.Capture();
            }

            // Remember current socket.
            _currentSocket = socket;
        }

        internal void StartOperationAccept()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.Accept;

            // AcceptEx needs a single buffer with room for two special sockaddr data structures.
            // It can also take additional buffer space in front of those special sockaddr 
            // structures that can be filled in with initial data coming in on a connection.

            // First calculate the special AcceptEx address buffer size.
            // It is the size of two native sockaddr buffers with 16 extra bytes each.
            // The native sockaddr buffers vary by address family so must reference the current socket.
            _acceptAddressBufferCount = 2 * (_currentSocket._rightEndPoint.Serialize().Size + 16);

            // If our caller specified a buffer (willing to get received data with the Accept) then
            // it needs to be large enough for the two special sockaddr buffers that AcceptEx requires.
            // Throw if that buffer is not large enough.  
            bool userSuppliedBuffer = _buffer != null;
            if (userSuppliedBuffer)
            {
                // Caller specified a buffer - see if it is large enough
                if (_count < _acceptAddressBufferCount)
                {
                    throw new ArgumentException(SR.Format(SR.net_buffercounttoosmall, "Count"));
                }

                // Buffer is already pinned if necessary.
            }
            else
            {
                // Caller didn't specify a buffer so use an internal one.
                // See if current internal one is big enough, otherwise create a new one.
                if (_acceptBuffer == null || _acceptBuffer.Length < _acceptAddressBufferCount)
                {
                    _acceptBuffer = new byte[_acceptAddressBufferCount];
                }
            }

            InnerStartOperationAccept(userSuppliedBuffer);
        }

        internal void StartOperationConnect()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.Connect;
            _multipleConnect = null;
            _connectSocket = null;

            InnerStartOperationConnect();
        }

        internal void StartOperationWrapperConnect(MultipleConnectAsync args)
        {
            _completedOperation = SocketAsyncOperation.Connect;
            _multipleConnect = args;
            _connectSocket = null;
        }

        internal void CancelConnectAsync()
        {
            if (_operating == InProgress && _completedOperation == SocketAsyncOperation.Connect)
            {
                if (_multipleConnect != null)
                {
                    // If a multiple connect is in progress, abort it.
                    _multipleConnect.Cancel();
                }
                else
                {
                    // Otherwise we're doing a normal ConnectAsync - cancel it by closing the socket.
                    // _currentSocket will only be null if _multipleConnect was set, so we don't have to check.
                    if (_currentSocket == null)
                    {
                        NetEventSource.Fail(this, "CurrentSocket and MultipleConnect both null!");
                    }
                    _currentSocket.Dispose();
                }
            }
        }

        internal void StartOperationDisconnect()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.Disconnect;
            InnerStartOperationDisconnect();
        }

        internal void StartOperationReceive()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.Receive;
            InnerStartOperationReceive();
        }

        internal void StartOperationReceiveFrom()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.ReceiveFrom;
            InnerStartOperationReceiveFrom();
        }

        internal void StartOperationReceiveMessageFrom()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.ReceiveMessageFrom;
            InnerStartOperationReceiveMessageFrom();
        }

        internal void StartOperationSend()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.Send;
            InnerStartOperationSend();
        }

        internal void StartOperationSendPackets()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.SendPackets;
            InnerStartOperationSendPackets();
        }

        internal void StartOperationSendTo()
        {
            // Remember the operation type.
            _completedOperation = SocketAsyncOperation.SendTo;
            InnerStartOperationSendTo();
        }

        internal void UpdatePerfCounters(int size, bool sendOp)
        {
            if (sendOp)
            {
                SocketPerfCounter.Instance.Increment(SocketPerfCounterName.SocketBytesSent, size);
                if (_currentSocket.Transport == TransportType.Udp)
                {
                    SocketPerfCounter.Instance.Increment(SocketPerfCounterName.SocketDatagramsSent);
                }
            }
            else
            {
                SocketPerfCounter.Instance.Increment(SocketPerfCounterName.SocketBytesReceived, size);
                if (_currentSocket.Transport == TransportType.Udp)
                {
                    SocketPerfCounter.Instance.Increment(SocketPerfCounterName.SocketDatagramsReceived);
                }
            }
        }

        internal void FinishOperationSyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            SetResults(socketError, bytesTransferred, flags);

            // This will be null if we're doing a static ConnectAsync to a DnsEndPoint with AddressFamily.Unspecified;
            // the attempt socket will be closed anyways, so not updating the state is OK.
            if (_currentSocket != null)
            {
                _currentSocket.UpdateStatusAfterSocketError(socketError);
            }

            Complete();
        }

        internal void FinishConnectByNameSyncFailure(Exception exception, int bytesTransferred, SocketFlags flags)
        {
            SetResults(exception, bytesTransferred, flags);

            if (_currentSocket != null)
            {
                _currentSocket.UpdateStatusAfterSocketError(_socketError);
            }

            Complete();
        }

        internal void FinishOperationAsyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            SetResults(socketError, bytesTransferred, flags);

            // This will be null if we're doing a static ConnectAsync to a DnsEndPoint with AddressFamily.Unspecified;
            // the attempt socket will be closed anyways, so not updating the state is OK.
            if (_currentSocket != null)
            {
                _currentSocket.UpdateStatusAfterSocketError(socketError);
            }

            Complete();
            if (_context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(_context, s_executionCallback, this);
            }
        }

        internal void FinishOperationAsyncFailure(Exception exception, int bytesTransferred, SocketFlags flags)
        {
            SetResults(exception, bytesTransferred, flags);

            if (_currentSocket != null)
            {
                _currentSocket.UpdateStatusAfterSocketError(_socketError);
            }

            Complete();
            if (_context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(_context, s_executionCallback, this);
            }
        }

        internal void FinishWrapperConnectSuccess(Socket connectSocket, int bytesTransferred, SocketFlags flags)
        {
            SetResults(SocketError.Success, bytesTransferred, flags);
            _currentSocket = connectSocket;
            _connectSocket = connectSocket;

            // Complete the operation and raise the event.
            Complete();
            if (_context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(_context, s_executionCallback, this);
            }
        }

        internal void FinishOperationSuccess(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            SetResults(socketError, bytesTransferred, flags);

            switch (_completedOperation)
            {
                case SocketAsyncOperation.Accept:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, false);
                        }
                    }

                    // Get the endpoint.
                    Internals.SocketAddress remoteSocketAddress = IPEndPointExtensions.Serialize(_currentSocket._rightEndPoint);

                    socketError = FinishOperationAccept(remoteSocketAddress);

                    if (socketError == SocketError.Success)
                    {
                        _acceptSocket = _currentSocket.UpdateAcceptSocket(_acceptSocket, _currentSocket._rightEndPoint.Create(remoteSocketAddress));

                        if (NetEventSource.IsEnabled) NetEventSource.Accepted(_acceptSocket, _acceptSocket.RemoteEndPoint, _acceptSocket.LocalEndPoint);
                    }
                    else
                    {
                        SetResults(socketError, bytesTransferred, SocketFlags.None);
                        _acceptSocket = null;
                    }
                    break;

                case SocketAsyncOperation.Connect:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, true);
                        }
                    }

                    socketError = FinishOperationConnect();

                    // Mark socket connected.
                    if (socketError == SocketError.Success)
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Connected(_currentSocket, _currentSocket.LocalEndPoint, _currentSocket.RemoteEndPoint);

                        _currentSocket.SetToConnected();
                        _connectSocket = _currentSocket;
                    }
                    break;

                case SocketAsyncOperation.Disconnect:
                    _currentSocket.SetToDisconnected();
                    _currentSocket._remoteEndPoint = null;

                    break;

                case SocketAsyncOperation.Receive:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, false);
                        }
                    }
                    break;

                case SocketAsyncOperation.ReceiveFrom:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, false);
                        }
                    }

                    // Deal with incoming address.
                    _socketAddress.InternalSize = GetSocketAddressSize();
                    Internals.SocketAddress socketAddressOriginal = IPEndPointExtensions.Serialize(_remoteEndPoint);
                    if (!socketAddressOriginal.Equals(_socketAddress))
                    {
                        try
                        {
                            _remoteEndPoint = _remoteEndPoint.Create(_socketAddress);
                        }
                        catch
                        {
                        }
                    }
                    break;

                case SocketAsyncOperation.ReceiveMessageFrom:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, false);
                        }
                    }

                    // Deal with incoming address.
                    _socketAddress.InternalSize = GetSocketAddressSize();
                    socketAddressOriginal = IPEndPointExtensions.Serialize(_remoteEndPoint);
                    if (!socketAddressOriginal.Equals(_socketAddress))
                    {
                        try
                        {
                            _remoteEndPoint = _remoteEndPoint.Create(_socketAddress);
                        }
                        catch
                        {
                        }
                    }

                    FinishOperationReceiveMessageFrom();
                    break;

                case SocketAsyncOperation.Send:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, true);
                        }
                    }
                    break;

                case SocketAsyncOperation.SendPackets:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogSendPacketsBuffers(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, true);
                        }
                    }

                    FinishOperationSendPackets();
                    break;

                case SocketAsyncOperation.SendTo:
                    if (bytesTransferred > 0)
                    {
                        // Log and Perf counters.
                        if (NetEventSource.IsEnabled)
                        {
                            LogBuffer(bytesTransferred);
                        }
                        if (Socket.s_perfCountersEnabled)
                        {
                            UpdatePerfCounters(bytesTransferred, true);
                        }
                    }
                    break;
            }

            if (socketError != SocketError.Success)
            {
                // Asynchronous failure or something went wrong after async success.
                SetResults(socketError, bytesTransferred, flags);
                _currentSocket.UpdateStatusAfterSocketError(socketError);
            }

            // Complete the operation and raise completion event.
            Complete();
            if (_context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(_context, s_executionCallback, this);
            }
        }
    }
}
