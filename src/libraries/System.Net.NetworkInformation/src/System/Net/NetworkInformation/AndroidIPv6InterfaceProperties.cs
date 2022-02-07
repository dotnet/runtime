// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal sealed class AndroidIPv6InterfaceProperties : UnixIPv6InterfaceProperties
    {
        private readonly AndroidNetworkInterface _androidNetworkInterface;

        public AndroidIPv6InterfaceProperties(AndroidNetworkInterface androidNetworkInterface)
            : base(androidNetworkInterface)
        {
            _androidNetworkInterface = androidNetworkInterface;
        }

        public override int Mtu => _androidNetworkInterface._mtu;

        public override long GetScopeId(ScopeLevel scopeLevel)
        {
            if (scopeLevel == ScopeLevel.None || scopeLevel == ScopeLevel.Interface ||
                scopeLevel == ScopeLevel.Link || scopeLevel == ScopeLevel.Subnet)
            {
                return _androidNetworkInterface.Index;
            }

            return 0;
        }
    }
}
