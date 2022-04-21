// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System
{
    public sealed class OperatingSystem : ISerializable, ICloneable
    {
#if TARGET_UNIX && !TARGET_OSX && !TARGET_MACCATALYST && !TARGET_IOS
        private static readonly string s_osPlatformName = Interop.Sys.GetUnixName();
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

            ArgumentNullException.ThrowIfNull(version);

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

                    Span<char> stackBuffer = stackalloc char[128];
                    _versionString = string.IsNullOrEmpty(_servicePack) ?
                        string.Create(null, stackBuffer, $"{os}{_version}") :
                        string.Create(null, stackBuffer, $"{os}{_version.ToString(3)} {_servicePack}");
                }

                return _versionString;
            }
        }

        /// <summary>
        /// Indicates whether the current application is running on the specified platform.
        /// </summary>
        /// <param name="platform">Case-insensitive platform name. Examples: Browser, Linux, FreeBSD, Android, iOS, macOS, tvOS, watchOS, Windows.</param>
        public static bool IsOSPlatform(string platform!!)
        {
#if TARGET_BROWSER
            return platform.Equals("BROWSER", StringComparison.OrdinalIgnoreCase);
#elif TARGET_WINDOWS
            return platform.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase);
#elif TARGET_OSX
            return platform.Equals("OSX", StringComparison.OrdinalIgnoreCase) || platform.Equals("MACOS", StringComparison.OrdinalIgnoreCase);
#elif TARGET_MACCATALYST
            return platform.Equals("MACCATALYST", StringComparison.OrdinalIgnoreCase) || platform.Equals("IOS", StringComparison.OrdinalIgnoreCase);
#elif TARGET_IOS
            return platform.Equals("IOS", StringComparison.OrdinalIgnoreCase);
#elif TARGET_UNIX
            return platform.Equals(s_osPlatformName, StringComparison.OrdinalIgnoreCase);
#else
#error Unknown OS
#endif
        }

        /// <summary>
        /// Check for the OS with a >= version comparison. Used to guard APIs that were added in the given OS release.
        /// </summary>
        /// <param name="platform">Case-insensitive platform name. Examples: Browser, Linux, FreeBSD, Android, iOS, macOS, tvOS, watchOS, Windows.</param>
        /// <param name="major">Major OS version number.</param>
        /// <param name="minor">Minor OS version number (optional).</param>
        /// <param name="build">Build OS version number (optional).</param>
        /// <param name="revision">Revision OS version number (optional).</param>
        public static bool IsOSPlatformVersionAtLeast(string platform, int major, int minor = 0, int build = 0, int revision = 0)
            => IsOSPlatform(platform) && IsOSVersionAtLeast(major, minor, build, revision);

        /// <summary>
        /// Indicates whether the current application is running as WASM in a Browser.
        /// </summary>
        [NonVersionable]
        public static bool IsBrowser() =>
#if TARGET_BROWSER
            true;
#else
            false;
#endif

        /// <summary>
        /// Indicates whether the current application is running on Linux.
        /// </summary>
        [NonVersionable]
        public static bool IsLinux() =>
#if TARGET_LINUX && !TARGET_ANDROID
            true;
#else
            false;
#endif

        /// <summary>
        /// Indicates whether the current application is running on FreeBSD.
        /// </summary>
        [NonVersionable]
        public static bool IsFreeBSD() =>
#if TARGET_FREEBSD
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the FreeBSD version (returned by 'uname') with a >= version comparison. Used to guard APIs that were added in the given FreeBSD release.
        /// </summary>
        public static bool IsFreeBSDVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => IsFreeBSD() && IsOSVersionAtLeast(major, minor, build, revision);

        /// <summary>
        /// Indicates whether the current application is running on Android.
        /// </summary>
        [NonVersionable]
        public static bool IsAndroid() =>
#if TARGET_ANDROID
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the Android API level (returned by 'ro.build.version.sdk') with a >= version comparison. Used to guard APIs that were added in the given Android release.
        /// </summary>
        public static bool IsAndroidVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
            => IsAndroid() && IsOSVersionAtLeast(major, minor, build, revision);

        /// <summary>
        /// Indicates whether the current application is running on iOS or MacCatalyst.
        /// </summary>
        [SupportedOSPlatformGuard("maccatalyst")]
        [NonVersionable]
        public static bool IsIOS() =>
#if TARGET_IOS || TARGET_MACCATALYST
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the iOS/MacCatalyst version (returned by 'libobjc.get_operatingSystemVersion') with a >= version comparison. Used to guard APIs that were added in the given iOS release.
        /// </summary>
        [SupportedOSPlatformGuard("maccatalyst")]
        [NonVersionable]
        public static bool IsIOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsIOS() && IsOSVersionAtLeast(major, minor, build, 0);

        /// <summary>
        /// Indicates whether the current application is running on macOS.
        /// </summary>
        [NonVersionable]
        public static bool IsMacOS() =>
#if TARGET_OSX
            true;
#else
            false;
#endif

        internal static bool IsOSXLike() =>
#if TARGET_OSX || TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the macOS version (returned by 'libobjc.get_operatingSystemVersion') with a >= version comparison. Used to guard APIs that were added in the given macOS release.
        /// </summary>
        public static bool IsMacOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsMacOS() && IsOSVersionAtLeast(major, minor, build, 0);

        /// <summary>
        /// Indicates whether the current application is running on Mac Catalyst.
        /// </summary>
        [NonVersionable]
        public static bool IsMacCatalyst() =>
#if TARGET_MACCATALYST
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the Mac Catalyst version (iOS version as presented in Apple documentation) with a >= version comparison. Used to guard APIs that were added in the given Mac Catalyst release.
        /// </summary>
        public static bool IsMacCatalystVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsMacCatalyst() && IsOSVersionAtLeast(major, minor, build, 0);

        /// <summary>
        /// Indicates whether the current application is running on tvOS.
        /// </summary>
        [NonVersionable]
        public static bool IsTvOS() =>
#if TARGET_TVOS
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the tvOS version (returned by 'libobjc.get_operatingSystemVersion') with a >= version comparison. Used to guard APIs that were added in the given tvOS release.
        /// </summary>
        public static bool IsTvOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsTvOS() && IsOSVersionAtLeast(major, minor, build, 0);

        /// <summary>
        /// Indicates whether the current application is running on watchOS.
        /// </summary>
        [NonVersionable]
        public static bool IsWatchOS() =>
#if TARGET_WATCHOS
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the watchOS version (returned by 'libobjc.get_operatingSystemVersion') with a >= version comparison. Used to guard APIs that were added in the given watchOS release.
        /// </summary>
        public static bool IsWatchOSVersionAtLeast(int major, int minor = 0, int build = 0)
            => IsWatchOS() && IsOSVersionAtLeast(major, minor, build, 0);

        /// <summary>
        /// Indicates whether the current application is running on Windows.
        /// </summary>
        [NonVersionable]
        public static bool IsWindows() =>
#if TARGET_WINDOWS
            true;
#else
            false;
#endif

        /// <summary>
        /// Check for the Windows version (returned by 'RtlGetVersion') with a >= version comparison. Used to guard APIs that were added in the given Windows release.
        /// </summary>
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
