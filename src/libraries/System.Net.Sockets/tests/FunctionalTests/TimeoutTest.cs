// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class TimeoutTest
    {
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void GetAndSet_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal(0, socket.ReceiveTimeout);

                socket.ReceiveTimeout = 100;
                Assert.Equal(100, socket.ReceiveTimeout);

                socket.ReceiveTimeout = -1;
                Assert.InRange(socket.ReceiveTimeout, 0, int.MaxValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SocketSendTimeout_GetAndSet_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal(0, socket.SendTimeout);

                socket.SendTimeout = 100;
                Assert.Equal(100, socket.SendTimeout);

                socket.SendTimeout = -1;
                Assert.InRange(socket.SendTimeout, 0, int.MaxValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReceiveTimesOut_Throws(bool forceNonBlocking)
        {
            using (Socket localSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            using (Socket remoteSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = localSocket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                localSocket.Listen(1);
                IAsyncResult localAsync = localSocket.BeginAccept(null, null);

                if (forceNonBlocking)
                {
                    remoteSocket.ForceNonBlocking();
                }

                remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                Socket acceptedSocket = localSocket.EndAccept(localAsync);
                acceptedSocket.ReceiveTimeout = TestSettings.FailingTestTimeout;

                if (forceNonBlocking)
                {
                    acceptedSocket.ForceNonBlocking();
                }

                SocketException sockEx = Assert.Throws<SocketException>(() =>
                {
                    acceptedSocket.Receive(new byte[1]);
                });

                Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                Assert.True(acceptedSocket.Connected);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SocketSendTimeout_Send_Success(bool forceNonBlocking)
        {
            using (Socket localSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            using (Socket remoteSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = localSocket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                localSocket.Listen(1);
                IAsyncResult localAsync = localSocket.BeginAccept(null, null);

                if (forceNonBlocking)
                {
                    remoteSocket.ForceNonBlocking();
                }

                remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                Socket acceptedSocket = localSocket.EndAccept(localAsync);
                acceptedSocket.SendTimeout = TestSettings.FailingTestTimeout;

                if (forceNonBlocking)
                {
                    acceptedSocket.ForceNonBlocking();
                }

                // Force Send to timeout by filling the kernel buffer.
                var sendBuffer = new byte[16 * 1024];
                SocketException sockEx = Assert.Throws<SocketException>((Action) (() =>
                {
                    while (true)
                    {
                        acceptedSocket.Send(sendBuffer);
                    }
                }));

                Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                Assert.True(acceptedSocket.Connected);
            }
        }
    }
}
