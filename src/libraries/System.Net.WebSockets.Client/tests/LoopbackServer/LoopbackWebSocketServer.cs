// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public static partial class LoopbackWebSocketServer
    {
        public static Task RunAsync(
            Func<ClientWebSocket, CancellationToken, Task> clientWebSocketFunc,
            Func<WebSocket, CancellationToken, Task> serverWebSocketFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            Assert.False(options.SkipServerHandshakeResponse, "Required to create ClientWebSocket");

            return RunAsyncPrivate(
                uri => RunClientWebSocketAsync(uri, clientWebSocketFunc, options, cancellationToken),
                (requestData, token) => RunServerWebSocketAsync(requestData, serverWebSocketFunc, options, token),
                options,
                cancellationToken);
        }

        public static Task RunAsync(
            Func<Uri, Task> loopbackClientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            Assert.False(options.DisposeClientWebSocket, "ClientWebSocket is not created in this overload");
            Assert.False(options.DisposeServerWebSocket, "ServerWebSocket is not created in this overload");
            Assert.False(options.DisposeHttpInvoker, "HttpInvoker is not used in this overload");
            Assert.Null(options.HttpInvoker); // Not supported in this overload

            return RunAsyncPrivate(loopbackClientFunc, loopbackServerFunc, options, cancellationToken);
        }

        private static Task RunAsyncPrivate(
            Func<Uri, Task> loopbackClientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken globalCt)
        {
            if (!options.AbortServerOnClientExit)
            {
                return RunClientAndHttpServerAsync(
                    loopbackClientFunc, loopbackServerFunc, options, CancellationToken.None, globalCt);
            }

            CancellationTokenSource clientExitCts = new CancellationTokenSource();

            return RunClientAndHttpServerAsync(
                async uri =>
                {
                    try
                    {
                        await loopbackClientFunc(uri);
                    }
                    finally
                    {
                        clientExitCts.Cancel();
                    }
                },
                loopbackServerFunc,
                options,
                clientExitCts.Token,
                globalCt);
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

        private static async Task RunClientWebSocketAsync(
            Uri uri,
            Func<ClientWebSocket, CancellationToken, Task> clientWebSocketFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            var clientWebSocket = await GetConnectedClientAsync(uri, options, cancellationToken).ConfigureAwait(false);

            await clientWebSocketFunc(clientWebSocket, cancellationToken).ConfigureAwait(false);

            if (options.DisposeClientWebSocket)
            {
                clientWebSocket.Dispose();
            }

            if (options.DisposeHttpInvoker)
            {
                options.HttpInvoker?.Dispose();
            }
        }

        public static async Task<ClientWebSocket> GetConnectedClientAsync(Uri uri, Options options, CancellationToken cancellationToken)
        {
            var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.HttpVersion = options.HttpVersion;
            clientWebSocket.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            if (options.UseSsl && options.HttpInvoker is null)
            {
                clientWebSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };
            }

            options.ConfigureClientOptions?.Invoke(clientWebSocket.Options);

            await clientWebSocket.ConnectAsync(uri, options.HttpInvoker, cancellationToken).ConfigureAwait(false);

            return clientWebSocket;
        }

        public record class Options()
        {
            public Version HttpVersion { get; init; }
            public bool UseSsl { get; init; }
            public HttpMessageInvoker? HttpInvoker { get; init; }

            public bool DisposeServerWebSocket { get; set; }
            public bool SkipServerHandshakeResponse { get; set; }
            public bool ParseEchoOptions { get; set; }
            public bool IgnoreServerErrors { get; set; }
            public bool AbortServerOnClientExit { get; set; }
            public string? ServerSubProtocol { get; set; }
            public string? ServerExtensions { get; set; }

            public bool DisposeClientWebSocket { get; set; }
            public bool DisposeHttpInvoker { get; set; }
            public Action<ClientWebSocketOptions>? ConfigureClientOptions { get; set; }

            public ITestOutputHelper? Output { get; set; }
        }
    }
}
