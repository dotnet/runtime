// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.Sockets
{
    partial class TcpClient
    {
        private void InitializeClientSocket()
        {
            _clientSocket = CreateSocket();
        }

        private Socket Client { get { return _clientSocket; } }

        private int AvailableCore { get { return _clientSocket.Available; } }

        private bool ConnectedCore { get { return _clientSocket.Connected; } }

        private bool ExclusiveAddressUseCore
        {
            get
            {
                return _clientSocket.ExclusiveAddressUse;
            }
            set
            {
                _clientSocket.ExclusiveAddressUse = value;
            }
        }

        private IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "BeginConnect", host);
            }

            IAsyncResult result = Client.BeginConnect(host, port, requestCallback, state);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "BeginConnect", null);
            }

            return result;
        }

        private IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "BeginConnect", addresses);
            }

            IAsyncResult result = Client.BeginConnect(addresses, port, requestCallback, state);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "BeginConnect", null);
            }

            return result;
        }

        private Task ConnectAsyncCore(string host, int port)
        {
            return Task.Factory.FromAsync(
                (targetHost, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetHost, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                host,
                port,
                state: this);
        }

        private Task ConnectAsyncCore(IPAddress[] addresses, int port)
        {
            return Task.Factory.FromAsync(
                (targetAddresses, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetAddresses, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                addresses,
                port,
                state: this);
        }

        // Gets or sets the size of the receive buffer in bytes.
        private int ReceiveBufferSizeCore
        {
            get
            {
                return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
            }
        }

        // Gets or sets the size of the send buffer in bytes.
        private int SendBufferSizeCore
        {
            get
            {
                return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value);
            }
        }

        // Gets or sets the receive time out value of the connection in milliseconds.
        private int ReceiveTimeoutCore
        {
            get
            {
                return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value);
            }
        }

        // Gets or sets the send time out value of the connection in milliseconds.
        private int SendTimeoutCore
        {
            get
            {
                return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
            }
        }

        // Gets or sets the value of the connection's linger option.
        private LingerOption LingerStateCore
        {
            get
            {
                return (LingerOption)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, value);
            }
        }

        // Enables or disables delay when send or receive buffers are full.
        private bool NoDelayCore
        {
            get
            {
                return (int)Client.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay) != 0;
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, value ? 1 : 0);
            }
        }
    }
}
