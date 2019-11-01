// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.PlatformAbstractions.Native
{
    internal static class PlatformApis
    {
        private class DistroInfo
        {
            public string Id;
            public string VersionId;
        }

        private static readonly Lazy<Platform> _platform = new Lazy<Platform>(DetermineOSPlatform);
        private static readonly Lazy<DistroInfo> _distroInfo = new Lazy<DistroInfo>(LoadDistroInfo);

        public static string GetOSName()
        {
            switch (GetOSPlatform())
            {
                case Platform.Windows:
                    return "Windows";
                case Platform.Linux:
                    return GetDistroId() ?? "Linux";
                case Platform.Darwin:
                    return "Mac OS X";
                case Platform.FreeBSD:
                    return "FreeBSD";
                default:
                    return "Unknown";
            }
        }

        public static string GetOSVersion()
        {
            switch (GetOSPlatform())
            {
                case Platform.Windows:
                    return NativeMethods.Windows.RtlGetVersion() ?? string.Empty;
                case Platform.Linux:
                    return GetDistroVersionId() ?? string.Empty;
                case Platform.Darwin:
                    return GetDarwinVersion() ?? string.Empty;
                case Platform.FreeBSD:
                    return GetFreeBSDVersion() ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static string GetDarwinVersion()
        {
            Version version;
            var kernelRelease = NativeMethods.Darwin.GetKernelRelease();
            if (!Version.TryParse(kernelRelease, out version) || version.Major < 5)
            {
                // 10.0 covers all versions prior to Darwin 5
                // Similarly, if the version is not a valid version number, but we have still detected that it is Darwin, we just assume
                // it is OS X 10.0
                return "10.0";
            }
            else
            {
                // Mac OS X 10.1 mapped to Darwin 5.x, and the mapping continues that way
                // So just subtract 4 from the Darwin version.
                // https://en.wikipedia.org/wiki/Darwin_%28operating_system%29
                return $"10.{version.Major - 4}";
            }
        }

        private static string GetFreeBSDVersion()
        {
            // This is same as sysctl kern.version
            // FreeBSD 11.0-RELEASE-p1 FreeBSD 11.0-RELEASE-p1 #0 r306420: Thu Sep 29 01:43:23 UTC 2016     root@releng2.nyi.freebsd.org:/usr/obj/usr/src/sys/GENERIC
            // What we want is major release as minor releases should be compatible.
            String version = RuntimeInformation.OSDescription;
            try
            {
                // second token up to first dot
                return RuntimeInformation.OSDescription.Split()[1].Split('.')[0];
            }
            catch
            {
            }
            return string.Empty;
        }

        public static Platform GetOSPlatform()
        {
            return _platform.Value;
        }

        private static string GetDistroId()
        {
            return _distroInfo.Value?.Id;
        }

        private static string GetDistroVersionId()
        {
            return _distroInfo.Value?.VersionId;
        }

        private static DistroInfo LoadDistroInfo()
        {
            DistroInfo result = null;

            // Sample os-release file:
            //   NAME="Ubuntu"
            //   VERSION = "14.04.3 LTS, Trusty Tahr"
            //   ID = ubuntu
            //   ID_LIKE = debian
            //   PRETTY_NAME = "Ubuntu 14.04.3 LTS"
            //   VERSION_ID = "14.04"
            //   HOME_URL = "http://www.ubuntu.com/"
            //   SUPPORT_URL = "http://help.ubuntu.com/"
            //   BUG_REPORT_URL = "http://bugs.launchpad.net/ubuntu/"
            // We use ID and VERSION_ID

            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                result = new DistroInfo();
                foreach (var line in lines)
                {
                    if (line.StartsWith("ID=", StringComparison.Ordinal))
                    {
                        result.Id = line.Substring(3).Trim('"', '\'');
                    }
                    else if (line.StartsWith("VERSION_ID=", StringComparison.Ordinal))
                    {
                        result.VersionId = line.Substring(11).Trim('"', '\'');
                    }
                }
            }
            else if (File.Exists("/etc/redhat-release"))
            {
                var lines = File.ReadAllLines("/etc/redhat-release");

                if (lines.Length >= 1)
                {
                    string line = lines[0];
                    if (line.StartsWith("Red Hat Enterprise Linux Server release 6.") ||
                        line.StartsWith("CentOS release 6."))
                    {
                        result = new DistroInfo();
                        result.Id = "rhel";
                        result.VersionId = "6";
                    }
                }
            }

            if (result != null)
            {
                result = NormalizeDistroInfo(result);
            }
            
            return result;
        }

        // For some distros, we don't want to use the full version from VERSION_ID. One example is
        // Red Hat Enterprise Linux, which includes a minor version in their VERSION_ID but minor
        // versions are backwards compatable.
        //
        // In this case, we'll normalized RIDs like 'rhel.7.2' and 'rhel.7.3' to a generic
        // 'rhel.7'. This brings RHEL in line with other distros like CentOS or Debian which
        // don't put minor version numbers in their VERSION_ID fields because all minor versions
        // are backwards compatible.
        private static DistroInfo NormalizeDistroInfo(DistroInfo distroInfo)
        {
            // Handle if VersionId is null by just setting the index to -1.
            int lastVersionNumberSeparatorIndex = distroInfo.VersionId?.IndexOf('.') ?? -1;

            if (lastVersionNumberSeparatorIndex != -1 && distroInfo.Id == "alpine")
            {
                // For Alpine, the version reported has three components, so we need to find the second version separator
                lastVersionNumberSeparatorIndex = distroInfo.VersionId.IndexOf('.', lastVersionNumberSeparatorIndex + 1);
            }

            if (lastVersionNumberSeparatorIndex != -1 && (distroInfo.Id == "rhel" || distroInfo.Id == "alpine"))
            {
                distroInfo.VersionId = distroInfo.VersionId.Substring(0, lastVersionNumberSeparatorIndex);
            }

            return distroInfo;
        }

        // I could probably have just done one method signature and put the #if inside the body but the implementations
        // are just completely different so I wanted to make that clear by putting the whole thing inside the #if.
#if NET45
        private static Platform DetermineOSPlatform()
        {
            var platform = (int)Environment.OSVersion.Platform;
            var isWindows = (platform != 4) && (platform != 6) && (platform != 128);

            if (isWindows)
            {
                return Platform.Windows;
            }
            else
            {
                try
                {
                    var uname = NativeMethods.Unix.GetUname();
                    if (string.Equals(uname, "Darwin", StringComparison.OrdinalIgnoreCase))
                    {
                        return Platform.Darwin;
                    }
                    if (string.Equals(uname, "Linux", StringComparison.OrdinalIgnoreCase))
                    {
                        return Platform.Linux;
                    }
                    if (string.Equals(uname, "FreeBSD", StringComparison.OrdinalIgnoreCase))
                    {
                        return Platform.FreeBSD;
                    }
                }
                catch
                {
                }
                return Platform.Unknown;
            }
        }
#else
        private static Platform DetermineOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Platform.Windows;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Platform.Linux;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.Darwin;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
            {
                return Platform.FreeBSD;
            }

            return Platform.Unknown;
        }
#endif
    }
}
