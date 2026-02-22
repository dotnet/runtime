// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class SunOSIPInterfaceProperties : UnixIPInterfaceProperties
    {
        private readonly SunOSIPv4InterfaceProperties _ipv4Properties;
        private readonly SunOSIPv6InterfaceProperties _ipv6Properties;

        public SunOSIPInterfaceProperties(SunOSNetworkInterface sni)
            : base(sni, globalConfig: true)
        {
            _ipv4Properties = new SunOSIPv4InterfaceProperties(sni);
            _ipv6Properties = new SunOSIPv6InterfaceProperties(sni);
        }

        public override IPv4InterfaceProperties GetIPv4Properties() => _ipv4Properties;
        public override IPv6InterfaceProperties GetIPv6Properties() => _ipv6Properties;

        public override bool IsDynamicDnsEnabled
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override IPAddressInformationCollection AnycastAddresses
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override GatewayIPAddressInformationCollection GatewayAddresses
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override IPAddressCollection DhcpServerAddresses
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override IPAddressCollection WinsServersAddresses
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
    }
}
