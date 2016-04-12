// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    internal class LinuxIPInterfaceProperties : UnixIPInterfaceProperties
    {
        private readonly LinuxNetworkInterface _linuxNetworkInterface;
        private readonly GatewayIPAddressInformationCollection _gatewayAddresses;
        private readonly IPAddressCollection _dhcpServerAddresses;
        private readonly IPAddressCollection _winsServerAddresses;
        private readonly LinuxIPv4InterfaceProperties _ipv4Properties;
        private readonly LinuxIPv6InterfaceProperties _ipv6Properties;

        public LinuxIPInterfaceProperties(LinuxNetworkInterface lni)
            : base(lni)
        {
            _linuxNetworkInterface = lni;
            _gatewayAddresses = GetGatewayAddresses();
            _dhcpServerAddresses = GetDhcpServerAddresses();
            _winsServerAddresses = GetWinsServerAddresses();
            _ipv4Properties = new LinuxIPv4InterfaceProperties(lni);
            _ipv6Properties = new LinuxIPv6InterfaceProperties(lni);
        }

        public override bool IsDynamicDnsEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

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
        public GatewayIPAddressInformationCollection GetGatewayAddresses()
        {
            List<GatewayIPAddressInformation> innerCollection
                = StringParsingHelpers.ParseGatewayAddressesFromRouteFile(NetworkFiles.Ipv4RouteFile, _linuxNetworkInterface.Name);
            return new GatewayIPAddressInformationCollection(innerCollection);
        }

        private IPAddressCollection GetDhcpServerAddresses()
        {
            List<IPAddress> internalCollection
                = StringParsingHelpers.ParseDhcpServerAddressesFromLeasesFile(NetworkFiles.DHClientLeasesFile, _linuxNetworkInterface.Name);
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
