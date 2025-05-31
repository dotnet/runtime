// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;
        private static volatile int s_osArchPlusOne;

        public static string OSDescription => s_osDescription ??=
#if TARGET_ANDROID
            $"Android (API level {Environment.OSVersion.Version.Major})";
#elif TARGET_OSX
            $"macOS {Environment.OSVersion.Version}";
#elif TARGET_MACCATALYST
            $"Mac Catalyst {Environment.OSVersion.Version}";
#elif TARGET_IOS
            $"iOS {Environment.OSVersion.Version}";
#elif TARGET_TVOS
            $"tvOS {Environment.OSVersion.Version}";
#elif TARGET_WATCHOS
            $"watchOS {Environment.OSVersion.Version}";
#elif TARGET_LINUX
            Interop.OSReleaseFile.GetPrettyName() ?? Interop.Sys.GetUnixVersion();
#else
            Interop.Sys.GetUnixVersion();
#endif

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
    }
}
