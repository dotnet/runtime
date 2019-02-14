// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignerCategoryAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DesignerCategoryAttribute();
            Assert.Equal(string.Empty, attribute.Category);
            Assert.True(attribute.IsDefaultAttribute());
            Assert.Equal("System.ComponentModel.DesignerCategoryAttribute", attribute.TypeId);
        }

        [Theory]
        [InlineData("", true, "System.ComponentModel.DesignerCategoryAttribute")]
        [InlineData("test category", false, "System.ComponentModel.DesignerCategoryAttributetest category")]
        public void Ctor_Category(string category, bool expectedIsDefaultAttribute, string expectedTypeId)
        {
            var attribute = new DesignerCategoryAttribute(category);
            Assert.Equal(category, attribute.Category);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
            Assert.Equal(expectedTypeId, attribute.TypeId);
        }

        [Fact]
        public void IsDefaultAttribute_NullCategory_ThrowsNullReferenceException()
        {
            var attribute = new CategoryAttribute(null);
            Assert.Null(attribute.Category);
            Assert.Equal(typeof(CategoryAttribute), attribute.TypeId);
            Assert.Throws<NullReferenceException>(() => attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new DesignerCategoryAttribute("category"), new DesignerCategoryAttribute("category"), true };
            yield return new object[] { new DesignerCategoryAttribute("category"), new DesignerCategoryAttribute(""), false };
            yield return new object[] { DesignerCategoryAttribute.Default, DesignerCategoryAttribute.Default, true };

            yield return new object[] { new DesignerCategoryAttribute(null), new DesignerCategoryAttribute(null), true };
            yield return new object[] { new DesignerCategoryAttribute("category"), new DesignerCategoryAttribute(null), false };
            yield return new object[] { new DesignerCategoryAttribute(null), new DesignerCategoryAttribute("category"), false };

            yield return new object[] { new DesignerCategoryAttribute("category"), new object(), false };
            yield return new object[] { new DesignerCategoryAttribute("category"), null, false };
            yield return new object[] { new DesignerCategoryAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DesignerCategoryAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DesignerCategoryAttribute otherAttribute && otherAttribute.Category != null && attribute.Category != null)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void GetHashCode_NullCategory_ThrowsNullReferenceException()
        {
            var attribute = new DesignerCategoryAttribute(null);
            Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
        }

        public static IEnumerable<object[]> DefaultCategories_TestData()
        {
            yield return new object[] { DesignerCategoryAttribute.Component, "Component" };
            yield return new object[] { DesignerCategoryAttribute.Default, string.Empty };
            yield return new object[] { DesignerCategoryAttribute.Form, "Form" };
            yield return new object[] { DesignerCategoryAttribute.Generic, "Designer" };
        }

        [Theory]
        [MemberData(nameof(DefaultCategories_TestData))]
        public void DefaultDesignerCategoryAttribute_GetCategory_ReturnsEmptyString(DesignerCategoryAttribute attribute, string expectedCategory)
        {
            Assert.Equal(expectedCategory, attribute.Category);
        }
    }
}
