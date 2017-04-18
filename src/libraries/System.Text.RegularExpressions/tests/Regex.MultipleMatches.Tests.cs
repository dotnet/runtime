// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexMultipleMatchTests
    {

        [Fact]
        public void Matches_MultipleCapturingGroups()
        {
            string[] expectedGroupValues = { "abracadabra", "abra", "cad" };
            string[] expectedGroupCaptureValues = { "abracad", "abra" };

            // Another example - given by Brad Merril in an article on RegularExpressions
            Regex regex = new Regex(@"(abra(cad)?)+");
            string input = "abracadabra1abracadabra2abracadabra3";
            Match match = regex.Match(input);
            while (match.Success)
            {
                string expected = "abracadabra";
                Assert.Equal(expected, match.Value);

                Assert.Equal(3, match.Groups.Count);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    Assert.Equal(expectedGroupValues[i], match.Groups[i].Value);
                    if (i == 1)
                    {
                        Assert.Equal(2, match.Groups[i].Captures.Count);
                        for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                        {
                            Assert.Equal(expectedGroupCaptureValues[j], match.Groups[i].Captures[j].Value);
                        }
                    }
                    else if (i == 2)
                    {
                        Assert.Equal(1, match.Groups[i].Captures.Count);
                        Assert.Equal("cad", match.Groups[i].Captures[0].Value);
                    }
                }
                Assert.Equal(1, match.Captures.Count);
                Assert.Equal("abracadabra", match.Captures[0].Value);
                match = match.NextMatch();
            }
        }

        public static IEnumerable<object[]> Matches_TestData()
        {
            yield return new object[]
            {
                "[0-9]", "12345asdfasdfasdfljkhsda67890", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("1", 0, 1),
                    new CaptureData("2", 1, 1),
                    new CaptureData("3", 2, 1),
                    new CaptureData("4", 3, 1),
                    new CaptureData("5", 4, 1),
                    new CaptureData("6", 24, 1),
                    new CaptureData("7", 25, 1),
                    new CaptureData("8", 26, 1),
                    new CaptureData("9", 27, 1),
                    new CaptureData("0", 28, 1),
                }
            };

            yield return new object[]
            {
                "[a-z0-9]+", "[token1]? GARBAGEtoken2GARBAGE ;token3!", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("token1", 1, 6),
                    new CaptureData("token2", 17, 6),
                    new CaptureData("token3", 32, 6)
                }
            };

            yield return new object[]
            {
                "(abc){2}", " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("abcabc", 2, 6),
                    new CaptureData("abcabc", 125, 6)
                }
            };

            yield return new object[]
            {
                @"foo\d+", "0123456789foo4567890foo1foo  0987", RegexOptions.RightToLeft,
                new CaptureData[]
                {
                    new CaptureData("foo1", 20, 4),
                    new CaptureData("foo4567890", 10, 10),
                }
            };

            yield return new object[]
            {
                "[a-z]", "a", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("a", 0, 1)
                }
            };

            yield return new object[]
            {
                "[a-z]", "a1bc", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("a", 0, 1),
                    new CaptureData("b", 2, 1),
                    new CaptureData("c", 3, 1)
                }
            };

            // Alternation construct
            yield return new object[]
            {
                "(?(A)A123|C789)", "A123 B456 C789", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("A123", 0, 4),
                    new CaptureData("C789", 10, 4),
                }
            };

            yield return new object[]
            {
                "(?(A)A123|C789)", "A123 B456 C789", RegexOptions.None,
                new CaptureData[]
                {
                    new CaptureData("A123", 0, 4),
                    new CaptureData("C789", 10, 4),
                }
            };
        }

        [Theory]
        [MemberData(nameof(Matches_TestData))]
        public void Matches(string pattern, string input, RegexOptions options, CaptureData[] expected)
        {
            if (options == RegexOptions.None)
            {
                Regex regexBasic = new Regex(pattern);
                VerifyMatches(regexBasic.Matches(input), expected);
                VerifyMatches(regexBasic.Match(input), expected);

                VerifyMatches(Regex.Matches(input, pattern), expected);
                VerifyMatches(Regex.Match(input, pattern), expected);
            }

            Regex regexAdvanced = new Regex(pattern, options);
            VerifyMatches(regexAdvanced.Matches(input), expected);
            VerifyMatches(regexAdvanced.Match(input), expected);

            VerifyMatches(Regex.Matches(input, pattern, options), expected);
            VerifyMatches(Regex.Match(input, pattern, options), expected);
        }

        public static void VerifyMatches(Match match, CaptureData[] expected)
        {
            for (int i = 0; match.Success; i++, match = match.NextMatch())
            {
                VerifyMatch(match, expected[i]);
            }
        }

        public static void VerifyMatches(MatchCollection matches, CaptureData[] expected)
        {
            Assert.Equal(expected.Length, matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                VerifyMatch(matches[i], expected[i]);
            }
        }

        public static void VerifyMatch(Match match, CaptureData expected)
        {
            Assert.True(match.Success);
            Assert.Equal(expected.Value, match.Value);
            Assert.Equal(expected.Index, match.Index);
            Assert.Equal(expected.Length, match.Length);

            Assert.Equal(expected.Value, match.Groups[0].Value);
            Assert.Equal(expected.Index, match.Groups[0].Index);
            Assert.Equal(expected.Length, match.Groups[0].Length);

            Assert.Equal(1, match.Captures.Count);
            Assert.Equal(expected.Value, match.Captures[0].Value);
            Assert.Equal(expected.Index, match.Captures[0].Index);
            Assert.Equal(expected.Length, match.Captures[0].Length);
        }

        [Fact]
        public void Matches_Invalid()
        {
            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Matches(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Matches(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Matches(null, "pattern", RegexOptions.None, TimeSpan.FromSeconds(1)));

            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Matches(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Matches(null, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Matches("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Matches("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Matches("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Options are invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)(-1), TimeSpan.FromSeconds(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)0x400));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)0x400, TimeSpan.FromSeconds(1)));

            // MatchTimeout is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.Matches("input", "pattern", RegexOptions.None, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.Matches("input", "pattern", RegexOptions.None, TimeSpan.Zero));

            // Start is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Matches("input", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Matches("input", 6));
        }

        [Fact]
        public void NextMatch_EmptyMatch_ReturnsEmptyMatch()
        {
            Assert.Same(Match.Empty, Match.Empty.NextMatch());
        }
    }
}
