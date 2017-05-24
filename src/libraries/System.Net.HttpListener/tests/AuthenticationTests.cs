// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading;
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

        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // Managed implementation connects successfully.
        [InlineData("Basic")]
        [InlineData("NTLM")]
        [InlineData("Negotiate")]
        [InlineData("Unknown")]
        public async Task NoAuthentication_AuthenticationProvided_ReturnsForbiddenStatusCode(string headerType)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.None;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(headerType, "body");
                await AuthenticationFailure(client, HttpStatusCode.Forbidden);
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.None)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous)]
        public async Task BasicAuthentication_ValidUsernameAndPassword_Success(AuthenticationSchemes authScheme)
        {
            _listener.AuthenticationSchemes = authScheme;
            await ValidateValidUser();
        }

        [ActiveIssue(19967, TargetFrameworkMonikers.NetFramework)]
        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [ActiveIssue(20099, TestPlatforms.Unix)]
        [MemberData(nameof(BasicAuthenticationHeader_TestData))]
        public async Task BasicAuthentication_InvalidRequest_SendsStatusCodeClient(string header, HttpStatusCode statusCode)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Basic, header);

                HttpResponseMessage response = await AuthenticationFailure(client, statusCode);

                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    Assert.Equal("Basic realm=\"\"", response.Headers.WwwAuthenticate.ToString());
                }
                else
                {
                    Assert.Empty(response.Headers.WwwAuthenticate);
                }
            }
        }

        public static IEnumerable<object[]> BasicAuthenticationHeader_TestData()
        {
            yield return new object[] { string.Empty, HttpStatusCode.Unauthorized };
            yield return new object[] { null, HttpStatusCode.Unauthorized };
            yield return new object[] { Convert.ToBase64String(Encoding.ASCII.GetBytes("username")), HttpStatusCode.BadRequest };
            yield return new object[] { "abc", HttpStatusCode.InternalServerError };
        }

        [ActiveIssue(19967, TargetFrameworkMonikers.NetFramework)]
        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [ActiveIssue(20098, TestPlatforms.Unix)]
        [InlineData("ExampleRealm")]
        [InlineData("  ExampleRealm  ")]
        [InlineData("")]
        [InlineData(null)]
        public async Task BasicAuthentication_RealmSet_SendsChallengeToClient(string realm)
        {
            _listener.Realm = realm;
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            Assert.Equal(realm, _listener.Realm);

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await AuthenticationFailure(client, HttpStatusCode.Unauthorized);
                Assert.Equal($"Basic realm=\"{realm}\"", response.Headers.WwwAuthenticate.ToString());
            }
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

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support NTLM")]
        [ActiveIssue(20096)]
        public async Task NtlmAuthentication_Conversation_ReturnsExpectedType2Message()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("NTLM", "TlRMTVNTUAABAAAABzIAAAYABgArAAAACwALACAAAABXT1JLU1RBVElPTkRPTUFJTg==");

                HttpResponseMessage message = await AuthenticationFailure(client, HttpStatusCode.Unauthorized);
                Assert.StartsWith("NTLM", message.Headers.WwwAuthenticate.ToString());
            }
        }

        public static IEnumerable<object[]> InvalidNtlmNegotiateAuthentication_TestData()
        {
            yield return new object[] { null, HttpStatusCode.Unauthorized };
            yield return new object[] { string.Empty, HttpStatusCode.Unauthorized };
            yield return new object[] { "abc", HttpStatusCode.BadRequest };
            yield return new object[] { "abcd", HttpStatusCode.BadRequest };
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support NTLM")]
        [ActiveIssue(20096)]
        [MemberData(nameof(InvalidNtlmNegotiateAuthentication_TestData))]
        public async Task NtlmAuthentication_InvalidRequestHeaders_ReturnsExpectedStatusCode(string header, HttpStatusCode statusCode)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("NTLM", header);

                HttpResponseMessage message = await AuthenticationFailure(client, statusCode);
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    Assert.Equal("NTLM", message.Headers.WwwAuthenticate.ToString());
                }
                else
                {
                    Assert.Empty(message.Headers.WwwAuthenticate);
                }
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support Negotiate")]
        [ActiveIssue(20096)]
        public async Task NegotiateAuthentication_Conversation_ReturnsExpectedType2Message()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Negotiate", "TlRMTVNTUAABAAAABzIAAAYABgArAAAACwALACAAAABXT1JLU1RBVElPTkRPTUFJTg==");

                HttpResponseMessage message = await AuthenticationFailure(client, HttpStatusCode.Unauthorized);
                Assert.StartsWith("Negotiate", message.Headers.WwwAuthenticate.ToString());
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support Negotiate")]
        [ActiveIssue(20096)]
        [MemberData(nameof(InvalidNtlmNegotiateAuthentication_TestData))]
        public async Task NegotiateAuthentication_InvalidRequestHeaders_ReturnsExpectedStatusCode(string header, HttpStatusCode statusCode)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Negotiate", header);

                HttpResponseMessage message = await AuthenticationFailure(client, statusCode);
                Assert.Empty(message.Headers.WwwAuthenticate);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task AuthenticationSchemeSelectorDelegate_ReturnsInvalidAuthenticationScheme_PerformsNoAuthentication()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            _listener.AuthenticationSchemeSelectorDelegate = (request) => (AuthenticationSchemes)(-1);

            using (var client = new HttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.GetAsync(_factory.ListeningUrl);
                HttpListenerContext context = await _listener.GetContextAsync();

                Assert.False(context.Request.IsAuthenticated);
                context.Response.Close();

                await clientTask;
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task AuthenticationSchemeSelectorDelegate_ThrowsException_SendsInternalServerErrorToClient()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            _listener.AuthenticationSchemeSelectorDelegate = (request) => { throw new InvalidOperationException(); };

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await AuthenticationFailure(client, HttpStatusCode.InternalServerError);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void AuthenticationSchemeSelectorDelegate_ThrowsOutOfMemoryException_RethrowsException()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            _listener.AuthenticationSchemeSelectorDelegate = (request) => { throw new OutOfMemoryException(); };

            using (var client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);
                Assert.Throws<OutOfMemoryException>(() => _listener.GetContext());
            }
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
        
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
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
        public async Task Realm_SetWithoutBasicAuthenticationScheme_SendsNoChallengeToClient()
        {
            _listener.Realm = "ExampleRealm";

            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.GetAsync(_factory.ListeningUrl);
                HttpListenerContext context = await _listener.GetContextAsync();
                context.Response.Close();

                HttpResponseMessage response = await clientTask;
                Assert.Empty(response.Headers.WwwAuthenticate);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void Realm_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.Realm = null);
        }

        public async Task<HttpResponseMessage> AuthenticationFailure(HttpClient client, HttpStatusCode errorCode)
        {
            Task<HttpResponseMessage> clientTask = client.GetAsync(_factory.ListeningUrl);

            // The server task will hang forever if it is not cancelled.
            var tokenSource = new CancellationTokenSource();
            Task<HttpListenerContext> serverTask = Task.Run(() => _listener.GetContext(), tokenSource.Token);

            // The client task should complete first - the server should send a 401 response.
            Task resultTask = await Task.WhenAny(clientTask, serverTask);
            tokenSource.Cancel();
            if (resultTask == serverTask)
            {
                await serverTask;
            }

            Assert.Same(clientTask, resultTask);

            Assert.Equal(errorCode, clientTask.Result.StatusCode);
            return clientTask.Result;
        }

        private async Task ValidateNullUser()
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Null(listenerContext.User);
            }
        }

        private async Task ValidateValidUser()
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Equal(TestUser, listenerContext.User.Identity.Name);
                Assert.True(listenerContext.User.Identity.IsAuthenticated);
                Assert.Equal(Basic, listenerContext.User.Identity.AuthenticationType);
            }
        }

        private AuthenticationSchemes SelectAnonymousAndBasicSchemes(HttpListenerRequest request) => AuthenticationSchemes.Anonymous | AuthenticationSchemes.Basic;

        private AuthenticationSchemes SelectAnonymousScheme(HttpListenerRequest request) => AuthenticationSchemes.Anonymous;

        private class CustomChannelBinding : ChannelBinding
        {
            public override int Size => 0;
            protected override bool ReleaseHandle() => true;
        }
    }
}
