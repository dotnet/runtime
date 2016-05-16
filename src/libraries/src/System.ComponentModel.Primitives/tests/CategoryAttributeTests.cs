// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class CategoryAttributeTests
    {
        [Fact]
        public void Equals_DifferentCategories()
        {
            Assert.False(CategoryAttribute.Action.Equals(CategoryAttribute.Appearance));
        }

        [Fact]
        public void Equals_SameCategory()
        {
            Assert.True(CategoryAttribute.Default.Equals(CategoryAttribute.Default));
        }

        [Fact]
        public void GetCategory()
        {
            var category = "test category";
            var attribute = new CategoryAttribute(category);

            Assert.Equal(category, attribute.Category);
        }

        [Theory]
        [MemberData(nameof(CategoryNameData))]
        public void CategoryNames(CategoryAttribute attribute, string name, bool isDefault)
        {
            Assert.Equal(name, attribute.Category);
            Assert.Equal(isDefault, attribute.IsDefaultAttribute());
        }

        private static IEnumerable<object[]> CategoryNameData()
        {
            yield return new object[] { CategoryAttribute.Appearance, "Appearance", false };
            yield return new object[] { CategoryAttribute.Asynchronous, "Asynchronous", false };
            yield return new object[] { CategoryAttribute.Behavior, "Behavior", false };
            yield return new object[] { CategoryAttribute.Data, "Data", false };
            yield return new object[] { CategoryAttribute.Default, "Misc", true };
            yield return new object[] { CategoryAttribute.Design, "Design", false };
            yield return new object[] { CategoryAttribute.DragDrop, "Drag Drop", false };
            yield return new object[] { CategoryAttribute.Focus, "Focus", false };
            yield return new object[] { CategoryAttribute.Key, "Key", false };
            yield return new object[] { CategoryAttribute.Layout, "Layout", false };
            yield return new object[] { CategoryAttribute.Mouse, "Mouse", false };
            yield return new object[] { CategoryAttribute.WindowStyle, "Window Style", false };
        }

        [Theory]
        [InlineData("Default", true)]
        [InlineData("default", false)]
        [InlineData("other", false)]
        public void DefaultValue(string name, bool isDefault)
        {
            Assert.Equal(isDefault, new CategoryAttribute(name).IsDefaultAttribute());
        }
    }
}
