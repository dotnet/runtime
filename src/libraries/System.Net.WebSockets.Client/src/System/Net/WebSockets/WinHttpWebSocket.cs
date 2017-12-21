// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WinHttpWebSocket : WebSocket
    {
        #region Constants
        // TODO (#7893): This code needs to be shared with WinHttpClientHandler
        private const string HeaderNameCookie = "Cookie";
        private const string HeaderNameWebSocketProtocol = "Sec-WebSocket-Protocol";
        #endregion

        // TODO (Issue 2503): move System.Net.* strings to resources as appropriate.

        // NOTE: All WinHTTP operations must be called while holding the _operation.Lock.
        // It is critical that no handle gets closed while a WinHTTP function is running.
        private WebSocketCloseStatus? _closeStatus = null;
        private string _closeStatusDescription = null;
        private string _subProtocol = null;
        private bool _disposed = false;

        private WinHttpWebSocketState _operation = new WinHttpWebSocketState();

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
                return _subProtocol;
            }
        }
        #endregion

        static readonly WebSocketState[] s_validConnectStates = { WebSocketState.None };
        static readonly WebSocketState[] s_validConnectingStates = { WebSocketState.Connecting };
        
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
                    _operation.IncrementHandlesOpenWithCallback();

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

                    const uint notificationFlags =
                        Interop.WinHttp.WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS |
                        Interop.WinHttp.WINHTTP_CALLBACK_FLAG_HANDLES |
                        Interop.WinHttp.WINHTTP_CALLBACK_FLAG_SECURE_FAILURE;

                    if (Interop.WinHttp.WinHttpSetStatusCallback(
                        _operation.RequestHandle,
                        WinHttpWebSocketCallback.s_StaticCallbackDelegate,
                        notificationFlags,
                        IntPtr.Zero) == (IntPtr)Interop.WinHttp.WINHTTP_INVALID_STATUS_CALLBACK)
                    {
                        WinHttpException.ThrowExceptionUsingLastError();
                    }

                    _operation.RequestHandle.AttachCallback();

                    // We need to pin the operation object at this point in time since the WinHTTP callback
                    // has been fully wired to the request handle and the operation object has been set as
                    // the context value of the callback. Any notifications from activity on the handle will
                    // result in the callback being called with the context value.
                    _operation.Pin();                    

                    AddRequestHeaders(uri, options);

                    _operation.TcsUpgrade = new TaskCompletionSource<bool>();
                }

                await InternalSendWsUpgradeRequestAsync().ConfigureAwait(false);

                await InternalReceiveWsUpgradeResponse().ConfigureAwait(false);

                lock (_operation.Lock)
                {
                    VerifyUpgradeResponse();

                    ThrowOnInvalidConnectState();

                    _operation.WebSocketHandle =
                        Interop.WinHttp.WinHttpWebSocketCompleteUpgrade(_operation.RequestHandle, IntPtr.Zero);

                    ThrowOnInvalidHandle(_operation.WebSocketHandle);
                    _operation.IncrementHandlesOpenWithCallback();

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
                           Interop.WinHttp.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
                           null,
                           null,
                           (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);

            ThrowOnInvalidHandle(sessionHandle);

            uint optionAssuredNonBlockingTrue = 1; // TRUE

            if (!Interop.WinHttp.WinHttpSetOption(
                sessionHandle,
                Interop.WinHttp.WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS,
                ref optionAssuredNonBlockingTrue,
                (uint)sizeof(uint)))
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

        private static readonly WebSocketState[] s_validSendStates = { WebSocketState.Open, WebSocketState.CloseReceived };
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

                _operation.PinSendBuffer(buffer);

                bool sendOperationAlreadyPending = false;
                if (_operation.PendingWriteOperation == false)
                {
                    lock (_operation.Lock)
                    {
                        _operation.CheckValidState(s_validSendStates);

                        if (_operation.PendingWriteOperation == false)
                        {
                            _operation.PendingWriteOperation = true;
                            _operation.TcsSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                            uint ret = Interop.WinHttp.WinHttpWebSocketSend(
                                _operation.WebSocketHandle,
                                bufferType,
                                buffer.Count > 0 ? Marshal.UnsafeAddrOfPinnedArrayElement(buffer.Array, buffer.Offset) : IntPtr.Zero,
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

        private static readonly WebSocketState[] s_validReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent };
        private static readonly WebSocketState[] s_validAfterReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent, WebSocketState.CloseReceived };
        public override async Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            _operation.InterlockedCheckValidStates(s_validReceiveStates);

            using (ThrowOrRegisterCancellation(cancellationToken))
            {
                _operation.PinReceiveBuffer(buffer);

                await InternalReceiveAsync(buffer).ConfigureAwait(false);

                // Check for abort.
                _operation.InterlockedCheckValidStates(s_validAfterReceiveStates);

                bool endOfMessage;
                WebSocketMessageType bufferType = WebSocketMessageTypeAdapter.GetWebSocketMessageType(_operation.BufferType, out endOfMessage);
                int bytesTransferred = checked((int)_operation.BytesTransferred);

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

        public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            _operation.InterlockedCheckValidStates(s_validReceiveStates);

            using (ThrowOrRegisterCancellation(cancellationToken))
            using (buffer.Retain(pin: true))
            {
                await InternalReceiveAsync(buffer).ConfigureAwait(false);

                // Check for abort.
                _operation.InterlockedCheckValidStates(s_validAfterReceiveStates);

                WebSocketMessageType bufferType = WebSocketMessageTypeAdapter.GetWebSocketMessageType(_operation.BufferType, out bool endOfMessage);
                int bytesTransferred = checked((int)_operation.BytesTransferred);

                if (bufferType == WebSocketMessageType.Close)
                {
                    UpdateServerCloseStatus();
                }

                return new ValueWebSocketReceiveResult(bytesTransferred, bufferType, endOfMessage);
            }
        }

        private Task<bool> InternalReceiveAsync(Memory<byte> pinnedBuffer)
        {
            bool receiveOperationAlreadyPending = false;
            if (_operation.PendingReadOperation == false)
            {
                lock (_operation.Lock)
                {
                    if (_operation.PendingReadOperation == false)
                    {
                        _operation.CheckValidState(s_validReceiveStates);

                        // Prevent continuations from running on the same thread as the callback to prevent re-entrance deadlocks
                        _operation.TcsReceive = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _operation.PendingReadOperation = true;

                        uint bytesRead = 0;
                        Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE winHttpBufferType = 0;
                        IntPtr pinnedBufferPtr;
                        unsafe
                        {
                            fixed (byte* p = &MemoryMarshal.GetReference(pinnedBuffer.Span))
                            {
                                pinnedBufferPtr = (IntPtr)p;
                            }
                        }

                        uint status = Interop.WinHttp.WinHttpWebSocketReceive(
                                            _operation.WebSocketHandle,
                                            pinnedBufferPtr,
                                            (uint)pinnedBuffer.Length,
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

        private static readonly WebSocketState[] s_validCloseOutputStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        private static readonly WebSocketState[] s_validCloseOutputStatesAfterUpdate = { WebSocketState.CloseReceived, WebSocketState.CloseSent };
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

        private void VerifyUpgradeResponse()
        {
            // Check the status code
            var statusCode = GetHttpStatusCode();
            if (statusCode != HttpStatusCode.SwitchingProtocols)
            {
                Abort();
                return;
            }

            _subProtocol = GetResponseHeader(HeaderNameWebSocketProtocol);
        }

        private void AddRequestHeaders(Uri uri, ClientWebSocketOptions options)
        {
            var requestHeadersBuffer = new StringBuilder();

            // Manually add cookies.
            if (options.Cookies != null)
            {
                AppendCookieHeaderLine(uri, options.Cookies, requestHeadersBuffer);
            }

            // Serialize general request headers.
            requestHeadersBuffer.AppendLine(options.RequestHeaders.ToString());

            using (List<string>.Enumerator e = options.RequestedSubProtocols.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    requestHeadersBuffer.Append(HeaderNameWebSocketProtocol + ": ");
                    requestHeadersBuffer.Append(e.Current);

                    while (e.MoveNext())
                    {
                        requestHeadersBuffer.Append(", ");
                        requestHeadersBuffer.Append(e.Current);
                    }

                    requestHeadersBuffer.AppendLine();
                }
            }

            // Add request headers to WinHTTP request handle.
            if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                _operation.RequestHandle,
                requestHeadersBuffer,
                (uint)requestHeadersBuffer.Length,
                Interop.WinHttp.WINHTTP_ADDREQ_FLAG_ADD))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }
        }

        private static void AppendCookieHeaderLine(Uri uri, CookieContainer cookies, StringBuilder requestHeadersBuffer)
        {
            Debug.Assert(cookies != null);
            Debug.Assert(requestHeadersBuffer != null);

            string cookieValues = cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieValues))
            {
                requestHeadersBuffer.Append(HeaderNameCookie + ": ");
                requestHeadersBuffer.AppendLine(cookieValues);
            }
        }

        private HttpStatusCode GetHttpStatusCode()
        {
            uint infoLevel = Interop.WinHttp.WINHTTP_QUERY_STATUS_CODE | Interop.WinHttp.WINHTTP_QUERY_FLAG_NUMBER;
            uint result = 0;
            uint resultSize = sizeof(uint);

            if (!Interop.WinHttp.WinHttpQueryHeaders(
                _operation.RequestHandle,
                infoLevel,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                ref result,
                ref resultSize,
                IntPtr.Zero))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }

            return (HttpStatusCode)result;
        }

        private unsafe string GetResponseHeader(string headerName, char[] buffer = null)
        {
            const int StackLimit = 128;

            Debug.Assert(buffer == null || (buffer != null && buffer.Length > StackLimit));

            int bufferLength;

            if (buffer == null)
            {
                bufferLength = StackLimit;
                char* pBuffer = stackalloc char[bufferLength];
                if (QueryHeaders(headerName, pBuffer, ref bufferLength))
                {
                    return new string(pBuffer, 0, bufferLength);
                }
            }
            else
            {
                bufferLength = buffer.Length;
                fixed (char* pBuffer = &buffer[0])
                {
                    if (QueryHeaders(headerName, pBuffer, ref bufferLength))
                    {
                        return new string(pBuffer, 0, bufferLength);
                    }
                }
            }

            int lastError = Marshal.GetLastWin32Error();

            if (lastError == Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND)
            {
                return null;
            }

            if (lastError == Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER)
            {
                buffer = new char[bufferLength];
                return GetResponseHeader(headerName, buffer);
            }

            throw WinHttpException.CreateExceptionUsingError(lastError);
        }

        private unsafe bool QueryHeaders(string headerName, char* buffer, ref int bufferLength)
        {
            Debug.Assert(bufferLength >= 0, "bufferLength must not be negative.");

            uint index = 0;

            // Convert the char buffer length to the length in bytes.
            uint bufferLengthInBytes = (uint)bufferLength * sizeof(char);

            // The WinHttpQueryHeaders buffer length is in bytes,
            // but the API actually returns Unicode characters.
            bool result = Interop.WinHttp.WinHttpQueryHeaders(
                _operation.RequestHandle,
                Interop.WinHttp.WINHTTP_QUERY_CUSTOM,
                headerName,
                new IntPtr(buffer),
                ref bufferLengthInBytes,
                ref index);

            // Convert the byte buffer length back to the length in chars.
            bufferLength = (int)bufferLengthInBytes / sizeof(char);

            return result;
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
                _operation.TcsReceive.TrySetCanceled();
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
