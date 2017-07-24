// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ReceiveMessageFrom
    {
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Success(bool forceNonBlocking)
        {
            if (Socket.OSSupportsIPv4)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

                    receiver.ForceNonBlocking(forceNonBlocking);

                    Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    sender.ForceNonBlocking(forceNonBlocking);

                    for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                    {
                        sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.Loopback, port));
                    }

                    IPPacketInformation packetInformation;
                    SocketFlags flags = SocketFlags.None;
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                    int len = receiver.ReceiveMessageFrom(new byte[1024], 0, 1024, ref flags, ref remoteEP, out packetInformation);

                    Assert.Equal(1024, len);
                    Assert.Equal(sender.LocalEndPoint, remoteEP);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);

                    sender.Dispose();
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Success_IPv6(bool forceNonBlocking)
        {
            if (Socket.OSSupportsIPv6)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.IPv6Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);

                    receiver.ForceNonBlocking(forceNonBlocking);

                    Socket sender = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));

                    sender.ForceNonBlocking(forceNonBlocking);

                    for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                    {
                        sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.IPv6Loopback, port));
                    }

                    IPPacketInformation packetInformation;
                    SocketFlags flags = SocketFlags.None;
                    EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);

                    int len = receiver.ReceiveMessageFrom(new byte[1024], 0, 1024, ref flags, ref remoteEP, out packetInformation);

                    Assert.Equal(1024, len);
                    Assert.Equal(sender.LocalEndPoint, remoteEP);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);

                    sender.Dispose();
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Success_APM(bool ipv4)
        {
            AddressFamily family;
            IPAddress loopback, any;
            SocketOptionLevel level;
            if (ipv4)
            {
                if (!Socket.OSSupportsIPv4) return;
                family = AddressFamily.InterNetwork;
                loopback = IPAddress.Loopback;
                any = IPAddress.Any;
                level = SocketOptionLevel.IP;
            }
            else
            {
                if (!Socket.OSSupportsIPv6) return;
                family = AddressFamily.InterNetworkV6;
                loopback = IPAddress.IPv6Loopback;
                any = IPAddress.IPv6Any;
                level = SocketOptionLevel.IPv6;
            }

            using (var receiver = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            {
                int port = receiver.BindToAnonymousPort(loopback);
                receiver.SetSocketOption(level, SocketOptionName.PacketInformation, true);
                sender.Bind(new IPEndPoint(loopback, 0));

                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.SendTo(new byte[1024], new IPEndPoint(loopback, port));
                }

                IPPacketInformation packetInformation;
                SocketFlags flags = SocketFlags.None;
                EndPoint remoteEP = new IPEndPoint(any, 0);

                IAsyncResult ar = receiver.BeginReceiveMessageFrom(new byte[1024], 0, 1024, flags, ref remoteEP, null, null);
                int len = receiver.EndReceiveMessageFrom(ar, ref flags, ref remoteEP, out packetInformation);

                Assert.Equal(1024, len);
                Assert.Equal(sender.LocalEndPoint, remoteEP);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 2)]
        public void Success_EventArgs(bool ipv4, int bufferMode)
        {
            AddressFamily family;
            IPAddress loopback, any;
            SocketOptionLevel level;
            if (ipv4)
            {
                if (!Socket.OSSupportsIPv4) return;
                family = AddressFamily.InterNetwork;
                loopback = IPAddress.Loopback;
                any = IPAddress.Any;
                level = SocketOptionLevel.IP;
            }
            else
            {
                if (!Socket.OSSupportsIPv6) return;
                family = AddressFamily.InterNetworkV6;
                loopback = IPAddress.IPv6Loopback;
                any = IPAddress.IPv6Any;
                level = SocketOptionLevel.IPv6;
            }

            using (var receiver = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            using (var saea = new SocketAsyncEventArgs())
            {
                int port = receiver.BindToAnonymousPort(loopback);
                receiver.SetSocketOption(level, SocketOptionName.PacketInformation, true);
                sender.Bind(new IPEndPoint(loopback, 0));

                saea.RemoteEndPoint = new IPEndPoint(any, 0);
                switch (bufferMode)
                {
                    case 0: // single buffer
                        saea.SetBuffer(new byte[1024], 0, 1024);
                        break;
                    case 1: // single buffer in buffer list
                        saea.BufferList = new List<ArraySegment<byte>>
                        {
                            new ArraySegment<byte>(new byte[1024])
                        };
                        break;
                    case 2: // multiple buffers in buffer list
                        saea.BufferList = new List<ArraySegment<byte>>
                        {
                            new ArraySegment<byte>(new byte[512]),
                            new ArraySegment<byte>(new byte[512])
                        };
                        break;
                }

                var mres = new ManualResetEventSlim();
                saea.Completed += delegate { mres.Set(); };
                
                bool pending = receiver.ReceiveMessageFromAsync(saea);
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.SendTo(new byte[1024], new IPEndPoint(loopback, port));
                }
                if (pending) Assert.True(mres.Wait(30000), "Expected operation to complete within timeout");

                Assert.Equal(1024, saea.BytesTransferred);
                Assert.Equal(sender.LocalEndPoint, saea.RemoteEndPoint);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, saea.ReceiveMessageFromPacketInfo.Address);
            }
        }
    }
}
