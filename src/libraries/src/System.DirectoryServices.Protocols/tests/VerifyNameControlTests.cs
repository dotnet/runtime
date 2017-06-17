// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class VerifyNameControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new VerifyNameControl();
            Assert.True(control.IsCritical);
            Assert.Equal(0, control.Flag);
            Assert.Null(control.ServerName);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1338", control.Type);
            
            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 5, 2, 1, 0, 4, 0 }, control.GetValue());
        }

        [Theory]
        [InlineData("", new byte[] { 48, 132, 0, 0, 0, 5, 2, 1, 0, 4, 0 })]
        [InlineData("S", new byte[] { 48, 132, 0, 0, 0, 7, 2, 1, 0, 4, 2, 83, 0 })]
        public void Ctor_ServerName(string serverName, byte[] expectedValue)
        {
            var control = new VerifyNameControl(serverName);
            Assert.True(control.IsCritical);
            Assert.Equal(0, control.Flag);
            Assert.Equal(serverName, control.ServerName);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1338", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Theory]
        [InlineData("", -1, new byte[] { 48, 132, 0, 0, 0, 8, 2, 4, 255, 255, 255, 255, 4, 0 })]
        [InlineData("S", 10, new byte[] { 48, 132, 0, 0, 0, 7, 2, 1, 10, 4, 2, 83, 0 })]
        public void Ctor_ServerName_Flag(string serverName, int flag, byte[] expectedValue)
        {
            var control = new VerifyNameControl(serverName, flag);
            Assert.True(control.IsCritical);
            Assert.Equal(flag, control.Flag);
            Assert.Equal(serverName, control.ServerName);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1338", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void Ctor_NullServerName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new VerifyNameControl(null));
            AssertExtensions.Throws<ArgumentNullException>("serverName", () => new VerifyNameControl(null, 0));
        }

        [Fact]
        public void ServerName_SetValid_GetReturnsExpected()
        {
            var control = new VerifyNameControl { ServerName = "ServerName" };
            Assert.Equal("ServerName", control.ServerName);
        }

        [Fact]
        public void ServerName_SetNull_ThrowsArgumentNullException()
        {
            var control = new VerifyNameControl();
            AssertExtensions.Throws<ArgumentNullException>("value", () => control.ServerName = null);
        }

        [Fact]
        public void Flag_Set_GetReturnsExpected()
        {
            var control = new VerifyNameControl { Flag = 10 };
            Assert.Equal(10, control.Flag);
        }
    }
}
