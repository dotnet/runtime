// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides User Datagram Protocol (UDP) statistical data.
    public abstract class UdpStatistics
    {
        /// Gets the number of User Datagram Protocol (UDP) datagrams received.
        public abstract long DatagramsReceived { get; }

        /// Gets the number of User Datagram Protocol (UDP) datagrams sent.
        public abstract long DatagramsSent { get; }

        /// Gets the number of User Datagram Protocol (UDP) datagrams received and discarded due to port errors.
        public abstract long IncomingDatagramsDiscarded { get; }

        /// Gets the number of User Datagram Protocol (UDP) datagrams received and discarded due to errors other than bad port information.
        public abstract long IncomingDatagramsWithErrors { get; }

        /// Gets the number of local endpoints listening for User Datagram Protocol (UDP) datagrams.
        public abstract int UdpListeners { get; }
    }
}
