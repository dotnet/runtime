// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class HttpConnectionHandler : HttpMessageHandler
    {
        private readonly HttpConnectionSettings _settings;
        private readonly HttpConnectionPools _connectionPools;

        public HttpConnectionHandler(HttpConnectionSettings settings)
        {
            _settings = settings;
            _connectionPools = new HttpConnectionPools(settings._maxConnectionsPerServer);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = new HttpConnectionKey(request.RequestUri);

            HttpConnectionPool pool = _connectionPools.GetOrAddPool(key);
            ValueTask<HttpConnection> connectionTask = pool.GetConnectionAsync(
                (state, ct) => state.handler.CreateConnection(state.request, state.key, state.pool, ct),
                (handler: this, request: request, key: key, pool: pool), cancellationToken);

            return connectionTask.IsCompletedSuccessfully ?
                connectionTask.Result.SendAsync(request, cancellationToken) :
                SendAsyncWithAwaitedConnection(connectionTask, request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsyncWithAwaitedConnection(
            ValueTask<HttpConnection> connectionTask, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpConnection connection = await connectionTask.ConfigureAwait(false);
            return await connection.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<SslStream> EstablishSslConnection(string host, HttpRequestMessage request, Stream stream, CancellationToken cancellationToken)
        {
            RemoteCertificateValidationCallback callback = null;
            if (_settings._serverCertificateCustomValidationCallback != null)
            {
                callback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    try
                    {
                        return _settings._serverCertificateCustomValidationCallback(request, certificate as X509Certificate2, chain, sslPolicyErrors);
                    }
                    catch (Exception e)
                    {
                        throw new HttpRequestException(SR.net_http_ssl_connection_failed, e);
                    }
                };
            }

            var sslStream = new SslStream(stream);

            try
            {
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    ClientCertificates = _settings._clientCertificates,
                    EnabledSslProtocols = _settings._sslProtocols,
                    CertificateRevocationCheckMode = _settings._checkCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = callback
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                sslStream.Dispose();
                if (e is AuthenticationException || e is IOException)
                {
                    throw new HttpRequestException(SR.net_http_ssl_connection_failed, e);
                }
                throw;
            }

            return sslStream;
        }

        private async ValueTask<HttpConnection> CreateConnection(
            HttpRequestMessage request, HttpConnectionKey key, HttpConnectionPool pool, CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri;

            Stream stream = await ConnectHelper.ConnectAsync(uri.IdnHost, uri.Port, cancellationToken).ConfigureAwait(false);

            TransportContext transportContext = null;

            if (HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
            {
                // Get the appropriate host name to use for the SSL connection, allowing a host header to override.
                string host = request.Headers.Host;
                if (host == null)
                {
                    // No host header, use the host from the Uri.
                    host = uri.IdnHost;
                }
                else
                {
                    // There is a host header.  Use it, but first see if we need to trim off a port.
                    int colonPos = host.IndexOf(':');
                    if (colonPos >= 0)
                    {
                        // There is colon, which could either be a port separator or a separator in
                        // an IPv6 address.  See if this is an IPv6 address; if it's not, use everything
                        // before the colon as the host name, and if it is, use everything before the last
                        // colon iff the last colon is after the end of the IPv6 address (otherwise it's a
                        // part of the address).
                        int ipV6AddressEnd = host.IndexOf(']');
                        if (ipV6AddressEnd == -1)
                        {
                            host = host.Substring(0, colonPos);
                        }
                        else
                        {
                            colonPos = host.LastIndexOf(':');
                            if (colonPos > ipV6AddressEnd)
                            {
                                host = host.Substring(0, colonPos);
                            }
                        }
                    }
                }

                // Establish the connection using the parsed host name.
                SslStream sslStream = await EstablishSslConnection(host, request, stream, cancellationToken).ConfigureAwait(false);
                stream = sslStream;
                transportContext = sslStream.TransportContext;
            }

            return new HttpConnection(pool, key, uri.IdnHost, stream, transportContext, false);
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
