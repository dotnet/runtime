// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Net.WebSockets
{
    internal class WinHttpWebSocket : WebSocket
    {
        // TODO (Issue 2503): move System.Net.* strings to resources as appropriate.

        // NOTE: All WinHTTP operations must be called while holding the _operation.Lock.
        // It is critical that no handle gets closed while a WinHTTP function is running.
        private WebSocketCloseStatus? _closeStatus = null;
        private string _closeStatusDescription = null;
        private bool _disposed = false;

        private WinHttpWebSocketState _operation = new WinHttpWebSocketState();

        // TODO (Issue 2505): temporary pinned buffer caches of 1 item. Will be replaced by PinnableBufferCache.
        private GCHandle _cachedSendPinnedBuffer;
        private GCHandle _cachedReceivePinnedBuffer;

        public WinHttpWebSocket()
        {
        }

        #region Properties
        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                return _closeStatus;
            }
        }

        public override string CloseStatusDescription
        {
            get
            {
                return _closeStatusDescription;
            }
        }

        public override WebSocketState State
        {
            get
            {
                return _operation.State;
            }
        }

        public override string SubProtocol
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage("This functionality is not yet implemented.");
            }
        }
        #endregion

        readonly static WebSocketState[] s_validConnectStates = { WebSocketState.None };
        readonly static WebSocketState[] s_validConnectingStates = { WebSocketState.Connecting };
        
        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            _operation.InterlockedCheckAndUpdateState(WebSocketState.Connecting, s_validConnectStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                lock (_operation.Lock)
                {
                    _operation.CheckValidState(s_validConnectingStates);

                    // Must grab lock until RequestHandle is populated, otherwise we risk resource leaks on Abort.
                    //
                    // TODO (Issue 2506): Alternatively, release the lock between WinHTTP operations and check, under lock, that the 
                    // state is still valid to continue operation.
                    _operation.SessionHandle = InitializeWinHttp(options);

                    _operation.ConnectionHandle = Interop.WinHttp.WinHttpConnectWithCallback(
                        _operation.SessionHandle,
                        uri.IdnHost,
                        (ushort)uri.Port,
                        0);

                    ThrowOnInvalidHandle(_operation.ConnectionHandle);

                    bool secureConnection = uri.Scheme == UriScheme.Https || uri.Scheme == UriScheme.Wss;

                    _operation.RequestHandle = Interop.WinHttp.WinHttpOpenRequestWithCallback(
                        _operation.ConnectionHandle,
                        "GET",
                        uri.PathAndQuery,
                        null,
                        Interop.WinHttp.WINHTTP_NO_REFERER,
                        null,
                        secureConnection ? Interop.WinHttp.WINHTTP_FLAG_SECURE : 0);

                    ThrowOnInvalidHandle(_operation.RequestHandle);

                    if (!Interop.WinHttp.WinHttpSetOption(
                        _operation.RequestHandle,
                        Interop.WinHttp.WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET,
                        IntPtr.Zero,
                        0))
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    // We need the address of the IntPtr to the GCHandle.
                    IntPtr context = _operation.ToIntPtr();
                    IntPtr contextAddress;
                    unsafe
                    {
                        contextAddress = (IntPtr)(void*)&context;
                    }

                    if (!Interop.WinHttp.WinHttpSetOption(
                        _operation.RequestHandle,
                        Interop.WinHttp.WINHTTP_OPTION_CONTEXT_VALUE,
                        contextAddress,
                        (uint)IntPtr.Size))
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    if (Interop.WinHttp.WinHttpSetStatusCallback(
                        _operation.RequestHandle,
                        WinHttpWebSocketCallback.s_StaticCallbackDelegate,
                        Interop.WinHttp.WINHTTP_CALLBACK_FLAG_ALL_NOTIFICATIONS,
                        IntPtr.Zero) == (IntPtr)Interop.WinHttp.WINHTTP_INVALID_STATUS_CALLBACK)
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    _operation.RequestHandle.AttachCallback();

                    _operation.TcsUpgrade = new TaskCompletionSource<bool>();
                }

                await InternalSendWsUpgradeRequestAsync().ConfigureAwait(false);

                await InternalReceiveWsUpgradeResponse().ConfigureAwait(false);

                lock (_operation.Lock)
                {
                    ThrowOnInvalidConnectState();

                    _operation.WebSocketHandle =
                        Interop.WinHttp.WinHttpWebSocketCompleteUpgrade(_operation.RequestHandle, IntPtr.Zero);

                    ThrowOnInvalidHandle(_operation.WebSocketHandle);

                    // We need the address of the IntPtr to the GCHandle.
                    IntPtr context = _operation.ToIntPtr();
                    IntPtr contextAddress;
                    unsafe
                    {
                        contextAddress = (IntPtr)(void*)&context;
                    }

                    if (!Interop.WinHttp.WinHttpSetOption(
                        _operation.WebSocketHandle,
                        Interop.WinHttp.WINHTTP_OPTION_CONTEXT_VALUE,
                        contextAddress,
                        (uint)IntPtr.Size))
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    _operation.WebSocketHandle.AttachCallback();
                    _operation.UpdateState(WebSocketState.Open);

                    if (_operation.RequestHandle != null)
                    {
                        _operation.RequestHandle.Dispose();
                        // RequestHandle will be set to null in the callback.
                    }
                    _operation.TcsUpgrade = null;

                    ctr.Dispose();
                }
            }
        }

        // Requires lock taken.
        private Interop.WinHttp.SafeWinHttpHandle InitializeWinHttp(ClientWebSocketOptions options)
        {
            Interop.WinHttp.SafeWinHttpHandle sessionHandle;
            sessionHandle = Interop.WinHttp.WinHttpOpen(
                           IntPtr.Zero,
                           Interop.WinHttp.WINHTTP_ACCESS_TYPE_NO_PROXY,
                           null,
                           null,
                           (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);

            ThrowOnInvalidHandle(sessionHandle);

            uint optionAssuredNonBlockingTrue = 1; // TRUE

            if (!Interop.WinHttp.WinHttpSetOption(
                sessionHandle,
                Interop.WinHttp.WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS,
                ref optionAssuredNonBlockingTrue,
                (uint)Marshal.SizeOf<uint>()))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }

            return sessionHandle;
        }

        private Task<bool> InternalSendWsUpgradeRequestAsync()
        {
            lock (_operation.Lock)
            {
                ThrowOnInvalidConnectState();
                if (!Interop.WinHttp.WinHttpSendRequest(
                                    _operation.RequestHandle,
                                    Interop.WinHttp.WINHTTP_NO_ADDITIONAL_HEADERS,
                                    0,
                                    IntPtr.Zero,
                                    0,
                                    0,
                                    _operation.ToIntPtr()))
                {
                    WinHttpException.ThrowExceptionUsingLastError();
                }
            }

            return _operation.TcsUpgrade.Task;
        }

        private Task<bool> InternalReceiveWsUpgradeResponse()
        {
            // TODO (Issue 2507): Potential optimization: move this in WinHttpWebSocketCallback.
            lock (_operation.Lock)
            {
                ThrowOnInvalidConnectState();

                _operation.TcsUpgrade = new TaskCompletionSource<bool>();

                if (!Interop.WinHttp.WinHttpReceiveResponse(_operation.RequestHandle, IntPtr.Zero))
                {
                    WinHttpException.ThrowExceptionUsingLastError();
                }
            }

            return _operation.TcsUpgrade.Task;
        }

        private void ThrowOnInvalidConnectState()
        {
            // A special behavior for WebSocket upgrade: throws ConnectFailure instead of other Abort messages.
            if (_operation.State != WebSocketState.Connecting)
            {
                throw new WebSocketException(SR.net_webstatus_ConnectFailure);
            }
        }

        private readonly static WebSocketState[] s_validSendStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            _operation.InterlockedCheckValidStates(s_validSendStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                var bufferType = WebSocketMessageTypeAdapter.GetWinHttpMessageType(messageType, endOfMessage);

                // TODO (Issue 2505): replace with PinnableBufferCache.
                if (!_cachedSendPinnedBuffer.IsAllocated || _cachedSendPinnedBuffer.Target != buffer.Array)
                {
                    if (_cachedSendPinnedBuffer.IsAllocated)
                    {
                        _cachedSendPinnedBuffer.Free();
                    }

                    _cachedSendPinnedBuffer = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                }

                bool sendOperationAlreadyPending = false;
                if (_operation.PendingWriteOperation == false)
                {
                    lock (_operation.Lock)
                    {
                        _operation.CheckValidState(s_validSendStates);

                        if (_operation.PendingWriteOperation == false)
                        {
                            _operation.PendingWriteOperation = true;
                            _operation.TcsSend = new TaskCompletionSource<bool>();

                            uint ret = Interop.WinHttp.WinHttpWebSocketSend(
                                _operation.WebSocketHandle,
                                bufferType,
                                Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset),
                                (uint)buffer.Count);

                            if (Interop.WinHttp.ERROR_SUCCESS != ret)
                            {
                                throw WinHttpException.CreateExceptionUsingError((int)ret);
                            }
                        }
                        else
                        {
                            sendOperationAlreadyPending = true;
                        }
                    }
                }
                else
                {
                    sendOperationAlreadyPending = true;
                }

                if (sendOperationAlreadyPending)
                {
                    var exception = new InvalidOperationException(
                        SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, "SendAsync"));

                    _operation.TcsSend.TrySetException(exception);
                    Abort();
                }

                return _operation.TcsSend.Task;
            }
        }

        private readonly static WebSocketState[] s_validReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent };
        private readonly static WebSocketState[] s_validAfterReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent, WebSocketState.CloseReceived };
        public override async Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            _operation.InterlockedCheckValidStates(s_validReceiveStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                // TODO (Issue 2505): replace with PinnableBufferCache.
                if (!_cachedReceivePinnedBuffer.IsAllocated || _cachedReceivePinnedBuffer.Target != buffer.Array)
                {
                    if (_cachedReceivePinnedBuffer.IsAllocated)
                    {
                        _cachedReceivePinnedBuffer.Free();
                    }

                    _cachedReceivePinnedBuffer = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                }

                await InternalReceiveAsync(buffer).ConfigureAwait(false);

                // Check for abort.
                _operation.InterlockedCheckValidStates(s_validAfterReceiveStates);

                WebSocketMessageType bufferType;
                bool endOfMessage;
                bufferType = WebSocketMessageTypeAdapter.GetWebSocketMessageType(_operation.BufferType, out endOfMessage);

                int bytesTransferred = 0;
                checked
                {
                    bytesTransferred = (int)_operation.BytesTransferred;
                }

                WebSocketReceiveResult ret;

                if (bufferType == WebSocketMessageType.Close)
                {
                    UpdateServerCloseStatus();
                    ret = new WebSocketReceiveResult(bytesTransferred, bufferType, endOfMessage, _closeStatus, _closeStatusDescription);
                }
                else
                {
                    ret = new WebSocketReceiveResult(bytesTransferred, bufferType, endOfMessage);
                }

                return ret;
            }
        }

        private Task<bool> InternalReceiveAsync(ArraySegment<byte> buffer)
        {
            bool receiveOperationAlreadyPending = false;
            if (_operation.PendingReadOperation == false)
            {
                lock (_operation.Lock)
                {
                    if (_operation.PendingReadOperation == false)
                    {
                        _operation.CheckValidState(s_validReceiveStates);

                        _operation.TcsReceive = new TaskCompletionSource<bool>();
                        _operation.PendingReadOperation = true;

                        uint bytesRead = 0;
                        Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE winHttpBufferType = 0;

                        uint status = Interop.WinHttp.WinHttpWebSocketReceive(
                                            _operation.WebSocketHandle,
                                            Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset),
                                            (uint)buffer.Count,
                                            out bytesRead,          // Unused in async mode: ignore.
                                            out winHttpBufferType); // Unused in async mode: ignore.

                        if (Interop.WinHttp.ERROR_SUCCESS != status)
                        {
                            throw WinHttpException.CreateExceptionUsingError((int)status);
                        }
                    }
                    else
                    {
                        receiveOperationAlreadyPending = true;
                    }
                }
            }
            else
            {
                receiveOperationAlreadyPending = true;
            }

            if (receiveOperationAlreadyPending)
            {
                var exception = new InvalidOperationException(
                    SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, "ReceiveAsync"));
                _operation.TcsReceive.TrySetException(exception);

                Abort();
            }

            return _operation.TcsReceive.Task;
        }

        private static readonly WebSocketState[] s_validCloseStates = { WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent };
        public override async Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            _operation.InterlockedCheckAndUpdateState(WebSocketState.CloseSent, s_validCloseStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                _operation.TcsClose = new TaskCompletionSource<bool>();
                await InternalCloseAsync(closeStatus, statusDescription).ConfigureAwait(false);
                UpdateServerCloseStatus();
            }
        }

        private Task<bool> InternalCloseAsync(WebSocketCloseStatus closeStatus, string statusDescription)
        {
            uint ret;
            _operation.TcsClose = new TaskCompletionSource<bool>();

            lock (_operation.Lock)
            {
                _operation.CheckValidState(s_validCloseStates);

                if (!string.IsNullOrEmpty(statusDescription))
                {
                    byte[] statusDescriptionBuffer = Encoding.UTF8.GetBytes(statusDescription);

                    ret = Interop.WinHttp.WinHttpWebSocketClose(
                                    _operation.WebSocketHandle,
                                    (ushort)closeStatus,
                                    statusDescriptionBuffer,
                                    (uint)statusDescriptionBuffer.Length);
                }
                else
                {
                    ret = Interop.WinHttp.WinHttpWebSocketClose(
                                    _operation.WebSocketHandle,
                                    (ushort)closeStatus,
                                    IntPtr.Zero,
                                    0);
                }

                if (ret != Interop.WinHttp.ERROR_SUCCESS)
                {
                    throw WinHttpException.CreateExceptionUsingError((int)ret);
                }
            }
            return _operation.TcsClose.Task;
        }

        private void UpdateServerCloseStatus()
        {
            uint ret;
            ushort serverStatus;
            var closeDescription = new byte[WebSocketValidate.MaxControlFramePayloadLength];
            uint closeDescriptionConsumed;

            lock (_operation.Lock)
            {
                ret = Interop.WinHttp.WinHttpWebSocketQueryCloseStatus(
                    _operation.WebSocketHandle,
                    out serverStatus,
                    closeDescription,
                    (uint)closeDescription.Length,
                    out closeDescriptionConsumed);

                if (ret != Interop.WinHttp.ERROR_SUCCESS)
                {
                    throw WinHttpException.CreateExceptionUsingError((int)ret);
                }

                _closeStatus = (WebSocketCloseStatus)serverStatus;
                _closeStatusDescription = Encoding.UTF8.GetString(closeDescription, 0, (int)closeDescriptionConsumed);
            }
        }

        private readonly static WebSocketState[] s_validCloseOutputStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        private readonly static WebSocketState[] s_validCloseOutputStatesAfterUpdate = { WebSocketState.CloseReceived, WebSocketState.CloseSent };
        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            _operation.InterlockedCheckAndUpdateState(WebSocketState.CloseSent, s_validCloseOutputStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                lock (_operation.Lock)
                {
                    _operation.CheckValidState(s_validCloseOutputStatesAfterUpdate);

                    uint ret;
                    _operation.TcsCloseOutput = new TaskCompletionSource<bool>();

                    if (!string.IsNullOrEmpty(statusDescription))
                    {
                        byte[] statusDescriptionBuffer = Encoding.UTF8.GetBytes(statusDescription);

                        ret = Interop.WinHttp.WinHttpWebSocketShutdown(
                                        _operation.WebSocketHandle,
                                        (ushort)closeStatus,
                                        statusDescriptionBuffer,
                                        (uint)statusDescriptionBuffer.Length);
                    }
                    else
                    {
                        ret = Interop.WinHttp.WinHttpWebSocketShutdown(
                                        _operation.WebSocketHandle,
                                        (ushort)closeStatus,
                                        IntPtr.Zero,
                                        0);
                    }

                    if (ret != Interop.WinHttp.ERROR_SUCCESS)
                    {
                        throw WinHttpException.CreateExceptionUsingError((int)ret);
                    }
                }

                return _operation.TcsCloseOutput.Task;
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                lock (_operation.Lock)
                {
                    // Disposing will involve calling WinHttpClose on handles. It is critical that no other WinHttp 
                    // function is running at the same time.

                    if (!_disposed)
                    {
                        _operation.Dispose();

                        // TODO (Issue 2508): Pinned buffers must be released in the callback, when it is guaranteed no further
                        // operations will be made to the send/receive buffers.
                        if (_cachedReceivePinnedBuffer.IsAllocated)
                        {
                            _cachedReceivePinnedBuffer.Free();
                        }

                        if (_cachedSendPinnedBuffer.IsAllocated)
                        {
                            _cachedSendPinnedBuffer.Free();
                        }

                        _disposed = true;
                    }
                }
            }

            // No need to suppress finalization since the finalizer is not overridden.
        }

        public override void Abort()
        {
            lock (_operation.Lock)
            {
                if ((State != WebSocketState.None) && (State != WebSocketState.Connecting))
                {
                    _operation.UpdateState(WebSocketState.Aborted);
                }
                else
                {
                    // ClientWebSocket Desktop behavior: a ws that was not connected will not switch state to Aborted.
                    _operation.UpdateState(WebSocketState.Closed);
                }

                Dispose();
            }

            CancelAllOperations();
        }

        private void CancelAllOperations()
        {
            if (_operation.TcsClose != null)
            {
                var exception = new WebSocketException(
                    WebSocketError.InvalidState,
                    SR.Format(
                        SR.net_WebSockets_InvalidState_ClosedOrAborted,
                        "System.Net.WebSockets.InternalClientWebSocket",
                        "Aborted"));

                _operation.TcsClose.TrySetException(exception);
            }

            if (_operation.TcsCloseOutput != null)
            {
                var exception = new WebSocketException(
                    WebSocketError.InvalidState,
                    SR.Format(
                        SR.net_WebSockets_InvalidState_ClosedOrAborted,
                        "System.Net.WebSockets.InternalClientWebSocket",
                        "Aborted"));

                _operation.TcsCloseOutput.TrySetException(exception);
            }

            if (_operation.TcsReceive != null)
            {
                var exception = new WebSocketException(
                    WebSocketError.InvalidState,
                    SR.Format(
                        SR.net_WebSockets_InvalidState_ClosedOrAborted,
                        "System.Net.WebSockets.InternalClientWebSocket",
                        "Aborted"));

                _operation.TcsReceive.TrySetException(exception);
            }

            if (_operation.TcsSend != null)
            {
                var exception = new OperationCanceledException();
                _operation.TcsSend.TrySetException(exception);
            }

            if (_operation.TcsUpgrade != null)
            {
                var exception = new WebSocketException(SR.net_webstatus_ConnectFailure);
                _operation.TcsUpgrade.TrySetException(exception);
            }
        }

        private void ThrowOnInvalidHandle(Interop.WinHttp.SafeWinHttpHandle value)
        {
            if (value.IsInvalid)
            {
                Abort();
                throw new WebSocketException(
                    SR.net_webstatus_ConnectFailure,
                    WinHttpException.CreateExceptionUsingLastError());
            }
        }

        private CancellationTokenRegistration ThrowOrRegisterCancellation(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Abort();
                cancellationToken.ThrowIfCancellationRequested();
            }

            CancellationTokenRegistration cancellationRegistration =
                cancellationToken.Register(s => ((WinHttpWebSocket)s).Abort(), this);

            return cancellationRegistration;
        }
    }
}
