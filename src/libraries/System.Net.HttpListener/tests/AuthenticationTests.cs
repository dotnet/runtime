// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class AuthenticationTests
    {
        private const string Basic = "Basic";
        private const string TestUser = "testuser";
        private const string TestPassword = "testpassword";

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.HasHttpApi))]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.None)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous)]
        public async Task TestBasicAuthentication(AuthenticationSchemes authScheme)
        {
            string url = UrlPrefix.CreateLocal();
            HttpListener listener = new HttpListener();

            try
            {
                listener.Prefixes.Add(url);
                listener.AuthenticationSchemes = authScheme;
                listener.Start();

                await ValidateValidUser(url, listener);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.HasHttpApi))]
        public async Task TestAnonymousAuthentication()
        {
            string url = UrlPrefix.CreateLocal();
            HttpListener listener = new HttpListener();

            try
            {
                listener.Prefixes.Add(url);
                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                listener.Start();

                await ValidateNullUser(url, listener);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.HasHttpApi))]
        public async Task TestBasicAuthenticationWithDelegate()
        {
            string url = UrlPrefix.CreateLocal();
            HttpListener listener = new HttpListener();
            try
            {
                listener.Prefixes.Add(url);
                listener.AuthenticationSchemes = AuthenticationSchemes.None;
                AuthenticationSchemeSelector selector = new AuthenticationSchemeSelector(SelectAnonymousAndBasicSchemes);
                listener.AuthenticationSchemeSelectorDelegate += selector;
                listener.Start();

                await ValidateValidUser(url, listener);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.HasHttpApi))]
        public async Task TestAnonymousAuthenticationWithDelegate()
        {
            string url = UrlPrefix.CreateLocal();
            HttpListener listener = new HttpListener();
            try
            {
                listener.Prefixes.Add(url);
                listener.AuthenticationSchemes = AuthenticationSchemes.None;
                AuthenticationSchemeSelector selector = new AuthenticationSchemeSelector(SelectAnonymousScheme);
                listener.AuthenticationSchemeSelectorDelegate += selector;
                listener.Start();

                await ValidateNullUser(url, listener);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        private async Task ValidateNullUser(string url, HttpListener listener)
        {
            var serverContextTask = listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                var clientTask = client.GetStringAsync(url);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Null(listenerContext.User);
            }
        }

        private async Task ValidateValidUser(string url, HttpListener listener)
        {
            var serverContextTask = listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                var clientTask = client.GetStringAsync(url);
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
