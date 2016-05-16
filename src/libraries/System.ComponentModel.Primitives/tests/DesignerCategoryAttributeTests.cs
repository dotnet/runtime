// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class DesignerCategoryAttributeTests
    {
        [Fact]
        public void Equals_DifferentCategories()
        {
            var firstAttribute = new CategoryAttribute("category");
            var secondAttribute = new DescriptionAttribute(string.Empty);

            Assert.False(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public void Equals_SameDescription()
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
    }
}
