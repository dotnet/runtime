// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        // AcceptSocket property variables.
        private Socket _acceptSocket;
        private Socket _connectSocket;

        // Single buffer.
        private Memory<byte> _buffer;
        private int _offset;
        private int _count;
        private bool _bufferIsExplicitArray;

        // BufferList property variables.
        private IList<ArraySegment<byte>> _bufferList;
        private List<ArraySegment<byte>> _bufferListInternal;

        // BytesTransferred property variables.
        private int _bytesTransferred;

        // DisconnectReuseSocket propery variables.
        private bool _disconnectReuseSocket;

        // LastOperation property variables.
        private SocketAsyncOperation _completedOperation;

        // ReceiveMessageFromPacketInfo property variables.
        private IPPacketInformation _receiveMessageFromPacketInfo;

        // RemoteEndPoint property variables.
        private EndPoint _remoteEndPoint;

        // SendPacketsSendSize property variable.
        private int _sendPacketsSendSize;

        // SendPacketsElements property variables.
        private SendPacketsElement[] _sendPacketsElements;

        // SendPacketsFlags property variable.
        private TransmitFileOptions _sendPacketsFlags;

        // SocketError property variables.
        private SocketError _socketError;
        private Exception _connectByNameError;

        // SocketFlags property variables.
        private SocketFlags _socketFlags;

        // UserToken property variables.
        private object _userToken;

        // Internal buffer for AcceptEx when Buffer not supplied.
        private byte[] _acceptBuffer;
        private int _acceptAddressBufferCount;

        // Internal SocketAddress buffer.
        internal Internals.SocketAddress _socketAddress;

        // Misc state variables.
        private readonly bool _flowExecutionContext;
        private ExecutionContext _context;
        private static readonly ContextCallback s_executionCallback = ExecutionCallback;
        private Socket _currentSocket;
        private bool _userSocket; // if false when performing Connect, _currentSocket should be disposed
        private bool _disposeCalled;

        // Controls thread safety via Interlocked.
        private const int Configuring = -1;
        private const int Free = 0;
        private const int InProgress = 1;
        private const int Disposed = 2;
        private int _operating;

        private MultipleConnectAsync _multipleConnect;

        public SocketAsyncEventArgs() : this(flowExecutionContext: true)
        {
        }

        /// <summary>Initialize the SocketAsyncEventArgs</summary>
        /// <param name="flowExecutionContext">
        /// Whether to capture and flow ExecutionContext. ExecutionContext flow should only
        /// be disabled if it's going to be handled by higher layers.
        /// </param>
        internal SocketAsyncEventArgs(bool flowExecutionContext)
        {
            _flowExecutionContext = flowExecutionContext;
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
            get
            {
                if (_bufferIsExplicitArray)
                {
                    bool success = MemoryMarshal.TryGetArray(_buffer, out ArraySegment<byte> arraySegment);
                    Debug.Assert(success);
                    return arraySegment.Array;
                }

                return null;
            }
        }

        public Memory<byte> MemoryBuffer => _buffer;

        public int Offset => _offset;

        public int Count => _count;

        // SendPacketsFlags property.
        public TransmitFileOptions SendPacketsFlags
        {
            get { return _sendPacketsFlags; }
            set { _sendPacketsFlags = value; }
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
                    if (value != null)
                    {
                        if (!_buffer.Equals(default))
                        {
                            // Can't have both set
                            throw new ArgumentException(SR.Format(SR.net_ambiguousbuffers, nameof(Buffer)));
                        }

                        // Copy the user-provided list into our internal buffer list,
                        // so that we are not affected by subsequent changes to the list.
                        // We reuse the existing list so that we can avoid reallocation when possible.
                        int bufferCount = value.Count;
                        if (_bufferListInternal == null)
                        {
                            _bufferListInternal = new List<ArraySegment<byte>>(bufferCount);
                        }
                        else
                        {
                            _bufferListInternal.Clear();
                        }

                        for (int i = 0; i < bufferCount; i++)
                        {
                            ArraySegment<byte> buffer = value[i];
                            RangeValidationHelpers.ValidateSegment(buffer);
                            _bufferListInternal.Add(buffer);
                        }
                    }
                    else
                    {
                        _bufferListInternal?.Clear();
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

        public event EventHandler<SocketAsyncEventArgs> Completed;

        protected virtual void OnCompleted(SocketAsyncEventArgs e)
        {
            Completed?.Invoke(e._currentSocket, e);
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

        public void SetBuffer(int offset, int count)
        {
            StartConfiguring();
            try
            {
                if (!_buffer.Equals(default))
                {
                    if ((uint)offset > _buffer.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    }
                    if ((uint)count > (_buffer.Length - offset))
                    {
                        throw new ArgumentOutOfRangeException(nameof(count));
                    }
                    if (!_bufferIsExplicitArray)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_BufferNotExplicitArray);
                    }

                    _offset = offset;
                    _count = count;
                }
            }
            finally
            {
                Complete();
            }
        }

        internal void CopyBufferFrom(SocketAsyncEventArgs source)
        {
            StartConfiguring();
            try
            {
                _buffer = source._buffer;
                _offset = source._offset;
                _count = source._count;
                _bufferIsExplicitArray = source._bufferIsExplicitArray;
            }
            finally
            {
                Complete();
            }
        }

        public void SetBuffer(byte[] buffer, int offset, int count)
        {
            StartConfiguring();
            try
            {
                if (buffer == null)
                {
                    // Clear out existing buffer.
                    _buffer = default;
                    _offset = 0;
                    _count = 0;
                    _bufferIsExplicitArray = false;
                }
                else
                {
                    // Can't have both Buffer and BufferList.
                    if (_bufferList != null)
                    {
                        throw new ArgumentException(SR.Format(SR.net_ambiguousbuffers, nameof(BufferList)));
                    }

                    // Offset and count can't be negative and the
                    // combination must be in bounds of the array.
                    if ((uint)offset > buffer.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    }
                    if ((uint)count > (buffer.Length - offset))
                    {
                        throw new ArgumentOutOfRangeException(nameof(count));
                    }

                    _buffer = buffer;
                    _offset = offset;
                    _count = count;
                    _bufferIsExplicitArray = true;
                }
            }
            finally
            {
                Complete();
            }
        }

        public void SetBuffer(Memory<byte> buffer)
        {
            StartConfiguring();
            try
            {
                if (buffer.Length != 0 && _bufferList != null)
                {
                    throw new ArgumentException(SR.Format(SR.net_ambiguousbuffers, nameof(BufferList)));
                }

                _buffer = buffer;
                _offset = 0;
                _count = buffer.Length;
                _bufferIsExplicitArray = false;
            }
            finally
            {
                Complete();
            }
        }

        internal bool HasMultipleBuffers => _bufferList != null;

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
            CompleteCore();

            // Clear any ExecutionContext that may have been captured.
            _context = null;

            // Mark as not in-use.
            _operating = Free;

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
            FreeInternals();

            // FileStreams may be created when using SendPacketsAsync - this Disposes them.
            FinishOperationSendPackets();

            // Don't bother finalizing later.
            GC.SuppressFinalize(this);
        }

        ~SocketAsyncEventArgs()
        {
            if (!Environment.HasShutdownStarted)
            {
                FreeInternals();
            }
        }

        // NOTE: Use a try/finally to make sure Complete is called when you're done
        private void StartConfiguring()
        {
            int status = Interlocked.CompareExchange(ref _operating, Configuring, Free);
            if (status != Free)
            {
                ThrowForNonFreeStatus(status);
            }
        }

        private void ThrowForNonFreeStatus(int status)
        {
            Debug.Assert(status == InProgress || status == Configuring || status == Disposed, $"Unexpected status: {status}");
            if (status == Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else
            {
                throw new InvalidOperationException(SR.net_socketopinprogress);
            }
        }

        // Prepares for a native async socket call.
        // This method performs the tasks common to all socket operations.
        internal void StartOperationCommon(Socket socket, SocketAsyncOperation operation)
        {
            // Change status to "in-use".
            int status = Interlocked.CompareExchange(ref _operating, InProgress, Free);
            if (status != Free)
            {
                ThrowForNonFreeStatus(status);
            }

            // Set the operation type and store the socket as current.
            _completedOperation = operation;
            _currentSocket = socket;

            // Capture execution context if needed (it is unless explicitly disabled).
            if (_flowExecutionContext)
            {
                _context = ExecutionContext.Capture();
            }

            StartOperationCommonCore();
        }

        partial void StartOperationCommonCore();

        internal void StartOperationAccept()
        {
            // AcceptEx needs a single buffer that's the size of two native sockaddr buffers with 16
            // extra bytes each. It can also take additional buffer space in front of those special
            // sockaddr structures that can be filled in with initial data coming in on a connection.
            _acceptAddressBufferCount = 2 * (Socket.GetAddressSize(_currentSocket._rightEndPoint) + 16);

            // If our caller specified a buffer (willing to get received data with the Accept) then
            // it needs to be large enough for the two special sockaddr buffers that AcceptEx requires.
            // Throw if that buffer is not large enough.
            bool userSuppliedBuffer = !_buffer.Equals(default);
            if (userSuppliedBuffer)
            {
                // Caller specified a buffer - see if it is large enough
                if (_count < _acceptAddressBufferCount)
                {
                    throw new ArgumentException(SR.Format(SR.net_buffercounttoosmall, nameof(Count)));
                }
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
        }

        internal void StartOperationConnect(MultipleConnectAsync multipleConnect, bool userSocket)
        {
            _multipleConnect = multipleConnect;
            _connectSocket = null;
            _userSocket = userSocket;
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

        internal void FinishOperationSyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            SetResults(socketError, bytesTransferred, flags);

            // This will be null if we're doing a static ConnectAsync to a DnsEndPoint with AddressFamily.Unspecified;
            // the attempt socket will be closed anyways, so not updating the state is OK.
            // If we're doing a static ConnectAsync to an IPEndPoint, we need to dispose
            // of the socket, as we manufactured it and the caller has no opportunity to do so.
            Socket currentSocket = _currentSocket;
            if (currentSocket != null)
            {
                currentSocket.UpdateStatusAfterSocketError(socketError);
                if (_completedOperation == SocketAsyncOperation.Connect && !_userSocket)
                {
                    currentSocket.Dispose();
                    _currentSocket = null;
                }
            }

            switch (_completedOperation)
            {
                case SocketAsyncOperation.SendPackets:
                    // We potentially own FileStreams that need to be disposed.
                    FinishOperationSendPackets();
                    break;
            }

            Complete();
        }

        internal void FinishConnectByNameSyncFailure(Exception exception, int bytesTransferred, SocketFlags flags)
        {
            SetResults(exception, bytesTransferred, flags);

            _currentSocket?.UpdateStatusAfterSocketError(_socketError);

            Complete();
        }

        internal void FinishOperationAsyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext context = _context; // store context before it's cleared as part of finishing the operation

            FinishOperationSyncFailure(socketError, bytesTransferred, flags);

            if (context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        internal void FinishConnectByNameAsyncFailure(Exception exception, int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext context = _context; // store context before it's cleared as part of finishing the operation

            FinishConnectByNameSyncFailure(exception, bytesTransferred, flags);

            if (context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        internal void FinishWrapperConnectSuccess(Socket connectSocket, int bytesTransferred, SocketFlags flags)
        {
            SetResults(SocketError.Success, bytesTransferred, flags);
            _currentSocket = connectSocket;
            _connectSocket = connectSocket;

            // Complete the operation and raise the event.
            ExecutionContext context = _context; // store context before it's cleared as part of completing the operation
            Complete();
            if (context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        internal void FinishOperationSyncSuccess(int bytesTransferred, SocketFlags flags)
        {
            SetResults(SocketError.Success, bytesTransferred, flags);

            if (NetEventSource.IsEnabled && bytesTransferred > 0)
            {
                LogBuffer(bytesTransferred);
            }

            SocketError socketError = SocketError.Success;
            switch (_completedOperation)
            {
                case SocketAsyncOperation.Accept:
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
                        SetResults(socketError, bytesTransferred, flags);
                        _acceptSocket = null;
                        _currentSocket.UpdateStatusAfterSocketError(socketError);
                    }
                    break;

                case SocketAsyncOperation.Connect:
                    socketError = FinishOperationConnect();
                    if (socketError == SocketError.Success)
                    {
                        if (NetEventSource.IsEnabled) NetEventSource.Connected(_currentSocket, _currentSocket.LocalEndPoint, _currentSocket.RemoteEndPoint);

                        // Mark socket connected.
                        _currentSocket.SetToConnected();
                        _connectSocket = _currentSocket;
                    }
                    else
                    {
                        SetResults(socketError, bytesTransferred, flags);
                        _currentSocket.UpdateStatusAfterSocketError(socketError);
                    }
                    break;

                case SocketAsyncOperation.Disconnect:
                    _currentSocket.SetToDisconnected();
                    _currentSocket._remoteEndPoint = null;
                    break;

                case SocketAsyncOperation.ReceiveFrom:
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

                case SocketAsyncOperation.SendPackets:
                    FinishOperationSendPackets();
                    break;
            }

            Complete();
        }

        internal void FinishOperationAsyncSuccess(int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext context = _context; // store context before it's cleared as part of finishing the operation

            FinishOperationSyncSuccess(bytesTransferred, flags);

            // Raise completion event.
            if (context == null)
            {
                OnCompleted(this);
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }
    }
}
