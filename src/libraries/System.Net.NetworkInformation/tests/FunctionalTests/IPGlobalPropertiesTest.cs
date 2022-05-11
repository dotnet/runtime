// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    public class IPGlobalPropertiesTest
    {
        private readonly ITestOutputHelper _log;
        public static readonly object[][] Loopbacks = new[]
        {
            new object[] { IPAddress.Loopback },
            new object[] { IPAddress.IPv6Loopback },
        };

        public IPGlobalPropertiesTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Expected behavior is different on Android")]
        public void IPGlobalProperties_AccessAllMethods_NoErrors()
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();

            Assert.NotNull(gp.GetActiveTcpConnections());
            Assert.NotNull(gp.GetActiveTcpListeners());
            Assert.NotNull(gp.GetActiveUdpListeners());

            Assert.NotNull(gp.GetIPv4GlobalStatistics());
            if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS() && !OperatingSystem.IsFreeBSD())
            {
                // OSX and FreeBSD do not provide IPv6  stats.
                Assert.NotNull(gp.GetIPv6GlobalStatistics());
            }

            Assert.NotNull(gp.GetIcmpV4Statistics());
            Assert.NotNull(gp.GetIcmpV6Statistics());
            Assert.NotNull(gp.GetTcpIPv4Statistics());
            Assert.NotNull(gp.GetTcpIPv6Statistics());
            Assert.NotNull(gp.GetUdpIPv4Statistics());
            Assert.NotNull(gp.GetUdpIPv6Statistics());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void IPGlobalProperties_AccessAllMethods_NoErrors_Android()
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();

            Assert.NotNull(gp.GetIPv4GlobalStatistics());
            Assert.NotNull(gp.GetIPv6GlobalStatistics());

            Assert.Throws<PlatformNotSupportedException>(() => gp.GetActiveTcpConnections());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetActiveTcpListeners());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetActiveUdpListeners());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetIcmpV4Statistics());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetIcmpV6Statistics());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetTcpIPv4Statistics());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetTcpIPv6Statistics());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetUdpIPv4Statistics());
            Assert.Throws<PlatformNotSupportedException>(() => gp.GetUdpIPv6Statistics());
        }

        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [PlatformSpecific(TestPlatforms.Android)]
        public void IPGlobalProperties_IPv4_IPv6_NoErrors_Android(int ipVersion)
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();
            IPGlobalStatistics statistics = ipVersion switch {
                4 => gp.GetIPv4GlobalStatistics(),
                6 => gp.GetIPv6GlobalStatistics(),
                _ => throw new ArgumentOutOfRangeException()
            };

            _log.WriteLine($"- IPv{ipVersion} statistics: -");
            _log.WriteLine($"Number of interfaces: {statistics.NumberOfInterfaces}");
            _log.WriteLine($"Number of IP addresses: {statistics.NumberOfIPAddresses}");

            Assert.InRange(statistics.NumberOfInterfaces, 1, int.MaxValue);
            Assert.InRange(statistics.NumberOfIPAddresses, 1, int.MaxValue);

            Assert.Throws<PlatformNotSupportedException>(() => statistics.DefaultTtl);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ForwardingEnabled);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.OutputPacketRequests);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.OutputPacketRoutingDiscards);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.OutputPacketsDiscarded);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.OutputPacketsWithNoRoute);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.PacketFragmentFailures);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.PacketReassembliesRequired);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.PacketReassemblyFailures);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.PacketReassemblyTimeout);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.PacketsFragmented);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.PacketsReassembled);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPackets);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPacketsDelivered);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPacketsDiscarded);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPacketsForwarded);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPacketsWithAddressErrors);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPacketsWithHeadersErrors);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.ReceivedPacketsWithUnknownProtocol);
            Assert.Throws<PlatformNotSupportedException>(() => statistics.NumberOfRoutes);
        }

        [Theory]
        [MemberData(nameof(Loopbacks))]
        [SkipOnPlatform(TestPlatforms.Android, "Unsupported on Android")]
        public void IPGlobalProperties_TcpListeners_Succeed(IPAddress address)
        {
            using (var server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                server.Bind(new IPEndPoint(address, 0));
                server.Listen(1);
                _log.WriteLine($"listening on {server.LocalEndPoint}");

                IPEndPoint[] tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                Assert.Contains(server.LocalEndPoint, tcpListeners);
            }
        }

        [Theory]
        [MemberData(nameof(Loopbacks))]
        [SkipOnPlatform(TestPlatforms.Android, "Unsupported on Android")]
        public void IPGlobalProperties_UdpListeners_Succeed(IPAddress address)
        {
            using (var server = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
            {
                server.Bind(new IPEndPoint(address, 0));
                _log.WriteLine($"listening on {server.LocalEndPoint}");

                IPEndPoint[] udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
                Assert.Contains(server.LocalEndPoint, udpListeners);
            }
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [PlatformSpecific(~(TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.Android))]
        [MemberData(nameof(Loopbacks))]
        public async Task IPGlobalProperties_TcpActiveConnections_Succeed(IPAddress address)
        {
            using (var server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                server.Bind(new IPEndPoint(address, 0));
                server.Listen(1);
                _log.WriteLine($"listening on {server.LocalEndPoint}");

                await client.ConnectAsync(server.LocalEndPoint);
                _log.WriteLine($"Looking for connection {client.LocalEndPoint} <-> {client.RemoteEndPoint}");

                TcpConnectionInformation[] tcpCconnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                bool found = false;
                foreach (TcpConnectionInformation ti in tcpCconnections)
                {
                    if (ti.LocalEndPoint.Equals(client.LocalEndPoint) && ti.RemoteEndPoint.Equals(client.RemoteEndPoint) &&
                       (ti.State == TcpState.Established))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Unsupported on Android")]
        public void IPGlobalProperties_TcpActiveConnections_NotListening()
        {
            TcpConnectionInformation[] tcpCconnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            foreach (TcpConnectionInformation ti in tcpCconnections)
            {
                Assert.NotEqual(TcpState.Listen, ti.State);
            }
        }

        [Fact]
        public async Task GetUnicastAddresses_NotEmpty()
        {
            IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
            Assert.NotEmpty(props.GetUnicastAddresses());
            Assert.NotEmpty(await props.GetUnicastAddressesAsync());
            Assert.NotEmpty(await Task.Factory.FromAsync(props.BeginGetUnicastAddresses, props.EndGetUnicastAddresses, null));
        }
    }
}
