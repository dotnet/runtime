// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    public abstract class IPInterfaceStatistics
    {
        /// Gets the number of bytes received on the interface.
        public abstract long BytesReceived { get; }

        /// Gets the number of bytes sent on the interface.
        public abstract long BytesSent { get; }

        /// Gets the number of incoming packets discarded.
        public abstract long IncomingPacketsDiscarded { get; }

        /// Gets the number of incoming packets with errors.
        public abstract long IncomingPacketsWithErrors { get; }

        /// Gets the number of incoming packets with an unknown protocol.
        public abstract long IncomingUnknownProtocolPackets { get; }

        /// Gets the number of non-unicast packets received on the interface.
        public abstract long NonUnicastPacketsReceived { get; }

        /// Gets the number of non-unicast packets sent on the interface.
        public abstract long NonUnicastPacketsSent { get; }

        /// Gets the number of outgoing packets that were discarded.
        public abstract long OutgoingPacketsDiscarded { get; }

        /// Gets the number of outgoing packets with errors.
        public abstract long OutgoingPacketsWithErrors { get; }

        /// Gets the length of the output queue.
        public abstract long OutputQueueLength { get; }

        /// Gets the number of unicast packets received on the interface.
        public abstract long UnicastPacketsReceived { get; }

        /// Gets the number of unicast packets sent on the interface.
        public abstract long UnicastPacketsSent { get; }
    }
}
