// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class AuthenticationHelper
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
            public string ChallengeData { get; }

            public AuthenticationChallenge(AuthenticationType authenticationType, string schemeName, NetworkCredential credential, string challenge)
            {
                AuthenticationType = authenticationType;
                SchemeName = schemeName;
                Credential = credential;
                ChallengeData = challenge;
            }
        }

        private static bool TryGetChallengeDataForScheme(string scheme, HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues, out string challengeData)
        {
            challengeData = null;

            foreach (AuthenticationHeaderValue ahv in authenticationHeaderValues)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(scheme, ahv.Scheme))
                {
                    // Note, a valid challenge can have challengeData == null
                    challengeData = ahv.Parameter;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetValidAuthenticationChallengeForScheme(string scheme, AuthenticationType authenticationType, Uri uri, ICredentials credentials,
            HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues, out AuthenticationChallenge challenge)
        {
            challenge = default;

            if (!TryGetChallengeDataForScheme(scheme, authenticationHeaderValues, out string challengeData))
            { 
                return false;
            }

            NetworkCredential credential = credentials.GetCredential(uri, scheme);
            if (credential == null)
            {
                // We have no credential for this auth type, so we can't response to the challenge.
                // We'll continue to look for a different auth type that we do have a credential for.
                return false;
            }

            challenge = new AuthenticationChallenge(authenticationType, scheme, credential, challengeData);
            return true;
        }

        private static bool TryGetAuthenticationChallenge(HttpResponseMessage response, bool isProxyAuth, Uri authUri, ICredentials credentials, out AuthenticationChallenge challenge)
        {
            challenge = default;

            if (!IsAuthenticationChallenge(response, isProxyAuth))
            {
                return false;
            }

            HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues = GetResponseAuthenticationHeaderValues(response, isProxyAuth);

            // Try to get a valid challenge for the schemes we support, in priority order.
            if (!TryGetValidAuthenticationChallengeForScheme(NegotiateScheme, AuthenticationType.Negotiate, authUri, credentials, authenticationHeaderValues, out challenge) &&
                !TryGetValidAuthenticationChallengeForScheme(NtlmScheme, AuthenticationType.Ntlm, authUri, credentials, authenticationHeaderValues, out challenge) &&
                !TryGetValidAuthenticationChallengeForScheme(DigestScheme, AuthenticationType.Digest, authUri, credentials, authenticationHeaderValues, out challenge) &&
                !TryGetValidAuthenticationChallengeForScheme(BasicScheme, AuthenticationType.Basic, authUri, credentials, authenticationHeaderValues, out challenge))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetRepeatedChallenge(HttpResponseMessage response, string scheme, bool isProxyAuth, out string challengeData)
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
            SetRequestAuthenticationHeaderValue(request, new AuthenticationHeaderValue(BasicScheme, GetBasicTokenForCredential(credential)), isProxyAuth);
        }

        private static async Task<bool> TrySetDigestAuthToken(HttpRequestMessage request, NetworkCredential credential, DigestResponse digestResponse, bool isProxyAuth)
        {
            string parameter = await GetDigestTokenForCredential(credential, request, digestResponse).ConfigureAwait(false);

            // Any errors in obtaining parameter return false and we don't proceed with auth
            if (string.IsNullOrEmpty(parameter))
            {
                return false;
            }

            var headerValue = new AuthenticationHeaderValue(DigestScheme, parameter);
            SetRequestAuthenticationHeaderValue(request, headerValue, isProxyAuth);
            return true;
        }

        private static Task<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, bool isProxyAuth, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            return isProxyAuth ?
                pool.SendWithRetryAsync(request, doRequestAuth, cancellationToken) :
                pool.SendWithProxyAuthAsync(request, doRequestAuth, cancellationToken);
        }

        private static async Task<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request, Uri authUri, ICredentials credentials, bool preAuthenticate, bool isProxyAuth, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            if (preAuthenticate)
            {
                NetworkCredential credential = credentials.GetCredential(authUri, BasicScheme);
                if (credential != null)
                {
                    SetBasicAuthToken(request, credential, isProxyAuth);
                }
            }

            HttpResponseMessage response = await InnerSendAsync(request, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);

            if (TryGetAuthenticationChallenge(response, isProxyAuth, authUri, credentials, out AuthenticationChallenge challenge))
            {
                if (challenge.AuthenticationType == AuthenticationType.Digest)
                {
                    var digestResponse = new DigestResponse(challenge.ChallengeData);
                    if (await TrySetDigestAuthToken(request, challenge.Credential, digestResponse, isProxyAuth).ConfigureAwait(false))
                    {
                        response.Dispose();
                        response = await InnerSendAsync(request, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);

                        // Retry in case of nonce timeout in server.
                        if (TryGetRepeatedChallenge(response, challenge.SchemeName, isProxyAuth, out string challengeData))
                        {
                            digestResponse = new DigestResponse(challengeData);
                            if (IsServerNonceStale(digestResponse) &&
                                await TrySetDigestAuthToken(request, challenge.Credential, digestResponse, isProxyAuth).ConfigureAwait(false))
                            {
                                response.Dispose();
                                response = await InnerSendAsync(request, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }
                else if (challenge.AuthenticationType == AuthenticationType.Basic)
                {
                    if (!preAuthenticate)
                    {
                        SetBasicAuthToken(request, challenge.Credential, isProxyAuth);

                        response.Dispose();
                        response = await InnerSendAsync(request, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return response;
        }

        public static Task<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, ICredentials proxyCredentials, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            return SendWithAuthAsync(request, proxyUri, proxyCredentials, false, true, doRequestAuth, pool, cancellationToken);
        }

        public static Task<HttpResponseMessage> SendWithRequestAuthAsync(HttpRequestMessage request, ICredentials credentials, bool preAuthenticate, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            return SendWithAuthAsync(request, request.RequestUri, credentials, preAuthenticate, false, true, pool, cancellationToken);
        }
    }
}

