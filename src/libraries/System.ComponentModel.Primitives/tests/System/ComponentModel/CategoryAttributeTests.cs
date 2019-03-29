// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CategoryAttributeTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // NetFX does not have fix for #21369
        public void Ctor_Default()
        {
            var attribute = new CategoryAttribute();
            Assert.Equal("Misc", attribute.Category);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Ctor_Category_TestData()
        {
            yield return new object[] { "", false };
            yield return new object[] { "test category", false };
            yield return new object[] { "Misc", true };
            yield return new object[] { "misc", false };
        }

        [Theory]
        [MemberData(nameof(Ctor_Category_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Does not have the fix for https://github.com/dotnet/corefx/issues/21356")]
        public void Ctor_Category(string category, bool expectedIsDefaultAttribute)
        {
            var attribute = new CategoryAttribute(category);
            Assert.Equal(category, attribute.Category);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void IsDefaultAttribute_NullCategory_ThrowsNullReferenceException()
        {
            var attribute = new CategoryAttribute(null);
            Assert.Null(attribute.Category);
            Assert.Throws<NullReferenceException>(() => attribute.IsDefaultAttribute());
        }
        
        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new CategoryAttribute("category"), new CategoryAttribute("category"), true };
            yield return new object[] { new CategoryAttribute("category"), new CategoryAttribute(null), false };
            yield return new object[] { CategoryAttribute.Action, CategoryAttribute.Action, true };
            yield return new object[] { CategoryAttribute.Action, CategoryAttribute.Appearance, false };

            yield return new object[] { CategoryAttribute.Action, null, false };
            yield return new object[] { CategoryAttribute.Action, new object(), false };
            yield return new object[] { new CategoryAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(CategoryAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is CategoryAttribute otherAttribute && otherAttribute.Category != null)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void Equals_NullCategory_ThrowsNullReferenceException()
        {
            var attribute = new CategoryAttribute(null);
            Assert.Throws<NullReferenceException>(() => attribute.Equals(new CategoryAttribute("a")));
        }

        [Fact]
        public void GetHashCode_NullCategory_ThrowsNullReferenceException()
        {
            var attribute = new CategoryAttribute(null);
            Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
        }

        public static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return Attribute(() => CategoryAttribute.Appearance, "Appearance");
            yield return Attribute(() => CategoryAttribute.Asynchronous, "Asynchronous");
            yield return Attribute(() => CategoryAttribute.Behavior, "Behavior");
            yield return Attribute(() => CategoryAttribute.Data, "Data");
            yield return Attribute(() => CategoryAttribute.Default, "Misc");
            yield return Attribute(() => CategoryAttribute.Design, "Design");
            yield return Attribute(() => CategoryAttribute.DragDrop, "Drag Drop");
            yield return Attribute(() => CategoryAttribute.Focus, "Focus");
            yield return Attribute(() => CategoryAttribute.Format, "Format");
            yield return Attribute(() => CategoryAttribute.Key, "Key");
            yield return Attribute(() => CategoryAttribute.Layout, "Layout");
            yield return Attribute(() => CategoryAttribute.Mouse, "Mouse");
            yield return Attribute(() => CategoryAttribute.WindowStyle, "Window Style");
        }

        private static object[] Attribute(Func<CategoryAttribute> attribute, string expectedCategory) => new object[] { attribute, expectedCategory };

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // NetFX does not have fix for #21369
        public void CategoryProperties_GetCategory_ReturnsExpected(Func<CategoryAttribute> attributeThunk, string expectedCategory)
        {
            CategoryAttribute attribute = attributeThunk();
            Assert.Same(attribute, attributeThunk());
            Assert.Equal(expectedCategory, attribute.Category);
        }
    }
}
