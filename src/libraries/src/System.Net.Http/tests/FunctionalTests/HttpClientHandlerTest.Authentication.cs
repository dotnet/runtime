// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandler_Authentication_Test : HttpClientHandlerTestBase
    {
        private const string Username = "testusername";
        private const string Password = "testpassword";
        private const string Domain = "testdomain";

        private static readonly NetworkCredential s_credentials = new NetworkCredential(Username, Password, Domain);
        private static readonly NetworkCredential s_credentialsNoDomain = new NetworkCredential(Username, Password);

        private async Task CreateAndValidateRequest(HttpClientHandler handler, Uri url, HttpStatusCode expectedStatusCode, ICredentials credentials)
        {
            handler.Credentials = credentials;

            using (HttpClient client = CreateHttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }
        }

        public HttpClientHandler_Authentication_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [MemberData(nameof(Authentication_TestData))]
        public async Task HttpClientHandler_Authentication_Succeeds(string authenticateHeader, bool result)
        {
            if (PlatformDetection.IsWindowsNanoServer)
            {
                // TODO: #28065: Fix failing authentication test cases on different httpclienthandlers.
                return;
            }

            // Digest authentication does not work with domain credentials on CurlHandler. This is blocked by the behavior of LibCurl.
            NetworkCredential credentials = (IsCurlHandler && authenticateHeader.ToLowerInvariant().Contains("digest")) ?
                s_credentialsNoDomain :
                s_credentials;

            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                string serverAuthenticateHeader = $"WWW-Authenticate: {authenticateHeader}\r\n";
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = result ?
                    server.AcceptConnectionPerformAuthenticationAndCloseAsync(serverAuthenticateHeader) :
                    server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, serverAuthenticateHeader);

                await TestHelper.WhenAllCompletedOrAnyFailedWithTimeout(TestHelper.PassingTestTimeoutMilliseconds,
                    CreateAndValidateRequest(handler, url, result ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello1\"\r\nWWW-Authenticate: Basic realm=\"hello2\"\r\n")]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Basic realm=\"hello\"\r\n")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\n")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello1\", nonce=\"hello\", algorithm=MD5\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\n")]
        public async Task HttpClientHandler_MultipleAuthenticateHeaders_WithSameAuth_Succeeds(string authenticateHeader)
        {
            if (IsWinHttpHandler)
            {
                // TODO: #28065: Fix failing authentication test cases on different httpclienthandlers.
                return;
            }

            await HttpClientHandler_MultipleAuthenticateHeaders_Succeeds(authenticateHeader);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\n")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\nWWW-Authenticate: Basic realm=\"hello\"\r\n")]
        public async Task HttpClientHandler_MultipleAuthenticateHeaders_Succeeds(string authenticateHeader)
        {
            if (PlatformDetection.IsWindowsNanoServer || (IsCurlHandler && authenticateHeader.Contains("Digest")))
            {
                // TODO: #28065: Fix failing authentication test cases on different httpclienthandlers.
                return;
            }

            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = server.AcceptConnectionPerformAuthenticationAndCloseAsync(authenticateHeader);
                await TestHelper.WhenAllCompletedOrAnyFailed(CreateAndValidateRequest(handler, url, HttpStatusCode.OK, s_credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: NTLM\r\n", "Basic", "Negotiate")]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\nWWW-Authenticate: Digest realm=\"hello\", nonce=\"hello\", algorithm=MD5\r\nWWW-Authenticate: NTLM\r\n", "Digest", "Negotiate")]
        public async Task HttpClientHandler_MultipleAuthenticateHeaders_PicksSupported(string authenticateHeader, string supportedAuth, string unsupportedAuth)
        {
            if (PlatformDetection.IsWindowsNanoServer || (IsCurlHandler && authenticateHeader.Contains("Digest")))
            {
                // TODO: #28065: Fix failing authentication test cases on different httpclienthandlers.
                return;
            }

            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.UseDefaultCredentials = false;

                var credentials = new CredentialCache();
                credentials.Add(url, supportedAuth, new NetworkCredential(Username, Password, Domain));
                credentials.Add(url, unsupportedAuth, new NetworkCredential(Username, Password, Domain));

                Task serverTask = server.AcceptConnectionPerformAuthenticationAndCloseAsync(authenticateHeader);
                await TestHelper.WhenAllCompletedOrAnyFailed(CreateAndValidateRequest(handler, url, HttpStatusCode.OK, credentials), serverTask);
            }, options);
        }

        [Theory]
        [InlineData("WWW-Authenticate: Basic realm=\"hello\"\r\n")]
        [InlineData("WWW-Authenticate: Digest realm=\"hello\", nonce=\"testnonce\"\r\n")]
        public async Task HttpClientHandler_IncorrectCredentials_Fails(string authenticateHeader)
        {
            var options = new LoopbackServer.Options { Domain = Domain, Username = Username, Password = Password };
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                Task serverTask = server.AcceptConnectionPerformAuthenticationAndCloseAsync(authenticateHeader);
                await TestHelper.WhenAllCompletedOrAnyFailed(CreateAndValidateRequest(handler, url, HttpStatusCode.Unauthorized, new NetworkCredential("wronguser", "wrongpassword")), serverTask);
            }, options);
        }

        public static IEnumerable<object[]> Authentication_TestData()
        {
            yield return new object[] { "Basic realm=\"testrealm\"", true };
            yield return new object[] { "Basic ", true };
            yield return new object[] { "Basic realm=withoutquotes", true };
            yield return new object[] { "basic ", true };
            yield return new object[] { "bAsiC ", true };
            yield return new object[] { "basic", true };

            yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}\", qop=auth-int, algorithm=MD5"))}\"", true };
            yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}\", qop=auth-int, algorithm=md5"))}\"", true };
            yield return new object[] { $"Basic realm=\"testrealm\", " +
                    $"Digest realm=\"testrealm\", nonce=\"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{DateTimeOffset.UtcNow}:XMh;z+$5|`i6Hx}}"))}\", algorithm=MD5", true };

            if (PlatformDetection.IsNetCore)
            {
                // TODO: #28060: Fix failing authentication test cases on Framework run.
                yield return new object[] { "Basic something, Digest something", false };
                yield return new object[] { $"Digest realm=\"testrealm\", nonce=\"testnonce\", algorithm=MD5 " +
                    $"Basic realm=\"testrealm\"", false };
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Basic")]
        [InlineData("Digest")]
        [InlineData("NTLM")]
        [InlineData("Kerberos")]
        [InlineData("Negotiate")]
        public async Task PreAuthenticate_NoPreviousAuthenticatedRequests_NoCredentialsSent(string credCacheScheme)
        {
            if (IsCurlHandler && credCacheScheme != null)
            {
                // When provided with a credential that targets just one auth scheme,
                // libcurl will often proactively send an auth header.
                return;
            }

            const int NumRequests = 3;
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // for simplicity of not needing to know every handler's pooling policy
                    handler.PreAuthenticate = true;
                    switch (credCacheScheme)
                    {
                        case null:
                            handler.Credentials = s_credentials;
                            break;

                        default:
                            var cc = new CredentialCache();
                            cc.Add(uri, credCacheScheme, s_credentials);
                            handler.Credentials = cc;
                            break;
                    }

                    for (int i = 0; i < NumRequests; i++)
                    {
                        Assert.Equal("hello world", await client.GetStringAsync(uri));
                    }
                }
            },
            async server =>
            {
                for (int i = 0; i < NumRequests; i++)
                {
                    List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world");
                    Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));
                }
            });
        }

        [Theory]
        [InlineData(null, "WWW-Authenticate: Basic realm=\"hello\"\r\n")]
        [InlineData("Basic", "WWW-Authenticate: Basic realm=\"hello\"\r\n")]
        public async Task PreAuthenticate_FirstRequestNoHeaderAndAuthenticates_SecondRequestPreauthenticates(string credCacheScheme, string authResponse)
        {
            if (IsCurlHandler && credCacheScheme != null)
            {
                // When provided with a credential that targets just one auth scheme,
                // libcurl will often proactively send an auth header.
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // for simplicity of not needing to know every handler's pooling policy
                    handler.PreAuthenticate = true;
                    switch (credCacheScheme)
                    {
                        case null:
                            handler.Credentials = s_credentials;
                            break;

                        default:
                            var cc = new CredentialCache();
                            cc.Add(uri, credCacheScheme, s_credentials);
                            handler.Credentials = cc;
                            break;
                    }

                    Assert.Equal("hello world", await client.GetStringAsync(uri));
                    Assert.Equal("hello world", await client.GetStringAsync(uri));
                }
            },
            async server =>
            {
                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, authResponse);
                Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));

                for (int i = 0; i < 2; i++)
                {
                    headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world");
                    Assert.Contains(headers, header => header.Contains("Authorization"));
                }
            });
        }

        // InlineDatas for all values that pass on WinHttpHandler, which is the most restrictive.
        // Uses numerical values for values named in .NET Core and not in .NET Framework.
        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.NonAuthoritativeInformation)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.ResetContent)]
        [InlineData(HttpStatusCode.PartialContent)]
        [InlineData((HttpStatusCode)207)] // MultiStatus
        [InlineData((HttpStatusCode)208)] // AlreadyReported
        [InlineData((HttpStatusCode)226)] // IMUsed
        [InlineData(HttpStatusCode.Ambiguous)]
        [InlineData(HttpStatusCode.NotModified)]
        [InlineData(HttpStatusCode.UseProxy)]
        [InlineData(HttpStatusCode.Unused)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.PaymentRequired)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.MethodNotAllowed)]
        [InlineData(HttpStatusCode.NotAcceptable)]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.Gone)]
        [InlineData(HttpStatusCode.LengthRequired)]
        [InlineData(HttpStatusCode.PreconditionFailed)]
        [InlineData(HttpStatusCode.RequestEntityTooLarge)]
        [InlineData(HttpStatusCode.RequestUriTooLong)]
        [InlineData(HttpStatusCode.UnsupportedMediaType)]
        [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable)]
        [InlineData(HttpStatusCode.ExpectationFailed)]
        [InlineData((HttpStatusCode)421)] // MisdirectedRequest
        [InlineData((HttpStatusCode)422)] // UnprocessableEntity
        [InlineData((HttpStatusCode)423)] // Locked
        [InlineData((HttpStatusCode)424)] // FailedDependency
        [InlineData(HttpStatusCode.UpgradeRequired)]
        [InlineData((HttpStatusCode)428)] // PreconditionRequired
        [InlineData((HttpStatusCode)429)] // TooManyRequests
        [InlineData((HttpStatusCode)431)] // RequestHeaderFieldsTooLarge
        [InlineData((HttpStatusCode)451)] // UnavailableForLegalReasons
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.NotImplemented)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.HttpVersionNotSupported)]
        [InlineData((HttpStatusCode)506)] // VariantAlsoNegotiates
        [InlineData((HttpStatusCode)507)] // InsufficientStorage
        [InlineData((HttpStatusCode)508)] // LoopDetected
        [InlineData((HttpStatusCode)510)] // NotExtended
        [InlineData((HttpStatusCode)511)] // NetworkAuthenticationRequired
        public async Task PreAuthenticate_FirstRequestNoHeader_SecondRequestVariousStatusCodes_ThirdRequestPreauthenticates(HttpStatusCode statusCode)
        {
            const string AuthResponse = "WWW-Authenticate: Basic realm=\"hello\"\r\n";

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // for simplicity of not needing to know every handler's pooling policy
                    handler.PreAuthenticate = true;
                    handler.Credentials = s_credentials;
                    client.DefaultRequestHeaders.ExpectContinue = false;

                    using (HttpResponseMessage resp = await client.GetAsync(uri))
                    {
                        Assert.Equal(statusCode, resp.StatusCode);
                    }
                    Assert.Equal("hello world", await client.GetStringAsync(uri));
                }
            },
            async server =>
            {
                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, AuthResponse);
                Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));

                headers = await server.AcceptConnectionSendResponseAndCloseAsync(statusCode);
                Assert.Contains(headers, header => header.Contains("Authorization"));

                headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK, content: "hello world");
                Assert.Contains(headers, header => header.Contains("Authorization"));
            });
        }

        [Theory]
        [InlineData("/something/hello.html", "/something/hello.html", true)]
        [InlineData("/something/hello.html", "/something/world.html", true)]
        [InlineData("/something/hello.html", "/something/", true)]
        [InlineData("/something/hello.html", "/", false)]
        [InlineData("/something/hello.html", "/hello.html", false)]
        [InlineData("/something/hello.html", "/world.html", false)]
        [InlineData("/something/hello.html", "/another/", false)]
        [InlineData("/something/hello.html", "/another/hello.html", false)]
        public async Task PreAuthenticate_AuthenticatedUrl_ThenTryDifferentUrl_SendsAuthHeaderOnlyIfPrefixMatches(
            string originalRelativeUri, string secondRelativeUri, bool expectedAuthHeader)
        {
            const string AuthResponse = "WWW-Authenticate: Basic realm=\"hello\"\r\n";

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // for simplicity of not needing to know every handler's pooling policy
                    handler.PreAuthenticate = true;
                    handler.Credentials = s_credentials;

                    Assert.Equal("hello world 1", await client.GetStringAsync(new Uri(uri, originalRelativeUri)));
                    Assert.Equal("hello world 2", await client.GetStringAsync(new Uri(uri, secondRelativeUri)));
                }
            },
            async server =>
            {
                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, AuthResponse);
                Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));

                headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world 1");
                Assert.Contains(headers, header => header.Contains("Authorization"));

                headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world 2");
                if (expectedAuthHeader)
                {
                    Assert.Contains(headers, header => header.Contains("Authorization"));
                }
                else
                {
                    Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));
                }
            });
        }

        [Fact]
        public async Task PreAuthenticate_SuccessfulBasicButThenFails_DoesntLoopInfinitely()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // for simplicity of not needing to know every handler's pooling policy
                    handler.PreAuthenticate = true;
                    handler.Credentials = s_credentials;

                    // First two requests: initially without auth header, then with
                    Assert.Equal("hello world", await client.GetStringAsync(uri));

                    // Attempt preauth, and when that fails, give up.
                    using (HttpResponseMessage resp = await client.GetAsync(uri))
                    {
                        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
                    }
                }
            },
            async server =>
            {
                // First request, no auth header, challenge Basic
                List<string> headers = headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "WWW-Authenticate: Basic realm=\"hello\"\r\n");
                Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));

                // Second request, contains Basic auth header
                headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world");
                Assert.Contains(headers, header => header.Contains("Authorization"));

                // Third request, contains Basic auth header but challenges anyway
                headers = headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "WWW-Authenticate: Basic realm=\"hello\"\r\n");
                Assert.Contains(headers, header => header.Contains("Authorization"));

                if (IsNetfxHandler)
                {
                    // For some reason, netfx tries one more time.
                    headers = headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "WWW-Authenticate: Basic realm=\"hello\"\r\n");
                    Assert.Contains(headers, header => header.Contains("Authorization"));
                }
            });
        }

        [Fact]
        public async Task PreAuthenticate_SuccessfulBasic_ThenDigestChallenged()
        {
            if (IsWinHttpHandler)
            {
                // WinHttpHandler fails with Unauthorized after the basic preauth fails.
                return;
            }

            if (IsCurlHandler)
            {
                // When provided with a credential that targets just one auth scheme,
                // libcurl will often proactively send an auth header.
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // for simplicity of not needing to know every handler's pooling policy
                    handler.PreAuthenticate = true;
                    handler.Credentials = s_credentials;

                    Assert.Equal("hello world", await client.GetStringAsync(uri));
                    Assert.Equal("hello world", await client.GetStringAsync(uri));
                }
            },
            async server =>
            {
                // First request, no auth header, challenge Basic
                List<string> headers = headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "WWW-Authenticate: Basic realm=\"hello\"\r\n");
                Assert.All(headers, header => Assert.DoesNotContain("Authorization", header));

                // Second request, contains Basic auth header, success
                headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world");
                Assert.Contains(headers, header => header.Contains("Authorization"));

                // Third request, contains Basic auth header, challenge digest
                headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "WWW-Authenticate: Digest realm=\"hello\", nonce=\"testnonce\"\r\n");
                Assert.Contains(headers, header => header.Contains("Authorization: Basic"));

                // Fourth request, contains Digest auth header, success
                headers = await server.AcceptConnectionSendResponseAndCloseAsync(content: "hello world");
                Assert.Contains(headers, header => header.Contains("Authorization: Digest"));
            });
        }

        public static IEnumerable<object[]> ServerUsesWindowsAuthentication_MemberData()
        {
            string server = Configuration.Http.WindowsServerHttpHost;
            string authEndPoint = "showidentity.ashx";

            yield return new object[] { $"http://{server}/test/auth/ntlm/{authEndPoint}", false };
            yield return new object[] { $"https://{server}/test/auth/ntlm/{authEndPoint}", false };

            // Curlhandler (due to libcurl bug) cannot do Negotiate (SPNEGO) Kerberos to NTLM fallback.
            yield return new object[] { $"http://{server}/test/auth/negotiate/{authEndPoint}", true };
            yield return new object[] { $"https://{server}/test/auth/negotiate/{authEndPoint}", true };

            // Server requires TLS channel binding token (cbt) with NTLM authentication.
            // CurlHandler (due to libcurl bug) cannot do NTLM authentication with cbt.
            yield return new object[] { $"https://{server}/test/auth/ntlm-epa/{authEndPoint}", true };
        }

        private static bool IsNtlmInstalled => Capability.IsNtlmInstalled();
        private static bool IsWindowsServerAvailable => !string.IsNullOrEmpty(Configuration.Http.WindowsServerHttpHost);
        private static bool IsDomainJoinedServerAvailable => !string.IsNullOrEmpty(Configuration.Http.DomainJoinedHttpHost);
        private static NetworkCredential DomainCredential = new NetworkCredential(
                    Configuration.Security.ActiveDirectoryUserName,
                    Configuration.Security.ActiveDirectoryUserPassword,
                    Configuration.Security.ActiveDirectoryName);

        public static IEnumerable<object[]> EchoServersData()
        {
            foreach (Uri serverUri in Configuration.Http.EchoServerList)
            {
                yield return new object[] { serverUri };
            }
        }

        [MemberData(nameof(EchoServersData))]
        [ConditionalTheory(nameof(IsDomainJoinedServerAvailable))]
        public async Task Proxy_DomainJoinedProxyServerUsesKerberos_Success(Uri server)
        {
            // We skip the test unless it is running on a Windows client machine. That is because only Windows
            // automatically registers an SPN for HTTP/<hostname> of the machine. This will enable Kerberos to properly
            // work with the loopback proxy server.
            if (!PlatformDetection.IsWindows || !PlatformDetection.IsNotWindowsNanoServer)
            {
                throw new SkipTestException("Test can only run on domain joined Windows client machine");
            }

            var options = new LoopbackProxyServer.Options { AuthenticationSchemes = AuthenticationSchemes.Negotiate };
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    // Use 'localhost' DNS name for loopback proxy server (instead of IP address) so that the SPN will
                    // get calculated properly to use Kerberos.
                    _output.WriteLine(proxyServer.Uri.AbsoluteUri.ToString());
                    handler.Proxy = new WebProxy("localhost", proxyServer.Uri.Port) { Credentials = DomainCredential };

                    using (HttpResponseMessage response = await client.GetAsync(server))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                        int requestCount = proxyServer.Requests.Count;

                        // We expect 2 requests to the proxy server. One without the 'Proxy-Authorization' header and
                        // one with the header.
                        Assert.Equal(2, requestCount);
                        Assert.Equal("Negotiate", proxyServer.Requests[requestCount - 1].AuthorizationHeaderValueScheme);

                        // Base64 tokens that use SPNEGO protocol start with 'Y'. NTLM tokens start with 'T'.
                        Assert.Equal('Y', proxyServer.Requests[requestCount - 1].AuthorizationHeaderValueToken[0]);
                    }
                }
            }
        }

        [ConditionalFact(nameof(IsDomainJoinedServerAvailable))]
        public async Task Credentials_DomainJoinedServerUsesKerberos_Success()
        {
            if (IsCurlHandler)
            {
                throw new SkipTestException("Skipping test on CurlHandler (libCurl)");
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Credentials = DomainCredential;

                string server = $"http://{Configuration.Http.DomainJoinedHttpHost}/test/auth/kerberos/showidentity.ashx";
                using (HttpResponseMessage response = await client.GetAsync(server))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string body = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(body);
                }
            }
        }

        [ConditionalFact(nameof(IsDomainJoinedServerAvailable))]
        public async Task Credentials_DomainJoinedServerUsesKerberos_UseIpAddressAndHostHeader_Success()
        {
            if (IsCurlHandler || IsWinHttpHandler)
            {
                throw new SkipTestException("Skipping test on platform handlers (CurlHandler, WinHttpHandler)");
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Credentials = DomainCredential;

                IPAddress[] addresses = Dns.GetHostAddresses(Configuration.Http.DomainJoinedHttpHost);
                IPAddress hostIP = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).Select(a => a).First();

                var request = new HttpRequestMessage();
                request.RequestUri = new Uri($"http://{hostIP}/test/auth/kerberos/showidentity.ashx");
                request.Headers.Host = Configuration.Http.DomainJoinedHttpHost;
                _output.WriteLine(request.RequestUri.AbsoluteUri.ToString());
                _output.WriteLine($"Host: {request.Headers.Host}");

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string body = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(body);
                }
            }
        }

        [ConditionalTheory(nameof(IsNtlmInstalled), nameof(IsWindowsServerAvailable))]
        [MemberData(nameof(ServerUsesWindowsAuthentication_MemberData))]
        public async Task Credentials_ServerUsesWindowsAuthentication_Success(string server, bool skipOnCurlHandler)
        {
            if (IsCurlHandler && skipOnCurlHandler)
            {
                throw new SkipTestException("CurlHandler (libCurl) doesn't handle Negotiate with NTLM fallback nor CBT");
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Credentials = new NetworkCredential(
                    Configuration.Security.WindowsServerUserName,
                    Configuration.Security.WindowsServerUserPassword);

                using (HttpResponseMessage response = await client.GetAsync(server))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string body = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(body);
                }
            }
        }

        [ConditionalTheory(nameof(IsNtlmInstalled))]
        [InlineData("NTLM")]
        [InlineData("Negotiate")]
        public async Task Credentials_ServerChallengesWithWindowsAuth_ClientSendsWindowsAuthHeader(string authScheme)
        {
            if (authScheme == "Negotiate" && IsCurlHandler)
            {
                throw new SkipTestException("CurlHandler (libCurl) doesn't handle Negotiate with NTLM fallback");
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClientHandler handler = CreateHttpClientHandler())
                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        handler.Credentials = new NetworkCredential("username", "password");
                        await client.GetAsync(uri);
                    }
                },
                async server =>
                {
                    var responseHeader = new HttpHeaderData[] { new HttpHeaderData("Www-authenticate", authScheme) };
                    HttpRequestData requestData = await server.HandleRequestAsync(
                        HttpStatusCode.Unauthorized, responseHeader);
                    Assert.Equal(0, requestData.GetHeaderValueCount("Authorization"));

                    requestData = await server.HandleRequestAsync();
                    string authHeaderValue = requestData.GetSingleHeaderValue("Authorization");
                    Assert.Contains(authScheme, authHeaderValue);
                    _output.WriteLine(authHeaderValue);
               });
        }
    }
}
