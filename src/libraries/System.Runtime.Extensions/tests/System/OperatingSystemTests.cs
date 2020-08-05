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

        [Fact, PlatformSpecific(TestPlatforms.Browser)]
        public static void CheckBrowser()
        {
            Assert.True(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        public static void CheckLinux()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.True(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]
        public static void CheckFreeBSD()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.True(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, true);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.Android)]
        public static void CheckAndroid()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.True(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, true);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.iOS)]
        public static void CheckIOS()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, true);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.OSX)]
        public static void CheckMacOS()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.True(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, true);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.tvOS)]
        public static void CheckTvOS()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.True(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.False(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, true);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, false);
        }

        [Fact, PlatformSpecific(TestPlatforms.Windows)]
        public static void CheckWindows()
        {
            Assert.False(OperatingSystem.IsBrowser());
            Assert.False(OperatingSystem.IsLinux());
            Assert.False(OperatingSystem.IsFreeBSD());
            Assert.False(OperatingSystem.IsAndroid());
            Assert.False(OperatingSystem.IsIOS());
            Assert.False(OperatingSystem.IsMacOS());
            Assert.False(OperatingSystem.IsTvOS());
            Assert.False(OperatingSystem.IsWatchOS());
            Assert.True(OperatingSystem.IsWindows());

            AssertVersionChecks(OperatingSystem.IsAndroidVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsFreeBSDVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsIOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsMacOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsTvOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWatchOSVersionAtLeast, false);
            AssertVersionChecks(OperatingSystem.IsWindowsVersionAtLeast, true);
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
