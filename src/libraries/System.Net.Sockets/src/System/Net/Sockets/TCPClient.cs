// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.TcpClient class provide TCP services at a higher level
    // of abstraction than the System.Net.Sockets.Socket class. System.Net.Sockets.TcpClient
    // is used to create a Client connection to a remote host.
    public partial class TcpClient : IDisposable
    {
        private readonly AddressFamily _family;
        private Socket _clientSocket;
        private NetworkStream _dataStream;
        private bool _cleanedUp = false;
        private bool _active;

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient() : this(AddressFamily.InterNetwork)
        {
        }

        // Initializes a new instance of the System.Net.Sockets.TcpClient class.
        public TcpClient(AddressFamily family)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "TcpClient", family);
            }

            // Validate parameter
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "TCP"), "family");
            }

            _family = family;
            InitializeClientSocket();

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "TcpClient", null);
            }
        }

        // Used by TcpListener.Accept().
        internal TcpClient(Socket acceptedSocket)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "TcpClient", acceptedSocket);
            }

            _clientSocket = acceptedSocket;
            _active = true;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "TcpClient", null);
            }
        }

        // Used by the class to provide the underlying network socket.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // TODO: Remove once https://github.com/dotnet/corefx/issues/5868 is addressed.
        public Socket Client
        {
            get { return ClientCore; }
            set { ClientCore = value; }
        }

        // Used by the class to indicate that a connection has been made.
        protected bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        public int Available { get { return AvailableCore; } }

        public bool Connected { get { return ConnectedCore; } }

        public bool ExclusiveAddressUse
        {
            get { return ExclusiveAddressUseCore; }
            set { ExclusiveAddressUseCore = value; }
        }

        public Task ConnectAsync(IPAddress address, int port)
        {
            return Task.Factory.FromAsync(
                (targetAddess, targetPort, callback, state) => ((TcpClient)state).BeginConnect(targetAddess, targetPort, callback, state),
                asyncResult => ((TcpClient)asyncResult.AsyncState).EndConnect(asyncResult),
                address,
                port,
                state: this);
        }

        public Task ConnectAsync(string host, int port)
        {
            return ConnectAsyncCore(host, port);
        }

        public Task ConnectAsync(IPAddress[] addresses, int port)
        {
            return ConnectAsyncCore(addresses, port);
        }

        private IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "BeginConnect", address);
            }

            IAsyncResult result = Client.BeginConnect(address, port, requestCallback, state);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "BeginConnect", null);
            }

            return result;
        }

        private void EndConnect(IAsyncResult asyncResult)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "EndConnect", asyncResult);
            }

            Client.EndConnect(asyncResult);
            _active = true;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "EndConnect", null);
            }
        }

        // Returns the stream used to read and write data to the remote host.
        public NetworkStream GetStream()
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "GetStream", "");
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

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "GetStream", _dataStream);
            }

            return _dataStream;
        }

        // Disposes the Tcp connection.
        protected virtual void Dispose(bool disposing)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Socket, this, "Dispose", "");
            }

            if (_cleanedUp)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "Dispose", "");
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
                    Socket chkClientSocket = _clientSocket;
                    if (chkClientSocket != null)
                    {
                        try
                        {
                            chkClientSocket.InternalShutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            chkClientSocket.Dispose();
                            _clientSocket = null;
                        }
                    }
                }

                GC.SuppressFinalize(this);
            }

            _cleanedUp = true;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Socket, this, "Dispose", "");
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
            get { return ReceiveBufferSizeCore; }
            set { ReceiveBufferSizeCore = value; }
        }

        // Gets or sets the size of the send buffer in bytes.
        public int SendBufferSize
        {
            get { return SendBufferSizeCore; }
            set { SendBufferSizeCore = value; }
        }

        // Gets or sets the receive time out value of the connection in milliseconds.
        public int ReceiveTimeout
        {
            get { return ReceiveTimeoutCore; }
            set { ReceiveTimeoutCore = value; }
        }

        // Gets or sets the send time out value of the connection in milliseconds.
        public int SendTimeout
        {
            get { return SendTimeoutCore; }
            set { SendTimeoutCore = value; }
        }

        // Gets or sets the value of the connection's linger option.
        public LingerOption LingerState
        {
            get { return LingerStateCore; }
            set { LingerStateCore = value; }
        }

        // Enables or disables delay when send or receive buffers are full.
        public bool NoDelay
        {
            get { return NoDelayCore; }
            set { NoDelayCore = value; }
        }

        private Socket CreateSocket()
        {
            return new Socket(_family, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
