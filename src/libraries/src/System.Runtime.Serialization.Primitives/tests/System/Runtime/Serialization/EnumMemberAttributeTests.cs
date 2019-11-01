// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class EnumMemberAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new EnumMemberAttribute();
            Assert.Null(attribute.Value);
            Assert.False(attribute.IsValueSetExplicitly);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void Value_Set_GetReturnsExpected(string value)
        {
            var attribute = new EnumMemberAttribute() { Value = value };
            Assert.Equal(value, attribute.Value);
            Assert.True(attribute.IsValueSetExplicitly);
        }
    }
}
