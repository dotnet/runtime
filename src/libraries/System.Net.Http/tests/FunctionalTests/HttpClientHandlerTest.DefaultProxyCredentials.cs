// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [ActiveIssue(23702, TargetFrameworkMonikers.NetFramework)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
        [Fact]
        public async Task ProxyExplicitlyProvided_DefaultCredentials_Ignored()
        {
            var explicitProxyCreds = new NetworkCredential("rightusername", "rightpassword");
            var defaultSystemProxyCreds = new NetworkCredential("wrongusername", "wrongpassword");
            string expectCreds = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{explicitProxyCreds.UserName}:{explicitProxyCreds.Password}"));

            await LoopbackServer.CreateClientAndServerAsync(async proxyUrl =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, explicitProxyCreds);
                    handler.DefaultProxyCredentials = defaultSystemProxyCreds;
                    using (HttpResponseMessage response = await client.GetAsync("http://notatrealserver.com/")) // URL does not matter
                    {
                        Assert.Equal(response.StatusCode, HttpStatusCode.OK);
                    }
                }
            }, async server =>
            {
                if (!IsCurlHandler) // libcurl sends Basic auth preemptively when only basic creds are provided; other handlers wait for 407.
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.ProxyAuthenticationRequired, "Proxy-Authenticate: Basic\r\n");
                }

                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK);
                Assert.Equal(expectCreds, LoopbackServer.GetRequestHeaderValue(headers, "Proxy-Authorization"));
            });
        }

        [OuterLoop("Uses external server")]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // The default proxy is resolved via WinINet on Windows.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProxySetViaEnvironmentVariable_DefaultProxyCredentialsUsed(bool useProxy)
        {
            const string ExpectedUsername = "rightusername";
            const string ExpectedPassword = "rightpassword";
            LoopbackServer.Options options = new LoopbackServer.Options { IsProxy = true, Username = ExpectedUsername, Password = ExpectedPassword };

            await LoopbackServer.CreateServerAsync(async (proxyServer, proxyUri) =>
            {
                // libcurl will read a default proxy from the http_proxy environment variable.  Ensure that when it does,
                // our default proxy credentials are used.  To avoid messing up anything else in this process, we run the
                // test in another process.
                var psi = new ProcessStartInfo();
                Task<List<string>> proxyTask = null;

                if (useProxy)
                {
                    proxyTask = proxyServer.AcceptConnectionPerformAuthenticationAndCloseAsync("Proxy-Authenticate: Basic realm=\"NetCore\"\r\n");
                    psi.Environment.Add("http_proxy", $"http://{proxyUri.Host}:{proxyUri.Port}");
                }

                RemoteExecutor.Invoke(async (useProxyString, useSocketsHttpHandlerString) =>
                {
                    using (HttpClientHandler handler = CreateHttpClientHandler(useSocketsHttpHandlerString))
                    using (var client = new HttpClient(handler))
                    {
                        var creds = new NetworkCredential(ExpectedUsername, ExpectedPassword);
                        handler.DefaultProxyCredentials = creds;
                        handler.UseProxy = bool.Parse(useProxyString);

                        HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer);
                        // Correctness of user and password is done in server part.
                        Assert.True(response.StatusCode ==  HttpStatusCode.OK);
                    }
                    return RemoteExecutor.SuccessExitCode;
                }, useProxy.ToString(), UseSocketsHttpHandler.ToString(), new RemoteInvokeOptions { StartInfo = psi }).Dispose();
                if (useProxy)
                {
                    await proxyTask;
                }
            }, options);
        }

        // The purpose of this test is mainly to validate the .NET Framework OOB System.Net.Http implementation
        // since it has an underlying dependency to WebRequest. While .NET Core implementations of System.Net.Http
        // are not using any WebRequest code, the test is still useful to validate correctness.
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task ProxyNotExplicitlyProvided_DefaultCredentialsSet_DefaultWebProxySetToNull_Success()
        {
            WebRequest.DefaultWebProxy = null;

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.DefaultProxyCredentials = new NetworkCredential("UsernameNotUsed", "PasswordNotUsed");
                HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer);

                Assert.Equal(response.StatusCode, HttpStatusCode.OK);
            }
        }
    }
}
