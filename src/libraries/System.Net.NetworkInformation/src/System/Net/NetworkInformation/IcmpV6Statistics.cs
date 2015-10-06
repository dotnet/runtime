// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides Internet Control Message Protocol for Internet Protocol version 6 (ICMPv6) statistical data for the local computer.
    public abstract class IcmpV6Statistics
    {
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages received because of a packet having an unreachable address in its destination.
        public abstract long DestinationUnreachableMessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages sent because of a packet having an unreachable address in its destination.
        public abstract long DestinationUnreachableMessagesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Reply messages received.
        public abstract long EchoRepliesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Reply messages sent.
        public abstract long EchoRepliesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Request messages received.
        public abstract long EchoRequestsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Request messages sent.
        public abstract long EchoRequestsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) error messages received.
        public abstract long ErrorsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) error messages sent.
        public abstract long ErrorsSent { get; }

        /// Gets the number of Internet Group management Protocol (IGMP) Group Membership Query messages received.
        public abstract long MembershipQueriesReceived { get; }

        /// Gets the number of Internet Group management Protocol (IGMP) Group Membership Query messages sent.
        public abstract long MembershipQueriesSent { get; }

        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Reduction messages received.
        public abstract long MembershipReductionsReceived { get; }

        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Reduction messages sent.
        public abstract long MembershipReductionsSent { get; }

        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Report messages received.
        public abstract long MembershipReportsReceived { get; }

        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Report messages sent.
        public abstract long MembershipReportsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages received.
        public abstract long MessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages sent.
        public abstract long MessagesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Advertisement messages received.
        public abstract long NeighborAdvertisementsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Advertisement messages sent.
        public abstract long NeighborAdvertisementsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Solicitation messages received.
        public abstract long NeighborSolicitsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Solicitation messages sent.
        public abstract long NeighborSolicitsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Packet Too Big messages received.
        public abstract long PacketTooBigMessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Packet Too Big messages sent.
        public abstract long PacketTooBigMessagesSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Parameter Problem messages received.
        public abstract long ParameterProblemsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Parameter Problem messages sent.
        public abstract long ParameterProblemsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Redirect messages received.
        public abstract long RedirectsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Redirect messages sent.
        public abstract long RedirectsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Advertisement messages received.
        public abstract long RouterAdvertisementsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Advertisement messages sent.
        public abstract long RouterAdvertisementsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Solicitation messages received.
        public abstract long RouterSolicitsReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Solicitation messages sent.
        public abstract long RouterSolicitsSent { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Time Exceeded messages received.
        public abstract long TimeExceededMessagesReceived { get; }

        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Time Exceeded messages sent.
        public abstract long TimeExceededMessagesSent { get; }
    }
}
