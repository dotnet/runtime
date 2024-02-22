// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexPrefixAnalyzerTests
    {
        [Theory]
        // Produce starting sets
        [InlineData(@"a", 0, "\0\u0002\0ab")]
        [InlineData(@"(a)", 0, "\0\u0002\0ab")]
        [InlineData(@"(a)+", 0, "\0\u0002\0ab")]
        [InlineData(@"abc", 0, "\0\u0002\0ab")]
        [InlineData(@"abc", (int)RegexOptions.RightToLeft, "\0\u0002\0cd")]
        [InlineData(@"abc|def", (int)RegexOptions.RightToLeft, "\0\u0004\0cdfg")]
        [InlineData(@"a?b", 0, "\0\u0002\0ac")]
        [InlineData(@"a?[bcd]", 0, "\0\u0002\0ae")]
        [InlineData(@"a?[bcd]*[xyz]", 0, "\0\u0004\0aex{")]
        [InlineData(@"[a-c]", 0, "\0\u0002\0ad")]
        [InlineData(@"a+b+c+", 0, "\0\u0002\0ab")]
        [InlineData(@"a*b+c+", 0, "\0\u0002\0ac")]
        [InlineData(@"a*b*c+", 0, "\0\u0002\0ad")]
        [InlineData(@".", 0, "\u0001\u0002\u0000\u000A\u000B")]
        [InlineData(@".|\n", 0, "\0\u0001\0\0")]
        [InlineData(@"[^\n]?[\n]", 0, "\0\u0001\0\0")]
        [InlineData(@"[^a]?[a]", 0, "\0\u0001\0\0")]
        [InlineData(@"(abc)?(?(\1)yes|no)", 0, "\0\u0006\0abnoyz")]
        [InlineData(@"(abc)?(?(xyz)yes|no)", 0, "\0\u0006\0abnoyz")]
        [InlineData(@"[^a-zA-Z0-9_.]", 0, "\u0001\u000A\u0000./0:A[_`a{")]
        // Can't produce starting sets
        [InlineData(@"", 0, null)]
        [InlineData(@"a*", 0, null)]
        [InlineData(@"a*b*", 0, null)]
        [InlineData(@"a*b*c*", 0, null)]
        [InlineData(@"a*|b*", 0, null)]
        [InlineData(@"(a)*", 0, null)]
        [InlineData(@"(?:a)*", 0, null)]
        [InlineData(@"(a*)+", 0, null)]
        [InlineData(@"(a*)+?", 0, null)]
        [InlineData(@"[^ab]|a", 0, null)]
        [InlineData(@"[^ab]|ab", 0, null)]
        [InlineData(@"[^ab]?[a]", 0, null)]
        [InlineData(@"[^ab]?(a)+", 0, null)]
        [InlineData(@"(abc)?\1", 0, null)]
        [InlineData(@"[abc-[bc]]|[def]", 0, null)]
        [InlineData(@"[def]|[abc-[bc]]", 0, null)]
        [InlineData(@"(abc)?(?(\1)d*|f)", 0, null)]
        [InlineData(@"(abc)?(?(\1)d|f*)", 0, null)]
        [InlineData(@"(abc)?(?(xyz)d*|f)", 0, null)]
        [InlineData(@"(abc)?(?(xyz)d|f*)", 0, null)]
        public void FindFirstCharClass(string pattern, int options, string? expectedSet)
        {
            RegexTree tree = RegexParser.Parse(pattern, (RegexOptions)options, CultureInfo.InvariantCulture);
            string actualSet = RegexPrefixAnalyzer.FindFirstCharClass(tree.Root);
            if (expectedSet != actualSet)
            {
                throw TrueException.ForNonTrueValue($"Expected {FormatSet(expectedSet)}, got {FormatSet(actualSet)}", true);
            }
        }

        [Fact]
        public void FindFirstCharClass_StressDeep()
        {
            int nesting = 8000;
            FindFirstCharClass(string.Concat(Enumerable.Repeat($"(a?", nesting).Concat(Enumerable.Repeat(")*", nesting))), 0, null);
        }

        [Theory]
        // case-sensitive
        [InlineData("abc", new[] { "abc" }, false)]
        [InlineData("(abc+|bcd+)", new[] { "abc", "bcd" }, false)]
        [InlineData("(ab+c|bcd+)", new[] { "ab", "bcd" }, false)]
        [InlineData("(ab+c|bcd+)*", null, false)]
        [InlineData("(ab+c|bcd+)+", new[] { "ab", "bcd" }, false)]
        [InlineData("(ab+c|bcd+){3,5}", new[] { "ab", "bcd" }, false)]
        [InlineData("abc|def", new[] { "abc", "def" }, false)]
        [InlineData("ab{4}c|def{5}|g{2,4}h", new[] { "abbbbc", "defffff", "gg" }, false)]
        [InlineData("abc|def|(ghi|jklm)", new[] { "abc", "def", "ghi", "jklm" }, false)]
        [InlineData("abc[def]ghi", new[] { "abcdghi", "abceghi", "abcfghi" }, false)]
        [InlineData("abc[def]ghi|[jkl]m", new[] { "abcdghi", "abceghi", "abcfghi", "jm", "km", "lm" }, false)]
        [InlineData("agggtaaa|tttaccct", new[] { "agggtaaa", "tttaccct" }, false)]
        [InlineData("[cgt]gggtaaa|tttaccc[acg]", new[] { "cgggtaaa", "ggggtaaa", "tgggtaaa", "tttaccca", "tttacccc", "tttacccg" }, false)]
        [InlineData("a[act]ggtaaa|tttacc[agt]t", new[] { "aaggtaaa", "acggtaaa", "atggtaaa", "tttaccat", "tttaccgt", "tttacctt" }, false)]
        [InlineData("ag[act]gtaaa|tttac[agt]ct", new[] { "agagtaaa", "agcgtaaa", "agtgtaaa", "tttacact", "tttacgct", "tttactct" }, false)]
        [InlineData("agg[act]taaa|ttta[agt]cct", new[] { "aggataaa", "aggctaaa", "aggttaaa", "tttaacct", "tttagcct", "tttatcct" }, false)]
        [InlineData("\b(abc|def)\b", new[] { "abc", "def" }, false)]
        [InlineData("^(abc|def)$", new[] { "abc", "def" }, false)]
        [InlineData("abcdefg|h", null, false)]
        [InlineData("abc[def]ghi|[jkl]", null, false)]
        [InlineData("[12][45][789]", new[] { "147", "148", "149", "157", "158", "159", "247", "248", "249", "257", "258", "259" }, false)]
        [InlineData("[12]a[45]b[789]c", new[] { "1a4b7c", "1a4b8c", "1a4b9c", "1a5b7c", "1a5b8c", "1a5b9c", "2a4b7c", "2a4b8c", "2a4b9c", "2a5b7c", "2a5b8c", "2a5b9c" }, false)]
        // case-insensitive
        [InlineData("[Aa][Bb][Cc]", new[] { "abc" }, true)]
        [InlineData("[Aa][Bbc][Cc]", null, true)]
        [InlineData(":[Aa]![Bb]@", new[] { ":a!b@" }, true)]
        [InlineData("(?i)abc", new[] { "abc" }, true)]
        [InlineData("(?i)(abc+|bcd+)", new[] { "abc", "bcd" }, true)]
        [InlineData("(?i)(ab+c|bcd+)", new[] { "ab", "bcd" }, true)]
        [InlineData("(?i)(ab+c|bcd+)*", null, true)]
        [InlineData("(?i)(ab+c|bcd+)+", new[] { "ab", "bcd" }, true)]
        [InlineData("(?i)(ab+c|bcd+){3,5}", new[] { "ab", "bcd" }, true)]
        [InlineData("(?i)abc|def", new[] { "abc", "def" }, true)]
        [InlineData("(?i)ab{4}c|def{5}|g{2,4}h", new[] { "abbbbc", "defffff", "gg" }, true)]
        [InlineData("(?i)(((?>abc)|(?>def)))", new[] { "abc", "def" }, true)]
        [InlineData("(?i)(abc|def|(ghi|jklm))", null, true)]
        [InlineData("(?i)(abc|def|(ghi|jlmn))", new[] { "abc", "def", "ghi", "jlmn" }, true)]
        [InlineData("abc", null, true)]
        [InlineData("abc|def", null, true)]
        [InlineData("abc|def|(ghi|jklm)", null, true)]
        [InlineData("://[Aa][Bb]|[Cc]@!", new[] { "://ab", "c@!" }, true)]
        public void FindPrefixes(string pattern, string[] expectedSet, bool ignoreCase)
        {
            RegexTree tree = RegexParser.Parse(pattern, RegexOptions.None, CultureInfo.InvariantCulture);
            string[] actual = RegexPrefixAnalyzer.FindPrefixes(tree.Root, ignoreCase);

            if (expectedSet is null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.NotNull(actual);

                Array.Sort(actual, StringComparer.Ordinal);
                Array.Sort(expectedSet, StringComparer.Ordinal);

                Assert.Equal(expectedSet, actual);
            }
        }

        private static string FormatSet(string set)
        {
            if (set is null)
            {
                return "(null)";
            }

            var sb = new StringBuilder();
            foreach (char c in set)
            {
                if (c is > (char)32 and < (char)127)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append($"\\u{(uint)c:X4}");
                }
            }
            return sb.ToString();
        }
    }
}
