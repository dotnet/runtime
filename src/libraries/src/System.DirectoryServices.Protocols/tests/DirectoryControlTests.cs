// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryControlTests
    {
        [Theory]
        [InlineData("", null, false, false)]
        [InlineData("Type", new byte[] { 1, 2, 3 }, true, true)]
        public void Ctor_Type_Value_IsCritical_ServerSide(string type, byte[] value, bool isCritical, bool serverSide)
        {
            var control = new DirectoryControl(type, value, isCritical, serverSide);
            Assert.Equal(type, control.Type);
            Assert.Equal(isCritical, control.IsCritical);
            Assert.Equal(serverSide, control.ServerSide);

            byte[] controlValue = control.GetValue();
            Assert.NotSame(controlValue, value);
            Assert.Equal(value ?? Array.Empty<byte>(), controlValue);
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new DirectoryControl(null, new byte[0], false, false));
        }

        [Fact]
        public void IsCritical_Set_GetReturnsExpected()
        {
            var control = new DirectoryControl("Type", null, false, true);
            Assert.False(control.IsCritical);

            control.IsCritical = true;
            Assert.True(control.IsCritical);
        }

        [Fact]
        public void ServerSide_Set_GetReturnsExpected()
        {
            var control = new DirectoryControl("Type", null, true, false);
            Assert.False(control.ServerSide);

            control.ServerSide = true;
            Assert.True(control.ServerSide);
        }
    }
}
