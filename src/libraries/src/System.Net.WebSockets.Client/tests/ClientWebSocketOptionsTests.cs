// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Test.Common;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ClientWebSocketOptionsTests : ClientWebSocketTestBase
    {
        // Windows 10 Version 1709 introduced the necessary APIs for the UAP version of
        // ClientWebSocket.ConnectAsync to carry out mutual TLS authentication.
        public static bool ClientCertificatesSupported => !PlatformDetection.IsUap;

        public ClientWebSocketOptionsTests(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(WebSocketsSupported))]
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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
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

        [ActiveIssue(28027)]
        [OuterLoop] // TODO: Issue #11345
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
    }
}
