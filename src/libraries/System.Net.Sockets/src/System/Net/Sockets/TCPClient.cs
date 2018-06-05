// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.TcpClient class provide TCP services at a higher level
    // of abstraction than the System.Net.Sockets.Socket class. System.Net.Sockets.TcpClient
    // is used to create a Client connection to a remote host.
    public class TcpClient : IDisposable
    {
        private AddressFamily _family;
        private Socket _clientSocket;
        private NetworkStream _dataStream;
        private bool _cleanedUp;
        private bool _active;

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient() : this(AddressFamily.Unknown)
        {
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient(AddressFamily family)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, family);

            // Validate parameter
            if (family != AddressFamily.InterNetwork &&
                family != AddressFamily.InterNetworkV6 &&
                family != AddressFamily.Unknown)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "TCP"), nameof(family));
            }

            _family = family;
            InitializeClientSocket();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class with the specified end point.
        public TcpClient(IPEndPoint localEP)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, localEP);

            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }

            _family = localEP.AddressFamily; // set before calling CreateSocket
            InitializeClientSocket();
            _clientSocket.Bind(localEP);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class and connects to the specified port on 
        // the specified host.
        public TcpClient(string hostname, int port)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, hostname);

            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            try
            {
                Connect(hostname, port);
            }
            catch
            {
                _clientSocket?.Close();
                throw;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Used by TcpListener.Accept().
        internal TcpClient(Socket acceptedSocket)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, acceptedSocket);

            _clientSocket = acceptedSocket;
            _active = true;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Used by the class to indicate that a connection has been made.
        protected bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        public int Available => _clientSocket?.Available ?? 0;

        // Used by the class to provide the underlying network socket.
        public Socket Client
        {
            get { return _clientSocket; }
            set
            {
                _clientSocket = value;
                _family = _clientSocket?.AddressFamily ?? AddressFamily.Unknown;
            }
        }

        public bool Connected => _clientSocket?.Connected ?? false;

        public bool ExclusiveAddressUse
        {
            get { return _clientSocket?.ExclusiveAddressUse ?? false; }
            set
            {
                if (_clientSocket != null)
                {
                    _clientSocket.ExclusiveAddressUse = value;
                }
            }
        }

        // Connects the Client to the specified port on the specified host.
        public void Connect(string hostname, int port)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, hostname);

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(GetType().FullName);
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

            // IPv6: We need to process each of the addresses returned from
            //       DNS when trying to connect. Use of AddressList[0] is
            //       bad form.
            IPAddress[] addresses = Dns.GetHostAddresses(hostname);
            ExceptionDispatchInfo lastex = null;

            try
            {
                foreach (IPAddress address in addresses)
                {
                    Socket tmpSocket = null;
                    try
                    {
                        if (_clientSocket == null)
                        {
                            // We came via the <hostname,port> constructor. Set the address family appropriately,
                            // create the socket and try to connect.
                            Debug.Assert(address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6);
                            if ((address.AddressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv4) || Socket.OSSupportsIPv6)
                            {
                                tmpSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                                tmpSocket.Connect(address, port);
                                _clientSocket = tmpSocket;
                                tmpSocket = null;
                            }

                            _family = address.AddressFamily;
                            _active = true;
                            break;
                        }
                        else if (address.AddressFamily == _family || _family == AddressFamily.Unknown)
                        {
                            // Only use addresses with a matching family
                            Connect(new IPEndPoint(address, port));
                            _active = true;
                            break;
                        }
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                        if (tmpSocket != null)
                        {
                            tmpSocket.Dispose();
                            tmpSocket = null;
                        }
                        lastex = ExceptionDispatchInfo.Capture(ex);
                    }
                }
            }
            finally
            {
                if (!_active)
                {
                    // The connect failed - rethrow the last error we had
                    lastex?.Throw();
                    throw new SocketException((int)SocketError.NotConnected);
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Connects the Client to the specified port on the specified host.
        public void Connect(IPAddress address, int port)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, address);

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(GetType().FullName);
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

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Connect the Client to the specified end point.
        public void Connect(IPEndPoint remoteEP)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, remoteEP);

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (remoteEP == null)
            {
                throw new ArgumentNullException(nameof(remoteEP));
            }

            Client.Connect(remoteEP);
            _family = Client.AddressFamily;
            _active = true;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public void Connect(IPAddress[] ipAddresses, int port)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, ipAddresses);

            Client.Connect(ipAddresses, port);
            _family = Client.AddressFamily;
            _active = true;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public Task ConnectAsync(IPAddress address, int port) =>
            Task.Factory.FromAsync(
                (targetAddess, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetAddess, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                address, port, state: this);

        public Task ConnectAsync(string host, int port) =>
            Task.Factory.FromAsync(
                (targetHost, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetHost, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                host, port, state: this);

        public Task ConnectAsync(IPAddress[] addresses, int port) =>
            Task.Factory.FromAsync(
                (targetAddresses, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetAddresses, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                addresses, port, state: this);

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, address);

            IAsyncResult result = Client.BeginConnect(address, port, requestCallback, state);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, (string)host);

            IAsyncResult result = Client.BeginConnect(host, port, requestCallback, state);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, addresses);

            IAsyncResult result = Client.BeginConnect(addresses, port, requestCallback, state);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, asyncResult);

            Socket s = Client;
            if (s == null)
            {
                // Dispose nulls out the client socket field.
                throw new ObjectDisposedException(GetType().Name);
            }

            s.EndConnect(asyncResult);
            _active = true;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // Returns the stream used to read and write data to the remote host.
        public NetworkStream GetStream()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                throw new InvalidOperationException(SR.net_notconnected);
            }

            if (_dataStream == null)
            {
                _dataStream = new NetworkStream(Client, true);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this, _dataStream);
            return _dataStream;
        }

        public void Close() => Dispose();

        // Disposes the Tcp connection.
        protected virtual void Dispose(bool disposing)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (_cleanedUp)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
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
                    Socket chkClientSocket = _clientSocket;
                    if (chkClientSocket != null)
                    {
                        try
                        {
                            chkClientSocket.InternalShutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            chkClientSocket.Close();
                            _clientSocket = null;
                        }
                    }
                }

                GC.SuppressFinalize(this);
            }

            _cleanedUp = true;
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public void Dispose() => Dispose(true);

        ~TcpClient()
        {
#if DEBUG
            DebugThreadTracking.SetThreadSource(ThreadKinds.Finalization);
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.System | ThreadKinds.Async))
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
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer); }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value); }
        }

        // Gets or sets the size of the send buffer in bytes.
        public int SendBufferSize
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer); }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value); }
        }

        // Gets or sets the receive time out value of the connection in milliseconds.
        public int ReceiveTimeout
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout); }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value); }
        }

        // Gets or sets the send time out value of the connection in milliseconds.
        public int SendTimeout
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout); }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value); }
        }

        // Gets or sets the value of the connection's linger option.
        public LingerOption LingerState
        {
            get { return Client.LingerState; }
            set { Client.LingerState = value; }
        }

        // Enables or disables delay when send or receive buffers are full.
        public bool NoDelay
        {
            get { return Client.NoDelay; }
            set { Client.NoDelay = value; }
        }

        private void InitializeClientSocket()
        {
            Debug.Assert(_clientSocket == null);
            if (_family == AddressFamily.Unknown)
            {
                // If AF was not explicitly set try to initialize dual mode socket or fall-back to IPv4.
                _clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                if (_clientSocket.AddressFamily == AddressFamily.InterNetwork)
                {
                    _family = AddressFamily.InterNetwork;
                }
            }
            else
            {
                _clientSocket = new Socket(_family, SocketType.Stream, ProtocolType.Tcp);
            }
        }
    }
}
