// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpProtocolTests : HttpClientHandlerTestBase
    {
        protected virtual Stream GetStream(Stream s) => s;
        protected virtual Stream GetStream_ClientDisconnectOk(Stream s) => s;

        public HttpProtocolTests(ITestOutputHelper output) : base(output) { }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Uap does not support 1.0")]
        [Fact]
        public async Task GetAsync_RequestVersion10_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version10;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
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

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
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
        public async Task GetAsync_RequestVersion0X_ThrowsOr11(int minorVersion)
        {
            Type exceptionType = null;
            if (UseSocketsHttpHandler)
            {
                exceptionType = typeof(NotSupportedException);
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(0, minorVersion);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    if (exceptionType == null)
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);
                        var requestLines = await serverTask;
                        Assert.Equal($"GET {url.PathAndQuery} HTTP/1.1", requestLines[0]);
                    }
                    else
                    {
                        await Assert.ThrowsAsync(exceptionType, (() => TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask)));
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream_ClientDisconnectOk});
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 6)]
        [InlineData(2, 0)]  // Note, this is plain HTTP (not HTTPS), so 2.0 is not supported and should degrade to 1.1
        [InlineData(2, 1)]
        [InlineData(2, 7)]
        [InlineData(3, 0)]
        [InlineData(4, 2)]
        public async Task GetAsync_UnknownRequestVersion_ThrowsOrDegradesTo11(int majorVersion, int minorVersion)
        {
            Type exceptionType = null;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(majorVersion, minorVersion);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    if (exceptionType == null)
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);
                        var requestLines = await serverTask;
                        Assert.Equal($"GET {url.PathAndQuery} HTTP/1.1", requestLines[0]);
                    }
                    else
                    {
                        await Assert.ThrowsAsync(exceptionType, (() => TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask)));
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream_ClientDisconnectOk });
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

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
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
            bool reportAs11 = false;
            bool reportAs00 = !UseSocketsHttpHandler;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/1.{responseMinorVersion} 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n");

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        if (reportAs11)
                        {
                            Assert.Equal(1, response.Version.Major);
                            Assert.Equal(1, response.Version.Minor);
                        }
                        else if (reportAs00)
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Uap ignores response version if not 1.0 or 1.1")]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9)]
        public async Task GetAsync_ResponseVersion0X_ThrowsOr10(int responseMinorVersion)
        {
            bool reportAs10 = false;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/0.{responseMinorVersion} 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n");

                    if (reportAs10)
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(1, response.Version.Major);
                            Assert.Equal(0, response.Version.Minor);
                        }
                    }
                    else
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(async () => await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask));
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream_ClientDisconnectOk });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Uap ignores response version if not 1.0 or 1.1")]
        [Theory]
        [InlineData(2, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        [InlineData(4, 2)]
        public async Task GetAsyncVersion11_BadResponseVersion_ThrowsOr00(int responseMajorVersion, int responseMinorVersion)
        {
            // Full framework reports 1.0 or 1.1, depending on minor version, instead of throwing
            bool reportAs1X = false;

            // CurlHandler reports these as 0.0, except for 2.0 which is reported as 2.0, instead of throwing.
            bool reportAs00 = false;
            bool reportAs20 = false;
            if (!PlatformDetection.IsWindows && !UseSocketsHttpHandler)
            {
                if (responseMajorVersion == 2 && responseMinorVersion == 0)
                {
                    reportAs20 = true;
                }
                else
                {
                    reportAs00 = true;
                }
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/{responseMajorVersion}.{responseMinorVersion} 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n");

                    if (reportAs00)
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(0, response.Version.Major);
                            Assert.Equal(0, response.Version.Minor);
                        }
                    }
                    else if (reportAs20)
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(2, response.Version.Major);
                            Assert.Equal(0, response.Version.Minor);
                        }
                    }
                    else if (reportAs1X)
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(1, response.Version.Major);
                            Assert.Equal(responseMinorVersion == 0 ? 0 : 1, response.Version.Minor);
                        }
                    }
                    else
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(async () => await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask));
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream_ClientDisconnectOk });
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
                return; // [ActiveIssue(25880)]
            }

            await GetAsyncSuccessHelper(statusLine, expectedStatusCode, expectedReason);
        }

        [Theory]
        [InlineData("HTTP/1.1 200      ", 200, "     ", "")]
        [InlineData("HTTP/1.1 200      Something", 200, "     Something", "Something")]
        public async Task GetAsync_ExpectedStatusCodeAndReason_PlatformBehaviorTest(string statusLine,
            int expectedStatusCode, string reasonWithSpace, string reasonNoSpace)
        {
            if (UseSocketsHttpHandler)
            {
                // SocketsHttpHandler and .NET Framework will keep the space characters.
                await GetAsyncSuccessHelper(statusLine, expectedStatusCode, reasonWithSpace);
            }
            else
            {
                // WinRT, WinHttpHandler, and CurlHandler will trim space characters.
                await GetAsyncSuccessHelper(statusLine, expectedStatusCode, reasonNoSpace);
            }
        }
        
        [Theory]
        [InlineData("HTTP/1.1 200", 200, "")] // This test data requires the fix in .NET Framework 4.7.3
        [InlineData("HTTP/1.1 200 O\tK", 200, "O\tK")]
        [InlineData("HTTP/1.1 200 O    \t\t  \t\t\t\t  \t K", 200, "O    \t\t  \t\t\t\t  \t K")]
        // Only CurlHandler will trim the '\t' at the end and causing failure.
        // [InlineData("HTTP/1.1 999 this\ttoo\t", 999, "this\ttoo\t")]
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
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
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
            
            // Only pass on .NET Core Windows & SocketsHttpHandler.
            if (PlatformDetection.IsNetCore && !PlatformDetection.IsUap && PlatformDetection.IsWindows)
            {
                yield return "HTTP/0.1 200 OK";
                yield return "HTTP/3.5 200 OK";
                yield return "HTTP/1.12 200 OK";
                yield return "HTTP/12.1 200 OK";
                yield return "HTTP/1.1 200 O\rK";
            }

            // Skip these test cases on CurlHandler since the behavior is different.
            if (PlatformDetection.IsWindows)
            {
                yield return "HTTP/1.A 200 OK";
                yield return "HTTP/1.1 ";
                yield return "HTTP/1.1 !11";
                yield return "HTTP/1.1 a11";
                yield return "HTTP/1.1 abc";
                yield return "HTTP/1.1\t\t";
                yield return "HTTP/1.1\t";
                yield return "HTTP/1.1  ";
            }
            
            // Skip these test cases on UAP since the behavior is different.
            if (!PlatformDetection.IsUap)
            {
                yield return "HTTP/1.1 200OK";
                yield return "HTTP/1.1 20c";
                yield return "HTTP/1.1 23";
                yield return "HTTP/1.1 2bc";
            }

            // Skip these test cases on UAP & CurlHandler since the behavior is different.
            if (!PlatformDetection.IsUap && PlatformDetection.IsWindows)
            {
                yield return "NOTHTTP/1.1";
                yield return "HTTP 1.1 200 OK";
                yield return "ABCD/1.1 200 OK";
                yield return "HTTP/1.1";
                yield return "HTTP\\1.1 200 OK";
                yield return "NOTHTTP/1.1 200 OK";
            }
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
            string responseString = "HTTP/1.1 200 O\n";
            int expectedStatusCode = 200;
            string expectedReason = "O";

            if (IsNetfxHandler)
            {
                // .NET Framework will throw HttpRequestException.
                await GetAsyncThrowsExceptionHelper(responseString);
            }
            else
            {
                await GetAsyncSuccessHelper(responseString, expectedStatusCode, expectedReason);
            }
        }

        [Theory]
        [InlineData("HTTP/1.1\t200 OK")]
        [InlineData("HTTP/1.1 200\tOK")]
        [InlineData("HTTP/1.1 200\t")]
        public async Task GetAsync_InvalidStatusLine_ThrowsExceptionOnSocketsHttpHandler(string responseString)
        {
            if (UseSocketsHttpHandler)
            {
                // SocketsHttpHandler and .NET Framework will throw HttpRequestException.
                await GetAsyncThrowsExceptionHelper(responseString);
            }
            // WinRT, WinHttpHandler, and CurlHandler will succeed.
        }

        private async Task GetAsyncThrowsExceptionHelper(string responseString)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task ignoredServerTask = server.AcceptConnectionSendCustomResponseAndCloseAsync(
                        responseString + "\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        public async Task GetAsync_ResponseHasNormalLineEndings_Success(string lineEnding)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendCustomResponseAndCloseAsync(
                        $"HTTP/1.1 200 OK{lineEnding}Connection: close\r\nDate: {DateTimeOffset.UtcNow:R}{lineEnding}Server: TestServer{lineEnding}Content-Length: 0{lineEnding}{lineEnding}");

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(200, (int)response.StatusCode);
                        Assert.Equal("OK", response.ReasonPhrase);
                        Assert.Equal("TestServer", response.Headers.Server.ToString());
                    }
                }
            }, new LoopbackServer.Options { StreamWrapper = GetStream });
        }

        public static IEnumerable<object> GetAsync_Chunked_VaryingSizeChunks_ReceivedCorrectly_MemberData()
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
                // [ActiveIssue(28423)]
                return;
            }

            if (!UseSocketsHttpHandler && lineEnding != "\r\n")
            {
                // Some handlers don't deal well with "\n" alone as the line ending
                return;
            }

            var rand = new Random(42);
            byte[] expectedData = new byte[100_000];
            rand.NextBytes(expectedData);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpMessageInvoker client = new HttpMessageInvoker(CreateHttpClientHandler()))
                using (HttpResponseMessage resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 }, CancellationToken.None))
                using (Stream respStream = await resp.Content.ReadAsStreamAsync())
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
                        while ((bytesRead = await respStream.ReadAsync(buffer)) > 0)
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

                    await connection.Writer.WriteAsync($"HTTP/1.1 200 OK{lineEnding}Transfer-Encoding: chunked{lineEnding}{lineEnding}");
                    for (int bytesSent = 0; bytesSent < expectedData.Length;)
                    {
                        int bytesRemaining = expectedData.Length - bytesSent;
                        int bytesToSend = rand.Next(1, Math.Min(bytesRemaining, maxChunkSize + 1));
                        await connection.Writer.WriteAsync(bytesToSend.ToString("X") + lineEnding);
                        await connection.Stream.WriteAsync(new Memory<byte>(expectedData, bytesSent, bytesToSend));
                        await connection.Writer.WriteAsync(lineEnding);
                        bytesSent += bytesToSend;
                    }
                    await connection.Writer.WriteAsync($"0{lineEnding}");
                    await connection.Writer.WriteAsync(lineEnding);
                });
            });
        }

        [ActiveIssue(29802, TargetFrameworkMonikers.Uap)]
        [Theory]
        [InlineData("get", "GET")]
        [InlineData("head", "HEAD")]
        [InlineData("post", "POST")]
        [InlineData("put", "PUT")]
        [InlineData("other", "other")]
        [InlineData("SometHING", "SometHING")]
        public async Task CustomMethod_SentUppercasedIfKnown(string specifiedMethod, string expectedMethod)
        {
            if (IsCurlHandler && specifiedMethod == "get")
            {
                // CurlHandler doesn't special-case "get" and sends it in the original casing.
                expectedMethod = specifiedMethod;
            }

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var m = new HttpRequestMessage(new HttpMethod(specifiedMethod), uri) { Version = VersionFromUseHttp2 };
                    (await client.SendAsync(m)).Dispose();
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
        protected override Stream GetStream_ClientDisconnectOk(Stream s) => new DribbleStream(s, true);
    }
}
