// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal sealed class AndroidIPInterfaceProperties : UnixIPInterfaceProperties
    {
        private readonly AndroidIPv4InterfaceProperties _ipv4Properties;
        private readonly AndroidIPv6InterfaceProperties _ipv6Properties;

        public AndroidIPInterfaceProperties(AndroidNetworkInterface ani)
            : base(ani, globalConfig: true)
        {
            _ipv4Properties = new AndroidIPv4InterfaceProperties(ani);
            _ipv6Properties = new AndroidIPv6InterfaceProperties(ani);
        }

        public override IPv4InterfaceProperties GetIPv4Properties() => _ipv4Properties;
        public override IPv6InterfaceProperties GetIPv6Properties() => _ipv6Properties;

        public override bool IsDynamicDnsEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override IPAddressInformationCollection AnycastAddresses => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override GatewayIPAddressInformationCollection GatewayAddresses => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override IPAddressCollection DhcpServerAddresses => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override IPAddressCollection WinsServersAddresses => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
    }
}
