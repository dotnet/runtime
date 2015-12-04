// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about a network interface that supports Internet Protocol (IP).
    /// such as the addresses assigned to the adapter, and other DNS and routing-related information.
    /// </summary>
    public abstract class IPInterfaceProperties
    {
        /// <summary>
        /// Gets a bool value that indicates whether this interface is configured to send name resolution queries to a Domain Name System (DNS) server.
        /// </summary>
        public abstract bool IsDnsEnabled { get; }

        /// <summary>
        /// Gets the Domain Name System (DNS) suffix associated with this interface.
        /// </summary>
        public abstract string DnsSuffix { get; }

        /// <summary>
        /// Gets a bool value that indicates whether this interface is configured to automatically register its IP address information with the Domain Name System (DNS).
        /// </summary>
        public abstract bool IsDynamicDnsEnabled { get; }

        /// <summary>
        /// The address is that of a Domain Name Service (DNS) server for the local computer.
        /// </summary>
        public abstract IPAddressCollection DnsAddresses { get; }

        /// <summary>
        /// Gets the addresses for Dynamic Host Configuration Protocol (DHCP) servers.
        /// </summary>
        public abstract IPAddressCollection DhcpServerAddresses { get; }

        /// <summary>
        /// Gets the list of Wins Servers registered with this interface
        /// </summary>
        public abstract IPAddressCollection WinsServersAddresses { get; }
    }
}
