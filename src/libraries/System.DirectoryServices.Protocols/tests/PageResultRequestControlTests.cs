// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class PageResultRequestControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new PageResultRequestControl();
            Assert.True(control.IsCritical);
            Assert.Empty(control.Cookie);
            Assert.True(control.ServerSide);
            Assert.Equal(512, control.PageSize);
            Assert.Equal("1.2.840.113556.1.4.319", control.Type);

            var expected = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 6, 2, 2, 2, 0, 4, 0 } : new byte[] { 48, 6, 2, 2, 2, 0, 4, 0 };
            Assert.Equal(expected, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_PageSize_Data()
        {
            yield return new object[] { 0, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 5, 2, 1, 0, 4, 0 } : new byte[] { 48, 5, 2, 1, 0, 4, 0 } };
            yield return new object[] { 10, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 5, 2, 1, 10, 4, 0 } : new byte[] { 48, 5, 2, 1, 10, 4, 0 } };
        }

        [Theory]
        [MemberData(nameof(Ctor_PageSize_Data))]
        public void Ctor_PageSize(int pageSize, byte[] expectedValue)
        {
            var control = new PageResultRequestControl(pageSize);
            Assert.True(control.IsCritical);
            Assert.Empty(control.Cookie);
            Assert.True(control.ServerSide);
            Assert.Equal(pageSize, control.PageSize);
            Assert.Equal("1.2.840.113556.1.4.319", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void Ctor_NegativePageSize_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("value", () => new PageResultRequestControl(-1));
        }

        public static IEnumerable<object[]> Ctor_Cookie_Data()
        {
            yield return new object[] { null, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 6, 2, 2, 2, 0, 4, 0 } : new byte[] { 48, 6, 2, 2, 2, 0, 4, 0 } };
            yield return new object[] { new byte[0], (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 6, 2, 2, 2, 0, 4, 0 } : new byte[] { 48, 6, 2, 2, 2, 0, 4, 0 } };
            yield return new object[] { new byte[] { 1, 2, 3, }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 9, 2, 2, 2, 0, 4, 3, 1, 2, 3 } : new byte[] { 48, 9, 2, 2, 2, 0, 4, 3, 1, 2, 3 } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Cookie_Data))]
        public void Ctor_Cookie(byte[] cookie, byte[] expectedValue)
        {
            var control = new PageResultRequestControl(cookie);
            Assert.True(control.IsCritical);
            Assert.NotSame(cookie, control.Cookie);
            Assert.Equal(cookie ?? Array.Empty<byte>(), control.Cookie);
            Assert.True(control.ServerSide);
            Assert.Equal(512, control.PageSize);
            Assert.Equal("1.2.840.113556.1.4.319", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void Cookie_Set_GetReturnsExpected()
        {
            var control = new PageResultRequestControl { Cookie = new byte[] { 1, 2, 3 } };
            Assert.Equal(new byte[] { 1, 2, 3 }, control.Cookie);
        }
    }
}
