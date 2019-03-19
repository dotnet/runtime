// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class EditorAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new EditorAttribute();
            Assert.Empty(attribute.EditorTypeName);
            Assert.Empty(attribute.EditorBaseTypeName);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("typeName", "")]
        [InlineData("typeName.dll", "baseTypeName")]
        public void Ctor_String_String(string typeName, string baseTypeName)
        {
            var attribute = new EditorAttribute(typeName, baseTypeName);
            Assert.Equal(typeName, attribute.EditorTypeName);
            Assert.Equal(baseTypeName, attribute.EditorBaseTypeName);
        }

        [Theory]
        [InlineData("", typeof(int))]
        [InlineData("typeName", typeof(EditorAttribute))]
        [InlineData("typeName.dll", typeof(EditorAttributeTests))]
        public void Ctor_String_Type(string typeName, Type baseType)
        {
            var attribute = new EditorAttribute(typeName, baseType);
            Assert.Equal(typeName, attribute.EditorTypeName);
            Assert.Equal(baseType.AssemblyQualifiedName, attribute.EditorBaseTypeName);
        }

        [Theory]
        [InlineData(typeof(EditorAttribute), typeof(int))]
        [InlineData(typeof(EditorAttributeTests), typeof(EditorAttribute))]
        [InlineData(typeof(int), typeof(EditorAttributeTests))]
        public void Ctor_Type_Type(Type type, Type baseType)
        {
            var attribute = new EditorAttribute(type, baseType);
            Assert.Equal(type.AssemblyQualifiedName, attribute.EditorTypeName);
            Assert.Equal(baseType.AssemblyQualifiedName, attribute.EditorBaseTypeName);
        }

        [Fact]
        public void Ctor_NullTypeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("typeName", () => new EditorAttribute(null, "baseTypeName"));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("typeName", () => new EditorAttribute((string)null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("type", () => new EditorAttribute((Type)null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullBaseType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("baseType", () => new EditorAttribute("typeName", (Type)null));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("baseType", () => new EditorAttribute(typeof(int), null));
        }

        [Theory]
        [InlineData("BaseTypeName", "System.ComponentModel.EditorAttributeBaseTypeName")]
        [InlineData("BaseTypeName,Other", "System.ComponentModel.EditorAttributeBaseTypeName")]
        public void TypeId_ValidEditorBaseTypeName_ReturnsExcepted(string baseTypeName, object expected)
        {
            var attribute = new EditorAttribute("Type", baseTypeName);
            Assert.Equal(expected, attribute.TypeId);
            Assert.Same(attribute.TypeId, attribute.TypeId);
        }

        [Fact]
        public void TypeId_NullBaseTypeName_ReturnsExpected()
        {
            var attribute = new EditorAttribute("Type", (string)null);
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Equal("System.ComponentModel.EditorAttribute", attribute.TypeId);
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attribute.TypeId);
            }
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new EditorAttribute("typeName", "baseTypeName");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new EditorAttribute("typeName", "baseTypeName"), true };
            yield return new object[] { attribute, new EditorAttribute("typename", "baseTypeName"), false };
            yield return new object[] { attribute, new EditorAttribute("typeName", "basetypename"), false };
            yield return new object[] { attribute, new EditorAttribute("typeName", (string)null), false };

            yield return new object[] { new EditorAttribute("typeName", (string)null), new EditorAttribute("typeName", (string)null), true };
            yield return new object[] { new EditorAttribute("typeName", (string)null), new EditorAttribute("typename", (string)null), false };
            yield return new object[] { new EditorAttribute("typeName", (string)null), new EditorAttribute("typeName", "baseTypeName"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(EditorAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsConsistentValue()
        {
            var attribute = new EditorAttribute();
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
    }
}
