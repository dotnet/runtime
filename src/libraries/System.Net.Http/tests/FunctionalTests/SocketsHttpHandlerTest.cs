// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class SocketsHttpHandler_HttpProtocolTests : HttpProtocolTests
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientTest : HttpClientTest
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_DiagnosticsTest : DiagnosticsTest
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public SocketsHttpHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_PostScenarioTest : PostScenarioTest
    {
        public SocketsHttpHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_ResponseStreamTest : ResponseStreamTest
    {
        public SocketsHttpHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest
    {
        public SocketsHttpHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientMiniStress : HttpClientMiniStress
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public SocketsHttpHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public SocketsHttpHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        protected override bool UseSocketsHttpHandler => true;
        protected override bool SupportsIdna => true;
    }

    public sealed class SocketsHttpHandler_HttpRetryProtocolTests : HttpRetryProtocolTests
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpCookieProtocolTests : HttpCookieProtocolTests
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        protected override bool UseSocketsHttpHandler => true;

        // TODO #27235:
        // Remove these reflection helpers once the property is exposed.
        private TimeSpan GetConnectTimeout(SocketsHttpHandler handler) =>
            (TimeSpan)typeof(SocketsHttpHandler).GetProperty("ConnectTimeout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handler);
        private void SetConnectTimeout(SocketsHttpHandler handler, TimeSpan timeout)
        {
            try
            {
                typeof(SocketsHttpHandler).GetProperty("ConnectTimeout", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(handler, timeout);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null) throw tie.InnerException;
                throw;
            }
        }

        // TODO #27145:
        // Remove these reflection helpers once the property is exposed.
        private TimeSpan GetExpect100ContinueTimeout(SocketsHttpHandler handler) =>
            (TimeSpan)typeof(SocketsHttpHandler).GetProperty("Expect100ContinueTimeout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handler);
        private void SetExpect100ContinueTimeout(SocketsHttpHandler handler, TimeSpan timeout)
        {
            try
            {
                typeof(SocketsHttpHandler).GetProperty("Expect100ContinueTimeout", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(handler, timeout);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null) throw tie.InnerException;
                throw;
            }
        }

        [Fact]
        public void ConnectTimeout_Default()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, GetConnectTimeout(handler));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(int.MaxValue + 1L)]
        public void ConnectTimeout_InvalidValues(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => SetConnectTimeout(handler, TimeSpan.FromMilliseconds(ms)));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MaxValue)]
        public void ConnectTimeout_ValidValues_Roundtrip(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                SetConnectTimeout(handler, TimeSpan.FromMilliseconds(ms));
                Assert.Equal(TimeSpan.FromMilliseconds(ms), GetConnectTimeout(handler));
            }
        }

        [Fact]
        public void ConnectTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (var client = new HttpClient(handler))
            {
                SetConnectTimeout(handler, TimeSpan.FromMilliseconds(int.MaxValue));
                client.GetAsync("http://" + Guid.NewGuid().ToString("N")); // ignoring failure
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), GetConnectTimeout(handler));
                Assert.Throws<InvalidOperationException>(() => SetConnectTimeout(handler, TimeSpan.FromMilliseconds(1)));
            }
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_TimesOutSSLAuth_Throws()
        {
            var releaseServer = new TaskCompletionSource<bool>();
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = new SocketsHttpHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    SetConnectTimeout(handler, TimeSpan.FromSeconds(1));

                    var sw = Stopwatch.StartNew();
                    await Assert.ThrowsAsync<OperationCanceledException>(() =>
                        invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                            new UriBuilder(uri) { Scheme = "https" }.ToString()), default));
                    sw.Stop();

                    Assert.InRange(sw.ElapsedMilliseconds, 500, 10_000);
                    releaseServer.SetResult(true);
                }
            }, server => releaseServer.Task); // doesn't establish SSL connection
        }


        [Fact]
        public void Expect100ContinueTimeout_Default()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(1), GetExpect100ContinueTimeout(handler));
            }
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(int.MaxValue + 1L)]
        public void Expect100ContinueTimeout_InvalidValues(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => SetExpect100ContinueTimeout(handler, TimeSpan.FromMilliseconds(ms)));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MaxValue)]
        public void Expect100ContinueTimeout_ValidValues_Roundtrip(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                SetExpect100ContinueTimeout(handler, TimeSpan.FromMilliseconds(ms));
                Assert.Equal(TimeSpan.FromMilliseconds(ms), GetExpect100ContinueTimeout(handler));
            }
        }

        [Fact]
        public void Expect100ContinueTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (var client = new HttpClient(handler))
            {
                SetExpect100ContinueTimeout(handler, TimeSpan.FromMilliseconds(int.MaxValue));
                client.GetAsync("http://" + Guid.NewGuid().ToString("N")); // ignoring failure
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), GetExpect100ContinueTimeout(handler));
                Assert.Throws<InvalidOperationException>(() => SetExpect100ContinueTimeout(handler, TimeSpan.FromMilliseconds(1)));
            }
        }

        [OuterLoop("Incurs significant delay")]
        [Fact]
        public async Task Expect100Continue_WaitsExpectedPeriodOfTimeBeforeSendingContent()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = new SocketsHttpHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    TimeSpan delay = TimeSpan.FromSeconds(3);

                    // TODO #27145: Remove reflection once publicly exposed
                    typeof(SocketsHttpHandler).GetProperty("Expect100ContinueTimeout", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(handler, delay);

                    var tcs = new TaskCompletionSource<bool>();
                    var content = new SetTcsContent(new MemoryStream(new byte[1]), tcs);
                    var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
                    request.Headers.ExpectContinue = true;

                    var sw = Stopwatch.StartNew();
                    (await invoker.SendAsync(request, default)).Dispose();
                    sw.Stop();

                    Assert.InRange(sw.Elapsed, delay - TimeSpan.FromSeconds(.5), delay * 5); // arbitrary wiggle room
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.Reader.ReadAsync(new char[1]);
                    await connection.SendResponseAsync();
                });
            });
        }

        private sealed class SetTcsContent : StreamContent
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public SetTcsContent(Stream stream, TaskCompletionSource<bool> tcs) : base(stream) => _tcs = tcs;

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                _tcs.SetResult(true);
                return base.SerializeToStreamAsync(stream, context);
            }
        }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        protected override bool UseSocketsHttpHandler => true;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_DuplexCommunication_Test : HttpClientTestBase
    {
        protected override bool UseSocketsHttpHandler => true;

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
                        // Read it in various ways.
                        serverStream.WriteByte(i);
                        var buffer = new byte[1];
                        switch (i % 6)
                        {
                            case 0:
                                Assert.Equal(i, serverToClientStream.ReadByte());
                                break;
                            case 1:
                                Assert.Equal(1, serverToClientStream.Read(buffer, 0, 1));
                                Assert.Equal(i, buffer[0]);
                                break;
                            case 2:
                                Assert.Equal(1, serverToClientStream.Read(new Span<byte>(buffer)));
                                Assert.Equal(i, buffer[0]);
                                break;
                            case 3:
                                Assert.Equal(1, await serverToClientStream.ReadAsync(buffer, 0, 1));
                                Assert.Equal(i, buffer[0]);
                                break;
                            case 4:
                                Assert.Equal(1, await serverToClientStream.ReadAsync(new Memory<byte>(buffer)));
                                Assert.Equal(i, buffer[0]);
                                break;
                            case 5:
                                Assert.Equal(1, await Task.Factory.FromAsync(serverToClientStream.BeginRead, serverToClientStream.EndRead, buffer, 0, 1, null));
                                Assert.Equal(i, buffer[0]);
                                break;
                        }
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

    public sealed class SocketsHttpHandler_ConnectionUpgrade_Test : HttpClientTestBase
    {
        protected override bool UseSocketsHttpHandler => true;

        [Fact]
        public async Task UpgradeConnection_ReturnsReadableAndWritableStream()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // We need to use ResponseHeadersRead here, otherwise we will hang trying to buffer the response body.
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Task<List<string>> serverTask = connection.ReadRequestHeaderAndSendCustomResponseAsync($"HTTP/1.1 101 Switching Protocols\r\nDate: {DateTimeOffset.UtcNow:R}\r\n\r\n");

                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        using (Stream clientStream = await (await getResponseTask).Content.ReadAsStreamAsync())
                        {
                            // Boolean properties returning correct values
                            Assert.True(clientStream.CanWrite);
                            Assert.True(clientStream.CanRead);
                            Assert.False(clientStream.CanSeek);

                            // Not supported operations
                            Assert.Throws<NotSupportedException>(() => clientStream.Length);
                            Assert.Throws<NotSupportedException>(() => clientStream.Position);
                            Assert.Throws<NotSupportedException>(() => clientStream.Position = 0);
                            Assert.Throws<NotSupportedException>(() => clientStream.Seek(0, SeekOrigin.Begin));
                            Assert.Throws<NotSupportedException>(() => clientStream.SetLength(0));

                            // Invalid arguments
                            var nonWritableStream = new MemoryStream(new byte[1], false);
                            var disposedStream = new MemoryStream();
                            disposedStream.Dispose();
                            Assert.Throws<ArgumentNullException>(() => clientStream.CopyTo(null));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.CopyTo(Stream.Null, 0));
                            Assert.Throws<ArgumentNullException>(() => { clientStream.CopyToAsync(null, 100, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.CopyToAsync(Stream.Null, 0, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.CopyToAsync(Stream.Null, -1, default); });
                            Assert.Throws<NotSupportedException>(() => { clientStream.CopyToAsync(nonWritableStream, 100, default); });
                            Assert.Throws<ObjectDisposedException>(() => { clientStream.CopyToAsync(disposedStream, 100, default); });
                            Assert.Throws<ArgumentNullException>(() => clientStream.Read(null, 0, 100));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.Read(new byte[1], -1, 1));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.Read(new byte[1], 2, 1));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.Read(new byte[1], 0, -1));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.Read(new byte[1], 0, 2));
                            Assert.Throws<ArgumentNullException>(() => clientStream.BeginRead(null, 0, 100, null, null));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.BeginRead(new byte[1], -1, 1, null, null));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.BeginRead(new byte[1], 2, 1, null, null));
                            Assert.Throws<ArgumentOutOfRangeException>(() => clientStream.BeginRead(new byte[1], 0, -1, null, null));
                            Assert.ThrowsAny<ArgumentException>(() => clientStream.BeginRead(new byte[1], 0, 2, null, null));
                            Assert.Throws<ArgumentNullException>(() => clientStream.EndRead(null));
                            Assert.Throws<ArgumentNullException>(() => { clientStream.ReadAsync(null, 0, 100, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.ReadAsync(new byte[1], -1, 1, default); });
                            Assert.ThrowsAny<ArgumentException>(() => { clientStream.ReadAsync(new byte[1], 2, 1, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { clientStream.ReadAsync(new byte[1], 0, -1, default); });
                            Assert.ThrowsAny<ArgumentException>(() => { clientStream.ReadAsync(new byte[1], 0, 2, default); });

                            // Validate writing APIs on clientStream

                            clientStream.WriteByte((byte)'!');
                            clientStream.Write(new byte[] { (byte)'\r', (byte)'\n' }, 0, 2);
                            Assert.Equal("!", await connection.Reader.ReadLineAsync());

                            clientStream.Write(new Span<byte>(new byte[] { (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'\r', (byte)'\n' }));
                            Assert.Equal("hello", await connection.Reader.ReadLineAsync());

                            await clientStream.WriteAsync(new byte[] { (byte)'w', (byte)'o', (byte)'r', (byte)'l', (byte)'d', (byte)'\r', (byte)'\n' }, 0, 7);
                            Assert.Equal("world", await connection.Reader.ReadLineAsync());

                            await clientStream.WriteAsync(new Memory<byte>(new byte[] { (byte)'a', (byte)'n', (byte)'d', (byte)'\r', (byte)'\n' }, 0, 5));
                            Assert.Equal("and", await connection.Reader.ReadLineAsync());

                            await Task.Factory.FromAsync(clientStream.BeginWrite, clientStream.EndWrite, new byte[] { (byte)'b', (byte)'e', (byte)'y', (byte)'o', (byte)'n', (byte)'d', (byte)'\r', (byte)'\n' }, 0, 8, null);
                            Assert.Equal("beyond", await connection.Reader.ReadLineAsync());

                            clientStream.Flush();
                            await clientStream.FlushAsync();

                            // Validate reading APIs on clientStream
                            await connection.Stream.WriteAsync(Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"));
                            var buffer = new byte[1];

                            Assert.Equal('a', clientStream.ReadByte());

                            Assert.Equal(1, clientStream.Read(buffer, 0, 1));
                            Assert.Equal((byte)'b', buffer[0]);

                            Assert.Equal(1, clientStream.Read(new Span<byte>(buffer, 0, 1)));
                            Assert.Equal((byte)'c', buffer[0]);

                            Assert.Equal(1, await clientStream.ReadAsync(buffer, 0, 1));
                            Assert.Equal((byte)'d', buffer[0]);

                            Assert.Equal(1, await clientStream.ReadAsync(new Memory<byte>(buffer, 0, 1)));
                            Assert.Equal((byte)'e', buffer[0]);

                            Assert.Equal(1, await Task.Factory.FromAsync(clientStream.BeginRead, clientStream.EndRead, buffer, 0, 1, null));
                            Assert.Equal((byte)'f', buffer[0]);

                            var ms = new MemoryStream();
                            Task copyTask = clientStream.CopyToAsync(ms);

                            string bigString = string.Concat(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz", 1000));
                            Task lotsOfDataSent = connection.Socket.SendAsync(Encoding.ASCII.GetBytes(bigString), SocketFlags.None);
                            connection.Socket.Shutdown(SocketShutdown.Send);
                            await copyTask;
                            await lotsOfDataSent;
                            Assert.Equal("ghijklmnopqrstuvwxyz" + bigString, Encoding.ASCII.GetString(ms.ToArray()));
                        }
                    });
                }
            });
        }
    }

    public sealed class SocketsHttpHandler_Connect_Test : HttpClientTestBase
    {
        protected override bool UseSocketsHttpHandler => true;

        [Fact]
        public async Task ConnectMethod_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("CONNECT"), url);
                    request.Headers.Add("Host", String.Format("{0}:{1}", request.RequestUri.IdnHost, request.RequestUri.Port));

                    // We need to use ResponseHeadersRead here, otherwise we will hang trying to buffer the response body.
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request,  HttpCompletionOption.ResponseHeadersRead);

                    await server.AcceptConnectionAsync(async connection =>
                    {
                        // Verify that Host header exist and has same value and URI authority.
                        List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                        string authority = lines[0].Split()[1];
                        string hostHeaderValue = null;
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("Host:",StringComparison.InvariantCultureIgnoreCase))
                            {
                                hostHeaderValue = line.Split()[1].Trim();
                                break;
                            }
                        }
                        Assert.Equal(authority, hostHeaderValue);

                        Task serverTask = connection.SendResponseAsync(HttpStatusCode.OK);
                        await TestHelper.WhenAllCompletedOrAnyFailed(responseTask, serverTask).ConfigureAwait(false);;

                        using (Stream clientStream = await (await responseTask).Content.ReadAsStreamAsync())
                        {
                            Assert.True(clientStream.CanWrite);
                            Assert.True(clientStream.CanRead);
                            Assert.False(clientStream.CanSeek);

                            TextReader clientReader = new StreamReader(clientStream);
                            TextWriter clientWriter = new StreamWriter(clientStream) { AutoFlush = true };
                            TextReader serverReader = connection.Reader;
                            TextWriter serverWriter = connection.Writer;

                            const string helloServer = "hello server";
                            const string helloClient = "hello client";
                            const string goodbyeServer = "goodbye server";
                            const string goodbyeClient = "goodbye client";

                            clientWriter.WriteLine(helloServer);
                            Assert.Equal(helloServer, serverReader.ReadLine());
                            serverWriter.WriteLine(helloClient);
                            Assert.Equal(helloClient, clientReader.ReadLine());
                            clientWriter.WriteLine(goodbyeServer);
                            Assert.Equal(goodbyeServer, serverReader.ReadLine());
                            serverWriter.WriteLine(goodbyeClient);
                            Assert.Equal(goodbyeClient, clientReader.ReadLine());
                        }
                    });
                }
            });
        }

        [Fact]
        public async Task ConnectMethod_Fails()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("CONNECT"), url);
                    request.Headers.Add("Host", String.Format("{0}:{1}", request.RequestUri.IdnHost, request.RequestUri.Port));
                    // We need to use ResponseHeadersRead here, otherwise we will hang trying to buffer the response body.
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request,  HttpCompletionOption.ResponseHeadersRead);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Task<List<string>> serverTask = connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            $"HTTP/1.1 403 Forbidden\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 7\r\n\r\nerror\r\n");

                        await TestHelper.WhenAllCompletedOrAnyFailed(responseTask, serverTask);
                        HttpResponseMessage response = await responseTask;

                        Assert.True(response.StatusCode ==  HttpStatusCode.Forbidden);
                    });
                }
            });
        }

        [Fact]
        public async Task ConnectMethod_Proxy()
        {
            LoopbackServer.Options options = new LoopbackServer.Options { UseSsl = true };
            HttpClientHandler handler = CreateHttpClientHandler();

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var ep = (IPEndPoint)listener.Server.LocalEndPoint;
            Uri proxyUrl = new Uri($"http://{ep.Address}:{ep.Port}/");

            //Task proxy = runProxy(listener);
            Task proxt = LoopbackGetRequestHttpProxy.StartAsync(listener, false, false);

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                // Point handler at out loopback proxy
                handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, null);
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> responseTask = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        Task<List<string>> serverTask = connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            $"HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\nOK\r\n");

                        await TestHelper.WhenAllCompletedOrAnyFailed(responseTask, serverTask).ConfigureAwait(false);;
                        HttpResponseMessage response = await responseTask.ConfigureAwait(false);;

                        Assert.True(response.StatusCode ==  HttpStatusCode.OK);

                    });
                }
            }, options);
        }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ConnectionPooling_Test : HttpClientTestBase
    {
        protected override bool UseSocketsHttpHandler => true;

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
            {
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    // Make multiple requests iteratively.
                    for (int i = 0; i < 2; i++)
                    {
                        Task<string> request = client.GetStringAsync(uri);
                        await server.AcceptConnectionSendResponseAndCloseAsync();
                        await request;

                        if (i == 0)
                        {
                            await Task.Delay(2000); // give client time to see the closing before next connect
                        }
                    }
                });
            }
        }

        [Fact]
        public async Task ServerSendsConnectionClose_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            {
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    string responseBody =
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        "Content-Length: 0\r\n" +
                        "Connection: close\r\n" +
                        "\r\n";

                    // Make first request.
                    Task<string> request1 = client.GetStringAsync(uri);
                    await server.AcceptConnectionAsync(async connection1 =>
                    {
                        await connection1.ReadRequestHeaderAndSendCustomResponseAsync(responseBody);
                        await request1;

                        // Make second request and expect it to be served from a different connection.
                        Task<string> request2 = client.GetStringAsync(uri);
                        await server.AcceptConnectionAsync(async connection2 =>
                        {
                            await connection2.ReadRequestHeaderAndSendCustomResponseAsync(responseBody);
                            await request2;
                        });
                    });
                });
            }
        }

        [Theory]
        [InlineData("PooledConnectionLifetime")]
        [InlineData("PooledConnectionIdleTimeout")]
        public async Task SmallConnectionTimeout_SubsequentRequestUsesDifferentConnection(string timeoutPropertyName)
        {
            using (var handler = new SocketsHttpHandler())
            {
                switch (timeoutPropertyName)
                {
                    case "PooledConnectionLifetime": handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    case "PooledConnectionIdleTimeout": handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    default: throw new ArgumentOutOfRangeException(nameof(timeoutPropertyName));
                }

                using (HttpClient client = new HttpClient(handler))
                {
                    await LoopbackServer.CreateServerAsync(async (server, uri) =>
                    {
                        // Make first request.
                        Task<string> request1 = client.GetStringAsync(uri);
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                            await request1;

                            // Wait a small amount of time before making the second request, to give the first request time to timeout.
                            await Task.Delay(100);

                            // Make second request and expect it to be served from a different connection.
                            Task<string> request2 = client.GetStringAsync(uri);
                            await server.AcceptConnectionAsync(async connection2 =>
                            {
                                await connection2.ReadRequestHeaderAndSendResponseAsync();
                                await request2;
                            });
                        });
                    });
                }
            }
        }
    }

    public sealed class SocketsHttpHandler_PublicAPIBehavior_Test
    {
        private static async Task IssueRequestAsync(HttpMessageHandler handler)
        {
            using (var c = new HttpMessageInvoker(handler, disposeHandler: false))
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    c.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("/shouldquicklyfail", UriKind.Relative)), default));
        }

        [Fact]
        public void AllowAutoRedirect_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.True(handler.AllowAutoRedirect);

                handler.AllowAutoRedirect = true;
                Assert.True(handler.AllowAutoRedirect);

                handler.AllowAutoRedirect = false;
                Assert.False(handler.AllowAutoRedirect);
            }
        }

        [Fact]
        public void AutomaticDecompression_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);

                handler.AutomaticDecompression = DecompressionMethods.GZip;
                Assert.Equal(DecompressionMethods.GZip, handler.AutomaticDecompression);

                handler.AutomaticDecompression = DecompressionMethods.Deflate;
                Assert.Equal(DecompressionMethods.Deflate, handler.AutomaticDecompression);

                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                Assert.Equal(DecompressionMethods.GZip | DecompressionMethods.Deflate, handler.AutomaticDecompression);
            }
        }

        [Fact]
        public void CookieContainer_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                CookieContainer container = handler.CookieContainer;
                Assert.Same(container, handler.CookieContainer);

                var newContainer = new CookieContainer();
                handler.CookieContainer = newContainer;
                Assert.Same(newContainer, handler.CookieContainer);
            }
        }

        [Fact]
        public void Credentials_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.Credentials);

                var newCredentials = new NetworkCredential("username", "password");
                handler.Credentials = newCredentials;
                Assert.Same(newCredentials, handler.Credentials);
            }
        }

        [Fact]
        public void DefaultProxyCredentials_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.DefaultProxyCredentials);

                var newCredentials = new NetworkCredential("username", "password");
                handler.DefaultProxyCredentials = newCredentials;
                Assert.Same(newCredentials, handler.DefaultProxyCredentials);
            }
        }

        [Fact]
        public void MaxAutomaticRedirections_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(50, handler.MaxAutomaticRedirections);

                handler.MaxAutomaticRedirections = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxAutomaticRedirections);

                handler.MaxAutomaticRedirections = 1;
                Assert.Equal(1, handler.MaxAutomaticRedirections);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxAutomaticRedirections = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxAutomaticRedirections = -1);
            }
        }

        [Fact]
        public void MaxConnectionsPerServer_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);

                handler.MaxConnectionsPerServer = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);

                handler.MaxConnectionsPerServer = 1;
                Assert.Equal(1, handler.MaxConnectionsPerServer);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxConnectionsPerServer = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxConnectionsPerServer = -1);
            }
        }

        [Fact]
        public void MaxResponseHeadersLength_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(64, handler.MaxResponseHeadersLength);

                handler.MaxResponseHeadersLength = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxResponseHeadersLength);

                handler.MaxResponseHeadersLength = 1;
                Assert.Equal(1, handler.MaxResponseHeadersLength);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseHeadersLength = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseHeadersLength = -1);
            }
        }

        [Fact]
        public void PreAuthenticate_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.False(handler.PreAuthenticate);

                handler.PreAuthenticate = false;
                Assert.False(handler.PreAuthenticate);

                handler.PreAuthenticate = true;
                Assert.True(handler.PreAuthenticate);
            }
        }

        [Fact]
        public void PooledConnectionIdleTimeout_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromMinutes(2), handler.PooledConnectionIdleTimeout);

                handler.PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan;
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionIdleTimeout);

                handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(0);
                Assert.Equal(TimeSpan.FromSeconds(0), handler.PooledConnectionIdleTimeout);

                handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(1);
                Assert.Equal(TimeSpan.FromSeconds(1), handler.PooledConnectionIdleTimeout);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(-2));
            }
        }

        [Fact]
        public void PooledConnectionLifetime_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);

                handler.PooledConnectionLifetime = Timeout.InfiniteTimeSpan;
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);

                handler.PooledConnectionLifetime = TimeSpan.FromSeconds(0);
                Assert.Equal(TimeSpan.FromSeconds(0), handler.PooledConnectionLifetime);

                handler.PooledConnectionLifetime = TimeSpan.FromSeconds(1);
                Assert.Equal(TimeSpan.FromSeconds(1), handler.PooledConnectionLifetime);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(-2));
            }
        }

        [Fact]
        public void Properties_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                IDictionary<string, object> props = handler.Properties;
                Assert.NotNull(props);
                Assert.Empty(props);

                props.Add("hello", "world");
                Assert.Equal(1, props.Count);
                Assert.Equal("world", props["hello"]);
            }
        }

        [Fact]
        public void Proxy_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Null(handler.Proxy);

                var proxy = new WebProxy();
                handler.Proxy = proxy;
                Assert.Same(proxy, handler.Proxy);
            }
        }

        [Fact]
        public void SslOptions_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                SslClientAuthenticationOptions options = handler.SslOptions;
                Assert.NotNull(options);

                Assert.True(options.AllowRenegotiation);
                Assert.Null(options.ApplicationProtocols);
                Assert.Equal(X509RevocationMode.NoCheck, options.CertificateRevocationCheckMode);
                Assert.Null(options.ClientCertificates);
                Assert.Equal(SslProtocols.None, options.EnabledSslProtocols);
                Assert.Equal(EncryptionPolicy.RequireEncryption, options.EncryptionPolicy);
                Assert.Null(options.LocalCertificateSelectionCallback);
                Assert.Null(options.RemoteCertificateValidationCallback);
                Assert.Null(options.TargetHost);

                Assert.Same(options, handler.SslOptions);

                var newOptions = new SslClientAuthenticationOptions();
                handler.SslOptions = newOptions;
                Assert.Same(newOptions, handler.SslOptions);
            }
        }

        [Fact]
        public void UseCookies_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.True(handler.UseCookies);

                handler.UseCookies = true;
                Assert.True(handler.UseCookies);

                handler.UseCookies = false;
                Assert.False(handler.UseCookies);
            }
        }

        [Fact]
        public void UseProxy_GetSet_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.True(handler.UseProxy);

                handler.UseProxy = false;
                Assert.False(handler.UseProxy);

                handler.UseProxy = true;
                Assert.True(handler.UseProxy);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AfterDisposeSendAsync_GettersUsable_SettersThrow(bool dispose)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Type expectedExceptionType;
                if (dispose)
                {
                    handler.Dispose();
                    expectedExceptionType = typeof(ObjectDisposedException);
                }
                else
                {
                    await IssueRequestAsync(handler);
                    expectedExceptionType = typeof(InvalidOperationException);
                }

                Assert.True(handler.AllowAutoRedirect);
                Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
                Assert.NotNull(handler.CookieContainer);
                Assert.Null(handler.Credentials);
                Assert.Null(handler.DefaultProxyCredentials);
                Assert.Equal(50, handler.MaxAutomaticRedirections);
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);
                Assert.Equal(64, handler.MaxResponseHeadersLength);
                Assert.False(handler.PreAuthenticate);
                Assert.Equal(TimeSpan.FromMinutes(2), handler.PooledConnectionIdleTimeout);
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.PooledConnectionLifetime);
                Assert.NotNull(handler.Properties);
                Assert.Null(handler.Proxy);
                Assert.NotNull(handler.SslOptions);
                Assert.True(handler.UseCookies);
                Assert.True(handler.UseProxy);

                Assert.Throws(expectedExceptionType, () => handler.AllowAutoRedirect = false);
                Assert.Throws(expectedExceptionType, () => handler.AutomaticDecompression = DecompressionMethods.GZip);
                Assert.Throws(expectedExceptionType, () => handler.CookieContainer = new CookieContainer());
                Assert.Throws(expectedExceptionType, () => handler.Credentials = new NetworkCredential("anotheruser", "anotherpassword"));
                Assert.Throws(expectedExceptionType, () => handler.DefaultProxyCredentials = new NetworkCredential("anotheruser", "anotherpassword"));
                Assert.Throws(expectedExceptionType, () => handler.MaxAutomaticRedirections = 2);
                Assert.Throws(expectedExceptionType, () => handler.MaxConnectionsPerServer = 2);
                Assert.Throws(expectedExceptionType, () => handler.MaxResponseHeadersLength = 2);
                Assert.Throws(expectedExceptionType, () => handler.PreAuthenticate = false);
                Assert.Throws(expectedExceptionType, () => handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(2));
                Assert.Throws(expectedExceptionType, () => handler.PooledConnectionLifetime = TimeSpan.FromSeconds(2));
                Assert.Throws(expectedExceptionType, () => handler.Proxy = new WebProxy());
                Assert.Throws(expectedExceptionType, () => handler.SslOptions = new SslClientAuthenticationOptions());
                Assert.Throws(expectedExceptionType, () => handler.UseCookies = false);
                Assert.Throws(expectedExceptionType, () => handler.UseProxy = false);
            }
        }
    }

    public sealed class SocketsHttpHandler_ExternalConfiguration_Test : HttpClientTestBase
    {
        private const string EnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER";
        private const string AppContextSettingName = "System.Net.Http.UseSocketsHttpHandler";

        private static bool UseSocketsHttpHandlerEnvironmentVariableIsNotSet =>
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableSettingName));

        [ConditionalTheory(nameof(UseSocketsHttpHandlerEnvironmentVariableIsNotSet))]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("tRuE", true)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("helloworld", false)]
        [InlineData("", false)]
        public void HttpClientHandler_SettingEnvironmentVariableChangesDefault(string envVarValue, bool expectedUseSocketsHandler)
        {
            RemoteInvoke((innerEnvVarValue, innerExpectedUseSocketsHandler) =>
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableSettingName, innerEnvVarValue);
                using (var handler = new HttpClientHandler())
                {
                    Assert.Equal(bool.Parse(innerExpectedUseSocketsHandler), IsSocketsHttpHandler(handler));
                }
                return SuccessExitCode;
            }, envVarValue, expectedUseSocketsHandler.ToString()).Dispose();
        }

        [Fact]
        public void HttpClientHandler_SettingAppContextChangesDefault()
        {
            RemoteInvoke(() =>
            {
                AppContext.SetSwitch(AppContextSettingName, isEnabled: true);
                using (var handler = new HttpClientHandler())
                {
                    Assert.True(IsSocketsHttpHandler(handler));
                }

                AppContext.SetSwitch(AppContextSettingName, isEnabled: false);
                using (var handler = new HttpClientHandler())
                {
                    Assert.False(IsSocketsHttpHandler(handler));
                }

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void HttpClientHandler_AppContextOverridesEnvironmentVariable()
        {
            RemoteInvoke(() =>
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableSettingName, "true");
                using (var handler = new HttpClientHandler())
                {
                    Assert.True(IsSocketsHttpHandler(handler));
                }

                AppContext.SetSwitch(AppContextSettingName, isEnabled: false);
                using (var handler = new HttpClientHandler())
                {
                    Assert.False(IsSocketsHttpHandler(handler));
                }

                AppContext.SetSwitch(AppContextSettingName, isEnabled: true);
                Environment.SetEnvironmentVariable(EnvironmentVariableSettingName, null);
                using (var handler = new HttpClientHandler())
                {
                    Assert.True(IsSocketsHttpHandler(handler));
                }

                return SuccessExitCode;
            }).Dispose();
        }
    }
}
