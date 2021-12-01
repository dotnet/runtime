// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    // Provides information specific to a network interface.
    // Note: Provides information specific to a network interface. A network interface can have more
    // than one IPAddress associated with it. We call the native GetAdaptersAddresses API to
    // pre-populate all of the interface instances and most of their associated information.
    internal sealed class SystemIPInterfaceProperties : IPInterfaceProperties
    {
        // These are valid for all interfaces.
        private readonly bool _dnsEnabled;
        private readonly bool _dynamicDnsEnabled;
        private readonly InternalIPAddressCollection _dnsAddresses;
        private readonly UnicastIPAddressInformationCollection _unicastAddresses;
        private readonly MulticastIPAddressInformationCollection _multicastAddresses;
        private readonly IPAddressInformationCollection _anycastAddresses;
        private readonly Interop.IpHlpApi.AdapterFlags _adapterFlags;
        private readonly string _dnsSuffix;
        private readonly SystemIPv4InterfaceProperties? _ipv4Properties;
        private readonly SystemIPv6InterfaceProperties? _ipv6Properties;
        private readonly InternalIPAddressCollection _winsServersAddresses;
        private readonly GatewayIPAddressInformationCollection _gatewayAddresses;
        private readonly InternalIPAddressCollection _dhcpServers;

        internal SystemIPInterfaceProperties(in Interop.IpHlpApi.FIXED_INFO fixedInfo, in Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
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
            if (_ipv4Properties is null)
            {
                throw new NetworkInformationException(SocketError.ProtocolNotSupported);
            }

            return _ipv4Properties;
        }

        public override IPv6InterfaceProperties GetIPv6Properties()
        {
            if (_ipv6Properties is null)
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
