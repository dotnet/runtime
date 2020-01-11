// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCharacterSetTests
    {
        [Theory]
        [InlineData(@"a", RegexOptions.IgnoreCase, new[] { 'a', 'A' })]
        [InlineData(@"\u00A9", RegexOptions.None, new[] { '\u00A9' })]
        [InlineData(@"\u00A9", RegexOptions.IgnoreCase, new[] { '\u00A9' })]
        [InlineData(@"az", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z' })]
        [InlineData(@"azY", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y' })]
        [InlineData(@"azY", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y' })]
        [InlineData(@"azY\u00A9", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9' })]
        [InlineData(@"azY\u00A9", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9' })]
        [InlineData(@"azY\u00A9\u05D0", RegexOptions.IgnoreCase, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9', '\u05D0' })]
        [InlineData(@"azY\u00A9\u05D0", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, new[] { 'a', 'A', 'z', 'Z', 'y', 'Y', '\u00A9', '\u05D0' })]
        [InlineData(@"a ", RegexOptions.None, new[] { 'a', ' ' })]
        [InlineData(@"a \t\r", RegexOptions.None, new[] { 'a', ' ', '\t', '\r' })]
        [InlineData(@"aeiou", RegexOptions.None, new[] { 'a', 'e', 'i', 'o', 'u' })]
        [InlineData(@"a-a", RegexOptions.None, new[] { 'a' })]
        [InlineData(@"ab", RegexOptions.None, new[] { 'a', 'b' })]
        [InlineData(@"a-b", RegexOptions.None, new[] { 'a', 'b' })]
        [InlineData(@"abc", RegexOptions.None, new[] { 'a', 'b', 'c' })]
        [InlineData(@"1369", RegexOptions.None, new[] { '1', '3', '6', '9' })]
        [InlineData(@"ACEGIKMOQSUWY", RegexOptions.None, new[] { 'A', 'C', 'E', 'G', 'I', 'K', 'M', 'O', 'Q', 'S', 'U', 'W', 'Y' })]
        [InlineData(@"abcAB", RegexOptions.None, new[] { 'A', 'B', 'a', 'b', 'c' })]
        [InlineData(@"a-c", RegexOptions.None, new[] { 'a', 'b', 'c' })]
        [InlineData(@"X-b", RegexOptions.None, new[] { 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b' })]
        [InlineData(@"\u0083\u00DE-\u00E1", RegexOptions.None, new[] { '\u0083', '\u00DE', '\u00DF', '\u00E0', '\u00E1' })]
        [InlineData(@"\u007A-\u0083\u00DE-\u00E1", RegexOptions.None, new[] { '\u007A', '\u007B', '\u007C', '\u007D', '\u007E', '\u007F', '\u0080', '\u0081', '\u0082', '\u0083', '\u00DE', '\u00DF', '\u00E0', '\u00E1' })]
        [InlineData(@"\u05D0", RegexOptions.None, new[] { '\u05D0' })]
        [InlineData(@"a\u05D0", RegexOptions.None, new[] { 'a', '\u05D0' })]
        [InlineData(@"\uFFFC-\uFFFF", RegexOptions.None, new[] { '\uFFFC', '\uFFFD', '\uFFFE', '\uFFFF' })]
        [InlineData(@"a-z-[d-w-[m-o]]", RegexOptions.None, new[] { 'a', 'b', 'c', 'm', 'n', 'n', 'o', 'x', 'y', 'z' })]
        [InlineData(@"\p{IsBasicLatin}-[\x00-\x7F]", RegexOptions.None, new char[0])]
        [InlineData(@"0-9-[2468]", RegexOptions.None, new[] { '0', '1', '3', '5', '7', '9' })]
        public void SetInclusionsExpected(string set, RegexOptions options, char[] expectedIncluded)
        {
            ValidateSet($"[{set}]", options, new HashSet<char>(expectedIncluded), null);
            if (!set.Contains("["))
            {
                ValidateSet($"[^{set}]", options, null, new HashSet<char>(expectedIncluded));
            }
        }

        [Theory]
        [InlineData('\0')]
        [InlineData('\uFFFF')]
        [InlineData('a')]
        [InlineData('5')]
        public void SingleExpected(char c)
        {
            string s = @"\u" + ((int)c).ToString("X4");
            var set = new HashSet<char>() { c };

            // One
            ValidateSet($"{s}", RegexOptions.None, set, null);
            ValidateSet($"[{s}]", RegexOptions.None, set, null);
            ValidateSet($"[^{s}]", RegexOptions.None, null, set);

            // Positive lookahead
            ValidateSet($"(?={s}){s}", RegexOptions.None, set, null);
            ValidateSet($"(?=[^{s}])[^{s}]", RegexOptions.None, null, set);

            // Negative lookahead
            ValidateSet($"(?![^{s}]){s}", RegexOptions.None, set, null);
            ValidateSet($"(?![{s}])[^{s}]", RegexOptions.None, null, set);

            // Concatenation
            ValidateSet($"[{s}{s}]", RegexOptions.None, set, null);
            ValidateSet($"[^{s}{s}{s}]", RegexOptions.None, null, set);

            // Alternation
            ValidateSet($"{s}|{s}", RegexOptions.None, set, null);
            ValidateSet($"[^{s}]|[^{s}]|[^{s}]", RegexOptions.None, null, set);
            ValidateSet($"{s}|[^{s}]", RegexOptions.None, null, new HashSet<char>());
        }

        [Fact]
        public void AllEmptySets()
        {
            var set = new HashSet<char>();

            ValidateSet(@"[\u0000-\uFFFF]", RegexOptions.None, null, set);
            ValidateSet(@"[\u0000-\uFFFFa-z]", RegexOptions.None, null, set);
            ValidateSet(@"[\u0000-\u1000\u1001-\u2002\u2003-\uFFFF]", RegexOptions.None, null, set);
            ValidateSet(@"[\u0000-\uFFFE\u0001-\uFFFF]", RegexOptions.None, null, set);

            ValidateSet(@"[^\u0000-\uFFFF]", RegexOptions.None, set, null);
            ValidateSet(@"[^\u0000-\uFFFFa-z]", RegexOptions.None, set, null);
            ValidateSet(@"[^\u0000-\uFFFE\u0001-\uFFFF]", RegexOptions.None, set, null);
            ValidateSet(@"[^\u0000-\u1000\u1001-\u2002\u2003-\uFFFF]", RegexOptions.None, set, null);
        }

        [Fact]
        public void AllButOneSets()
        {
            ValidateSet(@"[\u0000-\uFFFE]", RegexOptions.None, null, new HashSet<char>() { '\uFFFF' });
            ValidateSet(@"[\u0001-\uFFFF]", RegexOptions.None, null, new HashSet<char>() { '\u0000' });
            ValidateSet(@"[\u0000-ac-\uFFFF]", RegexOptions.None, null, new HashSet<char>() { 'b' });
        }

        [Fact]
        public void DotInclusionsExpected()
        {
            ValidateSet(".", RegexOptions.None, null, new HashSet<char>() { '\n' });
            ValidateSet(".", RegexOptions.IgnoreCase, null, new HashSet<char>() { '\n' });
            ValidateSet(".", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, null, new HashSet<char>() { '\n' });

            ValidateSet(".", RegexOptions.Singleline, null, new HashSet<char>());
            ValidateSet(".", RegexOptions.Singleline | RegexOptions.IgnoreCase, null, new HashSet<char>());
            ValidateSet(".", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, null, new HashSet<char>());
        }

        [Fact]
        public void WhitespaceInclusionsExpected()
        {
            var whitespaceInclusions = ComputeIncludedSet(char.IsWhiteSpace);
            ValidateSet(@"[\s]", RegexOptions.None, whitespaceInclusions, null);
            ValidateSet(@"[^\s]", RegexOptions.None, null, whitespaceInclusions);
            ValidateSet(@"[\S]", RegexOptions.None, null, whitespaceInclusions);
        }

        [Fact]
        public void DigitInclusionsExpected()
        {
            var digitInclusions = ComputeIncludedSet(char.IsDigit);
            ValidateSet(@"[\d]", RegexOptions.None, digitInclusions, null);
            ValidateSet(@"[^\d]", RegexOptions.None, null, digitInclusions);
            ValidateSet(@"[\D]", RegexOptions.None, null, digitInclusions);
        }

        [Theory]
        [InlineData(@"\p{Lu}", new[] { UnicodeCategory.UppercaseLetter })]
        [InlineData(@"\p{S}", new[] { UnicodeCategory.CurrencySymbol, UnicodeCategory.MathSymbol, UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherSymbol })]
        [InlineData(@"\p{Lu}\p{Zl}", new[] { UnicodeCategory.UppercaseLetter, UnicodeCategory.LineSeparator })]
        [InlineData(@"\w", new[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.OtherLetter, UnicodeCategory.ModifierLetter, UnicodeCategory.NonSpacingMark, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.ConnectorPunctuation })]

        public void UnicodeCategoryInclusionsExpected(string set, UnicodeCategory[] categories)
        {
            var categoryInclusions = ComputeIncludedSet(c => Array.IndexOf(categories, char.GetUnicodeCategory(c)) >= 0);
            ValidateSet($"[{set}]", RegexOptions.None, categoryInclusions, null);
            ValidateSet($"[^{set}]", RegexOptions.None, null, categoryInclusions);
        }

        [Theory]
        [InlineData(@"\p{IsGreek}", new[] { 0x0370, 0x03FF })]
        [InlineData(@"\p{IsRunic}\p{IsHebrew}", new[] { 0x0590, 0x05FF, 0x16A0, 0x16FF })]
        [InlineData(@"abx-z\p{IsRunic}\p{IsHebrew}", new[] { 0x0590, 0x05FF, 0x16A0, 0x16FF, 'a', 'a', 'b', 'b', 'x', 'x', 'y', 'z' })]
        public void NamedBlocksInclusionsExpected(string set, int[] ranges)
        {
            var included = new HashSet<char>();
            for (int i = 0; i < ranges.Length - 1; i += 2)
            {
                ComputeIncludedSet(c => c >= ranges[i] && c <= ranges[i + 1], included);
            }

            ValidateSet($"[{set}]", RegexOptions.None, included, null);
            ValidateSet($"[^{set}]", RegexOptions.None, null, included);
        }

        private static HashSet<char> ComputeIncludedSet(Func<char, bool> func)
        {
            var included = new HashSet<char>();
            ComputeIncludedSet(func, included);
            return included;
        }

        private static void ComputeIncludedSet(Func<char, bool> func, HashSet<char> included)
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (func((char)i))
                {
                    included.Add((char)i);
                }
            }
        }

        [Fact]
        public void ValidateValidateSet()
        {
            Assert.Throws<XunitException>(() => ValidateSet("[a]", RegexOptions.None, new HashSet<char>() { 'b' }, null));
            Assert.Throws<XunitException>(() => ValidateSet("[b]", RegexOptions.None, null, new HashSet<char>() { 'b' }));
        }

        private static void ValidateSet(string regex, RegexOptions options, HashSet<char> included, HashSet<char> excluded)
        {
            Assert.True((included != null) ^ (excluded != null));
            foreach (RegexOptions compiled in new[] { RegexOptions.None, RegexOptions.Compiled })
            {
                var r = new Regex(regex, options | compiled);
                for (int i = 0; i <= char.MaxValue; i++)
                {
                    bool actual = r.IsMatch(((char)i).ToString());
                    bool expected = included != null ? included.Contains((char)i) : !excluded.Contains((char)i);
                    if (actual != expected)
                    {
                        throw new XunitException($"Set=\"{regex}\", Options=\"{options}\", {i.ToString("X4")} => '{(char)i}' returned {actual}");
                    }
                }
            }
        }
    }
}
