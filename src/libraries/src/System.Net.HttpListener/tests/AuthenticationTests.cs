// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Security.Authentication.ExtendedProtection;
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

        public AuthenticationTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void AuthenticationSchemeSelectorDelegate_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.AuthenticationSchemeSelectorDelegate = null);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void AuthenticationSchemes_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.AuthenticationSchemes = AuthenticationSchemes.Basic);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void ExtendedProtectionPolicy_SetNull_ThrowsArgumentNullException()
        {
            using (var listener = new HttpListener())
            {
                Assert.Throws<ArgumentNullException>("value", () => listener.ExtendedProtectionPolicy = null);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void ExtendedProtectionPolicy_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.ExtendedProtectionPolicy = null);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void ExtendedProtectionPolicy_SetCustomChannelBinding_ThrowsObjectDisposedException()
        {
            using (var listener = new HttpListener())
            {
                var protectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, new CustomChannelBinding());
                AssertExtensions.Throws<ArgumentException>("value", "CustomChannelBinding", () => listener.ExtendedProtectionPolicy = protectionPolicy);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UnsafeConnectionNtlmAuthentication_Unix_ThrowsPlatformNotSupportedException()
        {
            using (var listener = new HttpListener())
            {
                Assert.Throws<PlatformNotSupportedException>(() => listener.UnsafeConnectionNtlmAuthentication);
                Assert.Throws<PlatformNotSupportedException>(() => listener.UnsafeConnectionNtlmAuthentication = false);
            }
        }
        
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UnsafeConnectionNtlmAuthentication_SetGet_ReturnsExpected()
        {
            using (var listener = new HttpListener())
            {
                Assert.Equal(false, listener.UnsafeConnectionNtlmAuthentication);

                listener.UnsafeConnectionNtlmAuthentication = true;
                Assert.True(listener.UnsafeConnectionNtlmAuthentication);

                listener.UnsafeConnectionNtlmAuthentication = false;
                Assert.False(listener.UnsafeConnectionNtlmAuthentication);

                listener.UnsafeConnectionNtlmAuthentication = false;
                Assert.False(listener.UnsafeConnectionNtlmAuthentication);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UnsafeConnectionNtlmAuthentication_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.UnsafeConnectionNtlmAuthentication = false);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void ExtendedProtectionSelectorDelegate_SetNull_ThrowsArgumentNullException()
        {
            using (var listener = new HttpListener())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", null, () => listener.ExtendedProtectionSelectorDelegate = null);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void ExtendedProtectionSelectorDelegate_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.ExtendedProtectionSelectorDelegate = null);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void Realm_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.Realm = null);
        }

        private async Task ValidateNullUser()
        {
            var serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                var clientTask = client.GetStringAsync(_factory.ListeningUrl);
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

                var clientTask = client.GetStringAsync(_factory.ListeningUrl);
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

        private class CustomChannelBinding : ChannelBinding
        {
            public override int Size => 0;
            protected override bool ReleaseHandle() => true;
        }
    }
}
