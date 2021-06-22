// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Default_Get_Null()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Null(handler.DefaultProxyCredentials);
            }
        }

        [Fact]
        public void SetGet_Roundtrips()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                var creds = new NetworkCredential("username", "password", "domain");

                handler.DefaultProxyCredentials = null;
                Assert.Null(handler.DefaultProxyCredentials);

                handler.DefaultProxyCredentials = creds;
                Assert.Same(creds, handler.DefaultProxyCredentials);

                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                Assert.Same(CredentialCache.DefaultCredentials, handler.DefaultProxyCredentials);
            }
        }

        [Fact]
        public async Task ProxyExplicitlyProvided_DefaultCredentials_Ignored()
        {
            var explicitProxyCreds = new NetworkCredential("rightusername", "rightpassword");
            var defaultSystemProxyCreds = new NetworkCredential("wrongusername", "wrongpassword");
            string expectCreds = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{explicitProxyCreds.UserName}:{explicitProxyCreds.Password}"));

            await LoopbackServer.CreateClientAndServerAsync(async proxyUrl =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, explicitProxyCreds);
                    handler.DefaultProxyCredentials = defaultSystemProxyCreds;
                    using (HttpResponseMessage response = await client.GetAsync("http://notatrealserver.com/")) // URL does not matter
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync(
                    HttpStatusCode.ProxyAuthenticationRequired, "Proxy-Authenticate: Basic\r\n");

                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK);
                Assert.Equal(expectCreds, LoopbackServer.GetRequestHeaderValue(headers, "Proxy-Authorization"));
            });
        }

#if !WINHTTPHANDLER_TEST
        [PlatformSpecific(TestPlatforms.AnyUnix)] // The default proxy is resolved via WinINet on Windows.
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProxySetViaEnvironmentVariable_DefaultProxyCredentialsUsed(bool useProxy)
        {
            const string ExpectedUsername = "rightusername";
            const string ExpectedPassword = "rightpassword";
            LoopbackServer.Options options = new LoopbackServer.Options { IsProxy = true, Username = ExpectedUsername, Password = ExpectedPassword };

            await LoopbackServer.CreateClientAndServerAsync(uri => Task.Run(() =>
            {
                var psi = new ProcessStartInfo();
                psi.Environment.Add("http_proxy", $"http://{uri.Host}:{uri.Port}");

                RemoteExecutor.Invoke(async (useProxyString, useVersionString, uriString) =>
                {
                    using (HttpClientHandler handler = CreateHttpClientHandler(useVersionString))
                    using (HttpClient client = CreateHttpClient(handler, useVersionString))
                    {
                        var creds = new NetworkCredential(ExpectedUsername, ExpectedPassword);
                        handler.DefaultProxyCredentials = creds;
                        handler.UseProxy = bool.Parse(useProxyString);

                        HttpResponseMessage response = await client.GetAsync(uriString);
                        // Correctness of user and password is done in server part.
                        Assert.True(response.StatusCode == HttpStatusCode.OK);
                    };
                }, useProxy.ToString(), UseVersion.ToString(),
                    // If proxy is used , the url does not matter. We set it to be different to avoid confusion.
                   useProxy ? Configuration.Http.RemoteEchoServer.ToString() : uri.ToString(),
                   new RemoteInvokeOptions { StartInfo = psi }).Dispose();
            }),
            server => server.AcceptConnectionAsync(async connection =>
            {
                const string headerName = "Proxy-Authorization";
                List<string> lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);

                // First request should not have proxy credentials in either case.
                for (int i = 1; i < lines.Count; i++)
                {
                    Assert.False(lines[i].StartsWith(headerName));
                }

                if (useProxy)
                {
                    // Reject request and wait for authenticated one.
                    await connection.SendResponseAsync(HttpStatusCode.ProxyAuthenticationRequired, "Proxy-Authenticate: Basic realm=\"NetCore\"\r\n").ConfigureAwait(false);

                    lines = await connection.ReadRequestHeaderAsync().ConfigureAwait(false);
                    bool valid = false;
                    for (int i = 1; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith(headerName))
                        {
                            valid = LoopbackServer.IsBasicAuthTokenValid(lines[i], options);
                        }
                    }

                    Assert.True(valid);
                }

                await connection.SendResponseAsync(HttpStatusCode.OK).ConfigureAwait(false);
            }));
        }
#endif

        // The purpose of this test is mainly to validate the .NET Framework OOB System.Net.Http implementation
        // since it has an underlying dependency to WebRequest. While .NET Core implementations of System.Net.Http
        // are not using any WebRequest code, the test is still useful to validate correctness.
        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task ProxyNotExplicitlyProvided_DefaultCredentialsSet_DefaultWebProxySetToNull_Success()
        {
            WebRequest.DefaultWebProxy = null;

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.DefaultProxyCredentials = new NetworkCredential("UsernameNotUsed", "PasswordNotUsed");
                HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
