// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class EnableBroadcastTest
    {
        [Fact]
        public void TcpConstructor_EnableBroadcast_GetterReturnsFalse()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.False(socket.EnableBroadcast);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TcpConstructor_EnableBroadcast_SetterThrows()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() =>
                {
                    socket.EnableBroadcast = true;
                });
            }
        }

        [Fact]
        public void UdpConstructor_EnableBroadcast_Configurable()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.False(socket.EnableBroadcast);

                socket.EnableBroadcast = true;
                Assert.True(socket.EnableBroadcast);

                socket.EnableBroadcast = false;
                Assert.False(socket.EnableBroadcast);
            }
        }
    }
}
