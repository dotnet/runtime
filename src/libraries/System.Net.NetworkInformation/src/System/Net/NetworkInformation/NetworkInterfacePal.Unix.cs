// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            if (OperatingSystem.IsLinux())
            {
                return LinuxNetworkInterface.GetLinuxNetworkInterfaces();
            }

            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
            {
                return BsdNetworkInterface.GetBsdNetworkInterfaces();
            }

            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static bool GetIsNetworkAvailable()
        {
            foreach (var ni in GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    return true;
                }
            }

            return false;
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static int IPv6LoopbackInterfaceIndex { get { return LoopbackInterfaceIndex; } }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static int LoopbackInterfaceIndex
        {
            get
            {
                NetworkInterface[] interfaces = GetAllNetworkInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (interfaces[i].NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        return ((UnixNetworkInterface)interfaces[i]).Index;
                    }
                }

                throw new NetworkInformationException(SR.net_NoLoopback);
            }
        }
    }
}
