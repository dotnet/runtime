// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Authentication;
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

        private static SslClientAuthenticationOptions SetUpRemoteCertificateValidationCallback(SslClientAuthenticationOptions sslOptions, HttpRequestMessage request)
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

            return sslOptions;
        }

        public static async ValueTask<SslStream> EstablishSslConnectionAsync(SslClientAuthenticationOptions sslOptions, HttpRequestMessage request, bool async, Stream stream, CancellationToken cancellationToken)
        {
            sslOptions = SetUpRemoteCertificateValidationCallback(sslOptions, request);

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

                HttpRequestException ex = new HttpRequestException(SR.net_http_ssl_connection_failed, e, httpRequestError: HttpRequestError.SecureConnectionError);
                throw ex;
            }

            // Handle race condition if cancellation happens after SSL auth completes but before the registration is disposed
            if (cancellationToken.IsCancellationRequested)
            {
                sslStream.Dispose();
                throw CancellationHelper.CreateOperationCanceledException(null, cancellationToken);
            }

            return sslStream;
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        public static async ValueTask<QuicConnection> ConnectQuicAsync(HttpRequestMessage request, DnsEndPoint endPoint, TimeSpan idleTimeout, SslClientAuthenticationOptions clientAuthenticationOptions, CancellationToken cancellationToken)
        {
            clientAuthenticationOptions = SetUpRemoteCertificateValidationCallback(clientAuthenticationOptions, request);

            try
            {
                return await QuicConnection.ConnectAsync(new QuicClientConnectionOptions()
                {
                    MaxInboundBidirectionalStreams = 0, // Client doesn't support inbound streams: https://www.rfc-editor.org/rfc/rfc9114.html#name-bidirectional-streams. An extension might change this.
                    MaxInboundUnidirectionalStreams = 5, // Minimum is 3: https://www.rfc-editor.org/rfc/rfc9114.html#unidirectional-streams (1x control stream + 2x QPACK). Set to 100 if/when support for PUSH streams is added.
                    IdleTimeout = idleTimeout,
                    DefaultStreamErrorCode = (long)Http3ErrorCode.RequestCancelled,
                    DefaultCloseErrorCode = (long)Http3ErrorCode.NoError,
                    RemoteEndPoint = endPoint,
                    ClientAuthenticationOptions = clientAuthenticationOptions
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }
        }

        internal static Exception CreateWrappedException(Exception exception, string host, int port, CancellationToken cancellationToken)
        {
            return CancellationHelper.ShouldWrapInOperationCanceledException(exception, cancellationToken) ?
                CancellationHelper.CreateOperationCanceledException(exception, cancellationToken) :
                new HttpRequestException($"{exception.Message} ({host}:{port})", exception, RequestRetryType.RetryOnNextProxy, DeduceError(exception));

            static HttpRequestError DeduceError(Exception exception)
            {
                // TODO: Deduce quic errors from QuicException.TransportErrorCode once https://github.com/dotnet/runtime/issues/87262 is implemented.
                if (exception is AuthenticationException)
                {
                    return HttpRequestError.SecureConnectionError;
                }

                if (exception is SocketException socketException && socketException.SocketErrorCode == SocketError.HostNotFound)
                {
                    return HttpRequestError.NameResolutionError;
                }

                return HttpRequestError.ConnectionError;
            }
        }
    }
}
