// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Tests
{
    [SkipOnCoreClr("System.Net.Tests may timeout in stress configurations", ~RuntimeConfiguration.Release)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2391", TestRuntimes.Mono)]
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

        // [ActiveIssue("https://github.com/dotnet/runtime/issues/22195", TestPlatforms.Unix)] // Managed implementation connects successfully.
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))]
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

        // [ActiveIssue("https://github.com/dotnet/runtime/issues/22195", TestPlatforms.Unix)] Managed implementation connects successfully.
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))]
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
            yield return new object[] { Convert.ToBase64String("username"u8), HttpStatusCode.BadRequest };
            yield return new object[] { "abc", HttpStatusCode.InternalServerError };
        }

        [Theory]
        [InlineData("ExampleRealm")]
        [InlineData("  ExampleRealm  ")]
        [InlineData("")]
        [InlineData(null)]
        public async Task BasicAuthentication_RealmSet_SendsChallengeToClient(string? realm)
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

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support NTLM")]
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

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support NTLM")]
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

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support Negotiate")]
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

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))] // [PlatformSpecific(TestPlatforms.Windows, "Managed impl doesn't support Negotiate")]
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

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsWindowsImplementation))]
        public async Task ExtendedProtectionSelectorDelegate_IncreasesPolicyBetweenNtlmLegs_AuthenticationFails()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;

            ExtendedProtectionPolicy relaxedPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            ExtendedProtectionPolicy strictPolicy =
                new ExtendedProtectionPolicy(
                    PolicyEnforcement.Always,
                    ProtectionScenario.TransportSelected,
                    new ServiceNameCollection(new[] { "HTTP/strict-only" }));

            _listener.ExtendedProtectionSelectorDelegate = request =>
                request.QueryString["strict"] == "1" ? strictPolicy : relaxedPolicy;

            NtlmHandshakeResult baselineResult = await TryCompleteNtlmOverSingleConnection(secondLegStrict: false);
            if (baselineResult == NtlmHandshakeResult.CredentialsUnavailable)
            {
                throw new SkipTestException("Unable to establish baseline NTLM authentication with default credentials.");
            }

            Assert.Equal(NtlmHandshakeResult.Authenticated, baselineResult);

            NtlmHandshakeResult strictSecondLegResult = await TryCompleteNtlmOverSingleConnection(secondLegStrict: true);
            Assert.Equal(NtlmHandshakeResult.Unauthorized, strictSecondLegResult);
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
                await AuthenticationFailure(client, HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public void AuthenticationSchemeSelectorDelegate_ThrowsOutOfMemoryException_RethrowsException()
        {
            _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            _listener.AuthenticationSchemeSelectorDelegate = (request) => { throw new OutOfMemoryException(); };

            using (var client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);
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
                Assert.False(listener.UnsafeConnectionNtlmAuthentication);

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
            Task<HttpListenerContext> serverTask = _listener.GetContextAsync();

            Task resultTask = await Task.WhenAny(clientTask, serverTask);
            if (resultTask == serverTask)
            {
                await serverTask;
            }

            Assert.Same(clientTask, resultTask);

            HttpResponseMessage response = await clientTask;
            Assert.Equal(errorCode, response.StatusCode);

            return response;
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

            HttpResponseMessage response = await clientTask;
            Assert.Equal(errorCode, response.StatusCode);

            return response;
        }

        private async Task ValidateNullUser()
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Http.Headers.AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TestUser, TestPassword))));

                _ = client.GetStringAsync(_factory.ListeningUrl);
                HttpListenerContext listenerContext = await serverContextTask;

                Assert.Null(listenerContext.User);
            }
        }

        private Task ValidateValidUser() =>
            ValidateValidUser(string.Format("{0}:{1}", TestUser, TestPassword), TestUser, TestPassword);

        private async Task<NtlmHandshakeResult> TryCompleteNtlmOverSingleConnection(bool secondLegStrict)
        {
            using Socket client = _factory.GetConnectedSocket();
            client.ReceiveTimeout = 15000;
            client.SendTimeout = 15000;

            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            NegotiateAuthenticationClientOptions clientOptions =
                new NegotiateAuthenticationClientOptions
                {
                    Package = "NTLM",
                    Credential = CredentialCache.DefaultNetworkCredentials,
                    TargetName = "HTTP/lax-target"
                };

            using NegotiateAuthentication clientContext = new NegotiateAuthentication(clientOptions);

            byte[]? type1 = clientContext.GetOutgoingBlob(ReadOnlySpan<byte>.Empty, out NegotiateAuthenticationStatusCode type1Status);
            if (type1 is null || type1Status != NegotiateAuthenticationStatusCode.ContinueNeeded)
            {
                return NtlmHandshakeResult.UnexpectedFailure;
            }

            Task<ResponseHeaders> firstResponseTask = Task.Run(() =>
                SendRequestAndReadHeaders(client, CreateNtlmRequest(Convert.ToBase64String(type1), strict: false)));

            Task firstCompletedTask = await Task.WhenAny(serverContextTask, firstResponseTask);
            if (firstCompletedTask == serverContextTask)
            {
                HttpListenerContext unexpectedContext = await serverContextTask;
                unexpectedContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                unexpectedContext.Response.Close();
                return NtlmHandshakeResult.UnexpectedFailure;
            }

            ResponseHeaders firstResponse = await firstResponseTask;
            if (firstResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return NtlmHandshakeResult.UnexpectedFailure;
            }

            string? challenge = GetNtlmChallenge(firstResponse.Headers);
            if (challenge is null)
            {
                return NtlmHandshakeResult.UnexpectedFailure;
            }

            byte[]? type2 = Convert.FromBase64String(challenge);
            byte[]? type3 = clientContext.GetOutgoingBlob(type2, out NegotiateAuthenticationStatusCode type3Status);
            if (type3 is null)
            {
                return type3Status == NegotiateAuthenticationStatusCode.UnknownCredentials
                    ? NtlmHandshakeResult.CredentialsUnavailable
                    : NtlmHandshakeResult.UnexpectedFailure;
            }

            if (type3Status != NegotiateAuthenticationStatusCode.Completed)
            {
                return NtlmHandshakeResult.UnexpectedFailure;
            }

            Task<ResponseHeaders> secondResponseTask = Task.Run(() =>
                SendRequestAndReadHeaders(client, CreateNtlmRequest(Convert.ToBase64String(type3), secondLegStrict)));

            Task completedTask = await Task.WhenAny(serverContextTask, secondResponseTask);
            if (completedTask == serverContextTask)
            {
                HttpListenerContext context = await serverContextTask;
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                context.Response.Close();

                ResponseHeaders successfulResponse = await secondResponseTask;
                return successfulResponse.StatusCode == HttpStatusCode.NoContent
                    ? NtlmHandshakeResult.Authenticated
                    : NtlmHandshakeResult.UnexpectedFailure;
            }

            ResponseHeaders failedResponse = await secondResponseTask;
            return failedResponse.StatusCode == HttpStatusCode.Unauthorized
                ? NtlmHandshakeResult.Unauthorized
                : NtlmHandshakeResult.UnexpectedFailure;
        }

        private byte[] CreateNtlmRequest(string authBlob, bool strict)
        {
            string query = strict ? "?strict=1" : "?strict=0";
            string[] headers =
            [
                "Connection: keep-alive",
                $"Authorization: NTLM {authBlob}"
            ];

            return _factory.GetContent("1.1", "HEAD", query, text: null, headers, headerOnly: true);
        }

        private static string? GetNtlmChallenge(List<string> headers)
        {
            foreach (string header in headers)
            {
                if (!header.StartsWith("WWW-Authenticate:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = header.Substring("WWW-Authenticate:".Length).Trim();
                if (!value.StartsWith("NTLM ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return value.Substring("NTLM ".Length).Trim();
            }

            return null;
        }

        private static ResponseHeaders SendRequestAndReadHeaders(Socket client, byte[] requestBytes)
        {
            int totalSent = 0;
            while (totalSent < requestBytes.Length)
            {
                int sent = client.Send(requestBytes, totalSent, requestBytes.Length - totalSent, SocketFlags.None);
                if (sent == 0)
                {
                    throw new InvalidOperationException("Socket closed before request bytes were fully sent.");
                }

                totalSent += sent;
            }

            string headersText = ReadHeaders(client);
            int separatorIndex = headersText.IndexOf("\r\n", StringComparison.Ordinal);
            Assert.True(separatorIndex >= 0, "Response did not include a status line.");

            string statusLine = headersText.Substring(0, separatorIndex);
            string[] statusLineParts = statusLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(statusLineParts.Length >= 2, $"Invalid status line: '{statusLine}'");
            Assert.True(int.TryParse(statusLineParts[1], out int statusCode), $"Invalid status code in status line: '{statusLine}'");

            List<string> headerLines = new List<string>();
            int position = separatorIndex + 2;
            while (position < headersText.Length)
            {
                int lineEnd = headersText.IndexOf("\r\n", position, StringComparison.Ordinal);
                if (lineEnd < 0)
                {
                    break;
                }

                if (lineEnd == position)
                {
                    break;
                }

                headerLines.Add(headersText.Substring(position, lineEnd - position));
                position = lineEnd + 2;
            }

            return new ResponseHeaders((HttpStatusCode)statusCode, headerLines);
        }

        private static string ReadHeaders(Socket client)
        {
            StringBuilder builder = new StringBuilder();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = client.Receive(buffer);
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException("Socket closed before response headers were fully received.");
                }

                builder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                string response = builder.ToString();
                int headerEnd = response.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd >= 0)
                {
                    return response.Substring(0, headerEnd);
                }
            }
        }

        private enum NtlmHandshakeResult
        {
            Authenticated,
            Unauthorized,
            CredentialsUnavailable,
            UnexpectedFailure
        }

        private sealed record ResponseHeaders(HttpStatusCode StatusCode, List<string> Headers);

        private async Task ValidateValidUser(string authHeader, string expectedUsername, string expectedPassword)
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    Basic,
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(authHeader)));

                _ = client.GetStringAsync(_factory.ListeningUrl);
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
