// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class AndroidIPv4InterfaceProperties : UnixIPv4InterfaceProperties
    {
        private readonly AndroidNetworkInterface _androidNetworkInterface;

        public AndroidIPv4InterfaceProperties(AndroidNetworkInterface androidNetworkInterface)
            : base(androidNetworkInterface)
        {
            _androidNetworkInterface = androidNetworkInterface;
        }

        public override int Mtu => _androidNetworkInterface._mtu;

        [UnsupportedOSPlatform("android")]
        public override bool IsAutomaticPrivateAddressingActive => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        [UnsupportedOSPlatform("android")]
        public override bool IsAutomaticPrivateAddressingEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        [UnsupportedOSPlatform("android")]
        public override bool IsDhcpEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        [UnsupportedOSPlatform("android")]
        public override bool IsForwardingEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        [UnsupportedOSPlatform("android")]
        public override bool UsesWins => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
    }
}
