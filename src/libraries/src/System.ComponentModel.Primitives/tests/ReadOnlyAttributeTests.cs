// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class ReadOnlyAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(ReadOnlyAttribute.Yes.Equals(ReadOnlyAttribute.No));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(ReadOnlyAttribute.Default.Equals(ReadOnlyAttribute.Default));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetIsReadOnly(bool value)
        {
            var attribute = new ReadOnlyAttribute(value);

            Assert.Equal(value, attribute.IsReadOnly);
        }
    }
}
