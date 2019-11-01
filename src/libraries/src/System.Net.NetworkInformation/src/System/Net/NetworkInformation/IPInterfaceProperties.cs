// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// The address identifies a single computer. Packets sent to a unicast address are sent to the computer identified by the address.
        /// </summary>
        public abstract UnicastIPAddressInformationCollection UnicastAddresses { get; }

        /// <summary>
        /// The address identifies multiple computers. Packets sent to a multicast address are sent to all computers identified by the address.
        /// </summary>
        public abstract MulticastIPAddressInformationCollection MulticastAddresses { get; }

        /// <summary>
        /// The address identifies multiple computers. Packets sent to an anycast address are sent to one of the computers identified by the address.
        /// </summary>
        public abstract IPAddressInformationCollection AnycastAddresses { get; }

        /// <summary>
        /// The address is that of a Domain Name Service (DNS) server for the local computer.
        /// </summary>
        public abstract IPAddressCollection DnsAddresses { get; }

        /// <summary>
        /// Gets the network gateway addresses.
        /// </summary>
        public abstract GatewayIPAddressInformationCollection GatewayAddresses { get; }

        /// <summary>
        /// Gets the addresses for Dynamic Host Configuration Protocol (DHCP) servers.
        /// </summary>
        public abstract IPAddressCollection DhcpServerAddresses { get; }

        /// <summary>
        /// Gets the list of Wins Servers registered with this interface
        /// </summary>
        public abstract IPAddressCollection WinsServersAddresses { get; }

        /// <summary>
        /// Gets the IP version 4.0 specific properties for this network interface.
        /// </summary>
        /// <returns>The interface' IPv4-specific properties.</returns>
        public abstract IPv4InterfaceProperties GetIPv4Properties();

        /// <summary>
        /// Gets the IP version 6.0 specific properties for this network interface.
        /// </summary>
        /// <returns>The interface' IPv6-specific properties.</returns>
        public abstract IPv6InterfaceProperties GetIPv6Properties();
    }
}
