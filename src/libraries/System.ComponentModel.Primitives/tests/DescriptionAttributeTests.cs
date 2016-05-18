// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class DescriptionAttributeTests
    {
        [Fact]
        public void Equals_DifferentDescriptions()
        {
            var firstAttribute = new DescriptionAttribute("description");
            var secondAttribute = new DescriptionAttribute(string.Empty);

            Assert.False(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public void Equals_SameDescription()
        {
            Assert.True(DescriptionAttribute.Default.Equals(DescriptionAttribute.Default));
        }

        [Fact]
        public void GetDescription()
        {
            var description = "test description";
            var attribute = new DescriptionAttribute(description);

            Assert.Equal(description, attribute.Description);
        }

        [Theory]
        [MemberData(nameof(DescriptionData))]
        public void CategoryNames(DescriptionAttribute attribute, string name)
        {
            Assert.Equal(name, attribute.Description);
        }

        private static IEnumerable<object[]> DescriptionData()
        {
            yield return new object[] { DescriptionAttribute.Default, "" };
            yield return new object[] { new DescriptionAttribute(""), "" };
            yield return new object[] { new DescriptionAttribute("other"), "other" };
        }
    }
}
