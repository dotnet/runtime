// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal sealed class HttpConnectionHandler : HttpMessageHandler
    {
        private readonly X509CertificateCollection _clientCertificates;
        private readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _serverCertificateCustomValidationCallback;
        private readonly bool _checkCertificateRevocationList;
        private readonly SslProtocols _sslProtocols;

        private readonly ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool> _connectionPoolTable;
        private bool _disposed;

        public HttpConnectionHandler(
            X509CertificateCollection clientCertificates,
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> serverCertificateCustomValidationCallback,
            bool checkCertificateRevocationList,
            SslProtocols sslProtocols)
        {
            _clientCertificates = clientCertificates;
            _serverCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
            _checkCertificateRevocationList = checkCertificateRevocationList;
            _sslProtocols = sslProtocols;

            _connectionPoolTable = new ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>();
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpConnection connection = null;

            // Try to get a connection from the connection pool
            HttpConnectionKey key = new HttpConnectionKey(request.RequestUri);
            HttpConnectionPool pool;
            if (_connectionPoolTable.TryGetValue(key, out pool))
            {
                connection = pool.GetConnection();
            }

            if (connection == null)
            {
                // No connection available in pool.  Create a new one.
                connection = await CreateConnection(request, key, pool);
            }

            return await connection.SendAsync(request, cancellationToken);
        }

        private async Task<SslStream> EstablishSslConnection(string host, HttpRequestMessage request, Stream stream)
        {
            RemoteCertificateValidationCallback callback = null;
            if (_serverCertificateCustomValidationCallback != null)
            {
                callback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return _serverCertificateCustomValidationCallback(request, certificate as X509Certificate2, chain, sslPolicyErrors);
                };
            }

            SslStream sslStream = new SslStream(stream, false, callback);

            try
            {
                // TODO: No cancellationToken?
                await sslStream.AuthenticateAsClientAsync(host, _clientCertificates, _sslProtocols, _checkCertificateRevocationList);
            }
            catch (AuthenticationException ae)
            {
                // TODO: Tests expect HttpRequestException here.  Is that correct behavior?
                sslStream.Dispose();
                throw new HttpRequestException("could not establish SSL connection", ae);
            }
            catch (IOException ie)
            {
                // TODO: Tests expect HttpRequestException here.  Is that correct behavior?
                sslStream.Dispose();
                throw new HttpRequestException("could not establish SSL connection", ie);
            }
            catch (Exception)
            {
                sslStream.Dispose();
                throw;
            }

            return sslStream;
        }

        private async Task<HttpConnection> CreateConnection(HttpRequestMessage request, HttpConnectionKey key, HttpConnectionPool pool)
        {
            Uri uri = request.RequestUri;

            Stream stream = await ConnectHelper.ConnectAsync(uri.Host, uri.Port);

            TransportContext transportContext = null;

            if (uri.Scheme == "https")
            {
                SslStream sslStream = await EstablishSslConnection(uri.Host, request, stream);

                stream = sslStream;
                transportContext = sslStream.TransportContext;
            }

            if (pool == null)
            {
                pool = _connectionPoolTable.GetOrAdd(key, new HttpConnectionPool());
            }

            var connection = new HttpConnection(pool, key, stream, transportContext, false);

            return connection;
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
