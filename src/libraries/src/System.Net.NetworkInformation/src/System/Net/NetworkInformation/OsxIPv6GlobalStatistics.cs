// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;

namespace System.Net.NetworkInformation
{
    internal class OsxIPv6GlobalStatistics : IPGlobalStatistics
    {
        private readonly ulong _outboundPackets;
        private readonly ulong _outputPacketsNoRoute;
        private readonly ulong _cantFrags;
        private readonly ulong _datagramsFragmented;
        private readonly ulong _packetsReassembled;
        private readonly ulong _totalPacketsReceived;
        private readonly ulong _packetsDelivered;
        private readonly ulong _packetsDiscarded;
        private readonly ulong _packetsForwarded;
        private readonly ulong _badAddress;
        private readonly ulong _badHeader;
        private readonly ulong _unknownProtos;
        private readonly ulong _defaultTtl;
        private readonly bool _forwarding;
        private readonly int _numInterfaces;
        private readonly int _numIPAddresses;

        public OsxIPv6GlobalStatistics()
        {
            Interop.Sys.IPv6GlobalStatistics statistics;
            if (Interop.Sys.GetIPv6GlobalStatistics(out statistics) == -1)
            {
                throw new NetworkInformationException((int)Interop.Sys.GetLastError());
            }

            var interfaces = (UnixNetworkInterface[])NetworkInterface.GetAllNetworkInterfaces();
            _numInterfaces = interfaces.Length;
            _numIPAddresses = interfaces.Sum(uni => uni.Addresses.Count);
        }

        public override int DefaultTtl { get { return _defaultTtl; } }

        public override bool ForwardingEnabled { get { return _forwarding; } }

        public override int NumberOfInterfaces { get { return _numInterfaces; } }

        public override int NumberOfIPAddresses { get { return _numIPAddresses; } }

        public override long OutputPacketRequests { get { return _outboundPackets; } }

        public override long OutputPacketRoutingDiscards { get { throw new NotImplementedException(); } }

        public override long OutputPacketsDiscarded { get { throw new NotImplementedException(); } }

        public override long OutputPacketsWithNoRoute { get { return _outputPacketsNoRoute; } }

        public override long PacketFragmentFailures { get { throw new NotImplementedException(); } }

        public override long PacketReassembliesRequired { get { throw new NotImplementedException(); } }

        public override long PacketReassemblyFailures { get { return _cantFrags; } }

        public override long PacketReassemblyTimeout { get { throw new NotImplementedException(); } }

        public override long PacketsFragmented { get { return _datagramsFragmented; } }

        public override long PacketsReassembled { get { return _packetsReassembled; } }

        public override long ReceivedPackets { get { return _totalPacketsReceived; } }

        public override long ReceivedPacketsDelivered { get { return _packetsDelivered; } }

        public override long ReceivedPacketsDiscarded { get { return _packetsDiscarded; } }

        public override long ReceivedPacketsForwarded { get { return _packetsForwarded; } }

        public override long ReceivedPacketsWithAddressErrors { get { return _badAddress; } }

        public override long ReceivedPacketsWithHeadersErrors { get { return _badHeader; } }

        public override long ReceivedPacketsWithUnknownProtocol { get { return _unknownProtos; } }

        public override int NumberOfRoutes { get { throw new NotImplementedException(); } }
    }
}