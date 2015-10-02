// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Tests;
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
            HttpTestServers2.BasicAuthUriForCreds(false, UserName, Password);
        private readonly static Uri SecureBasicAuthServerUri =
            HttpTestServers2.BasicAuthUriForCreds(true, UserName, Password);

        private readonly ITestOutputHelper _output;

        public readonly static object[][] EchoServers = HttpTestServers2.EchoServers;

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

        [Theory, MemberData("EchoServers")]
        public async Task PostUsingContentLengthSemantics_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: true, useChunkedEncodingUpload: false);
        }

        [Theory, MemberData("EchoServers")]
        public async Task PostUsingChunkedEncoding_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData("EchoServers")]
        public async Task PostRepeatedFlushContentUsingChunkedEncoding_Success(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new RepeatedFlushContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData("EchoServers")]
        public async Task PostUsingUsingConflictingSemantics_UsesChunkedSemantics(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: true, useChunkedEncodingUpload: true);
        }

        [Theory, MemberData("EchoServers")]
        public async Task PostUsingNoSpecifiedSemantics_UsesChunkedSemantics(Uri serverUri)
        {
            await PostHelper(serverUri, ExpectedContent, new StringContent(ExpectedContent),
                useContentLengthUpload: false, useChunkedEncodingUpload: false);
        }

        [Theory, MemberData("BasicAuthEchoServers")]
        [ActiveIssue(3699, PlatformID.AnyUnix)]
        public async Task PostRewindableContentUsingAuth_NoPreAuthenticate_Success(Uri serverUri)
        {
            HttpContent content = CustomContent.Create(ExpectedContent, true);
            var credential = new NetworkCredential(UserName, Password);
            await PostUsingAuthHelper(serverUri, ExpectedContent, content, credential, false);
        }

        [Theory, MemberData("BasicAuthEchoServers")]
        [ActiveIssue(3693, PlatformID.AnyUnix)]
        public async Task PostNonRewindableContentUsingAuth_NoPreAuthenticate_ThrowsInvalidOperationException(Uri serverUri)
        {
            HttpContent content = CustomContent.Create(ExpectedContent, false);
            var credential = new NetworkCredential(UserName, Password);
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                PostUsingAuthHelper(serverUri, ExpectedContent, content, credential, preAuthenticate: false));
        }

        [Theory, MemberData("BasicAuthEchoServers")]
        [ActiveIssue(3695, PlatformID.AnyUnix)]
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
                if (!useContentLengthUpload)
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

                    VerifyResponseBody(
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

                    VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        true,
                        requestBody);
                }
            }
        }

        private bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            // TODO: Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies
            string pattern = string.Format(@"""{0}"": ""{1}""", key, value);
            return message.Contains(pattern);
        }

        private void VerifyResponseBody(
            string responseContent,
            byte[] expectedMD5Hash,
            bool chunkedUpload,
            string requestBody)
        {
            // Compare computed hash with transmitted hash.
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(responseContent);
                byte[] actualMD5Hash = md5.ComputeHash(bytes);
                Assert.Equal(expectedMD5Hash, actualMD5Hash);
            }

            // Verify upload semsntics: 'Content-Length' vs. 'Transfer-Encoding: chunked'.
            bool requestUsedContentLengthUpload =
                JsonMessageContainsKeyValue(responseContent, "Content-Length", requestBody.Length.ToString());
            bool requestUsedChunkedUpload =
                JsonMessageContainsKeyValue(responseContent, "Transfer-Encoding", "chunked");
            Assert.NotEqual(requestUsedContentLengthUpload, requestUsedChunkedUpload);
            Assert.Equal(chunkedUpload, requestUsedChunkedUpload);
            Assert.Equal(!chunkedUpload, requestUsedContentLengthUpload);

            // Verify that request body content was correctly sent to server.
            Assert.True(JsonMessageContainsKeyValue(responseContent, "BodyContent", requestBody), "Valid request body");
        }
    }
}
