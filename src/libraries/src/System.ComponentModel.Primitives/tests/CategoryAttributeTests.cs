// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
