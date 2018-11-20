// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Net.NetworkInformation
{
    internal class BsdIPv4GlobalStatistics : IPGlobalStatistics
    {
        private readonly long _outboundPackets;
        private readonly long _outputPacketsNoRoute;
        private readonly long _cantFrags;
        private readonly long _datagramsFragmented;
        private readonly long _packetsReassembled;
        private readonly long _totalPacketsReceived;
        private readonly long _packetsDelivered;
        private readonly long _packetsDiscarded;
        private readonly long _packetsForwarded;
        private readonly long _badAddress;
        private readonly long _badHeader;
        private readonly long _unknownProtos;
        private readonly int _defaultTtl;
        private readonly bool _forwarding;
        private readonly int _numInterfaces;
        private readonly int _numIPAddresses;
        private readonly int _numRoutes;

        public unsafe BsdIPv4GlobalStatistics()
        {
            Interop.Sys.IPv4GlobalStatistics statistics;
            if (Interop.Sys.GetIPv4GlobalStatistics(out statistics) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            _outboundPackets = (long)statistics.OutboundPackets;
            _outputPacketsNoRoute = (long)statistics.OutputPacketsNoRoute;
            _cantFrags = (long)statistics.CantFrags;
            _datagramsFragmented = (long)statistics.DatagramsFragmented;
            _packetsReassembled = (long)statistics.PacketsReassembled;
            _totalPacketsReceived = (long)statistics.TotalPacketsReceived;
            _packetsDelivered = (long)statistics.PacketsDelivered;
            _packetsDiscarded = (long)statistics.PacketsDiscarded;
            _packetsForwarded = (long)statistics.PacketsForwarded;
            _badAddress = (long)statistics.BadAddress;
            _badHeader = (long)statistics.BadHeader;
            _unknownProtos = (long)statistics.UnknownProtos;
            _defaultTtl = statistics.DefaultTtl;
            _forwarding = statistics.Forwarding == 1;

            HashSet<string> interfaceSet = new HashSet<string>();
            int numIPAddresses = 0;
            Interop.Sys.EnumerateInterfaceAddresses(
                (name, addressInfo, netmaskInfo) =>
                {
                    interfaceSet.Add(name);
                    numIPAddresses++;
                },
                null,
                null);

            _numInterfaces = interfaceSet.Count;
            _numIPAddresses = numIPAddresses;

            _numRoutes = Interop.Sys.GetNumRoutes();
            if (_numRoutes == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }
        }

        public override int DefaultTtl { get { return _defaultTtl; } }

        public override bool ForwardingEnabled { get { return _forwarding; } }

        public override int NumberOfInterfaces { get { return _numInterfaces; } }

        public override int NumberOfIPAddresses { get { return _numIPAddresses; } }

        public override long OutputPacketRequests { get { return _outboundPackets; } }

        public override long OutputPacketRoutingDiscards { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long OutputPacketsDiscarded { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long OutputPacketsWithNoRoute { get { return _outputPacketsNoRoute; } }

        public override long PacketFragmentFailures { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long PacketReassembliesRequired { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long PacketReassemblyFailures { get { return _cantFrags; } }

        public override long PacketReassemblyTimeout { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); ; } }

        public override long PacketsFragmented { get { return _datagramsFragmented; } }

        public override long PacketsReassembled { get { return _packetsReassembled; } }

        public override long ReceivedPackets { get { return _totalPacketsReceived; } }

        public override long ReceivedPacketsDelivered { get { return _packetsDelivered; } }

        public override long ReceivedPacketsDiscarded { get { return _packetsDiscarded; } }

        public override long ReceivedPacketsForwarded { get { return _packetsForwarded; } }

        public override long ReceivedPacketsWithAddressErrors { get { return _badAddress; } }

        public override long ReceivedPacketsWithHeadersErrors { get { return _badHeader; } }

        public override long ReceivedPacketsWithUnknownProtocol { get { return _unknownProtos; } }

        public override int NumberOfRoutes { get { return _numRoutes; } }
    }
}
