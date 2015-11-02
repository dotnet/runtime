// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal class LinuxIPv6InterfaceProperties : UnixIPv6InterfaceProperties
    {
        private readonly LinuxNetworkInterface _linuxNetworkInterface;
        private readonly int _mtu;

        public LinuxIPv6InterfaceProperties(LinuxNetworkInterface linuxNetworkInterface)
            : base(linuxNetworkInterface)
        {
            _linuxNetworkInterface = linuxNetworkInterface;
            _mtu = GetMtu();
        }

        public override int Mtu { get { return _mtu; } }

        public override long GetScopeId(ScopeLevel scopeLevel)
        {
            throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        }

        private int GetMtu()
        {
            string path = path = Path.Combine(NetworkFiles.SysClassNetFolder, _linuxNetworkInterface.Name, NetworkFiles.MtuFileName);
            return StringParsingHelpers.ParseRawIntFile(path);
        }
    }
}
