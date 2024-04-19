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
    public sealed class InvokerConnectTest : ConnectTest
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

    public sealed class HttpClientConnectTest : ConnectTest
    {
        public HttpClientConnectTest(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    public class ConnectTest : ClientWebSocketTestBase
    {
        public ConnectTest(ITestOutputHelper output) : base(output) { }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1895")]
        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(UnavailableWebSocketServers))]
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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task EchoBinaryMessage_Success(Uri server)
        {
            await TestEcho(server, WebSocketMessageType.Binary, TimeOutMilliseconds, _output);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task EchoTextMessage_Success(Uri server)
        {
            await TestEcho(server, WebSocketMessageType.Text, TimeOutMilliseconds, _output);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoHeadersServers))]
        [SkipOnPlatform(TestPlatforms.Browser, "SetRequestHeader not supported on browser")]
        public async Task ConnectAsync_AddCustomHeaders_Success(Uri server)
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "SetRequestHeader not supported on browser")]
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
                    await ConnectAsync(cws, uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);
                Assert.True(headers.TryGetValue("Host", out string host));
                Assert.Equal(expectedHost, host);
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
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
                    ConnectAsync(cws, ub.Uri, cts.Token));
                _output.WriteLine(ex.Message);
                Assert.True(ex.WebSocketErrorCode == WebSocketError.Faulted ||
                    ex.WebSocketErrorCode == WebSocketError.NotAWebSocket, $"Actual WebSocketErrorCode {ex.WebSocketErrorCode} {ex.InnerException?.Message} \n {ex}");
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
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

                await ConnectAsync(cws, ub.Uri, cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(AcceptedProtocol, cws.SubProtocol);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "SetRequestHeader not supported on Browser")]
        public async Task ConnectAsync_NonStandardRequestHeaders_HeadersAddedWithoutValidation()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientSocket.Options.SetRequestHeader("Authorization", "AWS4-HMAC-SHA256 Credential=PLACEHOLDER /20190301/us-east-2/neptune-db/aws4_request, SignedHeaders=host;x-amz-date, Signature=b8155de54d9faab00000000000000000000000000a07e0d7dda49902e4d9202");
                    await ConnectAsync(clientSocket, uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.NotNull(await LoopbackHelper.WebSocketHandshakeAsync(connection));
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        [SkipOnPlatform(TestPlatforms.Browser, "Proxy not supported on Browser")]
        public async Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(Uri server)
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task ConnectAsync_CancellationRequestedAfterConnect_ThrowsOperationCanceledException()
        {
            var releaseServer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                var clientSocket = new ClientWebSocket();
                try
                {
                    var cts = new CancellationTokenSource();
                    Task t = ConnectAsync(clientSocket, uri, cts.Token);
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "CollectHttpResponseDetails not supported on Browser")]
        public async Task ConnectAsync_HttpResponseDetailsCollectedOnFailure()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientWebSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientWebSocket.Options.CollectHttpResponseDetails = true;
                    Task t = ConnectAsync(clientWebSocket, uri, cts.Token);
                    await Assert.ThrowsAnyAsync<WebSocketException>(() => t);

                    Assert.Equal(HttpStatusCode.Unauthorized, clientWebSocket.HttpStatusCode);
                    Assert.NotEmpty(clientWebSocket.HttpResponseHeaders);
                }
            }, server => server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "CollectHttpResponseDetails not supported on Browser")]
        public async Task ConnectAsync_HttpResponseDetailsCollectedOnFailure_CustomHeader()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientWebSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientWebSocket.Options.CollectHttpResponseDetails = true;
                    Task t = ConnectAsync(clientWebSocket, uri, cts.Token);
                    await Assert.ThrowsAnyAsync<WebSocketException>(() => t);

                    Assert.Equal(HttpStatusCode.Unauthorized, clientWebSocket.HttpStatusCode);
                    Assert.NotEmpty(clientWebSocket.HttpResponseHeaders);
                    Assert.Contains("X-CustomHeader1", clientWebSocket.HttpResponseHeaders);
                    Assert.Contains("X-CustomHeader2", clientWebSocket.HttpResponseHeaders);
                    Assert.NotNull(clientWebSocket.HttpResponseHeaders.Values);
                }
            }, server => server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "X-CustomHeader1: Value1\r\nX-CustomHeader2: Value2\r\n"), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "CollectHttpResponseDetails not supported on Browser")]
        public async Task ConnectAsync_HttpResponseDetailsCollectedOnSuccess_Extensions()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientWebSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientWebSocket.Options.CollectHttpResponseDetails = true;
                    await ConnectAsync(clientWebSocket, uri, cts.Token);

                    Assert.Equal(HttpStatusCode.SwitchingProtocols, clientWebSocket.HttpStatusCode);
                    Assert.NotEmpty(clientWebSocket.HttpResponseHeaders);
                    Assert.Contains("Sec-WebSocket-Extensions", clientWebSocket.HttpResponseHeaders);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection, "X-CustomHeader1");
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }
    }
}
