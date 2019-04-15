// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP will send default credentials based on manifest settings")]
    [PlatformSpecific(TestPlatforms.Windows)]
    public abstract class DefaultCredentialsTest : HttpClientHandlerTestBase
    {
        private static bool DomainJoinedTestsEnabled => !string.IsNullOrEmpty(Configuration.Http.DomainJoinedHttpHost);

        private static bool DomainProxyTestsEnabled => !string.IsNullOrEmpty(Configuration.Http.DomainJoinedProxyHost);

        // Enable this to test against local HttpListener over loopback
        // Note this doesn't work as expected with WinHttpHandler, because WinHttpHandler will always authenticate the 
        // current user against a loopback server using NTLM or Negotiate.
        private static bool LocalHttpListenerTestsEnabled = false;

        public static bool ServerAuthenticationTestsEnabled => (LocalHttpListenerTestsEnabled || DomainJoinedTestsEnabled);

        private static string s_specificUserName = Configuration.Security.ActiveDirectoryUserName;
        private static string s_specificPassword = Configuration.Security.ActiveDirectoryUserPassword;
        private static string s_specificDomain = Configuration.Security.ActiveDirectoryName;
        private readonly NetworkCredential _specificCredential =
            new NetworkCredential(s_specificUserName, s_specificPassword, s_specificDomain);
        private static Uri s_authenticatedServer = DomainJoinedTestsEnabled ? 
            new Uri($"http://{Configuration.Http.DomainJoinedHttpHost}/test/auth/negotiate/showidentity.ashx") : null;

        public DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }

        [OuterLoop("Uses external server")]
        [ConditionalTheory(nameof(ServerAuthenticationTestsEnabled))]
        [MemberData(nameof(AuthenticatedServers))]
        public async Task UseDefaultCredentials_DefaultValue_Unauthorized(string uri, bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [OuterLoop("Uses external server")]
        [ConditionalTheory(nameof(ServerAuthenticationTestsEnabled))]
        [MemberData(nameof(AuthenticatedServers))]
        public async Task UseDefaultCredentials_SetFalse_Unauthorized(string uri, bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = false;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [OuterLoop("Uses external server")]
        [ConditionalTheory(nameof(ServerAuthenticationTestsEnabled))]
        [MemberData(nameof(AuthenticatedServers))]
        public async Task UseDefaultCredentials_SetTrue_ConnectAsCurrentIdentity(string uri, bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = true;

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [OuterLoop("Uses external server")]
        [ConditionalTheory(nameof(ServerAuthenticationTestsEnabled))]
        [MemberData(nameof(AuthenticatedServers))]
        public async Task Credentials_SetToWrappedDefaultCredential_ConnectAsCurrentIdentity(string uri, bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.Credentials = new CredentialWrapper
            {
                InnerCredentials = CredentialCache.DefaultCredentials
            };

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string responseBody = await response.Content.ReadAsStringAsync();
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
                _output.WriteLine("currentIdentity={0}", currentIdentity.Name);
                VerifyAuthentication(responseBody, true, currentIdentity.Name);
            }
        }

        [OuterLoop("Uses external server")]
        [ConditionalTheory(nameof(ServerAuthenticationTestsEnabled))]
        [MemberData(nameof(AuthenticatedServers))]
        public async Task Credentials_SetToBadCredential_Unauthorized(string uri, bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.Credentials = new NetworkCredential("notarealuser", "123456");

            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [OuterLoop("Uses external server")]
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

        [OuterLoop("Uses external server")]
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

        [OuterLoop("Uses external server")]
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
        
        [OuterLoop("Uses external server")]
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

        public static IEnumerable<object[]> AuthenticatedServers()
        {
            // Note that localhost will not actually use the proxy, but there's no harm in testing it.
            foreach (bool b in new bool[] { true, false })
            {
                if (LocalHttpListenerTestsEnabled)
                {
                    yield return new object[] { HttpListenerAuthenticatedLoopbackServer.NtlmOnly.Uri, b };
                    yield return new object[] { HttpListenerAuthenticatedLoopbackServer.NegotiateOnly.Uri, b };
                    yield return new object[] { HttpListenerAuthenticatedLoopbackServer.NegotiateAndNtlm.Uri, b };
                    yield return new object[] { HttpListenerAuthenticatedLoopbackServer.BasicAndNtlm.Uri, b };
                }

                if (!string.IsNullOrEmpty(Configuration.Http.DomainJoinedHttpHost))
                {
                    yield return new object[] { $"http://{Configuration.Http.DomainJoinedHttpHost}/test/auth/negotiate/showidentity.ashx", b };
                    yield return new object[] { $"http://{Configuration.Http.DomainJoinedHttpHost}/test/auth/multipleschemes/showidentity.ashx", b };
                }
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

            public NetworkCredential GetCredential(Uri uri, string authType) => 
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

        private sealed class HttpListenerAuthenticatedLoopbackServer
        {
            private readonly HttpListener _listener;
            private readonly string _uri;

            public static readonly HttpListenerAuthenticatedLoopbackServer NtlmOnly = new HttpListenerAuthenticatedLoopbackServer("http://localhost:8080/", AuthenticationSchemes.Ntlm);
            public static readonly HttpListenerAuthenticatedLoopbackServer NegotiateOnly = new HttpListenerAuthenticatedLoopbackServer("http://localhost:8081/", AuthenticationSchemes.Negotiate);
            public static readonly HttpListenerAuthenticatedLoopbackServer NegotiateAndNtlm = new HttpListenerAuthenticatedLoopbackServer("http://localhost:8082/", AuthenticationSchemes.Negotiate | AuthenticationSchemes.Ntlm);
            public static readonly HttpListenerAuthenticatedLoopbackServer BasicAndNtlm = new HttpListenerAuthenticatedLoopbackServer("http://localhost:8083/", AuthenticationSchemes.Basic | AuthenticationSchemes.Ntlm);

            // Don't construct directly, use instances above
            private HttpListenerAuthenticatedLoopbackServer(string uri, AuthenticationSchemes authenticationSchemes)
            {
                _uri = uri;

                _listener = new HttpListener();
                _listener.Prefixes.Add(uri);
                _listener.AuthenticationSchemes = authenticationSchemes;
                _listener.Start();

                Task.Run(() => ProcessRequests());
            }

            public string Uri => _uri;

            private async void ProcessRequests()
            {
                while (true)
                {
                    var context = await _listener.GetContextAsync();

                    // Send a response in the JSON format that the client expects
                    string username = context.User.Identity.Name;
                    await context.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"{{\"authenticated\": \"true\", \"user\": \"{username}\" }}"));

                    context.Response.Close();
                }
            }
        }
    }
}
