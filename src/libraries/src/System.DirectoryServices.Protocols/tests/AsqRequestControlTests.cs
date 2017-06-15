// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
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
            
            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 }, control.GetValue());
        }

        [Theory]
        [InlineData(null, new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 })]
        [InlineData("", new byte[] { 48, 132, 0, 0, 0, 2, 4, 0 })]
        [InlineData("A", new byte[] { 48, 132, 0, 0, 0, 3, 4, 1, 65 })]
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
