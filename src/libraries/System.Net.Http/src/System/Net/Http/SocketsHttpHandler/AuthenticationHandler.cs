// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class AuthenticationHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly bool _preAuthenticate;
        private readonly ICredentials _credentials;

        public AuthenticationHandler(bool preAuthenticate, ICredentials credentials, HttpMessageHandler innerHandler)
        {
            Debug.Assert(innerHandler != null);
            Debug.Assert(credentials != null);

            _preAuthenticate = preAuthenticate;
            _credentials = credentials;
            _innerHandler = innerHandler;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_preAuthenticate)
            {
                AuthenticationHelper.TrySetBasicAuthToken(request, _credentials);
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                AuthenticationHeaderValue selectedAuth = GetSupportedAuthScheme(response.Headers.WwwAuthenticate);
                if (selectedAuth != null)
                {
                    switch (selectedAuth.Scheme)
                    {
                        case AuthenticationHelper.Digest:
                            // Update digest response with new parameter from WWWAuthenticate
                            var digestResponse = new AuthenticationHelper.DigestResponse(selectedAuth.Parameter);
                            if (await AuthenticationHelper.TrySetDigestAuthToken(request, _credentials, digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
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
                                                await AuthenticationHelper.TrySetDigestAuthToken(request, _credentials, digestResponse, HttpKnownHeaderNames.Authorization).ConfigureAwait(false))
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

                            if (AuthenticationHelper.TrySetBasicAuthToken(request, _credentials))
                            {
                                response.Dispose();
                                response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                            }
                            break;
                    }
                }
            }

            return response;
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

