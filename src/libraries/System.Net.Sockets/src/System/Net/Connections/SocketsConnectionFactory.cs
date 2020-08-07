// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// TODO
    /// </summary>
    public class SocketsConnectionFactory : ConnectionFactory, SocketConnection.IDataChannelProvider
    {
        private readonly AddressFamily _addressFamily;
        private readonly SocketType _socketType;
        private readonly ProtocolType _protocolType;

        // use same message as the default ctor
        private static readonly string s_cancellationMessage = new OperationCanceledException().Message;

        // See Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        public SocketsConnectionFactory(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        // dual-mode IPv6 socket. See Socket(SocketType socketType, ProtocolType protocolType)
        public SocketsConnectionFactory(SocketType socketType, ProtocolType protocolType)
            : this(AddressFamily.InterNetworkV6, socketType, protocolType)
        {
        }

        // This must be thread-safe!
        public override async ValueTask<Connection> ConnectAsync(
            EndPoint? endPoint,
            IConnectionProperties? options = null,
            CancellationToken cancellationToken = default)
        {
            if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));

            Socket socket = CreateSocket(_addressFamily, _socketType, _protocolType, endPoint, options);

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
                    SocketException ex = new SocketException((int)args.SocketError);
                    if (args.SocketError == SocketError.OperationAborted && cancellationToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException(s_cancellationMessage, ex, cancellationToken);
                    }

                    throw NetworkErrorHelper.MapSocketException(ex);
                }

                return new SocketConnection(socket, this, options);
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

        // These exist to provide an easy way to shim the default behavior.
        // Note: Connect must call this to create its socket.
        protected virtual Socket CreateSocket(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            EndPoint? endPoint,
            IConnectionProperties? options)
        {
            Socket socket = new Socket(addressFamily, socketType, protocolType);

            if (protocolType == ProtocolType.Tcp)
            {
                socket.NoDelay = true;
            }

            if (addressFamily == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = true;
            }

            return socket;
        }

        protected virtual Stream CreateStream(Socket socket, IConnectionProperties? options) => new NetworkStream(socket, ownsSocket: true);

        protected virtual IDuplexPipe CreatePipe(Socket socket, IConnectionProperties? options) => new SocketConnection.DuplexStreamPipe(CreateStream(socket, options));

        Stream SocketConnection.IDataChannelProvider.CreateStreamForConnection(Socket socket, IConnectionProperties options) => CreateStream(socket, options);

        IDuplexPipe SocketConnection.IDataChannelProvider.CreatePipeForConnection(Socket socket, IConnectionProperties options) => CreatePipe(socket, options);
    }
}
