// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class DataMemberAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DataMemberAttribute();
            Assert.True(attribute.EmitDefaultValue);
            Assert.False(attribute.IsRequired);
            Assert.Null(attribute.Name);
            Assert.False(attribute.IsNameSetExplicitly);
            Assert.Equal(-1, attribute.Order);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EmitDefaultValue_Set_GetReturnsExpected(bool value)
        {
            var attribute = new DataMemberAttribute() { EmitDefaultValue = value };
            Assert.Equal(value, attribute.EmitDefaultValue);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsRequired_Set_GetReturnsExpected(bool value)
        {
            var attribute = new DataMemberAttribute() { IsRequired = value };
            Assert.Equal(value, attribute.IsRequired);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void Name_Set_GetReturnsExpected(string value)
        {
            var attribute = new DataMemberAttribute() { Name = value };
            Assert.Equal(value, attribute.Name);
            Assert.True(attribute.IsNameSetExplicitly);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Order_Set_GetReturnsExpected(int value)
        {
            var attribute = new DataMemberAttribute() { Order = value };
            Assert.Equal(value, attribute.Order);
        }

        [Fact]
        public void Order_SetInvalid_ThrowsInvalidDataContractException()
        {
            var attribute = new DataMemberAttribute();
            Assert.Throws<InvalidDataContractException>(() => attribute.Order = -1);
        }
    }
}
