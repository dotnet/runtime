// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpProtocolTests : HttpClientTestBase
    {
        protected virtual Stream GetStream(Stream s) => s;
        protected virtual Stream GetStream_ClientDisconnectOk(Stream s) => s;

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // Uap does not support 1.0
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
                    Task<List<string>> serverTask =
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream });

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    var requestLines = await serverTask;
                    Assert.Equal($"GET {url.PathAndQuery} HTTP/1.0", requestLines[0]);
                }
            });
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
                    Task<List<string>> serverTask =
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream });

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    var requestLines = await serverTask;
                    Assert.Equal($"GET {url.PathAndQuery} HTTP/1.1", requestLines[0]);
                }
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9)]
        public async Task GetAsync_RequestVersion0X_ThrowsOr11(int minorVersion)
        {
            Type exceptionType = null;
            if (UseManagedHandler)
            {
                exceptionType = typeof(NotSupportedException);
            }
            else if (PlatformDetection.IsFullFramework)
            {
                exceptionType = typeof(ArgumentException);
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(0, minorVersion);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream_ClientDisconnectOk });

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
            });
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
            if (PlatformDetection.IsFullFramework)
            {
                exceptionType = typeof(ArgumentException);
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(majorVersion, minorVersion);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream_ClientDisconnectOk });

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
            });
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
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.{responseMinorVersion} 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream });

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(1, response.Version.Major);
                        Assert.Equal(responseMinorVersion, response.Version.Minor);
                    }
                }
            });
        }

        [Theory]
        [InlineData(2)]
        [InlineData(7)]
        public async Task GetAsync_ResponseUnknownVersion1X_Success(int responseMinorVersion)
        {
            bool reportAs11 = PlatformDetection.IsFullFramework;
            bool reportAs00 = !UseManagedHandler;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.{responseMinorVersion} 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream });

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
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // Uap ignores response version if not 1.0 or 1.1
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9)]
        public async Task GetAsync_ResponseVersion0X_ThrowsOr10(int responseMinorVersion)
        {
            bool reportAs10 = PlatformDetection.IsFullFramework;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = HttpVersion.Version11;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask =
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/0.{responseMinorVersion} 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream_ClientDisconnectOk });

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
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // Uap ignores response version if not 1.0 or 1.1
        [Theory]
        [InlineData(2, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        [InlineData(4, 2)]
        public async Task GetAsyncVersion11_BadResponseVersion_ThrowsOr00(int responseMajorVersion, int responseMinorVersion)
        {
            // Full framework reports 1.0 or 1.1, depending on minor version, instead of throwing
            bool reportAs1X = PlatformDetection.IsFullFramework;

            // CurlHandler reports these as 0.0, except for 2.0 which is reported as 2.0, instead of throwing.
            bool reportAs00 = false;
            bool reportAs20 = false;
            if (!PlatformDetection.IsWindows && !UseManagedHandler)
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
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/{responseMajorVersion}.{responseMinorVersion} 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream_ClientDisconnectOk });

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
            });
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
            await GetAsyncSuccessHelper(statusLine, expectedStatusCode, expectedReason);
        }

        [Theory]
        [InlineData("HTTP/1.1 200      ", 200, "     ", "")]
        [InlineData("HTTP/1.1 200      Something", 200, "     Something", "Something")]
        public async Task GetAsync_ExpectedStatusCodeAndReason_PlatformBehaviorTest(string statusLine,
            int expectedStatusCode, string reasonWithSpace, string reasonNoSpace)
        {
            if (UseManagedHandler || PlatformDetection.IsFullFramework)
            {
                // ManagedHandler and .NET Framework will keep the space characters.
                await GetAsyncSuccessHelper(statusLine, expectedStatusCode, reasonWithSpace);
            }
            else
            {
                // WinRT, WinHttpHandler, and CurlHandler will trim space characters.
                await GetAsyncSuccessHelper(statusLine, expectedStatusCode, reasonNoSpace);
            }
        }
        
        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The following pass on .NET Core but fail on .NET Framework.")]
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
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"{statusLine}\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "\r\n",
                            new LoopbackServer.Options { ResponseStreamWrapper = GetStream }));
                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
                        Assert.Equal(expectedReason, response.ReasonPhrase);
                    }
                }
            });
        }

        public static IEnumerable<string> GetInvalidStatusLine()
        {
            yield return "HTTP/1.1 2345";
            yield return "HTTP/A.1 200 OK";
            yield return "HTTP/X.Y.Z 200 OK";
            yield return "HTTP/1.A 200 OK";
            yield return "HTTP/1.1 ";
            yield return "HTTP/1.1 !11";
            yield return "HTTP/1.1 a11";
            yield return "HTTP/1.1 abc";
            yield return "HTTP/1.1\t\t";
            yield return "HTTP/1.1\t";
            yield return "HTTP/1.1  ";
            
            // Only pass on .NET Core Windows & ManagedHandler.
            if (PlatformDetection.IsNetCore && PlatformDetection.IsWindows)
            {
                yield return "HTTP/0.1 200 OK";
                yield return "HTTP/3.5 200 OK";
                yield return "HTTP/1.12 200 OK";
                yield return "HTTP/12.1 200 OK";
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
                // ActiveIssue: #26946
                // yield return "HTTP/1.1 200 O\rK";
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
            string responseString = "HTTP/1.1 200 O\nK";
            int expectedStatusCode = 200;
            string expectedReason = "O";

            if (IsWinHttpHandler || IsNetfxHandler || IsCurlHandler)
            {
                // WinHttpHandler, .NET Framework, and CurlHandler will throw HttpRequestException.
                await GetAsyncThrowsExceptionHelper(responseString);
            }
            else
            {
                // UAP and ManagedHandler will allow LF ending.
                await GetAsyncSuccessHelper(responseString, expectedStatusCode, expectedReason);
            }
        }

        [Theory]
        [InlineData("HTTP/1.1\t200 OK")]
        [InlineData("HTTP/1.1 200\tOK")]
        [InlineData("HTTP/1.1 200\t")]
        public async Task GetAsync_InvalidStatusLine_ThrowsExceptionOnManagedHandler(string responseString)
        {
            if (UseManagedHandler || PlatformDetection.IsFullFramework)
            {
                // ManagedHandler and .NET Framework will throw HttpRequestException.
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
                    Task ignoredServerTask = LoopbackServer.ReadRequestAndSendResponseAsync(
                        server,
                        responseString + "\r\nContent-Length: 0\r\n\r\n",
                        new LoopbackServer.Options { ResponseStreamWrapper = GetStream });

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                }
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]   // Does not support LF-only
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
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        $"HTTP/1.1 200 OK{lineEnding}Date: {DateTimeOffset.UtcNow:R}{lineEnding}Server: TestServer{lineEnding}Content-Length: 0{lineEnding}{lineEnding}",
                        new LoopbackServer.Options { ResponseStreamWrapper = GetStream });

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(200, (int)response.StatusCode);
                        Assert.Equal("OK", response.ReasonPhrase);
                        Assert.Equal("TestServer", response.Headers.Server.ToString());
                    }
                }
            });
        }
    }

    public class HttpProtocolTests_Dribble : HttpProtocolTests
    {
        protected override Stream GetStream(Stream s) => new DribbleStream(s);
        protected override Stream GetStream_ClientDisconnectOk(Stream s) => new DribbleStream(s, true);

        private sealed class DribbleStream : Stream
        {
            private readonly Stream _wrapped;
            private readonly bool _clientDisconnectAllowed;

            public DribbleStream(Stream wrapped, bool clientDisconnectAllowed = false)
            {
                _wrapped = wrapped;
                _clientDisconnectAllowed = clientDisconnectAllowed;
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        await _wrapped.WriteAsync(buffer, offset + i, 1);
                        await Task.Yield(); // introduce short delays, enough to send packets individually but not so long as to extend test duration significantly
                    }
                }
                catch (IOException) when (_clientDisconnectAllowed)
                {
                }
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => _wrapped.CanWrite;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => _wrapped.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => _wrapped.FlushAsync(cancellationToken);
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }
    }
}
