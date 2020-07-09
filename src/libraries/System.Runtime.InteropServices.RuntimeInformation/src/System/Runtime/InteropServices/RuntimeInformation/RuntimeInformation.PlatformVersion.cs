// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        public static bool IsOSPlatformOrLater(string platformName)
        {
            (OSPlatform platform, Version version) = Parse(platformName);

            return IsOSPlatformOrLater(platform, version.Major, version.Minor, version.Build, version.Revision);
        }

        public static bool IsOSPlatformOrLater(OSPlatform osPlatform, int major)
            => IsOSPlatform(osPlatform) && Environment.OSVersion.Version.Major >= major;

        public static bool IsOSPlatformOrLater(OSPlatform osPlatform, int major, int minor)
            => IsOSPlatform(osPlatform) && IsOSVersionOrLater(major, minor, int.MinValue, int.MinValue);

        public static bool IsOSPlatformOrLater(OSPlatform osPlatform, int major, int minor, int build)
            => IsOSPlatform(osPlatform) && IsOSVersionOrLater(major, minor, build, int.MinValue);

        public static bool IsOSPlatformOrLater(OSPlatform osPlatform, int major, int minor, int build, int revision)
            => IsOSPlatform(osPlatform) && IsOSVersionOrLater(major, minor, build, revision);

        public static bool IsOSPlatformEarlierThan(string platformName)
        {
            (OSPlatform platform, Version version) = Parse(platformName);

            return IsOSPlatformEarlierThan(platform, version.Major, version.Minor, version.Build, version.Revision);
        }

        public static bool IsOSPlatformEarlierThan(OSPlatform osPlatform, int major)
            => IsOSPlatform(osPlatform) && Environment.OSVersion.Version.Major < major;

        public static bool IsOSPlatformEarlierThan(OSPlatform osPlatform, int major, int minor)
            => IsOSPlatform(osPlatform) && !IsOSVersionOrLater(major, minor, int.MinValue, int.MinValue);

        public static bool IsOSPlatformEarlierThan(OSPlatform osPlatform, int major, int minor, int build)
            => IsOSPlatform(osPlatform) && !IsOSVersionOrLater(major, minor, build, int.MinValue);

        public static bool IsOSPlatformEarlierThan(OSPlatform osPlatform, int major, int minor, int build, int revision)
            => IsOSPlatform(osPlatform) && !IsOSVersionOrLater(major, minor, build, revision);

        private static bool IsOSVersionOrLater(int major, int minor, int build, int revision)
        {
            Version current = Environment.OSVersion.Version;
            if (current.Major != major)
            {
                return current.Major > major;
            }
            if (current.Minor != minor)
            {
                return current.Minor > minor;
            }
            if (current.Build != build)
            {
                return current.Build > build;
            }

            return current.Revision >= revision;
        }

        private static (OSPlatform, Version) Parse(string platformName)
        {
            if (platformName == null)
            {
                throw new ArgumentNullException(nameof(platformName));
            }
            if (platformName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyValue, nameof(platformName));
            }

            for (int i = 0; i < platformName.Length; i++)
            {
                if (char.IsDigit(platformName[i]))
                {
                    if (Version.TryParse(platformName.AsSpan().Slice(i), out var version))
                    {
                        return (OSPlatform.Create(platformName.Substring(0, i)), version);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            throw new ArgumentException(SR.Format(SR.Argument_InvalidPlatfromName, platformName), nameof(platformName));
        }
    }
}
