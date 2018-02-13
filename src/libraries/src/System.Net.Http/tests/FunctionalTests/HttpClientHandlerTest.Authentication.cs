// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandler_Authentication_Test : HttpClientTestBase
    {
        private const string Username = "testusername";
        private const string Password = "testpassword";
        private const string Domain = "testdomain";

        private NetworkCredential _credentials = new NetworkCredential(Username, Password, Domain);

        private Func<HttpClientHandler, Uri, HttpStatusCode, NetworkCredential, Task> _createAndValidateRequest = async (handler, url, expectedStatusCode, credentials) =>
        {
            handler.Credentials = credentials;

            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        };

        [Theory]
        [MemberData(nameof(Authentication_TestData))]
        public async Task HttpClientHandler_Authentication_Succeeds(string authenticateHeader, bool result)
        {
            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                string serverResponse = $"HTTP/1.1 401 UnAuthorized\r\nDate: {DateTimeOffset.UtcNow:R}\r\nWWW-Authenticate: {authenticateHeader}\r\nContent-Length: 0\r\n\r\n";
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = result ?
                    LoopbackServer.ReadRequestAndAuthenticateAsync(server, serverResponse, options) :
                    LoopbackServer.ReadRequestAndSendResponseAsync(server, serverResponse, options);
                await TestHelper.WhenAllCompletedOrAnyFailed(_createAndValidateRequest(handler, url, result ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, _credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\"")]
        [InlineData("WWW-Authenticate: Basic realm=\"hello1\"\r\nWWW-Authenticate: Basic realm=\"hello2\"")]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Basic realm=\"hello\"")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\"\r\nWWW-Authenticate: Basic realm=\"hello\"")]
        public async void HttpClientHandler_MultipleAuthenticateHeaders_Succeeds(string authenticateHeader)
        {
            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                string serverResponse = $"HTTP/1.1 401 UnAuthorized\r\nDate: {DateTimeOffset.UtcNow:R}\r\n{authenticateHeader}\r\nContent-Length: 0\r\n\r\n";
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = LoopbackServer.ReadRequestAndAuthenticateAsync(server, serverResponse, options);
                await TestHelper.WhenAllCompletedOrAnyFailed(_createAndValidateRequest(handler, url, HttpStatusCode.OK, _credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("HTTP/1.1 401 UnAuthorized\r\nWWW-Authenticate: Basic realm=\"hello\"\r\nContent-Length: 0\r\n\r\n")]
        [InlineData("HTTP/1.1 401 UnAuthorized\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"testnonce\"\r\nContent-Length: 0\r\n\r\n")]
        public async void HttpClientHandler_IncorrectCredentials_Fails(string serverResponse)
        {
            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = LoopbackServer.ReadRequestAndAuthenticateAsync(server, serverResponse, options);
                await TestHelper.WhenAllCompletedOrAnyFailed(_createAndValidateRequest(handler, url, HttpStatusCode.Unauthorized, new NetworkCredential("wronguser", "wrongpassword")), serverTask);
            }, options);
        }

        public static IEnumerable<object[]> Authentication_TestData()
        {
            yield return new object[] { "Basic realm=\"testrealm\"", true };
            yield return new object[] { "Basic ", true };
            yield return new object[] { "Basic realm=withoutquotes", true };
            yield return new object[] { "Basic something, Digest something", false };

            // Add digest tests fail on CurlHandler.
            if (PlatformDetection.IsWindows)
            {
                yield return new object[] { "Digest realm=\"testrealm\" nonce=\"testnonce\"", false };
                yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}\", qop=auth-int"))}\"", true };
                yield return new object[] { "Digest realm=\"api@example.org\", qop=\"auth\", algorithm=MD5-sess, nonce=\"5TsQWLVdgBdmrQ0XsxbDODV+57QdFR34I9HAbC/RVvkK\", " +
                "opaque=\"HRPCssKJSGjCrkzDg8OhwpzCiGPChXYjwrI2QmXDnsOS\", charset=UTF-8, userhash=true", true };
                yield return new object[] { "Digest realm=\"testrealm1\", nonce=\"testnonce1\" Digest realm=\"testrealm2\", nonce=\"testnonce2\"", false };
                yield return new object[] { $"Basic realm=\"testrealm\", " +
                $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}"))}\", algorithm=MD5", true };
                yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"testnonce\", algorithm=MD5 " +
                $"Basic realm=\"testrealm\"", false };
            }
        }
    }
}
