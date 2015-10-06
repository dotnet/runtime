// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public abstract class IPGlobalProperties
    {
        public static IPGlobalProperties GetIPGlobalProperties()
        {
            return new SystemIPGlobalProperties();
        }

        internal static IPGlobalProperties InternalGetIPGlobalProperties()
        {
            return new SystemIPGlobalProperties();
        }

        /// Gets the Active Udp Listeners on this machine.
        public abstract IPEndPoint[] GetActiveUdpListeners();

        /// Gets the Active Tcp Listeners on this machine.
        public abstract IPEndPoint[] GetActiveTcpListeners();

        /// Gets the Active Udp Listeners on this machine.
        public abstract TcpConnectionInformation[] GetActiveTcpConnections();

        /// Gets the Dynamic Host Configuration Protocol (DHCP) scope name.
        public abstract string DhcpScopeName { get; }

        /// Gets the domain in which the local computer is registered.
        public abstract string DomainName { get; }

        /// Gets the host name for the local computer.
        public abstract string HostName { get; }

        /// Gets a bool value that specifies whether the local computer is acting as a Windows Internet Name Service (WINS) proxy.
        public abstract bool IsWinsProxy { get; }

        /// Gets the Network Basic Input/Output System (NetBIOS) node type of the local computer.
        public abstract NetBiosNodeType NodeType { get; }

        public abstract TcpStatistics GetTcpIPv4Statistics();

        public abstract TcpStatistics GetTcpIPv6Statistics();

        /// Provides User Datagram Protocol (UDP) statistical data for the local computer.
        public abstract UdpStatistics GetUdpIPv4Statistics();

        public abstract UdpStatistics GetUdpIPv6Statistics();

        /// Provides Internet Control Message Protocol (ICMP) version 4 statistical data for the local computer.
        public abstract IcmpV4Statistics GetIcmpV4Statistics();

        /// Provides Internet Control Message Protocol (ICMP) version 6 statistical data for the local computer.
        public abstract IcmpV6Statistics GetIcmpV6Statistics();

        /// Provides Internet Protocol (IP) statistical data for the local computer.
        public abstract IPGlobalStatistics GetIPv4GlobalStatistics();

        public abstract IPGlobalStatistics GetIPv6GlobalStatistics();

        /// Returns a list of all unicast IP addresses after ensuring they are all stable.
        public virtual UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public virtual IAsyncResult BeginGetUnicastAddresses(AsyncCallback callback, object state)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public virtual UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public virtual Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            return Task<UnicastIPAddressInformationCollection>.Factory.FromAsync(BeginGetUnicastAddresses, EndGetUnicastAddresses, null);
        }
    }
}
