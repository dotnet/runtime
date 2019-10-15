// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
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

            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 0 }, control.GetValue());
        }

        [Fact]
        public void Ctor_Flag()
        {
            var control = new ExtendedDNControl(ExtendedDNFlag.StandardString);
            Assert.True(control.IsCritical);
            Assert.Equal(ExtendedDNFlag.StandardString, control.Flag);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.529", control.Type);

            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 1 }, control.GetValue());
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
