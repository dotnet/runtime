// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http;
using System.Net.Sockets;
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

        public static Task RunEchoAsync(Func<Uri, Task> loopbackClientFunc, Version httpVersion, bool useSsl, int timeOutMilliseconds)
        {
            var timeoutCts = new CancellationTokenSource(timeOutMilliseconds);
            var options = new Options
            {
                HttpVersion = httpVersion,
                UseSsl = useSsl,
                IgnoreServerErrors = true,
                AbortServerOnClientExit = true,
                ParseEchoOptions = true
            };

            //Console.WriteLine($"[{nameof(RunEchoAsync)}] Starting Echo test");

            return RunAsyncPrivate(
                loopbackClientFunc,
                (data, token) => RunEchoServerAsync(data, options, token),
                options,
                timeoutCts.Token);
        }

        private static Task RunAsyncPrivate(
            Func<Uri, Task> loopbackClientFunc,
            Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc,
            Options options,
            CancellationToken globalCt)
        {
            Func<Uri, Task> clientFunc;
            CancellationToken cancellationToken;
            CancellationToken clientExitCt;

            if (options.AbortServerOnClientExit)
            {
                //Console.WriteLine($"[{nameof(RunAsyncPrivate)}] AbortServerOnClientExit=true");
                CancellationTokenSource clientExitCts = new CancellationTokenSource();
                clientExitCt = clientExitCts.Token;
                cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(globalCt, clientExitCt).Token;

                clientFunc = async uri =>
                {
                    //Console.WriteLine($"[Client - {nameof(clientFunc)}] clientExitCt canceled={clientExitCt.IsCancellationRequested}, cancellationToken canceled={cancellationToken.IsCancellationRequested}");
                    try
                    {
                        //Console.WriteLine($"[Client - {nameof(clientFunc)}] Starting client");
                        await loopbackClientFunc(uri).ConfigureAwait(false);
                        //Console.WriteLine($"[Client - {nameof(clientFunc)}] Client completed SUCCESSFULLY");
                    }
                    //catch (Exception ex)
                    //{
                        //Console.WriteLine($"[Client - {nameof(clientFunc)}] Client FAILED with exception: {ex.Message}");
                    //    throw;
                    //}
                    finally
                    {
                        clientExitCts.Cancel();
                        //Console.WriteLine($"[Client - {nameof(clientFunc)}] clientExitCts cancelled (clientExitCt canceled={clientExitCt.IsCancellationRequested}, cancellationToken canceled={cancellationToken.IsCancellationRequested})");
                    }
                };
            }
            else
            {
                //Console.WriteLine($"[{nameof(RunAsyncPrivate)}] AbortServerOnClientExit=false");
                clientFunc = loopbackClientFunc;
                cancellationToken = globalCt;
                clientExitCt = CancellationToken.None;
            }

            async Task RunHttpServer<TServer>(
                TServer server,
                Func<TServer, Func<WebSocketRequestData, CancellationToken, Task>, Options, CancellationToken, Task> serverFunc
            ) where TServer : GenericLoopbackServer
            {
                try
                {
                    //Console.WriteLine($"[Server - {nameof(RunHttpServer)}<{typeof(TServer).Name}>] Starting server");
                    await Task.Run(() =>
                        serverFunc(server, loopbackServerFunc, options, cancellationToken),
                        cancellationToken);
                    //Console.WriteLine($"[Server - {nameof(RunHttpServer)}<{typeof(TServer).Name}>] Server completed SUCCESSFULLY");
                }
                catch (OperationCanceledException) when (options.AbortServerOnClientExit && clientExitCt.IsCancellationRequested) { } // expected
                catch (WebSocketException we) when (options.AbortServerOnClientExit && we.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) { } // expected
                catch (SocketException se) when (options.AbortServerOnClientExit && se.SocketErrorCode == SocketError.ConnectionReset) { } // expected
                catch (IOException ie) when (options.AbortServerOnClientExit && ie.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionReset) { } // expected
                catch (SocketException) when (options.IgnoreServerErrors) { } // ignore
                catch (IOException) when (options.IgnoreServerErrors) { } // ignore
                catch (WebSocketException we) when (options.IgnoreServerErrors)
                {
                    if (we.WebSocketErrorCode == WebSocketError.InvalidState)
                    {
                        const string closeOnClosedMsg = "The WebSocket is in an invalid state ('Closed') for this operation. Valid states are: 'Open, CloseSent, CloseReceived'";
                        const string closeOnAbortedMsg = "The WebSocket is in an invalid state ('Aborted') for this operation. Valid states are: 'Open, CloseSent, CloseReceived'";
                        if (we.Message == closeOnClosedMsg || we.Message == closeOnAbortedMsg)
                        {
                            return; // ignore
                        }
                    }

                    Console.WriteLine($"[Server - {nameof(RunHttpServer)}<{typeof(TServer).Name}>] Abort on an ignored WebSocketException ({we.WebSocketErrorCode}): {we.Message}");
                }
                catch (Exception e) when (options.IgnoreServerErrors)
                {
                    Console.WriteLine($"[Server - {nameof(RunHttpServer)}<{typeof(TServer).Name}>] Abort on an ignored exception: {e}");
                }
            }

            if (options.HttpVersion == HttpVersion.Version11)
            {
                //Console.WriteLine($"[Server - {nameof(RunHttp11Server)}] Waiting for client connection...");

                static Task RunHttp11Server(LoopbackServer server, Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc, Options options, CancellationToken cancellationToken)
                    => server.AcceptConnectionAsync(async connection =>
                        {
                            //Console.WriteLine($"[Server - {nameof(RunHttp11Server)}] Processing HTTP/1.1 request...");
                            var requestData = await WebSocketHandshakeHelper.ProcessHttp11RequestAsync(
                                connection,
                                options.SkipServerHandshakeResponse,
                                options.ParseEchoOptions,
                                cancellationToken).ConfigureAwait(false);
                            //Console.WriteLine($"[Server - {nameof(RunHttp11Server)}] WebSocketRequestData: {requestData}");
                            await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);
                            //Console.WriteLine($"[Server - {nameof(RunHttp11Server)}] loopbackServerFunc completed");
                        });

                return LoopbackServer.CreateClientAndServerAsync(
                    clientFunc,
                    server => RunHttpServer(server, RunHttp11Server),
                    new LoopbackServer.Options { WebSocketEndpoint = true, UseSsl = options.UseSsl });
            }
            else if (options.HttpVersion == HttpVersion.Version20)
            {
                //Console.WriteLine($"[Server - {nameof(RunHttp2Server)}] Waiting for client connection...");
                static async Task RunHttp2Server(Http2LoopbackServer server, Func<WebSocketRequestData, CancellationToken, Task> loopbackServerFunc, Options options, CancellationToken cancellationToken)
                {
                    var requestData = await WebSocketHandshakeHelper.ProcessHttp2RequestAsync(
                        server,
                        options.SkipServerHandshakeResponse,
                        options.ParseEchoOptions,
                        cancellationToken).ConfigureAwait(false);
                    //Console.WriteLine($"[Server - {nameof(RunHttp2Server)}] WebSocketRequestData: {requestData}");
                    await loopbackServerFunc(requestData, cancellationToken).ConfigureAwait(false);
                    //Console.WriteLine($"[Server - {nameof(RunHttp2Server)}] loopbackServerFunc completed");
                    //Console.WriteLine($"[Server - {nameof(RunHttp2Server)}] Shutting down HTTP/2 connection...");
                    await requestData.Http2Connection!.ShutdownIgnoringErrorsAsync(
                        requestData.Http2StreamId.Value);
                    //Console.WriteLine($"[Server - {nameof(RunHttp2Server)}] HTTP/2 connection shutdown completed");
                };

                var http2Options = new Http2Options { WebSocketEndpoint = true, UseSsl = options.UseSsl };
                options.ConfigureHttp2Options?.Invoke(http2Options);

                return Http2LoopbackServer.CreateClientAndServerAsync(
                    clientFunc,
                    server => RunHttpServer(server, RunHttp2Server),
                    http2Options);
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
            options.ConfigureServerOptions?.Invoke(wsOptions);

            var serverWebSocket = WebSocket.CreateFromStream(requestData.TransportStream, wsOptions);
            //Console.WriteLine($"[Server - {nameof(RunServerAsync)}] Created server websocket");
            using var registration = cancellationToken.Register(serverWebSocket.Abort);

            //Console.WriteLine($"[Server - {nameof(RunServerAsync)}] Processing...");
            await serverWebSocketFunc(serverWebSocket, cancellationToken).ConfigureAwait(false);
            //Console.WriteLine($"[Server - {nameof(RunServerAsync)}] Completed");

            if (options.DisposeServerWebSocket)
            {
                serverWebSocket?.Dispose();
            }
        }

        private static Task RunEchoServerAsync(WebSocketRequestData data, Options options, CancellationToken cancellationToken)
        {
            Assert.NotNull(data.EchoOptions);
            WebSocketEchoOptions echoOptions = data.EchoOptions.Value;

            if (echoOptions.SubProtocol is not null)
            {
                Options original = options;
                Action<WebSocketCreationOptions> originalConfigure = original.ConfigureServerOptions;
                Action<WebSocketCreationOptions> combinedConfigure = o =>
                {
                    o.SubProtocol = echoOptions.SubProtocol;
                    originalConfigure?.Invoke(o);
                };

                options = original with { ConfigureServerOptions = combinedConfigure };
            }

            //Console.WriteLine($"[Server - {nameof(RunEchoServerAsync)}] Starting Echo server");

            return RunServerAsync(
                data,
                (serverWebSocket, token) => WebSocketEchoHelper.RunEchoAll(
                    serverWebSocket,
                    echoOptions.ReplyWithPartialMessages,
                    echoOptions.ReplyWithEnhancedCloseMessage,
                    token),
                options,
                cancellationToken);
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
            public bool ParseEchoOptions { get; set; }
            public bool IgnoreServerErrors { get; set; }
            public bool AbortServerOnClientExit { get; set; }
            public Action<WebSocketCreationOptions>? ConfigureServerOptions { get; set; }
            public Action<Http2Options>? ConfigureHttp2Options { get; set; }

            public bool DisposeClientWebSocket { get; set; }
            public bool DisposeHttpInvoker { get; set; }
            public Action<ClientWebSocketOptions>? ConfigureClientOptions { get; set; }
        }
    }
}
