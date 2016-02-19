// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    public sealed partial class ClientWebSocket : WebSocket
    {
        private enum InternalState
        {
            Created = 0,
            Connecting = 1,
            Connected = 2,
            Disposed = 3
        }

        private readonly ClientWebSocketOptions _options;
        private WebSocketHandle _innerWebSocket;
        private readonly CancellationTokenSource _cts;

        // NOTE: this is really an InternalState value, but Interlocked doesn't support
        //       operations on values of enum types.
        private int _state;

        public ClientWebSocket()
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.WebSocket, this, ".ctor", null);
            }

            WebSocketHandle.CheckPlatformSupport();

            _state = (int)InternalState.Created;
            _options = new ClientWebSocketOptions();
            _cts = new CancellationTokenSource();

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.WebSocket, this, ".ctor", null);
            }
        }

        #region Properties

        public ClientWebSocketOptions Options
        {
            get
            {
                return _options;
            }
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                if (_innerWebSocket.IsValid)
                {
                    return _innerWebSocket.CloseStatus;
                }
                return null;
            }
        }

        public override string CloseStatusDescription
        {
            get
            {
                if (_innerWebSocket.IsValid)
                {
                    return _innerWebSocket.CloseStatusDescription;
                }
                return null;
            }
        }

        public override string SubProtocol
        {
            get
            {
                if (_innerWebSocket.IsValid)
                {
                    return _innerWebSocket.SubProtocol;
                }
                return null;
            }
        }

        public override WebSocketState State
        {
            get
            {
                // state == Connected or Disposed
                if (_innerWebSocket.IsValid)
                {
                    return _innerWebSocket.State;
                }
                switch ((InternalState)_state)
                {
                    case InternalState.Created:
                        return WebSocketState.None;
                    case InternalState.Connecting:
                        return WebSocketState.Connecting;
                    default: // We only get here if disposed before connecting
                        Debug.Assert((InternalState)_state == InternalState.Disposed);
                        return WebSocketState.Closed;
                }
            }
        }

        #endregion Properties

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.net_uri_NotAbsolute, nameof(uri));
            }
            if (uri.Scheme != UriScheme.Ws && uri.Scheme != UriScheme.Wss)
            {
                throw new ArgumentException(SR.net_WebSockets_Scheme, nameof(uri));
            }

            // Check that we have not started already
            var priorState = (InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connecting, (int)InternalState.Created);
            if (priorState == InternalState.Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (priorState != InternalState.Created)
            {
                throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }
            _options.SetToReadOnly();

            return ConnectAsyncCore(uri, cancellationToken);
        }

        private async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
        {
            _innerWebSocket = WebSocketHandle.Create();

            try
            {
                // Change internal state to 'connected' to enable the other methods
                if ((InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connected, (int)InternalState.Connecting) != InternalState.Connecting)
                {
                    // Aborted/Disposed during connect.
                    throw new ObjectDisposedException(GetType().FullName);
                }

                await _innerWebSocket.ConnectAsyncCore(uri, cancellationToken, _options).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Exception(NetEventSource.ComponentType.WebSocket, this, "ConnectAsync", ex);
                }
                throw;
            }
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            if (!((messageType == WebSocketMessageType.Text) || (messageType == WebSocketMessageType.Binary)))
            {
                string errorMessage = SR.Format(
                        SR.net_WebSockets_Argument_InvalidMessageType,
                        "Close",
                        "SendAsync",
                        "Binary",
                        "Text",
                        "CloseOutputAsync");

                throw new ArgumentException(errorMessage, nameof(messageType));
            }

            WebSocketValidate.ValidateArraySegment<byte>(buffer, "buffer");
            return _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            WebSocketValidate.ValidateArraySegment<byte>(buffer, "buffer");
            return _innerWebSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return _innerWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return _innerWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Abort()
        {
            if ((InternalState)_state == InternalState.Disposed)
            {
                return;
            }
            if (_innerWebSocket.IsValid)
            {
                _innerWebSocket.Abort();
            }
            Dispose();
        }

        public override void Dispose()
        {
            var priorState = (InternalState)Interlocked.Exchange(ref _state, (int)InternalState.Disposed);
            if (priorState == InternalState.Disposed)
            {
                // No cleanup required.
                return;
            }
            _cts.Cancel(false);
            _cts.Dispose();
            if (_innerWebSocket.IsValid)
            {
                _innerWebSocket.Dispose();
            }
        }

        private void ThrowIfNotConnected()
        {
            if ((InternalState)_state == InternalState.Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if ((InternalState)_state != InternalState.Connected)
            {
                throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
            }
        }
    }
}
