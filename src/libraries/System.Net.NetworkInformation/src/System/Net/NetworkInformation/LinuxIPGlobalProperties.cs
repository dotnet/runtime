// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    internal sealed class LinuxIPGlobalProperties : UnixIPGlobalProperties
    {
        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            return StringParsingHelpers.ParseActiveTcpConnectionsFromFiles(Socket.OSSupportsIPv4 ? NetworkFiles.Tcp4ConnectionsFile : null,
                                                                           Socket.OSSupportsIPv6 ? NetworkFiles.Tcp6ConnectionsFile : null);
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            return StringParsingHelpers.ParseActiveTcpListenersFromFiles(Socket.OSSupportsIPv4 ? NetworkFiles.Tcp4ConnectionsFile : null,
                                                                         Socket.OSSupportsIPv6 ? NetworkFiles.Tcp6ConnectionsFile : null);
        }

        public override IPEndPoint[] GetActiveUdpListeners()
        {
            return StringParsingHelpers.ParseActiveUdpListenersFromFiles(Socket.OSSupportsIPv4 ? NetworkFiles.Udp4ConnectionsFile : null,
                                                                         Socket.OSSupportsIPv6 ? NetworkFiles.Udp6ConnectionsFile : null);
        }

        public override IcmpV4Statistics GetIcmpV4Statistics()
        {
            return new LinuxIcmpV4Statistics();
        }

        public override IcmpV6Statistics GetIcmpV6Statistics()
        {
            return new LinuxIcmpV6Statistics();
        }

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
        {
            return new LinuxIPGlobalStatistics(ipv4: true);
        }

        public override IPGlobalStatistics GetIPv6GlobalStatistics()
        {
            return new LinuxIPGlobalStatistics(ipv4: false);
        }

        public override TcpStatistics GetTcpIPv4Statistics()
        {
            return new LinuxTcpStatistics(ipv4: true);
        }

        public override TcpStatistics GetTcpIPv6Statistics()
        {
            return new LinuxTcpStatistics(ipv4: false);
        }

        public override UdpStatistics GetUdpIPv4Statistics()
        {
            return new LinuxUdpStatistics(true);
        }

        public override UdpStatistics GetUdpIPv6Statistics()
        {
            return new LinuxUdpStatistics(false);
        }
    }
}
