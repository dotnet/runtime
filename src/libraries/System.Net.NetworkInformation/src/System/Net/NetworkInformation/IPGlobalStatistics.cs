// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides Internet Protocol (IP) statistical data for the local machine.
    public abstract class IPGlobalStatistics
    {
        /// Gets the default time-to-live (TTL) value for Internet Protocol (IP) packets.
        public abstract int DefaultTtl { get; }

        /// Gets a bool value that specifies whether Internet Protocol (IP) packet forwarding is enabled.
        public abstract bool ForwardingEnabled { get; }

        /// Gets the number of network interfaces.
        public abstract int NumberOfInterfaces { get; }

        /// Gets the number of Internet Protocol (IP) addresses assigned to the local computer.
        public abstract int NumberOfIPAddresses { get; }

        /// Gets the number of outbound Internet Protocol (IP) packets.
        public abstract long OutputPacketRequests { get; }

        /// Gets the number of routes in the routing table that have been discarded.
        public abstract long OutputPacketRoutingDiscards { get; }

        /// Gets the number of transmitted Internet Protocol (IP) packets that have been discarded.
        public abstract long OutputPacketsDiscarded { get; }

        /// Gets the number of Internet Protocol (IP) packets for which the local computer could not determine a route to the destination address.
        public abstract long OutputPacketsWithNoRoute { get; }

        /// Gets the number of Internet Protocol (IP) packets that could not be fragmented.
        public abstract long PacketFragmentFailures { get; }

        /// Gets the number of Internet Protocol (IP) packets that required reassembly.
        public abstract long PacketReassembliesRequired { get; }

        /// Gets the number of Internet Protocol (IP) packets that were not successfully reassembled.
        public abstract long PacketReassemblyFailures { get; }

        /// Gets the maximum amount of time within which all fragments of an Internet Protocol (IP) packet must arrive. 
        public abstract long PacketReassemblyTimeout { get; }

        /// Gets the number of Internet Protocol (IP) packets fragmented.
        public abstract long PacketsFragmented { get; }

        /// Gets the number of Internet Protocol (IP) packets reassembled.
        public abstract long PacketsReassembled { get; }

        /// Gets the number of Internet Protocol (IP) packets received.
        public abstract long ReceivedPackets { get; }

        // Gets the number of Internet Protocol(IP) packets received and delivered.
        public abstract long ReceivedPacketsDelivered { get; }

        /// Gets the number of Internet Protocol (IP) packets that have been received and discarded.
        public abstract long ReceivedPacketsDiscarded { get; }

        /// Gets the number of Internet Protocol (IP) packets forwarded.
        public abstract long ReceivedPacketsForwarded { get; }

        /// Gets the number of Internet Protocol (IP) packets with address errors that were received.
        public abstract long ReceivedPacketsWithAddressErrors { get; }

        /// Gets the number of Internet Protocol (IP) packets with header errors that were received.
        public abstract long ReceivedPacketsWithHeadersErrors { get; }

        /// Gets the number of Internet Protocol (IP) packets received on the local machine with an unknown protocol in the header.
        public abstract long ReceivedPacketsWithUnknownProtocol { get; }

        /// Gets the number of routes in the Internet Protocol (IP) routing table.
        public abstract int NumberOfRoutes { get; }
    }
}
