// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class RegularExpressionAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new RegularExpressionAttribute("SomePattern"), null);
            yield return new TestCase(new RegularExpressionAttribute("SomePattern"), string.Empty);
            yield return new TestCase(new RegularExpressionAttribute("defghi") { MatchTimeoutInMilliseconds = 5000 }, "defghi");
            yield return new TestCase(new RegularExpressionAttribute("[^a]+\\.[^z]+") { MatchTimeoutInMilliseconds = 10000 }, "bcdefghijklmnopqrstuvwxyz.abcdefghijklmnopqrstuvwxy");
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new RegularExpressionAttribute("defghi"), "zyxwvu");
            yield return new TestCase(new RegularExpressionAttribute("defghi"), "defghijkl");
            yield return new TestCase(new RegularExpressionAttribute("defghi"), "abcdefghi");
            yield return new TestCase(new RegularExpressionAttribute("defghi"), "abcdefghijkl");
            yield return new TestCase(new RegularExpressionAttribute("[^a]+\\.[^z]+") { MatchTimeoutInMilliseconds = 10000 } , "aaaaa");
            yield return new TestCase(new RegularExpressionAttribute("[^a]+\\.[^z]+") { MatchTimeoutInMilliseconds = 10000 }, "zzzzz");
            yield return new TestCase(new RegularExpressionAttribute("[^a]+\\.[^z]+") { MatchTimeoutInMilliseconds = 10000 }, "b.z");
            yield return new TestCase(new RegularExpressionAttribute("[^a]+\\.[^z]+") { MatchTimeoutInMilliseconds = 10000 }, "a.y");
        }

        [Theory]
        [InlineData("SomePattern")]
        public static void Ctor_String(string pattern)
        {
            var attribute = new RegularExpressionAttribute(pattern);
            Assert.Equal(pattern, attribute.Pattern);
        }

        [Theory]
        [InlineData(12345)]
        public static void MatchTimeoutInMilliseconds_GetSet_ReturnsExpected(int newValue)
        {
            var attribute = new RegularExpressionAttribute("SomePattern");
            attribute.MatchTimeoutInMilliseconds = newValue;
            Assert.Equal(newValue, attribute.MatchTimeoutInMilliseconds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public static void Validate_InvalidPattern_ThrowsInvalidOperationException(string pattern)
        {
            var attribute = new RegularExpressionAttribute(pattern);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Any", new ValidationContext(new object())));
        }

        [Fact]
        public static void Validate_ZeroMatchTimeoutInMilliseconds_ThrowsArgumentOutOfRangeException()
        {
            RegularExpressionAttribute attribute = new RegularExpressionAttribute("[^a]+\\.[^z]+") { MatchTimeoutInMilliseconds = 0 };
            Assert.Throws<ArgumentOutOfRangeException>("matchTimeout", () => attribute.Validate("a", new ValidationContext(new object())));
        }

        [Fact]
        public static void Validate_MatchingTimesOut_ThrowsRegexMatchTimeoutException()
        {
            RegularExpressionAttribute attribute = new RegularExpressionAttribute("(a+)+$") { MatchTimeoutInMilliseconds = 1 };
            Assert.Throws<RegexMatchTimeoutException>(() => attribute.Validate("aaaaaaaaaaaaaaaaaaaaaaaaaaaa>", new ValidationContext(new object())));
        }
    }
}
