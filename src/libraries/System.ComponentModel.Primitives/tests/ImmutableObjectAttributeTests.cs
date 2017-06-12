// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ImmutableObjectAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Immutable(bool immutable)
        {
            var attribute = new ImmutableObjectAttribute(immutable);
            Assert.Equal(immutable, attribute.Immutable);
            Assert.Equal(!immutable, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { ImmutableObjectAttribute.Yes, ImmutableObjectAttribute.Yes, true };
            yield return new object[] { ImmutableObjectAttribute.No, new ImmutableObjectAttribute(false), true };
            yield return new object[] { ImmutableObjectAttribute.Yes, ImmutableObjectAttribute.No, false };

            yield return new object[] { ImmutableObjectAttribute.Yes, new object(), false };
            yield return new object[] { ImmutableObjectAttribute.Yes, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ImmutableObjectAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is ImmutableObjectAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        private static IEnumerable<object[]> DefaultProperties_TestData()
        {
            yield return new object[] { ImmutableObjectAttribute.Yes, true };
            yield return new object[] { ImmutableObjectAttribute.Default, false };
            yield return new object[] { ImmutableObjectAttribute.No, false };
        }

        [Theory]
        [MemberData(nameof(DefaultProperties_TestData))]
        public void DefaultProperties_GetImmutable_ReturnsExpected(ImmutableObjectAttribute attribute, bool expectedImmutableObject)
        {
            Assert.Equal(expectedImmutableObject, attribute.Immutable);
            Assert.Equal(!expectedImmutableObject, attribute.IsDefaultAttribute());
        }
    }
}
