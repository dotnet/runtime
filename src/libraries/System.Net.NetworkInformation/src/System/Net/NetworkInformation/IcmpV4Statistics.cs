// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides Internet Control Message Protocol for IPv4 (ICMPv4) statistical data for the local computer.
    /// </summary>
    public abstract class IcmpV4Statistics
    {
        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Reply messages received.
        /// </summary>
        public abstract long AddressMaskRepliesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Reply messages sent.
        /// </summary>
        public abstract long AddressMaskRepliesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Request messages received.
        /// </summary>
        public abstract long AddressMaskRequestsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Address Mask Request messages sent.
        /// </summary>
        public abstract long AddressMaskRequestsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) messages received due to a packet having an unreachable address in its destination.
        /// </summary>
        public abstract long DestinationUnreachableMessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) messages sent because of a packet having an unreachable address in its destination.
        /// </summary>
        public abstract long DestinationUnreachableMessagesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Reply messages received.
        /// </summary>
        public abstract long EchoRepliesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Reply messages sent.
        /// </summary>
        public abstract long EchoRepliesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Request messages received.
        /// </summary>
        public abstract long EchoRequestsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Request messages sent.
        /// </summary>
        public abstract long EchoRequestsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) error messages received.
        /// </summary>
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public abstract long ErrorsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Echo Request messages sent.
        /// </summary>
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public abstract long ErrorsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol messages received.
        /// </summary>
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public abstract long MessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) messages sent.
        /// </summary>
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public abstract long MessagesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Parameter Problem messages received.
        /// </summary>
        public abstract long ParameterProblemsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Parameter Problem messages sent.
        /// </summary>
        public abstract long ParameterProblemsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Redirect messages received.
        /// </summary>
        public abstract long RedirectsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Redirect messages sent.
        /// </summary>
        public abstract long RedirectsSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Source Quench messages received.
        /// </summary>
        public abstract long SourceQuenchesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Source Quench messages sent.
        /// </summary>
        public abstract long SourceQuenchesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Time Exceeded messages received.
        /// </summary>
        public abstract long TimeExceededMessagesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Time Exceeded messages sent.
        /// </summary>
        public abstract long TimeExceededMessagesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Reply messages received.
        /// </summary>
        public abstract long TimestampRepliesReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Reply messages sent.
        /// </summary>
        public abstract long TimestampRepliesSent { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Request messages received.
        /// </summary>
        public abstract long TimestampRequestsReceived { get; }

        /// <summary>
        /// Gets the number of Internet Control Message Protocol version 4 (ICMPv4) Timestamp Request messages sent.
        /// </summary>
        public abstract long TimestampRequestsSent { get; }
    }
}
