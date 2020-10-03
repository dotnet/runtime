// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static class ConnectHelper
    {
        /// <summary>
        /// Helper type used by HttpClientHandler when wrapping SocketsHttpHandler to map its
        /// certificate validation callback to the one used by SslStream.
        /// </summary>
        internal sealed class CertificateCallbackMapper
        {
            public readonly Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> FromHttpClientHandler;
            public readonly RemoteCertificateValidationCallback ForSocketsHttpHandler;

            public CertificateCallbackMapper(Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> fromHttpClientHandler)
            {
                FromHttpClientHandler = fromHttpClientHandler;
                ForSocketsHttpHandler = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
                    FromHttpClientHandler((HttpRequestMessage)sender, certificate as X509Certificate2, chain, sslPolicyErrors);
            }
        }

        public static async ValueTask<Stream> ConnectAsync(Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> callback, DnsEndPoint endPoint, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            Stream stream;
            try
            {
                stream = await callback(new SocketsHttpConnectionContext(endPoint, requestMessage), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                throw CancellationHelper.CreateOperationCanceledException(innerException: null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }

            if (stream == null)
            {
                throw new HttpRequestException(SR.net_http_null_from_connect_callback);
            }

            return stream;
        }

        public static Stream Connect(string host, int port, CancellationToken cancellationToken)
        {
            // For synchronous connections, we can just create a socket and make the connection.
            cancellationToken.ThrowIfCancellationRequested();
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.NoDelay = true;
                using (cancellationToken.UnsafeRegister(static s => ((Socket)s!).Dispose(), socket))
                {
                    socket.Connect(new DnsEndPoint(host, port));
                }

                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception e)
            {
                socket.Dispose();
                throw CreateWrappedException(e, host, port, cancellationToken);
            }
        }

        public static ValueTask<SslStream> EstablishSslConnectionAsync(SslClientAuthenticationOptions sslOptions, HttpRequestMessage request, bool async, Stream stream, CancellationToken cancellationToken)
        {
            // If there's a cert validation callback, and if it came from HttpClientHandler,
            // wrap the original delegate in order to change the sender to be the request message (expected by HttpClientHandler's delegate).
            RemoteCertificateValidationCallback? callback = sslOptions.RemoteCertificateValidationCallback;
            if (callback != null && callback.Target is CertificateCallbackMapper mapper)
            {
                sslOptions = sslOptions.ShallowClone(); // Clone as we're about to mutate it and don't want to affect the cached copy
                Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> localFromHttpClientHandler = mapper.FromHttpClientHandler;
                HttpRequestMessage localRequest = request;
                sslOptions.RemoteCertificateValidationCallback = (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    Debug.Assert(localRequest != null);
                    bool result = localFromHttpClientHandler(localRequest, certificate as X509Certificate2, chain, sslPolicyErrors);
                    localRequest = null!; // ensure the SslOptions and this callback don't keep the first HttpRequestMessage alive indefinitely
                    return result;
                };
            }

            // Create the SslStream, authenticate, and return it.
            return EstablishSslConnectionAsyncCore(async, stream, sslOptions, cancellationToken);
        }

        private static async ValueTask<SslStream> EstablishSslConnectionAsyncCore(bool async, Stream stream, SslClientAuthenticationOptions sslOptions, CancellationToken cancellationToken)
        {
            SslStream sslStream = new SslStream(stream);

            try
            {
                if (async)
                {
                    await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using (cancellationToken.UnsafeRegister(static s => ((Stream)s!).Dispose(), stream))
                    {
                        sslStream.AuthenticateAsClient(sslOptions);
                    }
                }
            }
            catch (Exception e)
            {
                sslStream.Dispose();

                if (e is OperationCanceledException)
                {
                    throw;
                }

                if (CancellationHelper.ShouldWrapInOperationCanceledException(e, cancellationToken))
                {
                    throw CancellationHelper.CreateOperationCanceledException(e, cancellationToken);
                }

                throw new HttpRequestException(SR.net_http_ssl_connection_failed, e);
            }

            // Handle race condition if cancellation happens after SSL auth completes but before the registration is disposed
            if (cancellationToken.IsCancellationRequested)
            {
                sslStream.Dispose();
                throw CancellationHelper.CreateOperationCanceledException(null, cancellationToken);
            }

            return sslStream;
        }

        public static async ValueTask<QuicConnection> ConnectQuicAsync(DnsEndPoint endPoint, SslClientAuthenticationOptions? clientAuthenticationOptions, CancellationToken cancellationToken)
        {
            QuicConnection con = new QuicConnection(endPoint, clientAuthenticationOptions);
            try
            {
                await con.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return con;
            }
            catch (Exception ex)
            {
                con.Dispose();
                throw CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }
        }

        private static Exception CreateWrappedException(Exception error, string host, int port, CancellationToken cancellationToken)
        {
            return CancellationHelper.ShouldWrapInOperationCanceledException(error, cancellationToken) ?
                CancellationHelper.CreateOperationCanceledException(error, cancellationToken) :
                new HttpRequestException($"{error.Message} ({host}:{port})", error, RequestRetryType.RetryOnNextProxy);
        }
    }
}
