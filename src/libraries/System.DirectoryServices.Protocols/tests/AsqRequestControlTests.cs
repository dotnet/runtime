// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49105", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class AsqRequestControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new AsqRequestControl();
            Assert.Null(control.AttributeName);
            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1504", control.Type);

            var expected = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 } : new byte[] { 48, 2, 4, 0 };

            Assert.Equal(expected, control.GetValue());
        }

        public static IEnumerable<object[]> Ctor_String_Test_data()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { null, new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 } };
                yield return new object[] { "", new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 } };
                yield return new object[] { "A", new byte[] { 48, 132, 0, 0, 0, 3, 4, 1, 65 } };
            }
            else
            {
                yield return new object[] { null, new byte[] { 48, 2, 4, 0 } };
                yield return new object[] { "", new byte[] { 48, 2, 4, 0 } };
                yield return new object[] { "A", new byte[] { 48, 3, 4, 1, 65 } };
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Test_data))]
        public void Ctor_String(string attributeName, byte[] expectedValue)
        {
            var control = new AsqRequestControl(attributeName);
            Assert.Equal(attributeName, control.AttributeName);
            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1504", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void AttributeName_Set_GetReturnsExpected()
        {
            var control = new AsqRequestControl { AttributeName = "Name" };
            Assert.Equal("Name", control.AttributeName);
        }
    }
}
