// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
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

#if NETFRAMEWORK
            var expected = new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 0 };
#else
            var expected = new byte[] { 48, 3, 2, 1, 0 };
#endif
            Assert.Equal(expected, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_Flags_Data()
        {
#if NETFRAMEWORK
            yield return new object[] { SecurityMasks.Group, new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 2 } };
            yield return new object[] { SecurityMasks.None - 1, new byte[] { 48, 132, 0, 0, 0, 6, 2, 4, 255, 255, 255, 255 } };
#else
            yield return new object[] { SecurityMasks.Group, new byte[] { 48, 3, 2, 1, 2 } };
            yield return new object[] { SecurityMasks.None - 1, new byte[] { 48, 3, 2, 1, 255 } };
#endif
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
