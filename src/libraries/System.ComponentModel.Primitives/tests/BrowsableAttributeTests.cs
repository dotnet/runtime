// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class BrowsableAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetBrowsable(bool value)
        {
            var attribute = new BrowsableAttribute(value);

            Assert.Equal(value, attribute.Browsable);
        }

        [Theory]
        [MemberData(nameof(DefaultData))]
        public void GetBrowsable(BrowsableAttribute attribute, bool expectedBrowsable, bool expectedIsDefaultValue)
        {
            Assert.Equal(expectedBrowsable, attribute.Browsable);
            Assert.Equal(expectedIsDefaultValue, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(BrowsableAttribute.Yes.Equals(BrowsableAttribute.No));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(BrowsableAttribute.Yes.Equals(BrowsableAttribute.Yes));
        }

        [Fact]
        public void DefaultValue()
        {
            Assert.True(BrowsableAttribute.Default.IsDefaultAttribute());
            Assert.True(BrowsableAttribute.Yes.IsDefaultAttribute());
            Assert.False(BrowsableAttribute.No.IsDefaultAttribute());
            Assert.True(new BrowsableAttribute(true).IsDefaultAttribute());
            Assert.False(new BrowsableAttribute(false).IsDefaultAttribute());
        }

        private static IEnumerable<object[]> DefaultData()
        {
            yield return new object[] { BrowsableAttribute.Yes, true, true };
            yield return new object[] { BrowsableAttribute.Default, true, true };
            yield return new object[] { BrowsableAttribute.No, false, false };
        }
    }
}
