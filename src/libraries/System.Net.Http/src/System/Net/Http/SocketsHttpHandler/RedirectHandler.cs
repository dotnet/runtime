// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class RedirectHandler : HttpMessageHandlerStage
    {
        private readonly HttpMessageHandlerStage _initialInnerHandler;       // Used for initial request
        private readonly HttpMessageHandlerStage _redirectInnerHandler;      // Used for redirects; this allows disabling auth
        private readonly int _maxAutomaticRedirections;

        public RedirectHandler(int maxAutomaticRedirections, HttpMessageHandlerStage initialInnerHandler, HttpMessageHandlerStage redirectInnerHandler)
        {
            Debug.Assert(initialInnerHandler != null);
            Debug.Assert(redirectInnerHandler != null);
            Debug.Assert(maxAutomaticRedirections > 0);

            _maxAutomaticRedirections = maxAutomaticRedirections;
            _initialInnerHandler = initialInnerHandler;
            _redirectInnerHandler = redirectInnerHandler;
        }

        internal override async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _initialInnerHandler.SendAsync(request, async, cancellationToken).ConfigureAwait(false);

            uint redirectCount = 0;
            Uri? redirectUri;
            Debug.Assert(request.RequestUri != null);
            while ((redirectUri = GetUriForRedirect(request.RequestUri, response)) != null)
            {
                redirectCount++;

                if (redirectCount > _maxAutomaticRedirections)
                {
                    // If we exceed the maximum number of redirects
                    // then just return the 3xx response.
                    if (NetEventSource.Log.IsEnabled())
                    {
                        TraceError($"Exceeded max number of redirects. Redirect from {request.RequestUri} to {redirectUri} blocked.", request.GetHashCode());
                    }

                    break;
                }

                response.Dispose();

                // Clear the authorization header.
                request.Headers.Authorization = null;

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace($"Redirecting from {request.RequestUri} to {redirectUri} in response to status code {(int)response.StatusCode} '{response.StatusCode}'.", request.GetHashCode());
                }

                // Set up for the redirect
                request.RequestUri = redirectUri;
                if (RequestRequiresForceGet(response.StatusCode, request.Method))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Modified request from {request.Method} to {HttpMethod.Get} in response to status code {(int)response.StatusCode} '{response.StatusCode}'.", request.GetHashCode());
                    }

                    request.Method = HttpMethod.Get;
                    request.Content = null;
                    if (request.Headers.TransferEncodingChunked == true)
                    {
                        request.Headers.TransferEncodingChunked = false;
                    }
                }

                request.MarkAsRedirected();

                // Issue the redirected request.
                response = await _redirectInnerHandler.SendAsync(request, async, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private Uri? GetUriForRedirect(Uri requestUri, HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Moved:
                case HttpStatusCode.Found:
                case HttpStatusCode.SeeOther:
                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.MultipleChoices:
                case HttpStatusCode.PermanentRedirect:
                    break;

                default:
                    return null;
            }

            Uri? location = response.Headers.Location;
            if (location == null)
            {
                return null;
            }

            // Ensure the redirect location is an absolute URI.
            if (!location.IsAbsoluteUri)
            {
                location = new Uri(requestUri, location);
            }

            // Per https://tools.ietf.org/html/rfc7231#section-7.1.2, a redirect location without a
            // fragment should inherit the fragment from the original URI.
            string requestFragment = requestUri.Fragment;
            if (!string.IsNullOrEmpty(requestFragment))
            {
                string redirectFragment = location.Fragment;
                if (string.IsNullOrEmpty(redirectFragment))
                {
                    location = new UriBuilder(location) { Fragment = requestFragment }.Uri;
                }
            }

            // Disallow automatic redirection from secure to non-secure schemes
            if (HttpUtilities.IsSupportedSecureScheme(requestUri.Scheme) && !HttpUtilities.IsSupportedSecureScheme(location.Scheme))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    TraceError($"Insecure https to http redirect from '{requestUri}' to '{location}' blocked.", response.RequestMessage!.GetHashCode());
                }

                return null;
            }

            return location;
        }

        private static bool RequestRequiresForceGet(HttpStatusCode statusCode, HttpMethod requestMethod)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Moved:
                case HttpStatusCode.Found:
                case HttpStatusCode.MultipleChoices:
                    return requestMethod == HttpMethod.Post;
                case HttpStatusCode.SeeOther:
                    return requestMethod != HttpMethod.Get && requestMethod != HttpMethod.Head;
                default:
                    return false;
            }
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

        internal void Trace(string message, int requestId, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessage(0, 0, requestId, memberName, ToString() + ": " + message);

        internal void TraceError(string message, int requestId, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessageError(0, 0, requestId, memberName, ToString() + ": " + message);
    }
}
