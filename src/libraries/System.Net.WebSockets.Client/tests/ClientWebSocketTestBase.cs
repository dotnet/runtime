// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ClientWebSocketTestBase(ITestOutputHelper output)
    {
        public static readonly Uri[] EchoServers_Values = System.Net.Test.Common.Configuration.WebSockets.GetEchoServers();
        public static readonly Uri[] EchoHeadersServers_Values = System.Net.Test.Common.Configuration.WebSockets.GetEchoHeadersServers();
        public static readonly bool[] Bool_Values = [ false, true ];
        public static readonly bool[] UseSsl_Values = PlatformDetection.SupportsAlpn ? Bool_Values : [ false ];

        public static readonly object[][] EchoServers = ToMemberData(EchoServers_Values);
        public static readonly object[][] EchoHeadersServers = ToMemberData(EchoHeadersServers_Values);
        public static readonly object[][] EchoServersAndBoolean = ToMemberData(EchoServers_Values, Bool_Values);
        public static readonly object[][] UseSsl = ToMemberData(UseSsl_Values);
        public static readonly object[][] UseSslAndBoolean = ToMemberData(UseSsl_Values, Bool_Values);

        public static object[][] ToMemberData<T>(IEnumerable<T> data)
            => data.Select(a => new object[] { a }).ToArray();

        public static object[][] ToMemberData<TA, TB>(IEnumerable<TA> dataA, IEnumerable<TB> dataB)
            => dataA.SelectMany(a => dataB.Select(b => new object[] { a, b })).ToArray();

        public static object[][] ToMemberData<TA, TB, TC>(IEnumerable<TA> dataA, IEnumerable<TB> dataB, IEnumerable<TC> dataC)
            => dataA.SelectMany(a => dataB.SelectMany(b => dataC.Select(c => new object[] { a, b, c }))).ToArray();

        public const int TimeOutMilliseconds = 30000;
        public const int CloseDescriptionMaxLength = 123;
        public readonly ITestOutputHelper _output = output;

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
                    exceptionMessage = ResourceHelper.GetExceptionMessage("net_WebSockets_ConnectStatusExpected", (int)HttpStatusCode.OK, (int)HttpStatusCode.SwitchingProtocols);

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
                    _output.WriteLine($"Operation finished before CTS expired.");
                }
                catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or WebSocketException)
                {
                    Assert.True(WebSocketState.Aborted == cws.State, $"Actual {cws.State} when {e}");

                    if (e is WebSocketException wse)
                    {
                        Assert.True(WebSocketError.InvalidState == wse.WebSocketErrorCode, $"Actual WebSocketErrorCode {wse.WebSocketErrorCode} when {wse}");
                    }
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

        internal virtual Version HttpVersion => Net.HttpVersion.Version11;

        internal HttpMessageInvoker? GetInvoker()
        {
            if (UseSharedHandler)
            {
                return null;
            }

            HttpClientHandler handler = new HttpClientHandler();
            if (PlatformDetection.IsNotBrowser)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                ConfigureCustomHandler?.Invoke(handler);
            }

            if (UseCustomInvoker)
            {
                Debug.Assert(!UseHttpClient);
                return new HttpMessageInvoker(handler);
            }

            Debug.Assert(UseHttpClient);
            return new HttpClient(handler);
        }

        public Task<ClientWebSocket> GetConnectedWebSocket(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null)
            => WebSocketHelper.Retry(
                async () =>
                {
                    var cws = new ClientWebSocket();
                    configureOptions?.Invoke(cws.Options);

                    using var cts = new CancellationTokenSource(TimeOutMilliseconds);
                    Task taskConnect = ConnectAsync(cws, uri, cts.Token);

                    Assert.True(
                        (cws.State == WebSocketState.None) ||
                        (cws.State == WebSocketState.Connecting) ||
                        (cws.State == WebSocketState.Open) ||
                        (cws.State == WebSocketState.Aborted),
                        "State immediately after ConnectAsync incorrect: " + cws.State);
                    await taskConnect;

                    Assert.Equal(WebSocketState.Open, cws.State);
                    return cws;
                });

        protected Task ConnectAsync(ClientWebSocket cws, Uri uri, CancellationToken cancellationToken)
        {
            if (PlatformDetection.IsNotBrowser)
            {
                if (uri.Scheme == "wss" && UseSharedHandler)
                {
                    cws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                }

                cws.Options.HttpVersion = HttpVersion;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                if (HttpVersion == Net.HttpVersion.Version20 && uri.Query is not (null or "" or "?"))
                {
                    // RFC 7540, section 8.3. The CONNECT Method:
                    //  > The ":scheme" and ":path" pseudo-header fields MUST be omitted.
                    //
                    // HTTP/2 CONNECT requests must drop query (containing echo options) from the request URI.
                    // The information needs to be passed in a different way, e.g. in a custom header.

                    cws.Options.SetRequestHeader(WebSocketHelper.OriginalQueryStringHeader, uri.Query);
                }
            }

            return UseSharedHandler
                ? cws.ConnectAsync(uri, cancellationToken) // Ensure test coverage for both overloads
                : cws.ConnectAsync(uri, GetInvoker(), cancellationToken);
        }

        protected Task RunClientAsync(
            Uri uri,
            Func<ClientWebSocket, CancellationToken, Task> clientWebSocketFunc,
            Action<ClientWebSocketOptions>? configureOptions = null)
        {
            var cts = new CancellationTokenSource(TimeOutMilliseconds);
            return RunClientAsync(uri, clientWebSocketFunc, configureOptions, cts.Token);
        }

        protected async Task RunClientAsync(
            Uri uri,
            Func<ClientWebSocket, CancellationToken, Task> clientWebSocketFunc,
            Action<ClientWebSocketOptions>? configureOptions,
            CancellationToken cancellationToken)
        {
            using ClientWebSocket cws = await GetConnectedWebSocket(uri, configureOptions);
            await clientWebSocketFunc(cws, cancellationToken);
        }

        public static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }
    }
}
