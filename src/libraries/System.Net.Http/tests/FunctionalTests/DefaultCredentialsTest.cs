// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Security.Principal;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    // TODO: #2383 - Consolidate the use of the environment variable settings to Common/tests.
    public class DefaultCredentialsTest
    {
        private static string HttpInternalTestServer => Environment.GetEnvironmentVariable("HTTP_INTERNAL_TESTSERVER");
        private static bool HttpInternalTestsEnabled => !string.IsNullOrEmpty(HttpInternalTestServer);
        private static string SpecificUserName = "test";
        private static string SpecificPassword = "Password1";
        private static string SpecificDomain = HttpInternalTestServer;
        private static Uri AuthenticatedServer =
            new Uri($"http://{HttpInternalTestServer}/test/auth/negotiate/showidentity.ashx");

        private readonly ITestOutputHelper _output;
        private readonly NetworkCredential _specificCredential =
            new NetworkCredential(SpecificUserName, SpecificPassword, SpecificDomain);

        public DefaultCredentialsTest(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine(AuthenticatedServer.ToString());
        }

        [ConditionalTheory(nameof(HttpInternalTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_DefaultValue_Unauthorized(bool useProxy)
        {
            var handler = new HttpClientHandler();
            handler.UseProxy = useProxy;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(AuthenticatedServer))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [ConditionalTheory(nameof(HttpInternalTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetFalse_Unauthorized(bool useProxy)
        {
            var handler = new HttpClientHandler { UseProxy = useProxy, UseDefaultCredentials = false };

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(AuthenticatedServer))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [ConditionalTheory(nameof(HttpInternalTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetTrue_ConnectAsCurrentIdentity(bool useProxy)
        {
            var handler = new HttpClientHandler { UseProxy = useProxy, UseDefaultCredentials = true };

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(AuthenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [ConditionalTheory(nameof(HttpInternalTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Credentials_SetToSpecificCredential_ConnectAsSpecificIdentity(bool useProxy)
        {
            var handler = new HttpClientHandler {
                UseProxy = useProxy,
                UseDefaultCredentials = false,
                Credentials = _specificCredential };

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(AuthenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                string responseBody = await response.Content.ReadAsStringAsync();
                VerifyAuthentication(responseBody, true, SpecificDomain + "\\" + SpecificUserName);
            }
        }

        [ConditionalTheory(nameof(HttpInternalTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Credentials_SetToWrappedDefaultCredential_ConnectAsCurrentIdentity(bool useProxy)
        {
            var handler = new HttpClientHandler();
            handler.UseProxy = useProxy;
            handler.Credentials = new CredentialWrapper
            {
                InnerCredentials = CredentialCache.DefaultCredentials
            };

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(AuthenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [ConditionalFact(nameof(HttpInternalTestsEnabled))]
        public async Task Proxy_UseAuthenticatedProxyWithNoCredentials_ProxyAuthenticationRequired()
        {
            var handler = new HttpClientHandler();
            handler.Proxy = new AuthenticatedProxy(null);

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);
            }
        }

        [ConditionalFact(nameof(HttpInternalTestsEnabled))]
        public async Task Proxy_UseAuthenticatedProxyWithDefaultCredentials_OK()
        {
            var handler = new HttpClientHandler();
            handler.Proxy = new AuthenticatedProxy(CredentialCache.DefaultCredentials);

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalFact(nameof(HttpInternalTestsEnabled))]
        public async Task Proxy_UseAuthenticatedProxyWithWrappedDefaultCredentials_OK()
        {
            ICredentials wrappedCreds = new CredentialWrapper
            {
                InnerCredentials = CredentialCache.DefaultCredentials
            };

            var handler = new HttpClientHandler();
            handler.Proxy = new AuthenticatedProxy(wrappedCreds);

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private void VerifyAuthentication(string response, bool authenticated, string user)
        {
            // Convert all strings to lowercase to compare. Windows treats domain and username as case-insensitive.
            response = response.ToLower();
            user = user.ToLower();
            _output.WriteLine(response);

            if (!authenticated)
            {
                Assert.True(
                    TestHelper.JsonMessageContainsKeyValue(response, "authenticated", "false"),
                    "authenticated == false");
            }
            else
            {
                Assert.True(
                    TestHelper.JsonMessageContainsKeyValue(response, "authenticated", "true"),
                    "authenticated == true");
                Assert.True(
                    TestHelper.JsonMessageContainsKeyValue(response, "user", user),
                    $"user == {user}");
            }
        }

        private class CredentialWrapper : ICredentials
        {
            public ICredentials InnerCredentials { get; set; }

            public NetworkCredential GetCredential(Uri uri, String authType) => 
                InnerCredentials?.GetCredential(uri, authType);
        }

        private class AuthenticatedProxy : IWebProxy
        {
            ICredentials _credentials;
            Uri _proxyUri;

            public AuthenticatedProxy(ICredentials credentials)
            {
                _credentials = credentials;

                string host = Environment.GetEnvironmentVariable("HTTP_INTERNAL_PROXYSERVER");
                Assert.False(string.IsNullOrEmpty(host), "HTTP_INTERNAL_PROXYSERVER must specify proxy hostname");

                string portString = Environment.GetEnvironmentVariable("HTTP_INTERNAL_PROXYSERVER_PORT");
                Assert.False(string.IsNullOrEmpty(portString), "HTTP_INTERNAL_PROXYSERVER_PORT must specify proxy port number");

                int port;
                Assert.True(int.TryParse(portString, out port), "HTTP_INTERNAL_PROXYSERVER_PORT must be a valid port number");

                _proxyUri = new Uri(string.Format("http://{0}:{1}", host, port));
            }

            public ICredentials Credentials
            {
                get
                {
                    return _credentials;
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public Uri GetProxy(Uri destination)
            {
                return _proxyUri;
            }

            public bool IsBypassed(Uri host)
            {
                return false;
            }
        }        
    }
}
