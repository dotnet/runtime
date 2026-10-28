// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal static class IPGlobalPropertiesPal
    {
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        [UnsupportedOSPlatform("haiku")]
        public static IPGlobalProperties GetIPGlobalProperties()
        {
            if (OperatingSystem.IsLinux())
            {
                return new LinuxIPGlobalProperties();
            }

            if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsOpenBSD())
            {
                return new BsdIPGlobalProperties();
            }

            throw new PlatformNotSupportedException();
        }
    }
}
