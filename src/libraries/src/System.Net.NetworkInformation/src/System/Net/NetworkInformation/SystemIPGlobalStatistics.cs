// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    /// IP statistics.
    internal class SystemIPGlobalStatistics : IPGlobalStatistics
    {
        private readonly Interop.IpHlpApi.MibIpStats _stats = new Interop.IpHlpApi.MibIpStats();

        private SystemIPGlobalStatistics() { }

        internal SystemIPGlobalStatistics(AddressFamily family)
        {
            uint result = Interop.IpHlpApi.GetIpStatisticsEx(out _stats, family);

            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new NetworkInformationException((int)result);
            }
        }

        public override bool ForwardingEnabled { get { return _stats.forwardingEnabled; } }

        public override int DefaultTtl { get { return (int)_stats.defaultTtl; } }

        public override long ReceivedPackets { get { return _stats.packetsReceived; } }

        public override long ReceivedPacketsWithHeadersErrors { get { return _stats.receivedPacketsWithHeaderErrors; } }

        public override long ReceivedPacketsWithAddressErrors { get { return _stats.receivedPacketsWithAddressErrors; } }

        public override long ReceivedPacketsForwarded { get { return _stats.packetsForwarded; } }

        public override long ReceivedPacketsWithUnknownProtocol { get { return _stats.receivedPacketsWithUnknownProtocols; } }

        public override long ReceivedPacketsDiscarded { get { return _stats.receivedPacketsDiscarded; } }

        public override long ReceivedPacketsDelivered { get { return _stats.receivedPacketsDelivered; } }

        public override long OutputPacketRequests { get { return _stats.packetOutputRequests; } }

        public override long OutputPacketRoutingDiscards { get { return _stats.outputPacketRoutingDiscards; } }

        public override long OutputPacketsDiscarded { get { return _stats.outputPacketsDiscarded; } }

        public override long OutputPacketsWithNoRoute { get { return _stats.outputPacketsWithNoRoute; } }

        public override long PacketReassemblyTimeout { get { return _stats.packetReassemblyTimeout; } }

        public override long PacketReassembliesRequired { get { return _stats.packetsReassemblyRequired; } }

        public override long PacketsReassembled { get { return _stats.packetsReassembled; } }

        public override long PacketReassemblyFailures { get { return _stats.packetsReassemblyFailed; } }

        public override long PacketsFragmented { get { return _stats.packetsFragmented; } }

        public override long PacketFragmentFailures { get { return _stats.packetsFragmentFailed; } }

        public override int NumberOfInterfaces { get { return (int)_stats.interfaces; } }

        public override int NumberOfIPAddresses { get { return (int)_stats.ipAddresses; } }

        public override int NumberOfRoutes { get { return (int)_stats.routes; } }
    }
}
