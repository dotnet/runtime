// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class AuthenticationHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly bool _preAuthenticate;
        private ICredentials _credentials;
        private AuthenticationHelper.DigestResponse _digestResponse;

        public AuthenticationHandler(bool preAuthenticate, ICredentials credentials, HttpMessageHandler innerHandler)
        {
            Debug.Assert(credentials != null);
            Debug.Assert(innerHandler != null);

            _preAuthenticate = preAuthenticate;
            _credentials = credentials;
            _innerHandler = innerHandler;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_preAuthenticate)
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

            // In case of redirection, ensure _credentials as CredentialCache
            if (AutoRedirectHandler.RequestNeedsRedirect(response))
            {
                // Just as with WinHttpHandler and CurlHandler, for security reasons, we drop the server credential if it is
                // anything other than a CredentialCache. We allow credentials in a CredentialCache since they
                // are specifically tied to URIs.
                _credentials = _credentials as CredentialCache;
            }
            else if (_credentials != null && !_preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HttpHeaderValueCollection<AuthenticationHeaderValue> authenticateValues = response.Headers.WwwAuthenticate;

                foreach (AuthenticationHeaderValue h in authenticateValues)
                {
                    // We only support Basic and digest auth, ignore others
                    if (h.Scheme == AuthenticationHelper.Basic)
                    {
                        if (AuthenticationHelper.TrySetBasicAuthToken(request, _credentials))
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
                        if (await AuthenticationHelper.TrySetDigestAuthToken(request, _credentials, _digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
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
                                            await AuthenticationHelper.TrySetDigestAuthToken(request, _credentials, _digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
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
