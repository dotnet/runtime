// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class OperatingSystemTests
    {
        private static readonly string[] AllKnownPlatformNames = new[]
        {
            "Android",
            "macOS",
            "MacCatalyst",
            "iOS",
            "tvOS",
            "watchOS",
            "Windows",
            "Linux",
            "FreeBSD",
            "Browser"
        };

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
        public static void TestIsOSPlatform_Browser() => TestIsOSPlatform("BROWSER", OperatingSystem.IsBrowser);

        [Fact, PlatformSpecific(TestPlatforms.Browser)]
        public static void TestIsOSVersionAtLeast_Browser() => TestIsOSVersionAtLeast("BROWSER");

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        public static void TestIsOSPlatform_Linux() => TestIsOSPlatform("Linux", OperatingSystem.IsLinux);

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        public static void TestIsOSVersionAtLeast_Linux() => TestIsOSVersionAtLeast("Linux");

        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]
        public static void TestIsOSPlatform_FreeBSD() => TestIsOSPlatform("FreeBSD", OperatingSystem.IsFreeBSD);

        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]
        public static void TestIsOSVersionAtLeast_FreeBSD() => TestIsOSVersionAtLeast("FreeBSD");

        [Fact, PlatformSpecific(TestPlatforms.Android)]
        public static void TestIsOSPlatform_Android() => TestIsOSPlatform("Android", OperatingSystem.IsAndroid);

        [Fact, PlatformSpecific(TestPlatforms.Android)]
        public static void TestIsOSVersionAtLeast_Android() => TestIsOSVersionAtLeast("Android");

        [Fact, PlatformSpecific(TestPlatforms.Android)]
        public static void TestIsOSVersionAtLeast_Android_21() => Assert.True(OperatingSystem.IsAndroidVersionAtLeast(21)); // 21 is our min supported version

        [Fact, PlatformSpecific(TestPlatforms.iOS)]
        public static void TestIsOSPlatform_IOS() => TestIsOSPlatform("iOS", OperatingSystem.IsIOS);

        [Fact, PlatformSpecific(TestPlatforms.iOS)]
        public static void TestIsOSVersionAtLeast_IOS() => TestIsOSVersionAtLeast("iOS");

        [Fact, PlatformSpecific(TestPlatforms.OSX)]
        public static void TestIsOSPlatform_MacOS() => TestIsOSPlatform("macOS", OperatingSystem.IsMacOS);

        [Fact, PlatformSpecific(TestPlatforms.OSX)]
        public static void TestIsOSVersionAtLeast_MacOS() => TestIsOSVersionAtLeast("macOS");

        [Fact, PlatformSpecific(TestPlatforms.OSX)]
        public static void OSX_Is_Treated_as_macOS()
        {
            // we prefer "macOS", but still accept "OSX"

            Assert.True(OperatingSystem.IsOSPlatform("OSX"));

            AssertVersionChecks(true, (major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("OSX", major, minor, build, revision));
            AssertVersionChecks(true, (major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("osx", major, minor, build, revision));
            AssertVersionChecks(true, (major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("OSX", major, minor, build));
            AssertVersionChecks(true, (major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("osx", major, minor, build));
        }

        [Fact, PlatformSpecific(TestPlatforms.MacCatalyst)]
        public static void TestIsOSPlatform_MacCatalyst() => TestIsOSPlatform("MacCatalyst", OperatingSystem.IsMacCatalyst);

        [Fact, PlatformSpecific(TestPlatforms.MacCatalyst)]
        public static void TestIsOSVersionAtLeast_MacCatalyst() => TestIsOSVersionAtLeast("MacCatalyst");

        [Fact, PlatformSpecific(TestPlatforms.MacCatalyst)]
        public static void MacCatalyst_Is_Also_iOS()
        {
            Assert.True(OperatingSystem.IsOSPlatform("IOS"));
            Assert.True(OperatingSystem.IsIOS());

            AssertVersionChecks(true, (major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build, revision));
            AssertVersionChecks(true, (major, minor, build) => OperatingSystem.IsOSPlatformVersionAtLeast("IOS", major, minor, build));
        }

        [Fact, PlatformSpecific(TestPlatforms.iOS)]
        public static void IOS_Is_Not_Also_MacCatalyst()
        {
            Assert.False(OperatingSystem.IsOSPlatform("MacCatalyst"));
            Assert.False(OperatingSystem.IsMacCatalyst());
        }

        [Fact, PlatformSpecific(TestPlatforms.tvOS)]
        public static void TestIsOSPlatform_TvOS() => TestIsOSPlatform("tvOS", OperatingSystem.IsTvOS);

        [Fact, PlatformSpecific(TestPlatforms.tvOS)]
        public static void TestIsOSVersionAtLeast_TvOS() => TestIsOSVersionAtLeast("tvOS");

        [Fact, PlatformSpecific(TestPlatforms.Windows)]
        public static void TestIsOSPlatform_Windows() => TestIsOSPlatform("Windows", OperatingSystem.IsWindows);

        [Fact, PlatformSpecific(TestPlatforms.Windows)]
        public static void TestIsOSVersionAtLeast_Windows() => TestIsOSVersionAtLeast("Windows");

        private static void TestIsOSPlatform(string currentOSName, Func<bool> currentOSCheck)
        {
            foreach (string platformName in AllKnownPlatformNames)
            {
                bool expected = currentOSName.Equals(platformName, StringComparison.OrdinalIgnoreCase);

                Assert.Equal(expected, OperatingSystem.IsOSPlatform(platformName));
                Assert.Equal(expected, OperatingSystem.IsOSPlatform(platformName.ToUpper()));
                Assert.Equal(expected, OperatingSystem.IsOSPlatform(platformName.ToLower()));
            }

            Assert.True(currentOSCheck());

            bool[] allResults = new bool[]
            {
                OperatingSystem.IsBrowser(),
                OperatingSystem.IsLinux(),
                OperatingSystem.IsFreeBSD(),
                OperatingSystem.IsAndroid(),
                OperatingSystem.IsIOS(),
                OperatingSystem.IsMacOS(),
                OperatingSystem.IsMacCatalyst(),
                OperatingSystem.IsTvOS(),
                OperatingSystem.IsWatchOS(),
                OperatingSystem.IsWindows()
            };

            Assert.Single(allResults, true);
        }

        private static void TestIsOSVersionAtLeast(string currentOSName)
        {
            foreach (string platformName in AllKnownPlatformNames)
            {
                bool isCurrentOS = currentOSName.Equals(platformName, StringComparison.OrdinalIgnoreCase);

                AssertVersionChecks(isCurrentOS, (major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast(platformName, major, minor, build, revision));
                AssertVersionChecks(isCurrentOS, (major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast(platformName.ToLower(), major, minor, build, revision));
                AssertVersionChecks(isCurrentOS, (major, minor, build, revision) => OperatingSystem.IsOSPlatformVersionAtLeast(platformName.ToUpper(), major, minor, build, revision));
            }
            
            AssertVersionChecks(currentOSName.Equals("Android", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsAndroidVersionAtLeast);
            AssertVersionChecks(currentOSName.Equals("iOS", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsIOSVersionAtLeast);
            AssertVersionChecks(currentOSName.Equals("macOS", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsMacOSVersionAtLeast);
            AssertVersionChecks(currentOSName.Equals("MacCatalyst", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsMacCatalystVersionAtLeast);
            AssertVersionChecks(currentOSName.Equals("tvOS", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsTvOSVersionAtLeast);
            AssertVersionChecks(currentOSName.Equals("watchOS", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsWatchOSVersionAtLeast);
            AssertVersionChecks(currentOSName.Equals("Windows", StringComparison.OrdinalIgnoreCase), OperatingSystem.IsWindowsVersionAtLeast);
        }

        private static void AssertVersionChecks(bool isCurrentOS, Func<int, int, int, int, bool> isOSVersionAtLeast)
        {
            Version current = Environment.OSVersion.Version;

            Assert.False(isOSVersionAtLeast(current.Major + 1, current.Minor, current.Build, current.Revision));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor + 1, current.Build, current.Revision));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor, current.Build + 1, current.Revision));
            Assert.False(isOSVersionAtLeast(current.Major, current.Minor, current.Build, Math.Max(current.Revision + 1, 1))); // OSX Revision reports -1

            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build, current.Revision));

            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major - 1, current.Minor, current.Build, current.Revision));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor - 1, current.Build, current.Revision));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build - 1, current.Revision));
            Assert.Equal(isCurrentOS, isOSVersionAtLeast(current.Major, current.Minor, current.Build, current.Revision - 1));
        }

        private static void AssertVersionChecks(bool isCurrentOS, Func<int, int, int, bool> isOSVersionAtLeast)
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
