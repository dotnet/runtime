// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

        public static readonly bool[] Bool_Values = new[] { false, true };
        public static readonly bool[] UseSsl_Values = PlatformDetection.SupportsAlpn ? Bool_Values : new[] { false };
        public static readonly object[][] UseSsl_MemberData = ToMemberData(UseSsl_Values);

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
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
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

        internal HttpMessageInvoker? GetInvoker()
        {
            var handler = new HttpClientHandler();

            if (PlatformDetection.IsNotBrowser)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            ConfigureCustomHandler?.Invoke(handler);

            if (UseCustomInvoker)
            {
                Debug.Assert(!UseHttpClient);
                return new HttpMessageInvoker(handler);
            }

            if (UseHttpClient)
            {
                return new HttpClient(handler);
            }

            return null;
        }

        protected Task<ClientWebSocket> GetConnectedWebSocket(Uri uri, int TimeOutMilliseconds, ITestOutputHelper output) =>
            WebSocketHelper.GetConnectedWebSocket(uri, TimeOutMilliseconds, output, invoker: GetInvoker());

        protected Task ConnectAsync(ClientWebSocket cws, Uri uri, CancellationToken cancellationToken) =>
            cws.ConnectAsync(uri, GetInvoker(), cancellationToken);

        protected Task TestEcho(Uri uri, WebSocketMessageType type, int timeOutMilliseconds, ITestOutputHelper output) =>
            WebSocketHelper.TestEcho(uri, WebSocketMessageType.Text, TimeOutMilliseconds, _output, GetInvoker());

        public static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }
    }
}
