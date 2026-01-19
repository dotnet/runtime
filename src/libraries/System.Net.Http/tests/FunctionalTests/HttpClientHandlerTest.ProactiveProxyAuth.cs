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
                psi.Environment.Add("DOTNET_SYSTEM_NET_HTTP_ENABLEPROACTIVEPROXYAUTH", "1");

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
                // NOT setting DOTNET_SYSTEM_NET_HTTP_ENABLEPROACTIVEPROXYAUTH

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
                psi.Environment.Add("DOTNET_SYSTEM_NET_HTTP_ENABLEPROACTIVEPROXYAUTH", "1");

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
    }

    public sealed class ProactiveProxyAuthTest_Http11 : ProactiveProxyAuthTest
    {
        public ProactiveProxyAuthTest_Http11(ITestOutputHelper helper) : base(helper) { }
        protected override Version UseVersion => HttpVersion.Version11;
    }
}
