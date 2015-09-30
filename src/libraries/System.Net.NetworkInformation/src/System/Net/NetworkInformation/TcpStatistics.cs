// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides Transmission Control Protocol (TCP) statistical data.
    public abstract class TcpStatistics
    {
        /// Gets the number of accepted Transmission Control Protocol (TCP) connection requests.
        public abstract long ConnectionsAccepted { get; }

        /// Gets the number of Transmission Control Protocol (TCP) connection requests made by clients.
        public abstract long ConnectionsInitiated { get; }

        /// Gets the total number of Transmission Control Protocol (TCP) connections established.
        public abstract long CumulativeConnections { get; }

        /// Gets the number of current Transmission Control Protocol (TCP) connections.
        public abstract long CurrentConnections { get; }

        /// Gets the number of Transmission Control Protocol (TCP) errors received.
        public abstract long ErrorsReceived { get; }

        /// Gets the number of failed Transmission Control Protocol (TCP) connection attempts.
        public abstract long FailedConnectionAttempts { get; }

        /// Gets the maximum number of supported Transmission Control Protocol (TCP) connections.
        public abstract long MaximumConnections { get; }

        /// Gets the maximum retransmission time-out value for Transmission Control Protocol (TCP) segments.
        public abstract long MaximumTransmissionTimeout { get; }

        /// Gets the minimum retransmission time-out value for Transmission Control Protocol (TCP) segments.
        public abstract long MinimumTransmissionTimeout { get; }

        /// Gets the number of RST packets recived by Transmission Control Protocol (TCP) connections.
        public abstract long ResetConnections { get; }

        /// Gets the number of Transmission Control Protocol (TCP) segments received.
        public abstract long SegmentsReceived { get; }

        /// Gets the number of Transmission Control Protocol (TCP) segments re-sent.
        public abstract long SegmentsResent { get; }

        /// Gets the number of Transmission Control Protocol (TCP) segments sent.
        public abstract long SegmentsSent { get; }

        /// Gets the number of Transmission Control Protocol (TCP) segments sent with the reset flag set.
        public abstract long ResetsSent { get; }
    }
}
