// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class ConnectTest : ClientWebSocketTestBase
    {
        public ConnectTest(ITestOutputHelper output) : base(output) { }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(UnavailableWebSocketServers))]
        public async Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ConnectAsync(server, cts.Token));

                if (PlatformDetection.IsNetCore && !PlatformDetection.IsUap) // bug fix in netcoreapp: https://github.com/dotnet/corefx/pull/35960
                {
                    Assert.Equal(errorCode, ex.WebSocketErrorCode);
                }
                Assert.Equal(WebSocketState.Closed, cws.State);

                // .NET Framework and UAP implmentations have different exception message from .NET Core.
                if (!PlatformDetection.IsFullFramework && !PlatformDetection.IsUap)
                {
                    Assert.Equal(exceptionMessage, ex.Message);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task EchoBinaryMessage_Success(Uri server)
        {
            await WebSocketHelper.TestEcho(server, WebSocketMessageType.Binary, TimeOutMilliseconds, _output);
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task EchoTextMessage_Success(Uri server)
        {
            await WebSocketHelper.TestEcho(server, WebSocketMessageType.Text, TimeOutMilliseconds, _output);
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoHeadersServers))]
        public async Task ConnectAsync_AddCustomHeaders_Success(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                cws.Options.SetRequestHeader("X-CustomHeader1", "Value1");
                cws.Options.SetRequestHeader("X-CustomHeader2", "Value2");
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    Task taskConnect = cws.ConnectAsync(server, cts.Token);
                    Assert.True(
                        (cws.State == WebSocketState.None) ||
                        (cws.State == WebSocketState.Connecting) ||
                        (cws.State == WebSocketState.Open),
                        "State immediately after ConnectAsync incorrect: " + cws.State);
                    await taskConnect;
                }

                Assert.Equal(WebSocketState.Open, cws.State);

                byte[] buffer = new byte[65536];
                WebSocketReceiveResult recvResult;
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    recvResult = await ReceiveEntireMessageAsync(cws, new ArraySegment<byte>(buffer), cts.Token);
                }

                Assert.Equal(WebSocketMessageType.Text, recvResult.MessageType);
                string headers = WebSocketData.GetTextFromBuffer(new ArraySegment<byte>(buffer, 0, recvResult.Count));
                Assert.True(headers.Contains("X-CustomHeader1:Value1"));
                Assert.True(headers.Contains("X-CustomHeader2:Value2"));

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        [ActiveIssue(18784, TargetFrameworkMonikers.NetFramework)]
        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_AddHostHeader_Success()
        {
            Uri server = System.Net.Test.Common.Configuration.WebSockets.RemoteEchoServer;

            // Send via the physical address such as "corefx-net.cloudapp.net"
            // Set the Host header to logical address like "subdomain.corefx-net.cloudapp.net"
            // Verify the scenario works and the remote server received "Host: subdomain.corefx-net.cloudapp.net"
            string logicalHost = "subdomain." + server.Host;

            using (var cws = new ClientWebSocket())
            {
                // Set the Host header to the logical address
                cws.Options.SetRequestHeader("Host", logicalHost);
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    // Connect using the physical address
                    Task taskConnect = cws.ConnectAsync(server, cts.Token);
                    Assert.True(
                        (cws.State == WebSocketState.None) ||
                        (cws.State == WebSocketState.Connecting) ||
                        (cws.State == WebSocketState.Open),
                        "State immediately after ConnectAsync incorrect: " + cws.State);
                    await taskConnect;
                }

                Assert.Equal(WebSocketState.Open, cws.State);

                byte[] buffer = new byte[65536];
                WebSocketReceiveResult recvResult;
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    recvResult = await ReceiveEntireMessageAsync(cws, new ArraySegment<byte>(buffer), cts.Token);
                }

                Assert.Equal(WebSocketMessageType.Text, recvResult.MessageType);
                string headers = WebSocketData.GetTextFromBuffer(new ArraySegment<byte>(buffer, 0, recvResult.Count));
                Assert.Contains($"Host:{logicalHost}", headers, StringComparison.Ordinal);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoHeadersServers))]
        public async Task ConnectAsync_CookieHeaders_Success(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                Assert.Null(cws.Options.Cookies);
                cws.Options.Cookies = new CookieContainer();

                Cookie cookie1 = new Cookie("Cookies", "Are Yummy");
                Cookie cookie2 = new Cookie("Especially", "Chocolate Chip");
                Cookie secureCookie = new Cookie("Occasionally", "Raisin");
                secureCookie.Secure = true;

                cws.Options.Cookies.Add(server, cookie1);
                cws.Options.Cookies.Add(server, cookie2);
                cws.Options.Cookies.Add(server, secureCookie);

                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    Task taskConnect = cws.ConnectAsync(server, cts.Token);
                    Assert.True(
                        cws.State == WebSocketState.None ||
                        cws.State == WebSocketState.Connecting ||
                        cws.State == WebSocketState.Open,
                        "State immediately after ConnectAsync incorrect: " + cws.State);
                    await taskConnect;
                }

                Assert.Equal(WebSocketState.Open, cws.State);

                byte[] buffer = new byte[65536];
                WebSocketReceiveResult recvResult;
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    recvResult = await ReceiveEntireMessageAsync(cws, new ArraySegment<byte>(buffer), cts.Token);
                }

                Assert.Equal(WebSocketMessageType.Text, recvResult.MessageType);
                string headers = WebSocketData.GetTextFromBuffer(new ArraySegment<byte>(buffer, 0, recvResult.Count));

                Assert.True(headers.Contains("Cookies=Are Yummy"));
                Assert.True(headers.Contains("Especially=Chocolate Chip"));
                Assert.Equal(server.Scheme == "wss", headers.Contains("Occasionally=Raisin"));

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(Uri server)
        {
            const string AcceptedProtocol = "CustomProtocol";

            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "subprotocol=" + AcceptedProtocol;

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ConnectAsync(ub.Uri, cts.Token));
                _output.WriteLine(ex.Message);
                if (PlatformDetection.IsNetCore) // bug fix in netcoreapp: https://github.com/dotnet/corefx/pull/35960
                {
                    Assert.True(ex.WebSocketErrorCode == WebSocketError.Faulted ||
                        ex.WebSocketErrorCode == WebSocketError.NotAWebSocket);
                }
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(Uri server)
        {
            const string AcceptedProtocol = "AcceptedProtocol";
            const string OtherProtocol = "OtherProtocol";

            using (var cws = new ClientWebSocket())
            {
                cws.Options.AddSubProtocol(AcceptedProtocol);
                cws.Options.AddSubProtocol(OtherProtocol);
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "subprotocol=" + AcceptedProtocol;

                await cws.ConnectAsync(ub.Uri, cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(AcceptedProtocol, cws.SubProtocol);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_NonStandardRequestHeaders_HeadersAddedWithoutValidation()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientSocket.Options.SetRequestHeader("Authorization", "AWS4-HMAC-SHA256 Credential= AKIAXXXXXXXXXXXYSZA /20190301/us-east-2/neptune-db/aws4_request, SignedHeaders=host;x-amz-date, Signature=b8155de54d9faab00000000000000000000000000a07e0d7dda49902e4d9202");
                    await clientSocket.ConnectAsync(uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.True(await LoopbackHelper.WebSocketHandshakeAsync(connection));
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(Uri server)
        {
            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create())
            {
                cws.Options.Proxy = new WebProxy(proxyServer.Uri);
                await cws.ConnectAsync(server, cts.Token);

                string expectedCloseStatusDescription = "Client close status";
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, expectedCloseStatusDescription, cts.Token);

                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(expectedCloseStatusDescription, cws.CloseStatusDescription);
            }
        }
    }
}
