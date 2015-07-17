// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    public abstract class WebSocket : IDisposable
    {
        public abstract WebSocketCloseStatus? CloseStatus { get; }
        public abstract string CloseStatusDescription { get; }
        public abstract string SubProtocol { get; }
        public abstract WebSocketState State { get; }

        public abstract void Abort();
        public abstract Task CloseAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken);
        public abstract Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken);
        public abstract void Dispose();
        public abstract Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken);
        public abstract Task SendAsync(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken);
    }
}
