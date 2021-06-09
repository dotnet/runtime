// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1895")]
        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(UnavailableWebSocketServers))]
        public async Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ConnectAsync(server, cts.Token));

                if (PlatformDetection.IsNetCore && !PlatformDetection.IsInAppContainer) // bug fix in netcoreapp: https://github.com/dotnet/corefx/pull/35960
                {
                    Assert.Equal(errorCode, ex.WebSocketErrorCode);
                }
                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(exceptionMessage, ex.Message);

                // Other operations throw after failed connect
                await Assert.ThrowsAsync<ObjectDisposedException>(() => cws.ReceiveAsync(new byte[1], default));
                await Assert.ThrowsAsync<ObjectDisposedException>(() => cws.SendAsync(new byte[1], WebSocketMessageType.Binary, true, default));
                await Assert.ThrowsAsync<ObjectDisposedException>(() => cws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default));
                await Assert.ThrowsAsync<ObjectDisposedException>(() => cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default));
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
        [SkipOnPlatform(TestPlatforms.Browser, "CustomHeaders not supported on browser")]
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
                Assert.Contains("X-CustomHeader1:Value1", headers);
                Assert.Contains("X-CustomHeader2:Value2", headers);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
        public async Task ConnectAsync_AddHostHeader_Success()
        {
            string expectedHost = null;
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                expectedHost = "subdomain." + uri.Host;
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.SetRequestHeader("Host", expectedHost);
                    await cws.ConnectAsync(uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);
                Assert.True(headers.TryGetValue("Host", out string host));
                Assert.Equal(expectedHost, host);
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoHeadersServers))]
        [SkipOnPlatform(TestPlatforms.Browser, "Cookies not supported on browser")]
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

                Assert.Contains("Cookies=Are Yummy", headers);
                Assert.Contains("Especially=Chocolate Chip", headers);
                Assert.Equal(server.Scheme == "wss", headers.Contains("Occasionally=Raisin"));

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45583", TestPlatforms.Browser)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
        public async Task ConnectAsync_NonStandardRequestHeaders_HeadersAddedWithoutValidation()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientSocket.Options.SetRequestHeader("Authorization", "AWS4-HMAC-SHA256 Credential=PLACEHOLDER /20190301/us-east-2/neptune-db/aws4_request, SignedHeaders=host;x-amz-date, Signature=b8155de54d9faab00000000000000000000000000a07e0d7dda49902e4d9202");
                    await clientSocket.ConnectAsync(uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.NotNull(await LoopbackHelper.WebSocketHandshakeAsync(connection));
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_CancellationRequestedBeforeConnect_ThrowsOperationCanceledException()
        {
            using (var clientSocket = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = clientSocket.ConnectAsync(new Uri("ws://" + Guid.NewGuid().ToString("N")), cts.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_CancellationRequestedInflightConnect_ThrowsOperationCanceledException()
        {
            using (var clientSocket = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource();
                Task t = clientSocket.ConnectAsync(new Uri("ws://" + Guid.NewGuid().ToString("N")), cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
        public async Task ConnectAsync_CancellationRequestedAfterConnect_ThrowsOperationCanceledException()
        {
            var releaseServer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                var clientSocket = new ClientWebSocket();
                try
                {
                    var cts = new CancellationTokenSource();
                    Task t = clientSocket.ConnectAsync(uri, cts.Token);
                    Assert.False(t.IsCompleted);
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                }
                finally
                {
                    releaseServer.SetResult();
                    clientSocket.Dispose();
                }
            }, async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await releaseServer.Task;
                    });
                }
                // Ignore IO exception on server as there are race conditions when client is cancelling.
                catch (IOException) { }
            }, new LoopbackServer.Options { WebSocketEndpoint = true });
        }
    }
}
