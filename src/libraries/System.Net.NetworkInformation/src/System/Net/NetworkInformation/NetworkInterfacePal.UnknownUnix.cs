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
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static bool GetIsNetworkAvailable()
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static int IPv6LoopbackInterfaceIndex
        {
            get { throw new PlatformNotSupportedException(); }
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static int LoopbackInterfaceIndex
        {
            get { throw new PlatformNotSupportedException(); }
        }
    }
}
