// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientHandler_DefaultProxyCredentials_Test : HttpClientTestBase
    {
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
        [ActiveIssue(20010, TargetFrameworkMonikers.Uap)]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ProxyExplicitlyProvided_DefaultCredentials_Ignored()
        {
            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(out port, requireAuth: true, expectCreds: true);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            var rightCreds = new NetworkCredential("rightusername", "rightpassword");
            var wrongCreds = new NetworkCredential("wrongusername", "wrongpassword");

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, rightCreds);
                handler.DefaultProxyCredentials = wrongCreds;

                Task<HttpResponseMessage> responseTask = client.GetAsync(Configuration.Http.RemoteEchoServer);
                Task<string> responseStringTask = responseTask.ContinueWith(t =>
                {
                    using (t.Result) return t.Result.Content.ReadAsStringAsync();
                }, TaskScheduler.Default).Unwrap();
                await (new Task[] { proxyTask, responseTask, responseStringTask }).WhenAllOrAnyFailed();

                TestHelper.VerifyResponseBody(responseStringTask.Result, responseTask.Result.Content.Headers.ContentMD5, false, null);
                Assert.Equal(Encoding.ASCII.GetString(proxyTask.Result.ResponseContent), responseStringTask.Result);

                string expectedAuth = $"{rightCreds.UserName}:{rightCreds.Password}";
                Assert.Equal(expectedAuth, proxyTask.Result.AuthenticationHeaderValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue(25640, TestPlatforms.Windows)] // TODO It should be enabled for managed Handler on all platforms
        public void ProxySetViaEnvironmentVariable_DefaultProxyCredentialsUsed(bool useProxy)
        {
            int port = 0;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = null;
            if (useProxy)
            {
                proxyTask = LoopbackGetRequestHttpProxy.StartAsync(out port, requireAuth: true, expectCreds: true);
            }

            const string ExpectedUsername = "rightusername";
            const string ExpectedPassword = "rightpassword";

            // libcurl will read a default proxy from the http_proxy environment variable.  Ensure that when it does,
            // our default proxy credentials are used.  To avoid messing up anything else in this process, we run the
            // test in another process.
            var psi = new ProcessStartInfo();
            psi.Environment.Add("http_proxy", $"http://localhost:{port}");
            RemoteInvoke((useProxyString, useManagedHandlerString) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler(useManagedHandlerString))
                using (var client = new HttpClient(handler))
                {
                    var creds = new NetworkCredential(ExpectedUsername, ExpectedPassword);
                    handler.DefaultProxyCredentials = creds;
                    handler.UseProxy = bool.Parse(useProxyString);

                    Task<HttpResponseMessage> responseTask = client.GetAsync(Configuration.Http.RemoteEchoServer);
                    Task<string> responseStringTask = responseTask.ContinueWith(t =>
                    {
                        using (t.Result) return t.Result.Content.ReadAsStringAsync();
                    }, TaskScheduler.Default).Unwrap();
                    Task.WaitAll(responseTask, responseStringTask);

                    TestHelper.VerifyResponseBody(responseStringTask.Result, responseTask.Result.Content.Headers.ContentMD5, false, null);
                }
                return SuccessExitCode;
            }, useProxy.ToString(), UseManagedHandler.ToString(), new RemoteInvokeOptions { StartInfo = psi }).Dispose();

            if (useProxy)
            {
                Assert.Equal($"{ExpectedUsername}:{ExpectedPassword}", proxyTask.Result.AuthenticationHeaderValue);
            }
        }

        // The purpose of this test is mainly to validate the .NET Framework OOB System.Net.Http implementation
        // since it has an underlying dependency to WebRequest. While .NET Core implementations of System.Net.Http
        // are not using any WebRequest code, the test is still useful to validate correctness.
        [OuterLoop] // TODO: Issue #11345
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
