// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.TcpClient class provide TCP services at a higher level
    // of abstraction than the System.Net.Sockets.Socket class. System.Net.Sockets.TcpClient
    // is used to create a Client connection to a remote host.
    public class TcpClient : IDisposable
    {
        private AddressFamily _family;
        private Socket _clientSocket = null!; // initialized by helper called from ctor
        private NetworkStream? _dataStream;
        private volatile int _disposed;
        private bool _active;

        private bool Disposed => _disposed != 0;

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient() : this(AddressFamily.Unknown)
        {
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient(AddressFamily family)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, family);

            if (family is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 or AddressFamily.Unknown))
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "TCP"), nameof(family));
            }

            _family = family;
            InitializeClientSocket();
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class with the specified end point.
        public TcpClient(IPEndPoint localEP)
        {
            ArgumentNullException.ThrowIfNull(localEP);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, localEP);
            _family = localEP.AddressFamily; // set before calling CreateSocket
            InitializeClientSocket();
            _clientSocket.Bind(localEP);
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class and connects to the specified port on
        // the specified host.
        public TcpClient(string hostname, int port)
        {
            ArgumentNullException.ThrowIfNull(hostname);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, hostname);
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _family = AddressFamily.Unknown;

            try
            {
                Connect(hostname, port);
            }
            catch
            {
                _clientSocket?.Close();
                throw;
            }
        }

        // Used by TcpListener.Accept().
        internal TcpClient(Socket acceptedSocket)
        {
            _clientSocket = acceptedSocket;
            _active = true;
        }

        // Used by the class to indicate that a connection has been made.
        protected bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        public int Available => Client?.Available ?? 0;

        // Used by the class to provide the underlying network socket.
        public Socket Client
        {
            get { return Disposed ? null! : _clientSocket; }
            set
            {
                _clientSocket = value;
                _family = _clientSocket?.AddressFamily ?? AddressFamily.Unknown;
            }
        }

        public bool Connected => Client?.Connected ?? false;

        public bool ExclusiveAddressUse
        {
            get { return Client?.ExclusiveAddressUse ?? false; }
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
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(hostname);
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
            ExceptionDispatchInfo? lastex = null;

            try
            {
                foreach (IPAddress address in addresses)
                {
                    try
                    {
                        if (_clientSocket == null)
                        {
                            // We came via the <hostname,port> constructor. Set the address family appropriately,
                            // create the socket and try to connect.
                            Debug.Assert(address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6);
                            if ((address.AddressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv4) || Socket.OSSupportsIPv6)
                            {
                                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                                if (address.IsIPv4MappedToIPv6)
                                {
                                    socket.DualMode = true;
                                }

                                // Use of Interlocked.Exchanged ensures _clientSocket is written before Disposed is read.
                                Interlocked.Exchange(ref _clientSocket!, socket);
                                if (Disposed)
                                {
                                    // Dispose the socket so it throws ObjectDisposedException when we Connect.
                                    socket.Dispose();
                                }

                                try
                                {
                                    socket.Connect(address, port);
                                }
                                catch
                                {
                                    _clientSocket = null!;
                                    throw;
                                }
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
        }

        // Connects the Client to the specified port on the specified host.
        public void Connect(IPAddress address, int port)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(address);
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            IPEndPoint remoteEP = new IPEndPoint(address, port);
            Connect(remoteEP);
        }

        // Connect the Client to the specified end point.
        public void Connect(IPEndPoint remoteEP)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(remoteEP);

            Client.Connect(remoteEP);
            _family = Client.AddressFamily;
            _active = true;
        }

        public void Connect(IPAddress[] ipAddresses, int port)
        {
            Client.Connect(ipAddresses, port);
            _family = Client.AddressFamily;
            _active = true;
        }

        public Task ConnectAsync(IPAddress address, int port) =>
            CompleteConnectAsync(Client.ConnectAsync(address, port));

        public Task ConnectAsync(string host, int port) =>
            CompleteConnectAsync(Client.ConnectAsync(host, port));

        public Task ConnectAsync(IPAddress[] addresses, int port) =>
            CompleteConnectAsync(Client.ConnectAsync(addresses, port));

        /// <summary>
        /// Connects the client to a remote TCP host using the specified endpoint as an asynchronous operation.
        /// </summary>
        /// <param name="remoteEP">The <see cref="IPEndPoint"/> to which you intend to connect.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ConnectAsync(IPEndPoint remoteEP) =>
            CompleteConnectAsync(Client.ConnectAsync(remoteEP));

        private async Task CompleteConnectAsync(Task task)
        {
            await task.ConfigureAwait(false);
            _active = true;
        }

        public ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken) =>
            CompleteConnectAsync(Client.ConnectAsync(address, port, cancellationToken));

        public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken) =>
            CompleteConnectAsync(Client.ConnectAsync(host, port, cancellationToken));

        public ValueTask ConnectAsync(IPAddress[] addresses, int port, CancellationToken cancellationToken) =>
            CompleteConnectAsync(Client.ConnectAsync(addresses, port, cancellationToken));

        /// <summary>
        /// Connects the client to a remote TCP host using the specified endpoint as an asynchronous operation.
        /// </summary>
        /// <param name="remoteEP">The <see cref="IPEndPoint"/> to which you intend to connect.</param>
        /// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public ValueTask ConnectAsync(IPEndPoint remoteEP, CancellationToken cancellationToken) =>
            CompleteConnectAsync(Client.ConnectAsync(remoteEP, cancellationToken));

        private async ValueTask CompleteConnectAsync(ValueTask task)
        {
            await task.ConfigureAwait(false);
            _active = true;
        }

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback? requestCallback, object? state) =>
            Client.BeginConnect(address, port, requestCallback, state);

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback? requestCallback, object? state) =>
            Client.BeginConnect(host, port, requestCallback, state);

        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback? requestCallback, object? state) =>
            Client.BeginConnect(addresses, port, requestCallback, state);

        public void EndConnect(IAsyncResult asyncResult)
        {
            _clientSocket.EndConnect(asyncResult);
            _active = true;

        }

        // Returns the stream used to read and write data to the remote host.
        public NetworkStream GetStream()
        {
            ThrowIfDisposed();

            if (!Connected)
            {
                throw new InvalidOperationException(SR.net_notconnected);
            }

            if (_dataStream == null)
            {
                _dataStream = new NetworkStream(Client, true);
            }

            return _dataStream;
        }

        public void Close() => Dispose();

        // Disposes the Tcp connection.
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    IDisposable? dataStream = _dataStream;
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
                        Socket chkClientSocket = Volatile.Read(ref _clientSocket);
                        if (chkClientSocket != null)
                        {
                            try
                            {
                                chkClientSocket.InternalShutdown(SocketShutdown.Both);
                            }
                            finally
                            {
                                chkClientSocket.Close();
                            }
                        }
                    }

                    GC.SuppressFinalize(this);
                }
            }
        }

        public void Dispose() => Dispose(true);

        ~TcpClient() => Dispose(false);

        // Gets or sets the size of the receive buffer in bytes.
        public int ReceiveBufferSize
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)!; }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value); }
        }

        // Gets or sets the size of the send buffer in bytes.
        public int SendBufferSize
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer)!; }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value); }
        }

        // Gets or sets the receive time out value of the connection in milliseconds.
        public int ReceiveTimeout
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout)!; }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value); }
        }

        // Gets or sets the send time out value of the connection in milliseconds.
        public int SendTimeout
        {
            get { return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout)!; }
            set { Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value); }
        }

        // Gets or sets the value of the connection's linger option.
        [DisallowNull]
        public LingerOption? LingerState
        {
            get { return Client.LingerState; }
            set { Client.LingerState = value!; }
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

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
        }
    }
}
