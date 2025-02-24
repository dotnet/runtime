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
            Assert.False(options.SkipServerHandshakeResponse, "Not supported in this overload");

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

        public static Task RunEchoAsync(Func<Uri, Task> loopbackClientFunc, Options options, CancellationToken cancellationToken)
        {
            Assert.False(options.DisposeClientWebSocket, "Not supported in this overload");
            Assert.False(options.DisposeHttpInvoker, "Not supported in this overload");
            Assert.Null(options.HttpInvoker); // Not supported in this overload

            return RunAsyncPrivate(loopbackClientFunc, (data, token) => RunEchoServerAsync(data, options, token), options, cancellationToken);
        }

        private static Task RunAsyncPrivate(
            Func<Uri, Task> loopbackClientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            if (options.HttpVersion == HttpVersion.Version11)
            {
                return LoopbackServer.CreateClientAndServerAsync(
                    loopbackClientFunc,
                    async server =>
                    {
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            var requestData = await WebSocketHandshakeHelper.ProcessHttp11RequestAsync(connection, options.SkipServerHandshakeResponse, cancellationToken).ConfigureAwait(false);
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
                        var requestData = await WebSocketHandshakeHelper.ProcessHttp2RequestAsync(server, options.SkipServerHandshakeResponse, cancellationToken).ConfigureAwait(false);
                        var http2Connection = requestData.Http2Connection!;
                        var http2StreamId = requestData.Http2StreamId.Value;

                        await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);

                        await http2Connection.ShutdownIgnoringErrorsAsync(http2StreamId).ConfigureAwait(false);
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
            WebSocket serverWebSocket = null!;
            CancellationTokenRegistration registration = default;
            try
            {
                var wsOptions = new WebSocketCreationOptions { IsServer = true };
                options.ConfigureServerOptions?.Invoke(wsOptions);

                serverWebSocket = WebSocket.CreateFromStream(requestData.TransportStream, wsOptions);
                registration = cancellationToken.Register(() => serverWebSocket.Abort());

                await serverWebSocketFunc(serverWebSocket, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (options.IgnoreServerErrors)
            {
                // ignore
            }
            finally
            {
                registration.Dispose();
                if (options.DisposeServerWebSocket)
                {
                    serverWebSocket?.Dispose();
                }
            }
        }

        private static async Task RunEchoServerAsync(WebSocketRequestData data, Options options, CancellationToken cancellationToken)
        {
            try
            {
                WebSocketEchoOptions echoOptions = await WebSocketEchoHelper.ProcessOptions(data.Query, cancellationToken);

                if (options.ConfigureServerOptions is not null && echoOptions.SubProtocol is not null)
                {
                    Options copy = options;
                    options = copy with {
                        ConfigureServerOptions = o =>
                        {
                            o.SubProtocol = echoOptions.SubProtocol;
                            copy.ConfigureServerOptions.Invoke(o);
                        }
                    };
                }

                await RunServerAsync(
                    data,
                    (serverWebSocket, token) => WebSocketEchoHelper.RunEchoAll(
                        serverWebSocket,
                        echoOptions.ReplyWithPartialMessages,
                        echoOptions.ReplyWithEnhancedCloseMessage,
                        token),
                    options,
                    cancellationToken);
            }
            catch (Exception) when (options.IgnoreServerErrors)
            {
                // ignore
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
            public bool IgnoreServerErrors { get; set; }
            public Action<WebSocketCreationOptions>? ConfigureServerOptions { get; set; }

            public bool DisposeClientWebSocket { get; set; }
            public bool DisposeHttpInvoker { get; set; }
            public Action<ClientWebSocketOptions>? ConfigureClientOptions { get; set; }
        }
    }
}
