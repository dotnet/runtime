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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        public void IPGlobalProperties_AccessAllMethods_NoErrors()
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();

            Assert.NotNull(gp.GetActiveTcpConnections());
            Assert.NotNull(gp.GetActiveTcpListeners());
            Assert.NotNull(gp.GetActiveUdpListeners());

            Assert.NotNull(gp.GetIPv4GlobalStatistics());
            if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        [MemberData(nameof(Loopbacks))]
        public void IPGlobalProperties_TcpListeners_Succeed(IPAddress address)
        {
            using (var server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                server.Bind(new IPEndPoint(address, 0));
                server.Listen(1);
                _log.WriteLine($"listening on {server.LocalEndPoint}");

                IPEndPoint[] tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                bool found = false;
                foreach (IPEndPoint ep in tcpListeners)
                {
                    if (ep.Equals(server.LocalEndPoint))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        public void IPGlobalProperties_TcpActiveConnections_NotListening()
        {
            TcpConnectionInformation[] tcpCconnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            foreach (TcpConnectionInformation ti in tcpCconnections)
            {
                Assert.NotEqual(TcpState.Listen, ti.State);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/18258")]
        public async Task GetUnicastAddresses_NotEmpty()
        {
            IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
            Assert.NotEmpty(props.GetUnicastAddresses());
            Assert.NotEmpty(await props.GetUnicastAddressesAsync());
            Assert.NotEmpty(await Task.Factory.FromAsync(props.BeginGetUnicastAddresses, props.EndGetUnicastAddresses, null));
        }
    }
}
