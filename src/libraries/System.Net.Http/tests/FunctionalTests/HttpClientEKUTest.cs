// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientEKUTest : HttpClientHandlerTestBase
    {
        // Curl + OSX SecureTransport doesn't support the custom certificate callback.
        private static bool BackendSupportsCustomCertificateHandling =>
#if TargetsWindows
            true;
#else
            TestHelper.NativeHandlerSupportsSslConfiguration();
#endif

        private static bool CanTestCertificates =>
            Capability.IsTrustedRootCertificateInstalled() && 
            (BackendSupportsCustomCertificateHandling || Capability.AreHostsFileNamesInstalled());

        private static bool CanTestClientCertificates =>
            CanTestCertificates && BackendSupportsCustomCertificateHandling;

        public const int TestTimeoutMilliseconds = 15 * 1000;

        public static X509Certificate2 serverCertificateServerEku = Configuration.Certificates.GetServerCertificate();
        public static X509Certificate2 serverCertificateNoEku = Configuration.Certificates.GetNoEKUCertificate();
        public static X509Certificate2 serverCertificateWrongEku = Configuration.Certificates.GetClientCertificate();

        public static X509Certificate2 clientCertificateWrongEku = Configuration.Certificates.GetServerCertificate();
        public static X509Certificate2 clientCertificateNoEku = Configuration.Certificates.GetNoEKUCertificate();
        public static X509Certificate2 clientCertificateClientEku = Configuration.Certificates.GetClientCertificate();

        private VerboseTestLogging _log = VerboseTestLogging.GetInstance();

        public HttpClientEKUTest(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(CanTestCertificates))]
        public async Task HttpClient_NoEKUServerAuth_Ok()
        {
            var options = new HttpsTestServer.Options();
            options.ServerCertificate = serverCertificateNoEku;

            using (var server = new HttpsTestServer(options))
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                server.Start();

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();

                string requestUriString = GetUriStringAndConfigureHandler(options, server, handler);
                tasks[1] = client.GetStringAsync(requestUriString);

                await tasks.WhenAllOrAnyFailed(TestTimeoutMilliseconds);
            }
        }

        [ConditionalFact(nameof(CanTestCertificates))]
        public async Task HttpClient_ClientEKUServerAuth_Fails()
        {
            var options = new HttpsTestServer.Options();
            options.ServerCertificate = serverCertificateWrongEku;

            using (var server = new HttpsTestServer(options))
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                server.Start();

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();

                string requestUriString = GetUriStringAndConfigureHandler(options, server, handler);
                tasks[1] = client.GetStringAsync(requestUriString);

                await Assert.ThrowsAsync<HttpRequestException>(() => tasks[1]);
            }
        }

        [ConditionalFact(nameof(CanTestClientCertificates))]
        public async Task HttpClient_NoEKUClientAuth_Ok()
        {
            var options = new HttpsTestServer.Options();
            options.ServerCertificate = serverCertificateServerEku;
            options.RequireClientAuthentication = true;

            using (var server = new HttpsTestServer(options))
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                server.Start();

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();

                string requestUriString = GetUriStringAndConfigureHandler(options, server, handler);
                handler.ClientCertificates.Add(clientCertificateNoEku);
                tasks[1] = client.GetStringAsync(requestUriString);

                await tasks.WhenAllOrAnyFailed(TestTimeoutMilliseconds);
            }
        }

        [ConditionalFact(nameof(CanTestClientCertificates))]
        public async Task HttpClient_ServerEKUClientAuth_Fails()
        {
            var options = new HttpsTestServer.Options();
            options.ServerCertificate = serverCertificateServerEku;
            options.RequireClientAuthentication = true;

            using (var server = new HttpsTestServer(options))
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                server.Start();

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();

                string requestUriString = GetUriStringAndConfigureHandler(options, server, handler);
                handler.ClientCertificates.Add(clientCertificateWrongEku);
                tasks[1] = client.GetStringAsync(requestUriString);

                // Server aborts the TCP channel.
                await Assert.ThrowsAsync<HttpRequestException>(() => tasks[1]);
                
                await Assert.ThrowsAsync<AuthenticationException>(() => tasks[0]);
            }
        }

        private string GetUriStringAndConfigureHandler(HttpsTestServer.Options options, HttpsTestServer server, HttpClientHandler handler)
        {
            if (Capability.AreHostsFileNamesInstalled())
            {
                string hostName =
                    (new UriBuilder("https", options.ServerCertificate.GetNameInfo(X509NameType.SimpleName, false), server.Port)).ToString();

                Console.WriteLine("[E2E testing] - Using hostname {0}", hostName);
                return hostName;
            }
            else
            {
                handler.ServerCertificateCustomValidationCallback = AllowRemoteCertificateNameMismatch;
                return "https://localhost:" + server.Port.ToString();
            }
        }

        private bool AllowRemoteCertificateNameMismatch(HttpRequestMessage httpMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            return false;
        }
    }
}
