// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexFindOptimizationsTests
    {
        [Theory]

        [InlineData(@"^", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"^hello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"^hello$", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]
        [InlineData(@"^hi|^hello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning)]

        [InlineData(@"\G", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]
        [InlineData(@"\Ghello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]
        [InlineData(@"\Ghello$", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]
        [InlineData(@"\Ghi|\Ghello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start)]

        [InlineData(@"\Z", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]
        [InlineData(@"\Zhello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]
        [InlineData(@"\Zhello$", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]
        [InlineData(@"\Zhi|\Zhello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ)]

        [InlineData(@"\z", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]
        [InlineData(@"\zhello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]
        [InlineData(@"\zhello$", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]
        [InlineData(@"\zhi|\zhello", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End)]

        [InlineData(@"^", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"hello^", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"$hello^", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]
        [InlineData(@"hi^|hello^", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning)]

        [InlineData(@"\G", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]
        [InlineData(@"hello\G", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]
        [InlineData(@"$hello\G", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]
        [InlineData(@"hi\G|hello\G", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start)]

        [InlineData(@"\Z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]
        [InlineData(@"hello\Z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]
        [InlineData(@"$hello\Z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]
        [InlineData(@"hi\Z|hello\Z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ)]

        [InlineData(@"\z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        [InlineData(@"hello\z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        [InlineData(@"$hello\z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        [InlineData(@"hi\z|hello\z", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)]
        public void LeadingAnchor_LeftToRight(string pattern, RegexOptions options, int expectedMode)
        {
            Assert.Equal((FindNextStartingPositionMode)expectedMode, ComputeOptimizations(pattern, options).FindMode);
        }

        [Theory]
        [InlineData(@"abc\z", RegexOptions.None, (int)FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End, 3, (int)RegexNodeKind.End)]
        [InlineData(@"abc\Z", RegexOptions.None, (int)FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ, 3, (int)RegexNodeKind.EndZ)]
        [InlineData(@"abc$", RegexOptions.None, (int)FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ, 3, (int)RegexNodeKind.EndZ)]
        [InlineData(@"a{4,10}$", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, 10, (int)RegexNodeKind.EndZ)]
        [InlineData(@"(abc|defg){1,2}\z", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, 8, (int)RegexNodeKind.End)]
        public void TrailingAnchor(string pattern, RegexOptions options, int expectedMode, int expectedLength, int trailingAnchor)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(expectedLength, opts.MaxPossibleLength);
            Assert.Equal((RegexNodeKind)trailingAnchor, opts.TrailingAnchor);
        }

        [Theory]
        [InlineData(@"ab", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "ab")]
        [InlineData(@"ab", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "ab")]
        [InlineData(@"(a)(bc)", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "abc")]
        [InlineData(@"(a)(bc)", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "bc")]
        [InlineData(@"a{10}", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "aaaaaaaaaa")]
        [InlineData(@"a{10}", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "aaaaaaaaaa")]
        [InlineData(@"(?>a{10,20})", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "aaaaaaaaaa")]
        [InlineData(@"(?>a{10,20})", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "aaaaaaaaaa")]
        [InlineData(@"a{3,5}?", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "aaa")]
        [InlineData(@"a{3,5}?", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "aaa")]
        [InlineData(@"ab{5}", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "abbbbb")]
        [InlineData(@"ab{5}", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "abbbbb")]
        [InlineData(@"ab\w", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "ab")]
        [InlineData(@"\wab", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "ab")]
        [InlineData(@"(ab){3}", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "ababab")]
        [InlineData(@"(ab){3}", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "ab")]
        [InlineData(@"(ab){2,4}(de){4,}", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "abab")]
        [InlineData(@"(ab){2,4}(de){4,}", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "de")]
        [InlineData(@"ab|(abc)|(abcd)", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "ab")]
        [InlineData(@"ab|(abc)|(abcd)", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "ab")]
        [InlineData(@"ab(?=cd)", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingPrefix_LeftToRight, "ab")]
        [InlineData(@"ab(?=cd)", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingPrefix_RightToLeft, "ab")]
        public void LeadingPrefix(string pattern, RegexOptions options, int expectedMode, string expectedPrefix)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(expectedPrefix, opts.LeadingPrefix);
        }

        [Theory]
        [InlineData(@"[ab]", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "ab")]
        [InlineData(@"[Aa]", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "Aa")]
        [InlineData(@"a", RegexOptions.IgnoreCase, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "Aa")]
        [InlineData(@"ab|cd|ef|gh", RegexOptions.None, (int)FindNextStartingPositionMode.LeadingSet_LeftToRight, "aceg")]
        [InlineData(@"[ab]", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "ab")]
        [InlineData(@"[Aa]", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "Aa")]
        [InlineData(@"a", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "Aa")]
        [InlineData(@"ab|cd|ef|gh", RegexOptions.RightToLeft, (int)FindNextStartingPositionMode.LeadingSet_RightToLeft, "bdfh")]
        public void LeadingSet(string pattern, RegexOptions options, int expectedMode, string expectedChars)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.Equal(1, opts.FixedDistanceSets.Count);
            Assert.Equal(0, opts.FixedDistanceSets[0].Distance);
            Assert.Equal(expectedChars, new string(opts.FixedDistanceSets[0].Chars));
        }

        [Theory]
        [InlineData(@"\d*a", RegexOptions.None, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, null, 'a')]
        [InlineData(@"\d*abc", RegexOptions.None, (int)FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight, "abc", 0)]
        public void LiteralAfterLoop(string pattern, RegexOptions options, int expectedMode, string? expectedString, char expectedChar)
        {
            RegexFindOptimizations opts = ComputeOptimizations(pattern, options);
            Assert.Equal((FindNextStartingPositionMode)expectedMode, opts.FindMode);
            Assert.NotNull(opts.LiteralAfterLoop);
            Assert.Equal(expectedString, opts.LiteralAfterLoop.Value.Literal.String);
            Assert.Equal(expectedChar, opts.LiteralAfterLoop.Value.Literal.Char);
        }

        private static RegexFindOptimizations ComputeOptimizations(string pattern, RegexOptions options)
        {
            RegexTree tree = RegexParser.Parse(pattern, options, CultureInfo.InvariantCulture);
            return new RegexFindOptimizations(tree.Root, options);
        }
    }
}
