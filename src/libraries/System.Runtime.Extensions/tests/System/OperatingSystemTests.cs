// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class OperatingSystemTests
    {
        [Theory]
        [InlineData(PlatformID.Other, "1.0.0.0")]
        [InlineData(PlatformID.MacOSX, "1.2")]
        [InlineData(PlatformID.Unix, "1.2.3")]
        [InlineData(PlatformID.Win32NT, "1.2.3.4")]
        [InlineData(PlatformID.Win32S, "5.6")]
        [InlineData(PlatformID.Win32Windows, "5.6.7")]
        [InlineData(PlatformID.Win32Windows, "4.1")]
        [InlineData(PlatformID.Win32Windows, "4.0")]
        [InlineData(PlatformID.Win32Windows, "3.9")]
        [InlineData(PlatformID.WinCE, "5.6.7.8")]
        [InlineData(PlatformID.Xbox, "9.10")]
        public static void Ctor(PlatformID id, string versionString)
        {
            var os = new OperatingSystem(id, new Version(versionString));
            Assert.Equal(id, os.Platform);
            Assert.Equal(new Version(versionString), os.Version);
            Assert.Equal(string.Empty, os.ServicePack);
            Assert.NotEmpty(os.VersionString);
            Assert.Equal(os.VersionString, os.ToString());
        }

        [Fact]
        public static void Ctor_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("platform", () => new OperatingSystem((PlatformID)(-1), new Version(1, 2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("platform", () => new OperatingSystem((PlatformID)42, new Version(1, 2)));
            AssertExtensions.Throws<ArgumentNullException>("version", () => new OperatingSystem(PlatformID.Unix, null));
        }

        [Fact]
        public static void Clone()
        {
            var os = new OperatingSystem(PlatformID.Xbox, new Version(1, 2, 3, 4));
            var os2 = (OperatingSystem)os.Clone();
            Assert.Equal(os.Platform, os2.Platform);
            Assert.Equal(os.ServicePack, os2.ServicePack);
            Assert.Equal(os.Version, os2.Version);
            Assert.Equal(os.VersionString, os2.VersionString);
        }

        [Fact]
        public static void IsOSPlatform_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("platform", () => OperatingSystem.IsOSPlatform(null));
        }

        [Fact]
        public static void IsOSPlatformVersionAtLeast_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("platform", () => OperatingSystem.IsOSPlatformVersionAtLeast(null, 1));
        }

        [Fact, PlatformSpecific(TestPlatforms.Browser)]
        public static void CheckBrowser()
        {
            Assert.True(OperatingSystem.IsBrowser());
            Assert.True(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.True(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        public static void CheckLinux()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.True(OperatingSystem.IsLinux());
            Assert.True(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.True(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), true);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]
        public static void CheckFreeBSD()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.True(OperatingSystem.IsFreeBSD());
            Assert.True(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.True(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), true);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.Android)]
        public static void CheckAndroid()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.True(OperatingSystem.IsAndroid());
            Assert.True(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.True(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), true);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.iOS)]
        public static void CheckIOS()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.True(OperatingSystem.IsOSPlatform("IOS"));
            Assert.True(OperatingSystem.IsOSPlatform("iOS"));
            Assert.True(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("iOS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("iOS", major, minor, build), true);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.OSX)]
        public static void CheckMacOS()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.True(OperatingSystem.IsMacOS());
            Assert.True(OperatingSystem.IsOSPlatform("MACOS")); // both MACOS and OSX are supported
            Assert.True(OperatingSystem.IsOSPlatform("macOS"));
            Assert.True(OperatingSystem.IsOSPlatform("macos"));
            Assert.True(OperatingSystem.IsOSPlatform("OSX"));
            Assert.True(OperatingSystem.IsOSPlatform("osx"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("macOS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("OSX", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("osx", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("macOS", major, minor, build), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("OSX", major, minor, build), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("osx", major, minor, build), true);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.tvOS)]
        public static void CheckTvOS()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.True(OperatingSystem.IsTvOS());
            Assert.True(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.True(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.True(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.False(OperatingSystem.IsWindows());
            Assert.False(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.False(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("tvOS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build), true);
            AssertVersionChecks((major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("tvOS", major, minor, build), true);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), false);
        }

        [Fact, PlatformSpecific(TestPlatforms.Windows)]
        public static void CheckWindows()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsOSPlatform("BROWSER"));
            Assert.False(OperatingSystem.IsOSPlatform("browser"));
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsOSPlatform("LINUX"));
            Assert.False(OperatingSystem.IsOSPlatform("linux"));
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsOSPlatform("FREEBSD"));
            Assert.False(OperatingSystem.IsOSPlatform("freebsd"));
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsOSPlatform("ANDROID"));
            Assert.False(OperatingSystem.IsOSPlatform("android"));
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsOSPlatform("IOS"));
            Assert.False(OperatingSystem.IsOSPlatform("iOS"));
            Assert.False(OperatingSystem.IsOSPlatform("ios"));
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsOSPlatform("MACOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macOS"));
            Assert.False(OperatingSystem.IsOSPlatform("macos"));
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsOSPlatform("TVOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvOS"));
            Assert.False(OperatingSystem.IsOSPlatform("tvos"));
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsOSPlatform("WATCHOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchOS"));
            Assert.False(OperatingSystem.IsOSPlatform("watchios"));
            Assert.True(OperatingSystem.IsWindows());
            Assert.True(OperatingSystem.IsOSPlatform("WINDOWS"));
            Assert.True(OperatingSystem.IsOSPlatform("windows"));

            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("BROWSER", major, minor, build, revision), false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("LINUX", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("ANDROID", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("FREEBSD", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("MACOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("TVOS", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WATCHTV", major, minor, build, revision), false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("WINDOWS", major, minor, build, revision), true);
            AssertVersionChecks((major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("windows", major, minor, build, revision), true);
        }

        private static void AssertVersionChecks(Func<int, int, int, int, bool> isOSVersionAtLeast, bool isCurrentOS)
        {
            Version current = Environment.OSVersion.Version;

            Assert.False(isOSVersionAtLeast(current.Major + 1, current.Minor, current.Build, current.Revision));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor + 1, current.Build, current.Revision));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor, current.Build + 1, current.Revision));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor, current.Build, current.Revision + 1));

            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build, current.Revision));

            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major - 1, current.Minor, current.Build, current.Revision));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor - 1, current.Build, current.Revision));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build - 1, current.Revision));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build, current.Revision - 1));
        }

        private static void AssertVersionChecks(Func<int, int, int, bool> isOSVersionAtLeast, bool isCurrentOS)
        {
            Version current = Environment.OSVersion.Version;

            Assert.False(isOSVersionAtLeast(current.Major + 1, current.Minor, current.Build));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor + 1, current.Build));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor, current.Build + 1));

            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build));

            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major - 1, current.Minor, current.Build));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor - 1, current.Build));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build - 1));
        }
    }
}
