// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides Internet Protocol (IP) statistical data for the local machine.
    /// </summary>
    public abstract class IPGlobalStatistics
    {
        /// <summary>
        /// Gets the default time-to-live (TTL) value for Internet Protocol (IP) packets.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract int DefaultTtl { get; }

        /// <summary>
        /// Gets a bool value that specifies whether Internet Protocol (IP) packet forwarding is enabled.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract bool ForwardingEnabled { get; }

        /// <summary>
        /// Gets the number of network interfaces.
        /// </summary>
        public abstract int NumberOfInterfaces { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) addresses assigned to the local computer.
        /// </summary>
        public abstract int NumberOfIPAddresses { get; }

        /// <summary>
        /// Gets the number of outbound Internet Protocol (IP) packets.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long OutputPacketRequests { get; }

        /// <summary>
        /// Gets the number of routes in the routing table that have been discarded.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long OutputPacketRoutingDiscards { get; }

        /// <summary>
        /// Gets the number of transmitted Internet Protocol (IP) packets that have been discarded.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long OutputPacketsDiscarded { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets for which the local computer could not determine a route to the destination address.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long OutputPacketsWithNoRoute { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets that could not be fragmented.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long PacketFragmentFailures { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets that required reassembly.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long PacketReassembliesRequired { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets that were not successfully reassembled.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long PacketReassemblyFailures { get; }

        /// <summary>
        /// Gets the maximum amount of time within which all fragments of an Internet Protocol (IP) packet must arrive.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long PacketReassemblyTimeout { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets fragmented.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long PacketsFragmented { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets reassembled.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long PacketsReassembled { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets received.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPackets { get; }

        /// <summary>
        /// Gets the number of Internet Protocol(IP) packets received and delivered.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPacketsDelivered { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets that have been received and discarded.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPacketsDiscarded { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets forwarded.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPacketsForwarded { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets with address errors that were received.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPacketsWithAddressErrors { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets with header errors that were received.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPacketsWithHeadersErrors { get; }

        /// <summary>
        /// Gets the number of Internet Protocol (IP) packets received on the local machine with an unknown protocol in the header.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract long ReceivedPacketsWithUnknownProtocol { get; }

        /// <summary>
        /// Gets the number of routes in the Internet Protocol (IP) routing table.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract int NumberOfRoutes { get; }
    }
}
