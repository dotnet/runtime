// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static class ConnectHelper
    {
        /// <summary>Pool of event args to use to establish connections.</summary>
        private static readonly ConcurrentQueueSegment<ConnectEventArgs> s_connectEventArgs =
            new ConcurrentQueueSegment<ConnectEventArgs>(
                ConcurrentQueueSegment<ConnectEventArgs>.RoundUpToPowerOf2(Math.Max(2, Environment.ProcessorCount)));

        /// <summary>
        /// Helper type used by HttpClientHandler when wrapping SocketsHttpHandler to map its
        /// certificate validation callback to the one used by SslStream.
        /// </summary>
        internal sealed class CertificateCallbackMapper
        {
            public readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> FromHttpClientHandler;
            public readonly RemoteCertificateValidationCallback ForSocketsHttpHandler;

            public CertificateCallbackMapper(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> fromHttpClientHandler)
            {
                FromHttpClientHandler = fromHttpClientHandler;
                ForSocketsHttpHandler = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                    FromHttpClientHandler(sender as HttpRequestMessage, certificate as X509Certificate2, chain, sslPolicyErrors);
            }
        }

        public static async ValueTask<Stream> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            // Rather than creating a new Socket and calling ConnectAsync on it, we use the static
            // Socket.ConnectAsync with a SocketAsyncEventArgs, as we can then use Socket.CancelConnectAsync
            // to cancel it if needed. Rent or allocate one.
            ConnectEventArgs saea;
            if (!s_connectEventArgs.TryDequeue(out saea))
            {
                saea = new ConnectEventArgs();
            }

            try
            {
                saea.Initialize(cancellationToken);

                // Configure which server to which to connect.
                saea.RemoteEndPoint = new DnsEndPoint(host, port);

                // Initiate the connection.
                if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, saea))
                {
                    // Connect completing asynchronously. Enable it to be canceled and wait for it.
                    using (cancellationToken.Register(s => Socket.CancelConnectAsync((SocketAsyncEventArgs)s), saea))
                    {
                        await saea.Builder.Task.ConfigureAwait(false);
                    }
                }
                else if (saea.SocketError != SocketError.Success)
                {
                    // Connect completed synchronously but unsuccessfully.
                    throw new SocketException((int)saea.SocketError);
                }

                Debug.Assert(saea.SocketError == SocketError.Success, $"Expected Success, got {saea.SocketError}.");
                Debug.Assert(saea.ConnectSocket != null, "Expected non-null socket");

                // Configure the socket and return a stream for it.
                Socket socket = saea.ConnectSocket;
                socket.NoDelay = true;
                return new ExposedSocketNetworkStream(socket, ownsSocket: true);
            }
            catch (Exception error)
            {
                throw CancellationHelper.ShouldWrapInOperationCanceledException(error, cancellationToken) ?
                    CancellationHelper.CreateOperationCanceledException(error, cancellationToken) :
                    new HttpRequestException(error.Message, error);
            }
            finally
            {
                // Pool the event args, or if the pool is full, dispose of it.
                saea.Clear();
                if (!s_connectEventArgs.TryEnqueue(saea))
                {
                    saea.Dispose();
                }
            }
        }

        /// <summary>SocketAsyncEventArgs that carries with it additional state for a Task builder and a CancellationToken.</summary>
        private sealed class ConnectEventArgs : SocketAsyncEventArgs
        {
            public AsyncTaskMethodBuilder Builder { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public void Initialize(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
                var b = new AsyncTaskMethodBuilder();
                var ignored = b.Task; // force initialization
                Builder = b;
            }

            public void Clear() => CancellationToken = default;

            protected override void OnCompleted(SocketAsyncEventArgs _)
            {
                switch (SocketError)
                {
                    case SocketError.Success:
                        Builder.SetResult();
                        break;

                    case SocketError.OperationAborted:
                    case SocketError.ConnectionAborted:
                        if (CancellationToken.IsCancellationRequested)
                        {
                            Builder.SetException(CancellationHelper.CreateOperationCanceledException(null, CancellationToken));
                            break;
                        }
                        goto default;

                    default:
                        Builder.SetException(new SocketException((int)SocketError));
                        break;
                }
            }
        }

        public static ValueTask<SslStream> EstablishSslConnectionAsync(SslClientAuthenticationOptions sslOptions, HttpRequestMessage request, Stream stream, CancellationToken cancellationToken)
        {
            // If there's a cert validation callback, and if it came from HttpClientHandler,
            // wrap the original delegate in order to change the sender to be the request message (expected by HttpClientHandler's delegate).
            RemoteCertificateValidationCallback callback = sslOptions.RemoteCertificateValidationCallback;
            if (callback != null && callback.Target is CertificateCallbackMapper mapper)
            {
                sslOptions = sslOptions.ShallowClone(); // Clone as we're about to mutate it and don't want to affect the cached copy
                Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> localFromHttpClientHandler = mapper.FromHttpClientHandler;
                HttpRequestMessage localRequest = request;
                sslOptions.RemoteCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                    localFromHttpClientHandler(localRequest, certificate as X509Certificate2, chain, sslPolicyErrors);
            }

            // Create the SslStream, authenticate, and return it.
            return EstablishSslConnectionAsyncCore(stream, sslOptions, cancellationToken);
        }

        private static async ValueTask<SslStream> EstablishSslConnectionAsyncCore(Stream stream, SslClientAuthenticationOptions sslOptions, CancellationToken cancellationToken)
        {
            SslStream sslStream = new SslStream(stream);

            // TODO #25206 and #24430: Register/IsCancellationRequested should be removable once SslStream auth and sockets respect cancellation.
            CancellationTokenRegistration ctr = cancellationToken.Register(s => ((Stream)s).Dispose(), stream);
            try
            {
                await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                sslStream.Dispose();

                if (CancellationHelper.ShouldWrapInOperationCanceledException(e, cancellationToken))
                {
                    throw CancellationHelper.CreateOperationCanceledException(e, cancellationToken);
                }

                throw new HttpRequestException(SR.net_http_ssl_connection_failed, e);
            }
            finally
            {
                ctr.Dispose();
            }

            // Handle race condition if cancellation happens after SSL auth completes but before the registration is disposed
            if (cancellationToken.IsCancellationRequested)
            {
                sslStream.Dispose();
                throw CancellationHelper.CreateOperationCanceledException(null, cancellationToken);
            }

            return sslStream;
        }
    }
}
