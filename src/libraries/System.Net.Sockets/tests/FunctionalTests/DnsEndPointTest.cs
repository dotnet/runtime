// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class DnsEndPointTest : DualModeBase
    {
        private void OnConnectAsyncCompleted(object sender, SocketAsyncEventArgs args)
        {
            ManualResetEvent complete = (ManualResetEvent)args.UserToken;
            complete.Set();
        }

        [OuterLoop]
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void Socket_ConnectDnsEndPoint_Success(SocketImplementationType type)
        {
            int port;
            using (SocketTestServer server = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port))
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                sock.Connect(new DnsEndPoint("localhost", port));
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void Socket_ConnectDnsEndPoint_SetSocketProperties_Success(SocketImplementationType type)
        {
            int port;
            using (SocketTestServer server = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port))
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                sock.LingerState = new LingerOption(false, 0);
                sock.NoDelay = true;
                sock.ReceiveBufferSize = 1024;
                sock.ReceiveTimeout = 100;
                sock.SendBufferSize = 1024;
                sock.SendTimeout = 100;
                sock.Connect(new DnsEndPoint("localhost", port));
            }
        }

        [OuterLoop]
        [Fact]
        public void Socket_ConnectDnsEndPoint_Failure()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                SocketException ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    sock.Connect(new DnsEndPoint(Configuration.Sockets.InvalidHost, UnusedPort));
                });

                SocketError errorCode = ex.SocketErrorCode;
                Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                    $"SocketErrorCode: {errorCode}");

                ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    sock.Connect(new DnsEndPoint("localhost", UnusedPort));
                });

                Assert.Equal(SocketError.ConnectionRefused, ex.SocketErrorCode);
            }
        }

        [OuterLoop]
        [Fact]
        public void Socket_SendToDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    sock.SendTo(new byte[10], new DnsEndPoint("localhost", UnusedPort));
                });
            }
        }

        [OuterLoop]
        [Fact]
        public void Socket_ReceiveFromDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int port = sock.BindToAnonymousPort(IPAddress.Loopback);
                EndPoint endpoint = new DnsEndPoint("localhost", port);

                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    sock.ReceiveFrom(new byte[10], ref endpoint);
                });
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void Socket_BeginConnectDnsEndPoint_Success(SocketImplementationType type)
        {
            int port;
            using (SocketTestServer server = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port))
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", port), null, null);
                sock.EndConnect(result);
                Assert.True(sock.Connected);
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void Socket_BeginConnectDnsEndPoint_SetSocketProperties_Success(SocketImplementationType type)
        {
            int port;
            using (SocketTestServer server = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port))
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                sock.LingerState = new LingerOption(false, 0);
                sock.NoDelay = true;
                sock.ReceiveBufferSize = 1024;
                sock.ReceiveTimeout = 100;
                sock.SendBufferSize = 1024;
                sock.SendTimeout = 100;
                IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", port), null, null);
                sock.EndConnect(result);
            }
        }

        [OuterLoop]
        [Fact]
        public void Socket_BeginConnectDnsEndPoint_Failure()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                SocketException ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    IAsyncResult result = sock.BeginConnect(new DnsEndPoint(Configuration.Sockets.InvalidHost, UnusedPort), null, null);
                    sock.EndConnect(result);
                });

                SocketError errorCode = ex.SocketErrorCode;
                Assert.True((errorCode == SocketError.HostNotFound) || (errorCode == SocketError.NoData),
                    "SocketErrorCode: {0}" + errorCode);

                ex = Assert.ThrowsAny<SocketException>(() =>
                {
                    IAsyncResult result = sock.BeginConnect(new DnsEndPoint("localhost", UnusedPort), null, null);
                    sock.EndConnect(result);
                });

                Assert.Equal(SocketError.ConnectionRefused, ex.SocketErrorCode);
            }
        }

        [OuterLoop]
        [Fact]
        public void Socket_BeginSendToDnsEndPoint_ArgumentException()
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    sock.BeginSendTo(new byte[10], 0, 0, SocketFlags.None, new DnsEndPoint("localhost", UnusedPort), null, null);
                });
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_Success(SocketImplementationType type)
        {
            Assert.True(Capability.IPv4Support());

            int port;
            using (SocketTestServer server = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port))
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.Completed += OnConnectAsyncCompleted;

                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                bool willRaiseEvent = sock.ConnectAsync(args);
                if (willRaiseEvent)
                {
                    Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                    complete.Dispose(); // only dispose on success as we know we're done with the instance
                }

                Assert.Equal(SocketError.Success, args.SocketError);
                Assert.Null(args.ConnectByNameError);
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_SetSocketProperties_Success(SocketImplementationType type)
        {
            Assert.True(Capability.IPv4Support());

            int port;
            using (SocketTestServer server = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port))
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                sock.LingerState = new LingerOption(false, 0);
                sock.NoDelay = true;
                sock.ReceiveBufferSize = 1024;
                sock.ReceiveTimeout = 100;
                sock.SendBufferSize = 1024;
                sock.SendTimeout = 100;

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.Completed += OnConnectAsyncCompleted;

                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                bool willRaiseEvent = sock.ConnectAsync(args);
                if (willRaiseEvent)
                {
                    Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                    complete.Dispose(); // only dispose on success as we know we're done with the instance
                }

                Assert.Equal(SocketError.Success, args.SocketError);
                Assert.Null(args.ConnectByNameError);
            }
        }

        [OuterLoop]
        [Fact]
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_HostNotFound()
        {
            Assert.True(Capability.IPv4Support());

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint(Configuration.Sockets.InvalidHost, UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                bool willRaiseEvent = sock.ConnectAsync(args);
                if (willRaiseEvent)
                {
                    Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                    complete.Dispose(); // only dispose on success as we know we're done with the instance
                }

                AssertHostNotFoundOrNoData(args);
            }
        }

        [OuterLoop]
        [Fact]
        [Trait("IPv4", "true")]
        public void Socket_ConnectAsyncDnsEndPoint_ConnectionRefused()
        {
            Assert.True(Capability.IPv4Support());

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                bool willRaiseEvent = sock.ConnectAsync(args);
                if (willRaiseEvent)
                {
                    Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                    complete.Dispose(); // only dispose on success as we know we're done with the instance
                }

                Assert.Equal(SocketError.ConnectionRefused, args.SocketError);
                Assert.True(args.ConnectByNameError is SocketException);
                Assert.Equal(SocketError.ConnectionRefused, ((SocketException)args.ConnectByNameError).SocketErrorCode);
            }
        }

        [OuterLoop]
        [ConditionalTheory(nameof(LocalhostIsBothIPv4AndIPv6))]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        [Trait("IPv4", "true")]
        [Trait("IPv6", "true")]
        public void Socket_StaticConnectAsync_Success(SocketImplementationType type)
        {
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());

            int port4, port6;
            using (SocketTestServer server4 = SocketTestServer.SocketTestServerFactory(type, IPAddress.Loopback, out port4))
            using (SocketTestServer server6 = SocketTestServer.SocketTestServerFactory(type, IPAddress.IPv6Loopback, out port6))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port4);
                args.Completed += OnConnectAsyncCompleted;

                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args))
                {
                    Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                }

                Assert.Equal(SocketError.Success, args.SocketError);
                Assert.Null(args.ConnectByNameError);
                Assert.NotNull(args.ConnectSocket);
                Assert.True(args.ConnectSocket.AddressFamily == AddressFamily.InterNetwork);
                Assert.True(args.ConnectSocket.Connected);

                args.ConnectSocket.Dispose();

                args.RemoteEndPoint = new DnsEndPoint("localhost", port6);
                complete.Reset();

                if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args))
                {
                    Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                }

                complete.Dispose(); // only dispose on success as we know we're done with the instance

                Assert.Equal(SocketError.Success, args.SocketError);
                Assert.Null(args.ConnectByNameError);
                Assert.NotNull(args.ConnectSocket);
                Assert.True(args.ConnectSocket.AddressFamily == AddressFamily.InterNetworkV6);
                Assert.True(args.ConnectSocket.Connected);

                args.ConnectSocket.Dispose();
            }
        }

        [Fact]
        public void Socket_StaticConnectAsync_IPv6MappedIPv4_Success()
        {
            using SocketTestServer server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out int port);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("[::FFFF:127.0.0.1]", port);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args))
            {
                Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
            }

            Assert.Equal(SocketError.Success, args.SocketError);
            Assert.Null(args.ConnectByNameError);
            Assert.NotNull(args.ConnectSocket);
            Assert.True(args.ConnectSocket.Connected);
            args.ConnectSocket.Dispose();
        }

        [OuterLoop]
        [Fact]
        public void Socket_StaticConnectAsync_HostNotFound()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint(Configuration.Sockets.InvalidHost, UnusedPort);
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            bool willRaiseEvent = Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args);
            if (!willRaiseEvent)
            {
                OnConnectAsyncCompleted(null, args);
            }

            Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
            complete.Dispose(); // only dispose on success as we know we're done with the instance

            AssertHostNotFoundOrNoData(args);

            Assert.Null(args.ConnectSocket);

            complete.Dispose();
        }

        [OuterLoop]
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

            Assert.True(complete.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
            complete.Dispose(); // only dispose on success as we know we're done with the instance

            Assert.Equal(SocketError.ConnectionRefused, args.SocketError);
            Assert.True(args.ConnectByNameError is SocketException);
            Assert.Equal(SocketError.ConnectionRefused, ((SocketException)args.ConnectByNameError).SocketErrorCode);
            Assert.Null(args.ConnectSocket);

            complete.Dispose();
        }

        private static void CallbackThatShouldNotBeCalled(object sender, SocketAsyncEventArgs args)
        {
            throw new ShouldNotBeInvokedException();
        }

        [OuterLoop]
        [Fact]
        [Trait("IPv6", "true")]
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
    }
}
