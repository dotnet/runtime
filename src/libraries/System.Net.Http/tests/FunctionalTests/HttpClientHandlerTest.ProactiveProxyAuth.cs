// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class ProactiveProxyAuthTest : HttpClientHandlerTestBase
    {
        public ProactiveProxyAuthTest(ITestOutputHelper helper) : base(helper) { }

        private const string ProxyPreAuthEnvVar = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_PROXYPREAUTHENTICATE";

        /// <summary>
        /// Tests that when proxy credentials are provided and the opt-in switch is enabled,
        /// the Proxy-Authorization header is sent proactively on the first request without
        /// waiting for a 407 challenge.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ProxyAuth_CredentialsProvided_SentProactivelyOnFirstRequest()
        {
            const string ExpectedUsername = "testuser";
            const string ExpectedPassword = "testpassword";

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                var psi = new ProcessStartInfo();
                psi.Environment.Add("http_proxy", $"http://{proxyUri.Host}:{proxyUri.Port}");
                psi.Environment.Add(ProxyPreAuthEnvVar, "1");

                Task serverTask = proxyServer.AcceptConnectionAsync(async connection =>
                {
                    List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Verify the first request has the Proxy-Authorization header (proactive auth)
                    string? authHeader = null;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                        {
                            authHeader = line;
                            break;
                        }
                    }

                    Assert.NotNull(authHeader);
                    Assert.Contains("Basic", authHeader);

                    // Verify the credentials are correct
                    string expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ExpectedUsername}:{ExpectedPassword}"));
                    Assert.Contains(expectedToken, authHeader);

                    await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                });

                await RemoteExecutor.Invoke(async (username, password, useVersionString) =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                    handler.DefaultProxyCredentials = new NetworkCredential(username, password);

                    using HttpClient client = CreateHttpClient(handler, useVersionString);
                    using HttpResponseMessage response = await client.GetAsync("http://destination.test/");

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, ExpectedUsername, ExpectedPassword, UseVersion.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                await serverTask;
            });
        }

        /// <summary>
        /// Tests that credentials embedded in the proxy URL environment variable are sent proactively.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ProxyAuth_CredentialsInEnvironmentUrl_SentProactively()
        {
            const string ExpectedUsername = "envuser";
            const string ExpectedPassword = "envpass";

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                var psi = new ProcessStartInfo();
                // Credentials embedded in the proxy URL (common pattern for HTTP_PROXY/HTTPS_PROXY)
                psi.Environment.Add("http_proxy", $"http://{ExpectedUsername}:{ExpectedPassword}@{proxyUri.Host}:{proxyUri.Port}");
                psi.Environment.Add(ProxyPreAuthEnvVar, "1");

                Task serverTask = proxyServer.AcceptConnectionAsync(async connection =>
                {
                    List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Verify the first request has the Proxy-Authorization header
                    string? authHeader = null;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                        {
                            authHeader = line;
                            break;
                        }
                    }

                    Assert.NotNull(authHeader);
                    Assert.Contains("Basic", authHeader);

                    // Verify the credentials are correct
                    string expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ExpectedUsername}:{ExpectedPassword}"));
                    Assert.Contains(expectedToken, authHeader);

                    await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                });

                await RemoteExecutor.Invoke(async (useVersionString) =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                    using HttpClient client = CreateHttpClient(handler, useVersionString);
                    using HttpResponseMessage response = await client.GetAsync("http://destination.test/");

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, UseVersion.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                await serverTask;
            });
        }

        /// <summary>
        /// Tests that without the opt-in switch, credentials are NOT sent proactively
        /// (default RFC-compliant behavior).
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ProxyAuth_WithoutOptIn_NotSentProactively()
        {
            const string ExpectedUsername = "testuser";
            const string ExpectedPassword = "testpassword";

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                var psi = new ProcessStartInfo();
                psi.Environment.Add("http_proxy", $"http://{proxyUri.Host}:{proxyUri.Port}");
                // NOT setting ProxyPreAuthEnvVar - default behavior

                Task serverTask = proxyServer.AcceptConnectionAsync(async connection =>
                {
                    List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Verify the first request does NOT have the Proxy-Authorization header
                    foreach (string line in lines)
                    {
                        Assert.False(line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase),
                            "First request should not have Proxy-Authorization header without opt-in");
                    }

                    // Send 407 challenge
                    await connection.SendResponseAsync(HttpStatusCode.ProxyAuthenticationRequired,
                        "Proxy-Authenticate: Basic realm=\"Test\"\r\n").ConfigureAwait(false);

                    // Read the retry request with credentials
                    lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Now it should have credentials
                    string? authHeader = null;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                        {
                            authHeader = line;
                            break;
                        }
                    }
                    Assert.NotNull(authHeader);

                    await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                });

                await RemoteExecutor.Invoke(async (username, password, useVersionString) =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                    handler.DefaultProxyCredentials = new NetworkCredential(username, password);

                    using HttpClient client = CreateHttpClient(handler, useVersionString);
                    using HttpResponseMessage response = await client.GetAsync("http://destination.test/");

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, ExpectedUsername, ExpectedPassword, UseVersion.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                await serverTask;
            });
        }

        /// <summary>
        /// Tests that DefaultNetworkCredentials are NOT sent proactively even with the opt-in,
        /// as they are only for NTLM/Negotiate which require challenge-response.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ProxyAuth_DefaultCredentials_NotSentProactively()
        {
            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                var psi = new ProcessStartInfo();
                psi.Environment.Add("http_proxy", $"http://{proxyUri.Host}:{proxyUri.Port}");
                psi.Environment.Add(ProxyPreAuthEnvVar, "1");

                Task serverTask = proxyServer.AcceptConnectionAsync(async connection =>
                {
                    List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Verify the first request does NOT have the Proxy-Authorization header
                    // (DefaultNetworkCredentials should not trigger proactive auth)
                    foreach (string line in lines)
                    {
                        Assert.False(line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase),
                            "DefaultNetworkCredentials should not trigger proactive Basic auth");
                    }

                    await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                });

                await RemoteExecutor.Invoke(async (useVersionString) =>
                {
                    using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                    handler.DefaultProxyCredentials = CredentialCache.DefaultNetworkCredentials;

                    using HttpClient client = CreateHttpClient(handler, useVersionString);
                    using HttpResponseMessage response = await client.GetAsync("http://destination.test/");

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, UseVersion.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                await serverTask;
            });
        }

        /// <summary>
        /// Tests proactive auth with explicit WebProxy credentials (not environment variable).
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task ProxyAuth_ExplicitWebProxyCredentials_SentProactively()
        {
            const string ExpectedUsername = "proxyuser";
            const string ExpectedPassword = "proxypass";

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                var psi = new ProcessStartInfo();
                psi.Environment.Add(ProxyPreAuthEnvVar, "1");

                Task serverTask = proxyServer.AcceptConnectionAsync(async connection =>
                {
                    List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Verify the first request has the Proxy-Authorization header
                    string? authHeader = null;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                        {
                            authHeader = line;
                            break;
                        }
                    }

                    Assert.NotNull(authHeader);
                    Assert.Contains("Basic", authHeader);

                    string expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ExpectedUsername}:{ExpectedPassword}"));
                    Assert.Contains(expectedToken, authHeader);

                    await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                });

                // Encode proxy URI with embedded credentials so we stay within RemoteExecutor's 3-arg limit
                string proxyUriWithCreds = $"http://{ExpectedUsername}:{ExpectedPassword}@{proxyUri.Host}:{proxyUri.Port}";

                await RemoteExecutor.Invoke(async (proxyUriString, useVersionString) =>
                {
                    var proxyUriParsed = new Uri(proxyUriString);
                    using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                    handler.Proxy = new WebProxy(new Uri($"http://{proxyUriParsed.Host}:{proxyUriParsed.Port}"))
                    {
                        Credentials = new NetworkCredential(
                            proxyUriParsed.UserInfo.Split(':')[0],
                            proxyUriParsed.UserInfo.Split(':')[1])
                    };

                    using HttpClient client = CreateHttpClient(handler, useVersionString);
                    using HttpResponseMessage response = await client.GetAsync("http://destination.test/");

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, proxyUriWithCreds, UseVersion.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                await serverTask;
            });
        }

        /// <summary>
        /// Tests proactive proxy auth across 4 proxy/request protocol combinations:
        /// HTTP proxy + HTTP request, HTTP proxy + HTTPS request (CONNECT tunnel),
        /// HTTPS proxy + HTTP request, HTTPS proxy + HTTPS request (CONNECT tunnel).
        /// Verifies that the Proxy-Authorization header is present on the first request to the proxy.
        /// </summary>
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)] // HTTP proxy + HTTP request
        [InlineData(false, true)]  // HTTP proxy + HTTPS request (CONNECT tunnel)
        [InlineData(true, false)]  // HTTPS proxy + HTTP request
        [InlineData(true, true)]   // HTTPS proxy + HTTPS request (CONNECT tunnel)
        public async Task ProxyAuth_ProxyAndRequestProtocolCombinations_SentProactively(bool proxyUseSsl, bool requestUseSsl)
        {
            const string ExpectedUsername = "matrixuser";
            const string ExpectedPassword = "matrixpass";
            string expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ExpectedUsername}:{ExpectedPassword}"));

            var proxyOptions = new LoopbackServer.Options { UseSsl = proxyUseSsl };

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                var psi = new ProcessStartInfo();
                psi.Environment.Add(ProxyPreAuthEnvVar, "1");

                Task serverTask = proxyServer.AcceptConnectionAsync(async connection =>
                {
                    // Read the first request sent to the proxy
                    List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                    // Verify the Proxy-Authorization header is present on the first request
                    string? authHeader = null;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                        {
                            authHeader = line;
                            break;
                        }
                    }

                    Assert.NotNull(authHeader);
                    Assert.Contains("Basic", authHeader);
                    Assert.Contains(expectedToken, authHeader);

                    if (requestUseSsl)
                    {
                        // For HTTPS request, the proxy received a CONNECT request.
                        // Verify it's a CONNECT method.
                        Assert.StartsWith("CONNECT", lines[0]);

                        // Send 200 to establish the tunnel
                        await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);

                        // Now the client will negotiate TLS through the tunnel.
                        // Wrap the connection's stream in SSL to act as the destination server.
                        var sslConnection = await LoopbackServer.Connection.CreateAsync(
                            null, connection.Stream, new LoopbackServer.Options { UseSsl = true });
                        await sslConnection.ReadRequestHeaderAndSendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                    }
                    else
                    {
                        // For HTTP request, the proxy received a plain GET request.
                        Assert.StartsWith("GET", lines[0]);
                        await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
                    }
                });

                string requestScheme = requestUseSsl ? "https" : "http";

                // Encode proxy URI with embedded credentials so we stay within RemoteExecutor's 3-arg limit
                string proxyScheme = proxyUseSsl ? "https" : "http";
                string proxyUriWithCreds = $"{proxyScheme}://{ExpectedUsername}:{ExpectedPassword}@{proxyUri.Host}:{proxyUri.Port}";

                await RemoteExecutor.Invoke(async (proxyUriString, reqScheme, useVersionString) =>
                {
                    var proxyUriParsed = new Uri(proxyUriString);
                    using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                    handler.Proxy = new WebProxy(new Uri($"{proxyUriParsed.Scheme}://{proxyUriParsed.Host}:{proxyUriParsed.Port}"))
                    {
                        Credentials = new NetworkCredential(
                            proxyUriParsed.UserInfo.Split(':')[0],
                            proxyUriParsed.UserInfo.Split(':')[1])
                    };

                    using HttpClient client = CreateHttpClient(handler, useVersionString);
                    using HttpResponseMessage response = await client.GetAsync($"{reqScheme}://destination.test/");

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }, proxyUriWithCreds, requestScheme, UseVersion.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

                await serverTask;
            }, proxyOptions);
        }
    }

    public sealed class ProactiveProxyAuthTest_Http11 : ProactiveProxyAuthTest
    {
        public ProactiveProxyAuthTest_Http11(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version11;
    }
}
