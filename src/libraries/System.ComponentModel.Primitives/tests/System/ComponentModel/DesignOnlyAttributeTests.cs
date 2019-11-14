// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DesignOnlyAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_DesignOnly(bool designOnly)
        {
            var attribute = new DesignOnlyAttribute(designOnly);
            Assert.Equal(designOnly, attribute.IsDesignOnly);
            Assert.Equal(!designOnly, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { DesignOnlyAttribute.Yes, DesignOnlyAttribute.Yes, true };
            yield return new object[] { DesignOnlyAttribute.No, new DesignOnlyAttribute(false), true };
            yield return new object[] { DesignOnlyAttribute.Yes, DesignOnlyAttribute.No, false };

            yield return new object[] { DesignOnlyAttribute.Yes, new object(), false };
            yield return new object[] { DesignOnlyAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(DesignOnlyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is DesignOnlyAttribute otherAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { DesignOnlyAttribute.Yes, true };
            yield return new object[] { DesignOnlyAttribute.Default, false };
            yield return new object[] { DesignOnlyAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetDesignOnly_ReturnsExpected(DesignOnlyAttribute attribute, bool expectedDesignOnly)
        {
            Assert.Equal(expectedDesignOnly, attribute.IsDesignOnly);
            Assert.Equal(!expectedDesignOnly, attribute.IsDefaultAttribute());
        }
    }
}
