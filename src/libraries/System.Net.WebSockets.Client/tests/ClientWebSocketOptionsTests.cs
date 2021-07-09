// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class ClientWebSocketOptionsTests : ClientWebSocketTestBase
    {
        public ClientWebSocketOptionsTests(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials not supported on browser")]
        public static void UseDefaultCredentials_Roundtrips()
        {
            var cws = new ClientWebSocket();
            Assert.False(cws.Options.UseDefaultCredentials);
            cws.Options.UseDefaultCredentials = true;
            Assert.True(cws.Options.UseDefaultCredentials);
            cws.Options.UseDefaultCredentials = false;
            Assert.False(cws.Options.UseDefaultCredentials);
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "Proxy not supported on browser")]
        public static void Proxy_Roundtrips()
        {
            var cws = new ClientWebSocket();

            Assert.NotNull(cws.Options.Proxy);
            Assert.Same(cws.Options.Proxy, cws.Options.Proxy);

            IWebProxy p = new WebProxy();
            cws.Options.Proxy = p;
            Assert.Same(p, cws.Options.Proxy);

            cws.Options.Proxy = null;
            Assert.Null(cws.Options.Proxy);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/43751")]
        public async Task Proxy_SetNull_ConnectsSuccessfully(Uri server)
        {
            for (int i = 0; i < 3; i++) // Connect and disconnect multiple times to exercise shared handler on netcoreapp
            {
                var ws = await WebSocketHelper.Retry(_output, async () =>
                {
                    var cws = new ClientWebSocket();
                    cws.Options.Proxy = null;
                    await cws.ConnectAsync(server, default);
                    return cws;
                });
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
                ws.Dispose();
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/25440")]
        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task Proxy_ConnectThruProxy_Success(Uri server)
        {
            string proxyServerUri = System.Net.Test.Common.Configuration.WebSockets.ProxyServerUri;
            if (string.IsNullOrEmpty(proxyServerUri))
            {
                _output.WriteLine("Skipping test...no proxy server defined.");
                return;
            }

            _output.WriteLine($"ProxyServer: {proxyServerUri}");

            IWebProxy proxy = new WebProxy(new Uri(proxyServerUri));
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(
                server,
                TimeOutMilliseconds,
                _output,
                default(TimeSpan),
                proxy))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                Assert.Equal(WebSocketState.Open, cws.State);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = "Normal Closure";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                // Verify a clean close frame handshake.
                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "Buffer not supported on browser")]
        public static void SetBuffer_InvalidArgs_Throws()
        {
            // Recreate the minimum WebSocket buffer size values from the .NET Framework version of WebSocket,
            // and pick the correct name of the buffer used when throwing an ArgumentOutOfRangeException.
            int minSendBufferSize = 1;
            int minReceiveBufferSize = 1;
            string bufferName = "buffer";

            var cws = new ClientWebSocket();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, minSendBufferSize));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => cws.Options.SetBuffer(minReceiveBufferSize, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 0, new ArraySegment<byte>(new byte[1])));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, minSendBufferSize, new ArraySegment<byte>(new byte[1])));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => cws.Options.SetBuffer(minReceiveBufferSize, 0, new ArraySegment<byte>(new byte[1])));
            AssertExtensions.Throws<ArgumentNullException>("buffer.Array", () => cws.Options.SetBuffer(minReceiveBufferSize, minSendBufferSize, default(ArraySegment<byte>)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(bufferName, () => cws.Options.SetBuffer(minReceiveBufferSize, minSendBufferSize, new ArraySegment<byte>(new byte[0])));
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "KeepAlive not supported on browser")]
        public static void KeepAliveInterval_Roundtrips()
        {
            var cws = new ClientWebSocket();
            Assert.True(cws.Options.KeepAliveInterval > TimeSpan.Zero);

            cws.Options.KeepAliveInterval = TimeSpan.Zero;
            Assert.Equal(TimeSpan.Zero, cws.Options.KeepAliveInterval);

            cws.Options.KeepAliveInterval = TimeSpan.MaxValue;
            Assert.Equal(TimeSpan.MaxValue, cws.Options.KeepAliveInterval);

            cws.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
            Assert.Equal(Timeout.InfiniteTimeSpan, cws.Options.KeepAliveInterval);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => cws.Options.KeepAliveInterval = TimeSpan.MinValue);
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "Certificates not supported on browser")]
        public void RemoteCertificateValidationCallback_Roundtrips()
        {
            using (var cws = new ClientWebSocket())
            {
                Assert.Null(cws.Options.RemoteCertificateValidationCallback);

                RemoteCertificateValidationCallback callback = delegate { return true; };
                cws.Options.RemoteCertificateValidationCallback = callback;
                Assert.Same(callback, cws.Options.RemoteCertificateValidationCallback);

                cws.Options.RemoteCertificateValidationCallback = null;
                Assert.Null(cws.Options.RemoteCertificateValidationCallback);
            }
        }

        [OuterLoop("Connects to remote service")]
        [ConditionalTheory(nameof(WebSocketsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Browser, "Certificates not supported on browser")]
        public async Task RemoteCertificateValidationCallback_PassedRemoteCertificateInfo(bool secure)
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // see https://github.com/dotnet/runtime/issues/1491#issuecomment-376392057 for more details
            }

            bool callbackInvoked = false;

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.RemoteCertificateValidationCallback = (source, cert, chain, errors) =>
                    {
                        Assert.NotNull(source);
                        Assert.NotNull(cert);
                        Assert.NotNull(chain);
                        Assert.NotEqual(SslPolicyErrors.None, errors);
                        callbackInvoked = true;
                        return true;
                    };
                    await cws.ConnectAsync(uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.NotNull(await LoopbackHelper.WebSocketHandshakeAsync(connection));
            }),
            new LoopbackServer.Options { UseSsl = secure, WebSocketEndpoint = true });

            Assert.Equal(secure, callbackInvoked);
        }

        [OuterLoop("Connects to remote service")]
        [ConditionalFact(nameof(WebSocketsSupported))]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials not supported on browser")]
        public async Task ClientCertificates_ValidCertificate_ServerReceivesCertificateAndConnectAsyncSucceeds()
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // see https://github.com/dotnet/runtime/issues/1491#issuecomment-376392057 for more details
            }

            using (X509Certificate2 clientCert = Test.Common.Configuration.Certificates.GetClientCertificate())
            {
                await LoopbackServer.CreateClientAndServerAsync(async uri =>
                {
                    using (var clientSocket = new ClientWebSocket())
                    using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                    {
                        clientSocket.Options.ClientCertificates.Add(clientCert);
                        clientSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };
                        await clientSocket.ConnectAsync(uri, cts.Token);
                    }
                }, server => server.AcceptConnectionAsync(async connection =>
                {
                    // Validate that the client certificate received by the server matches the one configured on
                    // the client-side socket.
                    SslStream sslStream = Assert.IsType<SslStream>(connection.Stream);
                    Assert.NotNull(sslStream.RemoteCertificate);
                    Assert.Equal(clientCert, new X509Certificate2(sslStream.RemoteCertificate));

                    // Complete the WebSocket upgrade over the secure channel. After this is done, the client-side
                    // ConnectAsync should complete.
                    Assert.NotNull(await LoopbackHelper.WebSocketHandshakeAsync(connection));
                }), new LoopbackServer.Options { UseSsl = true, WebSocketEndpoint = true });
            }
        }

        [ConditionalTheory(nameof(WebSocketsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [InlineData("ws")]
        [InlineData("wss")]
        [SkipOnPlatform(TestPlatforms.Browser, "Credentials not supported on browser")]
        public async Task Connect_ViaProxy_ProxyTunnelRequestIssued(string scheme)
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // see https://github.com/dotnet/runtime/issues/1491#issuecomment-376392057 for more details
            }

            bool connectionAccepted = false;

            await LoopbackServer.CreateClientAndServerAsync(async proxyUri =>
            {
                using (var cws = new ClientWebSocket())
                {
                    cws.Options.Proxy = new WebProxy(proxyUri);
                    WebSocketException wse = await Assert.ThrowsAnyAsync<WebSocketException>(async () => await cws.ConnectAsync(new Uri($"{scheme}://doesntmatter.invalid"), default));

                    // Inner exception should indicate proxy connect failure with the error code we send (403)
                    HttpRequestException hre = Assert.IsType<HttpRequestException>(wse.InnerException);
                    Assert.Contains("403", hre.Message);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                var lines = await connection.ReadRequestHeaderAsync();
                Assert.Contains("CONNECT", lines[0]);
                connectionAccepted = true;

                // Send non-success error code so that SocketsHttpHandler won't retry.
                await connection.SendResponseAsync(statusCode: HttpStatusCode.Forbidden);
                connection.Dispose();
            }));

            Assert.True(connectionAccepted);
        }
    }
}
