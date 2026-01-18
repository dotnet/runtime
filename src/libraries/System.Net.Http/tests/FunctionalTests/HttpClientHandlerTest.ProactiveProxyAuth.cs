// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class ProactiveProxyAuthTest : HttpClientHandlerTestBase
    {
        public ProactiveProxyAuthTest(ITestOutputHelper helper) : base(helper) { }

        /// <summary>
        /// Tests that when proxy credentials are provided, the Proxy-Authorization header
        /// is sent proactively on the first request without waiting for a 407 challenge.
        /// This is important for proxies that don't send 407 responses but instead
        /// drop or reject unauthenticated connections.
        /// </summary>
        [Fact]
        public async Task ProxyAuth_CredentialsProvided_SentProactivelyOnFirstRequest()
        {
            const string username = "testuser";
            const string password = "testpassword";

            // Create a proxy server that does NOT require authentication (AuthenticationSchemes.None)
            // This allows us to verify that credentials are sent proactively even without a 407 challenge
            using LoopbackProxyServer proxyServer = LoopbackProxyServer.Create();

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    handler.Proxy = new WebProxy(proxyServer.Uri)
                    {
                        Credentials = new NetworkCredential(username, password)
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    // Make a request - credentials should be sent proactively
                    HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                    using HttpResponseMessage response = await client.SendAsync(TestAsync, request);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    // Verify that the proxy received the Proxy-Authorization header on the first request
                    Assert.Single(proxyServer.Requests);
                    var proxyRequest = proxyServer.Requests[0];
                    Assert.Equal("Basic", proxyRequest.AuthorizationHeaderValueScheme);
                    Assert.NotNull(proxyRequest.AuthorizationHeaderValueToken);

                    // Verify the credentials are correct (Base64 encoded "testuser:testpassword")
                    string expectedToken = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                    Assert.Equal(expectedToken, proxyRequest.AuthorizationHeaderValueToken);
                },
                async server =>
                {
                    await server.HandleRequestAsync(content: "Success");
                });
        }

        /// <summary>
        /// Tests that proactive proxy auth works for HTTPS CONNECT tunnels.
        /// </summary>
        [Fact]
        public async Task ProxyAuth_HttpsConnect_CredentialsSentProactively()
        {
            const string username = "tunneluser";
            const string password = "tunnelpass";

            using LoopbackProxyServer proxyServer = LoopbackProxyServer.Create();

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                    handler.Proxy = new WebProxy(proxyServer.Uri)
                    {
                        Credentials = new NetworkCredential(username, password)
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                    using HttpResponseMessage response = await client.SendAsync(TestAsync, request);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    // For HTTPS, the proxy receives a CONNECT request
                    Assert.True(proxyServer.Requests.Count >= 1);
                    var connectRequest = proxyServer.Requests[0];

                    // Verify CONNECT request has proactive auth
                    Assert.Equal("Basic", connectRequest.AuthorizationHeaderValueScheme);
                    Assert.NotNull(connectRequest.AuthorizationHeaderValueToken);
                },
                async server =>
                {
                    await server.HandleRequestAsync(content: "Secure Success");
                },
                options: new GenericLoopbackOptions { UseSsl = true });
        }

        /// <summary>
        /// Tests that DefaultNetworkCredentials are NOT sent proactively
        /// (they are only for NTLM/Negotiate which require challenge-response).
        /// </summary>
        [Fact]
        public async Task ProxyAuth_DefaultCredentials_NotSentProactively()
        {
            using LoopbackProxyServer proxyServer = LoopbackProxyServer.Create();

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    handler.Proxy = new WebProxy(proxyServer.Uri)
                    {
                        Credentials = CredentialCache.DefaultNetworkCredentials
                    };

                    using HttpClient client = CreateHttpClient(handler);

                    HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                    using HttpResponseMessage response = await client.SendAsync(TestAsync, request);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    // DefaultNetworkCredentials should NOT result in proactive Basic auth
                    Assert.Single(proxyServer.Requests);
                    var proxyRequest = proxyServer.Requests[0];
                    Assert.Null(proxyRequest.AuthorizationHeaderValueScheme);
                },
                async server =>
                {
                    await server.HandleRequestAsync(content: "Success");
                });
        }
    }

    public sealed class ProactiveProxyAuthTest_Http11 : ProactiveProxyAuthTest
    {
        public ProactiveProxyAuthTest_Http11(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version11;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class ProactiveProxyAuthTest_Http2 : ProactiveProxyAuthTest
    {
        public ProactiveProxyAuthTest_Http2(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }
}
