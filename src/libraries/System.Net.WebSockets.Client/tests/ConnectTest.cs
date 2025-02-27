// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class InvokerConnectTest : ConnectTestBase
    {
        public InvokerConnectTest(ITestOutputHelper output) : base(output) { }

        protected override bool UseCustomInvoker => true;

        public static IEnumerable<object[]> ConnectAsync_CustomInvokerWithIncompatibleWebSocketOptions_ThrowsArgumentException_MemberData()
        {
            yield return Throw(options => options.UseDefaultCredentials = true);
            yield return NoThrow(options => options.UseDefaultCredentials = false);
            yield return Throw(options => options.Credentials = new NetworkCredential());
            yield return Throw(options => options.Proxy = new WebProxy());

            // Will result in an exception on apple mobile platforms
            // and crash the test.
            if (PlatformDetection.IsNotAppleMobile)
            {
                yield return Throw(options => options.ClientCertificates.Add(Test.Common.Configuration.Certificates.GetClientCertificate()));
            }

            yield return NoThrow(options => options.ClientCertificates = new X509CertificateCollection());
            yield return Throw(options => options.RemoteCertificateValidationCallback = delegate { return true; });
            yield return Throw(options => options.Cookies = new CookieContainer());

            // We allow no proxy or the default proxy to be used
            yield return NoThrow(options => { });
            yield return NoThrow(options => options.Proxy = null);

            // These options don't conflict with the custom invoker
            yield return NoThrow(options => options.HttpVersion = new Version(2, 0));
            yield return NoThrow(options => options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher);
            yield return NoThrow(options => options.SetRequestHeader("foo", "bar"));
            yield return NoThrow(options => options.AddSubProtocol("foo"));
            yield return NoThrow(options => options.KeepAliveInterval = TimeSpan.FromSeconds(42));
            yield return NoThrow(options => options.DangerousDeflateOptions = new WebSocketDeflateOptions());
            yield return NoThrow(options => options.CollectHttpResponseDetails = true);

            static object[] Throw(Action<ClientWebSocketOptions> configureOptions) =>
                new object[] { configureOptions, true };

            static object[] NoThrow(Action<ClientWebSocketOptions> configureOptions) =>
                new object[] { configureOptions, false };
        }

        [Theory]
        [MemberData(nameof(ConnectAsync_CustomInvokerWithIncompatibleWebSocketOptions_ThrowsArgumentException_MemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "Custom invoker is ignored on Browser")]
        public async Task ConnectAsync_CustomInvokerWithIncompatibleWebSocketOptions_ThrowsArgumentException(Action<ClientWebSocketOptions> configureOptions, bool shouldThrow)
        {
            using var invoker = new HttpMessageInvoker(new SocketsHttpHandler
            {
                ConnectCallback = (_, _) => ValueTask.FromException<Stream>(new Exception("ConnectCallback"))
            });

            using var ws = new ClientWebSocket();
            configureOptions(ws.Options);

            Task connectTask = ws.ConnectAsync(new Uri("wss://dummy"), invoker, CancellationToken.None);
            if (shouldThrow)
            {
                Assert.Equal(TaskStatus.Faulted, connectTask.Status);
                await Assert.ThrowsAsync<ArgumentException>("options", () => connectTask);
            }
            else
            {
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() => connectTask);
                Assert.NotNull(ex.InnerException);
                Assert.Contains("ConnectCallback", ex.InnerException.Message);
            }

            foreach (X509Certificate cert in ws.Options.ClientCertificates)
            {
                cert.Dispose();
            }
        }
    }

    public sealed class HttpClientConnectTest : ConnectTestBase
    {
        public HttpClientConnectTest(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    public abstract class ConnectTestBase : ClientWebSocketTestBase
    {
        public ConnectTestBase(ITestOutputHelper output) : base(output) { }

        protected async Task RunClient_ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
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

        protected async Task RunClient_EchoBinaryMessage_Success(Uri server)
        {
            await TestEcho(server, WebSocketMessageType.Binary);
        }

        protected async Task RunClient_EchoTextMessage_Success(Uri server)
        {
            await TestEcho(server, WebSocketMessageType.Text);
        }

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
                string headers = WebSocketData.GetTextFromBuffer(new ArraySegment<byte>(buffer, 0, recvResult.Count));
                Assert.Contains("X-CustomHeader1:Value1", headers);
                Assert.Contains("X-CustomHeader2:Value2", headers);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        protected async Task RunClient_ConnectAsync_CookieHeaders_Success(Uri server)
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

        protected async Task RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(Uri server)
        {
            const string AcceptedProtocol = "CustomProtocol";

            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "subprotocol=" + AcceptedProtocol;

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    ConnectAsync(cws, ub.Uri, cts.Token));
                _output.WriteLine(ex.Message);
                Assert.True(ex.WebSocketErrorCode == WebSocketError.Faulted ||
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

                var ub = new UriBuilder(server);
                ub.Query = "subprotocol=" + AcceptedProtocol;

                await ConnectAsync(cws, ub.Uri, cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(AcceptedProtocol, cws.SubProtocol);
            }
        }

        protected async Task RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(Uri server)
        {
            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create())
            {
                ConfigureCustomHandler = handler => handler.Proxy = new WebProxy(proxyServer.Uri);

                if (UseSharedHandler)
                {
                    cws.Options.Proxy = new WebProxy(proxyServer.Uri);
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_CancellationRequestedBeforeConnect_ThrowsOperationCanceledException()
        {
            using (var clientSocket = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = ConnectAsync(clientSocket, new Uri($"ws://{Guid.NewGuid():N}"), cts.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_CancellationRequestedInflightConnect_ThrowsOperationCanceledException()
        {
            using (var clientSocket = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource();
                Task t = ConnectAsync(clientSocket, new Uri($"ws://{Guid.NewGuid():N}"), cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }
    }

    [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    public class ConnectTest : ConnectTestBase
    {
        public ConnectTest(ITestOutputHelper output) : base(output) { }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1895")]
        [Theory, MemberData(nameof(UnavailableWebSocketServers))]
        public Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
            => RunClient_ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(server, exceptionMessage, errorCode);

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
    }
}
