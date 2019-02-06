// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class MergablePropertyAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_AllowMerge(bool allowMerge)
        {
            var attribute = new MergablePropertyAttribute(allowMerge);
            Assert.Equal(allowMerge, attribute.AllowMerge);
            Assert.Equal(allowMerge, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { MergablePropertyAttribute.Yes, MergablePropertyAttribute.Yes, true };
            yield return new object[] { MergablePropertyAttribute.Yes, new MergablePropertyAttribute(true), true };
            yield return new object[] { MergablePropertyAttribute.Yes, MergablePropertyAttribute.No, false };

            yield return new object[] { MergablePropertyAttribute.Yes, new object(), false };
            yield return new object[] { MergablePropertyAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(MergablePropertyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is MergablePropertyAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        private static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { MergablePropertyAttribute.Yes, true };
            yield return new object[] { MergablePropertyAttribute.Default, true };
            yield return new object[] { MergablePropertyAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetAllowMerge_ReturnsExpected(MergablePropertyAttribute attribute, bool expectedAllowMerge)
        {
            Assert.Equal(expectedAllowMerge, attribute.AllowMerge);
            Assert.Equal(expectedAllowMerge, attribute.IsDefaultAttribute());
        }
    }
}
