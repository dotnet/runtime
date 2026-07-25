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
            // Skip backreferences for NonBacktracking engine
            bool skipBackreferences = RegexHelpers.IsNonBacktracking(engine);

            // Basic matching tests from search_test.cc and re2_test.cc
            // Note: Removed very basic patterns like "a", "a*", "a+" that are already well-covered in other tests
            yield return ("h.*o", RegexOptions.None, "hello world", true);
            yield return ("h.*o", RegexOptions.None, "othello, world", true);
            yield return ("[^\\s\\S]", RegexOptions.None, "aaaaaaa", false);
            yield return ("a*b", RegexOptions.None, "cab", true);
            yield return ("((((((((((((((((((((x))))))))))))))))))))", RegexOptions.None, "x", true);
            yield return ("[abcd]+", RegexOptions.None, "xxxabcdxxx", true);
            yield return ("[^x]+", RegexOptions.None, "xxxabcdxxx", true);
            yield return ("(fo|foo)", RegexOptions.None, "fo", true);
            yield return ("(foo|fo)", RegexOptions.None, "foo", true);

            // Anchor tests
            yield return ("^foo", RegexOptions.None, "foobar", true);
            yield return ("^foo", RegexOptions.None, "barfoo", false);
            yield return ("foo$", RegexOptions.None, "barfoo", true);
            yield return ("foo$", RegexOptions.None, "foobar", false);
            yield return ("^foo$", RegexOptions.None, "foobar", false);
            yield return ("^foo$", RegexOptions.None, "barfoo", false);

            // Word boundaries
            yield return ("\\bfoo\\b", RegexOptions.None, "nofoo foo that", true);
            yield return ("\\bfoo\\b", RegexOptions.None, "nofoo foothat", false);
            yield return ("a\\b", RegexOptions.None, "faoa x", true);
            yield return ("\\bbar", RegexOptions.None, "bar x", true);
            yield return ("\\bbar", RegexOptions.None, "foo\nbar x", true);
            yield return ("bar\\b", RegexOptions.None, "foobar", true);
            yield return ("bar\\b", RegexOptions.None, "foobar\nxxx", true);
            yield return ("\\b", RegexOptions.None, "x", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "X", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "bar", true);
            yield return ("\\b(foo|bar|[A-Z])\\b", RegexOptions.None, "foo", true);
            yield return ("\\b(fo|foo)\\b", RegexOptions.None, "fo", true);
            yield return ("\\b(fo|foo)\\b", RegexOptions.None, "foo", true);

            // Non-word boundaries
            yield return ("\\Bfoo\\B", RegexOptions.None, "n foo xfoox that", true);
            yield return ("a\\B", RegexOptions.None, "faoa x", true);
            yield return ("\\Bbar", RegexOptions.None, "bar x", false);
            yield return ("bar\\B", RegexOptions.None, "foobar", false);
            yield return ("(foo|bar|[A-Z])\\B", RegexOptions.None, "foox", true);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "xXy", true);
            yield return ("\\B(foo|bar|[A-Z])\\B", RegexOptions.None, "abara", true);
            yield return ("\\B(fo|foo)\\B", RegexOptions.None, "xfoo", true);
            yield return ("\\B(foo|fo)\\B", RegexOptions.None, "xfooo", true);

            // Word boundary with special characters
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
            // Note: .NET treats Unicode letters as word characters, unlike RE2/PCRE which only use ASCII
            // So \bx\b won't match "áxβ" in .NET
            yield return ("\\Bx\\B", RegexOptions.None, "axb", true);

            // UTF-8 tests
            yield return ("^\u672c$", RegexOptions.None, "\u672c", true);
            yield return ("^...$", RegexOptions.None, "\u65e5\u672c\u8a9e", true);
            yield return ("^...$", RegexOptions.None, ".\u672c.", true);

            // Octal escapes
            yield return ("\\141", RegexOptions.None, "a", true);
            yield return ("\\060", RegexOptions.None, "0", true);
            yield return ("\\01", RegexOptions.None, "\u0001", true);

            // Hexadecimal escapes
            yield return ("\\x61", RegexOptions.None, "a", true);
            yield return ("\\u0061", RegexOptions.None, "a", true);

            // Multiline mode
            yield return ("(?m)^foo", RegexOptions.None, "bar\nfoo", true);
            yield return ("(?m)^foo", RegexOptions.None, "barfoo", false);
            yield return ("(?m)bar$", RegexOptions.None, "bar\nfoo", true);
            yield return ("(?m)bar$", RegexOptions.None, "barfoo", false);

            // Context tests - removed basic "a" and "ab*" as they're covered elsewhere
            
            // Former bugs
            yield return ("\\w*I\\w*", RegexOptions.None, "Inc.", true);
            yield return ("(?:|a)*", RegexOptions.None, "aaa", true);
            yield return ("(?:|a)+", RegexOptions.None, "aaa", true);

            // PartialMatch tests - removed basic "x" and "h.*o" with "hello" as duplicative
            yield return ("h.*o", RegexOptions.None, "othello", true);
            yield return ("h.*o", RegexOptions.None, "hello!", true);

            // Braces
            yield return ("[0-9a-f+.-]{5,}", RegexOptions.None, "0abcd", true);
            yield return ("[0-9a-f+.-]{5,}", RegexOptions.None, "0abcde", true);
            yield return ("[0-9a-f+.-]{5,}", RegexOptions.None, "0abc", false);

            // Complicated RE - removed "XY" case which is likely covered
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "foo", true);
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "bar", true);
            yield return ("foo|bar|[A-Z]", RegexOptions.None, "X", true);

            // Check full-match handling
            yield return ("fo|foo", RegexOptions.None, "fo", true);
            yield return ("fo|foo", RegexOptions.None, "foo", true);
            yield return ("foo$", RegexOptions.None, "foo", true);

            // UTF-8 handling - removed basic "." test
            
            // Case insensitive
            yield return ("(?i)HELLO", RegexOptions.None, "hello", true);
            yield return ("(?i)hello", RegexOptions.None, "HELLO", true);
            yield return ("(?i)[a-z]+", RegexOptions.None, "AbCdE", true);

            // Perl operators that work
            yield return ("(?:foo)", RegexOptions.None, "foo", true);
            
            // Backreferences - skip for NonBacktracking engine
            if (!skipBackreferences)
            {
                yield return ("(foo)\\1", RegexOptions.None, "foofoo", true);
                yield return ("(foo)\\1", RegexOptions.None, "foobar", false);
            }

            // Quantifiers - keep only unique combinations not well-covered elsewhere
            yield return ("a{2}", RegexOptions.None, "a", false);
            yield return ("a{2}", RegexOptions.None, "aaa", true);
            yield return ("a{2,}", RegexOptions.None, "a", false);
            yield return ("a{2,4}", RegexOptions.None, "aaaaa", true);
        }

        [Theory]
        [MemberData(nameof(Re2TestData))]
        public void IsMatchTests(Regex regex, string input, bool expectSuccess)
            => Assert.Equal(expectSuccess, regex.IsMatch(input));
    }
}
