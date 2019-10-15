// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class BrowsableAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Browsable(bool browsable)
        {
            var attribute = new BrowsableAttribute(browsable);
            Assert.Equal(browsable, attribute.Browsable);
            Assert.Equal(browsable, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { BrowsableAttribute.Yes, BrowsableAttribute.Yes, true };
            yield return new object[] { BrowsableAttribute.Yes, new BrowsableAttribute(false), false };
            yield return new object[] { BrowsableAttribute.Yes, BrowsableAttribute.No, false };

            yield return new object[] { BrowsableAttribute.Yes, new object(), false };
            yield return new object[] { BrowsableAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(BrowsableAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is BrowsableAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { BrowsableAttribute.Yes, true };
            yield return new object[] { BrowsableAttribute.Default, true };
            yield return new object[] { BrowsableAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetBrowsable_ReturnsExpected(BrowsableAttribute attribute, bool expectedBrowsable)
        {
            Assert.Equal(expectedBrowsable, attribute.Browsable);
            Assert.Equal(expectedBrowsable, attribute.IsDefaultAttribute());
        }
    }
}
