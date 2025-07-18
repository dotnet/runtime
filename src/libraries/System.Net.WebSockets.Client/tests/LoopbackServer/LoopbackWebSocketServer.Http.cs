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
        private static Task RunClientAndHttpServerAsync(
            Func<Uri, Task> clientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken clientExitCt,
            CancellationToken globalCt)
        {
            if (options.HttpVersion == HttpVersion.Version11)
            {
                return LoopbackServer.CreateClientAndServerAsync(
                    clientFunc,
                    server => RunHttpServer(
                        ProcessHttp11WebSocketRequest, server, loopbackServerFunc, options, clientExitCt, globalCt),
                    new LoopbackServer.Options { WebSocketEndpoint = true, UseSsl = options.UseSsl });
            }

            if (options.HttpVersion == HttpVersion.Version20)
            {
                var http2Options = new Http2Options { WebSocketEndpoint = true, UseSsl = options.UseSsl, EnsureThreadSafeIO = true };

                return Http2LoopbackServer.CreateClientAndServerAsync(
                    clientFunc,
                    server => RunHttpServer(
                        ProcessHttp2WebSocketRequest, server, loopbackServerFunc, options, clientExitCt, globalCt),
                    http2Options);
            }

            throw new ArgumentException(nameof(options.HttpVersion));
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

        private static async Task RunHttpServer<THttpServer>(
            Func<THttpServer, Func<WebSocketRequestData, CancellationToken, Task>, Options, CancellationToken, Task> httpServerFunc,
            THttpServer httpServer,
            Func<WebSocketRequestData, CancellationToken, Task> wsServerFunc,
            Options options,
            CancellationToken clientExitCt,
            CancellationToken globalCt)
            where THttpServer : GenericLoopbackServer
        {
            try
            {
                using CancellationTokenSource linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(globalCt, clientExitCt);

                await httpServerFunc(httpServer, wsServerFunc, options, linkedCts.Token)
                    .WaitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (Exception e) when (options.IgnoreServerErrors)
            {
                if (e is OperationCanceledException && clientExitCt.IsCancellationRequested)
                {
                    return; // expected for aborting on client exit
                }

                if (e is WebSocketException we)
                {
                    if (we.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        return; // expected for aborting on client exit
                    }

                    if (we.WebSocketErrorCode == WebSocketError.InvalidState)
                    {
                        const string closeOnClosedMsg = "The WebSocket is in an invalid state ('Closed') for this operation. Valid states are: 'Open, CloseSent, CloseReceived'";
                        const string closeOnAbortedMsg = "The WebSocket is in an invalid state ('Aborted') for this operation. Valid states are: 'Open, CloseSent, CloseReceived'";
                        if (we.Message == closeOnClosedMsg || we.Message == closeOnAbortedMsg)
                        {
                            return; // expected (Close on a closed WebSocket is not no-op: see https://github.com/dotnet/runtime/issues/22000)
                        }
                    }

                    options.Output?.WriteLine($"[WARN] Server aborted on a WebSocketException ({we.WebSocketErrorCode}): {we}");
                    return; // ignore
                }

                if (e is IOException or SocketException)
                {
                    return; // ignore
                }

                throw; // don't swallow Assert failures and unexpected exceptions
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
