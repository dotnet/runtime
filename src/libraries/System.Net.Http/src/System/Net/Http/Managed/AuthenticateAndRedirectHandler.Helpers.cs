// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class AuthenticateAndRedirectHandler : HttpMessageHandler
    {
        private AuthenticationHelper.DigestResponse _digestResponse;

        private bool RequestNeedsRedirect(HttpResponseMessage response)
        {
            // Return if redirect not allowed.
            if (!_allowRedirect)
                return false;

            bool needRedirect = false;
            switch (response.StatusCode)
            {
                case HttpStatusCode.Moved:
                case HttpStatusCode.Found:
                case HttpStatusCode.SeeOther:
                case HttpStatusCode.TemporaryRedirect:
                    needRedirect = true;
                    break;

                case HttpStatusCode.MultipleChoices:
                    needRedirect = response.Headers.Location != null; // Don't redirect if no Location specified
                    break;
            }

            return needRedirect;
        }

        private static bool RequestRequiresForceGet(HttpStatusCode statusCode, HttpMethod requestMethod)
        {
            if (statusCode == HttpStatusCode.Moved ||
                statusCode == HttpStatusCode.Found ||
                statusCode == HttpStatusCode.SeeOther ||
                statusCode == HttpStatusCode.MultipleChoices)
            {
                return requestMethod == HttpMethod.Post;
            }

            return false;
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, bool useCredentialCache, CancellationToken cancellationToken)
        {
            if (_credentials != null && _preAuthenticate)
            {
                // Try using previous digest response WWWAuthenticate header
                if (_digestResponse != null)
                {
                    await AuthenticationHelper.TrySetDigestAuthToken(request, _credentials, _digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false);
                }
                else
                {
                    AuthenticationHelper.TrySetBasicAuthToken(request, _credentials);
                }
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);
            ICredentials currentCredential = useCredentialCache ? _credentials as CredentialCache : _credentials;

            if (currentCredential != null && !_preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HttpHeaderValueCollection<AuthenticationHeaderValue> authenticateValues = response.Headers.WwwAuthenticate;

                foreach (AuthenticationHeaderValue h in authenticateValues)
                {
                    // We only support Basic and digest auth, ignore others
                    if (h.Scheme == AuthenticationHelper.Basic)
                    {
                        if (AuthenticationHelper.TrySetBasicAuthToken(request, currentCredential))
                        {
                            response.Dispose();
                            response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }
                    else if (h.Scheme == AuthenticationHelper.Digest)
                    {
                        // Update digest response with new parameter from WWWAuthenticate
                        _digestResponse = new AuthenticationHelper.DigestResponse(h.Parameter);
                        if (await AuthenticationHelper.TrySetDigestAuthToken(request, currentCredential, _digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
                        {
                            response.Dispose();
                            response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);

                            // Retry in case of nonce timeout in server.
                            if (response.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                foreach (AuthenticationHeaderValue ahv in response.Headers.WwwAuthenticate)
                                {
                                    if (ahv.Scheme == AuthenticationHelper.Digest)
                                    {
                                        _digestResponse = new AuthenticationHelper.DigestResponse(ahv.Parameter);
                                        if (AuthenticationHelper.IsServerNonceStale(_digestResponse) &&
                                            await AuthenticationHelper.TrySetDigestAuthToken(request, currentCredential, _digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
                                        {
                                            response.Dispose();
                                            response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                                        }

                                        break;
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
            }

            return response;
        }
    }
}

