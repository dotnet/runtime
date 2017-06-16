// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal sealed class AuthenticationHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly bool _preAuthenticate;
        private readonly ICredentials _credentials;

        public AuthenticationHandler(bool preAuthenticate, ICredentials credentials, HttpMessageHandler innerHandler)
        {
            if (innerHandler == null)
            {
                throw new ArgumentNullException(nameof(innerHandler));
            }

            if (credentials == null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }

            _preAuthenticate = preAuthenticate;
            _credentials = credentials;
            _innerHandler = innerHandler;
        }

        private bool TrySetBasicAuthToken(HttpRequestMessage request)
        {
            NetworkCredential credential = _credentials.GetCredential(request.RequestUri, "Basic");
            if (credential == null)
            {
                return false;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuthenticationHelper.GetBasicTokenForCredential(credential));
            return true;
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_preAuthenticate)
            {
                TrySetBasicAuthToken(request);
            }

            HttpResponseMessage response = await _innerHandler.SendAsync(request, cancellationToken);

            if (!_preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HttpHeaderValueCollection<AuthenticationHeaderValue> authenticateValues = response.Headers.WwwAuthenticate;

                foreach (AuthenticationHeaderValue h in authenticateValues)
                {
                    // We only support Basic auth, ignore others
                    if (h.Scheme == "Basic")
                    {
                        if (!TrySetBasicAuthToken(request))
                        {
                            break;
                        }

                        response.Dispose();
                        response = await _innerHandler.SendAsync(request, cancellationToken);
                        break;
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
