// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class PlatformVersionTests
    {
        public static IEnumerable<object[]> AllKnownOsPlatforms()
        {
            yield return new object[] { OSPlatform.Windows };
            yield return new object[] { OSPlatform.Linux };
            yield return new object[] { OSPlatform.OSX };
            yield return new object[] { OSPlatform.Browser };
            yield return new object[] { OSPlatform.macOS };
            yield return new object[] { OSPlatform.iOS };
            yield return new object[] { OSPlatform.tvOS };
            yield return new object[] { OSPlatform.watchOS };
            yield return new object[] { OSPlatform.Android };
        }

        [Fact]
        public void IsOSPlatformOrLater_Null_ThrowsArgumentNullExceptionWithArgumentName()
        {
            Assert.Throws<ArgumentNullException>("platformName", () => RuntimeInformation.IsOSPlatformOrLater(null));
        }

        [Fact]
        public void IsOSPlatformOrLater_Empty_ThrowsArgumentNullExceptionWithArgumentName()
        {
            Assert.Throws<ArgumentException>("platformName", () => RuntimeInformation.IsOSPlatformOrLater(string.Empty));
        }

        [Theory]
        [InlineData("ios")] // missing version number
        [InlineData("ios14")] // ios14.0 is fine, ios14 is not: https://github.com/dotnet/runtime/pull/39005#discussion_r452541491
        [InlineData("ios14.0.0.0.0.0")] // too many numbers
        [InlineData("ios14.0.")] // version should not end with dot (but OS name could potentially end with dot, imagine "NET.")
        [InlineData("numbers1.2inplatformname1.2")] // numbers in platform names are not supported https://github.com/dotnet/runtime/pull/39005#discussion_r452644601
        public void IsOSPlatformOrLater_InvalidVersionNumber_ThrowsArgumentExceptionWithArgumentName(string platformName)
        {
            Assert.Throws<ArgumentException>("platformName", () => RuntimeInformation.IsOSPlatformOrLater(platformName));
        }

        [Theory]
        [MemberData(nameof(AllKnownOsPlatforms))]
        public void IsOSPlatformOrLater_ReturnsTrue_ForCurrentOS(OSPlatform osPlatform)
        {
            // IsOSPlatformOrLater("xyz1.2.3.4") running as "xyz1.2.3.4" should return true

            bool isCurrentPlatfom = RuntimeInformation.IsOSPlatform(osPlatform);
            Version current = Environment.OSVersion.Version;

            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{current}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform.ToString().ToLower()}{current}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform.ToString().ToUpper()}{current}"));

            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, current.Major));

            if (current.Minor >= 0)
            {
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, current.Major, current.Minor));

                if (current.Build >= 0)
                {
                    Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, current.Major, current.Minor, current.Build));

                    if (current.Revision >= 0)
                    {
                        Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, current.Major, current.Minor, current.Build, current.Revision));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllKnownOsPlatforms))]
        public void IsOSPlatformOrLater_ReturnsFalse_ForNewerVersionOfCurrentOS(OSPlatform osPlatform)
        {
            // IsOSPlatformOrLater("xyz11.0") running as "xyz10.0" should return false

            Version currentVersion = Environment.OSVersion.Version;

            Version newer = new Version(currentVersion.Major + 1, 0);
            Assert.False(RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{newer}"));
            Assert.False(RuntimeInformation.IsOSPlatformOrLater($"{osPlatform.ToString().ToLower()}{newer}"));
            Assert.False(RuntimeInformation.IsOSPlatformOrLater($"{osPlatform.ToString().ToUpper()}{newer}"));
            Assert.False(RuntimeInformation.IsOSPlatformOrLater(osPlatform, newer.Major));

            newer = new Version(currentVersion.Major, currentVersion.Minor + 1);
            Assert.False(RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{newer}"));
            Assert.False(RuntimeInformation.IsOSPlatformOrLater(osPlatform, newer.Major, newer.Minor));

            newer = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1);
            Assert.False(RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{newer}"));
            Assert.False(RuntimeInformation.IsOSPlatformOrLater(osPlatform, newer.Major, newer.Minor, newer.Build));

            newer = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build, currentVersion.Revision + 1);
            Assert.False(RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{newer}"));
            Assert.False(RuntimeInformation.IsOSPlatformOrLater(osPlatform, newer.Major, newer.Minor, newer.Build, newer.Revision));
        }

        [Theory]
        [MemberData(nameof(AllKnownOsPlatforms))]
        public void IsOSPlatformOrLater_ReturnsTrue_ForOlderVersionOfCurrentOS(OSPlatform osPlatform)
        {
            // IsOSPlatformOrLater("xyz10.0") running as "xyz11.0" should return true

            bool isCurrentPlatfom = RuntimeInformation.IsOSPlatform(osPlatform);
            Version current = Environment.OSVersion.Version;

            Version older = new Version(current.Major - 1, 0);
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{older}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform.ToString().ToLower()}{older}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform.ToString().ToUpper()}{older}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, older.Major));

            if (current.Minor > 0)
            {
                older = new Version(current.Major, current.Minor - 1);
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{older}"));
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, older.Major, older.Minor));
            }

            if (current.Build > 0)
            {
                older = new Version(current.Major, current.Minor, current.Build - 1);
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{older}"));
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, older.Major, older.Minor, older.Build));
            }

            if (current.Revision > 0)
            {
                older = new Version(current.Major, current.Minor, current.Build, current.Revision - 1);
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater($"{osPlatform}{older}"));
                Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformOrLater(osPlatform, older.Major, older.Minor, older.Build, older.Revision));
            }
        }

        [Fact]
        public void IsOSPlatformEarlierThan_Null_ThrowsArgumentNullExceptionWithArgumentName()
        {
            Assert.Throws<ArgumentNullException>("platformName", () => RuntimeInformation.IsOSPlatformEarlierThan(null));
        }

        [Fact]
        public void IsOSPlatformEarlierThan_Empty_ThrowsArgumentNullExceptionWithArgumentName()
        {
            Assert.Throws<ArgumentException>("platformName", () => RuntimeInformation.IsOSPlatformEarlierThan(string.Empty));
        }

        [Theory]
        [InlineData("ios")] // missing version number
        [InlineData("ios14")] // ios14.0 is fine, ios14 is not: https://github.com/dotnet/runtime/pull/39005#discussion_r452541491
        [InlineData("ios14.0.0.0.0.0")] // too many numbers
        [InlineData("ios14.0.")] // version should not end with dot (but OS name could potentially end with dot, imagine "NET.")
        [InlineData("numbers1.2inplatformname1.2")] // numbers in platform names are not supported https://github.com/dotnet/runtime/pull/39005#discussion_r452644601
        public void IsOSPlatformEarlierThan_InvalidVersionNumber_ThrowsArgumentExceptionWithArgumentName(string platformName)
        {
            Assert.Throws<ArgumentException>("platformName", () => RuntimeInformation.IsOSPlatformEarlierThan(platformName));
        }

        [Theory]
        [MemberData(nameof(AllKnownOsPlatforms))]
        public void IsOSPlatformEarlierThan_ReturnsFalse_ForCurrentOS(OSPlatform osPlatform)
        {
            // IsOSPlatformEarlierThan("xyz1.2.3.4") running as "xyz1.2.3.4" should return false

            Version current = Environment.OSVersion.Version;

            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{current}"));
            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform.ToString().ToLower()}{current}"));
            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform.ToString().ToUpper()}{current}"));

            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, current.Major));

            if (current.Minor >= 0)
            {
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, current.Major, current.Minor));

                if (current.Build >= 0)
                {
                    Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, current.Major, current.Minor, current.Build));

                    if (current.Revision >= 0)
                    {
                        Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, current.Major, current.Minor, current.Build, current.Revision));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllKnownOsPlatforms))]
        public void IsOSPlatformEarlierThan_ReturnsTrue_ForNewerVersionOfCurrentOS(OSPlatform osPlatform)
        {
            // IsOSPlatformEarlierThan("xyz11.0") running as "xyz10.0" should return true

            bool isCurrentPlatfom = RuntimeInformation.IsOSPlatform(osPlatform);
            Version current = Environment.OSVersion.Version;

            Version newer = new Version(current.Major + 1, 0);
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{newer}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform.ToString().ToLower()}{newer}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform.ToString().ToUpper()}{newer}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, newer.Major));

            newer = new Version(current.Major, current.Minor + 1);
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{newer}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, newer.Major, newer.Minor));

            newer = new Version(current.Major, current.Minor, current.Build + 1);
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{newer}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, newer.Major, newer.Minor, newer.Build));

            newer = new Version(current.Major, current.Minor, current.Build, current.Revision + 1);
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{newer}"));
            Assert.Equal(isCurrentPlatfom, RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, newer.Major, newer.Minor, newer.Build, newer.Revision));
        }

        [Theory]
        [MemberData(nameof(AllKnownOsPlatforms))]
        public void IsOSPlatformEarlierThan_ReturnsFalse_ForOlderVersionOfCurrentOS(OSPlatform osPlatform)
        {
            // IsOSPlatformEarlierThan("xyz10.0") running as "xyz11.0" should return false

            Version current = Environment.OSVersion.Version;

            Version older = new Version(current.Major - 1, 0);
            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{older}"));
            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform.ToString().ToLower()}{older}"));
            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform.ToString().ToUpper()}{older}"));
            Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, older.Major));

            if (current.Minor > 0)
            {
                older = new Version(current.Major, current.Minor - 1);
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{older}"));
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, older.Major, older.Minor));
            }

            if (current.Build > 0)
            {
                older = new Version(current.Major, current.Minor, current.Build - 1);
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{older}"));
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, older.Major, older.Minor, older.Build));
            }

            if (current.Revision > 0)
            {
                older = new Version(current.Major, current.Minor, current.Build, current.Revision - 1);
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan($"{osPlatform}{older}"));
                Assert.False(RuntimeInformation.IsOSPlatformEarlierThan(osPlatform, older.Major, older.Minor, older.Build, older.Revision));
            }
        }
    }
}
