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
        [InlineData(@"[\0-ad-\uFFFF-[a-d]]", @"[\0-ad-\uFFFF-[a-d]]")]
        public void DescribeSet(string set, string expected)
        {
            RegexNode setNode = RegexParser.Parse($"{set}", RegexOptions.None, CultureInfo.InvariantCulture).Root.Child(0);
            Assert.Equal(expected, RegexCharClass.DescribeSet(setNode.Str!));
        }

        [Theory]
        [InlineData(@"[\w]", false, false, false, false, false, '\0', '\0')]
        [InlineData(@"[a\p{L}]", false, false, false, false, false, '\0', '\0')]
        [InlineData(@"[\p{IsGreek}a]", false, false, false, false, true, 'a', '\u0400')]
        [InlineData(@"[a-z]", false, false, false, true, true, 'a', (char)('z' + 1))]
        [InlineData(@"[a-\u0080]", false, false, false, false, true, 'a', '\u0081')]
        [InlineData(@"[\0-\u007F]", true, false, false, true, true, '\0', '\u0080')]
        [InlineData(@"[\0-\u0081]", true, false, false, false, true, '\0', '\u0082')]
        [InlineData(@"[\0-\u0081-[a]]", false, false, false, false, true, '\0', '\u0082')]
        [InlineData(@"[\0-\u0081-[\u0081]]", false, false, false, false, true, '\0', '\u0082')]
        [InlineData(@"[^a-z]", false, true, false, false, true, 'a', (char)('z' + 1))]
        [InlineData(@"[^a-\u0080]", false, false, false, false, true, 'a', '\u0081')]
        [InlineData(@"[^\u0080-\u0082]", true, false, false, false, true, '\u0080', '\u0083')]
        [InlineData(@"[^\0-\u007F]", false, true, true, false, true, '\0', '\u0080')]
        [InlineData(@"[^\0-\u0080]", false, false, true, false, true, '\0', '\u0081')]
        [InlineData(@"[\u0001-\u007F]", false, false, false, true, true, '\u0001', '\u0080')]
        [InlineData(@"[\u0000-\u0010\u0012-\u007F]", false, false, false, true, true, '\u0000', '\u0080')]
        [InlineData(@"[a-z-[b-d]]", false, false, false, true, true, 'a', (char)('z' + 1))]
        [InlineData(@"[\0-\u007F-[b-d]]", false, false, false, true, true, '\0', '\u0080')]
        public void Analyze(
            string set,
            bool allAsciiContained,
            bool allNonAsciiContained,
            bool containsNoAscii,
            bool containsOnlyAscii,
            bool onlyRanges,
            char lowerBoundInclusiveIfOnlyRanges,
            char upperBoundExclusiveIfOnlyRanges)
        {
            RegexNode setNode = RegexParser.Parse($"{set}", RegexOptions.None, CultureInfo.InvariantCulture).Root.Child(0);
            RegexCharClass.CharClassAnalysisResults results = RegexCharClass.Analyze(setNode.Str!);

            Assert.Equal(allAsciiContained, results.AllAsciiContained);
            Assert.Equal(allNonAsciiContained, results.AllNonAsciiContained);
            Assert.Equal(containsNoAscii, results.ContainsNoAscii);
            Assert.Equal(containsOnlyAscii, results.ContainsOnlyAscii);
            Assert.Equal(onlyRanges, results.OnlyRanges);
            Assert.Equal(lowerBoundInclusiveIfOnlyRanges, results.LowerBoundInclusiveIfOnlyRanges);
            Assert.Equal(upperBoundExclusiveIfOnlyRanges, results.UpperBoundExclusiveIfOnlyRanges);
        }
    }
}
