// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHandle
    {
        private WebSocketState _state = WebSocketState.Connecting;
#pragma warning disable CA1822 // Mark members as static
        public HttpStatusCode HttpStatusCode => (HttpStatusCode)0;
#pragma warning restore CA1822 // Mark members as static

        public IReadOnlyDictionary<string, IEnumerable<string>>? HttpResponseHeaders { get; set; }

        public WebSocket? WebSocket { get; private set; }
        public WebSocketState State => WebSocket?.State ?? _state;

        public static ClientWebSocketOptions CreateDefaultOptions() => new ClientWebSocketOptions();

        public void Dispose()
        {
            _state = WebSocketState.Closed;
            WebSocket?.Dispose();
        }

        public void Abort()
        {
            _state = WebSocketState.Aborted;
            WebSocket?.Abort();
        }

        public Task ConnectAsync(Uri uri, HttpMessageInvoker? invoker, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ws = new BrowserWebSocket();
            WebSocket = ws;
            return ws.ConnectAsync(uri, options.RequestedSubProtocols, cancellationToken);
        }
    }
}
