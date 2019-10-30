// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class CollectionDataContractAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new CollectionDataContractAttribute();
            Assert.False(attribute.IsReference);
            Assert.False(attribute.IsReferenceSetExplicitly);
            Assert.Null(attribute.ItemName);
            Assert.False(attribute.IsItemNameSetExplicitly);
            Assert.Null(attribute.Name);
            Assert.False(attribute.IsNameSetExplicitly);
            Assert.Null(attribute.Namespace);
            Assert.False(attribute.IsNamespaceSetExplicitly);
            Assert.Null(attribute.KeyName);
            Assert.False(attribute.IsKeyNameSetExplicitly);
            Assert.Null(attribute.ValueName);
            Assert.False(attribute.IsValueNameSetExplicitly);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsReference_Set_GetReturnsExpected(bool value)
        {
            var attribute = new CollectionDataContractAttribute() { IsReference = value };
            Assert.Equal(value, attribute.IsReference);
            Assert.True(attribute.IsReferenceSetExplicitly);
        }

        public static TheoryData<string> StringValue_TestData => new TheoryData<string>()
        {
            { null },
            { "" },
            { "value" }
        };

        [Theory]
        [MemberData(nameof(StringValue_TestData))]
        public void ItemName_Set_GetReturnsExpected(string value)
        {
            var attribute = new CollectionDataContractAttribute() { ItemName = value };
            Assert.Equal(value, attribute.ItemName);
            Assert.True(attribute.IsItemNameSetExplicitly);
        }

        [Theory]
        [MemberData(nameof(StringValue_TestData))]
        public void Name_Set_GetReturnsExpected(string value)
        {
            var attribute = new CollectionDataContractAttribute() { Name = value };
            Assert.Equal(value, attribute.Name);
            Assert.True(attribute.IsNameSetExplicitly);
        }

        [Theory]
        [MemberData(nameof(StringValue_TestData))]
        public void Namespace_Set_GetReturnsExpected(string value)
        {
            var attribute = new CollectionDataContractAttribute() { Namespace = value };
            Assert.Equal(value, attribute.Namespace);
            Assert.True(attribute.IsNamespaceSetExplicitly);
        }

        [Theory]
        [MemberData(nameof(StringValue_TestData))]
        public void KeyName_Set_GetReturnsExpected(string value)
        {
            var attribute = new CollectionDataContractAttribute() { KeyName = value };
            Assert.Equal(value, attribute.KeyName);
            Assert.True(attribute.IsKeyNameSetExplicitly);
        }

        [Theory]
        [MemberData(nameof(StringValue_TestData))]
        public void ValueName_Set_GetReturnsExpected(string value)
        {
            var attribute = new CollectionDataContractAttribute() { ValueName = value };
            Assert.Equal(value, attribute.ValueName);
            Assert.True(attribute.IsValueNameSetExplicitly);
        }
    }
}
