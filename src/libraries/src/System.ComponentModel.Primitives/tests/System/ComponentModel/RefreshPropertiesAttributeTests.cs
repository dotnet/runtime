// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class RefreshPropertiesAttributeTests
    {
        [Theory]
        [InlineData(RefreshProperties.None - 1, false)]
        [InlineData(RefreshProperties.All, false)]
        [InlineData(RefreshProperties.None, true)]
        [InlineData(RefreshProperties.Repaint, false)]
        public static void Ctor_Refresh(RefreshProperties refresh, bool expectedIsDefaultAttribute)
        {
            var attribute = new RefreshPropertiesAttribute(refresh);
            Assert.Equal(refresh, attribute.RefreshProperties);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { RefreshPropertiesAttribute.All, RefreshPropertiesAttribute.All, true };
            yield return new object[] { RefreshPropertiesAttribute.All, new RefreshPropertiesAttribute(RefreshProperties.All), true };
            yield return new object[] { RefreshPropertiesAttribute.All, RefreshPropertiesAttribute.Repaint, false };

            yield return new object[] { RefreshPropertiesAttribute.All, new object(), false };
            yield return new object[] { RefreshPropertiesAttribute.All, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(RefreshPropertiesAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is RefreshPropertiesAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { RefreshPropertiesAttribute.Default, RefreshProperties.None, true };
            yield return new object[] { RefreshPropertiesAttribute.All, RefreshProperties.All, false };
            yield return new object[] { RefreshPropertiesAttribute.Repaint, RefreshProperties.Repaint, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetRefreshProperties_ReturnsExpected(RefreshPropertiesAttribute attribute, RefreshProperties expectedRefreshProperties, bool expectedIsDefaultAttribute)
        {
            Assert.Equal(expectedRefreshProperties, attribute.RefreshProperties);
            Assert.Equal(expectedIsDefaultAttribute, attribute.IsDefaultAttribute());
        }
    }
}
