// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpListenerContextTests : IDisposable
    {
        private HttpListenerFactory Factory { get; }
        private ClientWebSocket Socket { get; }

        public HttpListenerContextTests()
        {
            Factory = new HttpListenerFactory();
            Socket = new ClientWebSocket();
        }

        public void Dispose()
        {
            Factory.Dispose();
            Socket.Dispose();
        }

        public static bool IsNotWindows7OrUapCore { get; } = !PlatformDetection.IsWindows7 && PlatformDetection.IsNotOneCoreUAP;

        public static IEnumerable<object[]> SubProtocol_TestData()
        {
            yield return new object[] { new string[0], null };

            yield return new object[] { new string[] { "MyProtocol1" }, null };
            yield return new object[] { new string[] { "MyProtocol" }, "MyProtocol" };
            yield return new object[] { new string[] { "MyProtocol" }, "myPROTOCOL" };

            yield return new object[] { new string[] { "MyProtocol1", "MyProtocol2" }, null };
            yield return new object[] { new string[] { "MyProtocol1", "MyProtocol2" }, "MyProtocol2" };
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [MemberData(nameof(SubProtocol_TestData))]
        public async Task AcceptWebSocketAsync_ValidSubProtocol_Success(string[] clientProtocols, string serverProtocol)
        {
            HttpListenerContext context = await GetWebSocketContext(clientProtocols);
            HttpListenerWebSocketContext socketContext = await context.AcceptWebSocketAsync(serverProtocol);
            Assert.Equal(serverProtocol, socketContext.WebSocket.SubProtocol);
        }

        [ConditionalFact(nameof(IsNotWindows7OrUapCore))]
        // Both the managed and Windows implementations send headers to the socket during connection.
        // The Windows implementation fails with error code 1229: An operation was attempted on a nonexistent network connection.
        [ActiveIssue(18128, TestPlatforms.AnyUnix)]
        public async Task AcceptWebSocketAsync_SocketSpoofingAsWebSocket_ThrowsWebSocketException()
        {
            await GetSocketContext(new string[] { "Connection: Upgrade", "Upgrade: websocket", "Sec-WebSocket-Version: 13", "Sec-WebSocket-Key: Key" }, async context =>
            {
                await Assert.ThrowsAsync<WebSocketException>(() => context.AcceptWebSocketAsync(null));
            });
        }

        [ConditionalFact(nameof(IsNotWindows7OrUapCore))]
        public async Task AcceptWebSocketAsync_UnsupportedProtocol_ThrowsWebSocketException()
        {
            HttpListenerContext context = await GetWebSocketContext(new string[] { "MyProtocol" });
            await Assert.ThrowsAsync<WebSocketException>(() => context.AcceptWebSocketAsync("MyOtherProtocol"));
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [InlineData("Connection: ")]
        [InlineData("Connection: Connection\r\nUpgrade: ")]
        [InlineData("Connection: Test1\r\nUpgrade: Test2")]
        [InlineData("Connection: Upgrade\r\nUpgrade: Test2")]
        [InlineData("Connection: Upgrade\r\nUpgrade: websocket")]
        [InlineData("Connection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Version: ")]
        [InlineData("Connection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Version: 1")]
        [InlineData("Connection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Version: 13")]
        [InlineData("Connection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Version: 13\r\nSec-WebSocket-Key: ")]
        [InlineData("UnknownHeader: random")]
        public async Task AcceptWebSocketAsync_InvalidHeaders_ThrowsWebSocketException(string headers)
        {
            await GetSocketContext(new string[] { headers }, async context =>
            {
                await Assert.ThrowsAsync<WebSocketException>(() => context.AcceptWebSocketAsync(null));
            });
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("random(text")]
        [InlineData("random)text")]
        [InlineData("random<text")]
        [InlineData("random>text")]
        [InlineData("random@text")]
        [InlineData("random,text")]
        [InlineData("random;text")]
        [InlineData("random:text")]
        [InlineData("random\\text")]
        [InlineData("random\"text")]
        [InlineData("random/text")]
        [InlineData("random[text")]
        [InlineData("random]text")]
        [InlineData("random?text")]
        [InlineData("random=text")]
        [InlineData("random{text")]
        [InlineData("random}text")]
        [InlineData("\x19")]
        [InlineData("\x7f")]
        public async Task AcceptWebSocketAsync_InvalidSubProtocol_ThrowsArgumentException(string subProtocol)
        {
            HttpListenerContext context = await GetWebSocketContext();
            await Assert.ThrowsAsync<ArgumentException>("subProtocol", () => context.AcceptWebSocketAsync(subProtocol));
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [InlineData("!")]
        [InlineData("#")]
        [InlineData("YouDontKnowMe")]
        public async Task AcceptWebSocketAsync_NoSuchSubProtocol_ThrowsWebSocketException(string subProtocol)
        {
            HttpListenerContext context = await GetWebSocketContext();
            await Assert.ThrowsAsync<WebSocketException>(() => context.AcceptWebSocketAsync(subProtocol));
        }

        [ConditionalFact(nameof(IsNotWindows7OrUapCore))]
        public async Task AcceptWebSocketAsync_InvalidKeepAlive_ThrowsWebSocketException()
        {
            HttpListenerContext context = await GetWebSocketContext();

            TimeSpan keepAlive = TimeSpan.FromMilliseconds(-2);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("keepAliveInterval", () => context.AcceptWebSocketAsync(null, keepAlive));
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(255)]
        [InlineData(64 * 1024 + 1)]
        public async Task AcceptWebSocketAsync_InvalidReceiveBufferSize_ThrowsWebSocketException(int receiveBufferSize)
        {
            HttpListenerContext context = await GetWebSocketContext();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("receiveBufferSize", () => context.AcceptWebSocketAsync(null, receiveBufferSize, TimeSpan.MaxValue));
        }

        [ConditionalFact(nameof(IsNotWindows7OrUapCore))]   
        public async Task AcceptWebSocketAsync_NullArrayInArraySegment_ThrowsArgumentNullException()
        {
            HttpListenerContext context = await GetWebSocketContext();

            ArraySegment<byte> internalBuffer = new FakeArraySegment() { Array = null }.ToActual();
            await Assert.ThrowsAsync<ArgumentNullException>("internalBuffer.Array", () => context.AcceptWebSocketAsync(null, 1024, TimeSpan.MaxValue, internalBuffer));
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [InlineData(-1)]
        [InlineData(11)]
        public async Task AcceptWebSocketAsync_InvalidOffsetInArraySegment_ThrowsArgumentNullException(int offset)
        {
            HttpListenerContext context = await GetWebSocketContext();

            ArraySegment<byte> internalBuffer = new FakeArraySegment() { Array = new byte[10], Offset = offset }.ToActual();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("internalBuffer.Offset", () => context.AcceptWebSocketAsync(null, 1024, TimeSpan.MaxValue, internalBuffer));
        }

        [ConditionalTheory(nameof(IsNotWindows7OrUapCore))]
        [InlineData(0, -1)]
        [InlineData(0, 11)]
        [InlineData(10, 1)]
        [InlineData(9, 2)]
        public async Task AcceptWebSocketAsync_InvalidCountInArraySegment_ThrowsArgumentNullException(int offset, int count)
        {
            HttpListenerContext context = await GetWebSocketContext();

            ArraySegment<byte> internalBuffer = new FakeArraySegment() { Array = new byte[10], Offset = offset, Count = count }.ToActual();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("internalBuffer.Count", () => context.AcceptWebSocketAsync(null, 1024, TimeSpan.MaxValue, internalBuffer));
        }

        private async Task GetSocketContext(string[] headers, Func<HttpListenerContext, Task> contextAction)
        {
            using (Socket client = Factory.GetConnectedSocket())
            {
                client.Send(Factory.GetContent("1.1", "POST", null, "Text", headers, true));

                HttpListener listener = Factory.GetListener();
                await contextAction(await listener.GetContextAsync());
            }
        }

        private async Task<HttpListenerContext> GetWebSocketContext(string[] subProtocols = null)
        {
            if (subProtocols != null)
            {
                foreach (string subProtocol in subProtocols)
                {
                    Socket.Options.AddSubProtocol(subProtocol);
                }
            }

            var uriBuilder = new UriBuilder(Factory.ListeningUrl) { Scheme = "ws" };
            Task<HttpListenerContext> serverContextTask = Factory.GetListener().GetContextAsync();

            Task clientConnectTask = Socket.ConnectAsync(uriBuilder.Uri, CancellationToken.None);
            if (clientConnectTask == await Task.WhenAny(serverContextTask, clientConnectTask))
            {
                await clientConnectTask;
                Assert.True(false, "Client should not have completed prior to server sending response");
            }

            return await serverContextTask;
        }

        public struct FakeArraySegment
        {
            public byte[] Array;
            public int Offset;
            public int Count;

            public ArraySegment<byte> ToActual()
            {
                ArraySegmentWrapper wrapper = default(ArraySegmentWrapper);
                wrapper.Fake = this;
                return wrapper.Actual;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ArraySegmentWrapper
        {
            [FieldOffset(0)] public ArraySegment<byte> Actual;
            [FieldOffset(0)] public FakeArraySegment Fake;
        }

    }
}
