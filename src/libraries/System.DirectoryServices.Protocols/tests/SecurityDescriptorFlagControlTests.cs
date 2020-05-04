// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class SecurityDescriptorFlagControlTests
    {
        public static bool RunningOnWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

        [Fact]
        public void Ctor_Default()
        {
            var control = new SecurityDescriptorFlagControl();
            Assert.True(control.IsCritical);
            Assert.Equal(SecurityMasks.None, control.SecurityMasks);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.801", control.Type);

            var expected = (RunningOnWindows) ? new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 0 } : new byte[] { 48, 3, 2, 1, 0 };
            Assert.Equal(expected, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_Flags_Data()
        {
            yield return new object[] { SecurityMasks.Group, (RunningOnWindows) ? new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 2 } : new byte[] { 48, 3, 2, 1, 2 } };
            yield return new object[] { SecurityMasks.None - 1, (RunningOnWindows) ? new byte[] { 48, 132, 0, 0, 0, 6, 2, 4, 255, 255, 255, 255 } : new byte[] { 48, 3, 2, 1, 255 } };
        }

        [Theory]
        [MemberData(nameof(Ctor_Flags_Data))]
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
