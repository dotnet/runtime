// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides statistical data for a network interface on the local computer.
    /// </summary>
    public abstract partial class IPv4InterfaceStatistics
    {
        protected IPv4InterfaceStatistics() { }
        public abstract long BytesReceived { get; }
        public abstract long BytesSent { get; }
        public abstract long IncomingPacketsDiscarded { get; }
        public abstract long IncomingPacketsWithErrors { get; }
        public abstract long IncomingUnknownProtocolPackets { get; }
        public abstract long NonUnicastPacketsReceived { get; }
        public abstract long NonUnicastPacketsSent { get; }
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public abstract long OutgoingPacketsDiscarded { get; }
        public abstract long OutgoingPacketsWithErrors { get; }
        public abstract long OutputQueueLength { get; }
        public abstract long UnicastPacketsReceived { get; }
        public abstract long UnicastPacketsSent { get; }
    }
}
