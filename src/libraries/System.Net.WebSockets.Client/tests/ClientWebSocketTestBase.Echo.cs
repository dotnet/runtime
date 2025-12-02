// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ClientWebSocketTestBase
    {
        protected Task RunEchoAsync(Func<Uri, Task> clientFunc, bool useSsl)
        {
            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);
            var options = new LoopbackWebSocketServer.Options(HttpVersion, useSsl)
            {
                SkipServerHandshakeResponse = true,
                IgnoreServerErrors = true,
                AbortServerOnClientExit = true,
                ParseEchoOptions = true
            };

            return LoopbackWebSocketServer.RunEchoAsync(clientFunc, options, timeoutCts.Token);
        }

        protected Task RunEchoHeadersAsync(Func<Uri, Task> clientFunc, bool useSsl)
        {
            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);
            var options = new LoopbackWebSocketServer.Options(HttpVersion, useSsl)
            {
                IgnoreServerErrors = true,
                AbortServerOnClientExit = true
            };

            return LoopbackWebSocketServer.RunAsync(
                clientFunc,
                async (requestData, token) =>
                {
                    var serverWebSocket = WebSocket.CreateFromStream(
                        requestData.TransportStream,
                        new WebSocketCreationOptions { IsServer = true });

                    using var registration = token.Register(serverWebSocket.Abort);
                    await WebSocketEchoHelper.RunEchoHeaders(serverWebSocket, requestData.Headers, token);
                },
                options,
                timeoutCts.Token);
        }
    }
}
