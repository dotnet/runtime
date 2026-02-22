// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class SunOSIPv4InterfaceProperties : UnixIPv4InterfaceProperties
    {
        public SunOSIPv4InterfaceProperties(SunOSNetworkInterface sunOSNetworkInterface)
            : base(sunOSNetworkInterface)
        {
            Mtu = sunOSNetworkInterface._mtu;
        }

        public override int Mtu { get; }

        public override bool IsAutomaticPrivateAddressingActive
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override bool IsAutomaticPrivateAddressingEnabled
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override bool IsDhcpEnabled
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override bool IsForwardingEnabled
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override bool UsesWins
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
    }
}
