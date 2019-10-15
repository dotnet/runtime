// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ReceiveMessageFromAsync
    {
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ReceiveSentMessages_SocketAsyncEventArgs_Success(bool ipv4, bool changeReceiveBufferEachCall)
        {
            const int DataLength = 1024;
            AddressFamily family = ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
            IPAddress loopback = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            var completed = new ManualResetEventSlim(false);
            using (var sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            using (var receiver = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            {
                sender.Bind(new IPEndPoint(loopback, 0));
                receiver.SetSocketOption(ipv4 ? SocketOptionLevel.IP : SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                int port = receiver.BindToAnonymousPort(loopback);

                var args = new SocketAsyncEventArgs() { RemoteEndPoint = new IPEndPoint(ipv4 ? IPAddress.Any : IPAddress.IPv6Any, 0) };
                args.Completed += (s,e) => completed.Set();
                args.SetBuffer(new byte[DataLength], 0, DataLength);

                for (int iters = 0; iters < 5; iters++)
                {
                    for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                    {
                        sender.SendTo(new byte[DataLength], new IPEndPoint(loopback, port));
                    }

                    if (changeReceiveBufferEachCall)
                    {
                        args.SetBuffer(new byte[DataLength], 0, DataLength);
                    }

                    if (!receiver.ReceiveMessageFromAsync(args))
                    {
                        completed.Set();
                    }
                    Assert.True(completed.Wait(TestSettings.PassingTestTimeout), "Timeout while waiting for connection");
                    completed.Reset();

                    Assert.Equal(DataLength, args.BytesTransferred);
                    Assert.Equal(sender.LocalEndPoint, args.RemoteEndPoint);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, args.ReceiveMessageFromPacketInfo.Address);
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReceiveSentMessages_Tasks_Success(bool ipv4)
        {
            const int DataLength = 1024;
            AddressFamily family = ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
            IPAddress loopback = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            using (var receiver = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            {
                sender.Bind(new IPEndPoint(loopback, 0));
                receiver.SetSocketOption(ipv4 ? SocketOptionLevel.IP : SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                int port = receiver.BindToAnonymousPort(loopback);

                for (int iters = 0; iters < 5; iters++)
                {
                    for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                    {
                        sender.SendTo(new byte[DataLength], new IPEndPoint(loopback, port));
                    }

                    SocketReceiveMessageFromResult result = await receiver.ReceiveMessageFromAsync(
                        new ArraySegment<byte>(new byte[DataLength], 0, DataLength), SocketFlags.None,
                        new IPEndPoint(ipv4 ? IPAddress.Any : IPAddress.IPv6Any, 0));
                    Assert.Equal(DataLength, result.ReceivedBytes);
                    Assert.Equal(sender.LocalEndPoint, result.RemoteEndPoint);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, result.PacketInformation.Address);
                }
            }
        }
    }
}
