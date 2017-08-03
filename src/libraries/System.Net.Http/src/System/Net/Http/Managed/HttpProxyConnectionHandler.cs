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
    internal sealed class HttpProxyConnectionHandler : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly IWebProxy _proxy;
        private readonly ICredentials _defaultCredentials;
        private readonly HttpConnectionPools _connectionPools;
        private bool _disposed;

        public HttpProxyConnectionHandler(HttpConnectionSettings settings, HttpMessageHandler innerHandler)
        {
            Debug.Assert(innerHandler != null);

            _innerHandler = innerHandler;
            _proxy = (settings._useProxy && settings._proxy != null) ? settings._proxy : new PassthroughWebProxy(s_proxyFromEnvironment.Value);
            _defaultCredentials = settings._defaultProxyCredentials;
            _connectionPools = new HttpConnectionPools(settings._maxConnectionsPerServer);
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
                // TODO #21452: This seems a bit questionable, but it's what the tests expect
            }

            return proxyUri == null ?
                _innerHandler.SendAsync(request, cancellationToken) :
                SendWithProxyAsync(proxyUri, request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendWithProxyAsync(
            Uri proxyUri, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (proxyUri.Scheme != UriScheme.Http)
            {
                throw new InvalidOperationException($"invalid scheme {proxyUri.Scheme} for proxy");
            }

            if (request.RequestUri.Scheme == UriScheme.Https)
            {
                // TODO #21452: Implement SSL tunneling through proxy
                throw new NotImplementedException("no support for SSL tunneling through proxy");
            }

            HttpConnection connection = await GetOrCreateConnection(request, proxyUri).ConfigureAwait(false);

            HttpResponseMessage response = await connection.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Handle proxy authentication
            if (response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
            {
                foreach (AuthenticationHeaderValue h in response.Headers.ProxyAuthenticate)
                {
                    // We only support Basic auth, ignore others
                    const string Basic = "Basic";
                    if (h.Scheme == Basic)
                    {
                        NetworkCredential credential =
                            _proxy.Credentials?.GetCredential(proxyUri, Basic) ??
                            _defaultCredentials?.GetCredential(proxyUri, Basic);

                        if (credential != null)
                        {
                            response.Dispose();

                            request.Headers.ProxyAuthorization = new AuthenticationHeaderValue(Basic,
                             AuthenticationHelper.GetBasicTokenForCredential(credential));

                            connection = await GetOrCreateConnection(request, proxyUri).ConfigureAwait(false);
                            response = await connection.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                }
            }

            return response;
        }

        private ValueTask<HttpConnection> GetOrCreateConnection(HttpRequestMessage request, Uri proxyUri)
        {
            var key = new HttpConnectionKey(proxyUri);
            HttpConnectionPool pool = _connectionPools.GetOrAddPool(key);
            return pool.GetConnectionAsync(async state =>
            {
                Stream stream = await ConnectHelper.ConnectAsync(state.proxyUri.IdnHost, state.proxyUri.Port).ConfigureAwait(false);
                return new HttpConnection(state.pool, state.key, null, stream, null, true);
            }, (pool: pool, key: key, request: request, proxyUri: proxyUri));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _connectionPools.Dispose();
            }

            base.Dispose(disposing);
        }

        public static bool EnvironmentProxyConfigured => s_proxyFromEnvironment.Value != null;

        private static readonly Lazy<Uri> s_proxyFromEnvironment = new Lazy<Uri>(() =>
        {
            // http_proxy is standard on Unix, used e.g. by libcurl.
            // TODO #21452: We should support the full array of environment variables here,
            // including no_proxy, all_proxy, etc.

            string proxyString = Environment.GetEnvironmentVariable("http_proxy");
            if (!string.IsNullOrWhiteSpace(proxyString))
            {
                Uri proxyFromEnvironment;
                if (Uri.TryCreate(proxyString, UriKind.Absolute, out proxyFromEnvironment) ||
                    Uri.TryCreate(Uri.UriSchemeHttp + Uri.SchemeDelimiter + proxyString, UriKind.Absolute, out proxyFromEnvironment))
                {
                    return proxyFromEnvironment;
                }
            }

            return null;
        });

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
