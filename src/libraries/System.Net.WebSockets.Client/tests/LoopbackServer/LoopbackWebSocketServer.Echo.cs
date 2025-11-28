// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public static partial class LoopbackWebSocketServer
    {
        public static Task RunEchoAsync(Func<Uri, Task> loopbackClientFunc, Options options, CancellationToken cancellationToken)
        {
            Assert.True(options.IgnoreServerErrors);
            Assert.True(options.AbortServerOnClientExit);

            return RunAsync(
                loopbackClientFunc,
                (data, token) => RunEchoServerWebSocketAsync(data, options, token),
                options,
                cancellationToken);
        }

        private static async Task RunEchoServerWebSocketAsync(WebSocketRequestData data, Options options, CancellationToken cancellationToken)
        {
            WebSocketEchoOptions echoOptions = data.EchoOptions ?? WebSocketEchoOptions.Default;

            if (echoOptions.SubProtocol is not null)
            {
                Assert.True(options.SkipServerHandshakeResponse, "SkipServerHandshakeResponse must be true to negotiate subprotocols");
                Assert.Null(options.ServerSubProtocol);
                options = options with { ServerSubProtocol = echoOptions.SubProtocol };
            }

            if (options.SkipServerHandshakeResponse)
            {
                await SendNegotiatedServerResponseAsync(data, options, cancellationToken).ConfigureAwait(false);
            }

            await RunServerWebSocketAsync(
                data,
                (serverWebSocket, token) => WebSocketEchoHelper.RunEchoAll(
                    serverWebSocket,
                    echoOptions.ReplyWithPartialMessages,
                    echoOptions.ReplyWithEnhancedCloseMessage,
                    token),
                options,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
