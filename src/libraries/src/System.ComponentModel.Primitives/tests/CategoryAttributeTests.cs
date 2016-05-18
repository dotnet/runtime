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
        public void CategoryNames(CategoryAttribute attribute, string name)
        {
            Assert.Equal(name, attribute.Category);
        }

        private static IEnumerable<object[]> CategoryNameData()
        {
            yield return new object[] { CategoryAttribute.Appearance, "Appearance" };
            yield return new object[] { CategoryAttribute.Asynchronous, "Asynchronous" };
            yield return new object[] { CategoryAttribute.Behavior, "Behavior" };
            yield return new object[] { CategoryAttribute.Data, "Data" };
            yield return new object[] { CategoryAttribute.Default, "Misc" };
            yield return new object[] { CategoryAttribute.Design, "Design" };
            yield return new object[] { CategoryAttribute.DragDrop, "Drag Drop" };
            yield return new object[] { CategoryAttribute.Focus, "Focus" };
            yield return new object[] { CategoryAttribute.Key, "Key" };
            yield return new object[] { CategoryAttribute.Layout, "Layout" };
            yield return new object[] { CategoryAttribute.Mouse, "Mouse" };
            yield return new object[] { CategoryAttribute.WindowStyle, "Window Style" };
        }
    }
}
