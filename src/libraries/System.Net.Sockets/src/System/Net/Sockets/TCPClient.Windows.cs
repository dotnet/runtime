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
            Client = CreateSocket();
        }

        private void DisposeCore()
        {
            // Nop.  No additional state that needs to be disposed of.
        }

        // Used by the class to provide the underlying network socket.
        private Socket ClientCore
        {
            get { return _clientSocket; }
            set { _clientSocket = value; }
        }

        private int AvailableCore
        {
            get
            {
                // If we have a client socket, return its available value.
                // Otherwise, there isn't data available, so return 0.
                return _clientSocket?.Available ?? 0;
            }
        }

        private bool ConnectedCore
        {
            get
            {
                // If we have a client socket, return whether it's connected.
                // Otherwise as we don't have a socket, by definition it's not.
                return _clientSocket?.Connected ?? false;
            }
        }

        private bool ExclusiveAddressUseCore
        {
            get
            {
                return _clientSocket?.ExclusiveAddressUse ?? false;
            }
            set
            {
                if (_clientSocket != null)
                {
                    _clientSocket.ExclusiveAddressUse = value;
                }
            }
        }

        private Task ConnectAsyncCore(IPAddress address, int port)
        {
            return Task.Factory.FromAsync(
                (targetAddess, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetAddess, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                address,
                port,
                state: this);
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

        private IAsyncResult BeginConnectCore(string host, int port, AsyncCallback requestCallback, object state) =>
            Client.BeginConnect(host, port, requestCallback, state);

        private IAsyncResult BeginConnectCore(IPAddress address, int port, AsyncCallback requestCallback, object state) =>
            Client.BeginConnect(address, port, requestCallback, state);

        private IAsyncResult BeginConnectCore(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state) =>
            Client.BeginConnect(addresses, port, requestCallback, state);

        private void EndConnectCore(Socket socket, IAsyncResult asyncResult) =>
            socket.EndConnect(asyncResult);

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
