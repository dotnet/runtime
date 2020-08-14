// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class ExtendedDNControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new ExtendedDNControl();
            Assert.True(control.IsCritical);
            Assert.Equal(ExtendedDNFlag.HexString, control.Flag);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.529", control.Type);

            var expected = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 0 } : new byte[] { 48, 3, 2, 1, 0 };
            Assert.Equal(expected, control.GetValue());
        }

        [Fact]
        public void Ctor_Flag()
        {
            var control = new ExtendedDNControl(ExtendedDNFlag.StandardString);
            Assert.True(control.IsCritical);
            Assert.Equal(ExtendedDNFlag.StandardString, control.Flag);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.529", control.Type);

            var expected = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 1 } : new byte[] { 48, 3, 2, 1, 1 };
            Assert.Equal(expected, control.GetValue());
        }

        [Theory]
        [InlineData(ExtendedDNFlag.HexString - 1)]
        [InlineData(ExtendedDNFlag.StandardString + 1)]
        public void Ctor_InvalidFlag_ThrowsInvalidEnumArgumentException(ExtendedDNFlag flag)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => new ExtendedDNControl(flag));
        }
    }
}
