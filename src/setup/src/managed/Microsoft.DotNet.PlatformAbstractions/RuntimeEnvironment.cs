// Copyright(c) .NET Foundation and contributors.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.DotNet.PlatformAbstractions.Native;

namespace Microsoft.DotNet.PlatformAbstractions
{
    public static class RuntimeEnvironment
    {
        private static readonly string OverrideEnvironmentVariableName = "DOTNET_RUNTIME_ID";

        public static Platform OperatingSystemPlatform { get; } = PlatformApis.GetOSPlatform();

        public static string OperatingSystemVersion { get; } = PlatformApis.GetOSVersion();

        public static string OperatingSystem { get; } = PlatformApis.GetOSName();

        public static string RuntimeArchitecture { get; } = GetArch();

        private static string GetArch()
        {
#if NET45
            return Environment.Is64BitProcess ? "x64" : "x86";
#else
            return RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
#endif
        }

        public static string GetRuntimeIdentifier()
        {
            return
                Environment.GetEnvironmentVariable(OverrideEnvironmentVariableName) ??
                (GetRIDOS() + GetRIDVersion() + GetRIDArch());
        }

        private static string GetRIDArch()
        {
            return $"-{RuntimeArchitecture}";
        }

        private static string GetRIDVersion()
        {
            // Windows RIDs do not separate OS name and version by "." due to legacy
            // Others do, that's why we have the "." prefix on them below
            switch (OperatingSystemPlatform)
            {
                case Platform.Windows:
                    return GetWindowsProductVersion();
                case Platform.Linux:
                    if (string.IsNullOrEmpty(OperatingSystemVersion))
                    {
                        return string.Empty;
                    }

                    return $".{OperatingSystemVersion}";
                case Platform.Darwin:
                    return $".{OperatingSystemVersion}";
                case Platform.FreeBSD:
                    return $".{OperatingSystemVersion}";
                default:
                    return string.Empty; // Unknown Platform? Unknown Version!
            }
        }

        private static string GetWindowsProductVersion()
        {
            var ver = Version.Parse(OperatingSystemVersion);
            if (ver.Major == 6)
            {
                if (ver.Minor == 1)
                {
                    return "7";
                }
                else if (ver.Minor == 2)
                {
                    return "8";
                }
                else if (ver.Minor == 3)
                {
                    return "81";
                }
            }
            else if (ver.Major >= 10)
            {
                // Return the major version for use in RID computation without applying any cap.
                return ver.Major.ToString();
            }
            return string.Empty; // Unknown version
        }

        private static string GetRIDOS()
        {
            switch (OperatingSystemPlatform)
            {
                case Platform.Windows:
                    return "win";
                case Platform.Linux:
                    return OperatingSystem.ToLowerInvariant();
                case Platform.Darwin:
                    return "osx";
                case Platform.FreeBSD:
                    return "freebsd";
                default:
                    return "unknown";
            }
        }
    }
}
