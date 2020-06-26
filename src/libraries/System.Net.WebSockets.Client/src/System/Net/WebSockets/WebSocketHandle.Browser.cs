// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHandle
    {
        private WebSocketState _state = WebSocketState.Connecting;

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
            WebSocket?.Abort();
        }

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            // TODO: Implement this to connect and set WebSocket to a working instance.
            return Task.FromException(new PlatformNotSupportedException());
        }
    }
}
