// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class ParenthesizePropertyNameAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            var firstAttribute = new ParenthesizePropertyNameAttribute(true);
            var secondAttribute = new ParenthesizePropertyNameAttribute(false);

            Assert.False(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public void Equals_Null()
        {
            Assert.False(ParenthesizePropertyNameAttribute.Default.Equals(null));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(ParenthesizePropertyNameAttribute.Default.Equals(ParenthesizePropertyNameAttribute.Default));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetNeedParenthesis(bool value)
        {
            var attribute = new ParenthesizePropertyNameAttribute(value);

            Assert.Equal(value, attribute.NeedParenthesis);
        }
    }
}
