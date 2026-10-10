// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    public partial class IPGlobalPropertiesTest
    {
        private const int OperationNotPermitted = 1; // EPERM on macOS.
        private const int RemoteTimeoutMilliseconds = 10_000;
        private readonly ITestOutputHelper _log;

        public static IEnumerable<object[]> Loopbacks()
        {
            if (Socket.OSSupportsIPv4)
            {
                yield return new object[] { IPAddress.Loopback };
            }

            if (Socket.OSSupportsIPv6)
            {
                yield return new object[] { IPAddress.IPv6Loopback };
            }
        }

        public IPGlobalPropertiesTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.OSX)]
        [InlineData(nameof(IPGlobalProperties.GetActiveTcpConnections), "pcbcount")]
        [InlineData(nameof(IPGlobalProperties.GetActiveTcpConnections), "pcblist")]
        [InlineData(nameof(IPGlobalProperties.GetActiveTcpListeners), "pcbcount")]
        [InlineData(nameof(IPGlobalProperties.GetActiveTcpListeners), "pcblist")]
        [InlineData(nameof(IPGlobalProperties.GetActiveUdpListeners), "pcbcount")]
        [InlineData(nameof(IPGlobalProperties.GetActiveUdpListeners), "pcblist")]
        public void IPGlobalProperties_SysctlDenied_ThrowsNetworkInformationException(string methodName, string queryName)
        {
            // This uses macOS's deprecated raw-profile sandbox API and requires an unsandboxed test host.
            // A fresh child isolates the irreversible restriction but cannot escape an inherited sandbox.
            RemoteExecutor.Invoke(static (methodName, queryName) =>
            {
                string protocol = methodName == nameof(IPGlobalProperties.GetActiveUdpListeners) ? "udp" : "tcp";
                string profile = $"(version 1)(allow default)(deny sysctl-read (sysctl-name \"net.inet.{protocol}.{queryName}\"))";
                int result = SandboxInit(profile, 0, out nint errorBuffer);
                try
                {
                    Assert.True(result == 0, Marshal.PtrToStringUTF8(errorBuffer));
                }
                finally
                {
                    if (errorBuffer != 0)
                    {
                        SandboxFreeError(errorBuffer);
                    }
                }

                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                Func<Array> query = methodName switch
                {
                    nameof(IPGlobalProperties.GetActiveTcpConnections) => properties.GetActiveTcpConnections,
                    nameof(IPGlobalProperties.GetActiveTcpListeners) => properties.GetActiveTcpListeners,
                    nameof(IPGlobalProperties.GetActiveUdpListeners) => properties.GetActiveUdpListeners,
                    _ => throw new InvalidOperationException(methodName)
                };

                Marshal.SetLastPInvokeError(0);
                NetworkInformationException exception = Assert.Throws<NetworkInformationException>(() => query());
                Assert.Equal(OperationNotPermitted, exception.ErrorCode);
            }, methodName, queryName, new RemoteInvokeOptions { TimeOut = RemoteTimeoutMilliseconds }).Dispose();
        }

        [LibraryImport("/usr/lib/libsandbox.dylib", EntryPoint = "sandbox_init", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int SandboxInit(string profile, ulong flags, out nint errorBuffer);

        [LibraryImport("/usr/lib/libsandbox.dylib", EntryPoint = "sandbox_free_error")]
        private static partial void SandboxFreeError(nint errorBuffer);

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Expected behavior is different on Android")]
        [SkipOnPlatform(TestPlatforms.OpenBSD, "TCP/UDP connection enumeration is unsupported on OpenBSD")]
        public void IPGlobalProperties_AccessAllMethods_NoErrors()
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();

            Assert.NotNull(gp.GetActiveTcpConnections());
            Assert.NotNull(gp.GetActiveTcpListeners());
            Assert.NotNull(gp.GetActiveUdpListeners());

            if (Socket.OSSupportsIPv4)
            {
                Assert.NotNull(gp.GetIPv4GlobalStatistics());
                Assert.NotNull(gp.GetIcmpV4Statistics());
                Assert.NotNull(gp.GetTcpIPv4Statistics());
                Assert.NotNull(gp.GetUdpIPv4Statistics());
            }

            if (Socket.OSSupportsIPv6)
            {
                Assert.NotNull(gp.GetIcmpV6Statistics());
                Assert.NotNull(gp.GetTcpIPv6Statistics());
                Assert.NotNull(gp.GetUdpIPv6Statistics());

                if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS() && !OperatingSystem.IsFreeBSD())
                {
                    // OSX and FreeBSD do not provide IPv6  stats.
                    Assert.NotNull(gp.GetIPv6GlobalStatistics());
                }
            }
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
        [SkipOnPlatform(TestPlatforms.OpenBSD, "TCP connection enumeration is unsupported on OpenBSD")]
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
        [SkipOnPlatform(TestPlatforms.OpenBSD, "UDP listener enumeration is unsupported on OpenBSD")]
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
        [PlatformSpecific(~(TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.Android | TestPlatforms.OpenBSD))]
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
        [SkipOnPlatform(TestPlatforms.OpenBSD, "TCP connection enumeration is unsupported on OpenBSD")]
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

        [Fact]
        public void IPGlobalProperties_DomainName_ReturnsEmptyStringWhenNotSet()
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();

            // [ActiveIssue("https://github.com/dotnet/runtime/issues/109280")]
            string expectedDomainName = PlatformDetection.IsAndroid ? "localdomain" : string.Empty;
            Assert.Equal(expectedDomainName, gp.DomainName);
        }
    }
}
