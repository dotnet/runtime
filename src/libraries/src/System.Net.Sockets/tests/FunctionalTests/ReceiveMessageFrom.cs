// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ReceiveMessageFrom
    {
        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/987
        public void Success()
        {
            if (Socket.OSSupportsIPv4)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

                    Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

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
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/987
        public void Success_IPv6()
        {
            if (Socket.OSSupportsIPv6)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.IPv6Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);

                    Socket sender = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));

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
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/987
        public void Success_APM()
        {
            if (Socket.OSSupportsIPv4)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

                    Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                    {
                        sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.Loopback, port));
                    }

                    IPPacketInformation packetInformation;
                    SocketFlags flags = SocketFlags.None;
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                    IAsyncResult ar = receiver.BeginReceiveMessageFrom(new byte[1024], 0, 1024, flags, ref remoteEP, null, null);
                    ar.AsyncWaitHandle.WaitOne();
                    int len = receiver.EndReceiveMessageFrom(ar, ref flags, ref remoteEP, out packetInformation);

                    Assert.Equal(1024, len);
                    Assert.Equal(sender.LocalEndPoint, remoteEP);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);

                    sender.Dispose();
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/987
        public void Success_APM_IPv6()
        {
            if (Socket.OSSupportsIPv6)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.IPv6Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);

                    Socket sender = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));

                    for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                    {
                        sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.IPv6Loopback, port));
                    }

                    IPPacketInformation packetInformation;
                    SocketFlags flags = SocketFlags.None;
                    EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);

                    IAsyncResult ar = receiver.BeginReceiveMessageFrom(new byte[1024], 0, 1024, flags, ref remoteEP, null, null);
                    ar.AsyncWaitHandle.WaitOne();
                    int len = receiver.EndReceiveMessageFrom(ar, ref flags, ref remoteEP, out packetInformation);

                    Assert.Equal(1024, len);
                    Assert.Equal(sender.LocalEndPoint, remoteEP);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);

                    sender.Dispose();
                }
            }
        }
    }
}
