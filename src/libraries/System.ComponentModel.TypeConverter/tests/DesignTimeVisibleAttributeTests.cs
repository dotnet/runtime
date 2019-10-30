// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignTimeVisibleAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DesignTimeVisibleAttribute();
            Assert.False(attribute.Visible);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool visible)
        {
            var attribute = new DesignTimeVisibleAttribute(visible);
            Assert.Equal(visible, attribute.Visible);
            Assert.Equal(visible, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            DesignTimeVisibleAttribute attribute = DesignTimeVisibleAttribute.Yes;
            Assert.Same(attribute, DesignTimeVisibleAttribute.Yes);
            Assert.True(attribute.Visible);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            DesignTimeVisibleAttribute attribute = DesignTimeVisibleAttribute.No;
            Assert.Same(attribute, DesignTimeVisibleAttribute.No);
            Assert.False(attribute.Visible);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            DesignTimeVisibleAttribute attribute = DesignTimeVisibleAttribute.Default;
            Assert.Same(attribute, DesignTimeVisibleAttribute.Default);
            Assert.True(attribute.Visible);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new DesignTimeVisibleAttribute(true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new DesignTimeVisibleAttribute(true), true };
            yield return new object[] { attribute, new DesignTimeVisibleAttribute(false), false };
            yield return new object[] { new DesignTimeVisibleAttribute(false), new DesignTimeVisibleAttribute(false), true };
            yield return new object[] { new DesignTimeVisibleAttribute(false), new DesignTimeVisibleAttribute(true), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DesignTimeVisibleAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DesignTimeVisibleAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
