// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public class PostScenarioTest
    {
        private const string ExpectedContent = "Test contest";
        private const string UserName = "user1";
        private const string Password = "password1";
        private readonly static Uri BasicAuthServerUri =
            HttpTestServers.BasicAuthUriForCreds(false, UserName, Password);
        private readonly static Uri SecureBasicAuthServerUri =
            HttpTestServers.BasicAuthUriForCreds(true, UserName, Password);

        private readonly ITestOutputHelper _output;

        public readonly static object[][] EchoServers = HttpTestServers.EchoServers;

        public readonly static object[][] BasicAuthEchoServers =
            new object[][]
                {
                    new object[] { BasicAuthServerUri },
                    new object[] { SecureBasicAuthServerUri }
                };

        public PostScenarioTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostNoContentUsingContentLengthSemantics_Success(Uri serverUri)
        {
            await PostHelper(serverUri, string.Empty, null,
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostEmptyContentUsingContentLengthSemantics_Success(Uri serverUri)
        {
            await PostHelper(serverUri, string.Empty, new StringContent(string.Empty),
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [ActiveIssue(5485, PlatformID.Windows)]
        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostEmptyContentUsingChunkedEncoding_Success(Uri serverUri)
        {
            await PostHelper(serverUri, string.Empty, new StringContent(string.Empty),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostUsingContentLengthSemantics_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostUsingChunkedEncoding_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostSyncBlockingContentUsingChunkedEncoding_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new SyncBlockingContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostRepeatedFlushContentUsingChunkedEncoding_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new RepeatedFlushContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostUsingUsingConflictingSemantics_UsesChunkedSemantics(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: true, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostUsingNoSpecifiedSemantics_UsesChunkedSemantics(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: false);
        }

        [Theory, MemberData(nameof(BasicAuthEchoServers))]
        public async Task PostRewindableContentUsingAuth_NoPreAuthenticate_Success(Uri serverUri)
        {
            HttpContent content = CustomContent.Create(ExpectedContent, true);
            var credential = new NetworkCredential(UserName, Password);
            await PostUsingAuthHelper(serverUri, ExpectedContent, content, credential, false);
        }

        [Theory, MemberData(nameof(BasicAuthEchoServers))]
        public async Task PostNonRewindableContentUsingAuth_NoPreAuthenticate_ThrowsInvalidOperationException(Uri serverUri)
        {
            HttpContent content = CustomContent.Create(ExpectedContent, false);
            var credential = new NetworkCredential(UserName, Password);
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                PostUsingAuthHelper(serverUri, ExpectedContent, content, credential, preAuthenticate: false));
        }

        [Theory, MemberData(nameof(BasicAuthEchoServers))]
        public async Task PostNonRewindableContentUsingAuth_PreAuthenticate_Success(Uri serverUri)
        {
            HttpContent content = CustomContent.Create(ExpectedContent, false);
            var credential = new NetworkCredential(UserName, Password);
            await PostUsingAuthHelper(serverUri, ExpectedContent, content, credential, preAuthenticate: true);
        }
        
        private async Task PostHelper(
            Uri serverUri,
            string requestBody,
            HttpContent requestContent,
            bool useContentLengthUpload,
            bool useChunkedEncodingUpload)
        {
            using (var client = new HttpClient())
            {
                if (!useContentLengthUpload && requestContent != null)
                {
                    requestContent.Headers.ContentLength = null;
                }
                
                if (useChunkedEncodingUpload)
                {
                    client.DefaultRequestHeaders.TransferEncodingChunked = true;
                }

                using (HttpResponseMessage response = await client.PostAsync(serverUri, requestContent))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);

                    if (!useContentLengthUpload && !useChunkedEncodingUpload)
                    {
                        useChunkedEncodingUpload = true;
                    }

                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        useChunkedEncodingUpload,
                        requestBody);
                }
            }          
        }

        private async Task PostUsingAuthHelper(
            Uri serverUri,
            string requestBody,
            HttpContent requestContent,
            NetworkCredential credential,
            bool preAuthenticate)
        {
            var handler = new HttpClientHandler();
            handler.PreAuthenticate = preAuthenticate;
            handler.Credentials = credential;
            using (var client = new HttpClient(handler))
            {
                // Send HEAD request to help bypass the 401 auth challenge for the latter POST assuming
                // that the authentication will be cached and re-used later when PreAuthenticate is true.
                var request = new HttpRequestMessage(HttpMethod.Head, serverUri);
                HttpResponseMessage response;
                using (response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                // Now send POST request.
                request = new HttpRequestMessage(HttpMethod.Post, serverUri);
                request.Content = requestContent;
                requestContent.Headers.ContentLength = null;
                request.Headers.TransferEncodingChunked = true;

                using (response = await client.PostAsync(serverUri, requestContent))
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
