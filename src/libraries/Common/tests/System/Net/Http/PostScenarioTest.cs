// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public abstract class PostScenarioTest : HttpClientHandlerTestBase
    {
        private const string ExpectedContent = "Test contest";
        private const string UserName = "user1";
        private const string Password = "PLACEHOLDER";

        public PostScenarioTest(ITestOutputHelper output) : base(output) { }

#if !NETFRAMEWORK
        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostRewindableStreamContentMultipleTimes_StreamContentFullySent(Configuration.Http.RemoteServer remoteServer)
        {
            const string requestBody = "ABC";

            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)))
            {
                var content = new StreamContent(ms);

                for (int i = 1; i <= 3; i++)
                {
                    HttpResponseMessage response = await client.PostAsync(remoteServer.EchoUri, content);
                    Assert.Equal(requestBody.Length, ms.Position); // Stream left at end after send.

                    string responseBody = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseBody);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseBody, "BodyContent", requestBody));
                }
            }
        }
#endif

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [MemberData(nameof(RemoteServersMemberData))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupportedOrNotBrowser))]
        public async Task PostNoContentUsingContentLengthSemantics_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, string.Empty, null,
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostEmptyContentUsingContentLengthSemantics_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, string.Empty, new StringContent(string.Empty),
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostEmptyContentUsingChunkedEncoding_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, string.Empty, new StringContent(string.Empty),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostEmptyContentUsingConflictingSemantics_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, string.Empty, new StringContent(string.Empty),
                useContentLengthUpload: true, useChunkedEncodingUpload: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostUsingContentLengthSemantics_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostUsingChunkedEncoding_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostSyncBlockingContentUsingChunkedEncoding_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, ExpectedContent, new SyncBlockingContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostRepeatedFlushContentUsingChunkedEncoding_Success(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, ExpectedContent, new RepeatedFlushContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostUsingUsingConflictingSemantics_UsesChunkedSemantics(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: true, useChunkedEncodingUpload: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostUsingNoSpecifiedSemantics_UsesChunkedSemantics(Configuration.Http.RemoteServer remoteServer)
        {
            await PostHelper(remoteServer, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: false);
        }

        public static IEnumerable<object[]> RemoteServersAndLargeContentSizes()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
            {
                yield return new object[] { remoteServer, 5 * 1024 };
                yield return new object[] { remoteServer, 63 * 1024 };
                yield return new object[] { remoteServer, 129 * 1024 };
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(RemoteServersAndLargeContentSizes))]
        public async Task PostLargeContentUsingContentLengthSemantics_Success(Configuration.Http.RemoteServer remoteServer, int contentLength)
        {
            var rand = new Random(42);
            var sb = new StringBuilder(contentLength);
            for (int i = 0; i < contentLength; i++)
            {
                sb.Append((char)(rand.Next(0, 26) + 'a'));
            }
            string content = sb.ToString();

            await PostHelper(remoteServer, content, new StringContent(content),
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [OuterLoop("Uses external servers")]
        [SkipOnPlatform(TestPlatforms.Browser, "PreAuthenticate not supported on Browser")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task PostRewindableContentUsingAuth_NoPreAuthenticate_Success(Configuration.Http.RemoteServer remoteServer)
        {
            if (remoteServer.HttpVersion == new Version(2, 0))
            {
                // This is occasionally timing out in CI with SocketsHttpHandler and HTTP2, particularly on Linux
                // Likely this is just a very slow test and not a product issue, so just increasing the timeout may be the right fix.
                // Disable until we can investigate further.
                return;
            }

            HttpContent content = new StreamContent(new CustomContent.CustomStream(Encoding.UTF8.GetBytes(ExpectedContent), true));
            var credential = new NetworkCredential(UserName, Password);
            await PostUsingAuthHelper(remoteServer, ExpectedContent, content, credential, false);
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "PreAuthenticate not supported on Browser")]
        public async Task PostNonRewindableContentUsingAuth_NoPreAuthenticate_ThrowsHttpRequestException(Configuration.Http.RemoteServer remoteServer)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && remoteServer.HttpVersion.Major >= 2)
            {
                return;
            }

            HttpContent content = new StreamContent(new CustomContent.CustomStream(Encoding.UTF8.GetBytes(ExpectedContent), false));
            var credential = new NetworkCredential(UserName, Password);
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                PostUsingAuthHelper(remoteServer, ExpectedContent, content, credential, preAuthenticate: false));
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "PreAuthenticate not supported on Browser")]
        public async Task PostNonRewindableContentUsingAuth_PreAuthenticate_Success(Configuration.Http.RemoteServer remoteServer)
        {
            // Sync API supported only up to HTTP/1.1
            if (!TestAsync && remoteServer.HttpVersion.Major >= 2)
            {
                return;
            }

            HttpContent content = new StreamContent(new CustomContent.CustomStream(Encoding.UTF8.GetBytes(ExpectedContent), false));
            var credential = new NetworkCredential(UserName, Password);
            await PostUsingAuthHelper(remoteServer, ExpectedContent, content, credential, preAuthenticate: true);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [MemberData(nameof(RemoteServersMemberData))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupportedOrNotBrowser))]
        public async Task PostAsync_EmptyContent_ContentTypeHeaderNotSent(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response = await client.PostAsync(remoteServer.EchoUri, null))
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                bool sentContentType = TestHelper.JsonMessageContainsKey(responseContent, "Content-Type");

                Assert.False(sentContentType);
            }
        }

        private async Task PostHelper(
            Configuration.Http.RemoteServer remoteServer,
            string requestBody,
            HttpContent requestContent,
            bool useContentLengthUpload,
            bool useChunkedEncodingUpload)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                if (requestContent != null)
                {
                    if (useContentLengthUpload)
                    {
                        // Ensure that Content-Length is populated (see https://github.com/dotnet/runtime/issues/25086)
                        requestContent.Headers.ContentLength = requestContent.Headers.ContentLength;
                    }
                    else
                    {
                        requestContent.Headers.ContentLength = null;
                    }

                    // Compute MD5 of request body data. This will be verified by the server when it
                    // receives the request.
                    if (PlatformDetection.IsBrowser)
                    {
                        // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
                        requestContent.Headers.Add("Content-MD5-Skip", "browser");
                    }
                    else
                    {
                        requestContent.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(requestBody);
                    }
                }

                if (useChunkedEncodingUpload)
                {
                    client.DefaultRequestHeaders.TransferEncodingChunked = true;
                }

                using (HttpResponseMessage response = await client.PostAsync(remoteServer.VerifyUploadUri, requestContent))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        private async Task PostUsingAuthHelper(
            Configuration.Http.RemoteServer remoteServer,
            string requestBody,
            HttpContent requestContent,
            NetworkCredential credential,
            bool preAuthenticate)
        {
            Uri serverUri = remoteServer.BasicAuthUriForCreds(UserName, Password);

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.PreAuthenticate = preAuthenticate;
            handler.Credentials = credential;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                // Send HEAD request to help bypass the 401 auth challenge for the latter POST assuming
                // that the authentication will be cached and re-used later when PreAuthenticate is true.
                var request = new HttpRequestMessage(HttpMethod.Head, serverUri) { Version = remoteServer.HttpVersion };
                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                // Now send POST request.
                request = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = remoteServer.HttpVersion };
                request.Content = requestContent;
                requestContent.Headers.ContentLength = null;
                request.Headers.TransferEncodingChunked = true;

                using (HttpResponseMessage response = await client.SendAsync(TestAsync, request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);

                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        true,
                        requestBody);
                }
            }
        }
    }
}
