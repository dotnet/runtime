// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets.Client.Tests
{
    public static partial class LoopbackWebSocketServer
    {
        public static Task RunAsync(
            Func<Uri, Task> runClient,
            Func<WebSocket, CancellationToken, Task> runServer,
            Options options,
            CancellationToken cancellationToken)
                => RunAsync(
                    runClient,
                    (rd, ct) => RunServerWebSocketAsync(rd, runServer, options, ct),
                    options,
                    cancellationToken);

        public static Task RunAsync(
            Func<Uri, Task> runClient,
            Func<WebSocketRequestData, CancellationToken, Task> runServer,
            Options options,
            CancellationToken cancellationToken)
        {
            CancellationToken clientExitCt = CancellationToken.None;

            if (options.AbortServerOnClientExit)
            {
                CancellationTokenSource clientExitCts = new CancellationTokenSource();
                clientExitCt = clientExitCts.Token;

                var runClientCore = runClient;
                runClient = async uri =>
                {
                    try
                    {
                        await runClientCore(uri);
                    }
                    finally
                    {
                        clientExitCts.Cancel();
                    }
                };
            }

            var httpRunner = options.HttpVersion.Major switch
            {
                1 => Http11Runner.Singleton,
                2 => Http2Runner.Singleton,
                _ => throw new NotSupportedException($"HTTP version {options.HttpVersion} is not supported.")
            };

            return httpRunner.RunAsync(runClient, runServer, options, clientExitCt, cancellationToken);
        }

        private static async Task RunServerWebSocketAsync(
            WebSocketRequestData requestData,
            Func<WebSocket, CancellationToken, Task> serverWebSocketFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            var wsOptions = new WebSocketCreationOptions { IsServer = true, SubProtocol = options.ServerSubProtocol };

            var serverWebSocket = WebSocket.CreateFromStream(requestData.TransportStream, wsOptions);
            using (var registration = cancellationToken.Register(serverWebSocket.Abort))
            {
                await serverWebSocketFunc(serverWebSocket, cancellationToken).ConfigureAwait(false);
            }

            if (options.DisposeServerWebSocket)
            {
                serverWebSocket.Dispose();
            }
        }

        public record class Options(Version HttpVersion, bool UseSsl)
        {
            public bool DisposeServerWebSocket { get; set; }
            public bool SkipServerHandshakeResponse { get; set; }
            public bool ParseEchoOptions { get; set; }
            public bool IgnoreServerErrors { get; set; }
            public bool AbortServerOnClientExit { get; set; }
            public string? ServerSubProtocol { get; set; }
            public string? ServerExtensions { get; set; }
        }
    }
}
