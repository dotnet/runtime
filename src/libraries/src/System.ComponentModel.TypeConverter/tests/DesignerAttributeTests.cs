// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignerAttributeTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("designerTypeName")]
        [InlineData("designerTypeName.dll")]
        public void Ctor_String(string designerTypeName)
        {
            var attribute = new DesignerAttribute(designerTypeName);
            Assert.Equal(designerTypeName, attribute.DesignerTypeName);
            Assert.Equal(typeof(IDesigner).FullName, attribute.DesignerBaseTypeName);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DesignerAttribute))]
        [InlineData(typeof(DesignerAttributeTests))]
        public void Ctor_Type(Type designerType)
        {
            var attribute = new DesignerAttribute(designerType);
            Assert.Equal(designerType.AssemblyQualifiedName, attribute.DesignerTypeName);
            Assert.Equal(typeof(IDesigner).FullName, attribute.DesignerBaseTypeName);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("designerTypeName", "")]
        [InlineData("designerTypeName.dll", "designerBaseTypeName")]
        public void Ctor_String_String(string designerTypeName, string designerBaseTypeName)
        {
            var attribute = new DesignerAttribute(designerTypeName, designerBaseTypeName);
            Assert.Equal(designerTypeName, attribute.DesignerTypeName);
            Assert.Equal(designerBaseTypeName, attribute.DesignerBaseTypeName);
        }

        [Theory]
        [InlineData("", typeof(int))]
        [InlineData("designerTypeName", typeof(DesignerAttribute))]
        [InlineData("designerTypeName.dll", typeof(DesignerAttributeTests))]
        public void Ctor_String_Type(string designerTypeName, Type designerBaseType)
        {
            var attribute = new DesignerAttribute(designerTypeName, designerBaseType);
            Assert.Equal(designerTypeName, attribute.DesignerTypeName);
            Assert.Equal(designerBaseType.AssemblyQualifiedName, attribute.DesignerBaseTypeName);
        }

        [Theory]
        [InlineData(typeof(DesignerAttribute), typeof(int))]
        [InlineData(typeof(DesignerAttributeTests), typeof(DesignerAttribute))]
        [InlineData(typeof(int), typeof(DesignerAttributeTests))]
        public void Ctor_Type_Type(Type designerType, Type designerBaseType)
        {
            var attribute = new DesignerAttribute(designerType, designerBaseType);
            Assert.Equal(designerType.AssemblyQualifiedName, attribute.DesignerTypeName);
            Assert.Equal(designerBaseType.AssemblyQualifiedName, attribute.DesignerBaseTypeName);
        }

        [Fact]
        public void Ctor_NullDesignerTypeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(() => new DesignerAttribute((string)null));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(() => new DesignerAttribute(null, "designerBaseTypeName"));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(() => new DesignerAttribute((string)null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullDesignerType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("designerType", () => new DesignerAttribute((Type)null));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("designerType", () => new DesignerAttribute((Type)null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullDesignerBaseType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("designerBaseType", () => new DesignerAttribute("designerTypeName", (Type)null));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("designerBaseType", () => new DesignerAttribute(typeof(int), null));
        }

        [Theory]
        [InlineData("BaseDesignerTypeName", "System.ComponentModel.DesignerAttributeBaseDesignerTypeName")]
        [InlineData("BaseDesignerTypeName,Other", "System.ComponentModel.DesignerAttributeBaseDesignerTypeName")]
        public void TypeId_ValidDesignerBaseTypeName_ReturnsExcepted(string designerBaseTypeName, object expected)
        {
            var attribute = new DesignerAttribute("SerializerType", designerBaseTypeName);
            Assert.Equal(expected, attribute.TypeId);
            Assert.Same(attribute.TypeId, attribute.TypeId);
        }

        [Fact]
        public void TypeId_NullDesignerDesignerTypeName_ThrowsNullReferenceException()
        {
            var attribute = new DesignerAttribute("DesignerType", (string)null);
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Equal("System.ComponentModel.DesignerAttribute", attribute.TypeId);
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => attribute.TypeId);
            }
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DesignerAttribute("designerTypeName", "designerBaseTypeName");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DesignerAttribute("designerTypeName", "designerBaseTypeName"), true };
            yield return new object[] { attribute, new DesignerAttribute("designertypename", "designerBaseTypeName"), false };
            yield return new object[] { attribute, new DesignerAttribute("designerTypeName", "designerbasetypename"), false };
            yield return new object[] { attribute, new DesignerAttribute("designerTypeName", (string)null), false };

            yield return new object[] { new DesignerAttribute("designerTypeName", (string)null), new DesignerAttribute("designerTypeName", (string)null), true };
            yield return new object[] { new DesignerAttribute("designerTypeName", (string)null), new DesignerAttribute("designertypename", (string)null), false };
            yield return new object[] { new DesignerAttribute("designerTypeName", (string)null), new DesignerAttribute("designerTypeName", "designertBaseTypeName"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DesignerAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DesignerAttribute otherAttribute && attribute.DesignerBaseTypeName != null && otherAttribute.DesignerBaseTypeName != null)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void GetHashCode_NullBaseDesignerTypeName_ThrowsNullReferenceExeption()
        {
            var attribute = new DesignerAttribute("designerTypeName", (string)null);
            Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
        }
    }
}
