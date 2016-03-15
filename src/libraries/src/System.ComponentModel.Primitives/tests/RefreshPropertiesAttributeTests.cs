// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class RefreshPropertiesAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(RefreshPropertiesAttribute.All.Equals(RefreshPropertiesAttribute.Repaint));
        }

        [Fact]
        public void Equals_Null()
        {
            Assert.False(RefreshPropertiesAttribute.All.Equals(null));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(RefreshPropertiesAttribute.Default.Equals(RefreshPropertiesAttribute.Default));
        }

        [Theory]
        [InlineData(RefreshProperties.All)]
        [InlineData(RefreshProperties.None)]
        [InlineData(RefreshProperties.Repaint)]
        public void GetRefreshProperties(RefreshProperties value)
        {
            var attribute = new RefreshPropertiesAttribute(value);

            Assert.Equal(value, attribute.RefreshProperties);
        }
    }
}
