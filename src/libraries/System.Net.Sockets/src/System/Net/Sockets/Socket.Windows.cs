// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        private static CachedSerializedEndPoint? s_cachedAnyEndPoint;
        private static CachedSerializedEndPoint? s_cachedAnyV6EndPoint;
        private static CachedSerializedEndPoint? s_cachedMappedAnyV6EndPoint;
        private DynamicWinsockMethods? _dynamicWinsockMethods;

        internal void ReplaceHandleIfNecessaryAfterFailedConnect() { /* nop on Windows */ }

        private sealed class CachedSerializedEndPoint
        {
            public readonly IPEndPoint IPEndPoint;
            public readonly Internals.SocketAddress SocketAddress;

            public CachedSerializedEndPoint(IPAddress address)
            {
                IPEndPoint = new IPEndPoint(address, 0);
                SocketAddress = IPEndPointExtensions.Serialize(IPEndPoint);
            }
        }

        [SupportedOSPlatform("windows")]
        public Socket(SocketInformation socketInformation)
        {
            SocketError errorCode = SocketPal.CreateSocket(socketInformation, out _handle,
                ref _addressFamily, ref _socketType, ref _protocolType);

            if (errorCode != SocketError.Success)
            {
                Debug.Assert(_handle.IsInvalid);
                _handle = null!;

                if (errorCode == SocketError.InvalidArgument)
                {
                    throw new ArgumentException(SR.net_sockets_invalid_socketinformation, nameof(socketInformation));
                }

                // Failed to create the socket, throw.
                throw new SocketException((int)errorCode);
            }

            if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
            {
                _handle.Dispose();
                _handle = null!;
                throw new NotSupportedException(SR.net_invalidversion);
            }

            _isConnected = socketInformation.GetOption(SocketInformationOptions.Connected);
            _willBlock = !socketInformation.GetOption(SocketInformationOptions.NonBlocking);
            InternalSetBlocking(_willBlock);
            _isListening = socketInformation.GetOption(SocketInformationOptions.Listening);

            IPAddress tempAddress = _addressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
            IPEndPoint ep = new IPEndPoint(tempAddress, 0);

            Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(ep);
            unsafe
            {
                fixed (byte* bufferPtr = socketAddress.Buffer)
                fixed (int* sizePtr = &socketAddress.InternalSize)
                {
                    errorCode = SocketPal.GetSockName(_handle, bufferPtr, sizePtr);
                }
            }

            if (errorCode == SocketError.Success)
            {
                _rightEndPoint = ep.Create(socketAddress);
            }
            else if (errorCode == SocketError.InvalidArgument)
            {
                // Socket is not yet bound.
            }
            else
            {
                _handle.Dispose();
                _handle = null!;
                throw new SocketException((int)errorCode);
            }
        }

        private unsafe void LoadSocketTypeFromHandle(
            SafeSocketHandle handle, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType, out bool blocking, out bool isListening)
        {
            // This can be called without winsock initialized. The handle is not going to be a valid socket handle in that case and the code will throw exception anyway.
            // Initializing winsock will ensure the error SocketError.NotSocket as opposed to SocketError.NotInitialized.
            Interop.Winsock.EnsureInitialized();

            Interop.Winsock.WSAPROTOCOL_INFOW info = default;
            int optionLength = sizeof(Interop.Winsock.WSAPROTOCOL_INFOW);

            // Get the address family, socket type, and protocol type from the socket.
            if (Interop.Winsock.getsockopt(handle, SocketOptionLevel.Socket, (SocketOptionName)Interop.Winsock.SO_PROTOCOL_INFOW, (byte*)&info, ref optionLength) == SocketError.SocketError)
            {
                throw new SocketException((int)SocketPal.GetLastSocketError());
            }

            addressFamily = info.iAddressFamily;
            socketType = info.iSocketType;
            protocolType = info.iProtocol;

            isListening =
                SocketPal.GetSockOpt(_handle, SocketOptionLevel.Socket, SocketOptionName.AcceptConnection, out int isListeningValue) == SocketError.Success &&
                isListeningValue != 0;

            // There's no API to retrieve this (WSAIsBlocking isn't supported any more).  Assume it's blocking, but we might be wrong.
            // This affects the result of querying Socket.Blocking, which will mostly only affect user code that happens to query
            // that property, though there are a few places we check it internally, e.g. as part of NetworkStream argument validation.
            blocking = true;
        }

        [SupportedOSPlatform("windows")]
        public SocketInformation DuplicateAndClose(int targetProcessId)
        {
            ThrowIfDisposed();

            SocketError errorCode = SocketPal.DuplicateSocket(_handle, targetProcessId, out SocketInformation info);

            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }

            info.SetOption(SocketInformationOptions.Connected, Connected);
            info.SetOption(SocketInformationOptions.NonBlocking, !Blocking);
            info.SetOption(SocketInformationOptions.Listening, _isListening);

            Close(timeout: -1);

            return info;
        }

        public IAsyncResult BeginAccept(int receiveSize, AsyncCallback? callback, object? state)
        {
            return BeginAccept(acceptSocket: null, receiveSize, callback, state);
        }

        // This is the truly async version that uses AcceptEx.
        public IAsyncResult BeginAccept(Socket? acceptSocket, int receiveSize, AsyncCallback? callback, object? state)
        {
            return BeginAcceptCommon(acceptSocket, receiveSize, callback, state);
        }

        public Socket EndAccept(out byte[] buffer, IAsyncResult asyncResult)
        {
            Socket socket = EndAccept(out byte[] innerBuffer, out int bytesTransferred, asyncResult);
            buffer = new byte[bytesTransferred];
            Buffer.BlockCopy(innerBuffer, 0, buffer, 0, bytesTransferred);
            return socket;
        }

        public Socket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult)
        {
            return EndAcceptCommon(out buffer!, out bytesTransferred, asyncResult);
        }

        private DynamicWinsockMethods GetDynamicWinsockMethods()
        {
            return _dynamicWinsockMethods ??= DynamicWinsockMethods.GetMethods(_addressFamily, _socketType, _protocolType);
        }

        internal unsafe bool AcceptEx(SafeSocketHandle listenSocketHandle,
            SafeSocketHandle acceptSocketHandle,
            IntPtr buffer,
            int len,
            int localAddressLength,
            int remoteAddressLength,
            out int bytesReceived,
            NativeOverlapped* overlapped)
        {
            AcceptExDelegate acceptEx = GetDynamicWinsockMethods().GetAcceptExDelegate(listenSocketHandle);

            return acceptEx(listenSocketHandle,
                acceptSocketHandle,
                buffer,
                len,
                localAddressLength,
                remoteAddressLength,
                out bytesReceived,
                overlapped);
        }

        internal void GetAcceptExSockaddrs(IntPtr buffer,
            int receiveDataLength,
            int localAddressLength,
            int remoteAddressLength,
            out IntPtr localSocketAddress,
            out int localSocketAddressLength,
            out IntPtr remoteSocketAddress,
            out int remoteSocketAddressLength)
        {
            GetAcceptExSockaddrsDelegate getAcceptExSockaddrs = GetDynamicWinsockMethods().GetGetAcceptExSockaddrsDelegate(_handle);

            getAcceptExSockaddrs(buffer,
                receiveDataLength,
                localAddressLength,
                remoteAddressLength,
                out localSocketAddress,
                out localSocketAddressLength,
                out remoteSocketAddress,
                out remoteSocketAddressLength);
        }

        internal unsafe bool DisconnectEx(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved)
        {
            DisconnectExDelegate disconnectEx = GetDynamicWinsockMethods().GetDisconnectExDelegate(socketHandle);

            return disconnectEx(socketHandle, overlapped, flags, reserved);
        }

        internal unsafe bool DisconnectExBlocking(SafeSocketHandle socketHandle, int flags, int reserved)
        {
            DisconnectExDelegate disconnectEx = GetDynamicWinsockMethods().GetDisconnectExDelegate(socketHandle);

            return disconnectEx(socketHandle, null, flags, reserved);
        }

        partial void WildcardBindForConnectIfNecessary(AddressFamily addressFamily)
        {
            if (_rightEndPoint != null)
            {
                return;
            }

            // The socket must be bound before using ConnectEx.

            CachedSerializedEndPoint csep;
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    csep = IsDualMode ?
                        s_cachedMappedAnyV6EndPoint ??= new CachedSerializedEndPoint(s_IPAddressAnyMapToIPv6) :
                        s_cachedAnyEndPoint ??= new CachedSerializedEndPoint(IPAddress.Any);
                    break;

                case AddressFamily.InterNetworkV6:
                    csep = s_cachedAnyV6EndPoint ??= new CachedSerializedEndPoint(IPAddress.IPv6Any);
                    break;

                default:
                    return;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, csep.IPEndPoint);

            DoBind(csep.IPEndPoint, csep.SocketAddress);
        }

        internal unsafe bool ConnectEx(SafeSocketHandle socketHandle,
            IntPtr socketAddress,
            int socketAddressSize,
            IntPtr buffer,
            int dataLength,
            out int bytesSent,
            NativeOverlapped* overlapped)
        {
            ConnectExDelegate connectEx = GetDynamicWinsockMethods().GetConnectExDelegate(socketHandle);

            return connectEx(socketHandle, socketAddress, socketAddressSize, buffer, dataLength, out bytesSent, overlapped);
        }

        internal unsafe SocketError WSARecvMsg(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred, NativeOverlapped* overlapped, IntPtr completionRoutine)
        {
            WSARecvMsgDelegate recvMsg = GetDynamicWinsockMethods().GetWSARecvMsgDelegate(socketHandle);

            return recvMsg(socketHandle, msg, out bytesTransferred, overlapped, completionRoutine);
        }

        internal unsafe SocketError WSARecvMsgBlocking(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred)
        {
            WSARecvMsgDelegate recvMsg = GetDynamicWinsockMethods().GetWSARecvMsgDelegate(_handle);

            return recvMsg(socketHandle, msg, out bytesTransferred, null, IntPtr.Zero);
        }

        internal unsafe bool TransmitPackets(SafeSocketHandle socketHandle, IntPtr packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags)
        {
            TransmitPacketsDelegate transmitPackets = GetDynamicWinsockMethods().GetTransmitPacketsDelegate(socketHandle);

            return transmitPackets(socketHandle, packetArray, elementCount, sendSize, overlapped, flags);
        }

        internal static void SocketListToFileDescriptorSet(IList? socketList, Span<IntPtr> fileDescriptorSet, ref int refsAdded)
        {
            int count;
            if (socketList == null || (count = socketList.Count) == 0)
            {
                return;
            }

            Debug.Assert(fileDescriptorSet.Length >= count + 1);

            fileDescriptorSet[0] = (IntPtr)count;
            for (int current = 0; current < count; current++)
            {
                if (!(socketList[current] is Socket socket))
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_select, socketList[current]?.GetType().FullName, typeof(System.Net.Sockets.Socket).FullName), nameof(socketList));
                }

                bool success = false;
                socket.InternalSafeHandle.DangerousAddRef(ref success);
                fileDescriptorSet[current + 1] = socket.InternalSafeHandle.DangerousGetHandle();
                refsAdded++;
            }
        }

        // Transform the list socketList such that the only sockets left are those
        // with a file descriptor contained in the array "fileDescriptorArray".
        internal static void SelectFileDescriptor(IList? socketList, Span<IntPtr> fileDescriptorSet, ref int refsAdded)
        {
            // Walk the list in order.
            //
            // Note that the counter is not necessarily incremented at each step;
            // when the socket is removed, advancing occurs automatically as the
            // other elements are shifted down.
            int count;
            if (socketList == null || (count = socketList.Count) == 0)
            {
                return;
            }

            Debug.Assert(fileDescriptorSet.Length >= count + 1);

            int returnedCount = (int)fileDescriptorSet[0];
            if (returnedCount == 0)
            {
                // Unref safehandles.
                SocketListDangerousReleaseRefs(socketList, ref refsAdded);

                // No socket present, will never find any socket, remove them all.
                socketList.Clear();
                return;
            }

            lock (socketList)
            {
                for (int currentSocket = 0; currentSocket < count; currentSocket++)
                {
                    Socket? socket = socketList[currentSocket] as Socket;
                    Debug.Assert(socket != null);

                    // Look for the file descriptor in the array.
                    int currentFileDescriptor;
                    for (currentFileDescriptor = 0; currentFileDescriptor < returnedCount; currentFileDescriptor++)
                    {
                        if (fileDescriptorSet[currentFileDescriptor + 1] == socket._handle.DangerousGetHandle())
                        {
                            break;
                        }
                    }

                    if (currentFileDescriptor == returnedCount)
                    {
                        // Descriptor not found: remove the current socket and start again.
                        socket.InternalSafeHandle.DangerousRelease();
                        refsAdded--;
                        socketList.RemoveAt(currentSocket--);
                        count--;
                    }
                }
            }
        }

        private Socket GetOrCreateAcceptSocket(Socket? acceptSocket, bool checkDisconnected, string propertyName, out SafeSocketHandle handle)
        {
            // If an acceptSocket isn't specified, then we need to create one.
            if (acceptSocket == null)
            {
                acceptSocket = new Socket(_addressFamily, _socketType, _protocolType);
            }
            else if (acceptSocket._rightEndPoint != null && (!checkDisconnected || !acceptSocket._isDisconnected))
            {
                throw new InvalidOperationException(SR.Format(SR.net_sockets_namedmustnotbebound, propertyName));
            }

            handle = acceptSocket._handle;
            return acceptSocket;
        }

        private void SendFileInternal(string? fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
        {
            // Open the file, if any
            FileStream? fileStream = OpenFile(fileName);

            SocketError errorCode;
            using (fileStream)
            {
                SafeFileHandle? fileHandle = fileStream?.SafeFileHandle;

                // This can throw ObjectDisposedException.
                errorCode = SocketPal.SendFile(_handle, fileHandle, preBuffer, postBuffer, flags);
            }

            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            // If the user passed the Disconnect and/or ReuseSocket flags, then TransmitFile disconnected the socket.
            // Update our state to reflect this.
            if ((flags & (TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket)) != 0)
            {
                SetToDisconnected();
                _remoteEndPoint = null;
            }
        }

        private ValueTask SendFileInternalAsync(FileStream? fileStream, ReadOnlyMemory<byte> preBuffer, ReadOnlyMemory<byte> postBuffer, TransmitFileOptions flags = TransmitFileOptions.UseDefaultWorkerThread, CancellationToken cancellationToken = default)
        {
            FileSendSocketAsyncEventargs saea =
                Interlocked.Exchange(ref _fileSendEventArgs, null) ??
                new FileSendSocketAsyncEventargs(this);

            Debug.Assert(saea.Buffer == null);
            Debug.Assert(saea.BufferList == null);
            saea.Configure(fileStream, preBuffer, postBuffer, flags);
            saea.SocketFlags = SocketFlags.None;
            return saea.SendFileAsync(this, cancellationToken);
        }

        private bool SendFileAsync(FileSendSocketAsyncEventargs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.SendFile);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationSendFile(_handle, cancellationToken);
            }
            catch
            {
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        private IAsyncResult BeginSendFileInternal(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(SendFileAsync(fileName, preBuffer, postBuffer, flags).AsTask(), callback, state);
        }

        private void EndSendFileInternal(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle() =>
            _handle.GetThreadPoolBoundHandle() ??
            GetOrAllocateThreadPoolBoundHandleSlow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandleSlow()
        {
            // There is a known bug that exists through Windows 7 with UDP and SetFileCompletionNotificationModes.
            // So, don't try to enable skipping the completion port on success in this case.
            bool trySkipCompletionPortOnSuccess = !(CompletionPortHelper.PlatformHasUdpIssue && _protocolType == ProtocolType.Udp);
            return _handle.GetOrAllocateThreadPoolBoundHandle(trySkipCompletionPortOnSuccess);
        }

        internal sealed class FileSendSocketAsyncEventargs : SocketAsyncEventArgs, IValueTaskSource
        {
            private readonly Socket _owner;
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;

            /// <summary>The cancellation token used for the current operation.</summary>
            private CancellationToken _cancellationToken;

            private ReadOnlyMemory<byte> _preBuffer;
            private ReadOnlyMemory<byte> _postBuffer;
            private TransmitFileOptions _transmitFileOptions;

            public FileSendSocketAsyncEventargs(Socket owner) :
                base(unsafeSuppressExecutionContextFlow: true) // avoid flowing context at lower layers as we only expose ValueTask, which handles it
            {
                _owner = owner;
            }

            public void Configure(FileStream? fileStream, ReadOnlyMemory<byte> preBuffer, ReadOnlyMemory<byte> postBuffer, TransmitFileOptions flags)
            {
                StartConfiguring();
                try
                {
                    _sendFileFileStream = fileStream;
                    _preBuffer = preBuffer;
                    _postBuffer = postBuffer;
                    _transmitFileOptions = flags;
                }
                finally
                {
                    Complete();
                }
            }

            public ValueTask SendFileAsync(Socket socket, CancellationToken cancellationToken)
            {
                if (socket.SendFileAsync(this, cancellationToken))
                {
                    _cancellationToken = cancellationToken;
                    return new ValueTask(this, _mrvtsc.Version);
                }

                SocketError error = SocketError;

                Release();

                return error == SocketError.Success
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(CreateException(error));
            }

            public unsafe SocketError DoOperationSendFile(SafeSocketHandle handle, CancellationToken cancellationToken)
            {
                fixed (byte* preBufferPtr = &MemoryMarshal.GetReference(_preBuffer.Span))
                fixed (byte* postBufferPtr = &MemoryMarshal.GetReference(_postBuffer.Span))
                {
                    NativeOverlapped* overlapped = AllocateNativeOverlapped();
                    try
                    {
                        Debug.Assert(_singleBufferHandleState == SingleBufferHandleState.None);
                        _singleBufferHandleState = SingleBufferHandleState.InProcess;

                        SocketError socketError = SocketPal.SendFileAsync(
                            handle,
                            _sendFileFileStream,
                            overlapped,
                            (IntPtr)preBufferPtr, _preBuffer.Length,
                            (IntPtr)postBufferPtr, _postBuffer.Length,
                            _transmitFileOptions);

                        return ProcessIOFileSendResult(socketError, overlapped, cancellationToken);
                    }
                    catch
                    {
                        _singleBufferHandleState = SingleBufferHandleState.None;
                        FreeNativeOverlapped(overlapped);
                        throw;
                    }
                }
            }

            public void GetResult(short token)
            {
                _mrvtsc.GetResult(token);

                SocketError error = SocketError;
                CancellationToken cancellationToken = _cancellationToken;

                Release();

                if (error != SocketError.Success)
                {
                    ThrowException(error, cancellationToken);
                }
            }

            public ValueTaskSourceStatus GetStatus(short token) => _mrvtsc.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _mrvtsc.OnCompleted(continuation, state, token, flags);

            protected override void OnCompleted(SocketAsyncEventArgs e) => _mrvtsc.SetResult(true);

            private unsafe SocketError ProcessIOFileSendResult(SocketError socketError, NativeOverlapped* overlapped, CancellationToken cancellationToken)
            {
                // Note: We need to dispose of the overlapped iff the operation completed synchronously,
                // and if we do, we must do so before we mark the operation as completed.

                if (socketError == SocketError.Success)
                {
                    // Synchronous success.
                    if (_currentSocket!.SafeHandle.SkipCompletionPortOnSuccess)
                    {
                        // The socket handle is configured to skip completion on success,
                        // so we can set the results right now.
                        _singleBufferHandleState = SingleBufferHandleState.None;
                        FreeNativeOverlapped(overlapped);
                        FinishOperationSyncSuccess(0, SocketFlags.None);

                        return SocketError.Success;
                    }

                    // Completed synchronously, but the handle wasn't marked as skip completion port on success,
                    // so we still need to fall through and behave as if the IO was pending.
                }
                else
                {
                    // Get the socket error (which may be IOPending)
                    socketError = SocketPal.GetLastSocketError();
                    if (socketError != SocketError.IOPending)
                    {
                        // Completed synchronously with a failure.
                        _singleBufferHandleState = SingleBufferHandleState.None;
                        FreeNativeOverlapped(overlapped);
                        FinishOperationSyncFailure(socketError, 0, SocketFlags.None);

                        return socketError;
                    }

                    // Fall through to IOPending handling for asynchronous completion.
                }

                // Socket handle is going to post a completion to the completion port (may have done so already).
                // Return pending and we will continue in the completion port callback.
                if (_singleBufferHandleState == SingleBufferHandleState.InProcess)
                {
                    RegisterToCancelPendingIO(overlapped, cancellationToken); // must happen before we change state to Set to avoid race conditions
                    _preBufferHandle = _preBuffer.Pin();
                    _postBufferHandle = _postBuffer.Pin();
                    _singleBufferHandleState = SingleBufferHandleState.Set;
                }

                return SocketError.IOPending;
            }

            private void Release()
            {
                _cancellationToken = default;
                _mrvtsc.Reset();

                if (_sendFileFileStream != null)
                {
                    _sendFileFileStream.Dispose();
                    _sendFileFileStream = null;
                }

                ref FileSendSocketAsyncEventargs? cache = ref _owner._fileSendEventArgs;
                if (Interlocked.CompareExchange(ref cache, this, null) != null)
                {
                    Dispose();
                }
            }

            private static void ThrowException(SocketError error, CancellationToken cancellationToken)
            {
                if (error == SocketError.OperationAborted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw CreateException(error);
            }

            private static Exception CreateException(SocketError error) => new SocketException((int)error);
        }
    }
}
