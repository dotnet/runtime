// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    // Provides information specific to a network interface.
    // Note: Provides information specific to a network interface. A network interface can have more 
    // than one IPAddress associated with it. We call the native GetAdaptersAddresses API to
    // pre-populate all of the interface instances and most of their associated information.
    internal class SystemIPInterfaceProperties : IPInterfaceProperties
    {
        // These are valid for all interfaces.
        private readonly bool _dnsEnabled = false;
        private readonly bool _dynamicDnsEnabled = false;
        private readonly InternalIPAddressCollection _dnsAddresses = null;
        private readonly UnicastIPAddressInformationCollection _unicastAddresses = null;
        private readonly MulticastIPAddressInformationCollection _multicastAddresses = null;
        private readonly IPAddressInformationCollection _anycastAddresses = null;
        private readonly Interop.IpHlpApi.AdapterFlags _adapterFlags;
        private readonly string _dnsSuffix;
        private readonly SystemIPv4InterfaceProperties _ipv4Properties;
        private readonly SystemIPv6InterfaceProperties _ipv6Properties;
        private readonly InternalIPAddressCollection _winsServersAddresses;
        private readonly GatewayIPAddressInformationCollection _gatewayAddresses;
        private readonly InternalIPAddressCollection _dhcpServers;

        internal SystemIPInterfaceProperties(Interop.IpHlpApi.FIXED_INFO fixedInfo, Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
        {
            _adapterFlags = ipAdapterAddresses.flags;
            _dnsSuffix = ipAdapterAddresses.dnsSuffix;
            _dnsEnabled = fixedInfo.enableDns;
            _dynamicDnsEnabled = ((ipAdapterAddresses.flags & Interop.IpHlpApi.AdapterFlags.DnsEnabled) > 0);

            _multicastAddresses = SystemMulticastIPAddressInformation.ToMulticastIpAddressInformationCollection(
                Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressInformationCollection(ipAdapterAddresses.firstMulticastAddress));
            _dnsAddresses = Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressCollection(ipAdapterAddresses.firstDnsServerAddress);
            _anycastAddresses = Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressInformationCollection(
                ipAdapterAddresses.firstAnycastAddress);
            _unicastAddresses = SystemUnicastIPAddressInformation.MarshalUnicastIpAddressInformationCollection(
                ipAdapterAddresses.firstUnicastAddress);
            _winsServersAddresses = Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressCollection(
                ipAdapterAddresses.firstWinsServerAddress);
            _gatewayAddresses = SystemGatewayIPAddressInformation.ToGatewayIpAddressInformationCollection(
                Interop.IpHlpApi.IpAdapterAddress.MarshalIpAddressCollection(ipAdapterAddresses.firstGatewayAddress));

            _dhcpServers = new InternalIPAddressCollection();
            if (ipAdapterAddresses.dhcpv4Server.address != IntPtr.Zero)
            {
                _dhcpServers.InternalAdd(ipAdapterAddresses.dhcpv4Server.MarshalIPAddress());
            }

            if (ipAdapterAddresses.dhcpv6Server.address != IntPtr.Zero)
            {
                _dhcpServers.InternalAdd(ipAdapterAddresses.dhcpv6Server.MarshalIPAddress());
            }

            if ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv4Enabled) != 0)
            {
                _ipv4Properties = new SystemIPv4InterfaceProperties(fixedInfo, ipAdapterAddresses);
            }

            if ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv6Enabled) != 0)
            {
                _ipv6Properties = new SystemIPv6InterfaceProperties(ipAdapterAddresses.ipv6Index,
                    ipAdapterAddresses.mtu, ipAdapterAddresses.zoneIndices);
            }
        }

        public override bool IsDnsEnabled { get { return _dnsEnabled; } }

        public override bool IsDynamicDnsEnabled { get { return _dynamicDnsEnabled; } }

        public override IPv4InterfaceProperties GetIPv4Properties()
        {
            if ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv4Enabled) == 0)
            {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            }

            return _ipv4Properties;
        }

        public override IPv6InterfaceProperties GetIPv6Properties()
        {
            if ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv6Enabled) == 0)
            {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            }

            return _ipv6Properties;
        }

        public override string DnsSuffix
        {
            get
            {
                return _dnsSuffix;
            }
        }

        // Returns the addresses specified by the address type.
        public override IPAddressInformationCollection AnycastAddresses
        {
            get
            {
                return _anycastAddresses;
            }
        }

        // Returns the addresses specified by the address type.
        public override UnicastIPAddressInformationCollection UnicastAddresses
        {
            get
            {
                return _unicastAddresses;
            }
        }

        // Returns the addresses specified by the address type.
        public override MulticastIPAddressInformationCollection MulticastAddresses
        {
            get
            {
                return _multicastAddresses;
            }
        }

        // Returns the addresses specified by the address type.
        public override IPAddressCollection DnsAddresses
        {
            get
            {
                return _dnsAddresses;
            }
        }

        /// IP Address of the default gateway.
        public override GatewayIPAddressInformationCollection GatewayAddresses
        {
            get
            {
                return _gatewayAddresses;
            }
        }

        public override IPAddressCollection DhcpServerAddresses
        {
            get
            {
                return _dhcpServers;
            }
        }

        public override IPAddressCollection WinsServersAddresses
        {
            get
            {
                return _winsServersAddresses;
            }
        }
    }
}
