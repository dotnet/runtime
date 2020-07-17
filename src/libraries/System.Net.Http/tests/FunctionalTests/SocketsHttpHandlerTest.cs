// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class SocketsHttpHandler_HttpClientHandler_Asynchrony_Test : HttpClientHandler_Asynchrony_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Asynchrony_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ExecutionContext_Suppressed_Success()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                uri => Task.Run(() =>
                {
                    using (ExecutionContext.SuppressFlow())
                    using (HttpClient client = CreateHttpClient())
                    {
                        client.GetStringAsync(uri).GetAwaiter().GetResult();
                    }
                }),
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync();
                });
        }

        [OuterLoop("Relies on finalization")]
        [Fact]
        public async Task ExecutionContext_HttpConnectionLifetimeDoesntKeepContextAlive()
        {
            var clientCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        (Task completedWhenFinalized, Task getRequest) = MakeHttpRequestWithTcsSetOnFinalizationInAsyncLocal(client, uri);
                        await getRequest;

                        for (int i = 0; i < 3; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        await completedWhenFinalized.TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds);
                    }
                }
                finally
                {
                    clientCompleted.SetResult();
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAndSendResponseAsync();
                    await clientCompleted.Task;
                });
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // avoid JIT extending lifetime of the finalizable object
        private static (Task completedOnFinalized, Task getRequest) MakeHttpRequestWithTcsSetOnFinalizationInAsyncLocal(HttpClient client, Uri uri)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Put something in ExecutionContext, start the HTTP request, then undo the EC change.
            var al = new AsyncLocal<object>() { Value = new SetOnFinalized() { _completedWhenFinalized = tcs } };
            Task t = client.GetStringAsync(uri);
            al.Value = null;

            // Return a task that will complete when the SetOnFinalized is finalized,
            // as well as a task to wait on for the get request; for the get request,
            // we return a continuation to avoid any test-altering issues related to
            // the state machine holding onto stuff.
            t = t.ContinueWith(p => p.GetAwaiter().GetResult());
            return (tcs.Task, t);
        }

        private sealed class SetOnFinalized
        {
            internal TaskCompletionSource _completedWhenFinalized;
            ~SetOnFinalized() => _completedWhenFinalized.SetResult();
        }
    }

    public sealed class SocketsHttpHandler_HttpProtocolTests : HttpProtocolTests
    {
        public SocketsHttpHandler_HttpProtocolTests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        public SocketsHttpHandler_HttpProtocolTests_Dribble(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_DiagnosticsTest : DiagnosticsTest
    {
        public SocketsHttpHandler_DiagnosticsTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClient_SelectedSites_Test : HttpClient_SelectedSites_Test
    {
        public SocketsHttpHandler_HttpClient_SelectedSites_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        public SocketsHttpHandler_HttpClientEKUTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Decompression_Tests : HttpClientHandler_Decompression_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Decompression_Tests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        public SocketsHttpHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public SocketsHttpHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        public SocketsHttpHandler_HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Finalization_Http11_Test : HttpClientHandler_Finalization_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Finalization_Http11_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Finalization_Http2_Test : HttpClientHandler_Finalization_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Finalization_Http2_Test(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        public SocketsHttpHandler_HttpClientHandler_MaxConnectionsPerServer_Test(ITestOutputHelper output) : base(output) { }

        [OuterLoop("Incurs a small delay")]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task SmallConnectionLifetimeWithMaxConnections_PendingRequestUsesDifferentConnection(int lifetimeMilliseconds)
        {
            using (var handler = new SocketsHttpHandler())
            {
                handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(lifetimeMilliseconds);
                handler.MaxConnectionsPerServer = 1;

                using (HttpClient client = CreateHttpClient(handler))
                {
                    await LoopbackServer.CreateServerAsync(async (server, uri) =>
                    {
                        Task<string> request1 = client.GetStringAsync(uri);
                        Task<string> request2 = client.GetStringAsync(uri);

                        await server.AcceptConnectionAsync(async connection =>
                        {
                            Task secondResponse = server.AcceptConnectionAsync(connection2 =>
                                connection2.ReadRequestHeaderAndSendCustomResponseAsync(LoopbackServer.GetConnectionCloseResponse()));

                            // Wait a small amount of time before sending the first response, so the connection lifetime will expire.
                            Debug.Assert(lifetimeMilliseconds < 100);
                            await Task.Delay(1000);

                            // Second request should not have completed yet, as we haven't completed the first yet.
                            Assert.False(request2.IsCompleted);
                            Assert.False(secondResponse.IsCompleted);

                            // Send the first response and wait for the first request to complete.
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                            await request1;

                            // Now the second request should complete.
                            await secondResponse.TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds);
                        });
                    });
                }
            }
        }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        public SocketsHttpHandler_HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ResponseDrain_Test : HttpClientHandler_ResponseDrain_Test
    {
        protected override void SetResponseDrainTimeout(HttpClientHandler handler, TimeSpan time)
        {
            SocketsHttpHandler s = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
            Assert.NotNull(s);
            s.ResponseDrainTimeout = time;
        }

        public SocketsHttpHandler_HttpClientHandler_ResponseDrain_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void MaxResponseDrainSize_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(1024 * 1024, handler.MaxResponseDrainSize);

                handler.MaxResponseDrainSize = 0;
                Assert.Equal(0, handler.MaxResponseDrainSize);

                handler.MaxResponseDrainSize = int.MaxValue;
                Assert.Equal(int.MaxValue, handler.MaxResponseDrainSize);
            }
        }

        [Fact]
        public void MaxResponseDrainSize_InvalidArgument_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(1024 * 1024, handler.MaxResponseDrainSize);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseDrainSize = -1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.MaxResponseDrainSize = int.MinValue);

                Assert.Equal(1024 * 1024, handler.MaxResponseDrainSize);
            }
        }

        [Fact]
        public void MaxResponseDrainSize_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.MaxResponseDrainSize = 1;
                client.GetAsync("http://" + Guid.NewGuid().ToString("N")); // ignoring failure
                Assert.Equal(1, handler.MaxResponseDrainSize);
                Assert.Throws<InvalidOperationException>(() => handler.MaxResponseDrainSize = 1);
            }
        }

        [Fact]
        public void ResponseDrainTimeout_Roundtrips()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(2), handler.ResponseDrainTimeout);

                handler.ResponseDrainTimeout = TimeSpan.Zero;
                Assert.Equal(TimeSpan.Zero, handler.ResponseDrainTimeout);

                handler.ResponseDrainTimeout = TimeSpan.FromTicks(int.MaxValue);
                Assert.Equal(TimeSpan.FromTicks(int.MaxValue), handler.ResponseDrainTimeout);
            }
        }

        [Fact]
        public void MaxResponseDraiTime_InvalidArgument_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(2), handler.ResponseDrainTimeout);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ResponseDrainTimeout = TimeSpan.FromSeconds(-1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ResponseDrainTimeout = TimeSpan.MaxValue);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => handler.ResponseDrainTimeout = TimeSpan.FromSeconds(int.MaxValue));

                Assert.Equal(TimeSpan.FromSeconds(2), handler.ResponseDrainTimeout);
            }
        }

        [Fact]
        public void ResponseDrainTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ResponseDrainTimeout = TimeSpan.FromSeconds(42);
                client.GetAsync("http://" + Guid.NewGuid().ToString("N")); // ignoring failure
                Assert.Equal(TimeSpan.FromSeconds(42), handler.ResponseDrainTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.ResponseDrainTimeout = TimeSpan.FromSeconds(42));
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(1024 * 1024 * 2, 9_500, 1024 * 1024 * 3, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(1024 * 1024 * 2, 9_500, 1024 * 1024 * 3, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(1024 * 1024 * 2, 9_500, 1024 * 1024 * 13, LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeBeforeReadingToEnd_DrainsRequestsUnderMaxDrainSizeAndReusesConnection(int totalSize, int readSize, int maxDrainSize, LoopbackServer.ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.MaxResponseDrainSize = maxDrainSize;
                    handler.ResponseDrainTimeout = Timeout.InfiniteTimeSpan;

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, totalSize, mode);

                        // Read part but not all of response
                        Stream responseStream = await response1.Content.ReadAsStreamAsync(TestAsync);
                        await ReadToByteCount(responseStream, readSize);

                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on the same connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, totalSize, mode);
                        Assert.Equal(totalSize, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', totalSize);
                    string response = LoopbackServer.GetContentModeResponse(mode, content);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        server.ListenSocket.Close(); // Shut down the listen socket so attempts at additional connections would fail on the client
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(response);
                    });
                });
        }

        [OuterLoop]
        [Theory]
        [InlineData(100_000, 0, LoopbackServer.ContentMode.ContentLength)]
        [InlineData(100_000, 0, LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(100_000, 0, LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DisposeLargerThanMaxDrainSize_KillsConnection(int totalSize, int maxDrainSize, LoopbackServer.ContentMode mode)
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.MaxResponseDrainSize = maxDrainSize;
                    handler.ResponseDrainTimeout = Timeout.InfiniteTimeSpan;

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, totalSize, mode);
                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on a new connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, totalSize, mode);
                        Assert.Equal(totalSize, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', totalSize);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                        try
                        {
                            await connection.Writer.WriteAsync(LoopbackServer.GetContentModeResponse(mode, content, connectionClose: false));
                        }
                        catch (Exception) { }     // Eat errors from client disconnect.

                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(LoopbackServer.GetContentModeResponse(mode, content, connectionClose: true));
                    });
                });
        }

        [OuterLoop]
        [Theory]
        [InlineData(LoopbackServer.ContentMode.ContentLength)]
        [InlineData(LoopbackServer.ContentMode.SingleChunk)]
        [InlineData(LoopbackServer.ContentMode.BytePerChunk)]
        public async Task GetAsyncWithMaxConnections_DrainTakesLongerThanTimeout_KillsConnection(LoopbackServer.ContentMode mode)
        {
            const int ContentLength = 10_000;

            await LoopbackServer.CreateClientAndServerAsync(
                async url =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.MaxResponseDrainSize = int.MaxValue;
                    handler.ResponseDrainTimeout = TimeSpan.FromMilliseconds(1);

                    // Set MaxConnectionsPerServer to 1.  This will ensure we will wait for the previous request to drain (or fail to)
                    handler.MaxConnectionsPerServer = 1;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        client.Timeout = Timeout.InfiniteTimeSpan;

                        HttpResponseMessage response1 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        ValidateResponseHeaders(response1, ContentLength, mode);
                        response1.Dispose();

                        // Issue another request.  We'll confirm that it comes on a new connection.
                        HttpResponseMessage response2 = await client.GetAsync(url);
                        ValidateResponseHeaders(response2, ContentLength, mode);
                        Assert.Equal(ContentLength, (await response2.Content.ReadAsStringAsync()).Length);
                    }
                },
                async server =>
                {
                    string content = new string('a', ContentLength);
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        string response = LoopbackServer.GetContentModeResponse(mode, content, connectionClose: false);
                        await connection.ReadRequestHeaderAsync();
                        try
                        {
                            // Write out only part of the response
                            await connection.Writer.WriteAsync(response.Substring(0, response.Length / 2));
                        }
                        catch (Exception) { }     // Eat errors from client disconnect.

                        response = LoopbackServer.GetContentModeResponse(mode, content, connectionClose: true);
                        await server.AcceptConnectionSendCustomResponseAndCloseAsync(response);
                    });
                });
        }
    }

    public sealed class SocketsHttpHandler_PostScenarioTest : PostScenarioTest
    {
        public SocketsHttpHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposeTargetStream_ThrowsObjectDisposedException(bool knownLength)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        Task t = client.PostAsync(uri, new DisposeStreamWhileCopyingContent(knownLength));
                        Assert.IsType<ObjectDisposedException>((await Assert.ThrowsAsync<HttpRequestException>(() => t)).InnerException);
                    }
                }
                finally
                {
                    tcs.SetResult();
                }
            }, server => tcs.Task);
        }

        private sealed class DisposeStreamWhileCopyingContent : HttpContent
        {
            private readonly bool _knownLength;

            public DisposeStreamWhileCopyingContent(bool knownLength) => _knownLength = knownLength;

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                await stream.WriteAsync(new byte[42], 0, 42);
                stream.Dispose();
            }

            protected override bool TryComputeLength(out long length)
            {
                if (_knownLength)
                {
                    length = 42;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }
            }
        }
    }

    public sealed class SocketsHttpHandler_ResponseStreamTest : ResponseStreamTest
    {
        public SocketsHttpHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        public SocketsHttpHandler_HttpClientHandler_SslProtocols_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Proxy_Test : HttpClientHandler_Proxy_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }
    }

    public abstract class SocketsHttpHandler_TrailingHeaders_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_TrailingHeaders_Test(ITestOutputHelper output) : base(output) { }

        protected static byte[] DataBytes = Encoding.ASCII.GetBytes("data");

        protected static readonly IList<HttpHeaderData> TrailingHeaders = new HttpHeaderData[] {
            new HttpHeaderData("MyCoolTrailerHeader", "amazingtrailer"),
            new HttpHeaderData("EmptyHeader", ""),
            new HttpHeaderData("Accept-Encoding", "identity,gzip"),
            new HttpHeaderData("Hello", "World") };

        protected static Frame MakeDataFrame(int streamId, byte[] data, bool endStream = false) =>
            new DataFrame(data, (endStream ? FrameFlags.EndStream : FrameFlags.None), 0, streamId);
    }

    public class SocketsHttpHandler_Http1_TrailingHeaders_Test : SocketsHttpHandler_TrailingHeaders_Test
    {
        public SocketsHttpHandler_Http1_TrailingHeaders_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsyncDefaultCompletionOption_TrailingHeaders_Available(bool includeTrailerHeader)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            (includeTrailerHeader ? "Trailer: MyCoolTrailerHeader, Hello\r\n" : "") +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "MyCoolTrailerHeader: amazingtrailer\r\n" +
                            "Accept-encoding: identity,gzip\r\n" +
                            "Hello: World\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains("chunked", response.Headers.GetValues("Transfer-Encoding"));

                        // Check the Trailer header.
                        if (includeTrailerHeader)
                        {
                            Assert.Contains("MyCoolTrailerHeader", response.Headers.GetValues("Trailer"));
                            Assert.Contains("Hello", response.Headers.GetValues("Trailer"));
                        }

                        Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                        Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
                        Assert.Contains("identity,gzip", response.TrailingHeaders.GetValues("Accept-encoding"));

                        string data = await response.Content.ReadAsStringAsync();
                        Assert.Contains("data", data);
                        // Trailers should not be part of the content data.
                        Assert.DoesNotContain("MyCoolTrailerHeader", data);
                        Assert.DoesNotContain("amazingtrailer", data);
                        Assert.DoesNotContain("Hello", data);
                        Assert.DoesNotContain("World", data);
                    }
                }
            });
        }

        [Fact]
        public async Task GetAsyncResponseHeadersReadOption_TrailingHeaders_Available()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            "Trailer: MyCoolTrailerHeader\r\n" +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "MyCoolTrailerHeader: amazingtrailer\r\n" +
                            "Hello: World\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains("chunked", response.Headers.GetValues("Transfer-Encoding"));
                        Assert.Contains("MyCoolTrailerHeader", response.Headers.GetValues("Trailer"));

                        // Pending read on the response content.
                        var trailingHeaders = response.TrailingHeaders;
                        Assert.Empty(trailingHeaders);

                        Stream stream = await response.Content.ReadAsStreamAsync(TestAsync);
                        Byte[] data = new Byte[100];
                        // Read some data, preferably whole body.
                        int readBytes = await stream.ReadAsync(data, 0, 4);

                        // Intermediate test - haven't reached stream EOF yet.
                        Assert.Empty(response.TrailingHeaders);
                        if (readBytes == 4)
                        {
                            // If we consumed whole content, check content.
                            Assert.Contains("data", System.Text.Encoding.Default.GetString(data));
                        }

                        // Read data until EOF is reached
                        while (stream.Read(data, 0, data.Length) != 0)
                            ;

                        Assert.Same(trailingHeaders, response.TrailingHeaders);
                        Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                        Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
                    }
                }
            });
        }

        [Theory]
        [InlineData("Age", "1")]
        [InlineData("Authorization", "Basic YWxhZGRpbjpvcGVuc2VzYW1l")]
        [InlineData("Cache-Control", "no-cache")]
        [InlineData("Content-Encoding", "gzip")]
        [InlineData("Content-Length", "22")]
        [InlineData("Content-type", "foo/bar")]
        [InlineData("Content-Range", "bytes 200-1000/67589")]
        [InlineData("Date", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("Expect", "100-continue")]
        [InlineData("Expires", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("Host", "foo")]
        [InlineData("If-Match", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("If-Modified-Since", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("If-None-Match", "*")]
        [InlineData("If-Range", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("If-Unmodified-Since", "Wed, 21 Oct 2015 07:28:00 GMT")]
        [InlineData("Location", "/index.html")]
        [InlineData("Max-Forwards", "2")]
        [InlineData("Pragma", "no-cache")]
        [InlineData("Range", "5/10")]
        [InlineData("Retry-After", "20")]
        [InlineData("Set-Cookie", "foo=bar")]
        [InlineData("TE", "boo")]
        [InlineData("Transfer-Encoding", "chunked")]
        [InlineData("Transfer-Encoding", "gzip")]
        [InlineData("Vary", "*")]
        [InlineData("Warning", "300 - \"Be Warned!\"")]
        public async Task GetAsync_ForbiddenTrailingHeaders_Ignores(string name, string value)
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                    Assert.False(response.TrailingHeaders.TryGetValues(name, out IEnumerable<string> values));
                    Assert.Contains("Loopback", response.TrailingHeaders.GetValues("Server"));
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync(
                "HTTP/1.1 200 OK\r\n" +
                "Connection: close\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                $"Trailer: Set-Cookie, MyCoolTrailerHeader, {name}, Hello\r\n" +
                "\r\n" +
                "4\r\n" +
                "data\r\n" +
                "0\r\n" +
                "Set-Cookie: yummy\r\n" +
                "MyCoolTrailerHeader: amazingtrailer\r\n" +
                $"{name}: {value}\r\n" +
                "Server: Loopback\r\n" +
                $"{name}: {value}\r\n" +
                "\r\n"));
        }

        [Fact]
        public async Task GetAsync_NoTrailingHeaders_EmptyCollection()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            "Trailer: MyCoolTrailerHeader\r\n" +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains("chunked", response.Headers.GetValues("Transfer-Encoding"));

                        Assert.NotNull(response.TrailingHeaders);
                        Assert.Equal(0, response.TrailingHeaders.Count());
                        Assert.Same(response.TrailingHeaders, response.TrailingHeaders);
                    }
                }
            });
        }
    }

    // TODO: make generic to support HTTP/2 and HTTP/3.
    public sealed class SocketsHttpHandler_Http2_TrailingHeaders_Test : SocketsHttpHandler_TrailingHeaders_Test
    {
        public SocketsHttpHandler_Http2_TrailingHeaders_Test(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_NoTrailingHeaders_EmptyCollection()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes, endStream: true));

                // Server doesn't send trailing header frame.
                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(response.TrailingHeaders);
                Assert.Equal(0, response.TrailingHeaders.Count());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_MissingTrailer_TrailingHeadersAccepted()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data, missing Trailers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));

                // Additional trailing header frame.
                await connection.SendResponseHeadersAsync(streamId, isTrailingHeader:true, headers: TrailingHeaders, endStream : true);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(TrailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_TrailerHeaders_TrailingPseudoHeadersThrow()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));
                // Additional trailing header frame with pseudo-headers again..
                await connection.SendResponseHeadersAsync(streamId, isTrailingHeader:false, headers: TrailingHeaders, endStream : true);

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsyncResponseHeadersReadOption_TrailingHeaders_Available()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Response data, missing Trailers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Pending read on the response content.
                Assert.Empty(response.TrailingHeaders);

                Stream stream = await response.Content.ReadAsStreamAsync(TestAsync);
                Byte[] data = new Byte[100];
                await stream.ReadAsync(data, 0, data.Length);

                // Intermediate test - haven't reached stream EOF yet.
                Assert.Empty(response.TrailingHeaders);

                // Finish data stream and write out trailing headers.
                await connection.WriteFrameAsync(MakeDataFrame(streamId, DataBytes));
                await connection.SendResponseHeadersAsync(streamId, endStream : true, isTrailingHeader:true, headers: TrailingHeaders);

                // Read data until EOF is reached
                while (stream.Read(data, 0, data.Length) != 0);

                Assert.Equal(TrailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_TrailerHeaders_TrailingHeaderNoBody()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.SendResponseHeadersAsync(streamId, endStream : true, isTrailingHeader:true, headers: TrailingHeaders);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(TrailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_TrailingHeaders_NoData_EmptyResponseObserved()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                // Response header.
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // No data.

                // Response trailing headers
                await connection.SendResponseHeadersAsync(streamId, isTrailingHeader: true, headers: TrailingHeaders);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal<byte>(Array.Empty<byte>(), await response.Content.ReadAsByteArrayAsync());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }
    }

    public sealed class SocketsHttpHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest
    {
        public SocketsHttpHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public SocketsHttpHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandlerTest_AutoRedirect : HttpClientHandlerTest_AutoRedirect
    {
        public SocketsHttpHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public SocketsHttpHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        public SocketsHttpHandler_IdnaProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool SupportsIdna => true;
    }

    public sealed class SocketsHttpHandler_HttpRetryProtocolTests : HttpRetryProtocolTests
    {
        public SocketsHttpHandler_HttpRetryProtocolTests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandlerTest_Cookies : HttpClientHandlerTest_Cookies
    {
        public SocketsHttpHandlerTest_Cookies(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandlerTest_Cookies_Http11 : HttpClientHandlerTest_Cookies_Http11
    {
        public SocketsHttpHandlerTest_Cookies_Http11(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Http11_Cancellation_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ConnectTimeout_Default()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.ConnectTimeout);
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
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.ConnectTimeout = TimeSpan.FromMilliseconds(ms));
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
                handler.ConnectTimeout = TimeSpan.FromMilliseconds(ms);
                Assert.Equal(TimeSpan.FromMilliseconds(ms), handler.ConnectTimeout);
            }
        }

        [Fact]
        public void ConnectTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ConnectTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
                client.GetAsync("http://" + Guid.NewGuid().ToString("N")); // ignoring failure
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), handler.ConnectTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.ConnectTimeout = TimeSpan.FromMilliseconds(1));
            }
        }

        [Fact]
        public void Expect100ContinueTimeout_Default()
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Equal(TimeSpan.FromSeconds(1), handler.Expect100ContinueTimeout);
            }
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(int.MaxValue + 1L)]
        public void Expect100ContinueTimeout_InvalidValues(long ms)
        {
            using (var handler = new SocketsHttpHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(ms));
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
                handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(ms);
                Assert.Equal(TimeSpan.FromMilliseconds(ms), handler.Expect100ContinueTimeout);
            }
        }

        [Fact]
        public void Expect100ContinueTimeout_SetAfterUse_Throws()
        {
            using (var handler = new SocketsHttpHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
                client.GetAsync("http://" + Guid.NewGuid().ToString("N")); // ignoring failure
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), handler.Expect100ContinueTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.Expect100ContinueTimeout = TimeSpan.FromMilliseconds(1));
            }
        }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        public SocketsHttpHandler_HttpClientHandler_MaxResponseHeadersLength_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_Authentication_Test : HttpClientHandler_Authentication_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Authentication_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_ConnectionUpgrade_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_ConnectionUpgrade_Test(ITestOutputHelper output) : base(output) { }

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

                        using (Stream clientStream = await (await getResponseTask).Content.ReadAsStreamAsync(TestAsync))
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
                            Assert.Equal("!", await connection.ReadLineAsync());

                            clientStream.Write(new Span<byte>(new byte[] { (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'\r', (byte)'\n' }));
                            Assert.Equal("hello", await connection.ReadLineAsync());

                            await clientStream.WriteAsync(new byte[] { (byte)'w', (byte)'o', (byte)'r', (byte)'l', (byte)'d', (byte)'\r', (byte)'\n' }, 0, 7);
                            Assert.Equal("world", await connection.ReadLineAsync());

                            await clientStream.WriteAsync(new Memory<byte>(new byte[] { (byte)'a', (byte)'n', (byte)'d', (byte)'\r', (byte)'\n' }, 0, 5));
                            Assert.Equal("and", await connection.ReadLineAsync());

                            await Task.Factory.FromAsync(clientStream.BeginWrite, clientStream.EndWrite, new byte[] { (byte)'b', (byte)'e', (byte)'y', (byte)'o', (byte)'n', (byte)'d', (byte)'\r', (byte)'\n' }, 0, 8, null);
                            Assert.Equal("beyond", await connection.ReadLineAsync());

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

    public sealed class SocketsHttpHandler_Connect_Test : HttpClientHandler_Connect_Test
    {
        public SocketsHttpHandler_Connect_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class SocketsHttpHandler_HttpClientHandler_ConnectionPooling_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_HttpClientHandler_ConnectionPooling_Test(ITestOutputHelper output) : base(output) { }

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
        public async Task ServerSendsGarbageAfterInitialRequest_SubsequentRequestUsesDifferentConnection()
        {
            using (HttpClient client = CreateHttpClient())
            {
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    var releaseServer = new TaskCompletionSource();

                    // Make multiple requests iteratively.

                    Task serverTask1 = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.Writer.WriteAsync(LoopbackServer.GetHttpResponse(connectionClose: false) + "here is a bunch of garbage");
                        await releaseServer.Task; // keep connection alive on the server side
                    });
                    await client.GetStringAsync(uri);

                    Task serverTask2 = server.AcceptConnectionSendCustomResponseAndCloseAsync(LoopbackServer.GetHttpResponse(connectionClose: true));
                    await new[] { client.GetStringAsync(uri), serverTask2 }.WhenAllOrAnyFailed();

                    releaseServer.SetResult();
                    await serverTask1;
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

                using (HttpClient client = CreateHttpClient(handler))
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

        [Theory]
        [InlineData("PooledConnectionLifetime")]
        [InlineData("PooledConnectionIdleTimeout")]
        public async Task Http2_SmallConnectionTimeout_SubsequentRequestUsesDifferentConnection(string timeoutPropertyName)
        {
            await Http2LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler(HttpVersion.Version20);
                SocketsHttpHandler s = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                switch (timeoutPropertyName)
                {
                    case "PooledConnectionLifetime": s.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    case "PooledConnectionIdleTimeout": s.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1); break;
                    default: throw new ArgumentOutOfRangeException(nameof(timeoutPropertyName));
                }

                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestVersion = HttpVersion.Version20;
                    Task<string> request1 = client.GetStringAsync(url);

                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    int streamId = await connection.ReadRequestHeaderAsync();
                    await connection.SendDefaultResponseAsync(streamId);
                    await request1;

                    // Wait a small amount of time before making the second request, to give the first request time to timeout.
                    await Task.Delay(100);
                    // Grab reference to underlying socket and stream to make sure they are not disposed and closed.
                    (Socket socket, Stream stream) = connection.ResetNetwork();

                    // Make second request and expect it to be served from a different connection.
                    Task<string> request2 = client.GetStringAsync(url);
                    connection = await server.EstablishConnectionAsync();
                    streamId = await connection.ReadRequestHeaderAsync();
                    await connection.SendDefaultResponseAsync(streamId);
                    await request2;

                    // Close underlying socket from first connection.
                    socket.Close();
                }
            });
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void ConnectionsPooledThenDisposed_NoUnobservedTaskExceptions(bool secure)
        {
            RemoteExecutor.Invoke(async (secureString, useVersionString) =>
            {
                var releaseServer = new TaskCompletionSource();
                await LoopbackServer.CreateClientAndServerAsync(async uri =>
                {
                    using (var handler = new SocketsHttpHandler())
                    using (HttpClient client = CreateHttpClient(handler, useVersionString))
                    {
                        handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                        handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1);

                        var exceptions = new List<Exception>();
                        TaskScheduler.UnobservedTaskException += (s, e) => exceptions.Add(e.Exception);

                        await client.GetStringAsync(uri);
                        await Task.Delay(10); // any value >= the lifetime
                        Task ignored = client.GetStringAsync(uri); // force the pool to look for the previous connection and find it's too old
                        await Task.Delay(100); // give some time for the connection close to fail pending reads

                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        // Note that there are race conditions here such that we may not catch every failure,
                        // and thus could have some false negatives, but there won't be any false positives.
                        Assert.True(exceptions.Count == 0, string.Concat(exceptions));

                        releaseServer.SetResult();
                    }
                }, server => server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAndSendResponseAsync(content: "hello world");
                    await releaseServer.Task;
                }),
                new LoopbackServer.Options { UseSsl = bool.Parse(secureString) });
            }, secure.ToString(), UseVersion.ToString()).Dispose();
        }

        [OuterLoop]
        [Fact]
        public void HandlerDroppedWithoutDisposal_NotKeptAlive()
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            HandlerDroppedWithoutDisposal_NotKeptAliveCore(tcs);
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.True(tcs.Task.IsCompleted);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandlerDroppedWithoutDisposal_NotKeptAliveCore(TaskCompletionSource setOnFinalized)
        {
            // This relies on knowing that in order for the connection pool to operate, it needs
            // to maintain a reference to the supplied IWebProxy.  As such, we provide a proxy
            // that when finalized will set our event, so that we can determine the state associated
            // with a handler has gone away.
            IWebProxy p = new PassthroughProxyWithFinalizerCallback(() => setOnFinalized.TrySetResult());

            // Make a bunch of requests and drop the associated HttpClient instances after making them, without disposal.
            Task.WaitAll((from i in Enumerable.Range(0, 10)
                          select LoopbackServer.CreateClientAndServerAsync(
                              url => CreateHttpClient(new SocketsHttpHandler { Proxy = p }).GetStringAsync(url),
                              server => server.AcceptConnectionSendResponseAndCloseAsync())).ToArray());
        }

        private sealed class PassthroughProxyWithFinalizerCallback : IWebProxy
        {
            private readonly Action _callback;

            public PassthroughProxyWithFinalizerCallback(Action callback) => _callback = callback;
            ~PassthroughProxyWithFinalizerCallback() => _callback();

            public ICredentials Credentials { get; set; }
            public Uri GetProxy(Uri destination) => destination;
            public bool IsBypassed(Uri host) => true;
        }

        [Fact]
        public async Task ProxyAuth_SameConnection_Succeeds()
        {
            Task serverTask = LoopbackServer.CreateServerAsync(async (proxyServer, proxyUrl) =>
            {
                string responseBody =
                        "HTTP/1.1 407 Proxy Auth Required\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        "Proxy-Authenticate: Basic\r\n" +
                        "Content-Length: 0\r\n" +
                        "\r\n";

                using  (var handler = new HttpClientHandler())
                {
                    handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, new NetworkCredential("abc", "def"));

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        Task<string> request = client.GetStringAsync($"http://notarealserver.com/");

                        await proxyServer.AcceptConnectionAsync(async connection =>
                        {
                            // Get first request, no body for GET.
                            await connection.ReadRequestHeaderAndSendCustomResponseAsync(responseBody).ConfigureAwait(false);
                            // Client should send another request after being rejected with 407.
                            await connection.ReadRequestHeaderAndSendResponseAsync(content:"OK").ConfigureAwait(false);
                        });

                        string response = await request;
                        Assert.Equal("OK", response);
                    }
                }
            });
            await serverTask.TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds);
        }
    }

    public sealed class SocketsHttpHandler_PublicAPIBehavior_Test
    {
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
                    using (var c = new HttpMessageInvoker(handler, disposeHandler: false))
                        await Assert.ThrowsAnyAsync<Exception>(() =>
                            c.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("/shouldquicklyfail", UriKind.Relative)), default));
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

    public sealed class SocketsHttpHandlerTest_LocationHeader
    {
        private static readonly byte[] s_redirectResponseBefore = Encoding.ASCII.GetBytes(
            "HTTP/1.1 301 Moved Permanently\r\n" +
            "Connection: close\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Location: ");

        private static readonly byte[] s_redirectResponseAfter = Encoding.ASCII.GetBytes(
            "\r\n" +
            "Server: Loopback\r\n" +
            "\r\n" +
            "0\r\n\r\n");

        [Theory]
        // US-ASCII only
        [InlineData("http://a/", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/' })]
        [InlineData("http://a/asdasd", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', (byte)'a', (byte)'s', (byte)'d', (byte)'a', (byte)'s', (byte)'d' })]
        // 2, 3, 4 byte UTF-8 characters
        [InlineData("http://a/\u00A2", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC2, 0xA2 })]
        [InlineData("http://a/\u20AC", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xE2, 0x82, 0xAC })]
        [InlineData("http://a/\uD800\uDF48", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xF0, 0x90, 0x8D, 0x88 })]
        // 3 Polish letters
        [InlineData("http://a/\u0105\u015B\u0107", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC4, 0x85, 0xC5, 0x9B, 0xC4, 0x87 })]
        // Negative cases - should be interpreted as ISO-8859-1
        // Invalid utf-8 sequence (continuation without start)
        [InlineData("http://a/%C2%80", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0b10000000 })]
        // Invalid utf-8 sequence (not allowed character)
        [InlineData("http://a/\u00C3\u0028", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC3, 0x28 })]
        // Incomplete utf-8 sequence
        [InlineData("http://a/\u00C2", new byte[] { (byte)'h', (byte)'t', (byte)'t', (byte)'p', (byte)':', (byte)'/', (byte)'/', (byte)'a', (byte)'/', 0xC2 })]
        public async Task LocationHeader_DecodesUtf8_Success(string expected, byte[] location)
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.AllowAutoRedirect = false;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        Assert.Equal(expected, response.Headers.Location.ToString());
                    }
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync(PreperateResponseWithRedirect(location)));
        }

        private static byte[] PreperateResponseWithRedirect(byte[] location)
        {
            return s_redirectResponseBefore.Concat(location).Concat(s_redirectResponseAfter).ToArray();
        }
    }

    public sealed class SocketsHttpHandlerTest_Http2 : HttpClientHandlerTest_Http2
    {
        public SocketsHttpHandlerTest_Http2(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_ConnectionLimitNotReached_ConcurrentRequestsSuccessfullyHandled()
        {
            const int MaxHttp2ConnectionsPerServer = 3;
            const int MaxConcurrentStreams = 2;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler(MaxHttp2ConnectionsPerServer);
            using (HttpClient client = CreateHttpClient(handler))
            {
                server.AllowMultipleConnections = true;
                Task<HttpResponseMessage>[] sendTasks = new Task<HttpResponseMessage>[MaxHttp2ConnectionsPerServer * MaxConcurrentStreams];
                List<Http2LoopbackConnection> connections = new List<Http2LoopbackConnection>();
                for (int i = 0; i < sendTasks.Length; i++)
                {
                    sendTasks[i++] = client.GetAsync(server.Address);
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = MaxConcurrentStreams }).ConfigureAwait(false);
                    connections.Add(connection);
                    sendTasks[i] = client.GetAsync(server.Address);
                }

                Task[] respondTasks = new Task[sendTasks.Length];
                int respondTaskIndex = 0;
                foreach (Http2LoopbackConnection connection in connections)
                {
                    for (int i = 0; i < MaxConcurrentStreams; i++)
                    {
                        int streamId = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                        respondTasks[respondTaskIndex++] = connection.SendDefaultResponseAsync(streamId);
                    }
                }

                await Task.WhenAll(respondTasks).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds).ConfigureAwait(false);
                await Task.WhenAll(sendTasks).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds).ConfigureAwait(false);

                await VerifySendTasks(sendTasks).ConfigureAwait(false);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_ConnectionLimitReached_ConcurrentRequestsQueuedAndEvenlyDistributed()
        {
            const int MaxConcurrentStreams = 2;
            const int TotalRequestCount = 1000;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler(2);
            using (HttpClient client = CreateHttpClient(handler))
            {
                server.AllowMultipleConnections = true;
                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                Http2LoopbackConnection[] connections = new[] {
                    await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false),
                    await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false)
                };
                int warmUpRequestCount = sendTasks.Count;
                for (int i = 0; i < TotalRequestCount - warmUpRequestCount; i++)
                {
                    // This is an equvivalent of a "request burst" scenario because there are no connections handling requests at this point.
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                // If request queueing is enabled as expected, all the requests will be handled by just 2 connections.
                int[] handledRequests = new int[2];
                while (handledRequests[0] + handledRequests[1] < TotalRequestCount)
                {
                    for (int i = 0; i < connections.Length; i++)
                    {
                        try
                        {
                            int streamId = await connections[i].ReadRequestHeaderAsync().ConfigureAwait(false);
                            await connections[i].SendDefaultResponseAsync(streamId).ConfigureAwait(false);
                            handledRequests[i]++;
                        }
                        catch (OperationCanceledException)
                        {
                            continue;
                        }
                    }
                }

                await Task.WhenAll(sendTasks).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds).ConfigureAwait(false);

                Assert.Equal(handledRequests[0], TotalRequestCount / 2);
                Assert.Equal(handledRequests[1], TotalRequestCount / 2);
                Assert.Equal(handledRequests[0] + handledRequests[1], TotalRequestCount);

                await VerifySendTasks(sendTasks).ConfigureAwait(false);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_MaxConcurrentStreamsIncreasedAfterLimitIsReached_WaitingRequestsUnblocked()
        {
            const int MaxConcurrentStreams = 2;
            const int TotalRequestCount = 1000;
            CancellationTokenSource cts = new CancellationTokenSource();
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler(2);
            using (HttpClient client = CreateHttpClient(handler))
            {
                server.AllowMultipleConnections = true;
                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                Http2LoopbackConnection[] connections = new[] {
                    await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false),
                    await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false)
                };

                Task<List<int>>[] acceptTasks = new[] { AcceptRequests(connections[0]), AcceptRequests(connections[1]) };

                await Task.WhenAll(acceptTasks).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds).ConfigureAwait(false);

                List<int>[] acceptedStreams = new[] { acceptTasks[0].Result, acceptTasks[1].Result };
                int acceptedStreamCount = acceptedStreams.Sum(l => l.Count);
                Assert.Equal(MaxConcurrentStreams * 2, acceptedStreamCount);

                int warmUpRequestCount = sendTasks.Count;
                for (int i = 0; i < TotalRequestCount - warmUpRequestCount; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                uint sufficentStreamsOnConnection0 = TotalRequestCount - MaxConcurrentStreams;

                await connections[0].WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = sufficentStreamsOnConnection0 })).ConfigureAwait(false);
                await connections[0].ExpectSettingsAckAsync().ConfigureAwait(false);

                List<int> remainingStreams = await AcceptRequests(connections[0]).ConfigureAwait(false);

                Assert.Equal(TotalRequestCount, acceptedStreamCount + remainingStreams.Count);

                await SendResponses(connections[0], acceptedStreams[0].Concat(remainingStreams)).ConfigureAwait(false);
                await SendResponses(connections[1], acceptedStreams[1]).ConfigureAwait(false);

                await VerifySendTasks(sendTasks).ConfigureAwait(false);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_InfiniteRequestsCompletelyBlockOneConnection_AllRemaningRequestsHandledBySecondConnection()
        {
            const int MaxConcurrentStreams = 2;
            const int TotalRequestCount = 1000;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler(2);
            using (HttpClient client = CreateHttpClient(handler))
            {
                server.AllowMultipleConnections = true;
                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                Http2LoopbackConnection connection0 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);
                Http2LoopbackConnection connection1 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);
                int warmUpRequestCount = sendTasks.Count;
                for (int i = 0; i < TotalRequestCount - warmUpRequestCount; i++)
                {
                    // This is an equvivalent of a "request burst" scenario because there are no connections handling requests at this point.
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                // Block the first connection on infinite requests.
                List<int> blockedStreamIds = await AcceptRequests(connection0);

                int handledRequestCount = (await HandleAllPendingRequests(connection1, sendTasks.Count - blockedStreamIds.Count).ConfigureAwait(false)).Count;

                // First connection is blocked by 'MaxConcurrentStreams' requests.
                Assert.Equal(TotalRequestCount - blockedStreamIds.Count, handledRequestCount);

                //Complete inifinite requests.
                handledRequestCount += await SendResponses(connection0, blockedStreamIds);

                Assert.Equal(TotalRequestCount, handledRequestCount);

                await Task.WhenAll(sendTasks).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds).ConfigureAwait(false);

                await VerifySendTasks(sendTasks).ConfigureAwait(false);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MultipleConnectionsEnabled_OpenAndCloseMultipleConnections_Success()
        {
            const int MaxHttp2ConnectionsPerServer = 3;
            const int MaxConcurrentStreams = 2;
            const int TotalRequestCount = 100;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using SocketsHttpHandler handler = CreateHandler(MaxHttp2ConnectionsPerServer);
            using (HttpClient client = CreateHttpClient(handler))
            {
                server.AllowMultipleConnections = true;
                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                Http2LoopbackConnection connection0 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);
                Http2LoopbackConnection connection1 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);
                Http2LoopbackConnection connection2 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);
                for (int i = 0; i < MaxConcurrentStreams * MaxHttp2ConnectionsPerServer; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                var handledRequests0 = await HandleAllPendingRequests(connection0, sendTasks.Count).ConfigureAwait(false);
                var handledRequests1 = await HandleAllPendingRequests(connection1, sendTasks.Count).ConfigureAwait(false);
                var handledRequests2 = await HandleAllPendingRequests(connection2, sendTasks.Count).ConfigureAwait(false);
                int totalHandledRequests = handledRequests0.Count + handledRequests1.Count + handledRequests2.Count;

                Assert.InRange(handledRequests0.Count, 1, sendTasks.Count);
                Assert.InRange(handledRequests1.Count, 1, sendTasks.Count);
                Assert.InRange(handledRequests2.Count, 1, sendTasks.Count);

                await connection0.ShutdownIgnoringErrorsAsync(handledRequests0.LastStreamId).ConfigureAwait(false);
                await connection2.ShutdownIgnoringErrorsAsync(handledRequests2.LastStreamId).ConfigureAwait(false);

                int lastTaskCount = sendTasks.Count;
                //Fill remaining connection1's stream queue
                sendTasks.Add(client.GetAsync(server.Address));
                sendTasks.Add(client.GetAsync(server.Address));
                Http2LoopbackConnection connection3 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);
                Http2LoopbackConnection connection4 = await PrepareConnection(server, client, sendTasks, MaxConcurrentStreams).ConfigureAwait(false);

                int remainingRequestCount = TotalRequestCount - sendTasks.Count;
                for (int i = 0; i < remainingRequestCount; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                int[] lastHandledRequests = new int[3];
                int finalCount1 = 0;
                int finalCount3 = 0;
                int finalCount4 = 0;
                do
                {
                    lastHandledRequests[0] = (await HandleAllPendingRequests(connection1, sendTasks.Count).ConfigureAwait(false)).Count;
                    lastHandledRequests[1] = (await HandleAllPendingRequests(connection3, sendTasks.Count).ConfigureAwait(false)).Count;
                    lastHandledRequests[2] = (await HandleAllPendingRequests(connection4, sendTasks.Count).ConfigureAwait(false)).Count;
                    finalCount1 += lastHandledRequests[0];
                    finalCount3 += lastHandledRequests[1];
                    finalCount4 += lastHandledRequests[2];
                } while (lastHandledRequests.Any(c => c != 0));
                totalHandledRequests += finalCount1 + finalCount3 + finalCount4;

                Assert.InRange(finalCount1, 1, TotalRequestCount);
                Assert.InRange(finalCount3, 1, TotalRequestCount);
                Assert.InRange(finalCount4, 1, TotalRequestCount);
                Assert.Equal(TotalRequestCount, totalHandledRequests);

                await Task.WhenAll(sendTasks).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds).ConfigureAwait(false);

                await VerifySendTasks(sendTasks).ConfigureAwait(false);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public void Http2_SetInvalidValueToMaxHttp2ConnectionsPerServer_Throw()
        {
            using (SocketsHttpHandler handler = new SocketsHttpHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxHttp2ConnectionsPerServer = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxHttp2ConnectionsPerServer = -1);
            }
        }

        private async Task VerifySendTasks(IReadOnlyList<Task<HttpResponseMessage>> sendTasks)
        {
            foreach (Task<HttpResponseMessage> sendTask in sendTasks)
            {
                HttpResponseMessage response = await sendTask.ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private static SocketsHttpHandler CreateHandler(int MaxHttp2ConnectionsPerServer) => new SocketsHttpHandler
        {
            MaxHttp2ConnectionsPerServer = MaxHttp2ConnectionsPerServer,
            PooledConnectionIdleTimeout = TimeSpan.FromHours(1),
            PooledConnectionLifetime = TimeSpan.FromHours(1),
            SslOptions = { RemoteCertificateValidationCallback = delegate { return true; } }
        };

        private async Task<Http2LoopbackConnection> PrepareConnection(Http2LoopbackServer server, HttpClient client, List<Task<HttpResponseMessage>> sendTasks, uint maxConcurrentStreams)
        {
            sendTasks.Add(client.GetAsync(server.Address));
            sendTasks.Add(client.GetAsync(server.Address));
            Http2LoopbackConnection connection = await server.EstablishConnectionAsync(TimeSpan.FromSeconds(3), new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = maxConcurrentStreams }).ConfigureAwait(false);
            return connection;
        }

        private async Task<(int Count, int LastStreamId)> HandleAllPendingRequests(Http2LoopbackConnection connection, int totalRequestCount)
        {
            int streamId = -1;
            for (int i = 0; i < totalRequestCount; i++)
            {
                try
                {
                    // Exact number of requests sent over the given connection is unknown,
                    // so we keep reading headers and sending response while there are available requests.
                    streamId = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                    await connection.SendDefaultResponseAsync(streamId).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return (i, streamId);
                }
            }

            return (totalRequestCount, streamId);
        }

        private async Task<List<int>> AcceptRequests(Http2LoopbackConnection connection)
        {
            List<int> streamIds = new List<int>();
            while (true)
            {
                try
                {
                    streamIds.Add(await connection.ReadRequestHeaderAsync().ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                {
                    return streamIds;
                }
            }
        }

        private async Task<int> SendResponses(Http2LoopbackConnection connection, IEnumerable<int> streamIds)
        {
            int count = 0;
            foreach (int streamId in streamIds)
            {
                count++;
                await connection.SendDefaultResponseAsync(streamId).ConfigureAwait(false);
            }

            return count;
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_Cookies_Http2 : HttpClientHandlerTest_Cookies
    {
        public SocketsHttpHandlerTest_Cookies_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Http2 : HttpClientHandlerTest
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http11 : HttpClientHandlerTest_Headers
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http11(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http2 : HttpClientHandlerTest_Headers
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http2 : HttpClientHandler_Cancellation_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Finalization_Http3_Test : HttpClientHandler_Finalization_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Finalization_Http3_Test(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_Http3 : HttpClientHandlerTest_Http3
    {
        public SocketsHttpHandlerTest_Http3(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_Cookies_Http3 : HttpClientHandlerTest_Cookies
    {
        public SocketsHttpHandlerTest_Cookies_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Http3 : HttpClientHandlerTest
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http3 : HttpClientHandlerTest_Headers
    {
        public SocketsHttpHandlerTest_HttpClientHandlerTest_Headers_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http3 : HttpClientHandler_Cancellation_Test
    {
        public SocketsHttpHandler_HttpClientHandler_Cancellation_Test_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }

    [ConditionalClass(typeof(QuicConnection), nameof(QuicConnection.IsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientHandler_AltSvc_Test_Http3 : HttpClientHandler_AltSvc_Test
    {
        public SocketsHttpHandler_HttpClientHandler_AltSvc_Test_Http3(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
    }
}
