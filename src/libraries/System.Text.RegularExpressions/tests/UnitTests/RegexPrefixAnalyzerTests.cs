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
