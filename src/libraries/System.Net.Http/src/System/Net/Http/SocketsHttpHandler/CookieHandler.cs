// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class CookieHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly CookieContainer _cookieContainer;

        public CookieHandler(CookieContainer cookieContainer, HttpMessageHandler innerHandler)
        {
            _innerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
            _cookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Add cookies to request, if any
            string cookieHeader = _cookieContainer.GetCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add(HttpKnownHeaderNames.Cookie, cookieHeader);
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Handle Set-Cookie
            IEnumerable<string> setCookies;
            if (response.Headers.TryGetValues(HttpKnownHeaderNames.SetCookie, out setCookies))
            {
                foreach (string setCookie in setCookies)
                {
                    _cookieContainer.SetCookies(response.RequestMessage.RequestUri, setCookie);
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
