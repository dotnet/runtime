// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DisplayNameAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DisplayNameAttribute();
            Assert.Equal(string.Empty, attribute.DisplayName);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("test name", false)]
        public void Ctor_DisplayName(string displayName, bool expectedIsDefaultAttribute)
        {
            var attribute = new DisplayNameAttribute(displayName);
            Assert.Equal(displayName, attribute.DisplayName);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new DisplayNameAttribute("name"), new DisplayNameAttribute("name"), true };
            yield return new object[] { new DisplayNameAttribute("name"), new DisplayNameAttribute(""), false };
            yield return new object[] { DisplayNameAttribute.Default, DisplayNameAttribute.Default, true };

            yield return new object[] { new DisplayNameAttribute(null), new DisplayNameAttribute(null), true };
            yield return new object[] { new DisplayNameAttribute("name"), new DisplayNameAttribute(null), false };
            yield return new object[] { new DisplayNameAttribute(null), new DisplayNameAttribute("name"), false };

            yield return new object[] { new DisplayNameAttribute("name"), new object(), false };
            yield return new object[] { new DisplayNameAttribute("name"), null, false };
            yield return new object[] { new DisplayNameAttribute(null), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DisplayNameAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DisplayNameAttribute otherAttribute && otherAttribute.DisplayName != null && attribute.DisplayName != null)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void GetHashCode_NullDisplayName_ThrowsNullReferenceException()
        {
            var attribute = new DisplayNameAttribute(null);
            Assert.Throws<NullReferenceException>(() => attribute.GetHashCode());
        }

        [Fact]
        public void DefaultDisplayNameAttribute_GetDisplayName_ReturnsEmptyString()
        {
            Assert.Empty(DisplayNameAttribute.Default.DisplayName);
        }
    }
}
