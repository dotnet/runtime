// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public abstract class WebSocketCreateTest
    {
        protected abstract WebSocket CreateFromStream(Stream stream, bool isServer, string subProtocol, TimeSpan keepAliveInterval);

        [Fact]
        public void CreateFromStream_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("stream", () => CreateFromStream(null, true, "subProtocol", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("stream", () => CreateFromStream(new MemoryStream(new byte[100], writable: false), true, "subProtocol", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("stream", () => CreateFromStream(new UnreadableStream(), true, "subProtocol", TimeSpan.FromSeconds(30)));

            AssertExtensions.Throws<ArgumentException>("subProtocol", () => CreateFromStream(new MemoryStream(), true, "    ", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("subProtocol", () => CreateFromStream(new MemoryStream(), true, "\xFF", TimeSpan.FromSeconds(30)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () => CreateFromStream(new MemoryStream(), true, "subProtocol", TimeSpan.FromSeconds(-2)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(14)]
        [InlineData(4096)]
        public void CreateFromStream_ValidBufferSizes_CreatesWebSocket(int bufferSize)
        {
            Assert.NotNull(CreateFromStream(new MemoryStream(), false, null, Timeout.InfiniteTimeSpan));
            Assert.NotNull(CreateFromStream(new MemoryStream(), true, null, Timeout.InfiniteTimeSpan));
        }

        [OuterLoop("Uses external servers")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task WebSocketProtocol_CreateFromConnectedStream_CanSendReceiveData(Uri echoUri)
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool secure = echoUri.Scheme == "wss";
                client.Connect(echoUri.Host, secure ? 443 : 80);

                using (Stream stream = await CreateWebSocketStream(echoUri, client, secure))
                using (WebSocket socket = CreateFromStream(stream, false, null, TimeSpan.FromSeconds(10)))
                {
                    Assert.NotNull(socket);
                    Assert.Equal(WebSocketState.Open, socket.State);

                    string expected = "Hello World!";
                    ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(expected));
                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

                    buffer = new ArraySegment<byte>(new byte[buffer.Count]);
                    await socket.ReceiveAsync(buffer, CancellationToken.None);

                    Assert.Equal(expected, Encoding.UTF8.GetString(buffer.Array));
                }
            }
        }

        [Fact]
        public async Task ReceiveAsync_UTF8SplitAcrossMultipleBuffers_ValidDataReceived()
        {
            // 1 character - 2 bytes
            byte[] payload = Encoding.UTF8.GetBytes("\u00E6");
            var frame = new byte[payload.Length + 2];
            frame[0] = 0x81; // FIN = true, Opcode = Text
            frame[1] = (byte)payload.Length;
            Array.Copy(payload, 0, frame, 2, payload.Length);

            using (var stream = new MemoryStream(frame, writable: true))
            {
                WebSocket websocket = CreateFromStream(stream, false, "null", Timeout.InfiniteTimeSpan);

                // read first half of the multi-byte character
                var recvBuffer = new byte[1];
                WebSocketReceiveResult result = await websocket.ReceiveAsync(new ArraySegment<byte>(recvBuffer), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(1, result.Count);
                Assert.Equal(0xc3, recvBuffer[0]);

                // read second half of the multi-byte character
                result = await websocket.ReceiveAsync(new ArraySegment<byte>(recvBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(1, result.Count);
                Assert.Equal(0xa6, recvBuffer[0]);
            }
        }

        [Fact]
        public async Task ReceiveAsync_ServerSplitHeader_ValidDataReceived()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                await client.ConnectAsync(listener.LocalEndPoint);
                using (Socket server = await listener.AcceptAsync())
                {
                    WebSocket websocket = CreateFromStream(new NetworkStream(server, ownsSocket: false), isServer: true, null, Timeout.InfiniteTimeSpan);

                    // Send a full packet and a partial packet
                    var packets = new byte[7 + 11 + 4];
                    IList<byte> packet0 = new ArraySegment<byte>(packets, 0, 7);
                    packet0[0] = 0x82; // fin, binary
                    packet0[1] = 0x81; // masked, 1-byte length
                    packet0[6] = 42; // content

                    IList<byte> partialPacket1 = new ArraySegment<byte>(packets, 7, 11);
                    partialPacket1[0] = 0x82; // fin, binary
                    partialPacket1[1] = 0xFF; // masked, 8-byte length
                    partialPacket1[9] = 1; // length == 1

                    IList<byte> remainderPacket1 = new ArraySegment<byte>(packets, 7 + 11, 4);
                    remainderPacket1[3] = 84; // content

                    await client.SendAsync(new ArraySegment<byte>(packets, 0, packet0.Count + partialPacket1.Count), SocketFlags.None);

                    // Read the first packet
                    byte[] received = new byte[1];
                    WebSocketReceiveResult r = await websocket.ReceiveAsync(new ArraySegment<byte>(received), default);
                    Assert.True(r.EndOfMessage);
                    Assert.Equal(1, r.Count);
                    Assert.Equal(42, received[0]);

                    // Read the next packet, which is partial, then complete it.
                    // Partial read shouldn't cause a failure.
                    Task<WebSocketReceiveResult> tr = websocket.ReceiveAsync(new ArraySegment<byte>(received), default);
                    Assert.False(tr.IsCompleted);
                    await client.SendAsync((ArraySegment<byte>)remainderPacket1, SocketFlags.None);
                    r = await tr;
                    Assert.True(r.EndOfMessage);
                    Assert.Equal(1, r.Count);
                    Assert.Equal(84, received[0]);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory]
        [MemberData(nameof(EchoServersAndBoolean))]
        public async Task WebSocketProtocol_CreateFromConnectedStream_CloseAsyncClosesStream(Uri echoUri, bool explicitCloseAsync)
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool secure = echoUri.Scheme == "wss";
                client.Connect(echoUri.Host, secure ? 443 : 80);

                using (Stream stream = await CreateWebSocketStream(echoUri, client, secure))
                {
                    using (WebSocket socket = CreateFromStream(stream, false, null, TimeSpan.FromSeconds(10)))
                    {
                        Assert.NotNull(socket);
                        Assert.Equal(WebSocketState.Open, socket.State);

                        Assert.True(stream.CanRead);
                        Assert.True(stream.CanWrite);

                        if (explicitCloseAsync) // make sure CloseAsync ends up disposing the stream
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            Assert.False(stream.CanRead);
                            Assert.False(stream.CanWrite);
                        }
                    }

                    Assert.False(stream.CanRead);
                    Assert.False(stream.CanWrite);
                }
            }
        }

        [ActiveIssue(36016)]
        [OuterLoop("Uses external servers")]
        [Theory]
        [MemberData(nameof(EchoServersAndBoolean))]
        public async Task WebSocketProtocol_CreateFromConnectedStream_CloseAsyncAfterCloseReceivedClosesStream(Uri echoUri, bool useCloseOutputAsync)
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool secure = echoUri.Scheme == "wss";
                client.Connect(echoUri.Host, secure ? 443 : 80);

                using (Stream stream = await CreateWebSocketStream(echoUri, client, secure))
                using (WebSocket socket = CreateFromStream(stream, false, null, TimeSpan.FromSeconds(10)))
                {
                    Assert.NotNull(socket);
                    Assert.Equal(WebSocketState.Open, socket.State);

                    // Ask server to send us a close
                    await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(".close")), WebSocketMessageType.Text, true, default);

                    // Verify received server-initiated close message.
                    WebSocketReceiveResult recvResult = await socket.ReceiveAsync(new ArraySegment<byte>(new byte[256]), default);
                    Assert.Equal(WebSocketCloseStatus.NormalClosure, recvResult.CloseStatus);
                    Assert.Equal(WebSocketCloseStatus.NormalClosure, socket.CloseStatus);
                    Assert.Equal(WebSocketState.CloseReceived, socket.State);

                    Assert.True(stream.CanRead);
                    Assert.True(stream.CanWrite);

                    await (useCloseOutputAsync ?
                        socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None) :
                        socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None));

                    Assert.False(stream.CanRead);
                    Assert.False(stream.CanWrite);
                }
            }
        }

        private static async Task<Stream> CreateWebSocketStream(Uri echoUri, Socket client, bool secure)
        {
            Stream stream = new NetworkStream(client, ownsSocket: false);

            if (secure)
            {
                var ssl = new SslStream(stream, leaveInnerStreamOpen: true, delegate { return true; });
                await ssl.AuthenticateAsClientAsync(echoUri.Host);
                stream = ssl;
            }

            using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1, leaveOpen: true))
            {
                await writer.WriteAsync($"GET {echoUri.PathAndQuery} HTTP/1.1\r\n");
                await writer.WriteAsync($"Host: {echoUri.Host}\r\n");
                await writer.WriteAsync($"Upgrade: websocket\r\n");
                await writer.WriteAsync($"Connection: Upgrade\r\n");
                await writer.WriteAsync($"Sec-WebSocket-Version: 13\r\n");
                await writer.WriteAsync($"Sec-WebSocket-Key: {Convert.ToBase64String(Guid.NewGuid().ToByteArray())}\r\n");
                await writer.WriteAsync($"\r\n");
            }

            using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1, leaveOpen: true))
            {
                string statusLine = await reader.ReadLineAsync();
                Assert.NotEmpty(statusLine);
                Assert.Equal("HTTP/1.1 101 Switching Protocols", statusLine);
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync()));
            }

            return stream;
        }

        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.WebSockets.EchoServers;
        public static readonly object[][] EchoServersAndBoolean = EchoServers.SelectMany(o => new object[][]
        {
            new object[] { o[0], false },
            new object[] { o[0], true }
        }).ToArray();

        protected sealed class UnreadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => true;
            public override bool CanWrite => true;
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => throw new NotImplementedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }
    }
}
