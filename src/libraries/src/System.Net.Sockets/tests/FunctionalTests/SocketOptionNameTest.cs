// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Net.Test.Common;
using System.Runtime.InteropServices;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketOptionNameTest
    {
        private static bool SocketsReuseUnicastPortSupport
        {
            get
            {
                return Capability.SocketsReuseUnicastPortSupport();
            }
        }

        private static bool NoSocketsReuseUnicastPortSupport
        {
            get
            {
                return !Capability.SocketsReuseUnicastPortSupport();
            }
        }

        [ConditionalFact(nameof(NoSocketsReuseUnicastPortSupport))]
        public void ReuseUnicastPort_CreateSocketGetOption_NoSocketsReuseUnicastPortSupport_Throws()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Assert.Throws<SocketException>(() =>
                socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort));
        }

        [ConditionalFact(nameof(SocketsReuseUnicastPortSupport))]
        public void ReuseUnicastPort_CreateSocketGetOption_SocketsReuseUnicastPortSupport_OptionIsZero()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            var optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);
            Assert.Equal(0, optionValue);
        }

        [ConditionalFact(nameof(NoSocketsReuseUnicastPortSupport))]
        public void ReuseUnicastPort_CreateSocketSetOption_NoSocketsReuseUnicastPortSupport_Throws()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Assert.Throws<SocketException>(() =>
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1));
        }

        [ConditionalFact(nameof(SocketsReuseUnicastPortSupport))]
        public void ReuseUnicastPort_CreateSocketSetOptionToZeroAndGetOption_SocketsReuseUnicastPortSupport_OptionIsZero()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 0);
            int optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);
            Assert.Equal(0, optionValue);
        }

        // TODO: Issue #4887
        // The socket option 'ReuseUnicastPost' only works on Windows 10 systems. In addition, setting the option
        // is a no-op unless specialized network settings using PowerShell configuration are first applied to the
        // machine. This is currently difficult to test in the CI environment. So, this ests will be disabled for now
        [ActiveIssue(4887)]
        public void ReuseUnicastPort_CreateSocketSetOptionToOneAndGetOption_SocketsReuseUnicastPortSupport_OptionIsOne()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1);
            int optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);
            Assert.Equal(1, optionValue);
        }

        [Fact]
        public void MulticastOption_CreateSocketSetGetOption_GroupAndInterfaceIndex_SetSucceeds_GetThrows()
        {
            int interfaceIndex = 0;
            IPAddress groupIp = IPAddress.Parse("239.1.2.3");

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(groupIp, interfaceIndex));

                Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership));
            }
        }

        [Theory]
        [InlineData(0)] // Any
        [InlineData(1)] // Loopback
        [PlatformSpecific(~PlatformID.OSX)] // Receive times out on loopback interface
        public void MulticastInterface_Set_ValidIndex_Succeeds(int interfaceIndex)
        {
            IPAddress multicastAddress = IPAddress.Parse("239.1.2.3");
            string message = "hello";
            int port;

            using (Socket receiveSocket = CreateBoundUdpSocket(out port),
                          sendSocket    = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiveSocket.ReceiveTimeout = 1000;
                receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, interfaceIndex));

                sendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(interfaceIndex));
                sendSocket.SendTo(Encoding.UTF8.GetBytes(message), new IPEndPoint(multicastAddress, port));

                var receiveBuffer = new byte[1024];
                int bytesReceived = receiveSocket.Receive(receiveBuffer);
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesReceived);

                Assert.Equal(receivedMessage, message);
            }
        }

        [Fact]
        public void MulticastInterface_Set_InvalidIndex_Throws()
        {
            int interfaceIndex = 31415;
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Throws<SocketException>(() =>
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(interfaceIndex)));
            }
        }

        // Create an Udp Socket and binds it to an available port
        private static Socket CreateBoundUdpSocket(out int localPort)
        {
            Socket receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // sending a message will bind the socket to an available port
            string sendMessage = "dummy message";
            int port = 54320;
            IPAddress multicastAddress = IPAddress.Parse("239.1.1.1");
            receiveSocket.SendTo(Encoding.UTF8.GetBytes(sendMessage), new IPEndPoint(multicastAddress, port));

            localPort = (receiveSocket.LocalEndPoint as IPEndPoint).Port;
            return receiveSocket;
        }
    }
}
