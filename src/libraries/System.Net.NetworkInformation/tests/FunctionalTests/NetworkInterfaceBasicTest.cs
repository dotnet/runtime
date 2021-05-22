// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public NetworkInterfaceBasicTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
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

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    Assert.Equal(6, nic.GetPhysicalAddress().GetAddressBytes().Length);
                }
            }
        }

        [Fact]
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
                Assert.False(nic.IsReceiveOnly);
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

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    Assert.Equal(6, nic.GetPhysicalAddress().GetAddressBytes().Length);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX|TestPlatforms.FreeBSD)]
        public void BasicTest_AccessInstanceProperties_NoExceptions_Bsd()
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
                Assert.False(nic.IsReceiveOnly);
                _log.WriteLine("Type: " + nic.NetworkInterfaceType);
                _log.WriteLine("Status: " + nic.OperationalStatus);
                _log.WriteLine("Speed: " + nic.Speed);
                Assert.InRange(nic.Speed, 0, long.MaxValue);
                _log.WriteLine("SupportsMulticast: " + nic.SupportsMulticast);
                _log.WriteLine("GetPhysicalAddress(): " + nic.GetPhysicalAddress());

                if (nic.Name.StartsWith("en") || nic.Name == "lo0")
                {
                    // Ethernet, WIFI and loopback should have known status.
                    Assert.True((nic.OperationalStatus == OperationalStatus.Up) || (nic.OperationalStatus == OperationalStatus.Down));
                }

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    Assert.Equal(6, nic.GetPhysicalAddress().GetAddressBytes().Length);
                }
            }
        }

        [Fact]
        [Trait("IPv4", "true")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
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
                        Assert.True(nic.NetworkInterfaceType == NetworkInterfaceType.Loopback);
                        return; // Only check IPv4 loopback
                    }
                }
            }
        }

        [Fact]
        [Trait("IPv4", "true")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
        public void BasicTest_StaticLoopbackIndex_ExceptionIfV4NotSupported()
        {
            Assert.True(Capability.IPv4Support());

            _log.WriteLine("Loopback IPv4 index: " + NetworkInterface.LoopbackInterfaceIndex);
        }

        [Fact]
        [Trait("IPv6", "true")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
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

        [Fact]
        [Trait("IPv6", "true")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/runtime/issues/20029 and https://github.com/Microsoft/WSL/issues/3561
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
        [PlatformSpecific(TestPlatforms.OSX|TestPlatforms.FreeBSD)]
        public void BasicTest_GetIPInterfaceStatistics_Success_Bsd()
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


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/dotnet/runtime/issues/20029 and https://github.com/Microsoft/WSL/issues/3561
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36890", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void BasicTest_GetIsNetworkAvailable_Success()
        {
            Assert.True(NetworkInterface.GetIsNetworkAvailable());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.FreeBSD, "Expected behavior is different on OSX or FreeBSD")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50567", TestPlatforms.Android)]
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
