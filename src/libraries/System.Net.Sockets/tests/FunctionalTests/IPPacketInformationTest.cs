// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

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
        public void Equals_NonDefaultValue_Success()
        {
            IPPacketInformation packetInfo = GetNonDefaultIPPacketInformation();
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
        public void GetHashCode_NonDefaultValue_Success()
        {
            IPPacketInformation packetInfo = GetNonDefaultIPPacketInformation();

            Assert.Equal(packetInfo.GetHashCode(), packetInfo.GetHashCode());
        }

        private IPPacketInformation GetNonDefaultIPPacketInformation()
        {
            const int ReceiveTimeout = 10000;

            using (var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int port = receiver.BindToAnonymousPort(IPAddress.Loopback);

                var waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port),
                    UserToken = waitHandle
                };

                receiveArgs.SetBuffer(new byte[1], 0, 1);
                receiveArgs.Completed += (_, args) => ((ManualResetEvent)args.UserToken).Set();

                Assert.True(receiver.ReceiveMessageFromAsync(receiveArgs), "receiver.ReceiveMessageFromAsync");

                // Send a few packets, in case they aren't delivered reliably.
                sender.SendTo(new byte[1], new IPEndPoint(IPAddress.Loopback, port));

                Assert.True(waitHandle.WaitOne(ReceiveTimeout), "waitHandle.WaitOne");

                return receiveArgs.ReceiveMessageFromPacketInfo;
            }
        }
    }
}
