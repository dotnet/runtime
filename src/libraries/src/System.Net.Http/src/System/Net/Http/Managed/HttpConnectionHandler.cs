// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnectionHandler : HttpMessageHandler
    {
        private readonly HttpConnectionPools _connectionPools;

        public HttpConnectionHandler(HttpConnectionSettings settings)
        {
            _connectionPools = new HttpConnectionPools(settings, false, settings._maxConnectionsPerServer);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri;
            HttpConnectionKey key = new HttpConnectionKey(uri.IdnHost, uri.Port, GetSslHostName(request));
            HttpConnectionPool pool = _connectionPools.GetOrAddPool(key);
            return pool.SendAsync(request, cancellationToken);
        }

        private static string GetSslHostName(HttpRequestMessage request)
        {
            Uri uri = request.RequestUri;

            if (!HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
            {
                // Not using SSL.
                return null;
            }

            string hostHeader = request.Headers.Host;
            if (hostHeader == null)
            {
                // No explicit Host header.  Use host from uri.
                return request.RequestUri.IdnHost;
            }

            // There is a host header.  Use it, but first see if we need to trim off a port.
            int colonPos = hostHeader.IndexOf(':');
            if (colonPos >= 0)
            {
                // There is colon, which could either be a port separator or a separator in
                // an IPv6 address.  See if this is an IPv6 address; if it's not, use everything
                // before the colon as the host name, and if it is, use everything before the last
                // colon iff the last colon is after the end of the IPv6 address (otherwise it's a
                // part of the address).
                int ipV6AddressEnd = hostHeader.IndexOf(']');
                if (ipV6AddressEnd == -1)
                {
                    return hostHeader.Substring(0, colonPos);
                }
                else
                {
                    colonPos = hostHeader.LastIndexOf(':');
                    if (colonPos > ipV6AddressEnd)
                    {
                        return hostHeader.Substring(0, colonPos);
                    }
                }
            }

            return hostHeader;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectionPools.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
