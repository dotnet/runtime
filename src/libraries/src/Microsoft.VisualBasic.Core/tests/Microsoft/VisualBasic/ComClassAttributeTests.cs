// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class ComClassAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ComClassAttribute();
            Assert.Null(attribute.ClassID);
            Assert.Null(attribute.InterfaceID);
            Assert.Null(attribute.EventID);
            Assert.False(attribute.InterfaceShadows);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("classID")]
        public void Ctor_String(string classID)
        {
            var attribute = new ComClassAttribute(classID);
            Assert.Equal(classID, attribute.ClassID);
            Assert.Null(attribute.InterfaceID);
            Assert.Null(attribute.EventID);
            Assert.False(attribute.InterfaceShadows);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("classID", "interfaceID")]
        public void Ctor_String_String(string classID, string interfaceID)
        {
            var attribute = new ComClassAttribute(classID, interfaceID);
            Assert.Equal(classID, attribute.ClassID);
            Assert.Equal(interfaceID, attribute.InterfaceID);
            Assert.Null(attribute.EventID);
            Assert.False(attribute.InterfaceShadows);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        [InlineData("classID", "interfaceID", "eventID")]
        public void Ctor_String_String(string classID, string interfaceID, string eventID)
        {
            var attribute = new ComClassAttribute(classID, interfaceID, eventID);
            Assert.Equal(classID, attribute.ClassID);
            Assert.Equal(interfaceID, attribute.InterfaceID);
            Assert.Equal(eventID, attribute.EventID);
            Assert.False(attribute.InterfaceShadows);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InterfaceShadows_Set_GetReturnsExpected(bool value)
        {
            var attribute = new ComClassAttribute() { InterfaceShadows = value };
            Assert.Equal(value, attribute.InterfaceShadows);
        }
    }
}
