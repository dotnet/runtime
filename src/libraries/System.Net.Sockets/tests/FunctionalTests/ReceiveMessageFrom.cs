// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class ReceiveMessageFrom<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected ReceiveMessageFrom(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveSentMessages_Success(bool ipv4)
        {
            const int DatagramSize = 256;
            const int DatagramsToSend = 16;

            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            using Socket receiver = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket sender = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            receiver.SetSocketOption(ipv4 ? SocketOptionLevel.IP : SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
            ConfigureNonBlocking(sender);
            ConfigureNonBlocking(receiver);

            receiver.BindToAnonymousPort(address);
            sender.BindToAnonymousPort(address);

            byte[] sendBuffer = new byte[DatagramSize];
            byte[] receiveBuffer = new byte[DatagramSize];
            Random rnd = new Random(0);

            IPEndPoint remoteEp = new IPEndPoint(ipv4 ? IPAddress.Any : IPAddress.IPv6Any, 0);

            for (int i = 0; i < DatagramsToSend; i++)
            {
                rnd.NextBytes(sendBuffer);
                sender.SendTo(sendBuffer, receiver.LocalEndPoint);

                SocketReceiveMessageFromResult result = await ReceiveMessageFromAsync(receiver, receiveBuffer, remoteEp);
                IPPacketInformation packetInformation = result.PacketInformation;

                Assert.Equal(DatagramSize, result.ReceivedBytes);
                Assert.Equal(Fletcher32.Checksum(sendBuffer), Fletcher32.Checksum(receiveBuffer));
                Assert.Equal(sender.LocalEndPoint, result.RemoteEndPoint);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, packetInformation.Address);
            }
        }
    }

    public class ReceiveMessageFrom_Old
    {
        [OuterLoop]
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

                    sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.Loopback, port));

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

        [OuterLoop]
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

                    sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.IPv6Loopback, port));

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

        [OuterLoop]
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

                sender.SendTo(new byte[1024], new IPEndPoint(loopback, port));

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

        [OuterLoop]
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
                sender.SendTo(new byte[1024], new IPEndPoint(loopback, port));
                if (pending) Assert.True(mres.Wait(30000), "Expected operation to complete within timeout");

                Assert.Equal(1024, saea.BytesTransferred);
                Assert.Equal(sender.LocalEndPoint, saea.RemoteEndPoint);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, saea.ReceiveMessageFromPacketInfo.Address);
            }
        }
    }

    public sealed class ReceiveMessageFrom_Sync : ReceiveMessageFrom<SocketHelperArraySync>
    {
        public ReceiveMessageFrom_Sync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_SyncForceNonBlocking : ReceiveMessageFrom<SocketHelperSyncForceNonBlocking>
    {
        public ReceiveMessageFrom_SyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_Apm : ReceiveMessageFrom<SocketHelperApm>
    {
        public ReceiveMessageFrom_Apm(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_Task : ReceiveMessageFrom<SocketHelperTask>
    {
        public ReceiveMessageFrom_Task(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_Eap : ReceiveMessageFrom<SocketHelperEap>
    {
        public ReceiveMessageFrom_Eap(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_SpanSync : ReceiveMessageFrom<SocketHelperSpanSync>
    {
        public ReceiveMessageFrom_SpanSync(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_SpanSyncForceNonBlocking : ReceiveMessageFrom<SocketHelperSpanSyncForceNonBlocking>
    {
        public ReceiveMessageFrom_SpanSyncForceNonBlocking(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_MemoryArrayTask : ReceiveMessageFrom<SocketHelperMemoryArrayTask>
    {
        public ReceiveMessageFrom_MemoryArrayTask(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ReceiveMessageFrom_MemoryNativeTask : ReceiveMessageFrom<SocketHelperMemoryNativeTask>
    {
        public ReceiveMessageFrom_MemoryNativeTask(ITestOutputHelper output) : base(output) { }
    }
}
