// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexFindOptimizationsTests
    {
        [Theory]

        [InlineData(@"^", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"^hello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"^hello$", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"^hi|^hello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]

        [InlineData(@"\G", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]
        [InlineData(@"\Ghello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]
        [InlineData(@"\Ghello$", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]
        [InlineData(@"\Ghi|\Ghello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]

        [InlineData(@"\Z", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]
        [InlineData(@"\Zhello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]
        [InlineData(@"\Zhello$", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]
        [InlineData(@"\Zhi|\Zhello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]

        [InlineData(@"\z", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]
        [InlineData(@"\zhello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]
        [InlineData(@"\zhello$", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]
        [InlineData(@"\zhi|\zhello", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]

        [InlineData(@"(?=^abc)", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=^)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=.*$)abc", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight)]
        [InlineData(@"(?=^)abc", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft)]
        [InlineData(@"abc(?=^)", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"(?<!42)(?=^)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?<=^)abc", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight)]
        [InlineData(@"(?<=^)(?=^)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=^)+abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=^\w+)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=\b)^abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=\b)(?=^.*$)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=\b)(?=\B)^abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"(?=^.*def)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)] 
        [InlineData(@"(?=^)(?=^)abc", 0, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]

        [InlineData(@"^", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"hello^", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"$hello^", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"hi^|hello^", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]

        [InlineData(@"\G", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]
        [InlineData(@"hello\G", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]
        [InlineData(@"$hello\G", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]
        [InlineData(@"hi\G|hello\G", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]

        [InlineData(@"\Z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]
        [InlineData(@"hello\Z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]
        [InlineData(@"$hello\Z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]
        [InlineData(@"hi\Z|hello\Z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]

        [InlineData(@"\z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        [InlineData(@"hello\z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        [InlineData(@"$hello\z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        [InlineData(@"hi\z|hello\z", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        public void LeadingAnchor_LeftToRight(string pattern, int options, int expectedMode)
        {
            Assert.Equal((FindNextStartingPositionMode)expectedMode, ComputeOptimizations(pattern, (RegexOptions)options).FindMode);
        }

        [Theory]
        [InlineData(@"abc\z", 0, (int)FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End, 3, (int)RegexNodeKind.End)]
        [InlineData(@"abc\Z", 0, (int)FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ, 3, (int)RegexNodeKind.EndZ)]
        [InlineData(@"abc$", 0, (int)FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ, 3, (int)RegexNodeKind.EndZ)]
        [InlineData(@"a{4,10}$", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, 10, (int)RegexNodeKind.EndZ)]
        [InlineData(@"(abc|defg){1,2}\z", 0, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, 8, (int)RegexNodeKind.End)]
        public void TrailingAnchor(string pattern, int options, int expectedMode, int expectedLength, int trailingAnchor)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, (RegexOptions)options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(expectedLength, opts.MaxPossibleLength);
            Assert.Equal((RegexNodeKind)trailingAnchor, opts.TrailingAnchor);
        }

        [Theory]
        [InlineData(@"ab", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ab")]
        [InlineData(@"ab", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"(a)(bc)", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "abc")]
        [InlineData(@"(a)(bc)", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "bc")]
        [InlineData(@"a{10}", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "aaaaaaaaaa")]
        [InlineData(@"a{10}", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "aaaaaaaaaa")]
        [InlineData(@"(?>a{10,20})", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "aaaaaaaaaa")]
        [InlineData(@"(?>a{10,20})", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "aaaaaaaaaa")]
        [InlineData(@"a{3,5}?", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "aaa")]
        [InlineData(@"a{3,5}?", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "aaa")]
        [InlineData(@"ab{5}", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "abbbbb")]
        [InlineData(@"ab{5}", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "abbbbb")]
        [InlineData(@"ab\w", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ab")]
        [InlineData(@"\wab", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"(ab){3}", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ababab")]
        [InlineData(@"(ab){3}", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"(ab){2,4}(de){4,}", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "abab")]
        [InlineData(@"(ab){2,4}(de){4,}", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "de")]
        [InlineData(@"ab|(abc)|(abcd)", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ab")]
        [InlineData(@"ab(?=cd)", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ab")]
        [InlineData(@"ab(?=cd)ef", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "abef")]
        [InlineData(@"ab(?=cd){2}ef", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "abef")]
        [InlineData(@"(?=)ab", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ab")]
        [InlineData(@"(?=a)bc", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "bc")]
        [InlineData(@"(?=ab)cd", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "cd")]
        [InlineData(@"(?=ab)", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "ab")]
        [InlineData(@"(?<=ab)cd", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "cd")]
        [InlineData(@"(?!ab)cd", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "cd")]
        [InlineData(@"(?<!ab)cd", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "cd")]
        [InlineData(@"ab(?=cd)", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"ab(?<=cd)", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"(?=cd)ab", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"(?<=cd)ab", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingString_RightToLeft, "ab")]
        [InlineData(@"\bab(?=\w)(?!=\d)c\b", 0, (int)FindNextStartingPositionMode.LeadingString_LeftToRight, "abc")]
        [InlineData(@"\bab(?=\w)(?!=\d)c\b", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "abc")]
        // Capture groups should be transparent to ordinal case-insensitive prefix extraction
        [InlineData(@"(abc)", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "abc")]
        [InlineData(@"\b(in)\b", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "in")]
        [InlineData(@"\b(from).+(to)\b", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "from")]
        // Partial capture: inner Concatenate not fully consumed, followed by non-letter One('(')
        [InlineData(@"(abcde|abcfg)\(", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "abc")]
        // Partial capture: inner Concatenate not fully consumed, followed by letter Set([Ee])
        [InlineData(@"(abc|abd)e", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "ab")]
        // Adjacent captures: both fully consumed via inner Concatenate recursion, extraction continues across capture boundaries
        [InlineData(@"(ab)(cd)", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "abcd")]
        // Non-capture content before capture: tests Set processing then Capture unwrap in same Concatenate iteration
        [InlineData(@"ab(cd)", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "abcd")]
        // Single-char capture unwraps to Set (not Concatenate), exercises direct Set handling after Capture unwrap
        [InlineData(@"a(b)c", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "abc")]
        // Empty capture unwraps to Empty node, which is skipped; extraction continues with subsequent content
        [InlineData(@"()ab", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "ab")]
        // Atomic groups inside a Concatenate are not unwrapped (conservative); extraction stops before them
        [InlineData(@"ab(?>cd)ef", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight, "ab")]
        public void LeadingPrefix(string pattern, int options, int expectedMode, string expectedPrefix)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, (RegexOptions)options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(expectedPrefix, opts.LeadingPrefix);
        }

        [Fact]
        [OuterLoop("Stress test for deep nesting")]
        public void LeadingPrefix_DeepCapatureNesting_DoesNotStackOverflow()
        {
            // Deeply nested captures like (((((...))))) with IgnoreCase exercise the recursive
            // Capture-unwrapping path in TryGetOrdinalCaseInsensitiveString. Verify it doesn't SO.
            const int Depth = 2000;
            string pattern = new string('(', Depth) + "ab" + new string(')', Depth);
            RegexFindOptimizations opts = ComputeOptimizations(pattern, RegexOptions.IgnoreCase);
            // The prefix may or may not be extracted depending on stack limits, but it must not crash.
            // If extraction succeeds, it should find "ab".
            if (opts.FindMode == FindNextStartingPositionMode.LeadingString_OrdinalIgnoreCase_LeftToRight)
            {
                Assert.Equal("ab", opts.LeadingPrefix);
            }
        }

        [Theory]
        [InlineData(@"[ab]", 0, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "ab")]
        [InlineData(@"[Aa]", 0, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "Aa")]
        [InlineData(@"a", (int)RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "Aa")]
        [InlineData(@"ab|cd|ef|gh", 0, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "aceg")]
        [InlineData(@"[ab]", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "ab")]
        [InlineData(@"[Aa]", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "Aa")]
        [InlineData(@"a", (int)RegexOptions.IgnoreCase | (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "Aa")]
        [InlineData(@"ab|cd|ef|gh", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "bdfh")]
        [InlineData(@"\bab(?=\w)(?!=\d)c\b", (int)(RegexOptions.IgnoreCase | RegexOptions.RightToLeft), (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "Cc")]
        [InlineData(@"ab|(abc)|(abcd)", (int)RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "bcd")]
        public void LeadingSet(string pattern, int options, int expectedMode, string expectedChars)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, (RegexOptions)options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(1, opts.FixedDistanceSets.Count);
            Assert.Equal(0, opts.FixedDistanceSets[0].Distance);
            Assert.Equal(expectedChars, new string(opts.FixedDistanceSets[0].Chars));
        }

        [Theory]
        [InlineData(@"\d*a", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, null, StringComparison.Ordinal, 'a', null)]
        [InlineData(@"\d*abc", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", StringComparison.Ordinal, 0, null)]
        [InlineData(@"(\d*)(abc)", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", StringComparison.Ordinal, 0, null)]
        [InlineData(@"((\d*)(abc))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", StringComparison.Ordinal, 0, null)]
        [InlineData(@"(?>\s*)(((abc)+){2,})", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", StringComparison.Ordinal, 0, null)]
        [InlineData(@"((((\s*)))((((?i)abc)+){2,}))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", StringComparison.OrdinalIgnoreCase, 0, null)]
        [InlineData(@"((((\s*)))((((?i)a)+){2,}))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, null, StringComparison.Ordinal, 0, new char[] { 'A', 'a' })]
        [InlineData(@"((((?>\s*)))((([Aa][Bb][Cc])+){2,}))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", StringComparison.OrdinalIgnoreCase, 0, null)]
        [InlineData(@"((((\s*)))((([Aa][Bb]c)+){2,}))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "ab", StringComparison.OrdinalIgnoreCase, 0, null)]
        [InlineData(@"((((?>\s*)))((([Aa]bc)+){2,}))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, null, StringComparison.Ordinal, 0, new char[] { 'A', 'a' })]
        [InlineData(@"((((\s*)))((([Sst])+){2,}))", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, null, StringComparison.Ordinal, 0, new char[] { 'S', 's', 't' })]
        [InlineData(@"\d*[AaBb]{3,}", 0, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, null, StringComparison.Ordinal, 0, new char[] { 'A', 'B', 'a', 'b' })]
        public void LiteralAfterLoop(string pattern, int options, int expectedMode, string? expectedString, StringComparison expectedStringComparison, char expectedChar, char[]? expectedSet)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, (RegexOptions)options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.NotNull(opts.LiteralAfterLoop);
            Assert.Equal(expectedString, opts.LiteralAfterLoop.Value.Literal.String);
            Assert.Equal(expectedStringComparison, opts.LiteralAfterLoop.Value.Literal.StringComparison);
            Assert.Equal(expectedChar, opts.LiteralAfterLoop.Value.Literal.Char);
            Assert.Equal(expectedSet, opts.LiteralAfterLoop.Value.Literal.Chars);
        }

        [Theory]
        // Best FixedDistanceSet has high-frequency chars (avg freq >> 0.6) → LeadingStrings preferred
        [InlineData(@"abc|def|ghi", (int)RegexOptions.Compiled, (int)FindNextStartingPositionMode.LeadingStrings_LeftToRight)]
        [InlineData(@"agggtaaa|tttaccct", (int)RegexOptions.Compiled, (int)FindNextStartingPositionMode.LeadingStrings_LeftToRight)]
        // Best FixedDistanceSet has low-frequency chars (avg freq < 0.6) → FixedDistanceSets preferred
        [InlineData(@"ABC|DEF|GHI", (int)RegexOptions.Compiled, (int)FindNextStartingPositionMode.FixedDistanceSets_LeftToRight)]
        [InlineData(@"Sherlock|Holmes|Watson|Irene|Adler|John|Baker", (int)RegexOptions.Compiled, (int)FindNextStartingPositionMode.FixedDistanceSets_LeftToRight)]
        // Best FixedDistanceSet has a single char → IndexOf is faster than multi-string search regardless of frequency
        [InlineData(@"Sherlock|Street", (int)RegexOptions.Compiled, (int)FindNextStartingPositionMode.FixedDistanceSets_LeftToRight)]
        // Best FixedDistanceSet has non-ASCII chars → falls through (no frequency data)
        [InlineData("\u00e9lan|\u00e8re", (int)RegexOptions.Compiled, (int)FindNextStartingPositionMode.FixedDistanceSets_LeftToRight)]
        // Without Compiled (interpreter), LeadingStrings is not used
        [InlineData(@"abc|def|ghi", 0, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight)]
        public void LeadingStrings_FrequencyHeuristic(string pattern, int options, int expectedMode)
        {
            Assert.Equal((FindNextStartingPositionMode)expectedMode, ComputeOptimizations(pattern, (RegexOptions)options).FindMode);
        }

        [Theory]
        [InlineData(@".ab", 0, (int)FindNextStartingPositionMode.FixedDistanceString_LeftToRight, "ab", 1)]
        [InlineData(@".ab\w\w\wcdef\w\w\w\w\wghijklmnopq\w\w\w", 0, (int)FindNextStartingPositionMode.FixedDistanceString_LeftToRight, "ghijklmnopq", 15)]
        [InlineData(@"a[Bb]c[Dd]ef", 0, (int)FindNextStartingPositionMode.FixedDistanceString_LeftToRight, "ef", 4)]
        [InlineData(@"a[Bb]cd[Ee]fgh[Ii]", 0, (int)FindNextStartingPositionMode.FixedDistanceString_LeftToRight, "fgh", 5)]
        public void FixedDistanceString(string pattern, int options, int expectedMode, string expectedString, int distance)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, (RegexOptions)options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(expectedString, opts.FixedDistanceLiteral.String);
            Assert.Equal(distance, opts.FixedDistanceLiteral.Distance);
        }

        private static RegexFindOptimizations ComputeOptimizations(string pattern, RegexOptions options)
        {
            RegexTree tree = RegexParser.Parse(pattern, options, CultureInfo.InvariantCulture);
            return RegexFindOptimizations.Create(tree.Root, options);
        }
    }
}
