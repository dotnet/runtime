// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation.Metadata;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web;

using RTCertificate = Windows.Security.Cryptography.Certificates.Certificate;
using RTCertificateQuery = Windows.Security.Cryptography.Certificates.CertificateQuery;
using RTCertificateStores = Windows.Security.Cryptography.Certificates.CertificateStores;
using RTWebSocketError = Windows.Networking.Sockets.WebSocketError;

namespace System.Net.WebSockets
{
    internal class WinRTWebSocket : WebSocket
    {
        #region Constants
        private const string HeaderNameCookie = "Cookie";
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";
        #endregion

        private static readonly Lazy<bool> s_MessageWebSocketClientCertificateSupported =
            new Lazy<bool>(InitMessageWebSocketClientCertificateSupported);
        private static bool MessageWebSocketClientCertificateSupported => s_MessageWebSocketClientCertificateSupported.Value;
        private static readonly Lazy<bool> s_MessageWebSocketReceiveModeSupported =
            new Lazy<bool>(InitMessageWebSocketReceiveModeSupported);
        private static bool MessageWebSocketReceiveModeSupported => s_MessageWebSocketReceiveModeSupported.Value;

        private WebSocketCloseStatus? _closeStatus = null;
        private string _closeStatusDescription = null;
        private string _subProtocol = null;
        private bool _disposed = false;

        private object _stateLock = new object();
        private int _pendingWriteOperation;
        private MessageWebSocket _messageWebSocket;
        private DataWriter _messageWriter;

        private WebSocketState _state = WebSocketState.None;
        private TaskCompletionSource<ArraySegment<byte>> _receiveAsyncBufferTcs;
        private TaskCompletionSource<WebSocketReceiveResult> _webSocketReceiveResultTcs;
        private TaskCompletionSource<WebSocketReceiveResult> _closeWebSocketReceiveResultTcs;

        public WinRTWebSocket()
        {
            _state = WebSocketState.None;
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
                return _state;
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

        private static readonly WebSocketState[] s_validConnectStates = {WebSocketState.None};
        private static readonly WebSocketState[] s_validConnectingStates = {WebSocketState.Connecting};

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            InterlockedCheckAndUpdateState(WebSocketState.Connecting, s_validConnectStates);
            CheckValidState(s_validConnectingStates);

            _messageWebSocket = new MessageWebSocket();
            foreach (var header in options.RequestHeaders)
            {
                _messageWebSocket.SetRequestHeader((string)header, options.RequestHeaders[(string)header]);
            }

            string cookies = options.Cookies == null ? null : options.Cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookies))
            {
                _messageWebSocket.SetRequestHeader(HeaderNameCookie, cookies);
            }

            var websocketControl = _messageWebSocket.Control;
            foreach (var subProtocol in options.RequestedSubProtocols)
            {
                websocketControl.SupportedProtocols.Add(subProtocol);
            }

            if (options.ClientCertificates.Count > 0)
            {
                if (!MessageWebSocketClientCertificateSupported)
                {
                    throw new PlatformNotSupportedException(string.Format(CultureInfo.InvariantCulture,
                        SR.net_WebSockets_UWPClientCertSupportRequiresWindows10GreaterThan1703));
                }

                X509Certificate2 dotNetClientCert = CertificateHelper.GetEligibleClientCertificate(options.ClientCertificates);
                if (dotNetClientCert != null)
                {
                    RTCertificate winRtClientCert = await CertificateHelper.ConvertDotNetClientCertToWinRtClientCertAsync(dotNetClientCert).ConfigureAwait(false);
                    if (winRtClientCert == null)
                    {
                        throw new PlatformNotSupportedException(string.Format(
                                    CultureInfo.InvariantCulture,
                                    SR.net_WebSockets_UWPClientCertSupportRequiresCertInPersonalCertificateStore));
                    }

                    websocketControl.ClientCertificate = winRtClientCert;
                }
            }

            // Try to opt into PartialMessage receive mode so that we can hand partial data back to the app as it arrives.
            // If the MessageWebSocketControl.ReceiveMode API surface is not available, the MessageWebSocket.MessageReceived
            // event will only get triggered when an entire WebSocket message has been received. This results in large memory
            // footprint and prevents "streaming" scenarios (e.g., WCF) from working properly.
            if (MessageWebSocketReceiveModeSupported)
            {
                // Always enable partial message receive mode if the WinRT API supports it.
                _messageWebSocket.Control.ReceiveMode = MessageWebSocketReceiveMode.PartialMessage;
            }

            try
            {
                _receiveAsyncBufferTcs = new TaskCompletionSource<ArraySegment<byte>>();
                _closeWebSocketReceiveResultTcs = new TaskCompletionSource<WebSocketReceiveResult>();
                _messageWebSocket.MessageReceived += OnMessageReceived;
                _messageWebSocket.Closed += OnCloseReceived;
                await _messageWebSocket.ConnectAsync(uri).AsTask(cancellationToken).ConfigureAwait(false);
                _subProtocol = _messageWebSocket.Information.Protocol;
                _messageWriter = new DataWriter(_messageWebSocket.OutputStream);
            }
            catch (Exception)
            {
                UpdateState(WebSocketState.Closed);
                throw;
            }

            UpdateState(WebSocketState.Open);
        }

        public override void Abort()
        {
            AbortInternal();
        }

        private void AbortInternal(Exception customException = null)
        {
            lock (_stateLock)
            {
                if ((_state != WebSocketState.None) && (_state != WebSocketState.Connecting))
                {
                    UpdateState(WebSocketState.Aborted);
                }
                else
                {
                    // ClientWebSocket Desktop behavior: a ws that was not connected will not switch state to Aborted.
                    UpdateState(WebSocketState.Closed);
                }
            }

            CancelAllOperations(customException);
            Dispose();
        }

        private void CancelAllOperations(Exception customException)
        {
            if (_receiveAsyncBufferTcs != null)
            {
                // This exception will be received by OnMessageReceived and won't be exposed
                // to user code.
                var exception = new OperationCanceledException("Aborted");
                _receiveAsyncBufferTcs.TrySetException(exception);
            }

            if (_webSocketReceiveResultTcs != null)
            {
                if (customException != null)
                {
                    _webSocketReceiveResultTcs.TrySetException(customException);
                }
                else
                {
                    var exception = new WebSocketException(
                        WebSocketError.InvalidState,
                        SR.Format(
                            SR.net_WebSockets_InvalidState_ClosedOrAborted,
                            "System.Net.WebSockets.InternalClientWebSocket",
                            "Aborted"));

                    _webSocketReceiveResultTcs.TrySetException(exception);
                }
            }

            if (_closeWebSocketReceiveResultTcs != null)
            {
                if (customException != null)
                {
                    _closeWebSocketReceiveResultTcs.TrySetException(customException);
                }
                else
                {
                    var exception = new WebSocketException(
                        WebSocketError.InvalidState,
                        SR.Format(
                            SR.net_WebSockets_InvalidState_ClosedOrAborted,
                            "System.Net.WebSockets.InternalClientWebSocket",
                            "Aborted"));

                    _closeWebSocketReceiveResultTcs.TrySetException(exception);
                }
            }
        }

        private static readonly WebSocketState[] s_validCloseStates = { WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent, WebSocketState.Closed };
        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool callClose = false;
            lock (_stateLock)
            {
                callClose = (_state != WebSocketState.CloseSent) && (_state != WebSocketState.Closed);
            }

            InterlockedCheckAndUpdateCloseState(WebSocketState.CloseSent, s_validCloseStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                if (callClose)
                {
                    _messageWebSocket.Close((ushort) closeStatus, statusDescription ?? String.Empty);
                }

                var result = await _closeWebSocketReceiveResultTcs.Task.ConfigureAwait(false);
                _closeStatus = result.CloseStatus;
                _closeStatusDescription = result.CloseStatusDescription;
                InterlockedCheckAndUpdateCloseState(WebSocketState.CloseReceived, s_validCloseStates);
            }
        }

        private static readonly WebSocketState[] s_validCloseOutputStates = { WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent };
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            CheckValidState(s_validCloseOutputStates);
            _messageWebSocket.Close((ushort)closeStatus, statusDescription ?? String.Empty);
            InterlockedCheckAndUpdateCloseState(WebSocketState.CloseSent, s_validCloseOutputStates);
            return Task.CompletedTask;
        }

        private void OnCloseReceived(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            var recvResult = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, (WebSocketCloseStatus)args.Code,
                args.Reason);
            _closeWebSocketReceiveResultTcs.TrySetResult(recvResult);
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                lock (_stateLock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        if (_messageWebSocket != null)
                        {
                            _messageWebSocket.Dispose();
                        }

                        if (_messageWriter != null)
                        {
                            _messageWriter.Dispose();
                        }
                    }
                }
            }
        }

        private static readonly WebSocketState[] s_validReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent };
        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            InterlockedCheckValidStates(s_validReceiveStates);

            using (CancellationTokenRegistration ctr = ThrowOrRegisterCancellation(cancellationToken))
            {
                _webSocketReceiveResultTcs = new TaskCompletionSource<WebSocketReceiveResult>();
                _receiveAsyncBufferTcs.TrySetResult(buffer);

                Task<WebSocketReceiveResult> completedTask = await Task.WhenAny(
                    _webSocketReceiveResultTcs.Task,
                    _closeWebSocketReceiveResultTcs.Task).ConfigureAwait(false);

                WebSocketReceiveResult result = await completedTask;

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _closeStatus = result.CloseStatus;
                    _closeStatusDescription = result.CloseStatusDescription;
                    InterlockedCheckAndUpdateCloseState(WebSocketState.CloseReceived, s_validCloseOutputStates);
                }
                else
                {
                    _webSocketReceiveResultTcs = new TaskCompletionSource<WebSocketReceiveResult>();
                }

                return result;
            }
        }

        private void OnMessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            // GetDataReader() throws an exception when either:
            // (1) The underlying TCP connection is closed prematurely (e.g., FIN/RST received without sending/receiving a WebSocket Close frame).
            // (2) The server sends invalid data (e.g., corrupt HTTP headers or a message exceeding the MaxMessageSize).
            //
            // In both cases, the appropriate thing to do is to close the socket, as we have reached an unexpected state in
            // the WebSocket protocol.
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    uint dataAvailable;
                    while ((dataAvailable = reader.UnconsumedBufferLength) > 0)
                    {
                        ArraySegment<byte> buffer;
                        try
                        {
                            buffer = _receiveAsyncBufferTcs.Task.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) // Caused by Abort call on WebSocket
                        {
                            return;
                        }

                        _receiveAsyncBufferTcs = new TaskCompletionSource<ArraySegment<byte>>();
                        WebSocketMessageType messageType;
                        if (args.MessageType == SocketMessageType.Binary)
                        {
                            messageType = WebSocketMessageType.Binary;
                        }
                        else
                        {
                            messageType = WebSocketMessageType.Text;
                        }

                        bool endOfMessage = false;
                        uint readCount = Math.Min(dataAvailable, (uint) buffer.Count);
                        var dataBuffer = reader.ReadBuffer(readCount);
                        // Safe to cast readCount to int as the maximum value that readCount can be is buffer.Count.
                        dataBuffer.CopyTo(0, buffer.Array, buffer.Offset, (int) readCount);
                        if (dataAvailable == readCount)
                        {
                            endOfMessage = !IsPartialMessageEvent(args);
                        }

                        WebSocketReceiveResult recvResult = new WebSocketReceiveResult((int) readCount, messageType,
                            endOfMessage);
                        _webSocketReceiveResultTcs.TrySetResult(recvResult);
                    }
                }
            }
            catch (Exception exc)
            {
                // WinRT WebSockets always throw exceptions of type System.Exception. However, we can determine whether
                // or not we're dealing with a known error by using WinRT's WebSocketError.GetStatus method.
                WebErrorStatus status = RTWebSocketError.GetStatus(exc.HResult);
                WebSocketError actualError = WebSocketError.Faulted;
                switch (status)
                {
                    case WebErrorStatus.ConnectionAborted:
                    case WebErrorStatus.ConnectionReset:
                    case WebErrorStatus.Disconnected:
                        actualError = WebSocketError.ConnectionClosedPrematurely;
                        break;
                }

                // Propagate a custom exception to any pending ReceiveAsync/CloseAsync operations and close the socket.
                WebSocketException customException = new WebSocketException(actualError, exc);
                AbortInternal(customException);
            }
        }

        private static readonly WebSocketState[] s_validSendStates = { WebSocketState.Open, WebSocketState.CloseReceived };
        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            InterlockedCheckValidStates(s_validSendStates);

            if (cancellationToken.IsCancellationRequested)
            {
                Abort();
                cancellationToken.ThrowIfCancellationRequested();
            }

            var pendingWrite = Interlocked.CompareExchange(ref _pendingWriteOperation, 1, 0);
            if (pendingWrite == 1)
            {
                var exception = new InvalidOperationException(SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, "SendAsync"));
                Abort();
                throw exception;
            }

            try
            {
                _messageWriter.WriteBuffer(buffer.Array.AsBuffer(buffer.Offset, buffer.Count));
                if (endOfMessage)
                {
                    _messageWebSocket.Control.MessageType = messageType == WebSocketMessageType.Binary
                        ? SocketMessageType.Binary
                        : SocketMessageType.Utf8;
                    await _messageWriter.StoreAsync().AsTask(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                Interlocked.CompareExchange(ref _pendingWriteOperation, 0, 1);
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
                cancellationToken.Register(s =>
                {
                    var thisRef = (WinRTWebSocket)s;

                    // Propagate a custom exception to any pending ReceiveAsync/CloseAsync operations and close the socket.
                    var customException = new OperationCanceledException(nameof(WebSocketState.Aborted));
                    thisRef.AbortInternal(customException);
                }, this);

            return cancellationRegistration;
        }

        #region State // Code related to state management
        // TODO (#7896): Refactor state into encapsulated class
        public void InterlockedCheckValidStates(WebSocketState[] validStates)
        {
            lock (_stateLock)
            {
                CheckValidState(validStates);
            }
        }

        private void InterlockedCheckAndUpdateState(
            WebSocketState newState,
            params WebSocketState[] validStates)
        {
            lock (_stateLock)
            {
                CheckValidState(validStates);
                UpdateState(newState);
            }
        }

        private void InterlockedCheckAndUpdateCloseState(WebSocketState newState, params WebSocketState[] validStates)
        {
            lock (_stateLock)
            {
                CheckValidState(validStates);
                if ((_state == WebSocketState.CloseReceived && newState == WebSocketState.CloseSent) ||
                    (_state == WebSocketState.CloseSent && newState == WebSocketState.CloseReceived) ||
                    ( _state == WebSocketState.Closed))
                {
                    _state = WebSocketState.Closed;
                }
                else
                {
                    UpdateState(newState);
                }
            }
        }

        // Must be called with Lock taken.
        private void CheckValidState(WebSocketState[] validStates)
        {
            string validStatesText = string.Empty;

            if (validStates != null && validStates.Length > 0)
            {
                foreach (WebSocketState currentState in validStates)
                {
                    if (_state == currentState)
                    {
                        // Ordering is important to maintain .Net 4.5 WebSocket implementation exception behavior.
                        if (_disposed)
                        {
                            throw new ObjectDisposedException(GetType().FullName);
                        }

                        return;
                    }
                }

                validStatesText = string.Join(", ", validStates);
            }

            throw new WebSocketException(SR.Format(SR.net_WebSockets_InvalidState, _state, validStatesText));
        }

        private void UpdateState(WebSocketState value)
        {
            if ((_state != WebSocketState.Closed) && (_state != WebSocketState.Aborted))
            {
                _state = value;
            }
        }
        #endregion

        #region Helpers
        private static bool InitMessageWebSocketClientCertificateSupported()
        {
            return ApiInformation.IsPropertyPresent(
                "Windows.Networking.Sockets.MessageWebSocketControl",
                "ClientCertificate");
        }

        private static bool InitMessageWebSocketReceiveModeSupported()
        {
            return ApiInformation.IsPropertyPresent(
                "Windows.Networking.Sockets.MessageWebSocketControl",
                "ReceiveMode");
        }

        private bool IsPartialMessageEvent(MessageWebSocketMessageReceivedEventArgs eventArgs)
        {
            if (MessageWebSocketReceiveModeSupported)
            {
                return !eventArgs.IsMessageComplete;
            }

            // When MessageWebSocketMessageReceivedEventArgs.IsMessageComplete is not available, WinRT's behavior
            // is always to wait for the entire WebSocket message to arrive before raising a MessageReceived event.
            return false;
        }
        #endregion Helpers

    }
}
