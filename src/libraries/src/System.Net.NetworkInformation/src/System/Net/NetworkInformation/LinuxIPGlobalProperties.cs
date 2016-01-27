// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal class LinuxIPGlobalProperties : UnixIPGlobalProperties
    {
        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            return StringParsingHelpers.ParseActiveTcpConnectionsFromFiles(NetworkFiles.Tcp4ConnectionsFile, NetworkFiles.Tcp6ConnectionsFile);
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            return StringParsingHelpers.ParseActiveTcpListenersFromFiles(NetworkFiles.Tcp4ConnectionsFile, NetworkFiles.Tcp6ConnectionsFile);
        }

        public override IPEndPoint[] GetActiveUdpListeners()
        {
            return StringParsingHelpers.ParseActiveUdpListenersFromFiles(NetworkFiles.Udp4ConnectionsFile, NetworkFiles.Udp6ConnectionsFile);
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
