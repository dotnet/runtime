// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpProxyConnectionHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly IWebProxy _proxy;
        private readonly ICredentials _defaultCredentials;
        private readonly HttpConnectionPools _connectionPools;
        private bool _disposed;

        public HttpProxyConnectionHandler(HttpConnectionSettings settings, HttpMessageHandler innerHandler)
        {
            Debug.Assert(innerHandler != null);
            Debug.Assert(settings._useProxy);

            _innerHandler = innerHandler;
            _proxy = settings._proxy ?? ConstructSystemProxy();
            _defaultCredentials = settings._defaultProxyCredentials;
            _connectionPools = new HttpConnectionPools(settings, settings._maxConnectionsPerServer, usingProxy: true);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri proxyUri = null;
            try
            {
                if (!_proxy.IsBypassed(request.RequestUri))
                {
                    proxyUri = _proxy.GetProxy(request.RequestUri);
                }
            }
            catch (Exception)
            {
                // Eat any exception from the IWebProxy and just treat it as no proxy.
                // This matches the behavior of other handlers.
            }

            return proxyUri == null ?
                _innerHandler.SendAsync(request, cancellationToken) :
                SendWithProxyAsync(proxyUri, request, cancellationToken);
        }

        private Task<HttpResponseMessage> GetConnectionAndSendAsync(HttpRequestMessage request, Uri proxyUri, CancellationToken cancellationToken)
        {
            Debug.Assert(proxyUri.Scheme == UriScheme.Http);

            var key = new HttpConnectionKey(proxyUri.IdnHost, proxyUri.Port, null);
            HttpConnectionPool pool = _connectionPools.GetOrAddPool(key);
            return pool.SendAsync(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendWithProxyAsync(
            Uri proxyUri, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (proxyUri.Scheme != UriScheme.Http)
            {
                throw new InvalidOperationException(SR.net_http_invalid_proxy_scheme);
            }

            if (!HttpUtilities.IsSupportedNonSecureScheme(request.RequestUri.Scheme))
            {
                // TODO #23136: Implement SSL tunneling through proxy
                throw new NotImplementedException("no support for SSL tunneling through proxy");
            }

            HttpResponseMessage response = await GetConnectionAndSendAsync(request, proxyUri, cancellationToken).ConfigureAwait(false);

            // Handle proxy authentication
            if (response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
            {
                foreach (AuthenticationHeaderValue h in response.Headers.ProxyAuthenticate)
                {
                    if (h.Scheme == AuthenticationHelper.Basic)
                    {
                        NetworkCredential credential =
                            _proxy.Credentials?.GetCredential(proxyUri, AuthenticationHelper.Basic) ??
                            _defaultCredentials?.GetCredential(proxyUri, AuthenticationHelper.Basic);

                        if (credential != null)
                        {
                            // TODO #23156: Drain response before disposing.
                            response.Dispose();

                            request.Headers.ProxyAuthorization = new AuthenticationHeaderValue(AuthenticationHelper.Basic,
                                AuthenticationHelper.GetBasicTokenForCredential(credential));

                            response = await GetConnectionAndSendAsync(request, proxyUri, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                    else if (h.Scheme == AuthenticationHelper.Digest)
                    {
                        NetworkCredential credential =
                            _proxy.Credentials?.GetCredential(proxyUri, AuthenticationHelper.Digest) ??
                            _defaultCredentials?.GetCredential(proxyUri, AuthenticationHelper.Digest);

                        if (credential != null)
                        {
                            // Update digest response with new parameter from Proxy-Authenticate
                            AuthenticationHelper.DigestResponse digestResponse = new AuthenticationHelper.DigestResponse(h.Parameter);

                            if (await AuthenticationHelper.TrySetDigestAuthToken(request, credential, digestResponse, HttpKnownHeaderNames.ProxyAuthorization).ConfigureAwait(false))
                            {
                                // TODO #23156: Drain response before disposing.
                                response.Dispose();
                                response = await GetConnectionAndSendAsync(request, proxyUri, cancellationToken).ConfigureAwait(false);

                                // Retry in case of nonce timeout in server.
                                if (response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                                {
                                    foreach (AuthenticationHeaderValue ahv in response.Headers.ProxyAuthenticate)
                                    {
                                        if (ahv.Scheme == AuthenticationHelper.Digest)
                                        {
                                            digestResponse = new AuthenticationHelper.DigestResponse(ahv.Parameter);
                                            if (AuthenticationHelper.IsServerNonceStale(digestResponse) &&
                                                await AuthenticationHelper.TrySetDigestAuthToken(request, credential, digestResponse, HttpKnownHeaderNames.ProxyAuthorization).ConfigureAwait(false))
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
                        }
                    }
                }
            }

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _connectionPools.Dispose();
                if (_proxy is IDisposable obj)
                {
                    obj.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public static bool DefaultProxyConfigured => s_DefaultProxy.Value != null;

        private static readonly Lazy<IWebProxy> s_DefaultProxy = new Lazy<IWebProxy>(() => ConstructSystemProxy());

        private sealed class PassthroughWebProxy : IWebProxy
        {
            private readonly Uri _proxyUri;
            public PassthroughWebProxy(Uri proxyUri) => _proxyUri = proxyUri;
            public ICredentials Credentials { get => null; set { } }
            public Uri GetProxy(Uri destination) => _proxyUri;
            public bool IsBypassed(Uri host) => false;
        }
    }
}
