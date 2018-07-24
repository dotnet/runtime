// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class DataContractAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DataContractAttribute();
            Assert.False(attribute.IsReference);
            Assert.False(attribute.IsReferenceSetExplicitly);
            Assert.Null(attribute.Name);
            Assert.False(attribute.IsNameSetExplicitly);
            Assert.Null(attribute.Namespace);
            Assert.False(attribute.IsNamespaceSetExplicitly);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsReference_Set_GetReturnsExpected(bool value)
        {
            var attribute = new DataContractAttribute() { IsReference = value };
            Assert.Equal(value, attribute.IsReference);
            Assert.True(attribute.IsReferenceSetExplicitly);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void Name_Set_GetReturnsExpected(string value)
        {
            var attribute = new DataContractAttribute() { Name = value };
            Assert.Equal(value, attribute.Name);
            Assert.True(attribute.IsNameSetExplicitly);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void Namespace_Set_GetReturnsExpected(string value)
        {
            var attribute = new DataContractAttribute() { Namespace = value };
            Assert.Equal(value, attribute.Namespace);
            Assert.True(attribute.IsNamespaceSetExplicitly);
        }
    }
}
