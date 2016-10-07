// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    public partial class TcpClient : IDisposable
    {
        // Initializes a new instance of the System.Net.Sockets.TcpClient class with the specified end point.
        public TcpClient(IPEndPoint localEP)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(TcpClient), localEP);
            }

            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }

            // IPv6: Establish address family before creating a socket
            _family = localEP.AddressFamily;

            InitializeClientSocket();
            Client.Bind(localEP);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(TcpClient), "");
            }
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class and connects to the specified port on 
        // the specified host.
        public TcpClient(string hostname, int port)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(TcpClient), hostname);
            }

            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // IPv6: Delay creating the client socket until we have
            //       performed DNS resolution and know which address
            //       families we can use.

            try
            {
                Connect(hostname, port);
            }

            catch
            {
                if (_clientSocket != null)
                {
                    _clientSocket.Close();
                }
                throw;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(TcpClient), null);
            }
        }

        // Connects the Client to the specified port on the specified host.
        public void Connect(string hostname, int port)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(Connect), hostname);
            }
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // Check for already connected and throw here. This check
            // is not required in the other connect methods as they
            // will throw from WinSock. Here, the situation is more
            // complex since we have to resolve a hostname so it's
            // easier to simply block the request up front.
            if (_active)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }

            ConnectCore(hostname, port);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(Connect), null);
            }
        }

        // Connects the Client to the specified port on the specified host.
        public void Connect(IPAddress address, int port)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(Connect), address);
            }
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            IPEndPoint remoteEP = new IPEndPoint(address, port);
            Connect(remoteEP);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(Connect), null);
            }
        }

        // Connect the Client to the specified end point.
        public void Connect(IPEndPoint remoteEP)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(Connect), remoteEP);
            }
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (remoteEP == null)
            {
                throw new ArgumentNullException(nameof(remoteEP));
            }

            Client.Connect(remoteEP);
            _active = true;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(Connect), null);
            }
        }

        public void Connect(IPAddress[] ipAddresses, int port)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(Connect), ipAddresses);
            }

            ConnectCore(ipAddresses, port);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(Connect), null);
            }
        }

        public void Close()
        {
            Dispose();
        }
    }
}
