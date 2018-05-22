// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed class WebSocketProtocolTests
    {
        [Fact]
        public void CreateFromStream_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("stream",
                () => WebSocketProtocol.CreateFromStream(null, true, "subProtocol", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("stream",
                () => WebSocketProtocol.CreateFromStream(new MemoryStream(new byte[100], writable: false), true, "subProtocol", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("stream",
                () => WebSocketProtocol.CreateFromStream(new UnreadableStream(), true, "subProtocol", TimeSpan.FromSeconds(30)));

            AssertExtensions.Throws<ArgumentException>("subProtocol",
                () => WebSocketProtocol.CreateFromStream(new MemoryStream(), true, "    ", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("subProtocol",
                () => WebSocketProtocol.CreateFromStream(new MemoryStream(), true, "\xFF", TimeSpan.FromSeconds(30)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () =>
                WebSocketProtocol.CreateFromStream(new MemoryStream(), true, "subProtocol", TimeSpan.FromSeconds(-2)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(14)]
        [InlineData(4096)]
        public void CreateFromStream_ValidBufferSizes_Succeed(int bufferSize)
        {
            Assert.NotNull(WebSocketProtocol.CreateFromStream(new MemoryStream(), false, null, Timeout.InfiniteTimeSpan));
            Assert.NotNull(WebSocketProtocol.CreateFromStream(new MemoryStream(), true, null, Timeout.InfiniteTimeSpan));
        }

        [OuterLoop] // Connects to external server.
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task WebSocketProtocol_CreateFromConnectedStream_Succeeds(Uri echoUri)
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool secure = echoUri.Scheme == "wss";
                client.Connect(echoUri.Host, secure ? 443 : 80);

                Stream stream = new NetworkStream(client, ownsSocket: false);
                if (secure)
                {
                    SslStream ssl = new SslStream(stream, leaveInnerStreamOpen: true, delegate { return true; });
                    await ssl.AuthenticateAsClientAsync(echoUri.Host);
                    stream = ssl;
                }

                using (stream)
                {
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

                    using (WebSocket socket = WebSocketProtocol.CreateFromStream(stream, false, null, TimeSpan.FromSeconds(10)))
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
        }

        [Fact]
        public static async Task ManagedWebSocket_ReceiveUTF8SplitAcrossMultipleBuffers()
        {
            // 1 character - 2 bytes
            byte[] payload = Encoding.UTF8.GetBytes("\u00E6");
            var frame = new byte[payload.Length + 2];
            frame[0] = 0x81; // FIN = true, Opcode = Text
            frame[1] = (byte)payload.Length;
            Array.Copy(payload, 0, frame, 2, payload.Length);

            using (var stream = new MemoryStream(frame, writable: true))
            {
                WebSocket websocket = WebSocketProtocol.CreateFromStream(stream, false, "null", Timeout.InfiniteTimeSpan);

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

        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.WebSockets.EchoServers;

        private sealed class UnreadableStream : Stream
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
