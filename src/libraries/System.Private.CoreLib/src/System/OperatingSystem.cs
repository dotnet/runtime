// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System
{
    public sealed class OperatingSystem : ISerializable, ICloneable
    {
#if TARGET_UNIX && !TARGET_OSX
        private static string? s_osPlatformName;
#endif

        private readonly Version _version;
        private readonly PlatformID _platform;
        private readonly string? _servicePack;
        private string? _versionString;

        public OperatingSystem(PlatformID platform, Version version) : this(platform, version, null)
        {
        }

        internal OperatingSystem(PlatformID platform, Version version, string? servicePack)
        {
            if (platform < PlatformID.Win32S || platform > PlatformID.Other)
            {
                throw new ArgumentOutOfRangeException(nameof(platform), platform, SR.Format(SR.Arg_EnumIllegalVal, platform));
            }

            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            _platform = platform;
            _version = version;
            _servicePack = servicePack;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public PlatformID Platform => _platform;

        public string ServicePack => _servicePack ?? string.Empty;

        public Version Version => _version;

        public object Clone() => new OperatingSystem(_platform, _version, _servicePack);

        public override string ToString() => VersionString;

        public string VersionString
        {
            get
            {
                if (_versionString == null)
                {
                    string os;
                    switch (_platform)
                    {
                        case PlatformID.Win32S: os = "Microsoft Win32S "; break;
                        case PlatformID.Win32Windows: os = (_version.Major > 4 || (_version.Major == 4 && _version.Minor > 0)) ? "Microsoft Windows 98 " : "Microsoft Windows 95 "; break;
                        case PlatformID.Win32NT: os = "Microsoft Windows NT "; break;
                        case PlatformID.WinCE: os = "Microsoft Windows CE "; break;
                        case PlatformID.Unix: os = "Unix "; break;
                        case PlatformID.Xbox: os = "Xbox "; break;
                        case PlatformID.MacOSX: os = "Mac OS X "; break;
                        case PlatformID.Other: os = "Other "; break;
                        default:
                            Debug.Fail($"Unknown platform {_platform}");
                            os = "<unknown> "; break;
                    }

                    _versionString = string.IsNullOrEmpty(_servicePack) ?
                        os + _version.ToString() :
                        os + _version.ToString(3) + " " + _servicePack;
                }

                return _versionString;
            }
        }

        public static bool IsOSPlatform(string platform)
        {
            if (platform == null)
            {
                throw new ArgumentNullException(nameof(platform));
            }
            if (platform.Length == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyOrNullString, nameof(platform));
            }

#if TARGET_BROWSER
            return platform.Equals("BROWSER", StringComparison.OrdinalIgnoreCase);
#elif TARGET_WINDOWS
            return platform.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase);
#elif TARGET_OSX
            return platform.Equals("OSX", StringComparison.OrdinalIgnoreCase) || platform.Equals("MACOS", StringComparison.OrdinalIgnoreCase);
#elif TARGET_UNIX
            return platform.Equals(s_osPlatformName ??= Interop.Sys.GetUnixName(), StringComparison.OrdinalIgnoreCase);
#else
#error Unknown OS
#endif
        }

        public static bool IsOSPlatformVersionAtLeast(string platform, int major, int minor = 0, int build = 0, int revision = 0)
            => IsOSPlatform(platform) && IsOSVersionAtLeast(major, minor, build, revision);

        public static bool IsBrowser() =>
#if TARGET_BROWSER
            true;
#else
            false;
#endif

        public static bool IsLinux() =>
#if TARGET_LINUX
            true;
#else
            false;
#endif

        public static bool IsFreeBSD() =>
#if TARGET_FREEBSD
            true;
#else
            false;
#endif

        public static bool IsFreeBSDVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => IsFreeBSD() && IsOSVersionAtLeast(major, minor, build, revision);

        public static bool IsAndroid() =>
#if TARGET_ANDROID
            true;
#else
            false;
#endif

        public static bool IsAndroidVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => IsAndroid() && IsOSVersionAtLeast(major, minor, build, revision);

        public static bool IsIOS() =>
#if TARGET_IOS
            true;
#else
            false;
#endif

        public static bool IsIOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsIOS() && IsOSVersionAtLeast(major, minor, build, 0);

        public static bool IsMacOS() =>
#if TARGET_OSX
            true;
#else
            false;
#endif

        public static bool IsMacOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsMacOS() && IsOSVersionAtLeast(major, minor, build, 0);

        public static bool IsTvOS() =>
#if TARGET_TVOS
            true;
#else
            false;
#endif

        public static bool IsTvOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsTvOS() && IsOSVersionAtLeast(major, minor, build, 0);

        public static bool IsWatchOS() =>
#if TARGET_WATCHOS
            true;
#else
            false;
#endif

        public static bool IsWatchOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsWatchOS() && IsOSVersionAtLeast(major, minor, build, 0);

        public static bool IsWindows() =>
#if TARGET_WINDOWS
            true;
#else
            false;
#endif

        public static bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => IsWindows() && IsOSVersionAtLeast(major, minor, build, revision);

        private static bool IsOSVersionAtLeast(int major, int minor, int build, int revision)
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

            return current.Revision >= revision
                || (current.Revision == -1 && revision == 0); // it is unavailable on OSX and Environment.OSVersion.Version.Revision returns -1
        }
    }
}
