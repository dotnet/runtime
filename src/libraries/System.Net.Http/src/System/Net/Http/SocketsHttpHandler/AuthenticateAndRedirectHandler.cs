// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class AuthenticateAndRedirectHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly bool _preAuthenticate;
        private readonly ICredentials _credentials;
        private readonly bool _allowRedirect;
        private readonly int _maxAutomaticRedirections;

        public AuthenticateAndRedirectHandler(bool preAuthenticate, ICredentials credentials, bool allowRedirect, int maxAutomaticRedirections, HttpMessageHandler innerHandler)
        {
            Debug.Assert(innerHandler != null);

            _preAuthenticate = preAuthenticate;
            _credentials = credentials;
            _allowRedirect = allowRedirect;

            if (allowRedirect && maxAutomaticRedirections < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAutomaticRedirections));
            }

            _maxAutomaticRedirections = maxAutomaticRedirections;
            _innerHandler = innerHandler;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            uint redirectCount = 0;
            while (true)
            {
                // Just as with WinHttpHandler and CurlHandler, for security reasons, we drop the server credential if it is
                // anything other than a CredentialCache on redirection. We allow credentials in a CredentialCache since they
                // are specifically tied to URIs.
                ICredentials currentCredential = redirectCount > 0 ? _credentials as CredentialCache : _credentials;

                if (currentCredential != null && _preAuthenticate)
                {
                    AuthenticationHelper.TrySetBasicAuthToken(request, currentCredential);
                }

                response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (currentCredential != null && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    AuthenticationHeaderValue selectedAuth = GetSupportedAuthScheme(response.Headers.WwwAuthenticate);
                    if (selectedAuth != null)
                    {
                        switch (selectedAuth.Scheme)
                        {
                            case AuthenticationHelper.Digest:
                                // Update digest response with new parameter from WWWAuthenticate
                                var digestResponse = new AuthenticationHelper.DigestResponse(selectedAuth.Parameter);
                                if (await AuthenticationHelper.TrySetDigestAuthToken(request, currentCredential, digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
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
                                                digestResponse = new AuthenticationHelper.DigestResponse(ahv.Parameter);
                                                if (AuthenticationHelper.IsServerNonceStale(digestResponse) &&
                                                    await AuthenticationHelper.TrySetDigestAuthToken(request, currentCredential, digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
                                                {
                                                    response.Dispose();
                                                    response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }
                                break;

                            case AuthenticationHelper.Basic:
                                if (_preAuthenticate)
                                {
                                    // We already tried these credentials via preauthentication, so no need to try again
                                    break;
                                }

                                if (AuthenticationHelper.TrySetBasicAuthToken(request, currentCredential))
                                {
                                    response.Dispose();
                                    response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                                }
                                break;
                        }
                    }
                }

                if (!RequestNeedsRedirect(response))
                {
                    break;
                }

                // Clear the authorization header, if the request requires redirect.
                request.Headers.Authorization = null;

                Uri location = response.Headers.Location;
                if (location == null)
                {
                    // No location header. Nothing to redirect to.
                    break;
                }

                if (!location.IsAbsoluteUri)
                {
                    location = new Uri(request.RequestUri, location);
                }

                // Disallow automatic redirection from secure to non-secure schemes
                bool allowed =
                    (HttpUtilities.IsSupportedNonSecureScheme(request.RequestUri.Scheme) && HttpUtilities.IsSupportedScheme(location.Scheme)) ||
                    (HttpUtilities.IsSupportedSecureScheme(request.RequestUri.Scheme) && HttpUtilities.IsSupportedSecureScheme(location.Scheme));
                if (!allowed)
                {
                    break;
                }

                redirectCount++;
                if (redirectCount > _maxAutomaticRedirections)
                {
                    throw new HttpRequestException(SR.net_http_max_redirects);
                }

                // Set up for the automatic redirect
                request.RequestUri = location;

                if (RequestRequiresForceGet(response.StatusCode, request.Method))
                {
                    request.Method = HttpMethod.Get;
                    request.Content = null;
                }

                // Do the redirect.
                response.Dispose();
            }

            return response;
        }

        private bool RequestNeedsRedirect(HttpResponseMessage response)
        {
            // Return if redirect is not requested.
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

        private static AuthenticationHeaderValue GetSupportedAuthScheme(HttpHeaderValueCollection<AuthenticationHeaderValue> authenticateValues)
        {
            AuthenticationHeaderValue basicAuthenticationHeaderValue = null;

            // Only Digest and Basic auth supported, ignore others.
            foreach (AuthenticationHeaderValue ahv in authenticateValues)
            {
                if (ahv.Scheme == AuthenticationHelper.Digest)
                {
                    return ahv;
                }
                else if (ahv.Scheme == AuthenticationHelper.Basic)
                {
                    basicAuthenticationHeaderValue = ahv;
                }
            }

            return basicAuthenticationHeaderValue;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerHandler.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

