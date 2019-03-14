// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
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

        public IPGlobalPropertiesTest()
        {
            _log = TestLogging.GetInstance();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void IPGlobalProperties_AccessAllMethods_NoErrors()
        {
            IPGlobalProperties gp = IPGlobalProperties.GetIPGlobalProperties();

            Assert.NotNull(gp.GetActiveTcpConnections());
            Assert.NotNull(gp.GetActiveTcpListeners());
            Assert.NotNull(gp.GetActiveUdpListeners());

            Assert.NotNull(gp.GetIPv4GlobalStatistics());
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [MemberData(nameof(Loopbacks))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [MemberData(nameof(Loopbacks))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void IPGlobalProperties_TcpActiveConnections_NotListening()
        {
            TcpConnectionInformation[] tcpCconnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            foreach (TcpConnectionInformation ti in tcpCconnections)
            {
                Assert.NotEqual(TcpState.Listen, ti.State);
            }
        }
    }
}
