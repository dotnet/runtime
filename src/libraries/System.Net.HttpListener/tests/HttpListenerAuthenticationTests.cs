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
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpListenerAuthenticationTests : IDisposable
    {
        private const string Basic = "Basic";
        private const string TestUser = "testuser";
        private const string TestPassword = "testpassword";

        private HttpListenerFactory _factory;
        private HttpListener _listener;

        public HttpListenerAuthenticationTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
        }

        public void Dispose() => _factory.Dispose();

        // [ActiveIssue(20840, TestPlatforms.Unix)] // Managed implementation connects successfully.
        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
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

        // [ActiveIssue(20840, TestPlatforms.Unix)] Managed implementation connects successfully.
        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        [InlineData("Basic")]
        [InlineData("NTLM")]
        [InlineData("Negotiate")]
        [InlineData("Unknown")]
        public async Task NoAuthenticationGetContextAsync_AuthenticationProvided_ReturnsForbiddenStatusCode(string headerType)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.None;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(headerType, "body");
                await AuthenticationFailureAsyncContext(client, HttpStatusCode.Forbidden);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.None)]
        [InlineData(AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous)]
        public async Task BasicAuthentication_ValidUsernameAndPassword_Success(AuthenticationSchemes authScheme)
        {
            _listener.AuthenticationSchemes = authScheme;
            await ValidateValidUser();
        }

        [Theory]
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

        [Fact]
        public async Task TestAnonymousAuthentication()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            await ValidateNullUser();
        }

        [Fact]
        public async Task TestBasicAuthenticationWithDelegate()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.None;
            AuthenticationSchemeSelector selector = new AuthenticationSchemeSelector(SelectAnonymousAndBasicSchemes);
            _listener.AuthenticationSchemeSelectorDelegate += selector;

            await ValidateValidUser();
        }

        [Theory]
        [InlineData("somename:somepassword", "somename", "somepassword")]
        [InlineData("somename:", "somename", "")]
        [InlineData(":somepassword", "", "somepassword")]
        [InlineData("somedomain\\somename:somepassword", "somedomain\\somename", "somepassword")]
        [InlineData("\\somename:somepassword", "\\somename", "somepassword")]
        public async Task TestBasicAuthenticationWithValidAuthStrings(string authString, string expectedName, string expectedPassword)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            await ValidateValidUser(authString, expectedName, expectedPassword);
        }

        [Fact]
        public async Task TestAnonymousAuthenticationWithDelegate()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.None;
            AuthenticationSchemeSelector selector = new AuthenticationSchemeSelector(SelectAnonymousScheme);
            _listener.AuthenticationSchemeSelectorDelegate += selector;

            await ValidateNullUser();
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support NTLM")]
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

        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support NTLM")]
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

        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support Negotiate")]
        [MemberData(nameof(InvalidNtlmNegotiateAuthentication_TestData))]
        public async Task NegotiateAuthentication_InvalidRequestHeaders_ReturnsExpectedStatusCode(string header, HttpStatusCode statusCode)
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Negotiate", header);

                HttpResponseMessage message = await AuthenticationFailure(client, statusCode);
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    Assert.NotEmpty(message.Headers.WwwAuthenticate);
                }
                else
                {
                    Assert.Empty(message.Headers.WwwAuthenticate);
                }
            }
        }

        [Fact]
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

        [Fact]
        public async Task AuthenticationSchemeSelectorDelegate_ThrowsException_SendsInternalServerErrorToClient()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            _listener.AuthenticationSchemeSelectorDelegate = (request) => { throw new InvalidOperationException(); };

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await AuthenticationFailure(client, HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
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

        [Fact]
        public void AuthenticationSchemeSelectorDelegate_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.AuthenticationSchemeSelectorDelegate = null);
        }

        [Fact]
        public void AuthenticationSchemes_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.AuthenticationSchemes = AuthenticationSchemes.Basic);
        }

        [Fact]
        public void ExtendedProtectionPolicy_SetNull_ThrowsArgumentNullException()
        {
            using (var listener = new HttpListener())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => listener.ExtendedProtectionPolicy = null);
            }
        }

        [Fact]
        public void ExtendedProtectionPolicy_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.ExtendedProtectionPolicy = null);
        }

        [Fact]
        public void ExtendedProtectionPolicy_SetCustomChannelBinding_ThrowsObjectDisposedException()
        {
            using (var listener = new HttpListener())
            {
                var protectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always, new CustomChannelBinding());
                AssertExtensions.Throws<ArgumentException>("value", "CustomChannelBinding", () => listener.ExtendedProtectionPolicy = protectionPolicy);
            }
        }
        
        [Fact]
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

        [Fact]
        public void UnsafeConnectionNtlmAuthentication_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.UnsafeConnectionNtlmAuthentication = false);
        }

        [Fact]
        public void ExtendedProtectionSelectorDelegate_SetNull_ThrowsArgumentNullException()
        {
            using (var listener = new HttpListener())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", null, () => listener.ExtendedProtectionSelectorDelegate = null);
            }
        }

        [Fact]
        public void ExtendedProtectionSelectorDelegate_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.ExtendedProtectionSelectorDelegate = null);
        }

        [Fact]
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

        [Fact]
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

        public async Task<HttpResponseMessage> AuthenticationFailureAsyncContext(HttpClient client, HttpStatusCode errorCode)
        {
            Task<HttpResponseMessage> clientTask = client.GetAsync(_factory.ListeningUrl);
            Task<HttpListenerContext> serverTask = _listener.GetContextAsync();

            Task resultTask = await Task.WhenAny(clientTask, serverTask);
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

        private Task ValidateValidUser() =>
            ValidateValidUser(string.Format("{0}:{1}", TestUser, TestPassword), TestUser, TestPassword);

        private async Task ValidateValidUser(string authHeader, string expectedUsername, string expectedPassword)
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(authHeader)));

                Task <string> clientTask = client.GetStringAsync(_factory.ListeningUrl);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Equal(expectedUsername, listenerContext.User.Identity.Name);
                Assert.Equal(!string.IsNullOrEmpty(expectedUsername), listenerContext.User.Identity.IsAuthenticated);
                Assert.Equal(Basic, listenerContext.User.Identity.AuthenticationType);

                HttpListenerBasicIdentity id = Assert.IsType<HttpListenerBasicIdentity>(listenerContext.User.Identity);
                Assert.Equal(expectedPassword, id.Password);
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
