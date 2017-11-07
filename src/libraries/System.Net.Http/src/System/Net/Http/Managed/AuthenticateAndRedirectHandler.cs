// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
            bool useCredentialCache = false;
            while (true)
            {
                response = await SendRequestAsync(request, useCredentialCache, cancellationToken).ConfigureAwait(false);

                if (!RequestNeedsRedirect(response))
                {
                    break;
                }

                // Clear the authorization header, if the request requires redirect.
                request.Headers.Authorization = null;

                // Just as with WinHttpHandler and CurlHandler, for security reasons, we drop the server credential if it is
                // anything other than a CredentialCache. We allow credentials in a CredentialCache since they
                // are specifically tied to URIs.
                useCredentialCache = true;

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

