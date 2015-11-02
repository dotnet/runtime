// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides User Datagram Protocol (UDP) statistical data.
    /// </summary>
    public abstract class UdpStatistics
    {
        /// <summary>
        /// Gets the number of User Datagram Protocol (UDP) datagrams received.
        /// </summary>
        public abstract long DatagramsReceived { get; }

        /// <summary>
        /// Gets the number of User Datagram Protocol (UDP) datagrams sent.
        /// </summary>
        public abstract long DatagramsSent { get; }

        /// <summary>
        /// Gets the number of User Datagram Protocol (UDP) datagrams received and discarded due to port errors.
        /// </summary>
        public abstract long IncomingDatagramsDiscarded { get; }

        /// <summary>
        /// Gets the number of User Datagram Protocol (UDP) datagrams received and discarded due to errors other than bad port information.
        /// </summary>
        public abstract long IncomingDatagramsWithErrors { get; }

        /// <summary>
        /// Gets the number of local endpoints listening for User Datagram Protocol (UDP) datagrams.
        /// </summary>
        public abstract int UdpListeners { get; }
    }
}
