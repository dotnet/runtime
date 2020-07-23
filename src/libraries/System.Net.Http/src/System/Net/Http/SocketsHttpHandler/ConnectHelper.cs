// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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

        public static ValueTask<Stream> ConnectAsync(string host, int port, bool async, CancellationToken cancellationToken)
        {
            return async ? ConnectAsync(host, port, cancellationToken) : new ValueTask<Stream>(Connect(host, port, cancellationToken));
        }

        private static async ValueTask<Stream> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            // Rather than creating a new Socket and calling ConnectAsync on it, we use the static
            // Socket.ConnectAsync with a SocketAsyncEventArgs, as we can then use Socket.CancelConnectAsync
            // to cancel it if needed.
            var saea = new ConnectEventArgs();
            try
            {
                saea.Initialize(cancellationToken);

                // Configure which server to which to connect.
                saea.RemoteEndPoint = new DnsEndPoint(host, port);

                // Initiate the connection.
                if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, saea))
                {
                    // Connect completing asynchronously. Enable it to be canceled and wait for it.
                    using (cancellationToken.UnsafeRegister(static s => Socket.CancelConnectAsync((SocketAsyncEventArgs)s!), saea))
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
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                throw CreateWrappedException(error, host, port, cancellationToken);
            }
            finally
            {
                saea.Dispose();
            }
        }

        private static Stream Connect(string host, int port, CancellationToken cancellationToken)
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

        /// <summary>SocketAsyncEventArgs that carries with it additional state for a Task builder and a CancellationToken.</summary>
        private sealed class ConnectEventArgs : SocketAsyncEventArgs
        {
            internal ConnectEventArgs() :
                // The OnCompleted callback serves just to complete a task that's awaited in ConnectAsync,
                // so we don't need to also flow ExecutionContext again into the OnCompleted callback.
                base(unsafeSuppressExecutionContextFlow: true)
            {
            }

            public AsyncTaskMethodBuilder Builder { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public void Initialize(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
                AsyncTaskMethodBuilder b = default;
                _ = b.Task; // force initialization
                Builder = b;
            }

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
                            Builder.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(CancellationHelper.CreateOperationCanceledException(null, CancellationToken)));
                            break;
                        }
                        goto default;

                    default:
                        Builder.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new SocketException((int)SocketError)));
                        break;
                }
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

        public static async ValueTask<QuicConnection> ConnectQuicAsync(string host, int port, SslClientAuthenticationOptions? clientAuthenticationOptions, CancellationToken cancellationToken)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            Exception? lastException = null;

            foreach (IPAddress address in addresses)
            {
                QuicConnection con = new QuicConnection(new IPEndPoint(address, port), clientAuthenticationOptions);
                try
                {
                    await con.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    return con;
                }
                // TODO: it would be great to catch a specific exception here... QUIC implementation dependent.
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    con.Dispose();
                    lastException = ex;
                }
            }

            if (lastException != null)
            {
                throw CreateWrappedException(lastException, host, port, cancellationToken);
            }

            // TODO: find correct exception to throw here.
            throw new HttpRequestException("No host found.");
        }

        private static Exception CreateWrappedException(Exception error, string host, int port, CancellationToken cancellationToken)
        {
            return CancellationHelper.ShouldWrapInOperationCanceledException(error, cancellationToken) ?
                CancellationHelper.CreateOperationCanceledException(error, cancellationToken) :
                new HttpRequestException($"{error.Message} ({host}:{port})", error, RequestRetryType.RetryOnNextProxy);
        }
    }
}
