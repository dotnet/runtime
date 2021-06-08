// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal sealed class LinuxIPv6InterfaceProperties : UnixIPv6InterfaceProperties
    {
        private readonly LinuxNetworkInterface _linuxNetworkInterface;

        public LinuxIPv6InterfaceProperties(LinuxNetworkInterface linuxNetworkInterface)
            : base(linuxNetworkInterface)
        {
            _linuxNetworkInterface = linuxNetworkInterface;
        }

        public override int Mtu { get { return _linuxNetworkInterface._mtu; } }

        public override long GetScopeId(ScopeLevel scopeLevel)
        {
            if (scopeLevel == ScopeLevel.None || scopeLevel == ScopeLevel.Interface ||
                scopeLevel == ScopeLevel.Link || scopeLevel == ScopeLevel.Subnet)
            {
                return _linuxNetworkInterface.Index;
            }

            return 0;
        }
    }
}
