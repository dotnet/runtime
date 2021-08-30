// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides various global machine properties related to Internet Protocol (IP),
    /// such as the local host name, domain name, and active socket connections.
    /// </summary>
    public abstract class IPGlobalProperties
    {
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static IPGlobalProperties GetIPGlobalProperties()
        {
            return IPGlobalPropertiesPal.GetIPGlobalProperties();
        }

        /// <summary>
        /// Gets the Active Udp Listeners on this machine.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract IPEndPoint[] GetActiveUdpListeners();

        /// <summary>
        /// Gets the Active Tcp Listeners on this machine.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract IPEndPoint[] GetActiveTcpListeners();

        /// <summary>
        /// Gets the Active Udp Listeners on this machine.
        /// </summary>
        [UnsupportedOSPlatform("android")]
        public abstract TcpConnectionInformation[] GetActiveTcpConnections();

        /// <summary>
        /// Gets the Dynamic Host Configuration Protocol (DHCP) scope name.
        /// </summary>
        public abstract string DhcpScopeName { get; }

        /// <summary>
        /// Gets the domain in which the local computer is registered.
        /// </summary>
        public abstract string DomainName { get; }

        /// <summary>
        /// Gets the host name for the local computer.
        /// </summary>
        public abstract string HostName { get; }

        /// <summary>
        /// Gets a bool value that specifies whether the local computer is acting as a Windows Internet Name Service (WINS) proxy.
        /// </summary>
        public abstract bool IsWinsProxy { get; }

        /// <summary>
        /// Gets the Network Basic Input/Output System (NetBIOS) node type of the local computer.
        /// </summary>
        public abstract NetBiosNodeType NodeType { get; }

        public virtual IAsyncResult BeginGetUnicastAddresses(AsyncCallback? callback, object? state)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public virtual UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public abstract TcpStatistics GetTcpIPv4Statistics();

        public abstract TcpStatistics GetTcpIPv6Statistics();

        /// <summary>
        /// Provides User Datagram Protocol (UDP) statistical data for the local computer.
        /// </summary>
        public abstract UdpStatistics GetUdpIPv4Statistics();

        public abstract UdpStatistics GetUdpIPv6Statistics();

        /// <summary>
        /// Provides Internet Control Message Protocol (ICMP) version 4 statistical data for the local computer.
        /// </summary>
        public abstract IcmpV4Statistics GetIcmpV4Statistics();

        /// <summary>
        /// Provides Internet Control Message Protocol (ICMP) version 6 statistical data for the local computer.
        /// </summary>
        public abstract IcmpV6Statistics GetIcmpV6Statistics();

        /// <summary>
        /// Provides Internet Protocol (IP) statistical data for the local computer.
        /// </summary>
        public abstract IPGlobalStatistics GetIPv4GlobalStatistics();

        public abstract IPGlobalStatistics GetIPv6GlobalStatistics();

        public virtual UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <summary>
        /// Returns a list of all unicast IP addresses after ensuring they are all stable.
        /// </summary>
        /// <returns>The collection of unicast IP addresses.</returns>
        public virtual Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }
    }
}
