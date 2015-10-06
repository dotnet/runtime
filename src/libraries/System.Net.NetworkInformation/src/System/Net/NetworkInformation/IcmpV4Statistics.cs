// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides Internet Control Message Protocol for IPv4 (ICMPv4) statistical data for the local computer.
    public abstract class IcmpV4Statistics
    {
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Reply messages received.
        public abstract long AddressMaskRepliesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Reply messages sent.
        public abstract long AddressMaskRepliesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Request messages received.
        public abstract long AddressMaskRequestsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Request messages sent.
        public abstract long AddressMaskRequestsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) messages received due to a packet having an unreachable address in its destination.
        public abstract long DestinationUnreachableMessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) messages sent because of a packet having an unreachable address in its destination.
        public abstract long DestinationUnreachableMessagesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Reply messages received.
        public abstract long EchoRepliesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Reply messages sent.
        public abstract long EchoRepliesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Request messages received.
        public abstract long EchoRequestsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Request messages sent.
        public abstract long EchoRequestsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) error messages received.
        public abstract long ErrorsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Request messages sent.
        public abstract long ErrorsSent { get; }

        /// Gets the number of Internet Control Message Protocol messages received.
        public abstract long MessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) messages sent.
        public abstract long MessagesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Parameter Problem messages received.
        public abstract long ParameterProblemsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Parameter Problem messages sent.
        public abstract long ParameterProblemsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Redirect messages received.
        public abstract long RedirectsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Redirect messages sent.
        public abstract long RedirectsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Source Quench messages received.
        public abstract long SourceQuenchesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Source Quench messages sent.
        public abstract long SourceQuenchesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Time Exceeded messages received.
        public abstract long TimeExceededMessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Time Exceeded messages sent.
        public abstract long TimeExceededMessagesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Reply messages received.
        public abstract long TimestampRepliesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Reply messages sent.
        public abstract long TimestampRepliesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Request messages received.
        public abstract long TimestampRequestsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Request messages sent.
        public abstract long TimestampRequestsSent { get; }
    }
}
