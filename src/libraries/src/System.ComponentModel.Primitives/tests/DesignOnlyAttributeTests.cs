// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class DesignOnlyAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(DesignOnlyAttribute.No.Equals(DesignOnlyAttribute.Yes));
        }

        [Fact]
        public void Equals_SameValues()
        {
            Assert.True(DesignOnlyAttribute.Default.Equals(DesignOnlyAttribute.Default));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetIsDesignOnly(bool isDesignOnly)
        {
            var attribute = new DesignOnlyAttribute(isDesignOnly);

            Assert.Equal(isDesignOnly, attribute.IsDesignOnly);
        }
    }
}
