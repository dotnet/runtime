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
    /// A <see cref="ConnectionFactory"/> to establish socket-based connections.
    /// </summary>
    /// <remarks>
    /// When constructed with <see cref="ProtocolType.Tcp"/>, this factory will create connections with <see cref="Socket.NoDelay"/> enabled.
    /// In case of IPv6 sockets <see cref="Socket.DualMode"/> is also enabled.
    /// </remarks>
    public class SocketsConnectionFactory : ConnectionFactory
    {
        private readonly AddressFamily _addressFamily;
        private readonly SocketType _socketType;
        private readonly ProtocolType _protocolType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketsConnectionFactory"/> class.
        /// </summary>
        /// <param name="addressFamily">The <see cref="AddressFamily"/> to forward to the socket.</param>
        /// <param name="socketType">The <see cref="SocketType"/> to forward to the socket.</param>
        /// <param name="protocolType">The <see cref="ProtocolType"/> to forward to the socket.</param>
        public SocketsConnectionFactory(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketsConnectionFactory"/> class
        /// that will forward <see cref="AddressFamily.InterNetworkV6"/> to the Socket constructor.
        /// </summary>
        /// <param name="socketType">The <see cref="SocketType"/> to forward to the socket.</param>
        /// <param name="protocolType">The <see cref="ProtocolType"/> to forward to the socket.</param>
        /// <remarks>The created socket will be an IPv6 socket with <see cref="Socket.DualMode"/> enabled.</remarks>
        public SocketsConnectionFactory(SocketType socketType, ProtocolType protocolType)
            : this(AddressFamily.InterNetworkV6, socketType, protocolType)
        {
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">When <paramref name="endPoint"/> is <see langword="null"/>.</exception>
        public override async ValueTask<Connection> ConnectAsync(
            EndPoint? endPoint,
            IConnectionProperties? options = null,
            CancellationToken cancellationToken = default)
        {
            if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
            cancellationToken.ThrowIfCancellationRequested();

            Socket socket = CreateSocket(_addressFamily, _socketType, _protocolType, endPoint, options);

            try
            {
                await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Creates the socket that shall be used with the connection.
        /// </summary>
        /// <param name="addressFamily">The <see cref="AddressFamily"/> to forward to the socket.</param>
        /// <param name="socketType">The <see cref="SocketType"/> to forward to the socket.</param>
        /// <param name="protocolType">The <see cref="ProtocolType"/> to forward to the socket.</param>
        /// <param name="endPoint">The <see cref="EndPoint"/> this socket will be connected to.</param>
        /// <param name="options">Properties, if any, that might change how the socket is initialized.</param>
        /// <returns>A new unconnected <see cref="Socket"/>.</returns>
        /// <remarks>
        /// In case of TCP sockets, the default implementation of this method will create a socket with <see cref="Socket.NoDelay"/> enabled.
        /// In case of IPv6 sockets <see cref="Socket.DualMode"/> is also be enabled.
        /// </remarks>
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
    }
}
