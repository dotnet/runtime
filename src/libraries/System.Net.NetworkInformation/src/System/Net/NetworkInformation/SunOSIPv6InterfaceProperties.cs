// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class SunOSIPv6InterfaceProperties : UnixIPv6InterfaceProperties
    {
        private readonly SunOSNetworkInterface _sunOSNetworkInterface;

        public SunOSIPv6InterfaceProperties(SunOSNetworkInterface sunOSNetworkInterface)
            : base(sunOSNetworkInterface)
        {
            _sunOSNetworkInterface = sunOSNetworkInterface;
        }

        public override int Mtu => _sunOSNetworkInterface._mtu;

        public override long GetScopeId(ScopeLevel scopeLevel)
        {
            if (scopeLevel == ScopeLevel.None || scopeLevel == ScopeLevel.Interface ||
                scopeLevel == ScopeLevel.Link || scopeLevel == ScopeLevel.Subnet)
            {
                return _sunOSNetworkInterface.Index;
            }

            return 0;
        }
    }
}
