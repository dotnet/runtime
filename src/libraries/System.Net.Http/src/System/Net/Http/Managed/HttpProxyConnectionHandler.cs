// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal sealed class HttpProxyConnectionHandler : HttpMessageHandler
    {
        private readonly IWebProxy _proxy;
        private readonly HttpMessageHandler _innerHandler;

        private readonly ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool> _connectionPoolTable;
        private bool _disposed;

        public HttpProxyConnectionHandler(IWebProxy proxy, HttpMessageHandler innerHandler)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException(nameof(proxy));
            }

            if (innerHandler == null)
            {
                throw new ArgumentNullException(nameof(innerHandler));
            }

            _proxy = proxy;
            _innerHandler = innerHandler;

            _connectionPoolTable = new ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>();
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
                // TODO: This seems a bit questionable, but it's what the tests expect
            }

            if (proxyUri == null)
            {
                return await _innerHandler.SendAsync(request, cancellationToken);
            }

            if (proxyUri.Scheme != "http")
            {
                throw new InvalidOperationException($"invalid scheme {proxyUri.Scheme} for proxy");
            }

            if (request.RequestUri.Scheme == "https")
            {
                // TODO: Implement SSL tunneling through proxy
                throw new NotImplementedException("no support for SSL tunneling through proxy");
            }

            HttpConnection connection = await GetOrCreateConnection(request, proxyUri);

            HttpResponseMessage response = await connection.SendAsync(request, cancellationToken);

            // Handle proxy authentication
            if (response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired &&
                _proxy.Credentials != null)
            {
                foreach (AuthenticationHeaderValue h in response.Headers.ProxyAuthenticate)
                {
                    // We only support Basic auth, ignore others
                    if (h.Scheme == "Basic")
                    {
                        NetworkCredential credential = _proxy.Credentials.GetCredential(proxyUri, "Basic");
                        if (credential != null)
                        {
                            response.Dispose();

                            request.Headers.ProxyAuthorization = new AuthenticationHeaderValue("Basic",
                                BasicAuthenticationHelper.GetBasicTokenForCredential(credential));

                            connection = await GetOrCreateConnection(request, proxyUri);
                            response = await connection.SendAsync(request, cancellationToken);
                        }

                        break;
                    }
                }
            }

            return response;
        }

        private async Task<HttpConnection> GetOrCreateConnection(HttpRequestMessage request, Uri proxyUri)
        {
            HttpConnectionKey key = new HttpConnectionKey(proxyUri);

            HttpConnectionPool pool;
            if (_connectionPoolTable.TryGetValue(key, out pool))
            {
                HttpConnection poolConnection = pool.GetConnection();
                if (poolConnection != null)
                {
                    return poolConnection;
                }
            }

            Stream stream = await ConnectHelper.ConnectAsync(proxyUri.Host, proxyUri.Port);

            if (pool == null)
            {
                pool = _connectionPoolTable.GetOrAdd(key, new HttpConnectionPool());
            }

            return new HttpConnection(pool, key, stream, null, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !!_disposed)
            {
                _disposed = true;

                // Close all open connections
                // TODO: There's a timing issue here
                // Revisit when we improve the connection pooling implementation
                foreach (HttpConnectionPool connectionPool in _connectionPoolTable.Values)
                {
                    connectionPool.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
