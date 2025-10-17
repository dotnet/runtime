// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// These tests have been ported from the re2 test suite located at https://github.com/google/re2/tree/61c4644171ee6b480540bf9e569cba06d9090b4b/re2/testing
    /// in order to increase .NET's test coverage. You can find the relevant repo license in this folder's THIRD-PARTY-NOTICES.TXT file.
    /// </summary>
    public class RegexRe2Tests
    {
        public static IEnumerable<object[]> Re2TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                (string pattern, RegexOptions options, string input, bool expectedSuccess)[] cases = Re2TestData_Cases(engine).ToArray();
                Regex[] regexes = RegexHelpers.GetRegexes(engine, cases.Select(c => (c.pattern, (CultureInfo?)null, (RegexOptions?)c.options, (TimeSpan?)null)).ToArray());
                for (int i = 0; i < regexes.Length; i++)
                {
                    yield return new object[] { regexes[i], cases[i].input, cases[i].expectedSuccess };
                }
            }
        }

        public static IEnumerable<(string Pattern, RegexOptions options, string Input, bool ExpectedSuccess)> Re2TestData_Cases(RegexEngine engine)
        {
            // Basic matching tests from search_test.cc
            yield return ("a", RegexOptions.None, "a", true);
            yield return ("a", RegexOptions.None, "zyzzyva", true);
            yield return ("a+", RegexOptions.None, "aa", true);
            yield return ("(a+|b)+", RegexOptions.None, "ab", true);
            yield return ("ab|cd", RegexOptions.None, "xabcdx", true);
            yield return ("h.*od?", RegexOptions.None, "hello\ngoodbye\n", true);
            yield return ("h.*o", RegexOptions.None, "hello\ngoodbye\n", true);
            yield return ("h.*o", RegexOptions.None, "goodbye\nhello\n", true);
            yield return ("h.*o", RegexOptions.None, "hello world", true);
            yield return ("h.*o", RegexOptions.None, "othello, world", true);
            yield return ("[^\\s\\S]", RegexOptions.None, "aaaaaaa", false);
            yield return ("a", RegexOptions.None, "aaaaaaa", true);
            yield return ("a*", RegexOptions.None, "aaaaaaa", true);
            yield return ("a*", RegexOptions.None, "", true);
            yield return ("ab|cd", RegexOptions.None, "xabcdx", true);
            yield return ("a", RegexOptions.None, "cab", true);
            yield return ("a*b", RegexOptions.None, "cab", true);
            yield return ("((((((((((((((((((((x))))))))))))))))))))", RegexOptions.None, "x", true);
            yield return ("[abcd]", RegexOptions.None, "xxxabcdxxx", true);
            yield return ("[^x]", RegexOptions.None, "xxxabcdxxx", true);
            yield return ("[abcd]+", RegexOptions.None, "xxxabcdxxx", true);
            yield return ("[^x]+", RegexOptions.None, "xxxabcdxxx", true);
            yield return ("(fo|foo)", RegexOptions.None, "fo", true);
            yield return ("(foo|fo)", RegexOptions.None, "foo", true);

            // Case sensitivity tests
            yield return ("aa", RegexOptions.None, "aA", false);
            yield return ("a", RegexOptions.None, "Aa", false);
            yield return ("a", RegexOptions.None, "A", false);
            yield return ("ABC", RegexOptions.None, "abc", false);
            yield return ("abc", RegexOptions.None, "XABCY", false);
            yield return ("ABC", RegexOptions.None, "xabcy", false);

            // Anchor tests - ^ and $
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "foo", true);
            yield return ("^(foo|bar|[A-Z])", RegexOptions.None, "foo", true);
            yield return ("(foo|bar|[A-Z])$", RegexOptions.None, "foo\n", true);
            yield return ("(foo|bar|[A-Z])$", RegexOptions.None, "foo", true);
            yield return ("^(foo|bar|[A-Z])$", RegexOptions.None, "foo\n", false);
            yield return ("^(foo|bar|[A-Z])$", RegexOptions.None, "foo", true);
            yield return ("^(foo|bar|[A-Z])$", RegexOptions.None, "bar", true);
            yield return ("^(foo|bar|[A-Z])$", RegexOptions.None, "X", true);
            yield return ("^(foo|bar|[A-Z])$", RegexOptions.None, "XY", false);
            yield return ("^(fo|foo)$", RegexOptions.None, "fo", true);
            yield return ("^(fo|foo)$", RegexOptions.None, "foo", true);
            yield return ("^^(fo|foo)$", RegexOptions.None, "fo", true);
            yield return ("^^(fo|foo)$", RegexOptions.None, "foo", true);
            yield return ("^$", RegexOptions.None, "", true);
            yield return ("^$", RegexOptions.None, "x", false);
            yield return ("^^$", RegexOptions.None, "", true);
            yield return ("^$$", RegexOptions.None, "", true);
            yield return ("^^$", RegexOptions.None, "x", false);
            yield return ("^$$", RegexOptions.None, "x", false);
            yield return ("^^$$", RegexOptions.None, "", true);
            yield return ("^^$$", RegexOptions.None, "x", false);
            yield return ("^^^^^^^^$$$$$$$$", RegexOptions.None, "", true);
            yield return ("^", RegexOptions.None, "x", true);
            yield return ("$", RegexOptions.None, "x", true);

            // Word boundaries
            yield return ("\\bfoo\\b", RegexOptions.None, "nofoo foo that", true);
            yield return ("a\\b", RegexOptions.None, "faoa x", true);
            yield return ("\\bbar", RegexOptions.None, "bar x", true);
            yield return ("\\bbar", RegexOptions.None, "foo\nbar x", true);
            yield return ("bar\\b", RegexOptions.None, "foobar", true);
            yield return ("bar\\b", RegexOptions.None, "foobar\nxxx", true);
            yield return ("(foo|bar|[A-Z])\\b", RegexOptions.None, "foo", true);
            yield return ("(foo|bar|[A-Z])\\b", RegexOptions.None, "foo\n", true);
            yield return ("\\b", RegexOptions.None, "", false);
            yield return ("\\b", RegexOptions.None, "x", true);
            yield return ("\\b(foo|bar|[A-Z])", RegexOptions.None, "foo", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "X", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "XY", false);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "bar", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "foo", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "foo\n", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "ffoo bbar N x", true);
            yield return ("\\b(fo|foo)\\b", RegexOptions.None, "fo", true);
            yield return ("\\b(fo|foo)\\b", RegexOptions.None, "foo", true);
            yield return ("\\b\\b", RegexOptions.None, "", false);
            yield return ("\\b\\b", RegexOptions.None, "x", true);
            yield return ("\\b$", RegexOptions.None, "", false);
            yield return ("\\b$", RegexOptions.None, "x", true);
            yield return ("\\b$", RegexOptions.None, "y x", true);
            yield return ("\\b.$", RegexOptions.None, "x", true);
            yield return ("^\\b(fo|foo)\\b", RegexOptions.None, "fo", true);
            yield return ("^\\b(fo|foo)\\b", RegexOptions.None, "foo", true);
            yield return ("^\\b", RegexOptions.None, "", false);
            yield return ("^\\b", RegexOptions.None, "x", true);
            yield return ("^\\b\\b", RegexOptions.None, "", false);
            yield return ("^\\b\\b", RegexOptions.None, "x", true);
            yield return ("^\\b$", RegexOptions.None, "", false);
            yield return ("^\\b$", RegexOptions.None, "x", false);
            yield return ("^\\b.$", RegexOptions.None, "x", true);
            yield return ("^\\b.\\b$", RegexOptions.None, "x", true);
            yield return ("^^^^^^^^\\b$$$$$$$", RegexOptions.None, "", false);
            yield return ("^^^^^^^^\\b.$$$$$$", RegexOptions.None, "x", true);
            yield return ("^^^^^^^^\\b$$$$$$$", RegexOptions.None, "x", false);

            // Non-word boundaries
            yield return ("\\Bfoo\\B", RegexOptions.None, "n foo xfoox that", true);
            yield return ("a\\B", RegexOptions.None, "faoa x", true);
            yield return ("\\Bbar", RegexOptions.None, "bar x", false);
            yield return ("\\Bbar", RegexOptions.None, "foo\nbar x", false);
            yield return ("bar\\B", RegexOptions.None, "foobar", false);
            yield return ("bar\\B", RegexOptions.None, "foobar\nxxx", false);
            yield return ("(foo|bar|[A-Z])\\B", RegexOptions.None, "foox", true);
            yield return ("(foo|bar|[A-Z])\\B", RegexOptions.None, "foo\n", false);
            yield return ("\\B", RegexOptions.None, "", false);
            yield return ("\\B", RegexOptions.None, "x", false);
            yield return ("\\B(foo|bar|[A-Z])", RegexOptions.None, "foo", false);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "xXy", true);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "XY", false);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "XYZ", true);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "abara", true);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "xfoo_", true);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "xfoo\n", false);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "foo bar vNx", false);
            yield return ("\\B(fo|foo)\\B", RegexOptions.None, "xfoo", true);
            yield return ("\\B(foo|fo)\\B", RegexOptions.None, "xfooo", true);
            yield return ("\\B\\B", RegexOptions.None, "", false);
            yield return ("\\B\\B", RegexOptions.None, "x", false);
            yield return ("\\B$", RegexOptions.None, "", false);
            yield return ("\\B$", RegexOptions.None, "x", false);
            yield return ("\\B$", RegexOptions.None, "y x", false);
            yield return ("\\B.$", RegexOptions.None, "x", false);
            yield return ("^\\B(fo|foo)\\B", RegexOptions.None, "fo", false);
            yield return ("^\\B(fo|foo)\\B", RegexOptions.None, "foo", false);
            yield return ("^\\B", RegexOptions.None, "", false);
            yield return ("^\\B", RegexOptions.None, "x", false);
            yield return ("^\\B\\B", RegexOptions.None, "", false);
            yield return ("^\\B\\B", RegexOptions.None, "x", false);
            yield return ("^\\B$", RegexOptions.None, "", false);
            yield return ("^\\B$", RegexOptions.None, "x", false);
            yield return ("^\\B.$", RegexOptions.None, "x", false);
            yield return ("^\\B.\\B$", RegexOptions.None, "x", false);
            yield return ("^^^^^^^^\\B$$$$$$$", RegexOptions.None, "", false);
            yield return ("^^^^^^^^\\B.$$$$$$", RegexOptions.None, "x", false);
            yield return ("^^^^^^^^\\B$$$$$$$", RegexOptions.None, "x", false);

            // PCRE uses only ASCII for \b computation
            yield return ("\\bx\\b", RegexOptions.None, "x", true);
            yield return ("\\bx\\b", RegexOptions.None, "x>", true);
            yield return ("\\bx\\b", RegexOptions.None, "<x", true);
            yield return ("\\bx\\b", RegexOptions.None, "<x>", true);
            yield return ("\\bx\\b", RegexOptions.None, "ax", false);
            yield return ("\\bx\\b", RegexOptions.None, "xb", false);
            yield return ("\\bx\\b", RegexOptions.None, "axb", false);
            yield return ("\\bx\\b", RegexOptions.None, "«x", true);
            yield return ("\\bx\\b", RegexOptions.None, "x»", true);
            yield return ("\\bx\\b", RegexOptions.None, "«x»", true);
            yield return ("\\bx\\b", RegexOptions.None, "axb", false);
            yield return ("\\bx\\b", RegexOptions.None, "áxβ", true);
            yield return ("\\Bx\\B", RegexOptions.None, "axb", true);
            yield return ("\\Bx\\B", RegexOptions.None, "áxβ", false);

            // Weird boundary cases
            yield return ("^$^$", RegexOptions.None, "", true);
            yield return ("^$^", RegexOptions.None, "", true);
            yield return ("$^$", RegexOptions.None, "", true);
            yield return ("^$^$", RegexOptions.None, "x", false);
            yield return ("^$^", RegexOptions.None, "x", false);
            yield return ("$^$", RegexOptions.None, "x", false);
            yield return ("^$^$", RegexOptions.Multiline, "x\ny", false);
            yield return ("^$^", RegexOptions.Multiline, "x\ny", false);
            yield return ("$^$", RegexOptions.Multiline, "x\ny", true);
            yield return ("^$^$", RegexOptions.Multiline, "x\n\ny", false);
            yield return ("^$^", RegexOptions.Multiline, "x\n\ny", false);
            yield return ("$^$", RegexOptions.Multiline, "x\n\ny", true);
            yield return ("^(foo\\$)$", RegexOptions.None, "foo$bar", false);
            yield return ("(foo\\$)", RegexOptions.None, "foo$bar", true);
            yield return ("^...$", RegexOptions.None, "abc", true);

            // UTF-8 tests
            yield return ("^\u672c$", RegexOptions.None, "\u672c", true);
            yield return ("^...$", RegexOptions.None, "\u65e5\u672c\u8a9e", true);
            yield return ("^...$", RegexOptions.None, ".\u672c.", true);

            // Octal escapes
            yield return ("\\141", RegexOptions.None, "a", true);
            yield return ("\\060", RegexOptions.None, "0", true);
            yield return ("\\0600", RegexOptions.None, "00", true);
            yield return ("\\608", RegexOptions.None, "08", true);
            yield return ("\\01", RegexOptions.None, "\u0001", true);
            yield return ("\\018", RegexOptions.None, "\u00018", true);

            // Hexadecimal escapes
            yield return ("\\x61", RegexOptions.None, "a", true);
            yield return ("\\x{61}", RegexOptions.None, "a", true);
            yield return ("\\x{00000061}", RegexOptions.None, "a", true);

            // Character classes & case folding
            yield return ("(?i)[@-A]+", RegexOptions.None, "@AaB", true);
            yield return ("(?i)[A-Z]+", RegexOptions.None, "aAzZ", true);
            yield return ("(?i)[^\\\\]+", RegexOptions.None, "Aa\\", true);
            yield return ("(?i)[acegikmoqsuwy]+", RegexOptions.None, "acegikmoqsuwyACEGIKMOQSUWY", true);

            // Without case folding
            yield return ("[@-A]+", RegexOptions.None, "@AaB", true);
            yield return ("[A-Z]+", RegexOptions.None, "aAzZ", true);
            yield return ("[^\\\\]+", RegexOptions.None, "Aa\\", true);
            yield return ("[acegikmoqsuwy]+", RegexOptions.None, "acegikmoqsuwyACEGIKMOQSUWY", true);

            // Anchoring tests
            yield return ("^abc", RegexOptions.None, "abcdef", true);
            yield return ("^abc", RegexOptions.None, "aabcdef", false);
            yield return ("^[ay]*[bx]+c", RegexOptions.None, "abcdef", true);
            yield return ("^[ay]*[bx]+c", RegexOptions.None, "aabcdef", false);
            yield return ("def$", RegexOptions.None, "abcdef", true);
            yield return ("def$", RegexOptions.None, "abcdeff", false);
            yield return ("d[ex][fy]$", RegexOptions.None, "abcdef", true);
            yield return ("d[ex][fy]$", RegexOptions.None, "abcdeff", false);
            yield return ("[dz][ex][fy]$", RegexOptions.None, "abcdef", true);
            yield return ("[dz][ex][fy]$", RegexOptions.None, "abcdeff", false);
            yield return ("(?m)^abc", RegexOptions.None, "abcdef", true);
            yield return ("(?m)^abc", RegexOptions.None, "aabcdef", false);
            yield return ("(?m)^[ay]*[bx]+c", RegexOptions.None, "abcdef", true);
            yield return ("(?m)^[ay]*[bx]+c", RegexOptions.None, "aabcdef", false);
            yield return ("(?m)def$", RegexOptions.None, "abcdef", true);
            yield return ("(?m)def$", RegexOptions.None, "abcdeff", false);
            yield return ("(?m)d[ex][fy]$", RegexOptions.None, "abcdef", true);
            yield return ("(?m)d[ex][fy]$", RegexOptions.None, "abcdeff", false);
            yield return ("(?m)[dz][ex][fy]$", RegexOptions.None, "abcdef", true);
            yield return ("(?m)[dz][ex][fy]$", RegexOptions.None, "abcdeff", false);
            yield return ("^", RegexOptions.None, "a", true);
            yield return ("^^", RegexOptions.None, "a", true);

            // Context tests
            yield return ("a", RegexOptions.None, "a", true);
            yield return ("ab*", RegexOptions.None, "a", true);
            yield return ("a\\C*", RegexOptions.None, "a", true);
            yield return ("a\\C+", RegexOptions.None, "a", false);
            yield return ("a\\C?", RegexOptions.None, "a", true);
            yield return ("a\\C*?", RegexOptions.None, "a", true);
            yield return ("a\\C+?", RegexOptions.None, "a", false);
            yield return ("a\\C??", RegexOptions.None, "a", true);

            // Former bugs
            yield return ("a\\C*|ba\\C", RegexOptions.None, "baba", true);
            yield return ("\\w*I\\w*", RegexOptions.None, "Inc.", true);
            yield return ("(?:|a)*", RegexOptions.None, "aaa", true);
            yield return ("(?:|a)+", RegexOptions.None, "aaa", true);

            // Tests from re2_test.cc - FullMatch tests
            yield return ("h", RegexOptions.None, "h", true);
            yield return ("hello", RegexOptions.None, "hello", true);
            yield return ("h.*o", RegexOptions.None, "hello", true);
            yield return ("h.*o", RegexOptions.None, "othello", false);
            yield return ("h.*o", RegexOptions.None, "hello!", false);

            // PartialMatch tests
            yield return ("x", RegexOptions.None, "x", true);
            yield return ("h.*o", RegexOptions.None, "hello", true);
            yield return ("h.*o", RegexOptions.None, "othello", true);
            yield return ("h.*o", RegexOptions.None, "hello!", true);
            yield return ("((((((((((((((((((((x))))))))))))))))))))", RegexOptions.None, "x", true);

            // Braces
            yield return ("[0-9a-f+.-]{5,}", RegexOptions.None, "0abcd", true);
            yield return ("[0-9a-f+.-]{5,}", RegexOptions.None, "0abcde", true);
            yield return ("[0-9a-f+.-]{5,}", RegexOptions.None, "0abc", false);

            // Complicated RE
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "foo", true);
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "bar", true);
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "X", true);
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "XY", true);

            // Check full-match handling
            yield return ("fo|foo", RegexOptions.None, "fo", true);
            yield return ("fo|foo", RegexOptions.None, "foo", true);
            yield return ("fo|foo$", RegexOptions.None, "fo", true);
            yield return ("fo|foo$", RegexOptions.None, "foo", true);
            yield return ("foo$", RegexOptions.None, "foo", true);
            yield return ("foo\\$", RegexOptions.None, "foo$bar", false);
            yield return ("fo|bar", RegexOptions.None, "fox", true);

            // QuoteMeta tests
            yield return ("\\Q1.5-2.0?\\E", RegexOptions.None, "1.5-2.0?", true);
            yield return ("\\Q1.5-2.0?\\E", RegexOptions.None, "1 5-2 0?", false);
            yield return ("\\Q\\d\\E", RegexOptions.None, "\\d", true);
            yield return ("\\Q\\d\\E", RegexOptions.None, "d", false);

            // UTF-8 handling
            yield return (".", RegexOptions.None, "\u65e5", true);
            yield return (".", RegexOptions.None, "\u65e5\u672c", true);

            // Case insensitive
            yield return ("(?i)HELLO", RegexOptions.None, "hello", true);
            yield return ("(?i)hello", RegexOptions.None, "HELLO", true);
            yield return ("(?i)[a-z]+", RegexOptions.None, "AbCdE", true);

            // Multiline mode
            yield return ("(?m)^foo", RegexOptions.None, "bar\nfoo", true);
            yield return ("(?m)^foo", RegexOptions.None, "barfoo", false);
            yield return ("(?m)bar$", RegexOptions.None, "bar\nfoo", true);
            yield return ("(?m)bar$", RegexOptions.None, "barfoo", false);

            // Perl operators that work
            yield return ("(?:foo)", RegexOptions.None, "foo", true);
            yield return ("(foo)\\1", RegexOptions.None, "foofoo", true);
            yield return ("(foo)\\1", RegexOptions.None, "foobar", false);

            // Quantifiers
            yield return ("a?", RegexOptions.None, "", true);
            yield return ("a?", RegexOptions.None, "a", true);
            yield return ("a?", RegexOptions.None, "aa", true);
            yield return ("a+", RegexOptions.None, "", false);
            yield return ("a+", RegexOptions.None, "a", true);
            yield return ("a+", RegexOptions.None, "aa", true);
            yield return ("a*", RegexOptions.None, "", true);
            yield return ("a*", RegexOptions.None, "a", true);
            yield return ("a*", RegexOptions.None, "aa", true);
            yield return ("a{2}", RegexOptions.None, "a", false);
            yield return ("a{2}", RegexOptions.None, "aa", true);
            yield return ("a{2}", RegexOptions.None, "aaa", true);
            yield return ("a{2,}", RegexOptions.None, "a", false);
            yield return ("a{2,}", RegexOptions.None, "aa", true);
            yield return ("a{2,}", RegexOptions.None, "aaa", true);
            yield return ("a{2,4}", RegexOptions.None, "a", false);
            yield return ("a{2,4}", RegexOptions.None, "aa", true);
            yield return ("a{2,4}", RegexOptions.None, "aaa", true);
            yield return ("a{2,4}", RegexOptions.None, "aaaa", true);
            yield return ("a{2,4}", RegexOptions.None, "aaaaa", true);
        }

        [Theory]
        [MemberData(nameof(Re2TestData))]
        public void IsMatchTests(Regex regex, string input, bool expectSuccess)
            => Assert.Equal(expectSuccess, regex.IsMatch(input));
    }
}
