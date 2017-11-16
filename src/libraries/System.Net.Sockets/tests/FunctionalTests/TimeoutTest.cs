// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

        // Use a Timeout large enough so that we can effectively detect when it's not accurate,
        // but also not so large that it takes too long to run.
        const int Timeout = 2000;

        [ActiveIssue(23767, TestPlatforms.AnyUnix)]
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

                remoteSocket.ForceNonBlocking(forceNonBlocking);

                remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                Socket acceptedSocket = localSocket.EndAccept(localAsync);
                acceptedSocket.ReceiveTimeout = Timeout;

                acceptedSocket.ForceNonBlocking(forceNonBlocking);

                DateTime start = default(DateTime);

                SocketException sockEx = Assert.Throws<SocketException>(() =>
                {
                    start = DateTime.UtcNow;
                    acceptedSocket.Receive(new byte[1]);
                });

                double elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

                Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                Assert.True(acceptedSocket.Connected);

                // Try to ensure that the elapsed timeout is reasonably correct
                // Sometimes test machines run slowly
                Assert.InRange(elapsed, Timeout * 0.75, Timeout * 2);
            }
        }

        [OuterLoop] // TODO: Issue #11345
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

                var sw = new Stopwatch();

                // Force Send to timeout by filling the kernel buffer.
                var sendBuffer = new byte[16 * 1024];
                SocketException sockEx = Assert.Throws<SocketException>((Action) (() =>
                {
                    while (true)
                    {
                        sw.Restart();
                        acceptedSocket.Send(sendBuffer);
                    }
                }));

                double elapsed = sw.Elapsed.TotalMilliseconds;

                Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                Assert.True(acceptedSocket.Connected);

                // Try to ensure that the elapsed timeout is reasonably correct
                // Sometimes test machines run slowly
                Assert.InRange(elapsed, Timeout * 0.5, Timeout * 3);
            }
        }
    }
}
