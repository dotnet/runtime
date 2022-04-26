// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal sealed class AndroidIPv4InterfaceProperties : UnixIPv4InterfaceProperties
    {
        public AndroidIPv4InterfaceProperties(AndroidNetworkInterface androidNetworkInterface)
            : base(androidNetworkInterface)
        {
            Mtu = androidNetworkInterface._mtu;
        }

        public override int Mtu { get; }

        public override bool IsAutomaticPrivateAddressingActive => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override bool IsAutomaticPrivateAddressingEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override bool IsDhcpEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override bool IsForwardingEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override bool UsesWins => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
    }
}
