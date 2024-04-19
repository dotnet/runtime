// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpProtocolTests : HttpClientHandlerTestBase
    {
        protected virtual Stream GetStream(Stream s) => s;

        public HttpProtocolTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task GetAsync_RequestVersion10_Success()
        {
            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version10;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    var requestLines = await serverTask;
                    Assert.Equal($"GET {url.PathAndQuery} HTTP/1.0", requestLines[0]);
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Fact]
        public async Task GetAsync_RequestVersion11_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    var requestLines = await serverTask;
                    Assert.Equal($"GET {url.PathAndQuery} HTTP/1.1", requestLines[0]);
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9)]
        public async Task GetAsync_RequestVersion0X_ThrowsNotSupportedException(int minorVersion)
        {
            if (IsWinHttpHandler)
            {
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://nosuchhost.invalid");
                request.Version = new Version(0, minorVersion);

                Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);

                await Assert.ThrowsAsync<NotSupportedException>(() => getResponseTask);
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 6)]
        [InlineData(2, 0)]  // Note, this is plain HTTP (not HTTPS), so 2.0 is not supported and should degrade to 1.1
        [InlineData(2, 1)]
        [InlineData(2, 7)]
        [InlineData(3, 0)]
        [InlineData(4, 2)]
        public async Task GetAsync_UnknownRequestVersion_DegradesTo11(int majorVersion, int minorVersion)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && majorVersion >= 2)
            {
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(majorVersion, minorVersion);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);
                    var requestLines = await serverTask;
                    Assert.Equal($"GET {url.PathAndQuery} HTTP/1.1", requestLines[0]);
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task GetAsync_ResponseVersion10or11_Success(int responseMinorVersion)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);
                    Task<List<string>> serverTask =
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/1.{responseMinorVersion} 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n");

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(1, response.Version.Major);
                        Assert.Equal(responseMinorVersion, response.Version.Minor);
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData(2)]
        [InlineData(7)]
        public async Task GetAsync_ResponseUnknownVersion1X_Success(int responseMinorVersion)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);
                    Task<List<string>> serverTask =
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/1.{responseMinorVersion} 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n");

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    using (HttpResponseMessage response = await getResponseTask)
                    {                        
                        if (IsWinHttpHandler)
                        {
                            Assert.Equal(0, response.Version.Major);
                            Assert.Equal(0, response.Version.Minor);
                        }
                        else
                        {
                            Assert.Equal(1, response.Version.Major);
                            Assert.Equal(responseMinorVersion, response.Version.Minor);
                        }
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 9)]
        [InlineData(2, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        [InlineData(4, 2)]
        public async Task GetAsyncVersion11_BadResponseVersion_ThrowsOr00(int responseMajorVersion, int responseMinorVersion)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);
                    Task<List<string>> serverTask =
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/{responseMajorVersion}.{responseMinorVersion} 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n");

                    await Assert.ThrowsAsync<HttpRequestException>(() => getResponseTask);
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData("HTTP/1.1 200 OK", 200, "OK")]
        [InlineData("HTTP/1.1 200 Sure why not?", 200, "Sure why not?")]
        [InlineData("HTTP/1.1 200 OK\x0080", 200, "OK?")]
        [InlineData("HTTP/1.1 200 O K", 200, "O K")]
        [InlineData("HTTP/1.1 201 Created", 201, "Created")]
        [InlineData("HTTP/1.1 202 Accepted", 202, "Accepted")]
        [InlineData("HTTP/1.1 299 This is not a real status code", 299, "This is not a real status code")]
        [InlineData("HTTP/1.1 345 redirect to nowhere", 345, "redirect to nowhere")]
        [InlineData("HTTP/1.1 400 Bad Request", 400, "Bad Request")]
        [InlineData("HTTP/1.1 500 Internal Server Error", 500, "Internal Server Error")]
        [InlineData("HTTP/1.1 555 we just don't like you", 555, "we just don't like you")]
        [InlineData("HTTP/1.1 600 still valid", 600, "still valid")]
        public async Task GetAsync_ExpectedStatusCodeAndReason_Success(string statusLine, int expectedStatusCode, string expectedReason)
        {
            if (IsWinHttpHandler)
            {
                return;
            }

            await GetAsyncSuccessHelper(statusLine, expectedStatusCode, expectedReason);
        }

        [Theory]
        [InlineData("HTTP/1.1 200      ", 200, "     ")]
        [InlineData("HTTP/1.1 200      Something", 200, "     Something")]
        public async Task GetAsync_ExpectedStatusCodeAndReason_PlatformBehaviorTest(string statusLine, int expectedStatusCode, string reason)
        {
            if (IsWinHttpHandler)
            {
                // WinHttpHandler will trim space characters.
                await GetAsyncSuccessHelper(statusLine, expectedStatusCode, reason.Trim());
            }
            else
            {
                // SocketsHttpHandler and .NET Framework will keep the space characters.
                await GetAsyncSuccessHelper(statusLine, expectedStatusCode, reason);
            }
        }

        [Theory]
        [InlineData("HTTP/1.1 200", 200, "")] // This test data requires the fix in .NET Framework 4.7.3
        [InlineData("HTTP/1.1 200 O\tK", 200, "O\tK")]
        [InlineData("HTTP/1.1 200 O    \t\t  \t\t\t\t  \t K", 200, "O    \t\t  \t\t\t\t  \t K")]
        public async Task GetAsync_StatusLineNotFollowRFC_SuccessOnCore(string statusLine, int expectedStatusCode, string expectedReason)
        {
            await GetAsyncSuccessHelper(statusLine, expectedStatusCode, expectedReason);
        }

        private async Task GetAsyncSuccessHelper(string statusLine, int expectedStatusCode, string expectedReason)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(TestAsync, url);

                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"{statusLine}\r\n" +
                            "Connection: close\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "Content-Length: 0\r\n" +
                            "\r\n"));
                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
                        Assert.Equal(expectedReason, response.ReasonPhrase);
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        public static IEnumerable<string> GetInvalidStatusLine()
        {
            yield return "HTTP/1.1 2345";
            yield return "HTTP/A.1 200 OK";
            yield return "HTTP/X.Y.Z 200 OK";

            yield return "HTTP/0.1 200 OK";
            yield return "HTTP/3.5 200 OK";
            yield return "HTTP/1.12 200 OK";
            yield return "HTTP/12.1 200 OK";
            yield return "HTTP/1.1 200 O\rK";

            yield return "HTTP/1.A 200 OK";
            yield return "HTTP/1.1 ";
            yield return "HTTP/1.1 !11";
            yield return "HTTP/1.1 a11";
            yield return "HTTP/1.1 abc";
            yield return "HTTP/1.1\t\t";
            yield return "HTTP/1.1\t";
            yield return "HTTP/1.1  ";

            yield return "HTTP/1.1 200OK";
            yield return "HTTP/1.1 20c";
            yield return "HTTP/1.1 23";
            yield return "HTTP/1.1 2bc";

            yield return "NOTHTTP/1.1";
            yield return "HTTP 1.1 200 OK";
            yield return "ABCD/1.1 200 OK";
            yield return "HTTP/1.1";
            yield return "HTTP\\1.1 200 OK";
            yield return "NOTHTTP/1.1 200 OK";
        }

        public static TheoryData InvalidStatusLine = GetInvalidStatusLine().ToTheoryData();

        [Theory]
        [MemberData(nameof(InvalidStatusLine))]
        public async Task GetAsync_InvalidStatusLine_ThrowsException(string responseString)
        {
            await GetAsyncThrowsExceptionHelper(responseString);
        }

        [Fact]
        public async Task GetAsync_ReasonPhraseHasLF_BehaviorDifference()
        {
            await GetAsyncSuccessHelper("HTTP/1.1 200 O\n", 200, "O");
        }

        [Theory]
        [InlineData("HTTP/1.1\t200 OK")]
        [InlineData("HTTP/1.1 200\tOK")]
        [InlineData("HTTP/1.1 200\t")]
        public async Task GetAsync_InvalidStatusLine_ThrowsExceptionOnSocketsHttpHandler(string responseString)
        {
            if (!IsWinHttpHandler)
            {
                // SocketsHttpHandler and .NET Framework will throw HttpRequestException.
                await GetAsyncThrowsExceptionHelper(responseString);
            }
            // WinHttpHandler will succeed.
        }

        private async Task GetAsyncThrowsExceptionHelper(string responseString)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task ignoredServerTask = server.AcceptConnectionSendCustomResponseAndCloseAsync(
                        responseString + "\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(TestAsync, url));
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        public async Task GetAsync_ResponseHasNormalLineEndings_Success(string lineEnding)
        {
            // Using an unusually high timeout as this test can take a longer time to execute on busy CI machines.
            TimeSpan timeout = TimeSpan.FromMilliseconds(TestHelper.PassingTestTimeoutMilliseconds * 5);

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using HttpClient client = CreateHttpClient();
                client.Timeout = timeout;

                using HttpResponseMessage response = await client.GetAsync(TestAsync, url);

                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("OK", response.ReasonPhrase);
                Assert.Equal("TestServer", response.Headers.Server.ToString());
            },
            async server =>
            {
                await server.AcceptConnectionSendCustomResponseAndCloseAsync(
                    "HTTP/1.1 200 OK\nConnection: close\nDate: {DateTimeOffset.UtcNow:R}\nServer: TestServer\nContent-Length: 0\n\n".Replace("\n", lineEnding))
                    .WaitAsync(timeout);
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        public static IEnumerable<object[]> GetAsync_Chunked_VaryingSizeChunks_ReceivedCorrectly_MemberData()
        {
            foreach (int maxChunkSize in new[] { 1, 10_000 })
                foreach (string lineEnding in new[] { "\n", "\r\n" })
                    foreach (bool useCopyToAsync in new[] { false, true })
                        yield return new object[] { maxChunkSize, lineEnding, useCopyToAsync };
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(GetAsync_Chunked_VaryingSizeChunks_ReceivedCorrectly_MemberData))]
        public async Task GetAsync_Chunked_VaryingSizeChunks_ReceivedCorrectly(int maxChunkSize, string lineEnding, bool useCopyToAsync)
        {
            if (IsWinHttpHandler)
            {
                return;
            }
            
            var rand = new Random(42);
            byte[] expectedData = new byte[100_000];
            rand.NextBytes(expectedData);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpMessageInvoker client = new HttpMessageInvoker(CreateHttpClientHandler()))
                using (HttpResponseMessage resp = await client.SendAsync(TestAsync, new HttpRequestMessage(HttpMethod.Get, uri) { Version = base.UseVersion }, CancellationToken.None))
                using (Stream respStream = await resp.Content.ReadAsStreamAsync(TestAsync))
                {
                    var actualData = new MemoryStream();

                    if (useCopyToAsync)
                    {
                        await respStream.CopyToAsync(actualData);
                    }
                    else
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
#if !NETFRAMEWORK
                        while ((bytesRead = await respStream.ReadAsync(buffer)) > 0)
#else
                        while ((bytesRead = await respStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
#endif
                        {
                            actualData.Write(buffer, 0, bytesRead);
                        }
                    }

                    Assert.Equal<byte>(expectedData, actualData.ToArray());
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();

                    await connection.WriteStringAsync($"HTTP/1.1 200 OK{lineEnding}Transfer-Encoding: chunked{lineEnding}{lineEnding}");
                    for (int bytesSent = 0; bytesSent < expectedData.Length;)
                    {
                        int bytesRemaining = expectedData.Length - bytesSent;
                        int bytesToSend = rand.Next(1, Math.Min(bytesRemaining, maxChunkSize + 1));
                        await connection.WriteStringAsync($"{bytesToSend:X}{lineEnding}");
                        await connection.Stream.WriteAsync(new Memory<byte>(expectedData, bytesSent, bytesToSend));
                        await connection.WriteStringAsync(lineEnding);
                        bytesSent += bytesToSend;
                    }
                    await connection.WriteStringAsync($"0{lineEnding}");
                    await connection.WriteStringAsync(lineEnding);
                });
            });
        }

        [Theory]
        [InlineData("get", "GET")]
        [InlineData("head", "HEAD")]
        [InlineData("post", "POST")]
        [InlineData("put", "PUT")]
        [InlineData("delete", "DELETE")]
        [InlineData("options", "OPTIONS")]
        [InlineData("trace", "TRACE")]
#if !WINHTTPHANDLER_TEST
        [InlineData("patch", "PATCH")]
#endif
        [InlineData("other", "other")]
        [InlineData("SometHING", "SometHING")]
        public async Task CustomMethod_SentUppercasedIfKnown(string specifiedMethod, string expectedMethod)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var m = new HttpRequestMessage(new HttpMethod(specifiedMethod), uri) { Version = UseVersion };
                    (await client.SendAsync(TestAsync, m)).Dispose();
                }
            }, async server =>
            {
                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync();
                Assert.StartsWith(expectedMethod + " ", headers[0]);
            });
        }
    }

    public abstract class HttpProtocolTests_Dribble : HttpProtocolTests
    {
        public HttpProtocolTests_Dribble(ITestOutputHelper output) : base(output) { }

        protected override Stream GetStream(Stream s) => new DribbleStream(s);
    }
}
