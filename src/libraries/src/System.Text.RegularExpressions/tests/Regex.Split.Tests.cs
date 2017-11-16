// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexSplitTests
    {
        public static IEnumerable<object[]> Split_NonCompiled_TestData()
        {
            yield return new object[] { "    ", "word0    word1    word2    word3", RegexOptions.None, 32, 0, new string[] { "word0", "word1", "word2", "word3" } };

            yield return new object[] { ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 19, 0, new string[] { "kkk", "lll", "mmm", "nnn", "ooo" } };
            yield return new object[] { ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 0, 0, new string[] { "kkk", "lll", "mmm", "nnn", "ooo" } };
            
            yield return new object[] { @"(\s)?(-)", "once -upon-a time", RegexOptions.None, 17, 0, new string[] { "once", " ", "-", "upon", "-", "a time" } };
            yield return new object[] { @"(\s)?(-)", "once upon a time", RegexOptions.None, 16, 0, new string[] { "once upon a time" } };
            yield return new object[] { @"(\s)?(-)", "once - -upon- a- time", RegexOptions.None, 21, 0, new string[] { "once", " ", "-", "", " ", "-", "upon", "-", " a", "-", " time" } };

            yield return new object[] { "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.None, 19, 0, new string[] { "123", "b", "d", "456aBCDe789" } };
            yield return new object[] { "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.IgnoreCase, 19, 0, new string[] { "123", "b", "d", "456", "B", "D", "789" } };

            yield return new object[] { "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.None, 19, 0, new string[] { "123", "d", "b", "456aBCDe789" } };
            yield return new object[] { "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.IgnoreCase, 19, 0, new string[] { "123", "d", "b", "456", "D", "B", "789" } };

            // RightToLeft
            yield return new object[] { "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft, 19, 19, new string[] { "123", "d", "b", "456aBCDe789" } };
            yield return new object[] { "a(.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 19, 19, new string[] { "123", "d", "b", "456", "D", "B", "789" } };

            yield return new object[] { "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft, 19, 19, new string[] { "123", "b", "d", "456aBCDe789" } };
            yield return new object[] { "a(?<dot1>.)c(.)e", "123abcde456aBCDe789", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 19, 19, new string[] { "123", "b", "d", "456", "B", "D", "789" } };

            // IgnoreCase
            yield return new object[] { "[abc]", "1A2B3C4", RegexOptions.IgnoreCase, 7, 0, new string[] { "1", "2", "3", "4" } };

            // Custom index
            yield return new object[] { ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 2, 0, new string[] { "kkk", "lll:mmm:nnn:ooo" } };
            yield return new object[] { ":", "kkk:lll:mmm:nnn:ooo", RegexOptions.None, 3, 6, new string[] { "kkk:lll", "mmm", "nnn:ooo" } };

            // RightToLeft
            yield return new object[] { "foo", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 32, 32, new string[] { "0123456789", "4567890", "         " } };

            yield return new object[] { @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 20, 20, new string[] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "k" } };
            yield return new object[] { @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 10, 20, new string[] { "1a", "b", "c", "d", "e", "f", "g", "h", "i", "k" } };
            yield return new object[] { @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 2, 20, new string[] { "1a2b3c4d5e6f7g8h9i", "k" } };
            yield return new object[] { @"\d", "1a2b3c4d5e6f7g8h9i0k", RegexOptions.RightToLeft, 1, 20, new string[] { "1a2b3c4d5e6f7g8h9i0k" } };
        }

        [Theory]
        [MemberData(nameof(Split_NonCompiled_TestData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Split_NonCompiled_TestData), 2, MemberType = typeof(RegexCompilationHelper))]
        public void Split(string pattern, string input, RegexOptions options, int count, int start, string[] expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultStart(input, options, count);
            if (options == RegexOptions.None)
            {
                // Use Split(string), Split(string, string), Split(string, int) or Split(string, int, int)
                if (isDefaultStart && isDefaultCount)
                {
                    // Use Split(string) or Split(string, string)
                    Assert.Equal(expected, new Regex(pattern).Split(input));
                    Assert.Equal(expected, Regex.Split(input, pattern));
                }
                if (isDefaultStart)
                {
                    // Use Split(string, int)
                    Assert.Equal(expected, new Regex(pattern).Split(input, count));
                }
                // Use Split(string, int, int)
                Assert.Equal(expected, new Regex(pattern).Split(input, count, start));
            }
            if (isDefaultStart && isDefaultCount)
            {
                // Use Split(string, string, RegexOptions)
                Assert.Equal(expected, Regex.Split(input, pattern, options));
            }
            if (isDefaultStart)
            {
                // Use Split(string, int)
                Assert.Equal(expected, new Regex(pattern, options).Split(input, count));
            }
            // Use Split(string, int, int, int)
            Assert.Equal(expected, new Regex(pattern, options).Split(input, count, start));
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
