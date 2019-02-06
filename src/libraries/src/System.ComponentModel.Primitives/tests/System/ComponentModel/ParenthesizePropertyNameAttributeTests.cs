// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ParenthesizePropertyNameAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ParenthesizePropertyNameAttribute();
            Assert.False(attribute.NeedParenthesis);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_NeedParenthesis(bool needParenthesis)
        {
            var attribute = new ParenthesizePropertyNameAttribute(needParenthesis);
            Assert.Equal(needParenthesis, attribute.NeedParenthesis);
            Assert.Equal(!needParenthesis, attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new ParenthesizePropertyNameAttribute(true), new ParenthesizePropertyNameAttribute(true), true };
            yield return new object[] { new ParenthesizePropertyNameAttribute(true), new ParenthesizePropertyNameAttribute(false), false };
            yield return new object[] { ParenthesizePropertyNameAttribute.Default, ParenthesizePropertyNameAttribute.Default, true };

            yield return new object[] { ParenthesizePropertyNameAttribute.Default, new object(), false };
            yield return new object[] { ParenthesizePropertyNameAttribute.Default, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(ParenthesizePropertyNameAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is ParenthesizePropertyNameAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        [Fact]
        public void Default_GetNeedParenthesis_ReturnsFalse()
        {
            Assert.False(ParenthesizePropertyNameAttribute.Default.NeedParenthesis);
            Assert.True(ParenthesizePropertyNameAttribute.Default.IsDefaultAttribute());
        }
    }
}
