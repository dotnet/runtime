// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Connections;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// The default connection factory used by <see cref="SocketsHttpHandler"/>, opening TCP connections.
    /// </summary>
    public class SocketsHttpConnectionFactory : ConnectionFactory
    {
        internal static SocketsHttpConnectionFactory Default { get; } = new SocketsHttpConnectionFactory();

        /// <inheritdoc/>
        public sealed override ValueTask<Connection> ConnectAsync(EndPoint? endPoint, IConnectionProperties? options = null, CancellationToken cancellationToken = default)
        {
            if (options == null || !options.TryGet(out DnsEndPointWithProperties? httpOptions))
            {
                return ValueTask.FromException<Connection>(ExceptionDispatchInfo.SetCurrentStackTrace(new HttpRequestException($"{nameof(SocketsHttpConnectionFactory)} requires a {nameof(DnsEndPointWithProperties)} property.")));
            }

            return EstablishConnectionAsync(httpOptions!.InitialRequest, endPoint, options, cancellationToken);
        }

        /// <summary>
        /// Creates the socket to be used for a request.
        /// </summary>
        /// <param name="message">The request causing this socket to be opened. Once opened, it may be reused for many subsequent requests.</param>
        /// <param name="endPoint">The EndPoint this socket will be connected to.</param>
        /// <param name="options">Properties, if any, that might change how the socket is initialized.</param>
        /// <returns>A new unconnected socket.</returns>
        public virtual Socket CreateSocket(HttpRequestMessage message, EndPoint? endPoint, IConnectionProperties options)
        {
            return new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Establishes a new connection for a request.
        /// </summary>
        /// <param name="message">The request causing this connection to be established. Once connected, it may be reused for many subsequent requests.</param>
        /// <param name="endPoint">The EndPoint to connect to.</param>
        /// <param name="options">Properties, if any, that might change how the connection is made.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>A new open connection.</returns>
        public virtual async ValueTask<Connection> EstablishConnectionAsync(HttpRequestMessage message, EndPoint? endPoint, IConnectionProperties options, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));

            Socket socket = CreateSocket(message, endPoint, options);

            try
            {
                using var args = new TaskSocketAsyncEventArgs();
                args.RemoteEndPoint = endPoint;

                if (socket.ConnectAsync(args))
                {
                    using (cancellationToken.UnsafeRegister(o => Socket.CancelConnectAsync((SocketAsyncEventArgs)o!), args))
                    {
                        await args.Task.ConfigureAwait(false);
                    }
                }

                if (args.SocketError != SocketError.Success)
                {
                    Exception ex = args.SocketError == SocketError.OperationAborted && cancellationToken.IsCancellationRequested
                        ? (Exception)new OperationCanceledException(cancellationToken)
                        : new SocketException((int)args.SocketError);

                    throw ex;
                }

                socket.NoDelay = true;
                return new SocketConnection(socket);
            }
            catch (SocketException socketException)
            {
                socket.Dispose();
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
