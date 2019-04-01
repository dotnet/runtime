// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ReadOnlyAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_IsReadOnly(bool isReadOnly)
        {
            var attribute = new ReadOnlyAttribute(isReadOnly);
            Assert.Equal(isReadOnly, attribute.IsReadOnly);
            Assert.Equal(!isReadOnly, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { ReadOnlyAttribute.Yes, ReadOnlyAttribute.Yes, true };
            yield return new object[] { ReadOnlyAttribute.Yes, new ReadOnlyAttribute(true), true };
            yield return new object[] { ReadOnlyAttribute.Yes, ReadOnlyAttribute.No, false };

            yield return new object[] { ReadOnlyAttribute.Yes, new object(), false };
            yield return new object[] { ReadOnlyAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ReadOnlyAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is ReadOnlyAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { ReadOnlyAttribute.Yes, true };
            yield return new object[] { ReadOnlyAttribute.Default, false };
            yield return new object[] { ReadOnlyAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetIsReadOnly_ReturnsExpected(ReadOnlyAttribute attribute, bool expectedAllowMerge)
        {
            Assert.Equal(expectedAllowMerge, attribute.IsReadOnly);
            Assert.Equal(!expectedAllowMerge, attribute.IsDefaultAttribute());
        }
    }
}
