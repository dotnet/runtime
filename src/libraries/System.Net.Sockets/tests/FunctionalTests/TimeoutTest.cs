// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Net.Sockets.Tests
{
    [Collection(nameof(DisableParallelization))]
    public class TimeoutTest
    {
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

        // Use a Timeout large enough so that we can effectively detect when it's not accurate,
        // but also not so large that it takes too long to run.
        private const int Timeout = 2000;

        [OuterLoop]
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

                remoteSocket.ForceNonBlocking(forceNonBlocking);

                remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                Socket acceptedSocket = localSocket.EndAccept(localAsync);
                acceptedSocket.ReceiveTimeout = Timeout;

                acceptedSocket.ForceNonBlocking(forceNonBlocking);

                long start = Environment.TickCount64;

                SocketException sockEx = Assert.Throws<SocketException>(() =>
                {
                    acceptedSocket.Receive(new byte[1]);
                });

                long elapsed = Environment.TickCount64 - start;
                Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                Assert.True(acceptedSocket.Connected);

                // Try to ensure that the elapsed timeout is reasonably correct
                // Sometimes test machines run slowly
                Assert.InRange(elapsed, Timeout * 0.75, Timeout * 1.9999);
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SendTimesOut_Throws(bool forceNonBlocking)
        {
            using (Socket localSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            using (Socket remoteSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = localSocket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                localSocket.Listen(1);
                IAsyncResult localAsync = localSocket.BeginAccept(null, null);

                remoteSocket.ForceNonBlocking(forceNonBlocking);

                remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                Socket acceptedSocket = localSocket.EndAccept(localAsync);
                acceptedSocket.SendTimeout = Timeout;

                acceptedSocket.ForceNonBlocking(forceNonBlocking);

                long start = Environment.TickCount64;

                // Force Send to timeout by filling the kernel buffer.
                var sendBuffer = new byte[16 * 1024];
                SocketException sockEx = Assert.Throws<SocketException>((Action) (() =>
                {
                    while (true)
                    {
                        start = Environment.TickCount64;
                        acceptedSocket.Send(sendBuffer);
                    }
                }));

                long elapsed = Environment.TickCount64 - start;
                Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                Assert.True(acceptedSocket.Connected);

                // Try to ensure that the elapsed timeout is reasonably correct
                // Sometimes test machines run slowly
                Assert.InRange(elapsed, Timeout * 0.75, Timeout * 1.9999);
            }
        }
    }
}
