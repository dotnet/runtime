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
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Assert.Equal(0, socket.ReceiveTimeout);
            socket.ReceiveTimeout = 100;
            Assert.Equal(100, socket.ReceiveTimeout);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SocketSendTimeout_GetAndSet_Success()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Assert.Equal(0, socket.SendTimeout);
            socket.SendTimeout = 100;
            Assert.Equal(100, socket.SendTimeout);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void ReceiveTimesOut_Throws()
        {
            using (Socket localSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                using (Socket remoteSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    int port = localSocket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                    localSocket.Listen(1);
                    IAsyncResult localAsync = localSocket.BeginAccept(null, null);

                    remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                    Socket acceptedSocket = localSocket.EndAccept(localAsync);
                    acceptedSocket.ReceiveTimeout = TestSettings.FailingTestTimeout;

                    SocketException sockEx = Assert.Throws<SocketException>(() =>
                   {
                       acceptedSocket.Receive(new byte[1]);
                   });

                    Assert.Equal(SocketError.TimedOut, sockEx.SocketErrorCode);
                    Assert.True(acceptedSocket.Connected);
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SocketSendTimeout_Send_Success()
        {
            using (Socket localSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                using (Socket remoteSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    int port = localSocket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                    localSocket.Listen(1);
                    IAsyncResult localAsync = localSocket.BeginAccept(null, null);

                    remoteSocket.Connect(IPAddress.IPv6Loopback, port);

                    Socket acceptedSocket = localSocket.EndAccept(localAsync);
                    acceptedSocket.SendTimeout = TestSettings.PassingTestTimeout;

                    // Note that Send almost never times out because it only has to copy the data to the native buffer.
                    int bytes = acceptedSocket.Send(new byte[100]);
                    Assert.Equal(100, bytes);
                    Assert.True(acceptedSocket.Connected);
                }
            }
        }
    }
}
