// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// These tests were ported from https://github.com/nitely/nim-regex/blob/master/tests/tests.nim
    /// in order to increase .NET's test coverage. You can find the relevant repo license in this folder's THIRD-PARTY-NOTICES.TXT file.
    /// </summary>
    public class RegexNimTests
    {
        public static IEnumerable<object[]> NimTestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                (string pattern, RegexOptions options, string input, bool expectedSuccess)[] cases = NimTestData_Cases(engine).ToArray();
                Regex[] regexes = RegexHelpers.GetRegexes(engine, cases.Select(c => (c.pattern, (CultureInfo?)null, (RegexOptions?)c.options, (TimeSpan?)null)).ToArray());
                for (int i = 0; i < regexes.Length; i++)
                {
                    yield return new object[] { regexes[i], cases[i].input, cases[i].expectedSuccess };
                }
            }
        }

        public static IEnumerable<(string Pattern, RegexOptions options, string Input, bool ExpectedSuccess)> NimTestData_Cases(RegexEngine engine)
        {
            yield return ("", RegexOptions.None, "", true);
            yield return ("((a)*b)", RegexOptions.None, "aab", true);
            yield return ("a(b|c)*d", RegexOptions.None, "abbbbccccd", true);
            yield return ("((a)*(b)*)", RegexOptions.None, "abbb", true);
            yield return ("((a(b)*)*(b)*)", RegexOptions.None, "abbb", true);
            yield return ("a(b|c)*d", RegexOptions.None, "ab", false);
            yield return ("\\s\".*\"\\s", RegexOptions.None, " \"word\" ", true);
            yield return ("\\**", RegexOptions.None, "**", true);
            yield return ("\\++", RegexOptions.None, "++", true);
            yield return ("\\?+", RegexOptions.None, "??", true);
            yield return ("\\?*", RegexOptions.None, "??", true);
            yield return ("\\??", RegexOptions.None, "?", true);
            yield return ("\\???", RegexOptions.None, "?", true);
            yield return ("\\**?", RegexOptions.None, "**", true);
            yield return ("\\++?", RegexOptions.None, "++", true);
            yield return ("\\?+?", RegexOptions.None, "??", true);
            yield return ("\\?*?", RegexOptions.None, "??", true);
            yield return ("(a*)*", RegexOptions.None, "aaa", true);
            yield return ("((a*|b*))*", RegexOptions.None, "aaabbbaaa", true);
            yield return ("(a?)*", RegexOptions.None, "aaa", true);
            yield return ("((a)*(a)*)*", RegexOptions.None, "aaaa", true);
            yield return ("(a|b)*", RegexOptions.None, "abab", true);
            yield return ("(a|b)+", RegexOptions.None, "abab", true);
            yield return ("(a|b|c)*", RegexOptions.None, "abcabc", true);
            yield return ("(a|b|c)+", RegexOptions.None, "abcabc", true);
            yield return ("(a|b)*c", RegexOptions.None, "ababc", true);
            yield return ("a(a|b)*c", RegexOptions.None, "aababc", true);
            yield return ("a(a|b)+c", RegexOptions.None, "aababc", true);
            yield return ("a|b*", RegexOptions.None, "a", true);
            yield return ("a|b*", RegexOptions.None, "b", true);
            yield return ("a|b*", RegexOptions.None, "bb", true);
            yield return ("a*a*", RegexOptions.None, "aaa", true);
            yield return ("a*b*", RegexOptions.None, "aabb", true);
            yield return ("(a*)*b", RegexOptions.None, "aaab", true);
            yield return ("a*b*c*", RegexOptions.None, "aabbcc", true);
            yield return ("a*b*", RegexOptions.None, "ab", true);
            yield return ("a*b*", RegexOptions.None, "a", true);
            yield return ("a*b*", RegexOptions.None, "b", true);
            yield return ("a*b*", RegexOptions.None, "", true);
            yield return ("a+", RegexOptions.None, "a", true);
            yield return ("ab+", RegexOptions.None, "abb", true);
            yield return ("aba+", RegexOptions.None, "abaa", true);
            yield return ("a+a+", RegexOptions.None, "aa", true);
            yield return ("a+a+", RegexOptions.None, "aaa", true);
            yield return ("a+b+", RegexOptions.None, "ab", true);
            yield return ("a+b+", RegexOptions.None, "aabb", true);
            yield return ("(a+|b)+", RegexOptions.None, "aabb", true);
            yield return ("(a+|b+)*", RegexOptions.None, "aabb", true);
            yield return ("ab?", RegexOptions.None, "a", true);
            yield return ("ab?", RegexOptions.None, "ab", true);
            yield return ("ab?a", RegexOptions.None, "aba", true);
            yield return ("ab?a", RegexOptions.None, "aa", true);
            yield return ("a?b?", RegexOptions.None, "ab", true);
            yield return ("a?b?", RegexOptions.None, "a", true);
            yield return ("a?b?", RegexOptions.None, "b", true);
            yield return ("a?b?", RegexOptions.None, "", true);
            yield return ("a??b??", RegexOptions.None, "ab", true);
            yield return ("a??b??", RegexOptions.None, "a", true);
            yield return ("a??b??", RegexOptions.None, "b", true);
            yield return ("a??b??", RegexOptions.None, "", true);
            yield return ("\\(a\\)", RegexOptions.None, "(a)", true);
            yield return ("a\\*b", RegexOptions.None, "a*b", true);
            yield return ("a\\*b*", RegexOptions.None, "a*bbb", true);
            yield return ("\\\\", RegexOptions.None, "\\", true);
            yield return ("\\\\\\\\", RegexOptions.None, "\\\\", true);
            yield return ("\\w", RegexOptions.None, "a", true);
            yield return ("\\w*", RegexOptions.None, "abc123", true);
            yield return ("\\w+", RegexOptions.None, "abc123", true);
            yield return ("\\w+", RegexOptions.None, "abc_123", true);
            yield return ("\\d", RegexOptions.None, "1", true);
            yield return ("\\d*", RegexOptions.None, "123", true);
            yield return ("\\d+", RegexOptions.None, "123", true);
            yield return ("\\d+", RegexOptions.None, "123abc", true);
            yield return ("\\d", RegexOptions.None, "۲", true);
            yield return ("\\s", RegexOptions.None, " ", true);
            yield return ("\\s*", RegexOptions.None, "   ", true);
            yield return ("\\s*", RegexOptions.None, " \t\r", true);
            yield return ("\\s+", RegexOptions.None, "   ", true);
            yield return ("\\s+", RegexOptions.None, " \t\n", true);
            yield return ("\\s", RegexOptions.None, "\u0020", true);
            yield return ("\\s", RegexOptions.None, "\u2028", true);
            yield return ("\\W", RegexOptions.None, "!", true);
            yield return ("\\W+", RegexOptions.None, "!@#", true);
            yield return ("\\D", RegexOptions.None, "a", true);
            yield return ("\\D", RegexOptions.None, "⅕", true);
            yield return ("\\D+", RegexOptions.None, "abc", true);
            yield return ("\\D+", RegexOptions.None, "!@#", true);
            yield return ("\\S", RegexOptions.None, "a", true);
            yield return ("\\S+", RegexOptions.None, "abc", true);
            yield return ("[abc]", RegexOptions.None, "a", true);
            yield return ("[abc]", RegexOptions.None, "b", true);
            yield return ("[abc]", RegexOptions.None, "c", true);
            yield return ("[abc]", RegexOptions.None, "d", false);
            yield return ("[a-z]", RegexOptions.None, "a", true);
            yield return ("[a-z]", RegexOptions.None, "z", true);
            yield return ("[a-z]", RegexOptions.None, "A", false);
            yield return ("[a-z]+", RegexOptions.None, "abc", true);
            yield return ("[0-9]+", RegexOptions.None, "123", true);
            yield return ("[^abc]", RegexOptions.None, "d", true);
            yield return ("[^abc]", RegexOptions.None, "a", false);
            yield return ("[^a-z]", RegexOptions.None, "1", true);
            yield return ("a{3}", RegexOptions.None, "aaa", true);
            yield return ("a{3}", RegexOptions.None, "aa", false);
            yield return ("a{3}", RegexOptions.None, "aaaa", true);
            yield return ("a{2,4}", RegexOptions.None, "aa", true);
            yield return ("a{2,4}", RegexOptions.None, "aaa", true);
            yield return ("a{2,4}", RegexOptions.None, "aaaa", true);
            yield return ("a{2,4}", RegexOptions.None, "a", false);
            yield return ("a{2,4}", RegexOptions.None, "aaaaa", true);
            yield return ("a{2,}", RegexOptions.None, "aa", true);
            yield return ("a{2,}", RegexOptions.None, "aaa", true);
            yield return ("a{2,}", RegexOptions.None, "aaaa", true);
            yield return ("a{2,}", RegexOptions.None, "a", false);
            yield return ("(?:ab)+", RegexOptions.None, "ab", true);
            yield return ("(?:ab)+", RegexOptions.None, "abab", true);
            yield return ("(?:ab)+", RegexOptions.None, "ababab", true);
            yield return ("(?:ab)+", RegexOptions.None, "a", false);
            yield return ("a*?", RegexOptions.None, "aaa", true);
            yield return ("a??", RegexOptions.None, "aaa", true);
            yield return ("a{2,4}?", RegexOptions.None, "aaa", true);
            yield return ("(a*)*?b", RegexOptions.None, "aaab", true);
            yield return ("(a*?)*b", RegexOptions.None, "aaab", true);
            yield return ("abc$", RegexOptions.None, "abcz", false);
            yield return ("^abc$", RegexOptions.None, "abcz", false);
            yield return ("^abc$", RegexOptions.None, "zabc", false);
            yield return ("\\b", RegexOptions.None, "a", true);
            yield return ("\\b", RegexOptions.None, " a", true);
            yield return ("\\b", RegexOptions.None, "a ", true);
            yield return ("\\B", RegexOptions.None, "ab", true);
            yield return (".+", RegexOptions.None, "abc", true);
            yield return ("(?<foo>a)", RegexOptions.None, "a", true);
            yield return ("(?<foo>a)(?<bar>b)", RegexOptions.None, "ab", true);

            // Lookahead and lookbehind are not supported by NonBacktracking engine
            if (engine != RegexEngine.NonBacktracking)
            {
                yield return ("a(?=b)\\w", RegexOptions.None, "ab", true);
                yield return ("a(?=c)\\w", RegexOptions.None, "ab", false);
                yield return ("\\w(?<=a)b", RegexOptions.None, "ab", true);
                yield return ("\\w(?<=c)b", RegexOptions.None, "ab", false);
                yield return ("a(?!c)\\w", RegexOptions.None, "ab", true);
                yield return ("a(?!b)\\w", RegexOptions.None, "ab", false);
                yield return ("\\w(?<!c)b", RegexOptions.None, "ab", true);
                yield return ("\\w(?<!a)b", RegexOptions.None, "ab", false);
            }

            yield return ("[\\b]", RegexOptions.None, "\b", true);
            yield return ("\\b\\b\\baa\\b\\b\\b", RegexOptions.None, "aa", true);
            yield return ("(?i)abc", RegexOptions.IgnoreCase, "ABC", true);
            yield return ("(?i)abc", RegexOptions.IgnoreCase, "abc", true);
            yield return ("(?i)abc", RegexOptions.IgnoreCase, "AbC", true);
            yield return ("(?m)^abc$", RegexOptions.Multiline, "abc\nabc", true);
            yield return ("(?s).", RegexOptions.Singleline, "\n", true);
            yield return ("(a*)*", RegexOptions.None, "", true);
            yield return ("(a*)+", RegexOptions.None, "", true);
            yield return ("(a+)*", RegexOptions.None, "", true);
            yield return ("(a?)*", RegexOptions.None, "", true);
            yield return ("(a{0,1})*", RegexOptions.None, "", true);
            yield return ("(a{0,2})*", RegexOptions.None, "", true);
            yield return ("a|b", RegexOptions.None, "ab", true);
            yield return ("a|b|c", RegexOptions.None, "abc", true);
        }

        [Theory]
        [MemberData(nameof(NimTestData))]
        public void NimTests(Regex regex, string input, bool expectedSuccess)
        {
            Assert.Equal(expectedSuccess, regex.IsMatch(input));
        }
    }
}
