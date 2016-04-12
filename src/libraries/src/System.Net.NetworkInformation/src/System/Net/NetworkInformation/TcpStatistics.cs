// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides Transmission Control Protocol (TCP) statistical data.
    /// </summary>
    public abstract class TcpStatistics
    {
        /// <summary>
        /// Gets the number of accepted Transmission Control Protocol (TCP) connection requests.
        /// </summary>
        public abstract long ConnectionsAccepted { get; }

        /// <summary>
        /// Gets the number of Transmission Control Protocol (TCP) connection requests made by clients.
        /// </summary>
        public abstract long ConnectionsInitiated { get; }

        /// <summary>
        /// Gets the total number of Transmission Control Protocol (TCP) connections established.
        /// </summary>
        public abstract long CumulativeConnections { get; }

        /// <summary>
        /// Gets the number of current Transmission Control Protocol (TCP) connections.
        /// </summary>
        public abstract long CurrentConnections { get; }

        /// <summary>
        /// Gets the number of Transmission Control Protocol (TCP) errors received.
        /// </summary>
        public abstract long ErrorsReceived { get; }

        /// <summary>
        /// Gets the number of failed Transmission Control Protocol (TCP) connection attempts.
        /// </summary>
        public abstract long FailedConnectionAttempts { get; }

        /// <summary>
        /// Gets the maximum number of supported Transmission Control Protocol (TCP) connections.
        /// </summary>
        public abstract long MaximumConnections { get; }

        /// <summary>
        /// Gets the maximum retransmission time-out value for Transmission Control Protocol (TCP) segments.
        /// </summary>
        public abstract long MaximumTransmissionTimeout { get; }

        /// <summary>
        /// Gets the minimum retransmission time-out value for Transmission Control Protocol (TCP) segments.
        /// </summary>
        public abstract long MinimumTransmissionTimeout { get; }

        /// <summary>
        /// Gets the number of RST packets received by Transmission Control Protocol (TCP) connections.
        /// </summary>
        public abstract long ResetConnections { get; }

        /// <summary>
        /// Gets the number of Transmission Control Protocol (TCP) segments received.
        /// </summary>
        public abstract long SegmentsReceived { get; }

        /// <summary>
        /// Gets the number of Transmission Control Protocol (TCP) segments received.
        /// </summary>
        public abstract long SegmentsResent { get; }

        /// <summary>
        /// Gets the number of Transmission Control Protocol (TCP) segments sent.
        /// </summary>
        public abstract long SegmentsSent { get; }

        /// <summary>
        /// Gets the number of Transmission Control Protocol (TCP) segments sent with the reset flag set.
        /// </summary>
        public abstract long ResetsSent { get; }
    }
}
