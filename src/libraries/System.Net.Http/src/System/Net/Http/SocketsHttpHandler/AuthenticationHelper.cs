// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class AuthenticationHelper
    {
        private const string BasicScheme = "Basic";
        private const string DigestScheme = "Digest";
        private const string NtlmScheme = "NTLM";
        private const string NegotiateScheme = "Negotiate";

        private enum AuthenticationType
        {
            Basic,
            Digest,
            Ntlm,
            Negotiate
        }

        private readonly struct AuthenticationChallenge
        {
            public AuthenticationType AuthenticationType { get; }
            public string SchemeName { get; }
            public NetworkCredential Credential { get; }
            public string? ChallengeData { get; }

            public AuthenticationChallenge(AuthenticationType authenticationType, string schemeName, NetworkCredential credential, string? challenge)
            {
                AuthenticationType = authenticationType;
                SchemeName = schemeName;
                Credential = credential;
                ChallengeData = challenge;
            }
        }

        private static bool TryGetChallengeDataForScheme(string scheme, HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues, out string? challengeData)
        {
            foreach (AuthenticationHeaderValue ahv in authenticationHeaderValues)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(scheme, ahv.Scheme))
                {
                    // Note, a valid challenge can have challengeData == null
                    challengeData = ahv.Parameter;
                    return true;
                }
            }

            challengeData = null;
            return false;
        }

        // Helper function to determine if response is part of session-based authentication challenge.
        internal static bool IsSessionAuthenticationChallenge(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues = GetResponseAuthenticationHeaderValues(response, isProxyAuth: false);
            foreach (AuthenticationHeaderValue ahv in authenticationHeaderValues)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(NegotiateScheme, ahv.Scheme) || StringComparer.OrdinalIgnoreCase.Equals(NtlmScheme, ahv.Scheme))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetValidAuthenticationChallengeForScheme(string scheme, AuthenticationType authenticationType, Uri uri, ICredentials credentials,
            HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues, out AuthenticationChallenge challenge)
        {
            challenge = default;

            if (!TryGetChallengeDataForScheme(scheme, authenticationHeaderValues, out string? challengeData))
            {
                return false;
            }

            NetworkCredential? credential = credentials.GetCredential(uri, scheme);
            if (credential == null)
            {
                // We have no credential for this auth type, so we can't respond to the challenge.
                // We'll continue to look for a different auth type that we do have a credential for.
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.AuthenticationInfo(uri, $"Authentication scheme '{scheme}' supported by server, but not by client.");
                }
                return false;
            }

            challenge = new AuthenticationChallenge(authenticationType, scheme, credential, challengeData);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.AuthenticationInfo(uri, $"Authentication scheme '{scheme}' selected. Client username={challenge.Credential.UserName}");
            }
            return true;
        }

        private static bool TryGetAuthenticationChallenge(HttpResponseMessage response, bool isProxyAuth, Uri authUri, ICredentials credentials, out AuthenticationChallenge challenge)
        {
            if (!IsAuthenticationChallenge(response, isProxyAuth))
            {
                challenge = default;
                return false;
            }

            // Try to get a valid challenge for the schemes we support, in priority order.
            HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues = GetResponseAuthenticationHeaderValues(response, isProxyAuth);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.AuthenticationInfo(authUri, $"{(isProxyAuth ? "Proxy" : "Server")} authentication requested with WWW-Authenticate header value '{authenticationHeaderValues}'");
            }
            return
                TryGetValidAuthenticationChallengeForScheme(NegotiateScheme, AuthenticationType.Negotiate, authUri, credentials, authenticationHeaderValues, out challenge) ||
                TryGetValidAuthenticationChallengeForScheme(NtlmScheme, AuthenticationType.Ntlm, authUri, credentials, authenticationHeaderValues, out challenge) ||
                TryGetValidAuthenticationChallengeForScheme(DigestScheme, AuthenticationType.Digest, authUri, credentials, authenticationHeaderValues, out challenge) ||
                TryGetValidAuthenticationChallengeForScheme(BasicScheme, AuthenticationType.Basic, authUri, credentials, authenticationHeaderValues, out challenge);
        }

        private static bool TryGetRepeatedChallenge(HttpResponseMessage response, string scheme, bool isProxyAuth, out string? challengeData)
        {
            challengeData = null;

            if (!IsAuthenticationChallenge(response, isProxyAuth))
            {
                return false;
            }

            if (!TryGetChallengeDataForScheme(scheme, GetResponseAuthenticationHeaderValues(response, isProxyAuth), out challengeData))
            {
                // We got another challenge status code, but couldn't find the challenge for the scheme we're handling currently.
                // Just stop processing auth.
                return false;
            }

            return true;
        }

        private static bool IsAuthenticationChallenge(HttpResponseMessage response, bool isProxyAuth)
        {
            return isProxyAuth ?
                response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired :
                response.StatusCode == HttpStatusCode.Unauthorized;
        }

        private static HttpHeaderValueCollection<AuthenticationHeaderValue> GetResponseAuthenticationHeaderValues(HttpResponseMessage response, bool isProxyAuth)
        {
            return isProxyAuth ?
                response.Headers.ProxyAuthenticate :
                response.Headers.WwwAuthenticate;
        }

        private static void SetRequestAuthenticationHeaderValue(HttpRequestMessage request, AuthenticationHeaderValue headerValue, bool isProxyAuth)
        {
            if (isProxyAuth)
            {
                request.Headers.ProxyAuthorization = headerValue;
            }
            else
            {
                request.Headers.Authorization = headerValue;
            }
        }

        private static void SetBasicAuthToken(HttpRequestMessage request, NetworkCredential credential, bool isProxyAuth)
        {
            string authString = !string.IsNullOrEmpty(credential.Domain) ?
                credential.Domain + "\\" + credential.UserName + ":" + credential.Password :
                credential.UserName + ":" + credential.Password;

            string base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

            SetRequestAuthenticationHeaderValue(request, new AuthenticationHeaderValue(BasicScheme, base64AuthString), isProxyAuth);
        }

        private static async ValueTask<bool> TrySetDigestAuthToken(HttpRequestMessage request, NetworkCredential credential, DigestResponse digestResponse, bool isProxyAuth)
        {
            string? parameter = await GetDigestTokenForCredential(credential, request, digestResponse).ConfigureAwait(false);

            // Any errors in obtaining parameter return false and we don't proceed with auth
            if (string.IsNullOrEmpty(parameter))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.AuthenticationError(request.RequestUri, $"Unable to find 'Digest' authentication token when authenticating with {(isProxyAuth ? "proxy" : "server")}");
                }
                return false;
            }

            var headerValue = new AuthenticationHeaderValue(DigestScheme, parameter);
            SetRequestAuthenticationHeaderValue(request, headerValue, isProxyAuth);
            return true;
        }

        private static ValueTask<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, bool async, bool isProxyAuth, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            return isProxyAuth ?
                pool.SendWithVersionDetectionAndRetryAsync(request, async, doRequestAuth, cancellationToken) :
                pool.SendWithProxyAuthAsync(request, async, doRequestAuth, cancellationToken);
        }

        private static async ValueTask<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request, Uri authUri, bool async, ICredentials credentials, bool preAuthenticate, bool isProxyAuth, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            // If preauth is enabled and this isn't proxy auth, try to get a basic credential from the
            // preauth credentials cache, and if successful, set an auth header for it onto the request.
            // Currently we only support preauth for Basic.
            bool performedBasicPreauth = false;
            if (preAuthenticate)
            {
                Debug.Assert(pool.PreAuthCredentials != null);
                NetworkCredential? credential;
                lock (pool.PreAuthCredentials)
                {
                    // Just look for basic credentials.  If in the future we support preauth
                    // for other schemes, this will need to search in order of precedence.
                    Debug.Assert(pool.PreAuthCredentials.GetCredential(authUri, NegotiateScheme) == null);
                    Debug.Assert(pool.PreAuthCredentials.GetCredential(authUri, NtlmScheme) == null);
                    Debug.Assert(pool.PreAuthCredentials.GetCredential(authUri, DigestScheme) == null);
                    credential = pool.PreAuthCredentials.GetCredential(authUri, BasicScheme);
                }

                if (credential != null)
                {
                    SetBasicAuthToken(request, credential, isProxyAuth);
                    performedBasicPreauth = true;
                }
            }

            HttpResponseMessage response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);

            if (TryGetAuthenticationChallenge(response, isProxyAuth, authUri, credentials, out AuthenticationChallenge challenge))
            {
                switch (challenge.AuthenticationType)
                {
                    case AuthenticationType.Digest:
                        var digestResponse = new DigestResponse(challenge.ChallengeData);
                        if (await TrySetDigestAuthToken(request, challenge.Credential, digestResponse, isProxyAuth).ConfigureAwait(false))
                        {
                            response.Dispose();
                            response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);

                            // Retry in case of nonce timeout in server.
                            if (TryGetRepeatedChallenge(response, challenge.SchemeName, isProxyAuth, out string? challengeData))
                            {
                                digestResponse = new DigestResponse(challengeData);
                                if (IsServerNonceStale(digestResponse) &&
                                    await TrySetDigestAuthToken(request, challenge.Credential, digestResponse, isProxyAuth).ConfigureAwait(false))
                                {
                                    response.Dispose();
                                    response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                        break;

                    case AuthenticationType.Basic:
                        if (performedBasicPreauth)
                        {
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.AuthenticationError(authUri, $"Pre-authentication with {(isProxyAuth ? "proxy" : "server")} failed.");
                            }
                            break;
                        }

                        response.Dispose();
                        SetBasicAuthToken(request, challenge.Credential, isProxyAuth);
                        response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);

                        if (preAuthenticate)
                        {
                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.ProxyAuthenticationRequired:
                                case HttpStatusCode.Unauthorized:
                                    if (NetEventSource.Log.IsEnabled())
                                    {
                                        NetEventSource.AuthenticationError(authUri, $"Pre-authentication with {(isProxyAuth ? "proxy" : "server")} failed.");
                                    }
                                    break;

                                default:
                                    lock (pool.PreAuthCredentials!)
                                    {
                                        try
                                        {
                                            if (NetEventSource.Log.IsEnabled())
                                            {
                                                NetEventSource.Info(pool.PreAuthCredentials, $"Adding Basic credential to cache, uri={authUri}, username={challenge.Credential.UserName}");
                                            }
                                            pool.PreAuthCredentials.Add(authUri, BasicScheme, challenge.Credential);
                                        }
                                        catch (ArgumentException)
                                        {
                                            // The credential already existed.
                                            if (NetEventSource.Log.IsEnabled())
                                            {
                                                NetEventSource.Info(pool.PreAuthCredentials, $"Basic credential present in cache, uri={authUri}, username={challenge.Credential.UserName}");
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            if (NetEventSource.Log.IsEnabled() && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                NetEventSource.AuthenticationError(authUri, $"{(isProxyAuth ? "Proxy" : "Server")} authentication failed.");
            }

            return response;
        }

        public static ValueTask<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, bool async, ICredentials proxyCredentials, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            return SendWithAuthAsync(request, proxyUri, async, proxyCredentials, preAuthenticate: false, isProxyAuth: true, doRequestAuth, pool, cancellationToken);
        }

        public static ValueTask<HttpResponseMessage> SendWithRequestAuthAsync(HttpRequestMessage request, bool async, ICredentials credentials, bool preAuthenticate, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            Debug.Assert(request.RequestUri != null);
            return SendWithAuthAsync(request, request.RequestUri, async, credentials, preAuthenticate, isProxyAuth: false, doRequestAuth: true, pool, cancellationToken);
        }
    }
}
