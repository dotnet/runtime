﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Linq;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class Http1CloseResponseStreamZeroByteReadTest : Http1ResponseStreamZeroByteReadTestBase
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\n";

        protected override async Task WriteAsync(Stream stream, byte[] data) => await stream.WriteAsync(data);
    }

    public sealed class Http1RawResponseStreamZeroByteReadTest : Http1ResponseStreamZeroByteReadTestBase
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 101 Switching Protocols\r\n\r\n";

        protected override async Task WriteAsync(Stream stream, byte[] data) => await stream.WriteAsync(data);
    }

    public sealed class Http1ContentLengthResponseStreamZeroByteReadTest : Http1ResponseStreamZeroByteReadTestBase
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\n";

        protected override async Task WriteAsync(Stream stream, byte[] data) => await stream.WriteAsync(data);
    }

    public sealed class Http1SingleChunkResponseStreamZeroByteReadTest : Http1ResponseStreamZeroByteReadTestBase
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n";

        protected override async Task WriteAsync(Stream stream, byte[] data)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"{data.Length:X}\r\n"));
            await stream.WriteAsync(data);
            await stream.WriteAsync("\r\n"u8.ToArray());
        }
    }

    public sealed class Http1MultiChunkResponseStreamZeroByteReadTest : Http1ResponseStreamZeroByteReadTestBase
    {
        protected override string GetResponseHeaders() => "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n";

        protected override async Task WriteAsync(Stream stream, byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                await stream.WriteAsync(Encoding.ASCII.GetBytes($"1\r\n"));
                await stream.WriteAsync(data.AsMemory(i, 1));
                await stream.WriteAsync("\r\n"u8.ToArray());
            }
        }
    }

    public abstract class Http1ResponseStreamZeroByteReadTestBase
    {
        protected abstract string GetResponseHeaders();

        protected abstract Task WriteAsync(Stream stream, byte[] data);

        public static IEnumerable<object[]> ZeroByteRead_IssuesZeroByteReadOnUnderlyingStream_MemberData() =>
            from readMode in Enum.GetValues<StreamConformanceTests.ReadWriteMode>()
                .Where(mode => mode != StreamConformanceTests.ReadWriteMode.SyncByte) // Can't test zero-byte reads with ReadByte
            from useSsl in new[] { true, false }
            select new object[] { readMode, useSsl };

        [Theory]
        [MemberData(nameof(ZeroByteRead_IssuesZeroByteReadOnUnderlyingStream_MemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "ConnectCallback is not supported on Browser")]
        public async Task ZeroByteRead_IssuesZeroByteReadOnUnderlyingStream(StreamConformanceTests.ReadWriteMode readMode, bool useSsl)
        {
            (Stream httpConnection, Stream server) = ConnectedStreams.CreateBidirectional(4096, int.MaxValue);
            try
            {
                var sawZeroByteRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                httpConnection = new ReadInterceptStream(httpConnection, read =>
                {
                    if (read == 0)
                    {
                        sawZeroByteRead.TrySetResult();
                    }
                });

                using var handler = TestHelper.CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.ConnectCallback = delegate { return ValueTask.FromResult(httpConnection); };

                using var client = new HttpClient(handler);

                Task<HttpResponseMessage> clientTask = client.GetAsync($"http{(useSsl ? "s" : "")}://doesntmatter", HttpCompletionOption.ResponseHeadersRead);

                if (useSsl)
                {
                    var sslStream = new SslStream(server, false, delegate { return true; });
                    server = sslStream;

                    using (X509Certificate2 cert = Test.Common.Configuration.Certificates.GetServerCertificate())
                    {
                        await ((SslStream)server).AuthenticateAsServerAsync(
                            cert,
                            clientCertificateRequired: true,
                            enabledSslProtocols: SslProtocols.Tls12,
                            checkCertificateRevocation: false).WaitAsync(TestHelper.PassingTestTimeout);
                    }
                }

                await ResponseConnectedStreamConformanceTests.ReadHeadersAsync(server).WaitAsync(TestHelper.PassingTestTimeout);
                await server.WriteAsync(Encoding.ASCII.GetBytes(GetResponseHeaders()));

                using HttpResponseMessage response = await clientTask.WaitAsync(TestHelper.PassingTestTimeout);
                using Stream clientStream = response.Content.ReadAsStream();

                if (!useSsl)
                {
                    // SslStream does zero byte reads under the covers
                    Assert.False(sawZeroByteRead.Task.IsCompleted);
                }

                Task<int> zeroByteReadTask = Task.Run(() => StreamConformanceTests.ReadAsync(readMode, clientStream, Array.Empty<byte>(), 0, 0, CancellationToken.None));
                Assert.False(zeroByteReadTask.IsCompleted);

                // The zero-byte read should block until data is actually available
                await sawZeroByteRead.Task.WaitAsync(TestHelper.PassingTestTimeout);
                Assert.False(zeroByteReadTask.IsCompleted);

                byte[] data = "Hello"u8.ToArray();
                await WriteAsync(server, data);
                await server.FlushAsync();

                Assert.Equal(0, await zeroByteReadTask.WaitAsync(TestHelper.PassingTestTimeout));

                // Now that data is available, a zero-byte read should complete synchronously
                zeroByteReadTask = StreamConformanceTests.ReadAsync(readMode, clientStream, Array.Empty<byte>(), 0, 0, CancellationToken.None);
                Assert.True(zeroByteReadTask.IsCompleted);
                Assert.Equal(0, await zeroByteReadTask);

                var readBuffer = new byte[10];
                int read = 0;
                while (read < data.Length)
                {
                    read += await StreamConformanceTests.ReadAsync(readMode, clientStream, readBuffer, read, readBuffer.Length - read, CancellationToken.None).WaitAsync(TestHelper.PassingTestTimeout);
                }

                Assert.Equal(data.Length, read);
                Assert.Equal(data, readBuffer.AsSpan(0, read).ToArray());
            }
            finally
            {
                httpConnection.Dispose();
                server.Dispose();
            }
        }
    }

    public sealed class Http1ResponseStreamZeroByteReadTest : ResponseStreamZeroByteReadTestBase
    {
        public Http1ResponseStreamZeroByteReadTest(ITestOutputHelper output) : base(output) { }

        protected override Version UseVersion => HttpVersion.Version11;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class Http2ResponseStreamZeroByteReadTest : ResponseStreamZeroByteReadTestBase
    {
        public Http2ResponseStreamZeroByteReadTest(ITestOutputHelper output) : base(output) { }

        protected override Version UseVersion => HttpVersion.Version20;
    }

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class Http3ResponseStreamZeroByteReadTest : ResponseStreamZeroByteReadTestBase
    {
        public Http3ResponseStreamZeroByteReadTest(ITestOutputHelper output) : base(output) { }

        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public abstract class ResponseStreamZeroByteReadTestBase : HttpClientHandlerTestBase
    {
        public ResponseStreamZeroByteReadTestBase(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroByteRead_BlocksUntilDataIsAvailable(bool async)
        {
            var zeroByteReadIssued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);

                using HttpClient client = CreateHttpClient();
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                using Stream responseStream = await response.Content.ReadAsStreamAsync();

                var responseBuffer = new byte[1];
                Assert.Equal(1, await ReadAsync(async, responseStream, responseBuffer));
                Assert.Equal(42, responseBuffer[0]);

                Task<int> zeroByteReadTask = ReadAsync(async, responseStream, Array.Empty<byte>());
                Assert.False(zeroByteReadTask.IsCompleted);

                zeroByteReadIssued.SetResult();
                Assert.Equal(0, await zeroByteReadTask);
                Assert.Equal(0, await ReadAsync(async, responseStream, Array.Empty<byte>()));

                Assert.Equal(1, await ReadAsync(async, responseStream, responseBuffer));
                Assert.Equal(1, responseBuffer[0]);

                Assert.Equal(0, await ReadAsync(async, responseStream, Array.Empty<byte>()));

                Assert.Equal(1, await ReadAsync(async, responseStream, responseBuffer));
                Assert.Equal(2, responseBuffer[0]);

                zeroByteReadTask = ReadAsync(async, responseStream, Array.Empty<byte>());
                Assert.False(zeroByteReadTask.IsCompleted);

                zeroByteReadIssued.SetResult();
                Assert.Equal(0, await zeroByteReadTask);
                Assert.Equal(0, await ReadAsync(async, responseStream, Array.Empty<byte>()));

                Assert.Equal(1, await ReadAsync(async, responseStream, responseBuffer));
                Assert.Equal(3, responseBuffer[0]);

                Assert.Equal(0, await ReadAsync(async, responseStream, responseBuffer));
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestDataAsync();

                    await connection.SendResponseAsync(headers: new[] { new HttpHeaderData("Content-Length", "4") }, isFinal: false);

                    await connection.SendResponseBodyAsync(new byte[] { 42 }, isFinal: false);

                    await zeroByteReadIssued.Task;
                    zeroByteReadIssued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    await connection.SendResponseBodyAsync(new byte[] { 1, 2 }, isFinal: false);

                    await zeroByteReadIssued.Task;

                    await connection.SendResponseBodyAsync(new byte[] { 3 }, isFinal: true);
                });
            });

            static Task<int> ReadAsync(bool async, Stream stream, byte[] buffer)
            {
                if (async)
                {
                    return stream.ReadAsync(buffer).AsTask();
                }
                else
                {
                    return Task.Run(() => stream.Read(buffer));
                }
            }
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class Http2ConnectionZeroByteReadTest : HttpClientHandlerTestBase
    {
        public Http2ConnectionZeroByteReadTest(ITestOutputHelper output) : base(output) { }

        protected override Version UseVersion => HttpVersion.Version20;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectionIssuesZeroByteReadsOnUnderlyingStream(bool useSsl)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();

                int zeroByteReads = 0;
                GetUnderlyingSocketsHttpHandler(handler).PlaintextStreamFilter = (context, _) =>
                {
                    return new ValueTask<Stream>(new ReadInterceptStream(context.PlaintextStream, read =>
                    {
                        if (read == 0)
                        {
                            zeroByteReads++;
                        }
                    }));
                };

                using HttpClient client = CreateHttpClient(handler);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                Assert.Equal("Foo", await client.GetStringAsync(uri));

                Assert.NotEqual(0, zeroByteReads);
            },
            async server =>
            {
                await server.HandleRequestAsync(content: "Foo");
            }, http2Options: new Http2Options { UseSsl = useSsl });
        }
    }

    file sealed class ReadInterceptStream : DelegatingStream
    {
        private readonly Action<int> _readCallback;

        public ReadInterceptStream(Stream innerStream, Action<int> readCallback)
            : base(innerStream)
        {
            _readCallback = readCallback;
        }

        public override int Read(Span<byte> buffer)
        {
            _readCallback(buffer.Length);
            return base.Read(buffer);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _readCallback(count);
            return base.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _readCallback(buffer.Length);
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
