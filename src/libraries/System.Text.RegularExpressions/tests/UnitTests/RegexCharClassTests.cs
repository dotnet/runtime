// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCharClassTests
    {
        [Theory]
        [InlineData(@"[a\a]", @"[\aa]")]
        [InlineData(@"[a\b]", @"[\ba]")]
        [InlineData(@"[a\f]", @"[\fa]")]
        [InlineData(@"[a\r]", @"[\ra]")]
        [InlineData(@"[a\n]", @"[\na]")]
        [InlineData(@"[a\t]", @"[\ta]")]
        [InlineData(@"[a\v]", @"[\va]")]
        [InlineData(@"[a\\]", @"[\\a]")]
        [InlineData(@"[\r\a\b\n\f\t\v\\]", @"[\a-\r\\]")]
        [InlineData(@"[ab]", @"[ab]")]
        [InlineData(@"[^ab]", @"[^ab]")]
        [InlineData(@"[abcdefg]", @"[a-g]")]
        [InlineData(@"[\p{L}]", @"[\p{L}]")]
        [InlineData(@"[\w\W\s\S\d\D]", @"[\w\W\s\S\d\D]")]
        [InlineData(@"[\wabc]", @"[a-c\w]")]
        [InlineData(@"[A-Zabc]", @"[A-Za-c]")]
        [InlineData(@"[\p{IsGreek}]", @"[\u0370-\u03FF]")]
        [InlineData(@"[\0-ad-\uFFFF]", @"[^bc]")]
        [InlineData(@"[\0-ad-\uFFFF-[a-d]]", @"[\u0000-ad-\uFFFF-[a-d]]")]
        public void DescribeSet(string set, string expected)
        {
            RegexNode setNode = RegexParser.Parse($"{set}", RegexOptions.None, CultureInfo.InvariantCulture).Root.Child(0);
            Assert.Equal(expected, RegexCharClass.DescribeSet(setNode.Str!));
        }
    }
}
