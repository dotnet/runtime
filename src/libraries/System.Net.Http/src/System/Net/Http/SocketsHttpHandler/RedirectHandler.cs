// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class RedirectHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _initialInnerHandler;       // Used for initial request
        private readonly HttpMessageHandler _redirectInnerHandler;      // Used for redirects; this allows disabling auth
        private readonly int _maxAutomaticRedirections;

        public RedirectHandler(int maxAutomaticRedirections, HttpMessageHandler initialInnerHandler, HttpMessageHandler redirectInnerHandler)
        {
            Debug.Assert(initialInnerHandler != null);
            Debug.Assert(redirectInnerHandler != null);

            if (maxAutomaticRedirections < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAutomaticRedirections));
            }

            _maxAutomaticRedirections = maxAutomaticRedirections;
            _initialInnerHandler = initialInnerHandler;
            _redirectInnerHandler = redirectInnerHandler;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _initialInnerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);

            uint redirectCount = 0;
            Uri redirectUri;
            while ((redirectUri = GetUriForRedirect(request.RequestUri, response)) != null)
            {
                response.Dispose();

                redirectCount++;
                if (redirectCount > _maxAutomaticRedirections)
                {
                    throw new HttpRequestException(SR.net_http_max_redirects);
                }

                // Clear the authorization header.
                request.Headers.Authorization = null;

                // Set up for the redirect
                request.RequestUri = redirectUri;
                if (RequestRequiresForceGet(response.StatusCode, request.Method))
                {
                    request.Method = HttpMethod.Get;
                    request.Content = null;
                }

                // Issue the redirected request.
                response = await _redirectInnerHandler.SendAsync(request, cancellationToken);
            }

            return response;
        }

        private Uri GetUriForRedirect(Uri requestUri, HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Moved:
                case HttpStatusCode.Found:
                case HttpStatusCode.SeeOther:
                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.MultipleChoices:
                    break;

                default:
                    return null;
            }

            Uri location = response.Headers.Location;
            if (location == null)
            {
                return null;
            }

            if (!location.IsAbsoluteUri)
            {
                location = new Uri(requestUri, location);
            }

            // Disallow automatic redirection from secure to non-secure schemes
            if (HttpUtilities.IsSupportedSecureScheme(requestUri.Scheme) && !HttpUtilities.IsSupportedSecureScheme(location.Scheme))
            {
                return null;
            }

            return location;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _initialInnerHandler.Dispose();
                _redirectInnerHandler.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

