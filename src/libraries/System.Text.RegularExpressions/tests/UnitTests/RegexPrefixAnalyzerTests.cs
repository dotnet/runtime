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
        [InlineData(@"a", RegexOptions.None, "\0\u0002\0ab")]
        [InlineData(@"(a)", RegexOptions.None, "\0\u0002\0ab")]
        [InlineData(@"(a)+", RegexOptions.None, "\0\u0002\0ab")]
        [InlineData(@"abc", RegexOptions.None, "\0\u0002\0ab")]
        [InlineData(@"abc", RegexOptions.RightToLeft, "\0\u0002\0cd")]
        [InlineData(@"abc|def", RegexOptions.RightToLeft, "\0\u0004\0cdfg")]
        [InlineData(@"a?b", RegexOptions.None, "\0\u0002\0ac")]
        [InlineData(@"a?[bcd]", RegexOptions.None, "\0\u0002\0ae")]
        [InlineData(@"a?[bcd]*[xyz]", RegexOptions.None, "\0\u0004\0aex{")]
        [InlineData(@"[a-c]", RegexOptions.None, "\0\u0002\0ad")]
        [InlineData(@"a+b+c+", RegexOptions.None, "\0\u0002\0ab")]
        [InlineData(@"a*b+c+", RegexOptions.None, "\0\u0002\0ac")]
        [InlineData(@"a*b*c+", RegexOptions.None, "\0\u0002\0ad")]
        [InlineData(@".", RegexOptions.None, "\u0001\u0002\u0000\u000A\u000B")]
        [InlineData(@".|\n", RegexOptions.None, "\0\u0001\0\0")]
        [InlineData(@"[^\n]?[\n]", RegexOptions.None, "\0\u0001\0\0")]
        [InlineData(@"[^a]?[a]", RegexOptions.None, "\0\u0001\0\0")]
        [InlineData(@"(abc)?(?(\1)yes|no)", RegexOptions.None, "\0\u0006\0abnoyz")]
        [InlineData(@"(abc)?(?(xyz)yes|no)", RegexOptions.None, "\0\u0006\0abnoyz")]
        [InlineData(@"[^a-zA-Z0-9_.]", RegexOptions.None, "\u0001\u000A\u0000./0:A[_`a{")]
        // Can't produce starting sets
        [InlineData(@"", RegexOptions.None, null)]
        [InlineData(@"a*", RegexOptions.None, null)]
        [InlineData(@"a*b*", RegexOptions.None, null)]
        [InlineData(@"a*b*c*", RegexOptions.None, null)]
        [InlineData(@"a*|b*", RegexOptions.None, null)]
        [InlineData(@"(a)*", RegexOptions.None, null)]
        [InlineData(@"(?:a)*", RegexOptions.None, null)]
        [InlineData(@"(a*)+", RegexOptions.None, null)]
        [InlineData(@"(a*)+?", RegexOptions.None, null)]
        [InlineData(@"[^ab]|a", RegexOptions.None, null)]
        [InlineData(@"[^ab]|ab", RegexOptions.None, null)]
        [InlineData(@"[^ab]?[a]", RegexOptions.None, null)]
        [InlineData(@"[^ab]?(a)+", RegexOptions.None, null)]
        [InlineData(@"(abc)?\1", RegexOptions.None, null)]
        [InlineData(@"[abc-[bc]]|[def]", RegexOptions.None, null)]
        [InlineData(@"[def]|[abc-[bc]]", RegexOptions.None, null)]
        [InlineData(@"(abc)?(?(\1)d*|f)", RegexOptions.None, null)]
        [InlineData(@"(abc)?(?(\1)d|f*)", RegexOptions.None, null)]
        [InlineData(@"(abc)?(?(xyz)d*|f)", RegexOptions.None, null)]
        [InlineData(@"(abc)?(?(xyz)d|f*)", RegexOptions.None, null)]
        public void FindFirstCharClass(string pattern, RegexOptions options, string? expectedSet)
        {
            RegexTree tree = RegexParser.Parse(pattern, options, CultureInfo.InvariantCulture);
            string actualSet = RegexPrefixAnalyzer.FindFirstCharClass(tree.Root);
            if (expectedSet != actualSet)
            {
                throw new TrueException($"Expected {FormatSet(expectedSet)}, got {FormatSet(actualSet)}", true);
            }
        }

        [Fact]
        public void FindFirstCharClass_StressDeep()
        {
            int nesting = 8000;
            FindFirstCharClass(string.Concat(Enumerable.Repeat($"(a?", nesting).Concat(Enumerable.Repeat(")*", nesting))), RegexOptions.None, null);
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
                if (c > 32 && c < 127)
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
