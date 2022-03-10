// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

using JavaScript = System.Runtime.InteropServices.JavaScript;
using JSObject = System.Runtime.InteropServices.JavaScript.JSObject;

namespace System.Net.WebSockets
{
    /// <summary>
    /// Provides a client for connecting to WebSocket services.
    /// </summary>
    internal sealed class BrowserWebSocket : WebSocket
    {
        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;
        private JSObject? _innerWebSocket;
        private WebSocketState _state;
        private bool _disposed;
        private bool _aborted;

        #region Properties

        public override WebSocketState State
        {
            get
            {
                if (_innerWebSocket != null && !_disposed && (_state == WebSocketState.Connecting || _state == WebSocketState.Open || _state == WebSocketState.CloseSent))
                {
                    _state = GetReadyState();
                }
                return _state;
            }
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;
        public override string? CloseStatusDescription => _closeStatusDescription;
        public override string? SubProtocol => _innerWebSocket != null && !_innerWebSocket.IsDisposed ? _innerWebSocket!.GetObjectProperty("protocol")?.ToString() : null;

        #endregion Properties

        internal Task ConnectAsync(Uri uri, List<string>? requestedSubProtocols, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_state != WebSocketState.None)
            {
                throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }
            _state = WebSocketState.Connecting;
            return ConnectAsyncCore(uri, requestedSubProtocols, cancellationToken);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ThrowIfDisposed();

            // fast check of previous _state instead of GetReadyState(), the readyState would be validated on JS side
            if (_state != WebSocketState.Open)
            {
                throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
            }

            if (messageType != WebSocketMessageType.Binary && messageType != WebSocketMessageType.Text)
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_Argument_InvalidMessageType,
                    messageType,
                    nameof(SendAsync),
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text,
                    nameof(CloseOutputAsync)),
                    nameof(messageType));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromException<WebSocketReceiveResult>(new OperationCanceledException(cancellationToken));
            }
            ThrowIfDisposed();
            // fast check of previous _state instead of GetReadyState(), the readyState would be validated on JS side
            if (_state != WebSocketState.Open && _state != WebSocketState.CloseSent)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, _state, "Open, CloseSent"));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            return ReceiveAsyncCore(buffer, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            var state = State;
            if (state == WebSocketState.None || state == WebSocketState.Closed)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, state, "Connecting, Open, CloseSent, Aborted"));
            }

            return state == WebSocketState.Open || state == WebSocketState.Connecting || state == WebSocketState.Aborted
                ? CloseAsyncCore(closeStatus, statusDescription, false, cancellationToken)
                : Task.CompletedTask;
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            var state = State;
            if (state == WebSocketState.None || state == WebSocketState.Closed)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, state, "Connecting, Open, CloseSent, Aborted"));
            }

            return state == WebSocketState.Open || state == WebSocketState.Connecting || state == WebSocketState.Aborted || state == WebSocketState.CloseSent
                ? CloseAsyncCore(closeStatus, statusDescription, state != WebSocketState.Aborted, cancellationToken)
                : Task.CompletedTask;
        }

        public override void Abort()
        {
            if (!_disposed && State != WebSocketState.Closed)
            {
                _state = WebSocketState.Aborted;
                _aborted = true;
                if (_innerWebSocket != null)
                {
                    JavaScript.Runtime.WebSocketAbort(_innerWebSocket!);
                }
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                var state = State;
                _disposed = true;
                if (state < WebSocketState.Aborted && state != WebSocketState.None)
                {
                    Abort();
                }
                if (state != WebSocketState.Aborted)
                {
                    _state = WebSocketState.Closed;
                }
                _innerWebSocket?.Dispose();
                _innerWebSocket = null;
            }
        }

        private async Task ConnectAsyncCore(Uri uri, List<string>? requestedSubProtocols, CancellationToken cancellationToken)
        {
            try
            {
                object[]? subProtocols = requestedSubProtocols?.ToArray();
                var onClose = (int code, string reason) =>
                {
                    _closeStatus = (WebSocketCloseStatus)code;
                    _closeStatusDescription = reason;
                    WebSocketState state = State;
                    if (state == WebSocketState.Connecting || state == WebSocketState.Open || state == WebSocketState.CloseSent)
                    {
                        _state = WebSocketState.Closed;
                    }
                };

                var openTask = JavaScript.Runtime.WebSocketOpen(uri.ToString(), subProtocols, onClose, out _innerWebSocket, out IntPtr promiseJSHandle);
                var wrappedTask = CancelationHelper(openTask, promiseJSHandle, cancellationToken, _state);

                await wrappedTask.ConfigureAwait(true);
                if (State == WebSocketState.Connecting)
                {
                    _state = WebSocketState.Open;
                }
            }
            catch (OperationCanceledException ex)
            {
                _state = WebSocketState.Closed;
                if (_aborted)
                {
                    throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, ex);
                }
                throw;
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private async Task SendAsyncCore(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            try
            {
                var sendTask = JavaScript.Runtime.WebSocketSend(_innerWebSocket!, buffer, (int)messageType, endOfMessage, out IntPtr promiseJSHandle);
                if (sendTask == null)
                {
                    // return synchronously
                    return;
                }
                var wrappedTask = CancelationHelper(sendTask, promiseJSHandle, cancellationToken, _state);

                await wrappedTask.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, State, "Open"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                ArraySegment<int> response = new ArraySegment<int>(new int[3]);
                var receiveTask = JavaScript.Runtime.WebSocketReceive(_innerWebSocket!, buffer, response, out IntPtr promiseJSHandle);
                if (receiveTask == null)
                {
                    // return synchronously
                    return ConvertResponse(response);
                }

                var wrappedTask = CancelationHelper(receiveTask, promiseJSHandle, cancellationToken, _state);
                await wrappedTask.ConfigureAwait(true);

                return ConvertResponse(response);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, State, "Open, CloseSent"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private WebSocketReceiveResult ConvertResponse(ArraySegment<int> response)
        {
            const int countIndex = 0;
            const int typeIndex = 1;
            const int endIndex = 2;

            WebSocketMessageType messageType = (WebSocketMessageType)response[typeIndex];
            if (messageType == WebSocketMessageType.Close)
            {
                return new WebSocketReceiveResult(response[countIndex], messageType, response[endIndex] != 0, CloseStatus, CloseStatusDescription);
            }
            return new WebSocketReceiveResult(response[countIndex], messageType, response[endIndex] != 0);
        }

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus, string? statusDescription, bool waitForCloseReceived, CancellationToken cancellationToken)
        {
            _closeStatus = closeStatus;
            _closeStatusDescription = statusDescription;

            var closeTask = JavaScript.Runtime.WebSocketClose(_innerWebSocket!, (int)closeStatus, statusDescription, waitForCloseReceived, out IntPtr promiseJSHandle);
            if (closeTask != null)
            {
                var wrappedTask = CancelationHelper(closeTask, promiseJSHandle, cancellationToken, _state);
                await wrappedTask.ConfigureAwait(true);
            }

            var state = State;
            if (state == WebSocketState.Open || state == WebSocketState.Connecting || state == WebSocketState.CloseSent)
            {
                _state = waitForCloseReceived ? WebSocketState.Closed : WebSocketState.CloseSent;
            }
        }

        private async ValueTask<object> CancelationHelper(Task<object> jsTask, IntPtr promiseJSHandle, CancellationToken cancellationToken, WebSocketState previousState)
        {
            if (jsTask.IsCompletedSuccessfully)
            {
                return jsTask.Result;
            }
            try
            {
                using (var receiveRegistration = cancellationToken.Register(() =>
                {
                    // this check makes sure that promiseJSHandle is still valid handle
                    if (!jsTask.IsCompleted)
                    {
                        JavaScript.Runtime.CancelPromise(promiseJSHandle);
                    }
                }))
                {
                    return await jsTask.ConfigureAwait(true);
                }
            }
            catch (JSException ex)
            {
                if (State == WebSocketState.Aborted)
                {
                    throw new OperationCanceledException(nameof(WebSocketState.Aborted), ex);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _state = WebSocketState.Aborted;
                    throw new OperationCanceledException(cancellationToken);
                }
                if (ex.Message == "OperationCanceledException")
                {
                    _state = WebSocketState.Aborted;
                    throw new OperationCanceledException("The operation was cancelled.", ex, cancellationToken);
                }
                if (previousState == WebSocketState.Connecting)
                {
                    throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private WebSocketState GetReadyState()
        {
            int readyState = (int)_innerWebSocket!.GetObjectProperty("readyState");

            // https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/readyState
            return readyState switch
            {
                0 => WebSocketState.Connecting, // 0 (CONNECTING)
                1 => WebSocketState.Open, // 1 (OPEN)
                2 => WebSocketState.CloseSent, // 2 (CLOSING)
                3 => WebSocketState.Closed, // 3 (CLOSED)
                _ => WebSocketState.None
            };
        }
    }
}
