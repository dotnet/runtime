// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using TestUtilities;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class ClientWebSocketTestBase
    {
        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.WebSockets.GetEchoServers();
        public static readonly object[][] EchoHeadersServers = System.Net.Test.Common.Configuration.WebSockets.GetEchoHeadersServers();
        public static readonly object[][] EchoServersAndBoolean = EchoServers.SelectMany(o => new object[][]
        {
            new object[] { o[0], false },
            new object[] { o[0], true }
        }).ToArray();

        public static readonly bool[] Bool_Values = [ false, true ];
        public static readonly bool[] UseSsl_Values = PlatformDetection.SupportsAlpn ? Bool_Values : [ false ];
        public static readonly object[][] UseSsl_MemberData = ToMemberData(UseSsl_Values);
        public static readonly object[][] UseSslAndBoolean = ToMemberData(UseSsl_Values, Bool_Values);

        public static object[][] ToMemberData<T>(IEnumerable<T> data)
            => data.Select(a => new object[] { a }).ToArray();

        public static object[][] ToMemberData<TA, TB>(IEnumerable<TA> dataA, IEnumerable<TB> dataB)
            => dataA.SelectMany(a => dataB.Select(b => new object[] { a, b })).ToArray();

        public static object[][] ToMemberData<TA, TB, TC>(IEnumerable<TA> dataA, IEnumerable<TB> dataB, IEnumerable<TC> dataC)
            => dataA.SelectMany(a => dataB.SelectMany(b => dataC.Select(c => new object[] { a, b, c }))).ToArray();

        public const int TimeOutMilliseconds = 30000;
        public const int CloseDescriptionMaxLength = 123;
        public readonly ITestOutputHelper _output;

        public ClientWebSocketTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> UnavailableWebSocketServers
        {
            get
            {
                Uri server;
                string exceptionMessage;

                // Unknown server.
                {
                    server = new Uri(string.Format("ws://{0}", Guid.NewGuid().ToString()));
                    exceptionMessage = ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure");

                    yield return new object[] { server, exceptionMessage, WebSocketError.Faulted };
                }

                // Known server but not a real websocket endpoint.
                {
                    server = System.Net.Test.Common.Configuration.Http.RemoteEchoServer;
                    var ub = new UriBuilder("ws", server.Host, server.Port, server.PathAndQuery);
                    exceptionMessage = ResourceHelper.GetExceptionMessage("net_WebSockets_ConnectStatusExpected", (int) HttpStatusCode.OK, (int) HttpStatusCode.SwitchingProtocols);

                    yield return new object[] { ub.Uri, exceptionMessage, WebSocketError.NotAWebSocket };
                }
            }
        }

        public async Task TestCancellation(Func<ClientWebSocket, Task> action, Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                try
                {
                    await action(cws);
                    // Operation finished before CTS expired.
                }
                catch (OperationCanceledException exception)
                {
                    // Expected exception
                    Assert.True(WebSocketState.Aborted == cws.State, $"Actual {cws.State} when {exception}");
                }
                catch (ObjectDisposedException exception)
                {
                    // Expected exception
                    Assert.True(WebSocketState.Aborted == cws.State, $"Actual {cws.State} when {exception}");
                }
                catch (WebSocketException exception)
                {
                    Assert.True(WebSocketError.InvalidState == exception.WebSocketErrorCode, $"Actual WebSocketErrorCode {exception.WebSocketErrorCode} when {exception}");
                    Assert.True(WebSocketState.Aborted == cws.State, $"Actual {cws.State} when {exception}");
                }
            }
        }

        protected static async Task<WebSocketReceiveResult> ReceiveEntireMessageAsync(WebSocket ws, ArraySegment<byte> segment, CancellationToken cancellationToken)
        {
            int bytesReceived = 0;
            while (true)
            {
                WebSocketReceiveResult r = await ws.ReceiveAsync(segment, cancellationToken);
                if (r.EndOfMessage)
                {
                    return new WebSocketReceiveResult(bytesReceived + r.Count, r.MessageType, true, r.CloseStatus, r.CloseStatusDescription);
                }
                else
                {
                    bytesReceived += r.Count;
                    segment = new ArraySegment<byte>(segment.Array, segment.Offset + r.Count, segment.Count - r.Count);
                }
            }
        }

        protected virtual bool UseCustomInvoker => false;

        protected virtual bool UseHttpClient => false;

        protected bool UseSharedHandler => !UseCustomInvoker && !UseHttpClient;

        protected Action<HttpClientHandler>? ConfigureCustomHandler;
        protected bool UseSocketsHttpHandler = true;//false;

        internal virtual Version HttpVersion => Net.HttpVersion.Version11;

        internal HttpMessageInvoker? GetInvoker()
        {
            if (UseSharedHandler)
            {
                return null;
            }

            HttpMessageHandler handler;

            if (UseSocketsHttpHandler && SocketsHttpHandler.IsSupported)
            {
                if (ConfigureCustomHandler is not null)
                {
                    throw new InvalidOperationException("ConfigureCustomHandler is not supported when UseSocketsHttpHandler is true.");
                }

                var shh = new SocketsHttpHandler();
                shh.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                shh.ConnectCallback = async (context, ct) =>
                {
                    // Create and connect a socket using default settings.
                    Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(context.DnsEndPoint, ct).ConfigureAwait(false);
                        Stream stream = new NetworkStream(socket, ownsSocket: true);
/*#if !TARGET_BROWSER
                        var networkStream = stream;
                        var delegatingStream = new DelegateDelegatingStream(networkStream);
                        delegatingStream.DisposeFunc = disposing =>
                        {
                            //_output.WriteLine($"[Client] NetworkStream.Dispose({disposing})");
                            if (disposing)
                            {
                                networkStream.Dispose();
                            }
                        };
                        delegatingStream.DisposeAsyncFunc = () =>
                        {
                            //_output.WriteLine($"[Client] NetworkStream.DisposeAsync()");
                            return networkStream.DisposeAsync();
                        };
                        stream = delegatingStream;
#endif*/
                        return stream;
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                };
                handler = shh;
            }
            else if (PlatformDetection.IsNotBrowser)
            {
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                ConfigureCustomHandler?.Invoke(httpClientHandler);
                handler = httpClientHandler;
            }
            else
            {
                handler = new HttpClientHandler();
            }


            if (UseCustomInvoker)
            {
                Debug.Assert(!UseHttpClient);
                return new HttpMessageInvoker(handler);
            }

            Debug.Assert(UseHttpClient);
            return new HttpClient(handler);
        }

        protected Task<ClientWebSocket> GetConnectedWebSocket(Uri uri, int timeOutMilliseconds, ITestOutputHelper output)
            => WebSocketHelper.GetConnectedWebSocket(uri, timeOutMilliseconds, output, o => ConfigureHttpVersion(o, uri), GetInvoker());

        protected Task<ClientWebSocket> GetConnectedWebSocket(Uri uri)
            => GetConnectedWebSocket(uri, TimeOutMilliseconds, _output);

        protected Task ConnectAsync(ClientWebSocket cws, Uri uri, CancellationToken cancellationToken)
        {
            ConfigureHttpVersion(cws.Options, uri);
            return cws.ConnectAsync(uri, GetInvoker(), cancellationToken);
        }

        protected Task TestEcho(Uri uri, WebSocketMessageType type)
            => WebSocketHelper.TestEcho(uri, type, TimeOutMilliseconds, _output, o => ConfigureHttpVersion(o, uri), GetInvoker());

        protected void ConfigureHttpVersion(ClientWebSocketOptions options, Uri uri)
        {
            if (PlatformDetection.IsBrowser)
            {
                if (HttpVersion != Net.HttpVersion.Version11)
                {
                    throw new SkipTestException($"HTTP version {HttpVersion} is not supported for WebSockets on Browser.");
                }
                return;
            }

            options.HttpVersion = HttpVersion;
            options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            if (UseSharedHandler && uri.Scheme == "wss")
            {
                options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            if (HttpVersion == Net.HttpVersion.Version20 && uri.Query is not (null or "" or "?"))
            {
                // RFC 7540, section 8.3. The CONNECT Method:
                //  > The ":scheme" and ":path" pseudo-header fields MUST be omitted.
                //
                // HTTP/2 CONNECT requests must drop query (containing echo options) from the request URI.
                // The information needs to be passed in a different way, e.g. in a custom header.

                options.SetRequestHeader(WebSocketHelper.OriginalQueryStringHeader, uri.Query);
            }
        }

        public static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }

#if !TARGET_BROWSER
        // Loopback server related functions
        protected virtual bool SkipIfUseSsl => false;

        protected Task RunEchoAsync(Func<Uri, Task> clientFunc, bool useSsl)
        {
            if (SkipIfUseSsl && useSsl)
            {
                throw new SkipTestException("SSL is not supported in this test.");
            }
            return LoopbackWebSocketServer.RunEchoAsync(clientFunc, HttpVersion, useSsl, TimeOutMilliseconds, _output);
        }

        protected Task RunEchoHeadersAsync(Func<Uri, Task> clientFunc, bool useSsl)
        {
            if (SkipIfUseSsl && useSsl)
            {
                throw new SkipTestException("SSL is not supported in this test.");
            }

            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);
            var options = new LoopbackWebSocketServer.Options
            {
                HttpVersion = HttpVersion,
                UseSsl = useSsl,
                IgnoreServerErrors = true,
                AbortServerOnClientExit = true,
                TestOutputHelper = _output
            };

            return LoopbackWebSocketServer.RunAsync(
                clientFunc,
                async (requestData, token) =>
                {
                    // _output?.WriteLine($"[Server - {nameof(RunEchoHeadersAsync)}] WebSocket.CreateFromStream");
                    var serverWebSocket = WebSocket.CreateFromStream(
                        requestData.TransportStream,
                        new WebSocketCreationOptions { IsServer = true });

                    using (var registration = token.Register(() => {
                        // _output?.WriteLine($"[Server - {nameof(RunEchoHeadersAsync)}] Aborting server WebSocket on cancellation");
                        serverWebSocket.Abort();
                    }))
                    {
                        // _output?.WriteLine($"[Server - {nameof(RunEchoHeadersAsync)}] RunEchoHeaders");
                        await WebSocketEchoHelper.RunEchoHeaders(serverWebSocket, requestData.Headers, options.Logger, token);
                        // _output?.WriteLine($"[Server - {nameof(RunEchoHeadersAsync)}] RunEchoHeaders completed");
                    }

                    // _output?.WriteLine($"[Server - Run Echo Headers WS] Completed; Server WebSocket state: {serverWebSocket.State}");
                },
                options,
                timeoutCts.Token);
        }
#endif
    }
}
