// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class HelpKeywordAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new HelpKeywordAttribute();
            Assert.Null(attribute.HelpKeyword);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Theory]
        [InlineData("")]
        [InlineData("keyword")]
        public void Ctor_Keyword(string keyword)
        {
            var attribute = new HelpKeywordAttribute(keyword);
            Assert.Same(keyword, attribute.HelpKeyword);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Ctor_NullKeyword_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("keyword", () => new HelpKeywordAttribute((string)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(HelpKeywordAttributeTests))]
        public void Ctor_Type(Type type)
        {
            var attribute = new HelpKeywordAttribute(type);
            Assert.Equal(type.FullName, attribute.HelpKeyword);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Ctor_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("t", () => new HelpKeywordAttribute((Type)null));
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            HelpKeywordAttribute attribute = HelpKeywordAttribute.Default;
            Assert.Same(attribute, HelpKeywordAttribute.Default);
            Assert.Null(attribute.HelpKeyword);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new HelpKeywordAttribute();

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new HelpKeywordAttribute(), true };
            yield return new object[] { attribute, new HelpKeywordAttribute("keyword"), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(HelpKeywordAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimes_ReturnsEqual()
        {
            var attribute = new HelpKeywordAttribute();
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
    }
}
