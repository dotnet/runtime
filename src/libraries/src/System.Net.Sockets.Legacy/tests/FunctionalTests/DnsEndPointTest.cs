// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class DnsEndPointTest
    {
        // Port 8 is unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        private const int UnusedPort = 8;

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_ConnectDnsEndPoint_Success()
        {
            int port;
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port);

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new DnsEndPoint("localhost", port));

            sock.Dispose();
            server.Dispose();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_ConnectDnsEndPoint_Failure()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                SocketException ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    sock.Connect(new DnsEndPoint("notahostname.invalid.corp.microsoft.com", UnusedPort));
                });

                SocketError errorCode = ex.SocketErrorCode;
                Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                    "SocketErrorCode: {0}" + errorCode);

                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    sock.Connect(new DnsEndPoint("localhost", UnusedPort));
                });

                Assert.Equal(SocketError.ConnectionRefused, ex.SocketErrorCode);
            }
        }

        [Fact]
        public void Socket_SendToDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    sock.SendTo(new byte[10], new DnsEndPoint("localhost", UnusedPort));
                });
            }
        }

        [Fact]
        public void Socket_ReceiveFromDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int port = sock.BindToAnonymousPort(IPAddress.Loopback);
                EndPoint endpoint = new DnsEndPoint("localhost", port);

                Assert.Throws<ArgumentException>(() =>
                {
                    sock.ReceiveFrom(new byte[10], ref endpoint);
                });
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_BeginConnectDnsEndPoint_Success()
        {
            int port;
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port);

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", port), null, null);
            sock.EndConnect(result);

            sock.Dispose();
            server.Dispose();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_BeginConnectDnsEndPoint_Failure()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                SocketException ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    IAsyncResult result = sock.BeginConnect(new DnsEndPoint("notahostname.invalid.corp.microsoft.com", UnusedPort), null, null);
                    sock.EndConnect(result);
                });

                SocketError errorCode = ex.SocketErrorCode;
                Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                    "SocketErrorCode: {0}" + errorCode);

                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", UnusedPort), null, null);
                    sock.EndConnect(result);
                });

                Assert.Equal(SocketError.ConnectionRefused, ex.SocketErrorCode);
            }
        }

        [Fact]
        public void Socket_BeginSendToDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    sock.BeginSendTo(new byte[10], 0, 0, SocketFlags.None, new DnsEndPoint("localhost", UnusedPort), null, null);
                });
            }
        }

        private void OnConnectAsyncCompleted(object sender, SocketAsyncEventArgs args)
        {
            ManualResetEvent complete = (ManualResetEvent)args.UserToken;
            complete.Set();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_ConnectAsyncDnsEndPoint_Success()
        {
            int port;
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", port);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            bool willRaiseEvent = sock.ConnectAsync(args);
            if (willRaiseEvent)
            {
                Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
            }

            Assert.Equal(SocketError.Success, args.SocketError);
            Assert.Null(args.ConnectByNameError);

            complete.Dispose();
            sock.Dispose();
            server.Dispose();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_ConnectAsyncDnsEndPoint_HostNotFound()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("notahostname.invalid.corp.microsoft.com", UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            bool willRaiseEvent = sock.ConnectAsync(args);
            if (willRaiseEvent)
            {
                Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
            }

            AssertHostNotFoundOrNoData(args);

            complete.Dispose();
            sock.Dispose();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_ConnectAsyncDnsEndPoint_ConnectionRefused()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            bool willRaiseEvent = sock.ConnectAsync(args);
            if (willRaiseEvent)
            {
                Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");
            }

            Assert.Equal(SocketError.ConnectionRefused, args.SocketError);
            Assert.True(args.ConnectByNameError is SocketException);
            Assert.Equal(SocketError.ConnectionRefused, ((SocketException)args.ConnectByNameError).SocketErrorCode);

            complete.Dispose();
            sock.Dispose();
        }

        [Fact]
        public void Socket_StaticConnectAsync_Success()
        {
            Assert.True(Capability.IPv6Support() && Capability.IPv4Support());

            int port4, port6;
            SocketTestServer server4 = SocketTestServer.SocketTestServerFactory(IPAddress.Loopback, out port4);
            SocketTestServer server6 = SocketTestServer.SocketTestServerFactory(IPAddress.IPv6Loopback, out port6);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("127.0.0.1", port4);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");

            Assert.Equal(SocketError.Success, args.SocketError);
            Assert.Null(args.ConnectByNameError);
            Assert.NotNull(args.ConnectSocket);
            Assert.True(args.ConnectSocket.AddressFamily == AddressFamily.InterNetwork);
            Assert.True(args.ConnectSocket.Connected);

            args.ConnectSocket.Dispose();

            args.RemoteEndPoint = new DnsEndPoint("::1", port6);
            complete.Reset();

            Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");

            Assert.Equal(SocketError.Success, args.SocketError);
            Assert.Null(args.ConnectByNameError);
            Assert.NotNull(args.ConnectSocket);
            Assert.True(args.ConnectSocket.AddressFamily == AddressFamily.InterNetworkV6);
            Assert.True(args.ConnectSocket.Connected);

            args.ConnectSocket.Dispose();

            server4.Dispose();
            server6.Dispose();
        }

        [Fact]
        public void Socket_StaticConnectAsync_HostNotFound()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("notahostname.invalid.corp.microsoft.com", UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            bool willRaiseEvent = Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args);
            if (!willRaiseEvent)
            {
                OnConnectAsyncCompleted(null, args);
            }

            Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");

            AssertHostNotFoundOrNoData(args);

            Assert.Null(args.ConnectSocket);

            complete.Dispose();
        }

        [Fact]
        public void Socket_StaticConnectAsync_ConnectionRefused()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            bool willRaiseEvent = Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args);
            if (!willRaiseEvent)
            {
                OnConnectAsyncCompleted(null, args);
            }

            Assert.True(complete.WaitOne(Configuration.PassingTestTimeout), "Timed out while waiting for connection");

            Assert.Equal(SocketError.ConnectionRefused, args.SocketError);
            Assert.True(args.ConnectByNameError is SocketException);
            Assert.Equal(SocketError.ConnectionRefused, ((SocketException)args.ConnectByNameError).SocketErrorCode);
            Assert.Null(args.ConnectSocket);

            complete.Dispose();
        }

        public void CallbackThatShouldNotBeCalled(object sender, SocketAsyncEventArgs args)
        {
            Assert.True(false, "This Callback should not be called");
        }

        [Fact]
        public void Socket_StaticConnectAsync_SyncFailure()
        {
            Assert.True(Capability.IPv6Support()); // IPv6 required because we use AF.InterNetworkV6

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("127.0.0.1", UnusedPort, AddressFamily.InterNetworkV6);
            args.Completed += CallbackThatShouldNotBeCalled;

            Assert.False(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            Assert.Equal(SocketError.NoData, args.SocketError);
            Assert.Null(args.ConnectSocket);
        }

        private static void AssertHostNotFoundOrNoData(SocketAsyncEventArgs args)
        {
            SocketError errorCode = args.SocketError;
            Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                "SocketError: " + errorCode);

            Assert.True(args.ConnectByNameError is SocketException);
            errorCode = ((SocketException)args.ConnectByNameError).SocketErrorCode;
            Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                "SocketError: " + errorCode);
        }

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
    }
}
