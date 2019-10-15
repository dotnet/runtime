// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class SecurityDescriptorFlagControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new SecurityDescriptorFlagControl();
            Assert.True(control.IsCritical);
            Assert.Equal(SecurityMasks.None, control.SecurityMasks);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.801", control.Type);

            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 0 }, control.GetValue());
        }

        [Theory]
        [InlineData(SecurityMasks.Group, new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 2 })]
        [InlineData(SecurityMasks.None - 1, new byte[] { 48, 132, 0, 0, 0, 6, 2, 4, 255, 255, 255, 255 })]
        public void Ctor_Flags(SecurityMasks masks, byte[] expectedValue)
        {
            var control = new SecurityDescriptorFlagControl(masks);
            Assert.True(control.IsCritical);
            Assert.Equal(masks, control.SecurityMasks);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.801", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }
    }
}
