// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.TcpClient class provide TCP services at a higher level
    // of abstraction than the System.Net.Sockets.Socket class. System.Net.Sockets.TcpClient
    // is used to create a Client connection to a remote host.
    public class TcpClient : IDisposable
    {
        private Socket _clientSocket;
        private bool _active;
        private NetworkStream _dataStream;

        // IPv6: Maintain address family for the client.
        private AddressFamily _family = AddressFamily.InterNetwork;

        // Initializes a new instance of the System.Net.Sockets.TcpClient
        // class with the specified end point.
        public TcpClient(IPEndPoint localEP)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "TcpClient", localEP);
            }

            if (localEP == null)
            {
                throw new ArgumentNullException("localEP");
            }

            // IPv6: Establish address family before creating a socket
            _family = localEP.AddressFamily;

            initialize();
            Client.Bind(localEP);
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "TcpClient", "");
            }
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient()
            : this(AddressFamily.InterNetwork)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "TcpClient", null);
            }

            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "TcpClient", null);
            }
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient(AddressFamily family)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "TcpClient", family);
            }

            // Validate parameter
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "TCP"), "family");
            }

            _family = family;

            initialize();
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "TcpClient", null);
            }
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class and connects to the
        // specified port on the specified host.
        public TcpClient(string hostname, int port)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "TcpClient", hostname);
            }

            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
            }

            // IPv6: Delay creating the client socket until we have
            //       performed DNS resolution and know which address
            //       families we can use.
            try
            {
                Connect(hostname, port);
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException)
                {
                    throw;
                }

                if (_clientSocket != null)
                {
                    _clientSocket.Dispose();
                }
                throw e;
            }

            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "TcpClient", null);
            }
        }

        // Used by TcpListener.Accept().
        internal TcpClient(Socket acceptedSocket)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "TcpClient", acceptedSocket);
            }

            Client = acceptedSocket;
            _active = true;
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "TcpClient", null);
            }
        }

        // Used by the class to provide the underlying network socket.
        public Socket Client
        {
            get
            {
                return _clientSocket;
            }
            set
            {
                _clientSocket = value;
            }
        }

        // Used by the class to indicate that a connection has been made.
        protected bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;
            }
        }

        public int Available { get { return _clientSocket.Available; } }
        public bool Connected { get { return _clientSocket.Connected; } }
        public bool ExclusiveAddressUse
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

        // Connects the Client to the specified port on the specified host.
        public void Connect(string hostname, int port)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "Connect", hostname);
            }

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
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

            // IPv6: We need to process each of the addresses return from
            //       DNS when trying to connect. Use of AddressList[0] is
            //       bad form.
            IPAddress[] addresses = Dns.GetHostAddressesAsync(hostname).GetAwaiter().GetResult();
            Exception lastex = null;
            Socket ipv6Socket = null;
            Socket ipv4Socket = null;
            try
            {
                if (_clientSocket == null)
                {
                    if (Socket.OSSupportsIPv4)
                    {
                        ipv4Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    if (Socket.OSSupportsIPv6)
                    {
                        ipv6Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    }
                }

                foreach (IPAddress address in addresses)
                {
                    try
                    {
                        if (_clientSocket == null)
                        {
                            // We came via the <hostname,port> constructor. Set the
                            // address family appropriately, create the socket and
                            // try to connect.
                            if (address.AddressFamily == AddressFamily.InterNetwork && ipv4Socket != null)
                            {
                                ipv4Socket.Connect(address, port);
                                _clientSocket = ipv4Socket;
                                if (ipv6Socket != null)
                                {
                                    ipv6Socket.Dispose();
                                }
                            }
                            else if (ipv6Socket != null)
                            {
                                ipv6Socket.Connect(address, port);
                                _clientSocket = ipv6Socket;
                                if (ipv4Socket != null)
                                {
                                    ipv4Socket.Dispose();
                                }
                            }

                            _family = address.AddressFamily;
                            _active = true;
                            break;
                        }
                        else if (address.AddressFamily == _family)
                        {
                            // Only use addresses with a matching family.
                            Connect(new IPEndPoint(address, port));
                            _active = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is OutOfMemoryException)
                        {
                            throw;
                        }
                        lastex = ex;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OutOfMemoryException)
                {
                    throw;
                }
                lastex = ex;
            }
            finally
            {
                // Cleanup temp sockets if failed. Main socket gets closed when TCPClient gets closed.

                // Did we connect?
                if (!_active)
                {
                    if (ipv6Socket != null)
                    {
                        ipv6Socket.Dispose();
                    }

                    if (ipv4Socket != null)
                    {
                        ipv4Socket.Dispose();
                    }

                    // The connect failed - rethrow the last error we had.
                    if (lastex != null)
                    {
                        throw lastex;
                    }
                    else
                    {
                        throw new SocketException((int)SocketError.NotConnected);
                    }
                }
            }

            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "Connect", null);
            }
        }

        // Connects the Client to the specified port on the specified host.
        public void Connect(IPAddress address, int port)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "Connect", address);
            }

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
            }

            IPEndPoint remoteEP = new IPEndPoint(address, port);
            Connect(remoteEP);
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "Connect", null);
            }
        }

        // Connect the Client to the specified end point.
        public void Connect(IPEndPoint remoteEP)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "Connect", remoteEP);
            }

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (remoteEP == null)
            {
                throw new ArgumentNullException("remoteEP");
            }

            Client.Connect(remoteEP);
            _active = true;
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "Connect", null);
            }
        }

        public void Connect(IPAddress[] ipAddresses, int port)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "Connect", ipAddresses);
            }

            Client.Connect(ipAddresses, port);
            _active = true;
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "Connect", null);
            }
        }

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "BeginConnect", host);
            }

            IAsyncResult result = Client.BeginConnect(host, port, requestCallback, state);
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "BeginConnect", null);
            }

            return result;
        }

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "BeginConnect", address);
            }

            IAsyncResult result = Client.BeginConnect(address, port, requestCallback, state);
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "BeginConnect", null);
            }

            return result;
        }

        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "BeginConnect", addresses);
            }

            IAsyncResult result = Client.BeginConnect(addresses, port, requestCallback, state);
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "BeginConnect", null);
            }

            return result;
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "EndConnect", asyncResult);
            }

            Client.EndConnect(asyncResult);
            _active = true;
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "EndConnect", null);
            }
        }

        public Task ConnectAsync(IPAddress address, int port)
        {
            return Task.Factory.FromAsync(BeginConnect, EndConnect, address, port, null);
        }

        public Task ConnectAsync(string host, int port)
        {
            return Task.Factory.FromAsync(BeginConnect, EndConnect, host, port, null);
        }

        public Task ConnectAsync(IPAddress[] addresses, int port)
        {
            return Task.Factory.FromAsync(BeginConnect, EndConnect, addresses, port, null);
        }

        // Returns the stream used to read and write data to the remote host.
        public NetworkStream GetStream()
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "GetStream", "");
            }

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (!Client.Connected)
            {
                throw new InvalidOperationException(SR.net_notconnected);
            }

            if (_dataStream == null)
            {
                _dataStream = new NetworkStream(Client, true);
            }

            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "GetStream", _dataStream);
            }

            return _dataStream;
        }

        private bool _cleanedUp = false;

        // Disposes the Tcp connection.
        protected virtual void Dispose(bool disposing)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.Sockets, this, "Dispose", "");
            }

            if (_cleanedUp)
            {
                if (Logging.On)
                {
                    Logging.Exit(Logging.Sockets, this, "Dispose", "");
                }

                return;
            }

            if (disposing)
            {
                IDisposable dataStream = _dataStream;
                if (dataStream != null)
                {
                    dataStream.Dispose();
                }
                else
                {
                    // If the NetworkStream wasn't created, the Socket might
                    // still be there and needs to be closed. In the case in which
                    // we are bound to a local IPEndPoint this will remove the
                    // binding and free up the IPEndPoint for later uses.
                    Socket chkClientSocket = Client;
                    if (chkClientSocket != null)
                    {
                        try
                        {
                            chkClientSocket.InternalShutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            chkClientSocket.Dispose();
                            Client = null;
                        }
                    }
                }

                GC.SuppressFinalize(this);
            }

            _cleanedUp = true;
            if (Logging.On)
            {
                Logging.Exit(Logging.Sockets, this, "Dispose", "");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~TcpClient()
        {
#if DEBUG
            GlobalLog.SetThreadSource(ThreadKinds.Finalization);
            using (GlobalLog.SetThreadKind(ThreadKinds.System | ThreadKinds.Async))
            {
#endif
                Dispose(false);
#if DEBUG
            }
#endif
        }

        // Gets or sets the size of the receive buffer in bytes.
        public int ReceiveBufferSize
        {
            get
            {
                return numericOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
            }
        }

        // Gets or sets the size of the send buffer in bytes.
        public int SendBufferSize
        {
            get
            {
                return numericOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            }

            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket,
                                  SocketOptionName.SendBuffer, value);
            }
        }

        // Gets or sets the receive time out value of the connection in seconds.
        public int ReceiveTimeout
        {
            get
            {
                return numericOption(SocketOptionLevel.Socket,
                                     SocketOptionName.ReceiveTimeout);
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket,
                                  SocketOptionName.ReceiveTimeout, value);
            }
        }

        // Gets or sets the send time out value of the connection in seconds.
        public int SendTimeout
        {
            get
            {
                return numericOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
            }

            set
            {
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
            }
        }

        // Gets or sets the value of the connection's linger option.
        public LingerOption LingerState
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
        public bool NoDelay
        {
            get
            {
                return numericOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay) != 0 ? true : false;
            }
            set
            {
                Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, value ? 1 : 0);
            }
        }

        private void initialize()
        {
            // IPv6: Use the address family from the constructor (or Connect method).
            Client = new Socket(_family, SocketType.Stream, ProtocolType.Tcp);
            _active = false;
        }

        private int numericOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
        {
            return (int)Client.GetSocketOption(optionLevel, optionName);
        }
    }
}
