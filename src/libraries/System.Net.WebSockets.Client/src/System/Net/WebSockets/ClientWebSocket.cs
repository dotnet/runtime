// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    public sealed partial class ClientWebSocket : WebSocket
    {
        /// <summary>This is really an InternalState value, but Interlocked doesn't support operations on values of enum types.</summary>
        private int _state;
        private WebSocketHandle? _innerWebSocket;

        public ClientWebSocket()
        {
            _state = (int)InternalState.Created;
            Options = WebSocketHandle.CreateDefaultOptions();
        }

        public ClientWebSocketOptions Options { get; }

        public override WebSocketCloseStatus? CloseStatus => _innerWebSocket?.WebSocket?.CloseStatus;

        public override string? CloseStatusDescription => _innerWebSocket?.WebSocket?.CloseStatusDescription;

        public override string? SubProtocol => _innerWebSocket?.WebSocket?.SubProtocol;

        public override WebSocketState State
        {
            get
            {
                // state == Connected or Disposed
                if (_innerWebSocket != null)
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

        /// <summary>
        /// Gets the upgrade response status code if <see cref="ClientWebSocketOptions.CollectHttpResponseDetails" /> is set.
        /// </summary>
        public System.Net.HttpStatusCode HttpStatusCode => _innerWebSocket?.HttpStatusCode ?? 0;

        /// <summary>
        /// Gets the upgrade response headers if <see cref="ClientWebSocketOptions.CollectHttpResponseDetails" /> is set.
        /// The setter may be used to reduce the memory usage of an active WebSocket connection once headers are no longer needed.
        /// </summary>
        public IReadOnlyDictionary<string, IEnumerable<string>>? HttpResponseHeaders
        {
            get => _innerWebSocket?.HttpResponseHeaders;
            set
            {
                if (_innerWebSocket != null)
                {
                    _innerWebSocket.HttpResponseHeaders = value;
                }
            }
        }

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            return ConnectAsync(uri, null, cancellationToken);
        }

        public Task ConnectAsync(Uri uri, HttpMessageInvoker? invoker, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(uri);

            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.net_uri_NotAbsolute, nameof(uri));
            }
            if (uri.Scheme != UriScheme.Ws && uri.Scheme != UriScheme.Wss)
            {
                throw new ArgumentException(SR.net_WebSockets_Scheme, nameof(uri));
            }

            // Check that we have not started already.
            switch ((InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connecting, (int)InternalState.Created))
            {
                case InternalState.Disposed:
                    throw new ObjectDisposedException(GetType().FullName);

                case InternalState.Created:
                    break;

                default:
                    throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }

            Options.SetToReadOnly();
            return ConnectAsyncCore(uri, invoker, cancellationToken);
        }

        private async Task ConnectAsyncCore(Uri uri, HttpMessageInvoker? invoker, CancellationToken cancellationToken)
        {
            _innerWebSocket = new WebSocketHandle();

            try
            {
                await _innerWebSocket.ConnectAsync(uri, invoker, cancellationToken, Options).ConfigureAwait(false);
            }
            catch
            {
                Dispose();
                throw;
            }

            if ((InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connected, (int)InternalState.Connecting) != InternalState.Connecting)
            {
                Debug.Assert(_state == (int)InternalState.Disposed);
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            ConnectedWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            ConnectedWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            ConnectedWebSocket.ReceiveAsync(buffer, cancellationToken);

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            ConnectedWebSocket.ReceiveAsync(buffer, cancellationToken);

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            ConnectedWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            ConnectedWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

        private WebSocket ConnectedWebSocket
        {
            get
            {
                ObjectDisposedException.ThrowIf((InternalState)_state == InternalState.Disposed, this);

                if ((InternalState)_state != InternalState.Connected)
                {
                    throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
                }

                Debug.Assert(_innerWebSocket != null);
                Debug.Assert(_innerWebSocket.WebSocket != null);

                return _innerWebSocket.WebSocket;
            }
        }

        public override void Abort()
        {
            if ((InternalState)_state != InternalState.Disposed)
            {
                _innerWebSocket?.Abort();
                Dispose();
            }
        }

        public override void Dispose()
        {
            if ((InternalState)Interlocked.Exchange(ref _state, (int)InternalState.Disposed) != InternalState.Disposed)
            {
                _innerWebSocket?.Dispose();
            }
        }

        private enum InternalState
        {
            Created = 0,
            Connecting = 1,
            Connected = 2,
            Disposed = 3
        }
    }
}
