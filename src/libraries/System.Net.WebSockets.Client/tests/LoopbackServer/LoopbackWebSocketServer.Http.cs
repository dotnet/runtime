// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public static partial class LoopbackWebSocketServer
    {
        abstract class HttpRunner
        {
            protected abstract LoopbackServerFactory ServerFactory { get; }
            protected abstract GenericLoopbackOptions CreateOptions(bool useSsl);

            public async Task RunAsync(
                        Func<Uri, Task> clientFunc,
                        Func<WebSocketRequestData, CancellationToken, Task> wsServerFunc,
                        Options options,
                        CancellationToken clientExitCt,
                        CancellationToken globalCt)
            {
                using (var server = ServerFactory.CreateServer(CreateOptions(options.UseSsl)))
                {
                    Task clientTask = clientFunc(server.Address);
                    Task serverTask = ProcessWebSocketRequest(server, wsServerFunc, options, clientExitCt, globalCt);

                    await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed(LoopbackServerFactory.LoopbackServerTimeoutMilliseconds);
                }
            }

            private async Task ProcessWebSocketRequest(
                GenericLoopbackServer httpServer,
                Func<WebSocketRequestData, CancellationToken, Task> wsServerFunc,
                Options options,
                CancellationToken clientExitCt,
                CancellationToken globalCt)
            {
                try
                {
                    using CancellationTokenSource linkedCts =
                        CancellationTokenSource.CreateLinkedTokenSource(globalCt, clientExitCt);
                    await ProcessWebSocketRequestCore(httpServer, wsServerFunc, options, linkedCts.Token);
                }
                catch (Exception e) when (options.IgnoreServerErrors)
                {
                    if (e is OperationCanceledException && clientExitCt.IsCancellationRequested)
                    {
                        return; // expected
                    }

                    if (e is WebSocketException or IOException or SocketException)
                    {
                        return; // ignore
                    }

                    throw; // don't swallow Assert failures and unexpected exceptions
                }
            }

            protected abstract Task ProcessWebSocketRequestCore(
                GenericLoopbackServer httpServer,
                Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
                Options options,
                CancellationToken cancellationToken);
        }

        class Http11Runner : HttpRunner
        {
            public static HttpRunner Singleton { get; } = new Http11Runner();
            protected override LoopbackServerFactory ServerFactory => Http11LoopbackServerFactory.Singleton;

            protected override GenericLoopbackOptions CreateOptions(bool useSsl) => new LoopbackServer.Options
            {
                UseSsl = useSsl,
                WebSocketEndpoint = true
            };

            protected override Task ProcessWebSocketRequestCore(GenericLoopbackServer s, Func<WebSocketRequestData, CancellationToken, Task> func, Options o, CancellationToken ct)
                => ProcessHttp11WebSocketRequest((LoopbackServer)s, func, o, ct);
        }

        class Http2Runner : HttpRunner
        {
            public static HttpRunner Singleton { get; } = new Http2Runner();
            protected override LoopbackServerFactory ServerFactory => Http2LoopbackServerFactory.Singleton;

            protected override GenericLoopbackOptions CreateOptions(bool useSsl) => new Http2Options
            {
                UseSsl = useSsl,
                WebSocketEndpoint = true,
                EnsureThreadSafeIO = true
            };

            protected override Task ProcessWebSocketRequestCore(GenericLoopbackServer s, Func<WebSocketRequestData, CancellationToken, Task> func, Options o, CancellationToken ct)
                => ProcessHttp2WebSocketRequest((Http2LoopbackServer)s, func, o, ct);
        }

        private static Task ProcessHttp11WebSocketRequest(
            LoopbackServer http11server,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken cancellationToken)
            => http11server.AcceptConnectionAsync(
                async connection =>
                {
                    var requestData = await WebSocketHandshakeHelper.ProcessHttp11RequestAsync(
                        connection,
                        options.SkipServerHandshakeResponse,
                        options.ParseEchoOptions,
                        cancellationToken).ConfigureAwait(false);

                    await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);
                });

        private static async Task ProcessHttp2WebSocketRequest(
            Http2LoopbackServer http2Server,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken cancellationToken)
        {
            var requestData = await WebSocketHandshakeHelper.ProcessHttp2RequestAsync(
                http2Server,
                options.SkipServerHandshakeResponse,
                options.ParseEchoOptions,
                cancellationToken).ConfigureAwait(false);

            await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);
            var http2Connection = requestData.Http2Connection!;

            if (options.AbortServerOnClientExit) // we need to wait for the client to disconnect
            {
                // Due to the way Extended CONNECT is implemented, we might receive both EOS and RST_STREAM frames,
                // so we might need to drain more than 1 frame before shutting down the connection
                while (true)
                {
                    var frame = await http2Connection.ReadFrameAsync(cancellationToken).ConfigureAwait(false);
                    if (frame is null)
                    {
                        // No more frames to read
                        break;
                    }

                    if (!options.IgnoreServerErrors)
                    {
                        Assert.False(frame.Type == FrameType.Data && !((DataFrame)frame).EndStreamFlag, $"Unexpected DATA frame: {frame}");
                    }
                }

                await http2Connection.WaitForConnectionShutdownAsync(options.IgnoreServerErrors).ConfigureAwait(false);
            }
            else
            {
                await http2Connection.ShutdownIgnoringErrorsAsync(requestData.Http2StreamId.Value).ConfigureAwait(false);
            }
        }

        private static Task SendNegotiatedServerResponseAsync(WebSocketRequestData data, Options options, CancellationToken cancellationToken)
        {
            Assert.True(options.SkipServerHandshakeResponse);

            if (data.HttpVersion == HttpVersion.Version11)
            {
                return WebSocketHandshakeHelper.SendHttp11ServerResponseAsync(
                    data.Http11Connection!,
                    data.Headers["Sec-WebSocket-Key"],
                    options.ServerSubProtocol,
                    options.ServerExtensions,
                    cancellationToken);
            }

            if (data.HttpVersion == HttpVersion.Version20)
            {
                return WebSocketHandshakeHelper.SendHttp2ServerResponseAsync(
                    data.Http2Connection!,
                    data.Http2StreamId!.Value,
                    options.ServerSubProtocol,
                    options.ServerExtensions,
                    cancellationToken: cancellationToken);
            }

            throw new NotSupportedException($"HTTP version {data.HttpVersion} is not supported.");
        }
    }
}
