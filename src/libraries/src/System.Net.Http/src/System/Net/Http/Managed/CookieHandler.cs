// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal class CookieHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly CookieContainer _cookieContainer;

        public CookieHandler(CookieContainer cookieContainer, HttpMessageHandler innerHandler)
        {
            if (innerHandler == null)
            {
                throw new ArgumentNullException(nameof(innerHandler));
            }

            if (cookieContainer == null)
            {
                throw new ArgumentNullException(nameof(cookieContainer));
            }

            _innerHandler = innerHandler;
            _cookieContainer = cookieContainer;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Add cookies to request, if any
            string cookieHeader = _cookieContainer.GetCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken);

            // Handle Set-Cookie
            IEnumerable<string> setCookies;
            if (response.Headers.TryGetValues("Set-Cookie", out setCookies))
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
