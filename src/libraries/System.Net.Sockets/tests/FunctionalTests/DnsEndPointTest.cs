// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class DnsEndPointTest
    {
        // TODO: These constants are fill-ins for issues that need to be opened
        //       once this code is merged into corefx/master.
        private const int DummyLoopbackV6Issue = 123456;

        private const int TestPortBase = 7080;
        private readonly ITestOutputHelper _log;

        public DnsEndPointTest(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        private void OnConnectAsyncCompleted(object sender, SocketAsyncEventArgs args)
        {
            ManualResetEvent complete = (ManualResetEvent)args.UserToken;
            complete.Set();
        }

        [Fact]
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_Success()
        {
            Assert.True(Capability.IPv4Support());

            SocketTestServer server = SocketTestServer.SocketTestServerFactory(
                new IPEndPoint(IPAddress.Loopback, TestPortBase));

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase);
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
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_HostNotFound()
        {
            Assert.True(Capability.IPv4Support());

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("notahostname.invalid.corp.microsoft.com", TestPortBase + 1);
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
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_ConnectionRefused()
        {
            Assert.True(Capability.IPv4Support());

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 2);
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
        [Trait("IPv4", "true")]
        [Trait("IPv6", "true")]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void Socket_StaticConnectAsync_Success()
        {
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());

            SocketTestServer server4 = SocketTestServer.SocketTestServerFactory(
                new IPEndPoint(IPAddress.Loopback, TestPortBase + 3));

            SocketTestServer server6 = SocketTestServer.SocketTestServerFactory(
                new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 4));

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 3);
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

            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 4);
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
            args.RemoteEndPoint = new DnsEndPoint("notahostname.invalid.corp.microsoft.com", TestPortBase + 5);
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
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 6);
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
        [Trait("IPv6", "true")]
        public void Socket_StaticConnectAsync_SyncFailure()
        {
            Assert.True(Capability.IPv6Support());

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("127.0.0.1", TestPortBase + 7, AddressFamily.InterNetworkV6);
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
                "SocketError " + errorCode);
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
