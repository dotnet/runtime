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
            if (IsCurlHandler && authenticateHeader.Contains("Digest"))
            {
                // TODO: #27113: Fix failing authentication test cases on different httpclienthandlers.
                return;
            }

            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                string serverAuthenticateHeader = $"WWW-Authenticate: {authenticateHeader}";
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = result ?
                    server.AcceptConnectionPerformAuthenticationAndCloseAsync(serverAuthenticateHeader) :
                    server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, serverAuthenticateHeader);

                await TestHelper.WhenAllCompletedOrAnyFailedWithTimeout(TestHelper.PassingTestTimeoutMilliseconds,
                    _createAndValidateRequest(handler, url, result ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, _credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5")]
        [InlineData("WWW-Authenticate: Basic realm=\"hello1\"\r\nWWW-Authenticate: Basic realm=\"hello2\"")]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Basic realm=\"hello\"")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\nWWW-Authenticate: Basic realm=\"hello\"")]
        public async void HttpClientHandler_MultipleAuthenticateHeaders_Succeeds(string authenticateHeader)
        {
            if (IsCurlHandler && authenticateHeader.Contains("Digest"))
            {
                // TODO: #27113: Fix failing authentication test cases on different httpclienthandlers.
                return;
            }

            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = server.AcceptConnectionPerformAuthenticationAndCloseAsync(authenticateHeader);
                await TestHelper.WhenAllCompletedOrAnyFailed(_createAndValidateRequest(handler, url, HttpStatusCode.OK, _credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello\", nonce=\"testnonce\"")]
        public async void HttpClientHandler_IncorrectCredentials_Fails(string authenticateHeader)
        {
            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = server.AcceptConnectionPerformAuthenticationAndCloseAsync(authenticateHeader);
                await TestHelper.WhenAllCompletedOrAnyFailed(_createAndValidateRequest(handler, url, HttpStatusCode.Unauthorized, new NetworkCredential("wronguser", "wrongpassword")), serverTask);
            }, options);
        }

        public static IEnumerable<object[]> Authentication_TestData()
        {
            yield return new object[] { "Basic realm=\"testrealm\"", true };
            yield return new object[] { "Basic ", true };
            yield return new object[] { "Basic realm=withoutquotes", true };

            // Add digest tests fail on CurlHandler.
            // TODO: #27113: Fix failing authentication test cases on different httpclienthandlers.
            yield return new object[] { "Digest realm=\"testrealm\" nonce=\"testnonce\"", false };
            yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}\", qop=auth-int, algorithm=MD5"))}\"", true };
            yield return new object[] { "Digest realm=\"api@example.org\", qop=\"auth\", algorithm=MD5-sess, nonce=\"5TsQWLVdgBdmrQ0XsxbDODV+57QdFR34I9HAbC/RVvkK\", " +
                    "opaque=\"HRPCssKJSGjCrkzDg8OhwpzCiGPChXYjwrI2QmXDnsOS\", charset=UTF-8, userhash=true", true };
            yield return new object[] { $"Basic realm=\"testrealm\", " +
                    $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}"))}\", algorithm=MD5", true };

            if (PlatformDetection.IsNetCore)
            {
                // These fail on full framework runs.
                // TODO: #27113: Fix failing authentication test cases on different httpclienthandlers.
                yield return new object[] { "Digest realm=\"testrealm1\", nonce=\"testnonce1\" Digest realm=\"testrealm2\", nonce=\"testnonce2\"", false };
                yield return new object[] { "Basic something, Digest something", false };
                yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"testnonce\", algorithm=MD5 " +
                    $"Basic realm=\"testrealm\"", false };
            }
        }
    }
}
