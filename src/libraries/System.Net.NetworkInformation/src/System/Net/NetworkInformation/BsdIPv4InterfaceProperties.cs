// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public override bool IsAutomaticPrivateAddressingActive { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override bool IsAutomaticPrivateAddressingEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override bool IsDhcpEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        // Doesn't seem to be exposed on a per-interface basis.
        public override bool IsForwardingEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override int Mtu { get { return _mtu; } }

        public override bool UsesWins { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }
    }
}
