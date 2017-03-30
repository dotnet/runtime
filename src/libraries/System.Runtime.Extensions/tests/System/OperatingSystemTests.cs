// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Tests
{
    public static class OperatingSystemTests
    {
        [Theory]
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
            Assert.Throws<ArgumentOutOfRangeException>("platform", () => new OperatingSystem((PlatformID)(-1), new Version(1, 2)));
            Assert.Throws<ArgumentOutOfRangeException>("platform", () => new OperatingSystem((PlatformID)42, new Version(1, 2)));
            Assert.Throws<ArgumentNullException>("version", () => new OperatingSystem(PlatformID.Unix, null));
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
        public static void SerializeDeserialize()
        {
            var os = new OperatingSystem(PlatformID.WinCE, new Version(5, 6, 7, 8));
            var os2 = BinaryFormatterHelpers.Clone(os);
            Assert.Equal(os.Platform, os2.Platform);
            Assert.Equal(os.ServicePack, os2.ServicePack);
            Assert.Equal(os.Version, os2.Version);
            Assert.Equal(os.VersionString, os2.VersionString);
        }

        [Fact]
        public static void GetObjectData_InvalidArgs_Throws()
        {
            var os = new OperatingSystem(PlatformID.Win32NT, new Version(10, 0));
            Assert.Throws<ArgumentNullException>("info", () => os.GetObjectData(null, new StreamingContext()));
        }
    }
}
