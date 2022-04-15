// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class WebSocketConnection : AbstractConnection
    {
        public WebSocket WebSocket { get; init; }
        private readonly ILogger _logger;

        public WebSocketConnection(WebSocket webSocket, ILogger logger)
        {
            WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override DevToolsQueueBase NewQueue() => new DevToolsQueue(WebSocket);

        public override async Task<string> ReadOne(TaskCompletionSource client_initiated_close, CancellationToken token)
        {
            byte[] buff = new byte[4000];
            var mem = new MemoryStream();
            try
            {
                while (true)
                {
                    if (WebSocket.State != WebSocketState.Open)
                    {
                        _logger.LogError($"DevToolsProxy: Socket is no longer open.");
                        client_initiated_close.TrySetResult();
                        return null;
                    }

                    WebSocketReceiveResult result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buff), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        client_initiated_close.TrySetResult();
                        return null;
                    }

                    mem.Write(buff, 0, result.Count);

                    if (result.EndOfMessage)
                        return Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Length);
                }
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    client_initiated_close.TrySetResult();
                    return null;
                }
            }
            return null;
        }

        public override void Dispose()
        {
            WebSocket.Dispose();
            base.Dispose();
        }
    }
}
