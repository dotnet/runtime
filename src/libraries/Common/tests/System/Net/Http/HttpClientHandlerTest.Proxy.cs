// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
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

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_Proxy_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Dispose_HandlerWithProxy_ProxyNotDisposed()
        {
            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            var proxy = new TrackDisposalProxy();

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                {
                    handler.UseProxy = true;
                    handler.Proxy = proxy;
                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        Assert.Equal("hello", await client.GetStringAsync(uri));
                    }
                }
            }, async server =>
            {
                await server.HandleRequestAsync(content: "hello");
            });

            Assert.True(proxy.ProxyUsed);
            Assert.False(proxy.Disposed);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1507")]
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
            if (!PlatformDetection.IsWindows &&
                (proxyAuthScheme == AuthenticationSchemes.Negotiate || proxyAuthScheme == AuthenticationSchemes.Ntlm))
            {
                // CI machines don't have GSSAPI module installed and will fail with error from
                // System.Net.Security.NegotiateStreamPal.AcquireCredentialsHandle():
                //        "GSSAPI operation failed with error - An invalid status code was supplied
                //         Configuration file does not specify default realm)."
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

        public static bool IsSocketsHttpHandlerAndRemoteExecutorSupported => !HttpClientHandlerTestBase.IsWinHttpHandler && RemoteExecutor.IsSupported;

        [OuterLoop("Uses external server")]
        [ConditionalFact(nameof(IsSocketsHttpHandlerAndRemoteExecutorSupported))]
        public void Proxy_UseEnvironmentVariableToSetSystemProxy_RequestGoesThruProxy()
        {
            RemoteExecutor.Invoke(async (useVersionString) =>
            {
                var options = new LoopbackProxyServer.Options { AddViaRequestHeader = true };
                using (LoopbackProxyServer proxyServer = LoopbackProxyServer.Create(options))
                {
                    Environment.SetEnvironmentVariable("http_proxy", proxyServer.Uri.AbsoluteUri.ToString());

                    using (HttpClient client = CreateHttpClient(useVersionString))
                    using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        string body = await response.Content.ReadAsStringAsync();
                        Assert.Contains(proxyServer.ViaHeader, body);
                    }
                }
            }, UseVersion.ToString()).Dispose();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1507")]
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

                            // Check last request to proxy server. Handlers that don't use
                            // pre-auth for proxy will make 2 requests.
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        public async Task Proxy_SslProxyUnsupported_Throws()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.Proxy = new WebProxy("https://" + Guid.NewGuid().ToString("N"));

                Type expectedType = IsWinHttpHandler ? typeof(HttpRequestException) : typeof(NotSupportedException);

                await Assert.ThrowsAsync(expectedType, () => client.GetAsync("http://" + Guid.NewGuid().ToString("N")));
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task Proxy_SendSecureRequestThruProxy_ConnectTunnelUsed()
        {
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
            const string expectedUsername = "user";
            const string expectedPassword = "password";
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task MultiProxy_PAC_Failover_Succeeds()
        {
            if (IsWinHttpHandler)
            {
                // PAC-based failover is only supported on Windows/SocketsHttpHandler
                return;
            }

            // Create our failing proxy server.
            // Bind a port to reserve it, but don't start listening yet. The first Connect() should fail and cause a fail-over.
            using Socket failingProxyServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            failingProxyServer.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var failingEndPoint = (IPEndPoint)failingProxyServer.LocalEndPoint;

            using LoopbackProxyServer succeedingProxyServer = LoopbackProxyServer.Create();
            string proxyConfigString = $"{failingEndPoint.Address}:{failingEndPoint.Port} {succeedingProxyServer.Uri.Host}:{succeedingProxyServer.Uri.Port}";

            // Create a WinInetProxyHelper and override its values with our own.
            object winInetProxyHelper = Activator.CreateInstance(typeof(HttpClient).Assembly.GetType("System.Net.Http.WinInetProxyHelper", true), true);
            winInetProxyHelper.GetType().GetField("_autoConfigUrl", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).SetValue(winInetProxyHelper, null);
            winInetProxyHelper.GetType().GetField("_autoDetect", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).SetValue(winInetProxyHelper, false);
            winInetProxyHelper.GetType().GetField("_proxy", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).SetValue(winInetProxyHelper, proxyConfigString);
            winInetProxyHelper.GetType().GetField("_proxyBypass", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).SetValue(winInetProxyHelper, null);

            // Create a HttpWindowsProxy with our custom WinInetProxyHelper.
            IWebProxy httpWindowsProxy = (IWebProxy)Activator.CreateInstance(typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpWindowsProxy", true), Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance, null, new[] { winInetProxyHelper, null }, null);

            Task<bool> nextFailedConnection = null;

            // Run a request with that proxy.
            Task requestTask = LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    using HttpClient client = CreateHttpClient(handler);
                    handler.Proxy = httpWindowsProxy;

                    // First request is expected to hit the failing proxy server, then failover to the succeeding proxy server.
                    Assert.Equal("foo", await client.GetStringAsync(uri));

                    // Second request should start directly at the succeeding proxy server.
                    // So, start listening on our failing proxy server to catch if it tries to connect.
                    failingProxyServer.Listen(1);
                    nextFailedConnection = WaitForNextFailedConnection();
                    Assert.Equal("bar", await client.GetStringAsync(uri));
                },
                async server =>
                {
                    await server.HandleRequestAsync(statusCode: HttpStatusCode.OK, content: "foo");
                    await server.HandleRequestAsync(statusCode: HttpStatusCode.OK, content: "bar");
                });

            // Wait for request to finish.
            await requestTask;

            // Triggers WaitForNextFailedConnection to stop, and then check
            // to ensure we haven't got any further requests against it.
            failingProxyServer.Dispose();
            Assert.False(await nextFailedConnection);

            Assert.Equal(2, succeedingProxyServer.Requests.Count);

            async Task<bool> WaitForNextFailedConnection()
            {
                try
                {
                    (await failingProxyServer.AcceptAsync()).Dispose();
                    return true;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    // Dispose() of the loopback server will cause AcceptAsync() in EstablishConnectionAsync() to abort.
                    return false;
                }
            }
        }

        public static IEnumerable<object[]> BypassedProxies()
        {
            yield return new object[] { null };
            yield return new object[] { new UseSpecifiedUriWebProxy(new Uri($"http://{Guid.NewGuid().ToString().Substring(0, 15)}:12345"), bypass: true) };
        }

        public static IEnumerable<object[]> CredentialsForProxy()
        {
            yield return new object[] { null, false };
            foreach (bool wrapCredsInCache in BoolValues)
            {
                yield return new object[] { new NetworkCredential("username", "password"), wrapCredsInCache };
                yield return new object[] { new NetworkCredential("username", "password", "domain"), wrapCredsInCache };
            }
        }

        private sealed class TrackDisposalProxy : IWebProxy, IDisposable
        {
            public bool Disposed;
            public bool ProxyUsed;

            public void Dispose() => Disposed = true;
            public Uri GetProxy(Uri destination)
            {
                ProxyUsed = true;
                return null;
            }
            public bool IsBypassed(Uri host)
            {
                ProxyUsed = true;
                return true;
            }
            public ICredentials Credentials { get => null; set { } }
        }
    }
}
