// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class BsdIPv6InterfaceProperties : UnixIPv6InterfaceProperties
    {
        private readonly int _mtu;

        public BsdIPv6InterfaceProperties(BsdNetworkInterface oni, int mtu)
            : base(oni)
        {
            _mtu = mtu;
        }

        public override int Mtu { get { return _mtu; } }

        public override long GetScopeId(ScopeLevel scopeLevel)
        {
            throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        }
    }
}
