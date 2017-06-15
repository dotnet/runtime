// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class CrossDomainMoveControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new CrossDomainMoveControl();
            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Null(control.TargetDomainController);
            Assert.Equal("1.2.840.113556.1.4.521", control.Type);
            
            Assert.Empty(control.GetValue());
        }

        [Theory]
        [InlineData(null, new byte[0])]
        [InlineData("", new byte[] { 0, 0 })]
        [InlineData("A", new byte[] { 65, 0, 0 })]
        public void Ctor_String(string targetDomainController, byte[] expectedValue)
        {
            var control = new CrossDomainMoveControl(targetDomainController);
            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal(targetDomainController, control.TargetDomainController);
            Assert.Equal("1.2.840.113556.1.4.521", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void TargetDomainController_Set_GetReturnsExpected()
        {
            var control = new CrossDomainMoveControl { TargetDomainController = "Name" };
            Assert.Equal("Name", control.TargetDomainController);
        }
    }
}
