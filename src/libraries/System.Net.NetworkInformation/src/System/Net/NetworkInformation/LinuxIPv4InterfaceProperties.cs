// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class LinuxIPv4InterfaceProperties : UnixIPv4InterfaceProperties
    {
        private readonly LinuxNetworkInterface _linuxNetworkInterface;
        private readonly bool _isForwardingEnabled;

        public LinuxIPv4InterfaceProperties(LinuxNetworkInterface linuxNetworkInterface)
            : base(linuxNetworkInterface)
        {
            _linuxNetworkInterface = linuxNetworkInterface;
            _isForwardingEnabled = GetIsForwardingEnabled();
        }

        [UnsupportedOSPlatform("linux")]
        public override bool IsAutomaticPrivateAddressingActive { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("linux")]
        public override bool IsAutomaticPrivateAddressingEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("linux")]
        public override bool IsDhcpEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override bool IsForwardingEnabled { get { return _isForwardingEnabled; } }

        public override int Mtu { get { return _linuxNetworkInterface._mtu; } }

        public override bool UsesWins { get { return _linuxNetworkInterface.GetIPProperties().WinsServersAddresses.Count > 0; } }

        private bool GetIsForwardingEnabled()
        {
            string[] paths = new string[]
            {
                // /proc/sys/net/ipv4/conf/<name>/forwarding
                Path.Join(NetworkFiles.Ipv4ConfigFolder, _linuxNetworkInterface.Name, NetworkFiles.ForwardingFileName),
                // Fall back to global forwarding config /proc/sys/net/ipv4/ip_forward
                NetworkFiles.Ipv4GlobalForwardingFile
            };

            for (int i = 0; i < paths.Length; i++)
            {
                // Actual layout is specific to kernel version and it could change over time.
                // If the kernel version we're running on doesn't have this files we don't want to fail, but instead continue. We've hit this exceptions in Windows Subsystem for Linux in the past.
                // Also the /proc directory may not be mounted or accessible for other reasons. Therefore we catch these potential exceptions and return false instead.
                try
                {
                    return StringParsingHelpers.ParseRawIntFile(paths[i]) == 1;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            return false;
        }
    }
}
