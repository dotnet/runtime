﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                var result = new DistroInfo();
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

                return NormalizeDistroInfo(result);
            }
            return null;
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
            if (distroInfo.Id == "gentoo")
            {
                // Gentoo does not have versions, since they use rolling releases.
                // Workaround this by setting VersionId to the current year and month.
                distroInfo.VersionId = DateTime.Today.ToString("yyyy.MM");
            }

            int minorVersionNumberSeparatorIndex = distroInfo.VersionId.IndexOf('.');

            if (distroInfo.Id == "rhel" && minorVersionNumberSeparatorIndex != -1)
            {
                distroInfo.VersionId = distroInfo.VersionId.Substring(0, minorVersionNumberSeparatorIndex);
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
            return Platform.Unknown;
        }
#endif
    }
}
