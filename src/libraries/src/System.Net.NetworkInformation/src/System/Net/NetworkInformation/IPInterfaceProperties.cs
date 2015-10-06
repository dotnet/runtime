// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    public abstract class IPInterfaceProperties
    {
        /// Gets a bool value that indicates whether this interface is configured to send name resolution queries to a Domain Name System (DNS) server.
        public abstract bool IsDnsEnabled { get; }

        /// Gets the Domain Name System (DNS) suffix associated with this interface.
        public abstract string DnsSuffix { get; }

        /// Gets a bool value that indicates whether this interface is configured to automatically register its IP address information with the Domain Name System (DNS).
        public abstract bool IsDynamicDnsEnabled { get; }

        /// The address identifies a single computer. Packets sent to a unicast address are sent to the computer identified by the address.
        public abstract UnicastIPAddressInformationCollection UnicastAddresses { get; }

        /// The address identifies multiple computers. Packets sent to a multicast address are sent to all computers identified by the address.
        public abstract MulticastIPAddressInformationCollection MulticastAddresses { get; }

        /// The address identifies multiple computers. Packets sent to an anycast address are sent to one of the computers identified by the address.
        public abstract IPAddressInformationCollection AnycastAddresses { get; }

        /// The address is that of a Domain Name Service (DNS) server for the local computer.
        public abstract IPAddressCollection DnsAddresses { get; }

        /// Gets the network gateway addresses.
        public abstract GatewayIPAddressInformationCollection GatewayAddresses { get; }

        /// Gets the addresses for Dynamic Host Configuration Protocol (DHCP) servers.
        public abstract IPAddressCollection DhcpServerAddresses { get; }

        /// Gets the list of Wins Servers registered with this interface
        public abstract IPAddressCollection WinsServersAddresses { get; }

        /// Gets the IP version 4.0 specific properties for this network interface.
        public abstract IPv4InterfaceProperties GetIPv4Properties();

        public abstract IPv6InterfaceProperties GetIPv6Properties();
    }
}
