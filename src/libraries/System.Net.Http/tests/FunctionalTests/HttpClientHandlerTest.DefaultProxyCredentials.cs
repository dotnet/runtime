// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientHandler_DefaultProxyCredentials_Test : RemoteExecutorTestBase
    {
        [Fact]
        public void Default_Get_Null()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Null(handler.DefaultProxyCredentials);
            }
        }

        [Fact]
        public void SetGet_Roundtrips()
        {
            using (var handler = new HttpClientHandler())
            {
                var creds = new NetworkCredential("username", "password", "domain");
                handler.DefaultProxyCredentials = creds;
                Assert.Same(creds, handler.DefaultProxyCredentials);

                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                Assert.Same(CredentialCache.DefaultCredentials, handler.DefaultProxyCredentials);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void ProxyExplicitlyProvided_DefaultCredentials_Ignored()
        {
            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(out port, requireAuth: true, expectCreds: true);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            var rightCreds = new NetworkCredential("rightusername", "rightpassword");
            var wrongCreds = new NetworkCredential("wrongusername", "wrongpassword");

            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, rightCreds);
                handler.DefaultProxyCredentials = wrongCreds;

                Task<HttpResponseMessage> responseTask = client.GetAsync(Configuration.Http.RemoteEchoServer);
                Task<string> responseStringTask = responseTask.ContinueWith(t => t.Result.Content.ReadAsStringAsync(), TaskScheduler.Default).Unwrap();
                Task.WaitAll(proxyTask, responseTask, responseStringTask);

                TestHelper.VerifyResponseBody(responseStringTask.Result, responseTask.Result.Content.Headers.ContentMD5, false, null);
                Assert.Equal(Encoding.ASCII.GetString(proxyTask.Result.ResponseContent), responseStringTask.Result);

                string expectedAuth = $"{rightCreds.UserName}:{rightCreds.Password}";
                Assert.Equal(expectedAuth, proxyTask.Result.AuthenticationHeaderValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // proxies set via the http_proxy environment variable are specific to Unix
        public void ProxySetViaEnvironmentVariable_DefaultProxyCredentialsUsed()
        {
            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(out port, requireAuth: true, expectCreds: true);

            const string ExpectedUsername = "rightusername";
            const string ExpectedPassword = "rightpassword";

            // libcurl will read a default proxy from the http_proxy environment variable.  Ensure that when it does,
            // our default proxy credentials are used.  To avoid messing up anything else in this process, we run the
            // test in another process.
            var psi = new ProcessStartInfo();
            psi.Environment.Add("http_proxy", $"http://localhost:{port}");
            RemoteInvoke(() =>
            {
                using (var handler = new HttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    var creds = new NetworkCredential(ExpectedUsername, ExpectedPassword);
                    handler.DefaultProxyCredentials = creds;

                    Task<HttpResponseMessage> responseTask = client.GetAsync(Configuration.Http.RemoteEchoServer);
                    Task<string> responseStringTask = responseTask.ContinueWith(t => t.Result.Content.ReadAsStringAsync(), TaskScheduler.Default).Unwrap();
                    Task.WaitAll(responseTask, responseStringTask);

                    TestHelper.VerifyResponseBody(responseStringTask.Result, responseTask.Result.Content.Headers.ContentMD5, false, null);
                }
                return SuccessExitCode;
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();

            Assert.Equal($"{ExpectedUsername}:{ExpectedPassword}", proxyTask.Result.AuthenticationHeaderValue);
        }

    }
}
