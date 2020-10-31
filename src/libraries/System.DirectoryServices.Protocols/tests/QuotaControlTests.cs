// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class QuotaControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new QuotaControl();
            Assert.True(control.IsCritical);
            Assert.Null(control.QuerySid);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1852", control.Type);

            var expected = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 } : new byte[] { 48, 2, 4, 0 };
            Assert.Equal(expected, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_QuerySid_TestData()
        {
            yield return new object[] { new SecurityIdentifier("S-1-5-32-544"), new byte[] { 48, 132, 0, 0, 0, 18, 4, 16, 1, 2, 0, 0, 0, 0, 0, 5, 32, 0, 0, 0, 32, 2, 0, 0 } };
            yield return new object[] { null, new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 } };
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] //Security Identifiers only work on Windows
        [MemberData(nameof(Ctor_QuerySid_TestData))]
        public void Ctor_QuerySid_Test(SecurityIdentifier querySid, byte[] expectedValue)
        {
            var control = new QuotaControl(querySid);
            Assert.True(control.IsCritical);
            if (querySid != null)
            {
                Assert.NotSame(querySid, control.QuerySid);
            }
            Assert.Equal(querySid, control.QuerySid);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1852", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }
    }
}
