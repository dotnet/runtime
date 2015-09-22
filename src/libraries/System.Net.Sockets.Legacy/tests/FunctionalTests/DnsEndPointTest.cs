// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class DnsEndPointTest
    {
        // TODO: These constants are fill-ins for issues that need to be opened
        //       once this code is merged into corefx/master.
        private const int DummyLoopbackV6Issue = 123456;

        private const int TestPortBase = TestPortBases.DnsEndPoint;

        [Fact]
        public void Socket_ConnectDnsEndPoint_Success()
        {
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.Loopback, TestPortBase));

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new DnsEndPoint("localhost", TestPortBase));

            sock.Dispose();
            server.Dispose();
        }

        [Fact]
        public void Socket_ConnectDnsEndPoint_Failure()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                SocketException ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    sock.Connect(new DnsEndPoint("notahostname.invalid.corp.microsoft.com", TestPortBase + 1));
                });

                SocketError errorCode = ex.SocketErrorCode;
                Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                    "SocketErrorCode: {0}" + errorCode);

                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    sock.Connect(new DnsEndPoint("localhost", TestPortBase + 2));
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
                    sock.SendTo(new byte[10], new DnsEndPoint("localhost", TestPortBase + 3));
                });
            }
        }

        [Fact]
        public void Socket_ReceiveFromDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                sock.Bind(new IPEndPoint(IPAddress.Loopback, TestPortBase + 4));
                EndPoint endpoint = new DnsEndPoint("localhost", TestPortBase + 4);

                Assert.Throws<ArgumentException>(() =>
                {
                    sock.ReceiveFrom(new byte[10], ref endpoint);
                });
            }
        }

        [Fact]
        public void Socket_BeginConnectDnsEndPoint_Success()
        {
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.Loopback, TestPortBase + 5));

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", TestPortBase + 5), null, null);
            sock.EndConnect(result);

            sock.Dispose();
            server.Dispose();
        }

        [Fact]
        public void Socket_BeginConnectDnsEndPoint_Failure()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                SocketException ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    IAsyncResult result = sock.BeginConnect(new DnsEndPoint("notahostname.invalid.corp.microsoft.com", TestPortBase + 6), null, null);
                    sock.EndConnect(result);
                });

                SocketError errorCode = ex.SocketErrorCode;
                Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                    "SocketErrorCode: {0}" + errorCode);

                // TODO: Behavior difference from .Net Desktop. This will actually throw InternalSocketException.
                ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", TestPortBase + 6), null, null);
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
                    sock.BeginSendTo(new byte[10], 0, 0, SocketFlags.None, new DnsEndPoint("localhost", TestPortBase + 7), null, null);
                });
            }
        }

        private void OnConnectAsyncCompleted(object sender, SocketAsyncEventArgs args)
        {
            ManualResetEvent complete = (ManualResetEvent)args.UserToken;
            complete.Set();
        }

        [Fact]
        public void Socket_ConnectAsyncDnsEndPoint_Success()
        {
            SocketTestServer server = SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.Loopback, TestPortBase + 8));

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 8);
            args.Completed += OnConnectAsyncCompleted;
            
            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.True(sock.ConnectAsync(args));

            complete.WaitOne();

            Assert.Equal(SocketError.Success, args.SocketError);
            Assert.Null(args.ConnectByNameError);

            complete.Dispose();
            sock.Dispose();
            server.Dispose();
        }

        [Fact]
        public void Socket_ConnectAsyncDnsEndPoint_HostNotFound()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("notahostname.invalid.corp.microsoft.com", TestPortBase + 9);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.True(sock.ConnectAsync(args));

            complete.WaitOne();

            AssertHostNotFoundOrNoData(args);

            complete.Dispose();
            sock.Dispose();
        }

        [Fact]
        public void Socket_ConnectAsyncDnsEndPoint_ConnectionRefused()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 10);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.True(sock.ConnectAsync(args));

            complete.WaitOne();

            Assert.Equal(SocketError.ConnectionRefused, args.SocketError);
            Assert.True(args.ConnectByNameError is SocketException);
            Assert.Equal(SocketError.ConnectionRefused, ((SocketException)args.ConnectByNameError).SocketErrorCode);

            complete.Dispose();
            sock.Dispose();
        }

        [Fact]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void Socket_StaticConnectAsync_Success()
        {

            Assert.True(Capability.IPv6Support() && Capability.IPv4Support());

            SocketTestServer server4 = SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.Loopback, TestPortBase + 11));
            SocketTestServer server6 = SocketTestServer.SocketTestServerFactory(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 12));

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 11);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            complete.WaitOne();

            Assert.Equal(SocketError.Success, args.SocketError);
            Assert.Null(args.ConnectByNameError);
            Assert.NotNull(args.ConnectSocket);
            Assert.True(args.ConnectSocket.AddressFamily == AddressFamily.InterNetwork);
            Assert.True(args.ConnectSocket.Connected);

            args.ConnectSocket.Dispose();

            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 12);
            complete.Reset();

            Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            complete.WaitOne();

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
            args.RemoteEndPoint = new DnsEndPoint("notahostname.invalid.corp.microsoft.com", TestPortBase + 13);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            complete.WaitOne();

            AssertHostNotFoundOrNoData(args);

            Assert.Null(args.ConnectSocket);

            complete.Dispose();
        }

        [Fact]
        public void Socket_StaticConnectAsync_ConnectionRefused()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 14);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args));

            complete.WaitOne();

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
            args.RemoteEndPoint = new DnsEndPoint("127.0.0.1", TestPortBase + 15, AddressFamily.InterNetworkV6);
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
