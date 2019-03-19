// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_TrailingHeaders_Test : HttpClientHandlerTestBase
    {
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
    }
}
