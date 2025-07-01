// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public static partial class LoopbackWebSocketServer
    {
        public static Task RunEchoAsync(Func<Uri, Task> loopbackClientFunc, Version httpVersion, bool useSsl, int timeOutMilliseconds, ITestOutputHelper output)
        {
            var timeoutCts = new CancellationTokenSource(timeOutMilliseconds);
            var options = new Options
            {
                HttpVersion = httpVersion,
                UseSsl = useSsl,
                SkipServerHandshakeResponse = true, // to negotiate subprotocols and extensions
                IgnoreServerErrors = true,
                AbortServerOnClientExit = true,
                ParseEchoOptions = true,
                TestOutputHelper = output
            };

            //output.WriteLine("[Common - Run Echo WS] RunEchoAsync called with options: " + options);

            return RunAsyncPrivate(
                loopbackClientFunc,
                (data, token) => RunEchoServerWebSocketAsync(data, options, token),
                options,
                timeoutCts.Token);
        }

        private static async Task RunEchoServerWebSocketAsync(WebSocketRequestData data, Options options, CancellationToken cancellationToken)
        {
            //options.Logger?.WriteLine($"[Server - Run Echo WS] Starting...");

            Assert.NotNull(data.EchoOptions);
            WebSocketEchoOptions echoOptions = data.EchoOptions.Value;

            if (echoOptions.SubProtocol is not null)
            {
                Assert.Null(options.ServerSubProtocol);
                options = options with { ServerSubProtocol = echoOptions.SubProtocol };
            }

            await SendNegotiatedServerResponseAsync(data, options, cancellationToken).ConfigureAwait(false);

            //options.Logger?.WriteLine($"[Server - Run Echo WS] Connection established");

            var webSocket = await RunServerWebSocketAsync(
                data,
                (serverWebSocket, token) => WebSocketEchoHelper.RunEchoAll(
                    serverWebSocket,
                    echoOptions.ReplyWithPartialMessages,
                    echoOptions.ReplyWithEnhancedCloseMessage,
                    options.Logger,
                    token),
                options,
                cancellationToken).ConfigureAwait(false);

            //options.Logger?.WriteLine($"[Server - Run Echo WS] Completed; Server WebSocket state: {webSocket.State}");
        }
    }
}
