// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides information about network interfaces that support Internet Protocol (IP) version 4.0.
    public abstract class IPv4InterfaceProperties
    {
        /// Gets a bool value that indicates whether an interface uses Windows Internet Name Service (WINS).
        public abstract bool UsesWins { get; }

        /// Gets a bool value that indicates whether the interface is configured to use a dynamic host configuration protocol (DHCP) server to obtain an IP address.
        public abstract bool IsDhcpEnabled { get; }

        /// Gets a bool value that indicates whether this interface has an automatic private IP addressing (APIPA) address.
        public abstract bool IsAutomaticPrivateAddressingActive { get; }

        /// Gets a bool value that indicates whether this interface has automatic private IP addressing (APIPA) enabled.
        public abstract bool IsAutomaticPrivateAddressingEnabled { get; }

        /// Gets the interface index for the Internet Protocol (IP) address.
        public abstract int Index { get; }

        /// Gets a bool value that indicates whether this interface can route packets.
        public abstract bool IsForwardingEnabled { get; }

        /// Gets the maximum transmission unit (MTU) for this network interface.
        public abstract int Mtu { get; }
    }
}

