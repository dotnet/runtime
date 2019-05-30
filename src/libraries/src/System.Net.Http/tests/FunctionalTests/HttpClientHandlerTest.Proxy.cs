// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.DotNet.RemoteExecutor;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
    public abstract class HttpClientHandler_Proxy_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }

        [ActiveIssue(32809)]
        [OuterLoop("Uses external server")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(AuthenticationSchemes.Ntlm, true, false)]
        [InlineData(AuthenticationSchemes.Negotiate, true, false)]
        [InlineData(AuthenticationSchemes.Basic, false, false)]
        [InlineData(AuthenticationSchemes.Basic, true, false)]
        [InlineData(AuthenticationSchemes.Digest, false, false)]
        [InlineData(AuthenticationSchemes.Digest, true, false)]
        [InlineData(AuthenticationSchemes.Ntlm, false, false)]
        [InlineData(AuthenticationSchemes.Negotiate, false, false)]
        [InlineData(AuthenticationSchemes.Basic, false, true)]
        [InlineData(AuthenticationSchemes.Basic, true, true)]
        [InlineData(AuthenticationSchemes.Digest, false, true)]
        [InlineData(AuthenticationSchemes.Digest, true, true)]
        public async Task AuthProxy__ValidCreds_ProxySendsRequestToServer(
            AuthenticationSchemes proxyAuthScheme,
            bool secureServer,
            bool proxyClosesConnectionAfterFirst407Response)
        {
            if (PlatformDetection.IsFedora && IsCurlHandler)
            {
                // CurlHandler seems unstable on Fedora26 and returns error
                // "System.Net.Http.CurlException : Failure when receiving data from the peer".
                return;
            }

            if (PlatformDetection.IsWindowsNanoServer && IsWinHttpHandler && proxyAuthScheme == AuthenticationSchemes.Digest)
            {
                // WinHTTP doesn't support Digest very well and is disabled on Nano.
                return;
            }

            if (!PlatformDetection.IsWindows &&
                (proxyAuthScheme == AuthenticationSchemes.Negotiate || proxyAuthScheme == AuthenticationSchemes.Ntlm))
            {
                // CI machines don't have GSSAPI module installed and will fail with error from
                // System.Net.Security.NegotiateStreamPal.AcquireCredentialsHandle():
                //        "GSSAPI operation failed with error - An invalid status code was supplied
                //         Configuration file does not specify default realm)."
                return;
            }

            if (IsCurlHandler && proxyAuthScheme != AuthenticationSchemes.Basic)
            {
                // Issue #27870 curl HttpHandler can only do basic auth to proxy.
                return;
            }

            Uri serverUri = secureServer ? Configuration.Http.SecureRemoteEchoServer : Configuration.Http.RemoteEchoServer;

            var options = new LoopbackProxyServer.Options
                { AuthenticationSchemes = proxyAuthScheme,
                  ConnectionCloseAfter407 = proxyClosesConnectionAfterFirst407Response
                };
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyServer.Uri);
                    handler.Proxy.Credentials = new NetworkCredential("username", "password");
                    using (HttpResponseMessage response = await client.GetAsync(serverUri))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        TestHelper.VerifyResponseBody(
                            await response.Content.ReadAsStringAsync(),
                            response.Content.Headers.ContentMD5,
                            false,
                            null);                        
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [ConditionalFact]
        public void Proxy_UseEnvironmentVariableToSetSystemProxy_RequestGoesThruProxy()
        {
            if (!UseSocketsHttpHandler)
            {
                throw new SkipTestException("Test needs SocketsHttpHandler");
            }

            RemoteExecutor.Invoke(async (useSocketsHttpHandlerString, useHttp2String) =>
            {
                var options = new LoopbackProxyServer.Options { AddViaRequestHeader = true };
                using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
                {
                    Environment.SetEnvironmentVariable("http_proxy", proxyServer.Uri.AbsoluteUri.ToString());

                    using (HttpClient client = CreateHttpClient(useSocketsHttpHandlerString, useHttp2String))
                    using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        string body = await response.Content.ReadAsStringAsync();
                        Assert.Contains(proxyServer.ViaHeader, body);
                    }

                    return RemoteExecutor.SuccessExitCode;
                }
            }, UseSocketsHttpHandler.ToString(), UseHttp2.ToString()).Dispose();
        }

        [ActiveIssue(32809)]
        [OuterLoop("Uses external server")]
        [Theory]
        [MemberData(nameof(CredentialsForProxy))]
        public async Task Proxy_BypassFalse_GetRequestGoesThroughCustomProxy(ICredentials creds, bool wrapCredsInCache)
        {
            var options = new LoopbackProxyServer.Options
                { AuthenticationSchemes = creds != null ? AuthenticationSchemes.Basic : AuthenticationSchemes.None
                };
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
            {
                const string BasicAuth = "Basic";
                if (wrapCredsInCache)
                {
                    Assert.IsAssignableFrom<NetworkCredential>(creds);
                    var cache = new CredentialCache();
                    cache.Add(proxyServer.Uri, BasicAuth, (NetworkCredential)creds);
                    creds = cache;
                }

                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyServer.Uri) { Credentials = creds };

                    using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        TestHelper.VerifyResponseBody(
                            await response.Content.ReadAsStringAsync(),
                            response.Content.Headers.ContentMD5,
                            false,
                            null);

                        if (options.AuthenticationSchemes != AuthenticationSchemes.None)
                        {
                            NetworkCredential nc = creds?.GetCredential(proxyServer.Uri, BasicAuth);
                            Assert.NotNull(nc);
                            string expectedAuth =
                                string.IsNullOrEmpty(nc.Domain) ? $"{nc.UserName}:{nc.Password}" :
                                    $"{nc.Domain}\\{nc.UserName}:{nc.Password}";
                            _output.WriteLine($"expectedAuth={expectedAuth}");
                            string expectedAuthHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedAuth));

                            // Check last request to proxy server. CurlHandler will use pre-auth for Basic proxy auth,
                            // so there might only be 1 request received by the proxy server. Other handlers won't use
                            // pre-auth for proxy so there would be 2 requests.
                            int requestCount = proxyServer.Requests.Count;
                            _output.WriteLine($"proxyServer.Requests.Count={requestCount}");
                            Assert.Equal(BasicAuth, proxyServer.Requests[requestCount - 1].AuthorizationHeaderValueScheme);
                            Assert.Equal(expectedAuthHash, proxyServer.Requests[requestCount - 1].AuthorizationHeaderValueToken);
                        }
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [MemberData(nameof(BypassedProxies))]
        public async Task Proxy_BypassTrue_GetRequestDoesntGoesThroughCustomProxy(IWebProxy proxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = proxy;
            using (HttpClient client = CreateHttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
            {
                TestHelper.VerifyResponseBody(
                    await response.Content.ReadAsStringAsync(),
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task Proxy_HaveNoCredsAndUseAuthenticatedCustomProxy_ProxyAuthenticationRequiredStatusCode()
        {
            var options = new LoopbackProxyServer.Options { AuthenticationSchemes = AuthenticationSchemes.Basic };
            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Proxy = new WebProxy(proxyServer.Uri);
                using (HttpClient client = CreateHttpClient(handler))
                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        public async Task Proxy_SslProxyUnsupported_Throws()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Proxy = new WebProxy("https://" + Guid.NewGuid().ToString("N"));

                Type expectedType = IsNetfxHandler || UseSocketsHttpHandler ?
                    typeof(NotSupportedException) :
                    typeof(HttpRequestException);

                await Assert.ThrowsAsync(expectedType, () => client.GetAsync("http://" + Guid.NewGuid().ToString("N")));
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task Proxy_SendSecureRequestThruProxy_ConnectTunnelUsed()
        {
            if (PlatformDetection.IsFedora && IsCurlHandler)
            {
                // CurlHandler seems unstable on Fedora26 and returns error
                // "System.Net.Http.CurlException : Failure when receiving data from the peer".
                return;
            }

            using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create())
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Proxy = new WebProxy(proxyServer.Uri);
                using (HttpClient client = CreateHttpClient(handler))
                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.SecureRemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    _output.WriteLine($"Proxy request line: {proxyServer.Requests[0].RequestLine}");
                    Assert.Contains("CONNECT", proxyServer.Requests[0].RequestLine);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public async Task ProxyAuth_Digest_Succeeds()
        {
            if (IsCurlHandler)
            {
                // Issue #27870 CurlHandler can only do basic auth to proxy.
                return;
            }

            const string expectedUsername = "testusername";
            const string expectedPassword = "testpassword";
            const string authHeader = "Proxy-Authenticate: Digest realm=\"NetCore\", nonce=\"PwOnWgAAAAAAjnbW438AAJSQi1kAAAAA\", qop=\"auth\", stale=false\r\n";
            LoopbackServer.Options options = new LoopbackServer.Options { IsProxy = true, Username = expectedUsername, Password = expectedPassword };
            var proxyCreds = new NetworkCredential(expectedUsername, expectedPassword);

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUrl) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyUrl) { Credentials = proxyCreds };

                    // URL does not matter. We will get response from "proxy" code below.
                    Task<HttpResponseMessage> clientTask = client.GetAsync($"http://notarealserver.com/");

                    //  Send Digest challenge.
                    Task<List<string>> serverTask = proxyServer.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.ProxyAuthenticationRequired, authHeader);
                    if (clientTask == await Task.WhenAny(clientTask, serverTask).TimeoutAfter(TestHelper.PassingTestTimeoutMilliseconds))
                    {
                        // Client task shouldn't have completed successfully; propagate failure.
                        Assert.NotEqual(TaskStatus.RanToCompletion, clientTask.Status);
                        await clientTask;
                    }

                    // Verify user & password.
                    serverTask = proxyServer.AcceptConnectionPerformAuthenticationAndCloseAsync("");
                    await TaskTimeoutExtensions.WhenAllOrAnyFailed(new Task[] { clientTask, serverTask }, TestHelper.PassingTestTimeoutMilliseconds);

                    Assert.Equal(HttpStatusCode.OK, clientTask.Result.StatusCode);
                }
            }, options);

        }

        public static IEnumerable<object[]> BypassedProxies()
        {
            yield return new object[] { null };
            yield return new object[] { new UseSpecifiedUriWebProxy(new Uri($"http://{Guid.NewGuid().ToString().Substring(0, 15)}:12345"), bypass: true) };
        }

        public static IEnumerable<object[]> CredentialsForProxy()
        {
            yield return new object[] { null, false };
            foreach (bool wrapCredsInCache in new[] { true, false })
            {
                yield return new object[] { new NetworkCredential("username", "password"), wrapCredsInCache };
                yield return new object[] { new NetworkCredential("username", "password", "domain"), wrapCredsInCache };
            }
        }
    }
}
