// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class ImmutableObjectAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(ImmutableObjectAttribute.Yes.Equals(ImmutableObjectAttribute.No));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(ImmutableObjectAttribute.Yes.Equals(ImmutableObjectAttribute.Yes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetImmutable(bool value)
        {
            var attribute = new ImmutableObjectAttribute(value);

            Assert.Equal(value, attribute.Immutable);
        }

        [Theory]
        [MemberData(nameof(ImmutableAttributesData))]
        public void NameTests(ImmutableObjectAttribute attribute, bool isImmutable)
        {
            Assert.Equal(isImmutable, attribute.Immutable);
        }

        private static IEnumerable<object[]> ImmutableAttributesData()
        {
            yield return new object[] { ImmutableObjectAttribute.Default, false };
            yield return new object[] { new ImmutableObjectAttribute(true), true };
            yield return new object[] { new ImmutableObjectAttribute(false), false };
        }
    }
}
