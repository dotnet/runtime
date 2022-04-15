// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// FIXME: AJ: filescoped, nullable
namespace Microsoft.WebAssembly.Diagnostics
{

    internal abstract class AbstractConnection : IDisposable
    {
        public static AbstractConnection Create(ILogger logger, TcpClient tcpClient = null, WebSocket webSocket = null)
        {
            if (tcpClient is null && webSocket is null)
                throw new ArgumentException($"Both {nameof(tcpClient)}, and {nameof(webSocket)} cannot be null");
            if (tcpClient is not null && webSocket is not null)
                throw new ArgumentException($"Both {nameof(tcpClient)}, and {nameof(webSocket)} cannot be non-null");

            return tcpClient is not null
                        ? new TcpClientConnection(tcpClient, logger)
                        : new WebSocketConnection(webSocket, logger);
        }

        public abstract DevToolsQueueBase NewQueue();
        public virtual async Task<string> ReadOne(TaskCompletionSource client_initiated_close, CancellationToken token)
                => await Task.FromResult<string>(null);

        public virtual void Dispose()
        {}
    }
}
