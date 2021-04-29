// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49105", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class DirSyncRequestControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new DirSyncRequestControl();
            Assert.Equal(1048576, control.AttributeCount);
            Assert.Empty(control.Cookie);
            Assert.Equal(DirectorySynchronizationOptions.None, control.Option);

            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.841", control.Type);

            var expected = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } : new byte[] { 48, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 };
            Assert.Equal(expected, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_Cookie_Data()
        {
            yield return new object[] { null, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } : new byte[] { 48, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } };
            yield return new object[] { new byte[0], (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } : new byte[] { 48, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } };
            yield return new object[] { new byte[] { 97, 98, 99 }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 13, 2, 1, 0, 2, 3, 16, 0, 0, 4, 3, 97, 98, 99 } : new byte[] { 48, 13, 2, 1, 0, 2, 3, 16, 0, 0, 4, 3, 97, 98, 99 } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Cookie_Data))]
        public void Ctor_Cookie(byte[] cookie, byte[] expectedValue)
        {
            var control = new DirSyncRequestControl(cookie);
            Assert.Equal(1048576, control.AttributeCount);
            Assert.Equal(cookie ?? Array.Empty<byte>(), control.Cookie);
            Assert.Equal(DirectorySynchronizationOptions.None, control.Option);

            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.841", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_Cookie_Options_Data()
        {
            yield return new object[] { null, DirectorySynchronizationOptions.None, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } : new byte[] { 48, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } };
            yield return new object[] { new byte[0], DirectorySynchronizationOptions.None - 1, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 13, 2, 4, 255, 255, 255, 255, 2, 3, 16, 0, 0, 4, 0 } : new byte[] { 48, 10, 2, 1, 255, 2, 3, 16, 0, 0, 4, 0 } };
            yield return new object[] { new byte[] { 97, 98, 99 }, DirectorySynchronizationOptions.ObjectSecurity, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 13, 2, 1, 1, 2, 3, 16, 0, 0, 4, 3, 97, 98, 99 } : new byte[] { 48, 13, 2, 1, 1, 2, 3, 16, 0, 0, 4, 3, 97, 98, 99 } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Cookie_Options_Data))]
        public void Ctor_Cookie_Options(byte[] cookie, DirectorySynchronizationOptions option, byte[] expectedValue)
        {
            var control = new DirSyncRequestControl(cookie, option);
            Assert.Equal(1048576, control.AttributeCount);
            Assert.Equal(cookie ?? Array.Empty<byte>(), control.Cookie);
            Assert.Equal(option, control.Option);

            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.841", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_Cookie_Options_AttributeCount_Data()
        {
            yield return new object[] { null, DirectorySynchronizationOptions.None, 1048576, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } : new byte[] { 48, 10, 2, 1, 0, 2, 3, 16, 0, 0, 4, 0 } };
            yield return new object[] { new byte[0], DirectorySynchronizationOptions.None - 1, 0, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 11, 2, 4, 255, 255, 255, 255, 2, 1, 0, 4, 0 } : new byte[] { 48, 8, 2, 1, 255, 2, 1, 0, 4, 0 } };
            yield return new object[] { new byte[] { 97, 98, 99 }, DirectorySynchronizationOptions.ObjectSecurity, 10, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 11, 2, 1, 1, 2, 1, 10, 4, 3, 97, 98, 99 } : new byte[] { 48, 11, 2, 1, 1, 2, 1, 10, 4, 3, 97, 98, 99 } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Cookie_Options_AttributeCount_Data))]
        public void Ctor_Cookie_Options_AttributeCount(byte[] cookie, DirectorySynchronizationOptions option, int attributeCount , byte[] expectedValue)
        {
            var control = new DirSyncRequestControl(cookie, option, attributeCount);
            Assert.Equal(attributeCount, control.AttributeCount);
            Assert.Equal(cookie ?? Array.Empty<byte>(), control.Cookie);
            Assert.Equal(option, control.Option);

            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.841", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void Ctor_NegativeAttributeCount_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("value", () => new DirSyncRequestControl(new byte[0], DirectorySynchronizationOptions.None, -1));
        }

        [Fact]
        public void AttributeCount_SetValid_GetReturnsExpected()
        {
            var control = new DirSyncRequestControl { AttributeCount = 0 };
            Assert.Equal(0, control.AttributeCount);
        }

        [Fact]
        public void AttributeCount_SetNegative_ThrowsArgumentException()
        {
            var control = new DirSyncRequestControl();
            AssertExtensions.Throws<ArgumentException>("value", () => control.AttributeCount = -1);
        }

        [Fact]
        public void Cookie_Set_GetReturnsExpected()
        {
            byte[] cookie = new byte[] { 1, 2, 3 };
            var control = new DirSyncRequestControl { Cookie = cookie };
            Assert.NotSame(cookie, control.Cookie);
            Assert.Equal(cookie, control.Cookie);
        }
    }
}
