// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class AttributeProviderAttributeTests
    {
        [Theory]
        [InlineData("type", "property")]
        [InlineData("", "")]
        public void Ctor_TypeName_PropertyName(string typeName, string propertyName)
        {
            var attribute = new AttributeProviderAttribute(typeName, propertyName);
            Assert.Equal(typeName, attribute.TypeName);
            Assert.Equal(propertyName, attribute.PropertyName);
        }

        [Fact]
        public void Ctor_NullPropertyName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("propertyName", () => new AttributeProviderAttribute("typeName", null));
        }

        [Theory]
        [InlineData("type")]
        [InlineData("")]
        public void Ctor_TypeName(string typeName)
        {
            var attribute = new AttributeProviderAttribute(typeName);
            Assert.Equal(typeName, attribute.TypeName);
            Assert.Null(attribute.PropertyName);
        }

        [Fact]
        public void Ctor_NullTypeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => new AttributeProviderAttribute((string)null));
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => new AttributeProviderAttribute((string)null, "propertyName"));
        }

        [Theory]
        [InlineData(typeof(AttributeProviderAttribute))]
        public void Ctor_Type(Type type)
        {
            var attribute = new AttributeProviderAttribute(type);
            Assert.Equal(type.AssemblyQualifiedName, attribute.TypeName);
            Assert.Null(attribute.PropertyName);
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new AttributeProviderAttribute((Type)null));
        }
    }
}
