// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeConverterAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new TypeConverterAttribute();
            Assert.Empty(attribute.ConverterTypeName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("typeName")]
        public void Ctor_String(string typeName)
        {
            var attribute = new TypeConverterAttribute(typeName);
            Assert.Equal(typeName, attribute.ConverterTypeName);
        }

        [Fact]
        public void Ctor_NullStringNetCore_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => new TypeConverterAttribute((string)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type type)
        {
            var attribute = new TypeConverterAttribute(type);
            Assert.Equal(type.AssemblyQualifiedName, attribute.ConverterTypeName);
        }

        [Fact]
        public void Ctor_NullTypeNetCore_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new TypeConverterAttribute((Type)null));
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            Assert.Empty(TypeConverterAttribute.Default.ConverterTypeName);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new TypeConverterAttribute("typeName");
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new TypeConverterAttribute("typeName"), true };
            yield return new object[] { attribute, new TypeConverterAttribute("otherTypeName"), false };

            yield return new object[] { new TypeConverterAttribute("typeName"), new object(), false };
            yield return new object[] { new TypeConverterAttribute("typeName"), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equal_Invoke_ReturnsExpected(TypeConverterAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var attribute = new TypeConverterAttribute("typeName");
            Assert.Equal("typeName".GetHashCode(), attribute.GetHashCode());
        }
    }
}
