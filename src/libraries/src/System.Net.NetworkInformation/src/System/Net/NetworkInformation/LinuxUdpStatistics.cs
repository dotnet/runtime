// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal class LinuxUdpStatistics : UdpStatistics
    {
        private readonly UdpGlobalStatisticsTable _table;
        private readonly int _udpListeners;

        public LinuxUdpStatistics(bool ipv4)
        {
            if (ipv4)
            {
                _table = StringParsingHelpers.ParseUdpv4GlobalStatisticsFromSnmpFile(NetworkFiles.SnmpV4StatsFile);
                _udpListeners = StringParsingHelpers.ParseNumSocketConnections(NetworkFiles.SockstatFile, "UDP");
            }
            else
            {
                _table = StringParsingHelpers.ParseUdpv6GlobalStatisticsFromSnmp6File(NetworkFiles.SnmpV6StatsFile);
                _udpListeners = StringParsingHelpers.ParseNumSocketConnections(NetworkFiles.Sockstat6File, "UDP6");
            }
        }

        public override long DatagramsReceived { get { return _table.InDatagrams; } }

        public override long DatagramsSent { get { return _table.OutDatagrams; } }

        public override long IncomingDatagramsDiscarded { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long IncomingDatagramsWithErrors { get { return _table.InErrors; } }

        public override int UdpListeners { get { return _udpListeners; } }
    }
}
