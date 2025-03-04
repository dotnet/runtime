// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;
        private static volatile int s_osArchPlusOne;

        public static string OSDescription => s_osDescription ??= (GetPrettyOSDescription() ?? Interop.Sys.GetUnixVersion());

        public static Architecture OSArchitecture
        {
            get
            {
                int osArch = s_osArchPlusOne - 1;

                if (osArch < 0)
                {
                    osArch = Interop.Sys.GetOSArchitecture();
                    if (osArch < 0)
                        osArch = (int)ProcessArchitecture;
                    s_osArchPlusOne = osArch + 1;
                }

                Debug.Assert(osArch >= 0);
                return (Architecture)osArch;
            }
        }

        private static string? GetPrettyOSDescription()
        {
            if (OperatingSystem.IsLinux())
            {
                return Interop.OSReleaseFile.GetPrettyName();
            }

            return OperatingSystem.IsAndroid() ? $"Android (API {Environment.OSVersion.Version.Major})"
                : OperatingSystem.IsMacOS() ? FormatApplePlatformOSDescription("macOS")
                : OperatingSystem.IsMacCatalyst() ? FormatApplePlatformOSDescription("Mac Catalyst")
                : OperatingSystem.IsIOS() ? FormatApplePlatformOSDescription("iOS")
                : OperatingSystem.IsTvOS() ? FormatApplePlatformOSDescription("tvOS")
                : OperatingSystem.IsWatchOS() ? FormatApplePlatformOSDescription("watchOS")
                : null;
        }

        private static string FormatApplePlatformOSDescription(string osName)
        {
            return $"{osName} (version {Environment.OSVersion.Version})";
        }
    }
}
