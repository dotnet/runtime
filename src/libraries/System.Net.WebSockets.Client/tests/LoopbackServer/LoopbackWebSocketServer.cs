// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public static class LoopbackWebSocketServer
    {
        public static Task RunAsync(
            Func<ClientWebSocket, CancellationToken, Task> clientWebSocketFunc,
            Func<WebSocket, CancellationToken, Task> serverWebSocketFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            Assert.False(options.ManualServerHandshakeResponse, "Not supported in this overload");

            return RunAsyncPrivate(
                uri => RunClientAsync(uri, clientWebSocketFunc, options, cancellationToken),
                (requestData, token) => RunServerAsync(requestData, serverWebSocketFunc, options, token),
                options,
                cancellationToken);
        }

        public static Task RunAsync(
            Func<Uri, Task> loopbackClientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            Assert.False(options.DisposeClientWebSocket, "Not supported in this overload");
            Assert.False(options.DisposeServerWebSocket, "Not supported in this overload");
            Assert.False(options.DisposeHttpInvoker, "Not supported in this overload");
            Assert.Null(options.HttpInvoker); // Not supported in this overload

            return RunAsyncPrivate(loopbackClientFunc, loopbackServerFunc, options, cancellationToken);
        }

        private static Task RunAsyncPrivate(
            Func<Uri, Task> loopbackClientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            bool sendDefaultServerHandshakeResponse = !options.ManualServerHandshakeResponse;
            if (options.HttpVersion == HttpVersion.Version11)
            {
                return LoopbackServer.CreateClientAndServerAsync(
                    loopbackClientFunc,
                    async server =>
                    {
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            var requestData = await WebSocketHandshakeHelper.ProcessHttp11RequestAsync(connection, sendDefaultServerHandshakeResponse, cancellationToken).ConfigureAwait(false);
                            await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);
                        });
                    },
                    new LoopbackServer.Options { WebSocketEndpoint = true, UseSsl = options.UseSsl });
            }
            else if (options.HttpVersion == HttpVersion.Version20)
            {
                return Http2LoopbackServer.CreateClientAndServerAsync(
                    loopbackClientFunc,
                    async server =>
                    {
                        var requestData = await WebSocketHandshakeHelper.ProcessHttp2RequestAsync(server, sendDefaultServerHandshakeResponse, cancellationToken).ConfigureAwait(false);
                        var http2Connection = requestData.Http2Connection!;
                        var http2StreamId = requestData.Http2StreamId.Value;

                        await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);

                        await http2Connection.DisposeAsync().ConfigureAwait(false);
                    },
                    new Http2Options { WebSocketEndpoint = true, UseSsl = options.UseSsl });
            }
            else
            {
                throw new ArgumentException(nameof(options.HttpVersion));
            }
        }

        private static async Task RunServerAsync(
            WebSocketRequestData requestData,
            Func<WebSocket, CancellationToken, Task> serverWebSocketFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            var wsOptions = new WebSocketCreationOptions { IsServer = true };
            var serverWebSocket = WebSocket.CreateFromStream(requestData.WebSocketStream, wsOptions);

            await serverWebSocketFunc(serverWebSocket, cancellationToken).ConfigureAwait(false);

            if (options.DisposeServerWebSocket)
            {
                serverWebSocket.Dispose();
            }
        }

        private static async Task RunClientAsync(
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

            await clientWebSocket.ConnectAsync(uri, options.HttpInvoker, cancellationToken).ConfigureAwait(false);

            return clientWebSocket;
        }

        public record class Options(Version HttpVersion, bool UseSsl, HttpMessageInvoker? HttpInvoker)
        {
            public bool DisposeServerWebSocket { get; set; } = true;
            public bool DisposeClientWebSocket { get; set; }
            public bool DisposeHttpInvoker { get; set; }
            public bool ManualServerHandshakeResponse { get; set; }
        }
    }
}
