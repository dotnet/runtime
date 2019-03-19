// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel
{
    public class ToolboxItemAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool defaultType)
        {
            var attribute = new ToolboxItemAttribute(defaultType);
            if (defaultType)
            {
                Assert.Equal("System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", attribute.ToolboxItemTypeName);
                if (PlatformDetection.IsFullFramework)
                {
                    Assert.NotNull(attribute.ToolboxItemType);
                }
                else
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => attribute.ToolboxItemType);
                }
            }
            else
            {
                Assert.Empty(attribute.ToolboxItemTypeName);
                Assert.Null(attribute.ToolboxItemType);
            }
            Assert.Equal(defaultType, attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("invalidName", false)]
        [InlineData("invalidName.dll", false)]
        [InlineData("System.Int32", false)]
        [InlineData("System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", true)]
        public void Ctor_String(string toolboxItemTypeName, bool expectedIsDefault)
        {
            var attribute = new ToolboxItemAttribute(toolboxItemTypeName);
            Assert.Equal(toolboxItemTypeName, attribute.ToolboxItemTypeName);

            Type expectedType = Type.GetType(toolboxItemTypeName);
            if (expectedType != null)
            {
                Assert.Equal(expectedType, attribute.ToolboxItemType);
                Assert.Same(attribute.ToolboxItemType, attribute.ToolboxItemType);
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(null, () => attribute.ToolboxItemType);
            }
            Assert.Equal(expectedIsDefault, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Ctor_NullToolboxItemTypeName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(() => new ToolboxItemAttribute((string)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(ProvidePropertyAttribute))]
        [InlineData(typeof(ProvidePropertyAttributeTests))]
        public void Ctor_Type(Type toolboxItemType)
        {
            var attribute = new ToolboxItemAttribute(toolboxItemType);
            Assert.Equal(toolboxItemType.AssemblyQualifiedName, attribute.ToolboxItemTypeName);
            Assert.Same(toolboxItemType, attribute.ToolboxItemType);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Ctor_NullToolboxItemType_ThrowsNullReferenceException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("toolboxItemType", () => new ToolboxItemAttribute((Type)null));
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            ToolboxItemAttribute attribute = ToolboxItemAttribute.Default;
            Assert.Same(attribute, ToolboxItemAttribute.Default);
            Assert.Equal("System.Drawing.Design.ToolboxItem, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", attribute.ToolboxItemTypeName);
            if (PlatformDetection.IsFullFramework)
            {
                Assert.NotNull(attribute.ToolboxItemType);
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(null, () => attribute.ToolboxItemType);
            }
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void None_Get_ReturnsExpected()
        {
            ToolboxItemAttribute attribute = ToolboxItemAttribute.None;
            Assert.Same(attribute, ToolboxItemAttribute.None);
            Assert.Empty(attribute.ToolboxItemTypeName);
            Assert.Null(attribute.ToolboxItemType);
            Assert.False(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new ToolboxItemAttribute("toolboxItemTypeName");

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new ToolboxItemAttribute("toolboxItemTypeName"), true };
            yield return new object[] { attribute, new ToolboxItemAttribute("toolboxitemtypename"), false };
            yield return new object[] { attribute, new ToolboxItemAttribute(defaultType: false), false };

            yield return new object[] { new ToolboxItemAttribute(defaultType: false), new ToolboxItemAttribute(defaultType: false), true };
            yield return new object[] { new ToolboxItemAttribute(defaultType: false), new ToolboxItemAttribute("toolboxItemTypeName"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(ToolboxItemAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is ToolboxItemAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
