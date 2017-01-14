// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class AuthenticationTests : IDisposable
    {
        private const string Basic = "Basic";
        private const string TestUser = "testuser";
        private const string TestPassword = "testpassword";

        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private string _url;

        public AuthenticationTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _url = _factory.ListeningUrl;
        }

        public void Dispose() => _factory.Dispose();

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.None)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous)]
        public async Task TestBasicAuthentication(AuthenticationSchemes authScheme)
        {
            _listener.AuthenticationSchemes = authScheme;
            await ValidateValidUser();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task TestAnonymousAuthentication()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            await ValidateNullUser();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task TestBasicAuthenticationWithDelegate()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.None;
            AuthenticationSchemeSelector selector = new AuthenticationSchemeSelector(SelectAnonymousAndBasicSchemes);
            _listener.AuthenticationSchemeSelectorDelegate += selector;

            await ValidateValidUser();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task TestAnonymousAuthenticationWithDelegate()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.None;
            AuthenticationSchemeSelector selector = new AuthenticationSchemeSelector(SelectAnonymousScheme);
            _listener.AuthenticationSchemeSelectorDelegate += selector;

            await ValidateNullUser();
        }

        private async Task ValidateNullUser()
        {
            var serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                var clientTask = client.GetStringAsync(_url);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Null(listenerContext.User);
            }
        }

        private async Task ValidateValidUser()
        {
            var serverContextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                var clientTask = client.GetStringAsync(_url);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Equal(TestUser, listenerContext.User.Identity.Name);
                Assert.True(listenerContext.User.Identity.IsAuthenticated);
                Assert.Equal(Basic, listenerContext.User.Identity.AuthenticationType);
            }
        }

        private AuthenticationSchemes SelectAnonymousAndBasicSchemes(HttpListenerRequest request)
        {
            return AuthenticationSchemes.Anonymous | AuthenticationSchemes.Basic;
        }

        private AuthenticationSchemes SelectAnonymousScheme(HttpListenerRequest request)
        {
            return AuthenticationSchemes.Anonymous;
        }
    }
}
