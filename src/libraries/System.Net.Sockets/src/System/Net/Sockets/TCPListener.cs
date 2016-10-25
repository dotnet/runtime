// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.TcpListener class provide TCP services at a higher level of abstraction
    // than the System.Net.Sockets.Socket class. System.Net.Sockets.TcpListener is used to create a
    // host process that listens for connections from TCP clients.
    public class TcpListener
    {
        private IPEndPoint _serverSocketEP;
        private Socket _serverSocket;
        private bool _active;
        private bool _exclusiveAddressUse;

        // Initializes a new instance of the TcpListener class with the specified local end point.
        public TcpListener(IPEndPoint localEP)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "TcpListener", localEP);
            }

            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }
            _serverSocketEP = localEP;
            _serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "TcpListener", null);
            }
        }

        // Initializes a new instance of the TcpListener class that listens to the specified IP address
        // and port.
        public TcpListener(IPAddress localaddr, int port)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "TcpListener", localaddr);
            }

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
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "TcpListener", null);
            }
        }

        // Initiailizes a new instance of the TcpListener class that listens on the specified port.
        [Obsolete("This method has been deprecated. Please use TcpListener(IPAddress localaddr, int port) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
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
                return _serverSocket;
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
                return _active ? _serverSocket.LocalEndPoint : _serverSocketEP;
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return _serverSocket.ExclusiveAddressUse;
            }
            set
            {
                if (_active)
                {
                    throw new InvalidOperationException(SR.net_tcplistener_mustbestopped);
                }

                _serverSocket.ExclusiveAddressUse = value;
                _exclusiveAddressUse = value;
            }
        }

        public void AllowNatTraversal(bool allowed)
        {
            if (_active)
            {
                throw new InvalidOperationException(SR.net_tcplistener_mustbestopped);
            }

            _serverSocket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
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

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "Start", null);
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("TCPListener::Start()");
            }

            if (_serverSocket == null)
            {
                throw new InvalidOperationException(SR.net_InvalidSocketHandle);
            }

            // Already listening.
            if (_active)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "Start", null);
                }

                return;
            }

            _serverSocket.Bind(_serverSocketEP);
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
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "Start", null);
            }
        }

        // Closes the network connection.
        public void Stop()
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "Stop", null);
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("TCPListener::Stop()");
            }

            if (_serverSocket != null)
            {
                _serverSocket.Dispose();
                _serverSocket = null;
            }

            _active = false;
            _serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (_exclusiveAddressUse)
            {
                _serverSocket.ExclusiveAddressUse = true;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "Stop", null);
            }
        }

        // Determine if there are pending connection requests.
        public bool Pending()
        {
            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            return _serverSocket.Poll(0, SelectMode.SelectRead);
        }

        // Accept the first pending connection
        public Socket AcceptSocket()
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(AcceptSocket), null);
            }

            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            Socket socket = _serverSocket.Accept();

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(AcceptSocket), socket);
            }
            return socket;
        }

        public TcpClient AcceptTcpClient()
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, nameof(AcceptTcpClient), null);
            }

            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            Socket acceptedSocket = _serverSocket.Accept();
            TcpClient returnValue = new TcpClient(acceptedSocket);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, nameof(AcceptTcpClient), returnValue);
            }
            return returnValue;
        }

        public IAsyncResult BeginAcceptSocket(AsyncCallback callback, object state)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "BeginAcceptSocket", null);
            }

            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            IAsyncResult result = _serverSocket.BeginAccept(callback, state);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "BeginAcceptSocket", null);
            }

            return result;
        }

        public Socket EndAcceptSocket(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "EndAcceptSocket", null);
            }

            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            LazyAsyncResult lazyResult = asyncResult as LazyAsyncResult;
            Socket asyncSocket = lazyResult == null ? null : lazyResult.AsyncObject as Socket;
            if (asyncSocket == null)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            // This will throw ObjectDisposedException if Stop() has been called.
            Socket socket = asyncSocket.EndAccept(asyncResult);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "EndAcceptSocket", socket);
            }

            return socket;
        }

        public IAsyncResult BeginAcceptTcpClient(AsyncCallback callback, object state)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "BeginAcceptTcpClient", null);
            }

            if (!_active)
            {
                throw new InvalidOperationException(SR.net_stopped);
            }

            IAsyncResult result = _serverSocket.BeginAccept(callback, state);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "BeginAcceptTcpClient", null);
            }

            return result;
        }

        public TcpClient EndAcceptTcpClient(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "EndAcceptTcpClient", null);
            }

            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            LazyAsyncResult lazyResult = asyncResult as LazyAsyncResult;
            Socket asyncSocket = lazyResult == null ? null : lazyResult.AsyncObject as Socket;
            if (asyncSocket == null)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }

            // This will throw ObjectDisposedException if Stop() has been called.
            Socket socket = asyncSocket.EndAccept(asyncResult);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "EndAcceptTcpClient", socket);
            }

            return new TcpClient(socket);
        }

        public Task<Socket> AcceptSocketAsync()
        {
            return Task<Socket>.Factory.FromAsync(
                (callback, state) => ((TcpListener)state).BeginAcceptSocket(callback, state),
                asyncResult => ((TcpListener)asyncResult.AsyncState).EndAcceptSocket(asyncResult),
                state: this);
        }

        public Task<TcpClient> AcceptTcpClientAsync()
        {
            return Task<TcpClient>.Factory.FromAsync(
                (callback, state) => ((TcpListener)state).BeginAcceptTcpClient(callback, state),
                asyncResult => ((TcpListener)asyncResult.AsyncState).EndAcceptTcpClient(asyncResult),
                state: this);
        }


        // This creates a TcpListener that listens on both IPv4 and IPv6 on the given port.
        public static TcpListener Create(int port)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, "TcpListener.Create", "Port: " + port, null);
            }

            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            TcpListener listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.DualMode = true;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, "TcpListener.Create", "Port: " + port, null);
            }

            return listener;
        }
    }
}
