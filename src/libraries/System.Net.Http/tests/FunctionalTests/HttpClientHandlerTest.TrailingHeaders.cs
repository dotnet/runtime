// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_TrailingHeaders_Test : HttpClientHandlerTestBase
    {
        private static byte[] s_dataBytes = Encoding.ASCII.GetBytes("data");
        private static IList<HttpHeaderData> s_trailingHeaders = new HttpHeaderData[] {
            new HttpHeaderData("MyCoolTrailerHeader", "amazingtrailer"),
            new HttpHeaderData("EmptyHeader", ""),
            new HttpHeaderData("Hello", "World") };

        private static Frame MakeDataFrame(int streamId, byte[] data, bool endStream = false) =>
            new DataFrame(data, (endStream ? FrameFlags.EndStream : FrameFlags.None), 0, streamId);

        public HttpClientHandlerTest_TrailingHeaders_Test (ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsyncDefaultCompletionOption_TrailingHeaders_Available(bool includeTrailerHeader)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
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
                using (var client = new HttpClient(handler))
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

                        Stream stream = await response.Content.ReadAsStreamAsync();
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
                        while (stream.Read(data, 0, data.Length) != 0);

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
        [InlineData("Max-Forwards","2")]
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
                using (var client = new HttpClient(handler))
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
                using (var client = new HttpClient(handler))
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_NoTrailingHeaders_EmptyCollection()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(CreateHttpClientHandler(useSocketsHttpHandler: true, useHttp2LoopbackServer: true)))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                // Response header.
                await server.SendDefaultResponseHeadersAsync(streamId);

                // Response data.
                await server.WriteFrameAsync(MakeDataFrame(streamId, s_dataBytes, endStream: true));

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
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(CreateHttpClientHandler(useSocketsHttpHandler: true, useHttp2LoopbackServer: true)))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                // Response header.
                await server.SendDefaultResponseHeadersAsync(streamId);

                // Response data, missing Trailers.
                await server.WriteFrameAsync(MakeDataFrame(streamId, s_dataBytes));

                // Additional trailing header frame.
                await server.SendResponseHeadersAsync(streamId, isTrailingHeader:true, headers: s_trailingHeaders, endStream : true);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(s_trailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_TrailerHeaders_TrailingPseudoHeadersThrow()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(CreateHttpClientHandler(useSocketsHttpHandler: true, useHttp2LoopbackServer: true)))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                // Response header.
                await server.SendDefaultResponseHeadersAsync(streamId);
                await server.WriteFrameAsync(MakeDataFrame(streamId, s_dataBytes));
                // Additional trailing header frame with pseudo-headers again..
                await server.SendResponseHeadersAsync(streamId, isTrailingHeader:false, headers: s_trailingHeaders, endStream : true);

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsyncResponseHeadersReadOption_TrailingHeaders_Available()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(CreateHttpClientHandler(useSocketsHttpHandler: true, useHttp2LoopbackServer: true)))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                // Response header.
                await server.SendDefaultResponseHeadersAsync(streamId);

                // Response data, missing Trailers.
                await server.WriteFrameAsync(MakeDataFrame(streamId, s_dataBytes));

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Pending read on the response content.
                Assert.Empty(response.TrailingHeaders);

                Stream stream = await response.Content.ReadAsStreamAsync();
                Byte[] data = new Byte[100];
                await stream.ReadAsync(data, 0, data.Length);

                // Intermediate test - haven't reached stream EOF yet.
                Assert.Empty(response.TrailingHeaders);

                // Finish data stream and write out trailing headers.
                await server.WriteFrameAsync(MakeDataFrame(streamId, s_dataBytes));
                await server.SendResponseHeadersAsync(streamId, endStream : true, isTrailingHeader:true, headers: s_trailingHeaders);

                // Read data until EOF is reached
                while (stream.Read(data, 0, data.Length) != 0);

                Assert.Equal(s_trailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
        public async Task Http2GetAsync_TrailerHeaders_TrailingHeaderNoBody()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(CreateHttpClientHandler(useSocketsHttpHandler: true, useHttp2LoopbackServer: true)))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                // Response header.
                await server.SendDefaultResponseHeadersAsync(streamId);
                await server.SendResponseHeadersAsync(streamId, endStream : true, isTrailingHeader:true, headers: s_trailingHeaders);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(s_trailingHeaders.Count, response.TrailingHeaders.Count());
                Assert.Contains("amazingtrailer", response.TrailingHeaders.GetValues("MyCoolTrailerHeader"));
                Assert.Contains("World", response.TrailingHeaders.GetValues("Hello"));
            }
        }
    }
}
