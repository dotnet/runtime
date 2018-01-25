// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        // TODO #24713: The following pass on Windows but fail on CurlHandler on Linux.
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

        [Theory]
        [InlineData("HTTP/1.1 2345")]
        [InlineData("HTTP/A.1 200 OK")]
        [InlineData("HTTP/X.Y.Z 200 OK")]
        // TODO #24713: The following pass on Windows on .NET Core but fail on .NET Framework.
        //[InlineData("HTTP/0.1 200 OK")]
        //[InlineData("HTTP/3.5 200 OK")]
        //[InlineData("HTTP/1.12 200 OK")]
        //[InlineData("HTTP/12.1 200 OK")]
        // TODO #24713: The following pass on Windows on .NET Core but fail on UWP / WinRT.
        //[InlineData("HTTP/1.1 200 O\nK")]
        //[InlineData("HTTP/1.1 200OK")]
        //[InlineData("HTTP/1.1 20c")]
        //[InlineData("HTTP/1.1 23")]
        //[InlineData("HTTP/1.1 2bc")]
        // TODO #24713: The following pass on Windows but fail on CurlHandler on Linux.
        //[InlineData("NOTHTTP/1.1")]
        //[InlineData("HTTP/1.A 200 OK")]
        //[InlineData("HTTP 1.1 200 OK")]
        //[InlineData("ABCD/1.1 200 OK")]
        //[InlineData("HTTP/1.1")]
        //[InlineData("HTTP\\1.1 200 OK")]
        //[InlineData("HTTP/1.1 ")]
        //[InlineData("HTTP/1.1 !11")]
        //[InlineData("HTTP/1.1 a11")]
        //[InlineData("HTTP/1.1 abc")]
        //[InlineData("HTTP/1.1 200 O\rK")]
        //[InlineData("HTTP/1.1\t\t")]
        //[InlineData("HTTP/1.1\t")]
        //[InlineData("HTTP/1.1  ")]
        //[InlineData("NOTHTTP/1.1 200 OK")]
        public async Task GetAsync_InvalidStatusLine_ThrowsException(string responseString)
        {
            await GetAsyncThrowsExceptionHelper(responseString);
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
    }

    public class HttpProtocolTests_Dribble : HttpProtocolTests
    {
        protected override Stream GetStream(Stream s) => new DribbleStream(s);

        private sealed class DribbleStream : Stream
        {
            private readonly Stream _wrapped;

            public DribbleStream(Stream wrapped) => _wrapped = wrapped;

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                for (int i = 0; i < count; i++)
                {
                    await _wrapped.WriteAsync(buffer, offset + i, 1);
                    await Task.Delay(3); // introduce short delays, enough to send packets individually but not so long as to extend test duration significantly
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
