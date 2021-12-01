// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.TcpListener class provide TCP services at a higher level of abstraction
    // than the System.Net.Sockets.Socket class. System.Net.Sockets.TcpListener is used to create a
    // host process that listens for connections from TCP clients.
    public class TcpListener
    {
        private readonly IPEndPoint _serverSocketEP;
        private Socket? _serverSocket;
        private bool _active;
        private bool _exclusiveAddressUse;
        private bool? _allowNatTraversal;

        // Initializes a new instance of the TcpListener class with the specified local end point.
        public TcpListener(IPEndPoint localEP)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, localEP);

            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }
            _serverSocketEP = localEP;
            _serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        // Initializes a new instance of the TcpListener class that listens to the specified IP address
        // and port.
        public TcpListener(IPAddress localaddr, int port)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, localaddr);

            if (localaddr == null)
            {
                throw new ArgumentNullException(nameof(localaddr));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _serverSocketEP = new IPEndPoint(localaddr, port);
            _serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        // Initiailizes a new instance of the TcpListener class that listens on the specified port.
        [Obsolete("This constructor has been deprecated. Use TcpListener(IPAddress localaddr, int port) instead.")]
        public TcpListener(int port)
        {
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _serverSocketEP = new IPEndPoint(IPAddress.Any, port);
            _serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        // Used by the class to provide the underlying network socket.
        public Socket Server
        {
            get
            {
                CreateNewSocketIfNeeded();
                return _serverSocket!;
            }
        }

        // Used by the class to indicate that the listener's socket has been bound to a port
        // and started listening.
        protected bool Active
        {
            get
            {
                return _active;
            }
        }

        // Gets the m_Active EndPoint for the local listener socket.
        public EndPoint LocalEndpoint
        {
            get
            {
                return _active ? _serverSocket!.LocalEndPoint! : _serverSocketEP;
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return _serverSocket != null ? _serverSocket.ExclusiveAddressUse : _exclusiveAddressUse;
            }
            set
            {
                if (_active)
                {
                    throw new InvalidOperationException(SR.net_tcplistener_mustbestopped);
                }

                if (_serverSocket != null)
                {
                    _serverSocket.ExclusiveAddressUse = value;
                }
                _exclusiveAddressUse = value;
            }
        }

        [SupportedOSPlatform("windows")]
        public void AllowNatTraversal(bool allowed)
        {
            if (_active)
            {
                throw new InvalidOperationException(SR.net_tcplistener_mustbestopped);
            }

            if (_serverSocket != null)
            {
                SetIPProtectionLevel(allowed); // Set it only for the current socket to preserve existing behavior
            }
            else
            {
                _allowNatTraversal = allowed;
            }
        }

        // Starts listening to network requests.
        public void Start()
        {
            Start((int)SocketOptionName.MaxConnections);
        }

        public void Start(int backlog)
        {
            if (backlog > (int)SocketOptionName.MaxConnections || backlog < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(backlog));
            }

            // Already listening.
            if (_active)
            {
                return;
            }

            CreateNewSocketIfNeeded();

            _serverSocket!.Bind(_serverSocketEP);
            try
            {
                _serverSocket.Listen(backlog);
            }
            catch (SocketException)
            {
                // When there is an exception, unwind previous actions (bind, etc).
                Stop();
                throw;
            }

            _active = true;
        }

        // Closes the network connection.
        public void Stop()
        {
            _serverSocket?.Dispose();
            _active = false;
            _serverSocket = null;
        }

        // Determine if there are pending connection requests.
        public bool Pending()
        {
            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            return _serverSocket!.Poll(0, SelectMode.SelectRead);
        }

        // Accept the first pending connection
        public Socket AcceptSocket()
        {
            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            return _serverSocket!.Accept();
        }

        public TcpClient AcceptTcpClient()
        {
            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            Socket acceptedSocket = _serverSocket!.Accept();
            return new TcpClient(acceptedSocket);
        }

        public IAsyncResult BeginAcceptSocket(AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(AcceptSocketAsync(), callback, state);

        public Socket EndAcceptSocket(IAsyncResult asyncResult) =>
            EndAcceptCore<Socket>(asyncResult);

        public IAsyncResult BeginAcceptTcpClient(AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(AcceptTcpClientAsync(), callback, state);

        public TcpClient EndAcceptTcpClient(IAsyncResult asyncResult) =>
            EndAcceptCore<TcpClient>(asyncResult);

        public Task<Socket> AcceptSocketAsync() => AcceptSocketAsync(CancellationToken.None).AsTask();

        public ValueTask<Socket> AcceptSocketAsync(CancellationToken cancellationToken)
        {
            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            return _serverSocket!.AcceptAsync(cancellationToken);
        }

        public Task<TcpClient> AcceptTcpClientAsync() => AcceptTcpClientAsync(CancellationToken.None).AsTask();

        public ValueTask<TcpClient> AcceptTcpClientAsync(CancellationToken cancellationToken)
        {
            return WaitAndWrap(AcceptSocketAsync(cancellationToken));

            static async ValueTask<TcpClient> WaitAndWrap(ValueTask<Socket> task) =>
                new TcpClient(await task.ConfigureAwait(false));
        }

        // This creates a TcpListener that listens on both IPv4 and IPv6 on the given port.
        public static TcpListener Create(int port)
        {
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            TcpListener listener;
            if (Socket.OSSupportsIPv6)
            {
                // If OS supports IPv6 use dual mode so both address families work.
                listener = new TcpListener(IPAddress.IPv6Any, port);
                listener.Server.DualMode = true;
            }
            else
            {
                // If not, fall-back to old IPv4.
                listener = new TcpListener(IPAddress.Any, port);
            }

            return listener;
        }

        [SupportedOSPlatform("windows")]
        private void SetIPProtectionLevel(bool allowed)
            => _serverSocket!.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);

        private void CreateNewSocketIfNeeded()
        {
            _serverSocket ??= new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (_exclusiveAddressUse)
            {
                _serverSocket.ExclusiveAddressUse = true;
            }

            if (_allowNatTraversal != null)
            {
                Debug.Assert(OperatingSystem.IsWindows());
                SetIPProtectionLevel(_allowNatTraversal.GetValueOrDefault());
                _allowNatTraversal = null; // Reset value to avoid affecting more sockets
            }
        }

        private TResult EndAcceptCore<TResult>(IAsyncResult asyncResult)
        {
            try
            {
                return TaskToApm.End<TResult>(asyncResult);
            }
            catch (SocketException) when (!_active)
            {
                // Socket.EndAccept(iar) throws ObjectDisposedException when the underlying socket gets closed.
                // TcpClient's documented behavior was to propagate that exception, we need to emulate it for compatibility:
                throw new ObjectDisposedException(typeof(Socket).FullName);
            }
        }
    }
}
