// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    public class NetworkInterfaceBasicTest
    {
        private readonly ITestOutputHelper _log;

        public NetworkInterfaceBasicTest()
        {
            _log = TestLogging.GetInstance();
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        public void BasicTest_GetNetworkInterfaces_AtLeastOne()
        {
            Assert.NotEqual<int>(0, NetworkInterface.GetAllNetworkInterfaces().Length);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Not all APIs are supported on Linux and OSX
        public void BasicTest_AccessInstanceProperties_NoExceptions()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("- NetworkInterface -");
                _log.WriteLine("Name: " + nic.Name);
                _log.WriteLine("Description: " + nic.Description);
                _log.WriteLine("ID: " + nic.Id);
                _log.WriteLine("IsReceiveOnly: " + nic.IsReceiveOnly);
                _log.WriteLine("Type: " + nic.NetworkInterfaceType);
                _log.WriteLine("Status: " + nic.OperationalStatus);
                _log.WriteLine("Speed: " + nic.Speed);

                // Validate NIC speed overflow.
                // We've found that certain WiFi adapters will return speed of -1 when not connected.
                // We've found that Wi-Fi Direct Virtual Adapters return speed of -1 even when up.
                Assert.InRange(nic.Speed, -1, long.MaxValue);

                _log.WriteLine("SupportsMulticast: " + nic.SupportsMulticast);
                _log.WriteLine("GetPhysicalAddress(): " + nic.GetPhysicalAddress());
            }
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [PlatformSpecific(TestPlatforms.Linux)]  // Some APIs are not supported on Linux
        public void BasicTest_AccessInstanceProperties_NoExceptions_Linux()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("- NetworkInterface -");
                _log.WriteLine("Name: " + nic.Name);
                string description = nic.Description;
                Assert.False(string.IsNullOrEmpty(description), "NetworkInterface.Description should not be null or empty.");
                _log.WriteLine("Description: " + description);
                string id = nic.Id;
                Assert.False(string.IsNullOrEmpty(id), "NetworkInterface.Id should not be null or empty.");
                _log.WriteLine("ID: " + id);
                Assert.Throws<PlatformNotSupportedException>(() => nic.IsReceiveOnly);
                _log.WriteLine("Type: " + nic.NetworkInterfaceType);
                _log.WriteLine("Status: " + nic.OperationalStatus);

                try
                {
                    _log.WriteLine("Speed: " + nic.Speed);
                    Assert.InRange(nic.Speed, -1, long.MaxValue);
                }
                // We cannot guarantee this works on all devices.
                catch (PlatformNotSupportedException pnse)
                {
                    _log.WriteLine(pnse.ToString());
                }

                _log.WriteLine("SupportsMulticast: " + nic.SupportsMulticast);
                _log.WriteLine("GetPhysicalAddress(): " + nic.GetPhysicalAddress());
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // Some APIs are not supported on OSX
        public void BasicTest_AccessInstanceProperties_NoExceptions_Osx()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("- NetworkInterface -");
                _log.WriteLine("Name: " + nic.Name);
                string description = nic.Description;
                Assert.False(string.IsNullOrEmpty(description), "NetworkInterface.Description should not be null or empty.");
                _log.WriteLine("Description: " + description);
                string id = nic.Id;
                Assert.False(string.IsNullOrEmpty(id), "NetworkInterface.Id should not be null or empty.");
                _log.WriteLine("ID: " + id);
                Assert.Throws<PlatformNotSupportedException>(() => nic.IsReceiveOnly);
                _log.WriteLine("Type: " + nic.NetworkInterfaceType);
                _log.WriteLine("Status: " + nic.OperationalStatus);
                _log.WriteLine("Speed: " + nic.Speed);
                Assert.InRange(nic.Speed, 0, long.MaxValue);
                _log.WriteLine("SupportsMulticast: " + nic.SupportsMulticast);
                _log.WriteLine("GetPhysicalAddress(): " + nic.GetPhysicalAddress());
            }
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [Trait("IPv4", "true")]
        public void BasicTest_StaticLoopbackIndex_MatchesLoopbackNetworkInterface()
        {
            Assert.True(Capability.IPv4Support());

            _log.WriteLine("Loopback IPv4 index: " + NetworkInterface.LoopbackInterfaceIndex);

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicast in nic.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.Equals(IPAddress.Loopback))
                    {
                        Assert.Equal<int>(nic.GetIPProperties().GetIPv4Properties().Index,
                            NetworkInterface.LoopbackInterfaceIndex);
                        return; // Only check IPv4 loopback
                    }
                }
            }
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [Trait("IPv4", "true")]
        public void BasicTest_StaticLoopbackIndex_ExceptionIfV4NotSupported()
        {
            Assert.True(Capability.IPv4Support());

            _log.WriteLine("Loopback IPv4 index: " + NetworkInterface.LoopbackInterfaceIndex);
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [Trait("IPv6", "true")]
        public void BasicTest_StaticIPv6LoopbackIndex_MatchesLoopbackNetworkInterface()
        {
            Assert.True(Capability.IPv6Support());

            _log.WriteLine("Loopback IPv6 index: " + NetworkInterface.IPv6LoopbackInterfaceIndex);

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicast in nic.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.Equals(IPAddress.IPv6Loopback))
                    {
                        Assert.Equal<int>(
                            nic.GetIPProperties().GetIPv6Properties().Index,
                            NetworkInterface.IPv6LoopbackInterfaceIndex);

                        return; // Only check IPv6 loopback.
                    }
                }
            }
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [Trait("IPv6", "true")]
        public void BasicTest_StaticIPv6LoopbackIndex_ExceptionIfV6NotSupported()
        {
            Assert.True(Capability.IPv6Support());
            _log.WriteLine("Loopback IPv6 index: " + NetworkInterface.IPv6LoopbackInterfaceIndex);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Not all APIs are supported on Linux and OSX
        public void BasicTest_GetIPInterfaceStatistics_Success()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceStatistics stats = nic.GetIPStatistics();

                _log.WriteLine("- Stats for : " + nic.Name);
                _log.WriteLine("BytesReceived: " + stats.BytesReceived);
                _log.WriteLine("BytesSent: " + stats.BytesSent);
                _log.WriteLine("IncomingPacketsDiscarded: " + stats.IncomingPacketsDiscarded);
                _log.WriteLine("IncomingPacketsWithErrors: " + stats.IncomingPacketsWithErrors);
                _log.WriteLine("IncomingUnknownProtocolPackets: " + stats.IncomingUnknownProtocolPackets);
                _log.WriteLine("NonUnicastPacketsReceived: " + stats.NonUnicastPacketsReceived);
                _log.WriteLine("NonUnicastPacketsSent: " + stats.NonUnicastPacketsSent);
                _log.WriteLine("OutgoingPacketsDiscarded: " + stats.OutgoingPacketsDiscarded);
                _log.WriteLine("OutgoingPacketsWithErrors: " + stats.OutgoingPacketsWithErrors);
                _log.WriteLine("OutputQueueLength: " + stats.OutputQueueLength);
                _log.WriteLine("UnicastPacketsReceived: " + stats.UnicastPacketsReceived);
                _log.WriteLine("UnicastPacketsSent: " + stats.UnicastPacketsSent);
            }
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [PlatformSpecific(TestPlatforms.Linux)]  // Some APIs are not supported on Linux
        public void BasicTest_GetIPInterfaceStatistics_Success_Linux()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceStatistics stats = nic.GetIPStatistics();

                _log.WriteLine("- Stats for : " + nic.Name);
                _log.WriteLine("BytesReceived: " + stats.BytesReceived);
                _log.WriteLine("BytesSent: " + stats.BytesSent);
                _log.WriteLine("IncomingPacketsDiscarded: " + stats.IncomingPacketsDiscarded);
                _log.WriteLine("IncomingPacketsWithErrors: " + stats.IncomingPacketsWithErrors);
                Assert.Throws<PlatformNotSupportedException>(() => stats.IncomingUnknownProtocolPackets);
                _log.WriteLine("NonUnicastPacketsReceived: " + stats.NonUnicastPacketsReceived);
                Assert.Throws<PlatformNotSupportedException>(() => stats.NonUnicastPacketsSent);
                _log.WriteLine("OutgoingPacketsDiscarded: " + stats.OutgoingPacketsDiscarded);
                _log.WriteLine("OutgoingPacketsWithErrors: " + stats.OutgoingPacketsWithErrors);
                _log.WriteLine("OutputQueueLength: " + stats.OutputQueueLength);
                _log.WriteLine("UnicastPacketsReceived: " + stats.UnicastPacketsReceived);
                _log.WriteLine("UnicastPacketsSent: " + stats.UnicastPacketsSent);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // Some APIs are not supported on OSX
        public void BasicTest_GetIPInterfaceStatistics_Success_OSX()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceStatistics stats = nic.GetIPStatistics();

                _log.WriteLine("- Stats for : " + nic.Name);
                _log.WriteLine("BytesReceived: " + stats.BytesReceived);
                _log.WriteLine("BytesSent: " + stats.BytesSent);
                _log.WriteLine("IncomingPacketsDiscarded: " + stats.IncomingPacketsDiscarded);
                _log.WriteLine("IncomingPacketsWithErrors: " + stats.IncomingPacketsWithErrors);
                _log.WriteLine("IncomingUnknownProtocolPackets: " + stats.IncomingUnknownProtocolPackets);
                _log.WriteLine("NonUnicastPacketsReceived: " + stats.NonUnicastPacketsReceived);
                _log.WriteLine("NonUnicastPacketsSent: " + stats.NonUnicastPacketsSent);
                Assert.Throws<PlatformNotSupportedException>(() => stats.OutgoingPacketsDiscarded);
                _log.WriteLine("OutgoingPacketsWithErrors: " + stats.OutgoingPacketsWithErrors);
                _log.WriteLine("OutputQueueLength: " + stats.OutputQueueLength);
                _log.WriteLine("UnicastPacketsReceived: " + stats.UnicastPacketsReceived);
                _log.WriteLine("UnicastPacketsSent: " + stats.UnicastPacketsSent);
            }
        }


        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        public void BasicTest_GetIsNetworkAvailable_Success()
        {
            Assert.True(NetworkInterface.GetIsNetworkAvailable());
        }

        // dotnet: /d/git/corefx/src/Native/Unix/Common/pal_safecrt.h:47: errno_t memcpy_s(void *, size_t, const void *, size_t): Assertion `sizeInBytes >= count' failed.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/corefx/issues/15513
        [PlatformSpecific(~TestPlatforms.OSX)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NetworkInterface_LoopbackInterfaceIndex_MatchesReceivedPackets(bool ipv6)
        {
            using (var client = new Socket(SocketType.Dgram, ProtocolType.Udp))
            using (var server = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                server.Bind(new IPEndPoint(ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, 0));
                var serverEndPoint = (IPEndPoint)server.LocalEndPoint;

                Task<SocketReceiveMessageFromResult> receivedTask = 
                    server.ReceiveMessageFromAsync(new ArraySegment<byte>(new byte[1]), SocketFlags.None, serverEndPoint);
                while (!receivedTask.IsCompleted)
                {
                    client.SendTo(new byte[] { 42 }, serverEndPoint);
                    await Task.Delay(1);
                }

                Assert.Equal(
                    (await receivedTask).PacketInformation.Interface,
                    ipv6 ? NetworkInterface.IPv6LoopbackInterfaceIndex : NetworkInterface.LoopbackInterfaceIndex);
            }
        }
    }
}
