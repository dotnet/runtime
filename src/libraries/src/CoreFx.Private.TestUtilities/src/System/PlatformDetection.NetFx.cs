// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace System
{
    public static partial class PlatformDetection
    {
        private static string FrameworkName => AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
        private static Version TargetVersion => String.IsNullOrEmpty(FrameworkName) ? new Version(4, 5, 0, 0) : new FrameworkName(FrameworkName).Version;

        // The current full framework xunit runner is targeting 4.5.2 so we expect TargetsNetFx452OrLower to be true.
        // When we update xunit runner in the future which may target recent framework version, TargetsNetFx452OrLower can start return
        // false but we don't expect any code change though.
        public static bool TargetsNetFx452OrLower => TargetVersion.CompareTo(new Version(4, 5, 3, 0)) < 0;

        public static bool IsNetfx462OrNewer => GetFrameworkVersion() >= new Version(4, 6, 2);

        public static bool IsNetfx470OrNewer => GetFrameworkVersion() >= new Version(4, 7, 0);

        public static bool IsNetfx471OrNewer => GetFrameworkVersion() >= new Version(4, 7, 1);

        // To get the framework version we can do it throught the registry key and getting the Release value under the .NET Framework key.
        // the mapping to each version can be found in: https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        // everytime we ship a new version this method should be updated to include the new framework version.
        private static Version GetFrameworkVersion()
        {
            using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                if (ndpKey != null)
                {
                    int value = (int)(ndpKey.GetValue("Release") ?? 0);
                    if (value >= 461308)
                        return new Version(4, 7, 1);
                    if (value >= 460798)
                        return new Version(4, 7, 0);
                    if (value >= 394802)
                        return new Version(4, 6, 2);
                    if (value >= 394254)
                        return new Version(4, 6, 1);
                    if (value >= 393295)
                        return new Version(4, 6, 0);
                    if (value >= 379893)
                        return new Version(4, 5, 2);
                    if (value >= 378675)
                        return new Version(4, 5, 1);
                    if (value >= 378389)
                        return new Version(4, 5, 0);

                    throw new NotSupportedException($"No 4.5 or later framework version detected, framework key value: {value}");
                }

                throw new NotSupportedException(@"No registry key found under 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' to determine running framework version");
            }
        }
    }
}
