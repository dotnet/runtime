// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

using EchoQueryKey = System.Net.Test.Common.WebSocketEchoOptions.EchoQueryKey;

namespace System.Net.WebSockets.Client.Tests
{
    //
    // Class hierarchy:
    //
    // - ConnectTestBase                                  → file:ConnectTest.cs
    //   ├─ ConnectTest_External
    //   │  ├─ [*]ConnectTest_SharedHandler_External
    //   │  ├─ [*]ConnectTest_Invoker_External
    //   │  └─ [*]ConnectTest_HttpClient_External
    //   └─ ConnectTest_LoopbackBase                      → file:ConnectTest.Loopback.cs
    //      ├─ ConnectTest_Loopback
    //      │  ├─ [*]ConnectTest_SharedHandler_Loopback   → file:ConnectTest.Loopback.cs, ConnectTest.SharedHandler.cs
    //      │  ├─ [*]ConnectTest_Invoker_Loopback         → file:ConnectTest.Loopback.cs, ConnectTest.Invoker.cs
    //      │  └─ [*]ConnectTest_HttpClient_Loopback
    //      └─ ConnectTest_Http2Loopback                  → file:ConnectTest.Loopback.cs, ConnectTest.Http2.cs
    //         ├─ [*]ConnectTest_Invoker_Http2Loopback
    //         └─ [*]ConnectTest_HttpClient_Http2Loopback
    //
    // ---
    // `[*]` - concrete runnable test classes
    // `→ file:` - file containing the class and its concrete subclasses

    public abstract class ConnectTestBase(ITestOutputHelper output) : ClientWebSocketTestBase(output)
    {
        #region Common (Echo Server) tests

        protected Task RunClient_EchoBinaryMessage_Success(Uri server)
            => RunClientAsync(server,
                (cws, ct) => WebSocketHelper.TestEcho(cws, WebSocketMessageType.Binary, ct));


        protected Task RunClient_EchoTextMessage_Success(Uri server)
            => RunClientAsync(server,
                (cws, ct) => WebSocketHelper.TestEcho(cws, WebSocketMessageType.Text, ct));

        protected async Task RunClient_ConnectAsync_AddCustomHeaders_Success(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                cws.Options.SetRequestHeader("X-CustomHeader1", "Value1");
                cws.Options.SetRequestHeader("X-CustomHeader2", "Value2");
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    Task taskConnect = ConnectAsync(cws, server, cts.Token);
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
                string headers = new ArraySegment<byte>(buffer, 0, recvResult.Count).Utf8ToString();
                Assert.Contains("X-CustomHeader1:Value1", headers, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("X-CustomHeader2:Value2", headers, StringComparison.OrdinalIgnoreCase);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        protected async Task RunClient_ConnectAsync_CookieHeaders_Success(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                Assert.Null(cws.Options.Cookies);

                var cookies = new CookieContainer();

                Cookie cookie1 = new Cookie("Cookies", "Are Yummy");
                Cookie cookie2 = new Cookie("Especially", "Chocolate Chip");
                Cookie secureCookie = new Cookie("Occasionally", "Raisin") { Secure = true };

                cookies.Add(server, cookie1);
                cookies.Add(server, cookie2);
                cookies.Add(server, secureCookie);

                if (UseSharedHandler)
                {
                    cws.Options.Cookies = cookies;
                }
                else
                {
                    ConfigureCustomHandler = handler => handler.CookieContainer = cookies;
                }

                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    Task taskConnect = ConnectAsync(cws, server, cts.Token);
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
                string headers = new ArraySegment<byte>(buffer, 0, recvResult.Count).Utf8ToString();

                Assert.Contains("Cookies=Are Yummy", headers);
                Assert.Contains("Especially=Chocolate Chip", headers);
                Assert.Equal(server.Scheme == "wss", headers.Contains("Occasionally=Raisin"));

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        protected async Task RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(Uri server)
        {
            const string AcceptedProtocol = "CustomProtocol";

            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server) { Query = $"{EchoQueryKey.SubProtocol}={AcceptedProtocol}" };

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    ConnectAsync(cws, ub.Uri, cts.Token));
                _output.WriteLine(ex.Message);
                Assert.True(ex.WebSocketErrorCode == WebSocketError.UnsupportedProtocol ||
                    ex.WebSocketErrorCode == WebSocketError.Faulted ||
                    ex.WebSocketErrorCode == WebSocketError.NotAWebSocket, $"Actual WebSocketErrorCode {ex.WebSocketErrorCode} {ex.InnerException?.Message} \n {ex}");
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        protected async Task RunClient_ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(Uri server)
        {
            const string AcceptedProtocol = "AcceptedProtocol";
            const string OtherProtocol = "OtherProtocol";

            using (var cws = new ClientWebSocket())
            {
                cws.Options.AddSubProtocol(AcceptedProtocol);
                cws.Options.AddSubProtocol(OtherProtocol);
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server) { Query = $"{EchoQueryKey.SubProtocol}={AcceptedProtocol}" };

                await ConnectAsync(cws, ub.Uri, cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(AcceptedProtocol, cws.SubProtocol);
            }
        }

        protected async Task RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(Uri server)
        {
            if (HttpVersion != Net.HttpVersion.Version11)
            {
                throw new SkipTestException("LoopbackProxyServer is HTTP/1.1 only");
            }

            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create())
            {
                if (UseSharedHandler)
                {
                    cws.Options.Proxy = new WebProxy(proxyServer.Uri);
                }
                else
                {
                    ConfigureCustomHandler = handler => handler.Proxy = new WebProxy(proxyServer.Uri);
                }

                await ConnectAsync(cws, server, cts.Token);

                string expectedCloseStatusDescription = "Client close status";
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, expectedCloseStatusDescription, cts.Token);

                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(expectedCloseStatusDescription, cws.CloseStatusDescription);
                Assert.Equal(1, proxyServer.Connections);
            }
        }

        #endregion
    }

    [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    public abstract class ConnectTest_External(ITestOutputHelper output) : ConnectTestBase(output)
    {
        #region Common (Echo Server) tests

        [Theory, MemberData(nameof(EchoServers))]
        public Task EchoBinaryMessage_Success(Uri server)
            => RunClient_EchoBinaryMessage_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task EchoTextMessage_Success(Uri server)
            => RunClient_EchoTextMessage_Success(server);

        [SkipOnPlatform(TestPlatforms.Browser, "SetRequestHeader not supported on browser")]
        [Theory, MemberData(nameof(EchoHeadersServers))]
        public Task ConnectAsync_AddCustomHeaders_Success(Uri server)
            => RunClient_ConnectAsync_AddCustomHeaders_Success(server);

        [SkipOnPlatform(TestPlatforms.Browser, "Cookies not supported on browser")]
        [Theory, MemberData(nameof(EchoHeadersServers))]
        public Task ConnectAsync_CookieHeaders_Success(Uri server)
            => RunClient_ConnectAsync_CookieHeaders_Success(server);

        [ActiveIssue("https://github.com/dotnet/runtime/issues/101115", typeof(PlatformDetection), nameof(PlatformDetection.IsFirefox))]
        [Theory, MemberData(nameof(EchoServers))]
        public Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(Uri server)
            => RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(Uri server)
            => RunClient_ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(server);

        [SkipOnPlatform(TestPlatforms.Browser, "Proxy not supported on Browser")]
        [Theory, MemberData(nameof(EchoServers))]
        public Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(Uri server)
            => RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(server);

        #endregion

        #region External-only tests

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1895")]
        [Theory]
        [MemberData(nameof(UnavailableWebSocketServers))]
        public async Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    ConnectAsync(cws, server, cts.Token));

                if (!PlatformDetection.IsInAppContainer) // bug fix in netcoreapp: https://github.com/dotnet/corefx/pull/35960
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

        [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets are not supported on this platform")]
        [ConditionalFact] // Uses SkipTestException
        public async Task ConnectAsync_Http11Server_DowngradeFail()
        {
            if (UseSharedHandler)
            {
                throw new SkipTestException("HTTP/2 is not supported with SharedHandler");
            }

            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = Net.HttpVersion.Version20;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                Task t = cws.ConnectAsync(Test.Common.Configuration.WebSockets.SecureRemoteEchoServer, GetInvoker(), cts.Token);

                var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                Assert.True(ex.InnerException.Data.Contains("HTTP2_ENABLED"));
                HttpRequestException inner = Assert.IsType<HttpRequestException>(ex.InnerException);
                HttpRequestError expectedError = PlatformDetection.SupportsAlpn ?
                    HttpRequestError.SecureConnectionError :
                    HttpRequestError.VersionNegotiationError;
                Assert.Equal(expectedError, inner.HttpRequestError);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets are not supported on this platform")]
        [ConditionalTheory] // Uses SkipTestException
        [MemberData(nameof(EchoServers))]
        public async Task ConnectAsync_Http11Server_DowngradeSuccess(Uri server)
        {
            if (UseSharedHandler)
            {
                throw new SkipTestException("HTTP/2 is not supported with SharedHandler");
            }

            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = Net.HttpVersion.Version20;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                await cws.ConnectAsync(server, GetInvoker(), cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }

        [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets are not supported on this platform")]
        [ConditionalTheory] // Uses SkipTestException
        [MemberData(nameof(EchoServers))]
        public async Task ConnectAsync_Http11WithRequestVersionOrHigher_DowngradeSuccess(Uri server)
        {
            if (UseSharedHandler)
            {
                throw new SkipTestException("HTTP/2 is not supported with SharedHandler");
            }

            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = Net.HttpVersion.Version11;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                await cws.ConnectAsync(server, GetInvoker(), cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }

        #endregion
    }

#region Runnable test classes: External/Outerloop

    public sealed class ConnectTest_SharedHandler_External(ITestOutputHelper output) : ConnectTest_External(output) { }

    public sealed class ConnectTest_Invoker_External(ITestOutputHelper output) : ConnectTest_External(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class ConnectTest_HttpClient_External(ITestOutputHelper output) : ConnectTest_External(output)
    {
        protected override bool UseHttpClient => true;
    }

#endregion
}
