// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class BsdIPv4InterfaceProperties : UnixIPv4InterfaceProperties
    {
        private readonly int _mtu;

        public BsdIPv4InterfaceProperties(BsdNetworkInterface oni, int mtu)
            : base(oni)
        {
            _mtu = mtu;
        }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override bool IsAutomaticPrivateAddressingActive { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override bool IsAutomaticPrivateAddressingEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override bool IsDhcpEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        // Doesn't seem to be exposed on a per-interface basis.
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override bool IsForwardingEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override int Mtu { get { return _mtu; } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override bool UsesWins { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }
    }
}
