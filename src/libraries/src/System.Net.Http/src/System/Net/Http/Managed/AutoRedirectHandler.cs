// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal sealed class AutoRedirectHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly int _maxAutomaticRedirections;

        public AutoRedirectHandler(int maxAutomaticRedirections, HttpMessageHandler innerHandler)
        {
            if (innerHandler == null)
            {
                throw new ArgumentNullException(nameof(innerHandler));
            }

            if (maxAutomaticRedirections < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAutomaticRedirections));
            }

            _maxAutomaticRedirections = maxAutomaticRedirections;
            _innerHandler = innerHandler;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken);

            int redirectCount = 0;
            while (true)
            {
                bool needRedirect = false;
                bool forceGet = false;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Moved:
                    case HttpStatusCode.TemporaryRedirect:
                        needRedirect = true;
                        break;

                    case HttpStatusCode.Found:
                    case HttpStatusCode.SeeOther:
                        needRedirect = true;
                        forceGet = true;
                        break;

                    case HttpStatusCode.MultipleChoices:
                        // Don't redirect if no Location specified
                        if (response.Headers.Location != null)
                        {
                            needRedirect = true;
                        }
                        break;
                }

                if (!needRedirect)
                {
                    break;
                }

                Uri location = response.Headers.Location;
                if (location == null)
                {
                    throw new HttpRequestException("no Location header for redirect");
                }

                if (!location.IsAbsoluteUri)
                {
                    location = new Uri(request.RequestUri, location);
                }

                // Disallow automatic redirection from https to http
                if (request.RequestUri.Scheme == "https" && location.Scheme == "http")
                {
                    break;
                }

                redirectCount++;
                if (redirectCount > _maxAutomaticRedirections)
                {
                    throw new HttpRequestException("max redirects exceeded");
                }

                // Set up for the automatic redirect
                request.RequestUri = location;

                if (forceGet)
                {
                    request.Method = HttpMethod.Get;
                    request.Content = null;
                }

                // Do the redirect.
                response.Dispose();
                response = await _innerHandler.SendAsync(request, cancellationToken);
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
