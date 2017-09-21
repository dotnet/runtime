// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // TODO: #2383 - Consolidate the use of the environment variable settings to Common/tests.
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "dotnet/corefx #20010")]
    [PlatformSpecific(TestPlatforms.Windows)]
    public class DefaultCredentialsTest : HttpClientTestBase
    {
        private static string DomainJoinedTestServer => Configuration.Http.DomainJoinedHttpHost;
        private static bool DomainJoinedTestsEnabled => !string.IsNullOrEmpty(DomainJoinedTestServer);
        private static bool DomainProxyTestsEnabled => (!string.IsNullOrEmpty(Configuration.Http.DomainJoinedProxyHost)) && DomainJoinedTestsEnabled;

        private static string s_specificUserName = Configuration.Security.ActiveDirectoryUserName;
        private static string s_specificPassword = Configuration.Security.ActiveDirectoryUserPassword;
        private static string s_specificDomain = Configuration.Security.ActiveDirectoryName;
        private static Uri s_authenticatedServer =
            new Uri($"http://{DomainJoinedTestServer}/test/auth/negotiate/showidentity.ashx");
            
        // This test endpoint offers multiple schemes, Basic and NTLM, in that specific order. This endpoint
        // helps test that the client will use the stronger of the server proposed auth schemes and
        // not the first auth scheme.
        private static Uri s_multipleSchemesAuthenticatedServer =
            new Uri($"http://{DomainJoinedTestServer}/test/auth/multipleschemes/showidentity.ashx");

        private readonly ITestOutputHelper _output;
        private readonly NetworkCredential _specificCredential =
            new NetworkCredential(s_specificUserName, s_specificPassword, s_specificDomain);

        public DefaultCredentialsTest(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine(s_authenticatedServer.ToString());
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalTheory(nameof(DomainJoinedTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_DefaultValue_Unauthorized(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(s_authenticatedServer))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalTheory(nameof(DomainJoinedTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetFalse_Unauthorized(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = false;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(s_authenticatedServer))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalTheory(nameof(DomainJoinedTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetTrue_ConnectAsCurrentIdentity(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = true;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(s_authenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalTheory(nameof(DomainJoinedTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetTrueAndServerOffersMultipleSchemes_Ok(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = true;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(s_multipleSchemesAuthenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalTheory(nameof(DomainJoinedTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Credentials_SetToSpecificCredential_ConnectAsSpecificIdentity(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = false;
            handler.Credentials = _specificCredential;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(s_authenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                string responseBody = await response.Content.ReadAsStringAsync();
                VerifyAuthentication(responseBody, true, s_specificDomain + "\\" + s_specificUserName);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalTheory(nameof(DomainJoinedTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Credentials_SetToWrappedDefaultCredential_ConnectAsCurrentIdentity(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.Credentials = new CredentialWrapper
            {
                InnerCredentials = CredentialCache.DefaultCredentials
            };

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(s_authenticatedServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalFact(nameof(DomainProxyTestsEnabled))]
        public async Task Proxy_UseAuthenticatedProxyWithNoCredentials_ProxyAuthenticationRequired()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = new AuthenticatedProxy(null);

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, response.StatusCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(10041)]
        [ConditionalFact(nameof(DomainProxyTestsEnabled))]
        public async Task Proxy_UseAuthenticatedProxyWithDefaultCredentials_OK()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = new AuthenticatedProxy(CredentialCache.DefaultCredentials);

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
        
        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(DomainProxyTestsEnabled))]
        public async Task Proxy_UseAuthenticatedProxyWithWrappedDefaultCredentials_OK()
        {
            ICredentials wrappedCreds = new CredentialWrapper
            {
                InnerCredentials = CredentialCache.DefaultCredentials
            };

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = new AuthenticatedProxy(wrappedCreds);

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
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

                string host = Configuration.Http.DomainJoinedProxyHost;
                Assert.False(string.IsNullOrEmpty(host), "DomainJoinedProxyHost must specify proxy hostname");

                string portString = Configuration.Http.DomainJoinedProxyPort;
                Assert.False(string.IsNullOrEmpty(portString), "DomainJoinedProxyPort must specify proxy port number");

                int port;
                Assert.True(int.TryParse(portString, out port), "DomainJoinedProxyPort must be a valid port number");

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
