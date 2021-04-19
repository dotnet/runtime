// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal sealed class LinuxIPGlobalStatistics : IPGlobalStatistics
    {
        // MIB-II statistics data.
        private readonly IPGlobalStatisticsTable _table;

        // Miscellaneous IP information, not defined in MIB-II.
        private int _numRoutes;
        private int _numInterfaces;
        private int _numIPAddresses;

        public LinuxIPGlobalStatistics(bool ipv4)
        {
            if (ipv4)
            {
                _table = StringParsingHelpers.ParseIPv4GlobalStatisticsFromSnmpFile(NetworkFiles.SnmpV4StatsFile);
                _numRoutes = StringParsingHelpers.ParseNumRoutesFromRouteFile(NetworkFiles.Ipv4RouteFile);
                _numInterfaces = StringParsingHelpers.ParseNumIPInterfaces(NetworkFiles.Ipv4ConfigFolder);
            }
            else
            {
                _table = StringParsingHelpers.ParseIPv6GlobalStatisticsFromSnmp6File(NetworkFiles.SnmpV6StatsFile);
                _numRoutes = StringParsingHelpers.ParseNumRoutesFromRouteFile(NetworkFiles.Ipv6RouteFile);
                _numInterfaces = StringParsingHelpers.ParseNumIPInterfaces(NetworkFiles.Ipv6ConfigFolder);

                // /proc/sys/net/ipv6/conf/default/forwarding
                string forwardingConfigFile = Path.Combine(NetworkFiles.Ipv6ConfigFolder,
                                                NetworkFiles.DefaultNetworkInterfaceFileName,
                                                NetworkFiles.ForwardingFileName);
                _table.Forwarding = StringParsingHelpers.ParseRawIntFile(forwardingConfigFile) == 1;

                // snmp6 does not include Default TTL info. Read it from snmp.
                _table.DefaultTtl = StringParsingHelpers.ParseDefaultTtlFromFile(NetworkFiles.SnmpV4StatsFile);
            }

            _numIPAddresses = GetNumIPAddresses();
        }

        public override int DefaultTtl { get { return _table.DefaultTtl; } }

        public override bool ForwardingEnabled { get { return _table.Forwarding; } }

        public override int NumberOfInterfaces { get { return _numInterfaces; } }

        public override int NumberOfIPAddresses { get { return _numIPAddresses; } }

        public override int NumberOfRoutes { get { return _numRoutes; } }

        public override long OutputPacketRequests { get { return _table.OutRequests; } }

        public override long OutputPacketRoutingDiscards { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long OutputPacketsDiscarded { get { return _table.OutDiscards; } }

        public override long OutputPacketsWithNoRoute { get { return _table.OutNoRoutes; } }

        public override long PacketFragmentFailures { get { return _table.FragmentFails; } }

        public override long PacketReassembliesRequired { get { return _table.ReassemblyRequireds; } }

        public override long PacketReassemblyFailures { get { return _table.ReassemblyFails; } }

        public override long PacketReassemblyTimeout { get { return _table.ReassemblyTimeout; } }

        public override long PacketsFragmented { get { return _table.FragmentCreates; } }

        public override long PacketsReassembled { get { return _table.ReassemblyOKs; } }

        public override long ReceivedPackets { get { return _table.InReceives; } }

        public override long ReceivedPacketsDelivered { get { return _table.InDelivers; } }

        public override long ReceivedPacketsDiscarded { get { return _table.InDiscards; } }

        public override long ReceivedPacketsForwarded { get { return _table.ForwardedDatagrams; } }

        public override long ReceivedPacketsWithAddressErrors { get { return _table.InAddressErrors; } }

        public override long ReceivedPacketsWithHeadersErrors { get { return _table.InHeaderErrors; } }

        public override long ReceivedPacketsWithUnknownProtocol { get { return _table.InUnknownProtocols; } }

        private static unsafe int GetNumIPAddresses()
        {
            int count = 0;
            Interop.Sys.EnumerateInterfaceAddresses(
                (name, ipAddressInfo) =>
                {
                    count++;
                },
                (name, ipAddressInfo, scopeId) =>
                {
                    count++;
                },
                // Ignore link-layer addresses that are discovered; don't create a callback.
                null);

            return count;
        }
    }
}
