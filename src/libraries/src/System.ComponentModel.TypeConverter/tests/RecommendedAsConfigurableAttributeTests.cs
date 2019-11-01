// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0618

namespace System.ComponentModel.Tests
{
    public class RecommendedAsConfigurableAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool recommendAsConfigurable)
        {
            var attribute = new RecommendedAsConfigurableAttribute(recommendAsConfigurable);
            Assert.Equal(recommendAsConfigurable, attribute.RecommendedAsConfigurable);
            Assert.Equal(!recommendAsConfigurable, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            RecommendedAsConfigurableAttribute attribute = RecommendedAsConfigurableAttribute.Default;
            Assert.Same(attribute, RecommendedAsConfigurableAttribute.Default);
            Assert.Same(attribute, RecommendedAsConfigurableAttribute.No);
            Assert.False(attribute.RecommendedAsConfigurable);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            RecommendedAsConfigurableAttribute attribute = RecommendedAsConfigurableAttribute.Yes;
            Assert.Same(attribute, RecommendedAsConfigurableAttribute.Yes);
            Assert.True(attribute.RecommendedAsConfigurable);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            RecommendedAsConfigurableAttribute attribute = RecommendedAsConfigurableAttribute.No;
            Assert.Same(attribute, RecommendedAsConfigurableAttribute.No);
            Assert.False(attribute.RecommendedAsConfigurable);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new RecommendedAsConfigurableAttribute(true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new RecommendedAsConfigurableAttribute(true), true };
            yield return new object[] { attribute, new RecommendedAsConfigurableAttribute(false), false };
            yield return new object[] { new RecommendedAsConfigurableAttribute(false), new RecommendedAsConfigurableAttribute(false), true };
            yield return new object[] { new RecommendedAsConfigurableAttribute(false), new RecommendedAsConfigurableAttribute(true), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(RecommendedAsConfigurableAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is RecommendedAsConfigurableAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}

#pragma warning restore 0618
