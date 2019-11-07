// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class OptionalFieldAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new OptionalFieldAttribute();
            Assert.Equal(1, attribute.VersionAdded);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void VersionAdded_Set_GetReturnsExpected(int value)
        {
            var attribute = new OptionalFieldAttribute() { VersionAdded = value };
            Assert.Equal(value, attribute.VersionAdded);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void VersionAdded_ValueLessThanZero_ThrowsArgumentException(int value)
        {
            var attribute = new OptionalFieldAttribute();
            AssertExtensions.Throws<ArgumentException>(null, () => attribute.VersionAdded = value);
        }
    }
}