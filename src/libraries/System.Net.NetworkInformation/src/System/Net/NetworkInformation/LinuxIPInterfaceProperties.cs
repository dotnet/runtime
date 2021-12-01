// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class LinuxIPInterfaceProperties : UnixIPInterfaceProperties
    {
        private readonly LinuxNetworkInterface _linuxNetworkInterface;
        private readonly GatewayIPAddressInformationCollection _gatewayAddresses;
        private readonly IPAddressCollection _dhcpServerAddresses;
        private readonly IPAddressCollection _winsServerAddresses;
        private readonly LinuxIPv4InterfaceProperties _ipv4Properties;
        private readonly LinuxIPv6InterfaceProperties _ipv6Properties;

        public LinuxIPInterfaceProperties(LinuxNetworkInterface lni, LinuxNetworkInterface.LinuxNetworkInterfaceSystemProperties systemProperties)
            : base(lni, globalConfig: true)
        {
            _linuxNetworkInterface = lni;
            _gatewayAddresses = GetGatewayAddresses(systemProperties);
            _dhcpServerAddresses = GetDhcpServerAddresses();
            _winsServerAddresses = GetWinsServerAddresses();
            _dnsSuffix = systemProperties.DnsSuffix;
            _dnsAddresses = systemProperties.DnsAddresses;
            _ipv4Properties = new LinuxIPv4InterfaceProperties(lni);
            _ipv6Properties = new LinuxIPv6InterfaceProperties(lni);
        }

        [UnsupportedOSPlatform("linux")]
        public override bool IsDynamicDnsEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("linux")]
        public override IPAddressInformationCollection AnycastAddresses { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override GatewayIPAddressInformationCollection GatewayAddresses { get { return _gatewayAddresses; } }

        public override IPAddressCollection DhcpServerAddresses { get { return _dhcpServerAddresses; } }

        public override IPAddressCollection WinsServersAddresses { get { return _winsServerAddresses; } }

        public override IPv4InterfaceProperties GetIPv4Properties()
        {
            return _ipv4Properties;
        }

        public override IPv6InterfaceProperties GetIPv6Properties()
        {
            return _ipv6Properties;
        }

        // /proc/net/route contains some information about gateway addresses,
        // and separates the information about by each interface.
        public GatewayIPAddressInformationCollection GetGatewayAddresses(LinuxNetworkInterface.LinuxNetworkInterfaceSystemProperties systemProperties)
        {
            List<GatewayIPAddressInformation> collection = new List<GatewayIPAddressInformation>();

            if (systemProperties.IPv4Routes != null)
            {
                StringParsingHelpers.ParseIPv4GatewayAddressesFromRouteFile(collection, systemProperties.IPv4Routes, _linuxNetworkInterface.Name);
            }

            if (systemProperties.IPv6Routes != null)
            {
                StringParsingHelpers.ParseIPv6GatewayAddressesFromRouteFile(collection, systemProperties.IPv6Routes, _linuxNetworkInterface.Name, _linuxNetworkInterface.Index);
            }

            return new GatewayIPAddressInformationCollection(collection);
        }

        private IPAddressCollection GetDhcpServerAddresses()
        {
            List<IPAddress> internalCollection = new List<IPAddress>();

            StringParsingHelpers.ParseDhcpServerAddressesFromLeasesFile(internalCollection, NetworkFiles.DHClientLeasesFile, _linuxNetworkInterface.Name);
            StringParsingHelpers.ParseDhcpServerAddressesFromLeasesFile(internalCollection, string.Format(NetworkFiles.DHClientInterfaceLeasesFile, _linuxNetworkInterface.Name), _linuxNetworkInterface.Name);
            StringParsingHelpers.ParseDhcpServerAddressesFromLeasesFile(internalCollection, string.Format(NetworkFiles.DHClientSecondaryInterfaceLeasesFile, _linuxNetworkInterface.Name), _linuxNetworkInterface.Name);

            return new InternalIPAddressCollection(internalCollection);
        }

        private IPAddressCollection GetWinsServerAddresses()
        {
            List<IPAddress> internalCollection
                = StringParsingHelpers.ParseWinsServerAddressesFromSmbConfFile(NetworkFiles.SmbConfFile);
            return new InternalIPAddressCollection(internalCollection);
        }
    }
}
