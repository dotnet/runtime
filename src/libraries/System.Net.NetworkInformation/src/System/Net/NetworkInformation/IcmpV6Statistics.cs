// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides Internet Control Message Protocol for Internet Protocol version 6 (ICMPv6) statistical data for the local computer.
    /// </summary>
    public abstract class IcmpV6Statistics
    {
        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages received because of a packet having an unreachable address in its destination.
        /// </summary>
        public abstract long DestinationUnreachableMessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages sent because of a packet having an unreachable address in its destination.
        /// </summary>
        public abstract long DestinationUnreachableMessagesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Reply messages received.
        /// </summary>
        public abstract long EchoRepliesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Reply messages sent.
        /// </summary>
        public abstract long EchoRepliesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Request messages received.
        /// </summary>
        public abstract long EchoRequestsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Echo Request messages sent.
        /// </summary>
        public abstract long EchoRequestsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) error messages received.
        /// </summary>
        public abstract long ErrorsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) error messages sent.
        /// </summary>
        public abstract long ErrorsSent { get; }

        /// <summary>
        /// Gets the number of Internet Group management Protocol (IGMP) Group Membership Query messages received.
        /// </summary>
        public abstract long MembershipQueriesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Group management Protocol (IGMP) Group Membership Query messages sent.
        /// </summary>
        public abstract long MembershipQueriesSent { get; }

        /// <summary>
        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Reduction messages received.
        /// </summary>
        public abstract long MembershipReductionsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Reduction messages sent.
        /// </summary>
        public abstract long MembershipReductionsSent { get; }

        /// <summary>
        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Report messages received.
        /// </summary>
        public abstract long MembershipReportsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Group Management Protocol (IGMP) Group Membership Report messages sent.
        /// </summary>
        public abstract long MembershipReportsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages received.
        /// </summary>
        public abstract long MessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) messages sent.
        /// </summary>
        public abstract long MessagesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Advertisement messages received.
        /// </summary>
        public abstract long NeighborAdvertisementsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Advertisement messages sent.
        /// </summary>
        public abstract long NeighborAdvertisementsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Solicitation messages received.
        /// </summary>
        public abstract long NeighborSolicitsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Neighbor Solicitation messages sent.
        /// </summary>
        public abstract long NeighborSolicitsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Packet Too Big messages received.
        /// </summary>
        public abstract long PacketTooBigMessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Packet Too Big messages sent.
        /// </summary>
        public abstract long PacketTooBigMessagesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Parameter Problem messages received.
        /// </summary>
        public abstract long ParameterProblemsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Parameter Problem messages sent.
        /// </summary>
        public abstract long ParameterProblemsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Redirect messages received.
        /// </summary>
        public abstract long RedirectsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Redirect messages sent.
        /// </summary>
        public abstract long RedirectsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Advertisement messages received.
        /// </summary>
        public abstract long RouterAdvertisementsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Advertisement messages sent.
        /// </summary>
        public abstract long RouterAdvertisementsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Solicitation messages received.
        /// </summary>
        public abstract long RouterSolicitsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Router Solicitation messages sent.
        /// </summary>
        public abstract long RouterSolicitsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Time Exceeded messages received.
        /// </summary>
        public abstract long TimeExceededMessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 6 (ICMPv6) Time Exceeded messages sent.
        /// </summary>
        public abstract long TimeExceededMessagesSent { get; }
    }
}
