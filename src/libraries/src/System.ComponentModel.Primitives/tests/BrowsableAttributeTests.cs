// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
