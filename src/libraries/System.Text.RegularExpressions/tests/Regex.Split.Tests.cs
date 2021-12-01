// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexSplitTests
    {
        public static IEnumerable<object[]> Split_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "", "", RegexOptions.None, 0, 0, new string[] { "", "" } };
                yield return new object[] { engine, "123", "abc", RegexOptions.None, 3, 0, new string[] { "abc" } };

                yield return new object[] { engine, "    ", "word0    word1    word2    word3", RegexOptions.None, 32, 0, new string[] { "word0", "word1", "word2", "word3" } };

                yield return new object[] { engine, ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 19, 0, new string[] { "kkk", "lll", "mmm", "nnn", "ooo" } };
                yield return new object[] { engine, ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 0, 0, new string[] { "kkk", "lll", "mmm", "nnn", "ooo" } };

                // IgnoreCase
                yield return new object[] { engine, "[abc]", "1A2B3C4", RegexOptions.IgnoreCase, 7, 0, new string[] { "1", "2", "3", "4" } };

                // Custom index
                yield return new object[] { engine, ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 2, 0, new string[] { "kkk", "lll:mmm:nnn:ooo" } };
                yield return new object[] { engine, ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 3, 6, new string[] { "kkk:lll", "mmm", "nnn:ooo" } };

                // Tricky corner cases involving empty matches of anchors
                yield return new object[] { engine, @"\b", "Hello World!", RegexOptions.None, 3, 6, new string[] { "Hello ", "World", "!" } };
                yield return new object[] { engine, @"\b", "Hello World!", RegexOptions.None, 0, 0, new string[] { "", "Hello", " ", "World", "!" } };
                yield return new object[] { engine, @"^", "Hello \nWorld!", RegexOptions.None | RegexOptions.Multiline, 0, 0, new string[] { "", "Hello \n", "World!" } };
                yield return new object[] { engine, @"$", "Hello \nWorld!", RegexOptions.None | RegexOptions.Multiline, 0, 0, new string[] { "Hello ", "\nWorld!", "" } };

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    yield return new object[] { engine, @"(\s)?(-)", "once -upon-a time", RegexOptions.None, 17, 0, new string[] { "once", " ", "-", "upon", "-", "a time" } };
                    yield return new object[] { engine, @"(\s)?(-)", "once upon a time", RegexOptions.None, 16, 0, new string[] { "once upon a time" } };
                    yield return new object[] { engine, @"(\s)?(-)", "once - -upon- a- time", RegexOptions.None, 21, 0, new string[] { "once", " ", "-", "", " ", "-", "upon", "-", " a", "-", " time" } };

                    yield return new object[] { engine, "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.None, 19, 0, new string[] { "123", "b", "d", "456aBCDe789" } };
                    yield return new object[] { engine, "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.IgnoreCase, 19, 0, new string[] { "123", "b", "d", "456", "B", "D", "789" } };

                    yield return new object[] { engine, "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.None, 19, 0, new string[] { "123", "d", "b", "456aBCDe789" } };
                    yield return new object[] { engine, "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.IgnoreCase, 19, 0, new string[] { "123", "d", "b", "456", "D", "B", "789" } };

                    // RightToLeft
                    yield return new object[] { engine, "", "", RegexOptions.RightToLeft, 0, 0, new string[] { "", "" } };
                    yield return new object[] { engine, "123", "abc", RegexOptions.RightToLeft, 3, 0, new string[] { "abc" } };

                    yield return new object[] { engine, "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft, 19, 19, new string[] { "123", "d", "b", "456aBCDe789" } };
                    yield return new object[] { engine, "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 19, 19, new string[] { "123", "d", "b", "456", "D", "B", "789" } };

                    yield return new object[] { engine, "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft, 19, 19, new string[] { "123", "b", "d", "456aBCDe789" } };
                    yield return new object[] { engine, "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 19, 19, new string[] { "123", "b", "d", "456", "B", "D", "789" } };

                    yield return new object[] { engine, "foo", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 32, 32, new string[] { "0123456789", "4567890", "         " } };

                    yield return new object[] { engine, @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 20, 20, new string[] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "k" } };
                    yield return new object[] { engine, @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 10, 20, new string[] { "1a", "b", "c", "d", "e", "f", "g", "h", "i", "k" } };
                    yield return new object[] { engine, @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 2, 20, new string[] { "1a2b3c4d5e6f7g8h9i", "k" } };
                    yield return new object[] { engine, @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 1, 20, new string[] { "1a2b3c4d5e6f7g8h9i0k" } };

                    // Anchors
                    yield return new object[] { engine, @"(?<=\G..)(?=..)", "aabbccdd", RegexOptions.None, 8, 0, new string[] { "aa", "bb", "cc", "dd" } };
                }

                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    // RegexOptions.NonBacktracking also ignores named captures as if they were not given
                    yield return new object[] { engine, "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.None, 19, 0, new string[] { "123", "456aBCDe789" } };
                    yield return new object[] { engine, "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.IgnoreCase, 19, 0, new string[] { "123", "456", "789" } };
                }

                // RegexOptions.NonBacktracking is similar to RegexOptions.ExplicitCapture when there are no explicit capture names because NonBacktracking does not support captures
                // This is the reason for including the RegexOptions.ExplicitCapture options also to check that the results are the same in this case
                foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.ExplicitCapture })
                {
                    if (options != RegexOptions.None || RegexHelpers.IsNonBacktracking(engine))
                    {
                        yield return new object[] { engine, @"(\s)?(-)", "once -upon-a time", options, 17, 0, new string[] { "once", "upon", "a time" } };
                        yield return new object[] { engine, @"(\s)?(-)", "once upon a time", options, 16, 0, new string[] { "once upon a time" } };
                        yield return new object[] { engine, @"(\s)?(-)", "once - -upon- a- time", options, 21, 0, new string[] { "once", "", "upon", " a", " time" } };
                        yield return new object[] { engine, "a(.)c(.)e", "123abcde456aBCDe789", options, 19, 0, new string[] { "123", "456aBCDe789" } };
                        yield return new object[] { engine, "a.c.e", "123abcde456aBCDe789", options, 19, 0, new string[] { "123", "456aBCDe789" } };
                        yield return new object[] { engine, "a.c.e", "123abcde456aBCDe789", options | RegexOptions.IgnoreCase, 19, 0, new string[] { "123", "456", "789" } };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Split_TestData))]
        public async Task Split(RegexEngine engine, string pattern, string input, RegexOptions options, int count, int start, string[] expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, count);

            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            if (isDefaultStart && isDefaultCount)
            {
                Assert.Equal(expected, r.Split(input));
            }

            if (isDefaultStart)
            {
                Assert.Equal(expected, r.Split(input, count));
            }

            Assert.Equal(expected, r.Split(input, count, start));
        }

        [Fact]
        public void Split_Invalid()
        {
            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Split(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Split(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Split(null, "pattern", RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Split(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Split(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Split(null, 0, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Split("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Split("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Split("input", null, RegexOptions.None, TimeSpan.FromMilliseconds(1)));

            // Count is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").Split("input", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").Split("input", -1, 0));

            // Start is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Split("input", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Split("input", 0, 6));
        }
    }
}
