// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about network interfaces that support Internet Protocol (IP) version 4.0.
    /// </summary>
    public abstract class IPv4InterfaceProperties
    {
        /// <summary>
        /// Gets a bool value that indicates whether an interface uses Windows Internet Name Service (WINS).
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        public abstract bool UsesWins { get; }

        /// <summary>
        /// Gets a bool value that indicates whether the interface is configured to use a dynamic host configuration protocol (DHCP) server to obtain an IP address.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract bool IsDhcpEnabled { get; }

        /// <summary>
        /// Gets a bool value that indicates whether this interface has an automatic private IP addressing (APIPA) address.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract bool IsAutomaticPrivateAddressingActive { get; }

        /// <summary>
        /// Gets a bool value that indicates whether this interface has automatic private IP addressing (APIPA) enabled.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public abstract bool IsAutomaticPrivateAddressingEnabled { get; }

        /// <summary>
        /// Gets the interface index for the Internet Protocol (IP) address.
        /// </summary>
        public abstract int Index { get; }

        /// <summary>
        /// Gets a bool value that indicates whether this interface can route packets.
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        public abstract bool IsForwardingEnabled { get; }

        /// <summary>
        /// Gets the maximum transmission unit (MTU) for this network interface.
        /// </summary>
        public abstract int Mtu { get; }
    }
}
