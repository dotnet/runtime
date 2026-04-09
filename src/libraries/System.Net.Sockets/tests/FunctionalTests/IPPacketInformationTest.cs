// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class IPPacketInformationTest
    {
        [Fact]
        public void Equals_DefaultValues_Success()
        {
            Assert.Equal(default(IPPacketInformation), default(IPPacketInformation));

            Assert.True(default(IPPacketInformation) == default(IPPacketInformation));
            Assert.True(default(IPPacketInformation).Equals(default(IPPacketInformation)));

            Assert.False(default(IPPacketInformation) != default(IPPacketInformation));
        }

        [Fact]
        public void GetHashCode_DefaultValues_Success()
        {
            Assert.Equal(default(IPPacketInformation).GetHashCode(), default(IPPacketInformation).GetHashCode());
        }

        [Fact]
        public async Task Equals_NonDefaultValue_Success()
        {
            IPPacketInformation packetInfo = await GetNonDefaultIPPacketInformation();
            IPPacketInformation packetInfoCopy = packetInfo;

            Assert.Equal(packetInfo, packetInfoCopy);
            Assert.True(packetInfo == packetInfoCopy);
            Assert.True(packetInfo.Equals(packetInfoCopy));
            Assert.True(packetInfo.Equals((object)packetInfoCopy));
            Assert.False(packetInfo != packetInfoCopy);

            Assert.NotEqual(default, packetInfo);
            Assert.False(packetInfo == default(IPPacketInformation));
            Assert.False(packetInfo.Equals(default(IPPacketInformation)));
            Assert.False(packetInfo.Equals((object)default(IPPacketInformation)));
            Assert.True(packetInfo != default(IPPacketInformation));

            int ignored = packetInfo.Interface; // just make sure it doesn't throw, nothing else to verify
        }

        [Fact]
        public async Task GetHashCode_NonDefaultValue_Success()
        {
            IPPacketInformation packetInfo = await GetNonDefaultIPPacketInformation();

            Assert.Equal(packetInfo.GetHashCode(), packetInfo.GetHashCode());
        }

        private async Task<IPPacketInformation> GetNonDefaultIPPacketInformation()
        {
            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int port = receiver.BindToAnonymousPort(IPAddress.Loopback);
                // Send a few packets, in case they aren't delivered reliably.
                var receiveTask = receiver.ReceiveMessageFromAsync(new byte[1], new IPEndPoint(IPAddress.Loopback, port));
                var sendTask = sender.SendToAsync(new byte[1], new IPEndPoint(IPAddress.Loopback, port));

                Assert.True(await Task.WhenAny(receiveTask, Task.Delay(TestSettings.PassingTestTimeout)) == receiveTask, "Timed out");

                var result = await receiveTask;

                return result.PacketInformation;
            }
        }
    }
}
