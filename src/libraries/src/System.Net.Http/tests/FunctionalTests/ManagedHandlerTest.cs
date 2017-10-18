// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class ManagedHandler_HttpProtocolTests : HttpProtocolTests
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientTest : HttpClientTest
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_DiagnosticsTest : DiagnosticsTest
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public ManagedHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_PostScenarioTest : PostScenarioTest
    {
        public ManagedHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_ResponseStreamTest : ResponseStreamTest
    {
        public ManagedHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest
    {
        public ManagedHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientMiniStress : HttpClientMiniStress
    {
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public ManagedHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseManagedHandler => true;
    }

    public sealed class ManagedHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public ManagedHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseManagedHandler => true;
    }

    // TODO #23141: Socket's don't support canceling individual operations, so ReadStream on NetworkStream
    // isn't cancelable once the operation has started.  We either need to wrap the operation with one that's
    // "cancelable", meaning that the underlying operation will still be running even though we've returned "canceled",
    // or we need to just recognize that cancellation in such situations can be left up to the caller to do the
    // same thing if it's really important.
    //public sealed class ManagedHandler_CancellationTest : CancellationTest
    //{
    //    public ManagedHandler_CancellationTest(ITestOutputHelper output) : base(output) { }
    //    protected override bool UseManagedHandler => true;
    //}

    // TODO #23142: The managed handler doesn't currently track how much data was written for the response headers.
    //public sealed class ManagedHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    //{
    //    protected override bool UseManagedHandler => true;
    //}

    public sealed class ManagedHandler_HttpClientHandler_DuplexCommunication_Test : HttpClientTestBase
    {
        protected override bool UseManagedHandler => true;

        [Fact]
        public async Task SendBytesBackAndForthBetweenClientAndServer_Success()
        {
            using (HttpClient client = CreateHttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);
                var ep = (IPEndPoint)listener.LocalEndPoint;

                var clientToServerStream = new ProducerConsumerStream();
                clientToServerStream.WriteByte(0);

                var reqMsg = new HttpRequestMessage
                {
                    RequestUri = new Uri($"http://{ep.Address}:{ep.Port}/"),
                    Content = new StreamContent(clientToServerStream),
                };
                Task<HttpResponseMessage> req = client.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);

                using (Socket server = await listener.AcceptAsync())
                using (var serverStream = new NetworkStream(server, ownsSocket: false))
                {
                    // Skip request headers.
                    while (true)
                    {
                        if (serverStream.ReadByte() == '\r')
                        {
                            serverStream.ReadByte();
                            break;
                        }
                        while (serverStream.ReadByte() != '\r') { }
                        serverStream.ReadByte();
                    }

                    // Send response headers.
                    await server.SendAsync(
                        new ArraySegment<byte>(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\n\r\n")),
                        SocketFlags.None);

                    HttpResponseMessage resp = await req;
                    Stream serverToClientStream = await resp.Content.ReadAsStreamAsync();

                    // Communication should now be open between the client and server.
                    // Ping pong bytes back and forth.
                    for (byte i = 0; i < 100; i++)
                    {
                        // Send a byte from the client to the server.  The server will receive
                        // the byte as a chunk.
                        if (i > 0) clientToServerStream.WriteByte(i); // 0 was already seeded when the stream was created above
                        Assert.Equal('1', serverStream.ReadByte());
                        Assert.Equal('\r', serverStream.ReadByte());
                        Assert.Equal('\n', serverStream.ReadByte());
                        Assert.Equal(i, serverStream.ReadByte());
                        Assert.Equal('\r', serverStream.ReadByte());
                        Assert.Equal('\n', serverStream.ReadByte());

                        // Send a byte from the server to the client.  The client will receive
                        // the byte on its own, with HttpClient stripping away the chunk encoding.
                        serverStream.WriteByte(i);
                        Assert.Equal(i, serverToClientStream.ReadByte());
                    }

                    clientToServerStream.DoneWriting();
                    server.Shutdown(SocketShutdown.Send);
                    Assert.Equal(-1, clientToServerStream.ReadByte());
                }
            }
        }

        private sealed class ProducerConsumerStream : Stream
        {
            private readonly BlockingCollection<byte[]> _buffers = new BlockingCollection<byte[]>();
            private ArraySegment<byte> _remaining;

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count > 0)
                {
                    byte[] tmp = new byte[count];
                    Buffer.BlockCopy(buffer, offset, tmp, 0, count);
                    _buffers.Add(tmp);
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count > 0)
                {
                    if (_remaining.Count == 0)
                    {
                        if (!_buffers.TryTake(out byte[] tmp, Timeout.Infinite))
                        {
                            return 0;
                        }
                        _remaining = new ArraySegment<byte>(tmp, 0, tmp.Length);
                    }

                    if (_remaining.Count <= count)
                    {
                        count = _remaining.Count;
                        Buffer.BlockCopy(_remaining.Array, _remaining.Offset, buffer, offset, count);
                        _remaining = default(ArraySegment<byte>);
                    }
                    else
                    {
                        Buffer.BlockCopy(_remaining.Array, _remaining.Offset, buffer, offset, count);
                        _remaining = new ArraySegment<byte>(_remaining.Array, _remaining.Offset + count, _remaining.Count - count);
                    }
                }

                return count;
            }

            public void DoneWriting() => _buffers.CompleteAdding();

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
        }

    }

    public sealed class ManagedHandler_HttpClientHandler_ConnectionPooling_Test : HttpClientTestBase
    {
        protected override bool UseManagedHandler => true;

        // TODO: Currently the subsequent tests sometimes fail/hang with WinHttpHandler / CurlHandler.
        // In theory they should pass with any handler that does appropriate connection pooling.
        // We should understand why they sometimes fail there and ideally move them to be
        // used by all handlers this test project tests.

        [Fact]
        public async Task MultipleIterativeRequests_SameConnectionReused()
        {
            using (HttpClient client = CreateHttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);
                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http://{ep.Address}:{ep.Port}/");

                string responseBody =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                    "Content-Length: 0\r\n" +
                    "\r\n";

                Task<string> firstRequest = client.GetStringAsync(uri);
                using (Socket server = await listener.AcceptAsync())
                using (var serverStream = new NetworkStream(server, ownsSocket: false))
                using (var serverReader = new StreamReader(serverStream))
                {
                    while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync()));
                    await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await firstRequest;

                    Task<Socket> secondAccept = listener.AcceptAsync(); // shouldn't complete

                    Task<string> additionalRequest = client.GetStringAsync(uri);
                    while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync()));
                    await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await additionalRequest;

                    Assert.False(secondAccept.IsCompleted, $"Second accept should never complete");
                }
            }
        }

        [OuterLoop("Incurs a delay")]
        [Fact]
        public async Task ServerDisconnectsAfterInitialRequest_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(100);
                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http://{ep.Address}:{ep.Port}/");

                string responseBody =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                    "Content-Length: 0\r\n" +
                    "\r\n";

                // Make multiple requests iteratively.
                for (int i = 0; i < 2; i++)
                {
                    Task<string> request = client.GetStringAsync(uri);
                    using (Socket server = await listener.AcceptAsync())
                    using (var serverStream = new NetworkStream(server, ownsSocket: false))
                    using (var serverReader = new StreamReader(serverStream))
                    {
                        while (!string.IsNullOrWhiteSpace(await serverReader.ReadLineAsync()));
                        await server.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                        await request;

                        server.Shutdown(SocketShutdown.Both);
                        if (i == 0)
                        {
                            await Task.Delay(2000); // give client time to see the closing before next connect
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task ServerSendsConnectionClose_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(100);
                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http://{ep.Address}:{ep.Port}/");

                string responseBody =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                    "Content-Length: 0\r\n" +
                    "Connection: close\r\n" +
                    "\r\n";

                // Make multiple requests iteratively.
                Task<string> request1 = client.GetStringAsync(uri);
                using (Socket server1 = await listener.AcceptAsync())
                using (var serverStream1 = new NetworkStream(server1, ownsSocket: false))
                using (var serverReader1 = new StreamReader(serverStream1))
                {
                    while (!string.IsNullOrWhiteSpace(await serverReader1.ReadLineAsync()));
                    await server1.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                    await request1;

                    Task<string> request2 = client.GetStringAsync(uri);
                    using (Socket server2 = await listener.AcceptAsync())
                    using (var serverStream2 = new NetworkStream(server2, ownsSocket: false))
                    using (var serverReader2 = new StreamReader(serverStream2))
                    {
                        while (!string.IsNullOrWhiteSpace(await serverReader2.ReadLineAsync()));
                        await server2.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(responseBody)), SocketFlags.None);
                        await request2;
                    }
                }
            }
        }
    }
}
