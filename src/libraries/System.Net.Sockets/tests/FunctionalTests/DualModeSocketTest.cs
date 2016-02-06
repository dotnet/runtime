// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualMode
    {
        // Ports 8 and 8887 are unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        private const int UnusedPort = 8;
        private const int UnusedBindablePort = 8887;

        private readonly ITestOutputHelper _log;

        public DualMode(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());
        }

        #region Constructor and Property

        [Fact]
        public void DualModeConstructor_InterNetworkV6Default()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
        }

        [Fact]
        public void DualModeUdpConstructor_DualModeConfgiured()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
        }

        #endregion Constructor and Property

        #region ConnectAsync

        #region ConnectAsync to IPEndPoint

        [Fact] // Base case
        public void Socket_ConnectAsyncV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            Assert.Throws<NotSupportedException>(() =>
            {
                socket.ConnectAsync(args);
            });
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
           {
               DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
           });
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
          {
              DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
          });
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.UserToken = waitHandle;

                socket.ConnectAsync(args);

                Assert.True(waitHandle.WaitOne(5000), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        #endregion ConnectAsync to IPAddress

        #region ConnectAsync to DnsEndPoint

        [Theory]
        [MemberData("DualMode_Connect_IPAddress_DualMode_Data")]
        [PlatformSpecific(PlatformID.Windows)]
        public void DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            int port;
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.UserToken = waitHandle;

                socket.ConnectAsync(args);

                waitHandle.WaitOne();
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        #endregion ConnectAsync to DnsEndPoint

        #endregion ConnectAsync

        #region Accept

        #region Bind

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BindV4IPEndPoint_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, UnusedBindablePort));
                }
            });
        }

        [Fact] // Base Case; BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindMappedV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback.MapToIPv6());
            }
        }

        [Fact] // BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback);
            }
        }

        [Fact]
        public void BindV6IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
            }
        }

        [Fact]
        public void Socket_BindDnsEndPoint_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new DnsEndPoint("localhost", UnusedBindablePort));
                }
            });
        }

        #endregion Bind

        #region Accept Async/Event

        [Fact]
        public void AcceptAsyncV4BoundToSpecificV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToSpecificV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void DualModeConnect_AcceptAsync_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += AsyncCompleted;
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                args.UserToken = waitHandle;
                args.SocketError = SocketError.SocketError;

                _log.WriteLine(args.GetHashCode() + " SocketAsyncEventArgs with manual event " + waitHandle.GetHashCode());
                if (!serverSocket.AcceptAsync(args))
                {
                    throw new SocketException((int)args.SocketError);
                }

                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);

                var waitHandles = new WaitHandle[2];
                waitHandles[0] = waitHandle;
                waitHandles[1] = client.WaitHandle;

                int completedHandle = WaitHandle.WaitAny(waitHandles, 5000);

                if (completedHandle == WaitHandle.WaitTimeout)
                {
                    throw new TimeoutException("Timed out while waiting for either of client and server connections...");
                }

                if (completedHandle == 1)   // Client finished
                {
                    if (client.Error != SocketError.Success)
                    {
                        // Client SocketException
                        throw new SocketException((int)client.Error);
                    }

                    if (!waitHandle.WaitOne(5000))  // Now wait for the server.
                    {
                        throw new TimeoutException("Timed out while waiting for the server accept...");
                    }
                }

                _log.WriteLine(args.SocketError.ToString());


                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Socket clientSocket = args.AcceptSocket;
                Assert.NotNull(clientSocket);
                Assert.True(clientSocket.Connected);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                clientSocket.Dispose();
            }
        }

        #endregion Accept Async/Event

        #endregion Accept

        #region Connectionless

        #region SendTo Async/Event

        [Fact] // Base case
        public void Socket_SendToAsyncV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);
            bool async = socket.SendToAsync(args);
            Assert.False(async);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(SocketError.Fault, args.SocketError);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // NOTE: on Linux, this API returns ENETUNREACH instead of EFAULT: this platform
                //       checks the family of the provided socket address before checking its size
                //       (as long as the socket address is large enough to store an address family).
                Assert.Equal(SocketError.NetworkUnreachable, args.SocketError);
            }
            else
            {
                // NOTE: on other Unix platforms, this API returns EINVAL instead of EFAULT.
                Assert.Equal(SocketError.InvalidArgument, args.SocketError);
            }
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToAsyncDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);
            Assert.Throws<ArgumentException>(() =>
            {
                socket.SendToAsync(args);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);
            });
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            int port;
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out port))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.UserToken = waitHandle;
                args.Completed += AsyncCompleted;

                bool async = client.SendToAsync(args);
                if (async)
                {
                    Assert.True(waitHandle.WaitOne(5000), "Timeout while waiting for connection");
                }
                Assert.Equal(1, args.BytesTransferred);
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                bool success = server.WaitHandle.WaitOne(Configuration.FailingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Async/Event

        #region ReceiveFrom Async/Event

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            args.SetBuffer(new byte[1], 0, 1);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.ReceiveFromAsync(args);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.ReceiveFromAsync(args);
                });
            }
        }

        [Fact]
        public void ReceiveFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [ActiveIssue(4004, PlatformID.OSX)]
        public void ReceiveFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void ReceiveFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void ReceiveFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveFromAsync_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(listenOn, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.UserToken = waitHandle;
                args.Completed += AsyncCompleted;

                bool async = serverSocket.ReceiveFromAsync(args);
                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                if (async && !waitHandle.WaitOne(200))
                {
                    throw new TimeoutException();
                }

                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Assert.Equal(1, args.BytesTransferred);
                Assert.Equal<Type>(args.RemoteEndPoint.GetType(), typeof(IPEndPoint));
                IPEndPoint remoteEndPoint = args.RemoteEndPoint as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Async/Event

        #endregion Connectionless

        #region GC Finalizer test
        // This test assumes sequential execution of tests and that it is going to be executed after other tests
        // that used Sockets. 
        [Fact]
        public void TestFinalizers()
        {
            // Making several passes through the FReachable list.
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        #endregion

        #region Helpers

        public static readonly object[][] DualMode_Connect_IPAddress_DualMode_Data = {
            new object[] { IPAddress.Loopback, false },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.IPv6Any, true },
        };

        private class SocketServer : IDisposable
        {
            private readonly ITestOutputHelper _output;
            private Socket _server;
            private Socket _acceptedSocket;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    _server = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
                else
                {
                    _server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }

                port = _server.BindToAnonymousPort(address);
                _server.Listen(1);

                IPAddress remoteAddress = address.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                EndPoint remote = new IPEndPoint(remoteAddress, 0);
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remote;
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Accepted);
                e.UserToken = _waitHandle;

                _server.AcceptAsync(e);
            }

            private void Accepted(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Accepted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                _acceptedSocket = e.AcceptSocket;

                handle.Set();
            }

            public void Dispose()
            {
                try
                {
                    _server.Dispose();
                    if (_acceptedSocket != null)
                        _acceptedSocket.Dispose();
                }
                catch (Exception) { }
            }
        }

        private class SocketClient
        {
            private IPAddress _connectTo;
            private Socket _serverSocket;
            private int _port;
            private readonly ITestOutputHelper _output;

            private EventWaitHandle _waitHandle = new AutoResetEvent(false);
            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketError Error
            {
                get;
                private set;
            }

            public SocketClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port)
            {
                _output = output;
                _connectTo = connectTo;
                _serverSocket = serverSocket;
                _port = port;
                Error = SocketError.Success;

                Task.Run(() => ConnectClient(null));
            }

            private void ConnectClient(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(Connected);
                    e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                    e.UserToken = _waitHandle;

                    socket.ConnectAsync(e);
                }
                catch (SocketException ex)
                {
                    Error = ex.SocketErrorCode;
                    _serverSocket.Dispose(); // Cancels the test
                }
            }
            private void Connected(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Connected: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                Error = e.SocketError;
                handle.Set();
            }
        }

        private class SocketUdpServer : IDisposable
        {
            private readonly ITestOutputHelper _output;
            private Socket _server;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketUdpServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    _server = new Socket(SocketType.Dgram, ProtocolType.Udp);
                }
                else
                {
                    _server = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                }

                port = _server.BindToAnonymousPort(address);

                IPAddress remoteAddress = address.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                EndPoint remote = new IPEndPoint(remoteAddress, 0);
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remote;
                e.SetBuffer(new byte[1], 0, 1);
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                e.UserToken = _waitHandle;

                _server.ReceiveFromAsync(e);
            }

            private void Received(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Received: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                handle.Set();
            }

            public void Dispose()
            {
                try
                {
                    _server.Dispose();
                }
                catch (Exception) { }
            }
        }

        private class SocketUdpClient
        {
            private readonly ITestOutputHelper _output;

            private int _port;
            private IPAddress _connectTo;
            private Socket _serverSocket;

            public SocketUdpClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port)
            {
                _output = output;

                _connectTo = connectTo;
                _port = port;
                _serverSocket = serverSocket;

                Task.Run(() => ClientSend(null));
            }

            private void ClientSend(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                    e.SetBuffer(new byte[1], 0, 1);

                    socket.SendToAsync(e);
                }
                catch (SocketException)
                {
                    _serverSocket.Dispose(); // Cancels the test
                }
            }
        }

        private void AsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            EventWaitHandle handle = (EventWaitHandle)e.UserToken;

            _log.WriteLine(
                "AsyncCompleted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                handle.GetHashCode() + " error: " + e.SocketError);

            handle.Set();
        }

        #endregion Helpers
    }
}
