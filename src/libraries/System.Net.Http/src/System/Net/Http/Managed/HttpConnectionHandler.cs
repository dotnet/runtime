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
                state => state.handler.CreateConnection(state.request, state.key, state.pool),
                (handler: this, request: request, key: key, pool: pool));

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

        private async ValueTask<SslStream> EstablishSslConnection(string host, HttpRequestMessage request, Stream stream)
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

            SslStream sslStream = new SslStream(stream, false, callback);

            try
            {
                // TODO https://github.com/dotnet/corefx/issues/23077#issuecomment-321807131: No cancellationToken?
                await sslStream.AuthenticateAsClientAsync(host, _settings._clientCertificates, _settings._sslProtocols, _settings._checkCertificateRevocationList).ConfigureAwait(false);
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

        private async ValueTask<HttpConnection> CreateConnection(HttpRequestMessage request, HttpConnectionKey key, HttpConnectionPool pool)
        {
            Uri uri = request.RequestUri;

            Stream stream = await ConnectHelper.ConnectAsync(uri.IdnHost, uri.Port).ConfigureAwait(false);

            TransportContext transportContext = null;

            if (HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
            {
                SslStream sslStream = await EstablishSslConnection(uri.IdnHost, request, stream).ConfigureAwait(false);
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
