// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeDescriptionProviderAttributeTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("typeName")]
        public void Ctor_String(string typeName)
        {
            var attribute = new TypeDescriptionProviderAttribute(typeName);
            Assert.Equal(typeName, attribute.TypeName);
        }

        [Fact]
        public void Ctor_NullTypeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => new TypeDescriptionProviderAttribute((string)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(TypeDescriptionProviderAttribute))]
        public void Ctor_Type(Type type)
        {
            var attribute = new TypeDescriptionProviderAttribute(type);
            Assert.Equal(type.AssemblyQualifiedName, attribute.TypeName);
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new TypeDescriptionProviderAttribute((Type)null));
        }
    }
}
