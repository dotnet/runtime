// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DescriptionAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DescriptionAttribute();
            Assert.Equal(string.Empty, attribute.Description);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("test description", false)]
        public void Ctor_Description(string description, bool expectedIsDefaultAttribute)
        {
            var attribute = new DescriptionAttribute(description);
            Assert.Equal(description, attribute.Description);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new DescriptionAttribute("description"), new DescriptionAttribute("description"), true };
            yield return new object[] { new DescriptionAttribute("description"), new DescriptionAttribute(""), false };
            yield return new object[] { DescriptionAttribute.Default, DescriptionAttribute.Default, true };

            yield return new object[] { new DescriptionAttribute(null), new DescriptionAttribute(null), true };
            yield return new object[] { new DescriptionAttribute("description"), new DescriptionAttribute(null), false };
            yield return new object[] { new DescriptionAttribute(null), new DescriptionAttribute("description"), false };

            yield return new object[] { new DescriptionAttribute("description"), new object(), false };
            yield return new object[] { new DescriptionAttribute("description"), null, false };
            yield return new object[] { new DescriptionAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DescriptionAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DescriptionAttribute otherAttribute && otherAttribute.Description != null && attribute.Description != null)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void GetHashCode_NullDescription_ThrowsNullReferenceException()
        {
            var attribute = new DescriptionAttribute(null);
            Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
        }

        [Fact]
        public void DefaultDescriptionAttribute_GetDescription_ReturnsEmptyString()
        {
            Assert.Empty(DescriptionAttribute.Default.Description);
        }
    }
}
