// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Sockets
{
    public partial class SocketAsyncEventArgs : EventArgs, IDisposable
    {
        // AcceptSocket property variables.
        private Socket? _acceptSocket;
        private Socket? _connectSocket;

        // Single buffer.
        private Memory<byte> _buffer;
        private int _offset;
        private int _count;
        private bool _bufferIsExplicitArray;

        // BufferList property variables.
        private IList<ArraySegment<byte>>? _bufferList;
        private List<ArraySegment<byte>>? _bufferListInternal;

        // BytesTransferred property variables.
        private int _bytesTransferred;

        // DisconnectReuseSocket propery variables.
        private bool _disconnectReuseSocket;

        // LastOperation property variables.
        private SocketAsyncOperation _completedOperation;

        // ReceiveMessageFromPacketInfo property variables.
        private IPPacketInformation _receiveMessageFromPacketInfo;

        // RemoteEndPoint property variables.
        private EndPoint? _remoteEndPoint;

        // SendPacketsSendSize property variable.
        private int _sendPacketsSendSize;

        // SendPacketsElements property variables.
        private SendPacketsElement[]? _sendPacketsElements;

        // SendPacketsFlags property variable.
        private TransmitFileOptions _sendPacketsFlags;

        // SocketError property variables.
        private SocketError _socketError;
        private Exception? _connectByNameError;

        // SocketFlags property variables.
        private SocketFlags _socketFlags;

        // UserToken property variables.
        private object? _userToken;

        // Internal buffer for AcceptEx when Buffer not supplied.
        private byte[]? _acceptBuffer;
        private int _acceptAddressBufferCount;

        // Internal SocketAddress buffer.
        internal Internals.SocketAddress? _socketAddress;

        // Misc state variables.
        private readonly bool _flowExecutionContext;
        private ExecutionContext? _context;
        private static readonly ContextCallback s_executionCallback = ExecutionCallback;
        private Socket? _currentSocket;
        private bool _userSocket; // if false when performing Connect, _currentSocket should be disposed
        private bool _disposeCalled;

        // Controls thread safety via Interlocked.
        private const int Configuring = -1;
        private const int Free = 0;
        private const int InProgress = 1;
        private const int Disposed = 2;
        private int _operating;

        private CancellationTokenSource? _multipleConnectCancellation;

        public SocketAsyncEventArgs() : this(unsafeSuppressExecutionContextFlow: false)
        {
        }

        /// <summary>Initialize the SocketAsyncEventArgs</summary>
        /// <param name="unsafeSuppressExecutionContextFlow">
        /// Whether to disable the capturing and flow of ExecutionContext. ExecutionContext flow should only
        /// be disabled if it's going to be handled by higher layers.
        /// </param>
        public SocketAsyncEventArgs(bool unsafeSuppressExecutionContextFlow)
        {
            _flowExecutionContext = !unsafeSuppressExecutionContextFlow;
            InitializeInternals();
        }

        public Socket? AcceptSocket
        {
            get { return _acceptSocket; }
            set { _acceptSocket = value; }
        }

        public Socket? ConnectSocket
        {
            get { return _connectSocket; }
        }

        public byte[]? Buffer
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
        public IList<ArraySegment<byte>>? BufferList
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
                            throw new ArgumentException(SR.net_ambiguousbuffers);
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

        public event EventHandler<SocketAsyncEventArgs>? Completed;

        private void OnCompletedInternal()
        {
            // The following check checks if the operation was Accept (1) or Connect (2)
            if (LastOperation <= SocketAsyncOperation.Connect)
            {
                AfterConnectAcceptTelemetry();
            }

            OnCompleted(this);
        }

        protected virtual void OnCompleted(SocketAsyncEventArgs e)
        {
            Completed?.Invoke(e._currentSocket, e);
        }

        private void AfterConnectAcceptTelemetry()
        {
            switch (LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    SocketsTelemetry.Log.AfterAccept(SocketError);
                    break;

                case SocketAsyncOperation.Connect:
                    SocketsTelemetry.Log.AfterConnect(SocketError);
                    break;

                default:
                    Debug.Fail($"Callers should guard against calling this method for '{LastOperation}'");
                    break;
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

        public EndPoint? RemoteEndPoint
        {
            get { return _remoteEndPoint; }
            set { _remoteEndPoint = value; }
        }

        public SendPacketsElement[]? SendPacketsElements
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

        public Exception? ConnectByNameError
        {
            get { return _connectByNameError; }
        }

        public SocketFlags SocketFlags
        {
            get { return _socketFlags; }
            set { _socketFlags = value; }
        }

        public object? UserToken
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

        public void SetBuffer(byte[]? buffer, int offset, int count)
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
                        throw new ArgumentException(SR.net_ambiguousbuffers);
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
                    throw new ArgumentException(SR.net_ambiguousbuffers);
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
                SocketException? socketException = exception as SocketException;
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

        private static void ExecutionCallback(object? state)
        {
            var thisRef = (SocketAsyncEventArgs)state!;
            thisRef.OnCompletedInternal();
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
            throw status == Disposed ?
                new ObjectDisposedException(GetType().FullName) :
                new InvalidOperationException(SR.net_socketopinprogress);
        }

        // Prepares for a native async socket call.
        // This method performs the tasks common to all socket operations.
        internal void StartOperationCommon(Socket? socket, SocketAsyncOperation operation)
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
            // If Telemetry is enabled, make sure to capture the context if we're making a Connect or Accept call to preserve the activity
            if (_flowExecutionContext ||
                (SocketsTelemetry.Log.IsEnabled() && (operation == SocketAsyncOperation.Connect || operation == SocketAsyncOperation.Accept)))
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
            _acceptAddressBufferCount = 2 * (Socket.GetAddressSize(_currentSocket!._rightEndPoint!) + 16);

            // If our caller specified a buffer (willing to get received data with the Accept) then
            // it needs to be large enough for the two special sockaddr buffers that AcceptEx requires.
            // Throw if that buffer is not large enough.
            bool userSuppliedBuffer = !_buffer.Equals(default);
            if (userSuppliedBuffer)
            {
                // Caller specified a buffer - see if it is large enough
                if (_count < _acceptAddressBufferCount)
                {
                    throw new ArgumentException(SR.net_buffercounttoosmall, nameof(Count));
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

        internal void StartOperationConnect(bool saeaMultiConnectCancelable, bool userSocket)
        {
            _multipleConnectCancellation = saeaMultiConnectCancelable ? new CancellationTokenSource() : null;
            _connectSocket = null;
            _userSocket = userSocket;
        }

        internal void CancelConnectAsync()
        {
            if (_operating == InProgress && _completedOperation == SocketAsyncOperation.Connect)
            {
                CancellationTokenSource? multipleConnectCancellation = _multipleConnectCancellation;
                if (multipleConnectCancellation != null)
                {
                    // If a multiple connect is in progress, abort it.
                    multipleConnectCancellation.Cancel();
                }
                else
                {
                    // Otherwise we're doing a normal ConnectAsync - cancel it by closing the socket.
                    _currentSocket?.Dispose();
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
            Socket? currentSocket = _currentSocket;
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

            // Don't log transfered byte count in case of a failure.

            Complete();
        }

        internal void FinishOperationAsyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext? context = _context; // store context before it's cleared as part of finishing the operation

            FinishOperationSyncFailure(socketError, bytesTransferred, flags);

            if (context == null)
            {
                OnCompletedInternal();
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        /// <summary>Performs an asynchronous connect involving a DNS lookup.</summary>
        /// <param name="endPoint">The DNS end point to which to connect.</param>
        /// <param name="socketType">The SocketType to use to construct new sockets, if necessary.</param>
        /// <param name="protocolType">The ProtocolType to use to construct new sockets, if necessary.</param>
        /// <returns>true if the operation is pending; otherwise, false if it's already completed.</returns>
        internal bool DnsConnectAsync(DnsEndPoint endPoint, SocketType socketType, ProtocolType protocolType)
        {
            Debug.Assert(endPoint.AddressFamily == AddressFamily.Unspecified ||
                         endPoint.AddressFamily == AddressFamily.InterNetwork ||
                         endPoint.AddressFamily == AddressFamily.InterNetworkV6);

            CancellationToken cancellationToken = _multipleConnectCancellation?.Token ?? default;

            // In .NET 5 and earlier, the APM implementation allowed for synchronous exceptions from this to propagate
            // synchronously.  This call is made here rather than in the Core async method below to preserve that behavior.
            Task<IPAddress[]> addressesTask = Dns.GetHostAddressesAsync(endPoint.Host, endPoint.AddressFamily, cancellationToken);

            // Initialize the internal event args instance.  It needs to be initialized with `this` instance's buffer
            // so that it may be used as part of receives during a connect.
            // TODO https://github.com/dotnet/runtime/issues/30252#issuecomment-511231055: Try to avoid this extra level of SAEA.
            var internalArgs = new MultiConnectSocketAsyncEventArgs();
            internalArgs.CopyBufferFrom(this);

            // Delegate to the actual implementation.  The returned Task is unused and ignored, as the whole body is surrounded
            // by a try/catch.  Thus we ignore the result.  We avoid an "async void" method so as to skip the implicit SynchronizationContext
            // interactions async void methods entail.
            _ = Core(internalArgs, addressesTask, endPoint.Port, socketType, protocolType, cancellationToken);

            // Determine whether the async operation already completed and stored the results into `this`.
            // If we reached this point and the operation hasn't yet stored the results, then it's considered
            // pending.  If by the time we get here it has stored the results, it's considered completed.
            // The callback won't invoke the Completed event if it gets there first.
            return internalArgs.ReachedCoordinationPointFirst();

            async Task Core(MultiConnectSocketAsyncEventArgs internalArgs, Task<IPAddress[]> addressesTask, int port, SocketType socketType, ProtocolType protocolType, CancellationToken cancellationToken)
            {
                Socket? tempSocketIPv4 = null, tempSocketIPv6 = null;
                Exception? caughtException = null;
                try
                {
                    // Try each address in turn.  We store the last error received, such that if we fail to connect to all addresses,
                    // we can use the last error to represent the entire operation.
                    SocketError lastError = SocketError.NoData;
                    foreach (IPAddress address in await addressesTask.ConfigureAwait(false))
                    {
                        Socket? attemptSocket = null;
                        if (_currentSocket != null)
                        {
                            // If this SocketAsyncEventArgs was configured with a socket, then use it.
                            // If that instance doesn't support this address, move on to the next.
                            if (!_currentSocket.CanTryAddressFamily(address.AddressFamily))
                            {
                                continue;
                            }

                            attemptSocket = _currentSocket;
                        }
                        else
                        {
                            // If this SocketAsyncEventArgs doesn't have a socket, then we need to create a temporary one, which we do
                            // based on this address' address family (and then reuse for subsequent addresses for the same family).
                            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                attemptSocket = tempSocketIPv6 ??= (Socket.OSSupportsIPv6 ? new Socket(AddressFamily.InterNetworkV6, socketType, protocolType) : null);
                                if (attemptSocket is not null && address.IsIPv4MappedToIPv6)
                                {
                                    // We need a DualMode socket to connect to an IPv6-mapped IPv4 address.
                                    attemptSocket.DualMode = true;
                                }
                            }
                            else if (address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                attemptSocket = tempSocketIPv4 ??= (Socket.OSSupportsIPv4 ? new Socket(AddressFamily.InterNetwork, socketType, protocolType) : null);
                            }

                            // If we were unable to get a socket to use for this address, move on to the next address.
                            if (attemptSocket is null)
                            {
                                continue;
                            }
                        }

                        // Reset the socket if necessary to support another connect.  This is necessary on Unix in particular where
                        // the same socket handle can't be used for another connect, so we swap in a new handle under the covers if
                        // possible.  We do this not just for the 2nd+ address but also for the first in case the Socket was already
                        // used for a connection attempt outside of this call.
                        attemptSocket.ReplaceHandleIfNecessaryAfterFailedConnect();

                        // Reconfigure the internal event args for the new address.
                        if (internalArgs.RemoteEndPoint is IPEndPoint existing)
                        {
                            existing.Address = address;
                            Debug.Assert(existing.Port == port);
                        }
                        else
                        {
                            internalArgs.RemoteEndPoint = new IPEndPoint(address, port);
                        }

                        // Issue the connect.  If it pends, wait for it to complete.
                        if (attemptSocket.ConnectAsync(internalArgs))
                        {
                            using (cancellationToken.UnsafeRegister(s => Socket.CancelConnectAsync((SocketAsyncEventArgs)s!), internalArgs))
                            {
                                await new ValueTask(internalArgs, internalArgs.Version).ConfigureAwait(false);
                            }
                        }

                        // If it completed successfully, we're done; cleanup will be handled by the finally.
                        if (internalArgs.SocketError == SocketError.Success)
                        {
                            return;
                        }

                        // If the operation was canceled, simulate the appropriate SocketError.
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new SocketException((int)SocketError.OperationAborted);
                        }

                        lastError = internalArgs.SocketError;
                        internalArgs.Reset();
                    }

                    caughtException = new SocketException((int)lastError);
                }
                catch (ObjectDisposedException)
                {
                    // This can happen if the user closes the socket and is equivalent to a call to CancelConnectAsync.
                    caughtException = new SocketException((int)SocketError.OperationAborted);
                }
                catch (Exception exc)
                {
                    caughtException = exc;
                }
                finally
                {
                    // Close the sockets as needed.
                    if (tempSocketIPv4 != null && !tempSocketIPv4.Connected)
                    {
                        tempSocketIPv4.Dispose();
                    }
                    if (tempSocketIPv6 != null && !tempSocketIPv6.Connected)
                    {
                        tempSocketIPv6.Dispose();
                    }
                    if (_currentSocket != null)
                    {
                        // If the caller-provided socket was a temporary and isn't connected now, or if the failed with an abortive exception,
                        // dispose of the socket.
                        if ((!_userSocket && !_currentSocket.Connected) ||
                            caughtException is OperationCanceledException ||
                            (caughtException is SocketException se && se.SocketErrorCode == SocketError.OperationAborted))
                        {
                            _currentSocket.Dispose();
                        }
                    }

                    // Store the results.
                    if (caughtException != null)
                    {
                        SetResults(caughtException, 0, SocketFlags.None);
                        _currentSocket?.UpdateStatusAfterSocketError(_socketError);
                    }
                    else
                    {
                        SetResults(SocketError.Success, internalArgs.BytesTransferred, internalArgs.SocketFlags);
                        _connectSocket = _currentSocket = internalArgs.ConnectSocket!;
                    }

                    // Complete the operation.
                    if (SocketsTelemetry.Log.IsEnabled()) LogBytesTransferEvents(_connectSocket?.SocketType, SocketAsyncOperation.Connect, internalArgs.BytesTransferred);

                    Complete();

                    // Clean up after our temporary arguments.
                    internalArgs.Dispose();

                    // If the caller is treating this operation as pending, own the completion.
                    if (!internalArgs.ReachedCoordinationPointFirst())
                    {
                        // Regardless of _flowExecutionContext, context will have been flown through this async method, as that's part
                        // of what async methods do.  As such, we're already on whatever ExecutionContext is the right one to invoke
                        // the completion callback.  This method may have even mutated the ExecutionContext, in which case for telemetry
                        // we need those mutations to be surfaced as part of this callback, so that logging performed here sees those
                        // mutations (e.g. to the current Activity).
                        OnCompleted(this);
                    }
                }
            }
        }

        private sealed class MultiConnectSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource
        {
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            private int _isCompleted;

            public MultiConnectSocketAsyncEventArgs() : base(unsafeSuppressExecutionContextFlow: false) { }

            public void GetResult(short token) => _mrvtsc.GetResult(token);
            public ValueTaskSourceStatus GetStatus(short token) => _mrvtsc.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _mrvtsc.OnCompleted(continuation, state, token, flags);

            public short Version => _mrvtsc.Version;
            public void Reset() => _mrvtsc.Reset();

            protected override void OnCompleted(SocketAsyncEventArgs e) => _mrvtsc.SetResult(true);

            public bool ReachedCoordinationPointFirst() => Interlocked.Exchange(ref _isCompleted, 1) == 0;
        }

        internal void FinishOperationSyncSuccess(int bytesTransferred, SocketFlags flags)
        {
            SetResults(SocketError.Success, bytesTransferred, flags);

            if (NetEventSource.Log.IsEnabled() && bytesTransferred > 0)
            {
                LogBuffer(bytesTransferred);
            }

            SocketError socketError;
            switch (_completedOperation)
            {
                case SocketAsyncOperation.Accept:
                    // Get the endpoint.
                    Internals.SocketAddress remoteSocketAddress = IPEndPointExtensions.Serialize(_currentSocket!._rightEndPoint!);

                    socketError = FinishOperationAccept(remoteSocketAddress);

                    if (socketError == SocketError.Success)
                    {
                        _acceptSocket = _currentSocket.UpdateAcceptSocket(_acceptSocket!, _currentSocket._rightEndPoint!.Create(remoteSocketAddress));

                        if (NetEventSource.Log.IsEnabled())
                        {
                            try
                            {
                                NetEventSource.Accepted(_acceptSocket, _acceptSocket.RemoteEndPoint, _acceptSocket.LocalEndPoint);
                            }
                            catch (ObjectDisposedException) { }
                        }
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
                        if (NetEventSource.Log.IsEnabled())
                        {
                            try
                            {
                                NetEventSource.Connected(_currentSocket!, _currentSocket!.LocalEndPoint, _currentSocket.RemoteEndPoint);
                            }
                            catch (ObjectDisposedException) { }
                        }

                        // Mark socket connected.
                        _currentSocket!.SetToConnected();
                        _connectSocket = _currentSocket;
                    }
                    else
                    {
                        SetResults(socketError, bytesTransferred, flags);
                        _currentSocket!.UpdateStatusAfterSocketError(socketError);
                    }
                    break;

                case SocketAsyncOperation.Disconnect:
                    _currentSocket!.SetToDisconnected();
                    _currentSocket._remoteEndPoint = null;
                    break;

                case SocketAsyncOperation.ReceiveFrom:
                    // Deal with incoming address.
                    _socketAddress!.InternalSize = GetSocketAddressSize();
                    Internals.SocketAddress socketAddressOriginal = IPEndPointExtensions.Serialize(_remoteEndPoint!);
                    if (!socketAddressOriginal.Equals(_socketAddress))
                    {
                        try
                        {
                            _remoteEndPoint = _remoteEndPoint!.Create(_socketAddress);
                        }
                        catch
                        {
                        }
                    }
                    break;

                case SocketAsyncOperation.ReceiveMessageFrom:
                    // Deal with incoming address.
                    _socketAddress!.InternalSize = GetSocketAddressSize();
                    socketAddressOriginal = IPEndPointExtensions.Serialize(_remoteEndPoint!);
                    if (!socketAddressOriginal.Equals(_socketAddress))
                    {
                        try
                        {
                            _remoteEndPoint = _remoteEndPoint!.Create(_socketAddress);
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

            if (SocketsTelemetry.Log.IsEnabled()) LogBytesTransferEvents(_currentSocket?.SocketType, _completedOperation, bytesTransferred);

            Complete();
        }

        internal void FinishOperationAsyncSuccess(int bytesTransferred, SocketFlags flags)
        {
            ExecutionContext? context = _context; // store context before it's cleared as part of finishing the operation

            FinishOperationSyncSuccess(bytesTransferred, flags);

            // Raise completion event.
            if (context == null)
            {
                OnCompletedInternal();
            }
            else
            {
                ExecutionContext.Run(context, s_executionCallback, this);
            }
        }

        private void FinishOperationSync(SocketError socketError, int bytesTransferred, SocketFlags flags)
        {
            Debug.Assert(socketError != SocketError.IOPending);

            if (socketError == SocketError.Success)
            {
                FinishOperationSyncSuccess(bytesTransferred, flags);
            }
            else
            {
                FinishOperationSyncFailure(socketError, bytesTransferred, flags);
            }

            // The following check checks if the operation was Accept (1) or Connect (2)
            if (LastOperation <= SocketAsyncOperation.Connect)
            {
                AfterConnectAcceptTelemetry();
            }
        }

        private static void LogBytesTransferEvents(SocketType? socketType, SocketAsyncOperation operation, int bytesTransferred)
        {
            switch (operation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.ReceiveMessageFrom:
                case SocketAsyncOperation.Accept:
                    SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                    if (socketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
                    break;
                case SocketAsyncOperation.Send:
                case SocketAsyncOperation.SendTo:
                case SocketAsyncOperation.SendPackets:
                case SocketAsyncOperation.Connect:
                    SocketsTelemetry.Log.BytesSent(bytesTransferred);
                    if (socketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramSent();
                    break;
            }
        }
    }
}
