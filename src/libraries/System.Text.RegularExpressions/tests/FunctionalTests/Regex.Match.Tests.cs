// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexMatchTests
    {
        public static IEnumerable<object[]> Match_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                (string Pattern, string Input, RegexOptions Options, int Beginning, int Length, bool ExpectedSuccess, string ExpectedValue)[] cases = Match_MemberData_Cases(engine).ToArray();
                Regex[] regexes = RegexHelpers.GetRegexesAsync(engine, cases.Select(c => (c.Pattern, (RegexOptions?)c.Options, (TimeSpan?)null)).ToArray()).Result;
                for (int i = 0; i < regexes.Length; i++)
                {
                    yield return new object[] { engine, cases[i].Pattern, cases[i].Input, cases[i].Options, regexes[i], cases[i].Beginning, cases[i].Length, cases[i].ExpectedSuccess, cases[i].ExpectedValue };
                }
            }
        }

        private static IEnumerable<(string Pattern, string Input, RegexOptions Options, int Beginning, int Length, bool ExpectedSuccess, string ExpectedValue)> Match_MemberData_Cases(RegexEngine engine)
        {
            // pattern, input, options, beginning, length, expectedSuccess, expectedValue
            yield return (@"H#", "#H#", RegexOptions.IgnoreCase, 0, 3, true, "H#"); // https://github.com/dotnet/runtime/issues/39390
            yield return (@"H#", "#H#", RegexOptions.None, 0, 3, true, "H#");

            // Testing octal sequence matches: "\\060(\\061)?\\061"
            // Octal \061 is ASCII 49 ('1')
            yield return (@"\060(\061)?\061", "011", RegexOptions.None, 0, 3, true, "011");

            // Testing hexadecimal sequence matches: "(\\x30\\x31\\x32)"
            // Hex \x31 is ASCII 49 ('1')
            yield return (@"(\x30\x31\x32)", "012", RegexOptions.None, 0, 3, true, "012");

            // Testing control character escapes???: "2", "(\u0032)"
            yield return ("(\u0034)", "4", RegexOptions.None, 0, 1, true, "4");

            // Using long loop prefix
            yield return (@"a{10}", new string('a', 10), RegexOptions.None, 0, 10, true, new string('a', 10));
            yield return (@"a{100}", new string('a', 100), RegexOptions.None, 0, 100, true, new string('a', 100));

            yield return (@"a{10}b", new string('a', 10) + "bc", RegexOptions.None, 0, 12, true, new string('a', 10) + "b");
            yield return (@"a{100}b", new string('a', 100) + "bc", RegexOptions.None, 0, 102, true, new string('a', 100) + "b");

            yield return (@"a{11}b", new string('a', 10) + "bc", RegexOptions.None, 0, 12, false, string.Empty);
            yield return (@"a{101}b", new string('a', 100) + "bc", RegexOptions.None, 0, 102, false, string.Empty);

            yield return (@"a{1,3}b", "bc", RegexOptions.None, 0, 2, false, string.Empty);
            yield return (@"a{1,3}b", "abc", RegexOptions.None, 0, 3, true, "ab");
            yield return (@"a{1,3}b", "aaabc", RegexOptions.None, 0, 5, true, "aaab");
            yield return (@"a{1,3}b", "aaaabc", RegexOptions.None, 0, 6, true, "aaab");
            yield return (@"a{1,3}?b", "bc", RegexOptions.None, 0, 2, false, string.Empty);
            yield return (@"a{1,3}?b", "abc", RegexOptions.None, 0, 3, true, "ab");
            yield return (@"a{1,3}?b", "aaabc", RegexOptions.None, 0, 5, true, "aaab");
            yield return (@"a{1,3}?b", "aaaabc", RegexOptions.None, 0, 6, true, "aaab");

            yield return (@"a{2,}b", "abc", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"a{2,}b", "aabc", RegexOptions.None, 0, 4, true, "aab");
            yield return (@"a{2,}?b", "abc", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"a{2,}?b", "aabc", RegexOptions.None, 0, 4, true, "aab");

            // {,n} is treated as a literal rather than {0,n} as it should be
            yield return (@"a{,3}b", "a{,3}bc", RegexOptions.None, 0, 6, true, "a{,3}b");
            yield return (@"a{,3}b", "aaabc", RegexOptions.None, 0, 5, false, string.Empty);

            // Using [a-z], \s, \w: Actual - "([a-zA-Z]+)\\s(\\w+)"
            yield return (@"([a-zA-Z]+)\s(\w+)", "David Bau", RegexOptions.None, 0, 9, true, "David Bau");
            yield return (@"([a-zA-Z]+?)\s(\w+)", "David Bau", RegexOptions.None, 0, 9, true, "David Bau");

            // \\S, \\d, \\D, \\W: Actual - "(\\S+):\\W(\\d+)\\s(\\D+)"
            yield return (@"(\S+):\W(\d+)\s(\D+)", "Price: 5 dollars", RegexOptions.None, 0, 16, true, "Price: 5 dollars");

            // \\S, \\d, \\D, \\W: Actual - "[^0-9]+(\\d+)"
            yield return (@"[^0-9]+(\d+)", "Price: 30 dollars", RegexOptions.None, 0, 17, true, "Price: 30");

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // Zero-width negative lookahead assertion
                yield return (@"abc(?!XXX)\w+", "abcXXXdef", RegexOptions.None, 0, 9, false, string.Empty);

                // Zero-width positive lookbehind assertion
                yield return (@"(\w){6}(?<=XXX)def", "abcXXXdef", RegexOptions.None, 0, 9, true, "abcXXXdef");
                yield return (@"(?<=c)def", "123abcdef", RegexOptions.None, 0, 9, true, "def");
                yield return (@"(?<=abc)def", "123abcdef", RegexOptions.None, 0, 9, true, "def");
                yield return (@"(?<=a\wc)def", "123abcdef", RegexOptions.None, 0, 9, true, "def");
                yield return (@"(?<=\ba\wc)def", "123 abcdef", RegexOptions.None, 0, 10, true, "def");
                yield return (@"(?<=.\ba\wc\B)def", "123 abcdef", RegexOptions.None, 0, 10, true, "def");
                yield return (@"(?<=^123 abc)def", "123 abcdef", RegexOptions.None, 0, 10, true, "def");
                yield return (@"(?<=^123 abc)def", "123 abcdef", RegexOptions.Multiline, 0, 10, true, "def");
                yield return (@"(?<=123$\nabc)def", "123\nabcdef", RegexOptions.Multiline, 0, 10, true, "def");
                yield return (@"\w{3}(?<=^xyz|^abc)defg", "abcdefg", RegexOptions.None, 0, 7, true, "abcdefg");
                yield return (@"(abc)\w(?<=(?(1)d|e))", "abcdabc", RegexOptions.None, 0, 7, true, "abcd");
                yield return (@"(abc)\w(?<=(?(cd)d|e))", "abcdabc", RegexOptions.None, 0, 7, true, "abcd");
                yield return (@"\w{3}(?<=(\w){6,8})", "abcdefghijklmnop", RegexOptions.None, 0, 16, true, "def");
                yield return (@"\w{3}(?<=(?:\d\w){4})", "a1b2c3d4e5", RegexOptions.None, 0, 10, true, "d4e");
                yield return (@"\w{3}(?<=(\w){6,8}?)", "abcdefghijklmnop", RegexOptions.None, 0, 16, true, "def");
                yield return (@"\w{3}(?<=(?:\d\w){3,4}?\d)", "a1b2c3d4e5", RegexOptions.None, 0, 10, true, "3d4");
                yield return (@"(\w{3})\w*(?<=\1)", "---abcdefababc123", RegexOptions.None, 0, 17, true, "abcdefababc");
                yield return (@"(?<=\w{3})\w{4}", "abcdefghijklmnop", RegexOptions.None, 0, 16, true, "defg");
                yield return (@"(?<=\w{3,8})\w{4}", "abcdefghijklmnop", RegexOptions.None, 0, 16, true, "defg");
                yield return (@"(?<=\w*)\w{4}", "abcdefghijklmnop", RegexOptions.None, 0, 16, true, "abcd");
                yield return (@"(?<=\w?)\w{4}", "abcdefghijklmnop", RegexOptions.None, 0, 16, true, "abcd");
                yield return (@"(?<=\d?)a{4}", "123aaaaaaaaa", RegexOptions.None, 0, 12, true, "aaaa");
                yield return (@"(?<=a{3,5}[ab]*)1234", "aaaaaaa1234", RegexOptions.None, 0, 11, true, "1234");
                yield return (@"(\w)*?3(?<=33)$", "1233", RegexOptions.None, 0, 4, true, "1233");

                // Zero-width negative lookbehind assertion: Actual - "(\\w){6}(?<!XXX)def"
                yield return (@"(\w){6}(?<!XXX)def", "XXXabcdef", RegexOptions.None, 0, 9, true, "XXXabcdef");
                yield return (@"123(?<!$) abcdef", "123 abcdef", RegexOptions.None, 0, 10, true, "123 abcdef");
                yield return (@"(abc)\w(?<!(?(1)e|d))", "abcdabc", RegexOptions.None, 0, 7, true, "abcd");
                yield return (@"(abc)\w(?<!(?(cd)e|d))", "abcdabc", RegexOptions.None, 0, 7, true, "abcd");

                // Nonbacktracking subexpression: Actual - "[^0-9]+(?>[0-9]+)3"
                // The last 3 causes the match to fail, since the non backtracking subexpression does not give up the last digit it matched
                // for it to be a success. For a correct match, remove the last character, '3' from the pattern
                yield return ("[^0-9]+(?>[0-9]+)3", "abc123", RegexOptions.None, 0, 6, false, string.Empty);
                yield return ("[^0-9]+(?>[0-9]+)", "abc123", RegexOptions.None, 0, 6, true, "abc123");

                yield return (@"(?!.*a)\w*g", "bcaefg", RegexOptions.None, 0, 6, true, "efg");
                yield return (@"(?!.*a)\w*g", "aaaaag", RegexOptions.None, 0, 6, true, "g");
                yield return (@"(?!.*a)\w*g", "aaaaaa", RegexOptions.None, 0, 6, false, string.Empty);
            }

            // More nonbacktracking expressions
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.IgnoreCase })
            {
                string Case(string s) => (options & RegexOptions.IgnoreCase) != 0 ? s.ToUpper() : s;

                yield return (Case("(?:hi|hello|hey)hi"), "hellohi", options, 0, 7, true, "hellohi"); // allow backtracking and it succeeds
                yield return (Case(@"a[^wyz]*w"), "abczw", RegexOptions.IgnoreCase, 0, 0, false, string.Empty);

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    // Atomic greedy
                    yield return (Case("(?>[0-9]+)abc"), "abc12345abc", options, 3, 8, true, "12345abc");
                    yield return (Case("(?>(?>[0-9]+))abc"), "abc12345abc", options, 3, 8, true, "12345abc");
                    yield return (Case("(?>[0-9]*)abc"), "abc12345abc", options, 3, 8, true, "12345abc");
                    yield return (Case("(?>[^z]+)z"), "zzzzxyxyxyz123", options, 4, 9, true, "xyxyxyz");
                    yield return (Case("(?>(?>[^z]+))z"), "zzzzxyxyxyz123", options, 4, 9, true, "xyxyxyz");
                    yield return (Case("(?>[^z]*)z123"), "zzzzxyxyxyz123", options, 4, 10, true, "xyxyxyz123");

                    yield return (Case("(?>[^12]+)1"), "121231", options, 0, 6, true, "31");
                    yield return (Case("(?>[^123]+)1"), "12312341", options, 0, 8, true, "41");
                    yield return (Case("(?>[^1234]+)1"), "1234123451", options, 0, 10, true, "51");
                    yield return (Case("(?>[^12345]+)1"), "123451234561", options, 0, 12, true, "61");
                    yield return (Case("(?>[^123456]+)1"), "12345612345671", options, 0, 14, true, "71");

                    yield return (Case("(?>a+)123"), "aa1234", options, 0, 5, true, "aa123");
                    yield return (Case("(?>a*)123"), "aa1234", options, 0, 5, true, "aa123");
                    yield return (Case("(?>(?>a*))123"), "aa1234", options, 0, 5, true, "aa123");
                    yield return (Case("(?>a{2,})b"), "aaab", options, 0, 4, true, "aaab");
                    yield return (Case("[a-z]{0,4}(?>[x-z]*.)(?=xyz1)"), "abcdxyz1", options, 0, 8, true, "abcd");
                    yield return (Case("[a-z]{0,4}(?=[x-z]*.)(?=cd)"), "abcdxyz1", options, 0, 8, true, "ab");
                    yield return (Case("[a-z]{0,4}(?![x-z]*[wx])(?=cd)"), "abcdxyz1", options, 0, 8, true, "ab");

                    // Atomic lazy
                    yield return (Case("(?>[0-9]+?)abc"), "abc12345abc", options, 3, 8, true, "5abc");
                    yield return (Case("(?>(?>[0-9]+?))abc"), "abc12345abc", options, 3, 8, true, "5abc");
                    yield return (Case("(?>[0-9]*?)abc"), "abc12345abc", options, 3, 8, true, "abc");
                    yield return (Case("(?>[^z]+?)z"), "zzzzxyxyxyz123", options, 4, 9, true, "yz");
                    yield return (Case("(?>(?>[^z]+?))z"), "zzzzxyxyxyz123", options, 4, 9, true, "yz");
                    yield return (Case("(?>[^z]*?)z123"), "zzzzxyxyxyz123", options, 4, 10, true, "z123");
                    yield return (Case("(?>a+?)123"), "aa1234", options, 0, 5, true, "a123");
                    yield return (Case("(?>a*?)123"), "aa1234", options, 0, 5, true, "123");
                    yield return (Case("(?>(?>a*?))123"), "aa1234", options, 0, 5, true, "123");
                    yield return (Case("(?>a{2,}?)b"), "aaab", options, 0, 4, true, "aab");

                    // Alternations
                    yield return (Case("(?>hi|hello|hey)hi"), "hellohi", options, 0, 0, false, string.Empty);
                    yield return (Case("(?>hi|hello|hey)hi"), "hihi", options, 0, 4, true, "hihi");
                }
            }

            // Loops at beginning of expressions
            yield return (@"a+", "aaa", RegexOptions.None, 0, 3, true, "aaa");
            yield return (@"a+\d+", "a1", RegexOptions.None, 0, 2, true, "a1");
            yield return (@".+\d+", "a1", RegexOptions.None, 0, 2, true, "a1");
            yield return (".+\nabc", "a\nabc", RegexOptions.None, 0, 5, true, "a\nabc");
            yield return (@"\d+", "abcd123efg", RegexOptions.None, 0, 10, true, "123");
            yield return (@"\d+\d+", "abcd123efg", RegexOptions.None, 0, 10, true, "123");
            yield return (@"\w+123\w+", "abcd123efg", RegexOptions.None, 0, 10, true, "abcd123efg");
            yield return (@"\d+\w+", "abcd123efg", RegexOptions.None, 0, 10, true, "123efg");
            yield return (@"\w+@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{3,}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{4,}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, false, string.Empty);
            yield return (@"\w{2,5}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{3}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{0,3}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{0,2}c@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w*@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"(\w+)@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"((\w+))@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"(\w+)c@\w+.com", "abc@def.comabcdef", RegexOptions.None, 0, 17, true, "abc@def.com");
            yield return (@"\w+://\w+\.\w+", "test https://dot.net test", RegexOptions.None, 0, 25, true, "https://dot.net");
            yield return (@"\w+[:|]//\w+\.\w+", "test https://dot.net test", RegexOptions.None, 0, 25, true, "https://dot.net");
            yield return (@"\w+[|:$]//\w+\.\w+", "test https://dot.net test", RegexOptions.None, 0, 25, true, "https://dot.net");
            yield return (@"\w+[|$:*]//\w+\.\w+", "test https://dot.net test", RegexOptions.None, 0, 25, true, "https://dot.net");
            yield return (@"\w+[|$*:&]//\w+\.\w+", "test https://dot.net test", RegexOptions.None, 0, 25, true, "https://dot.net");
            yield return (@".+a", "baa", RegexOptions.None, 0, 3, true, "baa");
            yield return (@"[ab]+a", "cacbaac", RegexOptions.None, 0, 7, true, "baa");
            yield return (@"^(\d{2,3}){2}$", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"(\d{2,3}){2}", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"((\d{2,3})){2}", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"(abc\d{2,3}){2}", "abc123abc4567", RegexOptions.None, 0, 12, true, "abc123abc456");

            // Lazy versions of those loops
            yield return (@"a+?", "aaa", RegexOptions.None, 0, 3, true, "a");
            yield return (@"a+?\d+?", "a1", RegexOptions.None, 0, 2, true, "a1");
            yield return (@".+?\d+?", "a1", RegexOptions.None, 0, 2, true, "a1");
            yield return (".+?\nabc", "a\nabc", RegexOptions.None, 0, 5, true, "a\nabc");
            yield return (@"\d+?", "abcd123efg", RegexOptions.None, 0, 10, true, "1");
            yield return (@"\d+?\d+?", "abcd123efg", RegexOptions.None, 0, 10, true, "12");
            yield return (@"\w+?123\w+?", "abcd123efg", RegexOptions.None, 0, 10, true, "abcd123e");
            yield return (@"\d+?\w+?", "abcd123efg", RegexOptions.None, 0, 10, true, "12");
            yield return (@"\w+?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{3,}?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{4,}?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, false, string.Empty);
            yield return (@"\w{2,5}?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{3}?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{0,3}?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w{0,2}?c@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"\w*?@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"(\w+?)@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"((\w+?))@\w+?\.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com");
            yield return (@"(\w+?)c@\w+?\.com", "abc@def.comabcdef", RegexOptions.None, 0, 17, true, "abc@def.com");
            yield return (@".+?a", "baa", RegexOptions.None, 0, 3, true, "ba");
            yield return (@"[ab]+?a", "cacbaac", RegexOptions.None, 0, 7, true, "ba");
            yield return (@"^(\d{2,3}?){2}$", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"(\d{2,3}?){2}", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"((\d{2,3}?)){2}", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"(abc\d{2,3}?){2}", "abc123abc4567", RegexOptions.None, 0, 12, true, "abc123abc45");
            yield return (@"(b|a|aa)((?:aa)+?)+?$", "aaaaaaaa", RegexOptions.None, 0, 8, true, "aaaaaaaa");
            yield return (@"(|a|aa)(((?:aa)+?)+?|aaaaab)\w$", "aaaaaabc", RegexOptions.None, 0, 8, true, "aaaaaabc");

            // Testing selected FindOptimizations finds the right prefix
            yield return (@"(^|a+)bc", " aabc", RegexOptions.None, 0, 5, true, "aabc");
            yield return (@"(^|($|a+))bc", " aabc", RegexOptions.None, 0, 5, true, "aabc");
            yield return (@"yz(^|a+)bc", " yzaabc", RegexOptions.None, 0, 7, true, "yzaabc");
            yield return (@"(^a|a$) bc", "a bc", RegexOptions.None, 0, 4, true, "a bc");
            yield return (@"(abcdefg|abcdef|abc|a)h", "    ah  ", RegexOptions.None, 0, 8, true, "ah");
            yield return (@"(^abcdefg|abcdef|^abc|a)h", "    abcdefh  ", RegexOptions.None, 0, 13, true, "abcdefh");
            yield return (@"(a|^abcdefg|abcdef|^abc)h", "    abcdefh  ", RegexOptions.None, 0, 13, true, "abcdefh");
            yield return (@"(abcdefg|abcdef)h", "    abcdefghij  ", RegexOptions.None, 0, 16, true, "abcdefgh");

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // Back references not support with NonBacktracking

                yield return (@"(\w+)c@\w+.com\1", "abc@def.comabcdef", RegexOptions.None, 0, 17, true, "abc@def.comab");
                yield return (@"(\w+)@def.com\1", "abc@def.comab", RegexOptions.None, 0, 13, false, string.Empty);
                yield return (@"(\w+)@def.com\1", "abc@def.combc", RegexOptions.None, 0, 13, true, "bc@def.combc");
                yield return (@"(\w*)@def.com\1", "abc@def.com", RegexOptions.None, 0, 11, true, "@def.com");
                yield return (@"\w+(?<!a)", "a", RegexOptions.None, 0, 1, false, string.Empty);
                yield return (@"\w+(?<!a)", "aa", RegexOptions.None, 0, 2, false, string.Empty);
                yield return (@"(?>\w+)(?<!a)", "a", RegexOptions.None, 0, 1, false, string.Empty);
                yield return (@"(?>\w+)(?<!a)", "aa", RegexOptions.None, 0, 2, false, string.Empty);

                yield return (@"(\w+?)c@\w+?.com\1", "abc@def.comabcdef", RegexOptions.None, 0, 17, true, "abc@def.comab");
                yield return (@"(\w+?)@def.com\1", "abc@def.comab", RegexOptions.None, 0, 13, false, string.Empty);
                yield return (@"(\w+?)@def.com\1", "abc@def.combc", RegexOptions.None, 0, 13, true, "bc@def.combc");
                yield return (@"(\w*?)@def.com\1", "abc@def.com", RegexOptions.None, 0, 11, true, "@def.com");
                yield return (@"\w+?(?<!a)", "a", RegexOptions.None, 0, 1, false, string.Empty);
                yield return (@"\w+?(?<!a)", "aa", RegexOptions.None, 0, 2, false, string.Empty);
                yield return (@"(?>\w+?)(?<!a)", "a", RegexOptions.None, 0, 1, false, string.Empty);
                yield return (@"(?>\w+?)(?<!a)", "aa", RegexOptions.None, 0, 2, false, string.Empty);
            }

            yield return (@"(\d{2,3})+", "1234", RegexOptions.None, 0, 4, true, "123");
            yield return (@"(\d{2,3})*", "123456", RegexOptions.None, 0, 4, true, "123");
            yield return (@"(\d{2,3})+?", "1234", RegexOptions.None, 0, 4, true, "123");
            yield return (@"(\d{2,3})*?", "123456", RegexOptions.None, 0, 4, true, "");
            yield return (@"(\d{2,3}?)+", "1234", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"(\d{2,3}?)*", "123456", RegexOptions.None, 0, 4, true, "1234");
            yield return (@"(\d{2,3}?)+?", "1234", RegexOptions.None, 0, 4, true, "12");
            yield return (@"(\d{2,3}?)*?", "123456", RegexOptions.None, 0, 4, true, "");

            foreach (RegexOptions lineOption in new[] { RegexOptions.None, RegexOptions.Singleline })
            {
                yield return (@".*", "abc", lineOption, 1, 2, true, "bc");
                yield return (@".*c", "abc", lineOption, 1, 2, true, "bc");
                yield return (@"b.*", "abc", lineOption, 1, 2, true, "bc");
                yield return (@".*", "abc", lineOption, 2, 1, true, "c");

                yield return (@"a.*[bc]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*[bc]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*[bc]", "xyza12345d6789", lineOption, 0, 14, false, "");

                yield return (@"a.*[bcd]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*[bcd]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*[bcd]", "xyza12345d6789", lineOption, 0, 14, true, "a12345d");
                yield return (@"a.*[bcd]", "xyza12345e6789", lineOption, 0, 14, false, "");

                yield return (@"a.*[bcde]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*[bcde]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*[bcde]", "xyza12345d6789", lineOption, 0, 14, true, "a12345d");
                yield return (@"a.*[bcde]", "xyza12345e6789", lineOption, 0, 14, true, "a12345e");
                yield return (@"a.*[bcde]", "xyza12345f6789", lineOption, 0, 14, false, "");

                yield return (@"a.*[bcdef]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*[bcdef]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*[bcdef]", "xyza12345d6789", lineOption, 0, 14, true, "a12345d");
                yield return (@"a.*[bcdef]", "xyza12345e6789", lineOption, 0, 14, true, "a12345e");
                yield return (@"a.*[bcdef]", "xyza12345f6789", lineOption, 0, 14, true, "a12345f");
                yield return (@"a.*[bcdef]", "xyza12345g6789", lineOption, 0, 14, false, "");

                yield return (@".*?", "abc", lineOption, 1, 2, true, "");
                yield return (@".*?c", "abc", lineOption, 1, 2, true, "bc");
                yield return (@"b.*?", "abc", lineOption, 1, 2, true, "b");
                yield return (@".*?", "abc", lineOption, 2, 1, true, "");

                yield return (@"a.*?[bc]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*?[bc]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*?[bc]", "xyza12345d6789", lineOption, 0, 14, false, "");

                yield return (@"a.*?[bcd]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*?[bcd]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*?[bcd]", "xyza12345d6789", lineOption, 0, 14, true, "a12345d");
                yield return (@"a.*?[bcd]", "xyza12345e6789", lineOption, 0, 14, false, "");

                yield return (@"a.*?[bcde]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*?[bcde]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*?[bcde]", "xyza12345d6789", lineOption, 0, 14, true, "a12345d");
                yield return (@"a.*?[bcde]", "xyza12345e6789", lineOption, 0, 14, true, "a12345e");
                yield return (@"a.*?[bcde]", "xyza12345f6789", lineOption, 0, 14, false, "");

                yield return (@"a.*?[bcdef]", "xyza12345b6789", lineOption, 0, 14, true, "a12345b");
                yield return (@"a.*?[bcdef]", "xyza12345c6789", lineOption, 0, 14, true, "a12345c");
                yield return (@"a.*?[bcdef]", "xyza12345d6789", lineOption, 0, 14, true, "a12345d");
                yield return (@"a.*?[bcdef]", "xyza12345e6789", lineOption, 0, 14, true, "a12345e");
                yield return (@"a.*?[bcdef]", "xyza12345f6789", lineOption, 0, 14, true, "a12345f");
                yield return (@"a.*?[bcdef]", "xyza12345g6789", lineOption, 0, 14, false, "");
            }

            // Nested loops
            yield return ("a*(?:a[ab]*)*", "aaaababbbbbbabababababaaabbb", RegexOptions.None, 0, 28, true, "aaaa");
            yield return ("a*(?:a[ab]*?)*?", "aaaababbbbbbabababababaaabbb", RegexOptions.None, 0, 28, true, "aaaa");

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return (@"\Aaaa\w+zzz\Z", "aaaasdfajsdlfjzzz", RegexOptions.IgnoreCase, 0, 17, true, "aaaasdfajsdlfjzzz");
            yield return (@"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.IgnoreCase, 0, 4, false, string.Empty);
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return (@"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.RightToLeft, 0, 4, false, string.Empty);
                yield return (@"\Aaaaaa\w+zzzzz\Z", "aaaa", RegexOptions.RightToLeft, 0, 4, false, string.Empty);
                yield return (@"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 0, 4, false, string.Empty);
            }
            yield return (@"abc\Adef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty);
            yield return (@"abc\adef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty);
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return (@"abc\Gdef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty);
            }
            yield return (@"abc^def", "abcdef", RegexOptions.None, 0, 0, false, string.Empty);
            yield return (@"abc\Zef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty);
            yield return (@"abc\zef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty);

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return (@"\Aaaa\w+zzz\Z", "aaaasdfajsdlfjzzza", RegexOptions.None, 0, 18, false, string.Empty);

            // Anchors
            foreach (RegexOptions anchorOptions in new[] { RegexOptions.None, RegexOptions.Multiline })
            {
                yield return (@"^abc", "abc", anchorOptions, 0, 3, true, "abc");
                yield return (@"^abc", " abc", anchorOptions, 0, 4, false, "");
                yield return (@"^abc|^def", "def", anchorOptions, 0, 3, true, "def");
                yield return (@"^abc|^def", " abc", anchorOptions, 0, 4, false, "");
                yield return (@"^abc|^def", " def", anchorOptions, 0, 4, false, "");
                yield return (@"abc|^def", " abc", anchorOptions, 0, 4, true, "abc");
                yield return (@"abc|^def|^efg", " abc", anchorOptions, 0, 4, true, "abc");
                yield return (@"^abc|def|^efg", " def", anchorOptions, 0, 4, true, "def");
                yield return (@"^abc|def", " def", anchorOptions, 0, 4, true, "def");
                yield return (@"abcd$", "1234567890abcd", anchorOptions, 0, 14, true, "abcd");
                foreach (string endAnchor in new[] { @"$", @"\Z", @"\z" })
                {
                    yield return (@"abc{1,4}d" + endAnchor, "1234567890abcd", anchorOptions, 0, 14, true, "abcd");
                    yield return (@"abc{1,4}d" + endAnchor, "1234567890abccccd", anchorOptions, 0, 17, true, "abccccd");
                }
            }

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // \G means "end of last match if any, else point where matching started"
                yield return (@"\Gabc", "abc", RegexOptions.None, 0, 3, true, "abc");
                yield return (@"\Gabc", " abc", RegexOptions.None, 0, 4, false, "");
                yield return (@"\Gabc", " abc", RegexOptions.None, 1, 3, true, "abc");
                yield return (@"\Gabc", "  abc", RegexOptions.None, 1, 4, false, "");
                yield return (@"\Gabc", "  abc", RegexOptions.None, 1, 3, false, "");
                yield return (@"\Gabc|\Gdef", "def", RegexOptions.None, 0, 3, true, "def");
                yield return (@"\Gabc|\Gdef", " abc", RegexOptions.None, 0, 4, false, "");
                yield return (@"\Gabc|\Gdef", " def", RegexOptions.None, 0, 4, false, "");
                yield return (@"\Gabc|\Gdef", " abc", RegexOptions.None, 1, 3, true, "abc");
                yield return (@"\Gabc|\Gdef", " def", RegexOptions.None, 1, 3, true, "def");
                yield return (@"abc|\Gdef", " abc", RegexOptions.None, 0, 4, true, "abc");
                yield return (@"\Gabc|def", " def", RegexOptions.None, 0, 4, true, "def");
            }

            // Anchors and multiline
            yield return (@"^A$", "A\n", RegexOptions.Multiline, 0, 2, true, "A");
            yield return (@"^A$", "ABC\n", RegexOptions.Multiline, 0, 4, false, string.Empty);
            yield return (@"^A$", "123\nA", RegexOptions.Multiline, 0, 5, true, "A");
            yield return (@"^A$", "123\nA\n456", RegexOptions.Multiline, 0, 9, true, "A");
            yield return (@"^A$|^B$", "123\nB\n456", RegexOptions.Multiline, 0, 9, true, "B");

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return (@"\A(line2\n)line3\Z", "line2\nline3\n", RegexOptions.Multiline, 0, 12, true, "line2\nline3");

            // Using beginning/end of string chars ^: Actual - "^b"
            yield return ("^b", "ab", RegexOptions.None, 0, 2, false, string.Empty);

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // Actual - "(?<char>\\w)\\<char>"
                yield return (@"(?<char>\w)\<char>", "aa", RegexOptions.None, 0, 2, true, "aa");

                // Actual - "(?<43>\\w)\\43"
                yield return (@"(?<43>\w)\43", "aa", RegexOptions.None, 0, 2, true, "aa");

                // Actual - "abc(?(1)111|222)"
                yield return ("(abbc)(?(1)111|222)", "abbc222", RegexOptions.None, 0, 7, false, string.Empty);
            }

            // "x" option. Removes unescaped whitespace from the pattern: Actual - " ([^/]+) ","x"
            yield return ("            ((.)+) #comment     ", "abc", RegexOptions.IgnorePatternWhitespace, 0, 3, true, "abc");

            // "x" option. Removes unescaped whitespace from the pattern. : Actual - "\x20([^/]+)\x20","x"
            yield return ("\x20([^/]+)\x20\x20\x20\x20\x20\x20\x20", " abc       ", RegexOptions.IgnorePatternWhitespace, 0, 10, true, " abc      ");

            // Turning on case insensitive option in mid-pattern : Actual - "aaa(?i:match this)bbb"
            if ("i".ToUpper() == "I")
            {
                yield return ("aaa(?i:match this)bbb", "aaaMaTcH ThIsbbb", RegexOptions.None, 0, 16, true, "aaaMaTcH ThIsbbb");
            }
            yield return ("(?i:a)b(?i:c)d", "aaaaAbCdddd", RegexOptions.None, 0, 11, true, "AbCd");
            yield return ("(?i:[\u0000-\u1000])[Bb]", "aaaaAbCdddd", RegexOptions.None, 0, 11, true, "Ab");

            // Turning off case insensitive option in mid-pattern : Actual - "aaa(?-i:match this)bbb", "i"
            yield return ("aAa(?-i:match this)bbb", "AaAmatch thisBBb", RegexOptions.IgnoreCase, 0, 16, true, "AaAmatch thisBBb");

            // Turning on/off all the options at once : Actual - "aaa(?imnsx-imnsx:match this)bbb", "i"
            yield return ("aaa(?imnsx-imnsx:match this)bbb", "AaAmatcH thisBBb", RegexOptions.IgnoreCase, 0, 16, false, string.Empty);

            // Actual - "aaa(?#ignore this completely)bbb"
            yield return ("aAa(?#ignore this completely)bbb", "aAabbb", RegexOptions.None, 0, 6, true, "aAabbb");

            // Trying empty string: Actual "[a-z0-9]+", ""
            yield return ("[a-z0-9]+", "", RegexOptions.None, 0, 0, false, string.Empty);

            // Numbering pattern slots: "(?<1>\\d{3})(?<2>\\d{3})(?<3>\\d{4})"
            yield return (@"(?<1>\d{3})(?<2>\d{3})(?<3>\d{4})", "8885551111", RegexOptions.None, 0, 10, true, "8885551111");
            yield return (@"(?<1>\d{3})(?<2>\d{3})(?<3>\d{4})", "Invalid string", RegexOptions.None, 0, 14, false, string.Empty);

            // Not naming pattern slots at all: "^(cat|chat)"
            yield return ("^(cat|chat)", "cats are bad", RegexOptions.None, 0, 12, true, "cat");

            yield return ("abc", "abc", RegexOptions.None, 0, 3, true, "abc");
            yield return ("abc", "aBc", RegexOptions.None, 0, 3, false, string.Empty);
            yield return ("abc", "aBc", RegexOptions.IgnoreCase, 0, 3, true, "aBc");
            yield return (@"abc.*def", "abcghiDEF", RegexOptions.IgnoreCase, 0, 9, true, "abcghiDEF");

            // Using *, +, ?, {}: Actual - "a+\\.?b*\\.?c{2}"
            yield return (@"a+\.?b*\.+c{2}", "ab.cc", RegexOptions.None, 0, 5, true, "ab.cc");
            yield return (@"[^a]+\.[^z]+", "zzzzz", RegexOptions.None, 0, 5, false, string.Empty);

            // IgnoreCase
            yield return ("AAA", "aaabbb", RegexOptions.IgnoreCase, 0, 6, true, "aaa");
            yield return (@"\p{Lu}", "1bc", RegexOptions.IgnoreCase, 0, 3, true, "b");
            yield return (@"\p{Ll}", "1bc", RegexOptions.IgnoreCase, 0, 3, true, "b");
            yield return (@"\p{Lt}", "1bc", RegexOptions.IgnoreCase, 0, 3, true, "b");
            yield return (@"\p{Lo}", "1bc", RegexOptions.IgnoreCase, 0, 3, false, string.Empty);
            yield return (".[abc]", "xYZAbC", RegexOptions.IgnoreCase, 0, 6, true, "ZA");
            yield return (".[abc]", "xYzXyZx", RegexOptions.IgnoreCase, 0, 6, false, "");

            // Sets containing characters that differ by a bit
            yield return ("123[Aa]", "123a", RegexOptions.None, 0, 4, true, "123a");
            yield return ("123[0p]", "123p", RegexOptions.None, 0, 4, true, "123p");
            yield return ("123[Aa@]", "123@", RegexOptions.None, 0, 4, true, "123@");

            // "\D+"
            yield return (@"\D+", "12321", RegexOptions.None, 0, 5, false, string.Empty);

            // Groups
            yield return ("(?<first_name>\\S+)\\s(?<last_name>\\S+)", "David Bau", RegexOptions.None, 0, 9, true, "David Bau");

            // "^b"
            yield return ("^b", "abc", RegexOptions.None, 0, 3, false, string.Empty);

            // Trim leading and trailing whitespace
            yield return (@"\s*(.*?)\s*$", " Hello World ", RegexOptions.None, 0, 13, true, " Hello World ");

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // Throws NotSupported with NonBacktracking engine because of the balancing group dog-0
                yield return (@"(?<cat>cat)\w+(?<dog-0>dog)", "cat_Hello_World_dog", RegexOptions.None, 0, 19, false, string.Empty);
            }

            // Atomic Zero-Width Assertions \A \Z \z \b \B
            yield return (@"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", RegexOptions.None, 0, 20, false, string.Empty);
            yield return (@"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", RegexOptions.Multiline, 0, 20, false, string.Empty);
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return (@"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", RegexOptions.ECMAScript, 0, 20, false, string.Empty);
            }

            yield return (@"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat", RegexOptions.None, 0, 15, false, string.Empty);
            yield return (@"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat     ", RegexOptions.Multiline, 0, 20, false, string.Empty);
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return (@"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat     ", RegexOptions.ECMAScript, 0, 20, false, string.Empty);
            }

            yield return (@"(cat)\s+(dog)\z", "cat   dog\n\n\ncat", RegexOptions.None, 0, 15, false, string.Empty);
            yield return (@"(cat)\s+(dog)\z", "cat   dog\n\n\ncat     ", RegexOptions.Multiline, 0, 20, false, string.Empty);
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return (@"(cat)\s+(dog)\z", "cat   dog\n\n\ncat     ", RegexOptions.ECMAScript, 0, 20, false, string.Empty);
            }
            yield return (@"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", RegexOptions.None, 0, 16, false, string.Empty);
            yield return (@"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, 0, 16, false, string.Empty);
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return (@"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, 0, 16, false, string.Empty);
            }

            yield return (@"\b@cat", "123START123;@catEND", RegexOptions.None, 0, 19, false, string.Empty);
            yield return (@"\b<cat", "123START123'<catEND", RegexOptions.None, 0, 19, false, string.Empty);
            yield return (@"\b,cat", "satwe,,,START',catEND", RegexOptions.None, 0, 21, false, string.Empty);
            yield return (@"\b\[cat", "`12START123'[catEND", RegexOptions.None, 0, 19, false, string.Empty);

            yield return (@"\B@cat", "123START123@catEND", RegexOptions.None, 0, 18, false, string.Empty);
            yield return (@"\B<cat", "123START123<catEND", RegexOptions.None, 0, 18, false, string.Empty);
            yield return (@"\B,cat", "satwe,,,START,catEND", RegexOptions.None, 0, 20, false, string.Empty);
            yield return (@"\B\[cat", "`12START123[catEND", RegexOptions.None, 0, 18, false, string.Empty);

            // Lazy operator Backtracking
            yield return (@"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/", "http://www.msn.com", RegexOptions.IgnoreCase, 0, 18, false, string.Empty);

            // Expressions containing Nothing (subexpressions that never match).
            // (Lookarounds aren't supported by NonBacktracking, but optimizer reduces (?!) to Nothing, which is supported.)
            yield return ("(?!)", "cat", RegexOptions.None, 0, 3, false, string.Empty); 
            yield return ("(?!)|((?!))|(?!)", "cat", RegexOptions.None, 0, 3, false, string.Empty); 
            yield return ("cat(?!)", "cat", RegexOptions.None, 0, 3, false, string.Empty);
            yield return ("(?<!)", "cat", RegexOptions.None, 0, 3, false, string.Empty);
            yield return ("(?!)|cat", "cat", RegexOptions.None, 0, 3, true, "cat");
            yield return ("dog|(?!)|cat", "cat", RegexOptions.None, 0, 3, true, "cat");
            yield return ("dog|cat(?!)|cat", "cat", RegexOptions.None, 0, 3, true, "cat");

            // Alternation construct
            foreach (string input in new[] { "abc", "def" })
            {
                string upper = input.ToUpperInvariant();

                // Two branches
                yield return (@"abc|def", input, RegexOptions.None, 0, input.Length, true, input);
                yield return (@"abc|def", upper, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, input.Length, true, upper);
                yield return (@"abc|def", upper, RegexOptions.None, 0, input.Length, false, "");

                // Three branches
                yield return (@"abc|agh|def", input, RegexOptions.None, 0, input.Length, true, input);
                yield return (@"abc|agh|def", upper, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, input.Length, true, upper);
                yield return (@"abc|agh|def", upper, RegexOptions.None, 0, input.Length, false, "");

                // Four branches
                yield return (@"abc|agh|def|aij", input, RegexOptions.None, 0, input.Length, true, input);
                yield return (@"abc|agh|def|aij", upper, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, input.Length, true, upper);
                yield return (@"abc|agh|def|aij", upper, RegexOptions.None, 0, input.Length, false, "");

                // Four branches (containing various other constructs)
                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    yield return (@"abc|(agh)|(?=def)def|(?:(?(aij)aij|(?!)))", input, RegexOptions.None, 0, input.Length, true, input);
                    yield return (@"abc|(agh)|(?=def)def|(?:(?(aij)aij|(?!)))", upper, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, input.Length, true, upper);
                    yield return (@"abc|(agh)|(?=def)def|(?:(?(aij)aij|(?!)))", upper, RegexOptions.None, 0, input.Length, false, "");
                }

                // Sets in various positions in each branch
                yield return (@"a\wc|\wgh|de\w", input, RegexOptions.None, 0, input.Length, true, input);
                yield return (@"a\wc|\wgh|de\w", upper, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, input.Length, true, upper);
                yield return (@"a\wc|\wgh|de\w", upper, RegexOptions.None, 0, input.Length, false, "");
            }
            yield return ("[^a-z0-9]etag|[^a-z0-9]digest", "this string has .digest as a substring", RegexOptions.None, 16, 7, true, ".digest");
            yield return (@"(\w+|\d+)a+[ab]+", "123123aa", RegexOptions.None, 0, 8, true, "123123aa");
            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                yield return ("(?(dog2))", "dog2", RegexOptions.None, 0, 4, true, string.Empty);
                yield return ("(?(a:b))", "a", RegexOptions.None, 0, 1, true, string.Empty);
                yield return ("(?(a:))", "a", RegexOptions.None, 0, 1, true, string.Empty);
                yield return ("(?(cat)cat|dog)", "cat", RegexOptions.None, 0, 3, true, "cat");
                yield return ("(?((?=cat))cat|dog)", "cat", RegexOptions.None, 0, 3, true, "cat");
                yield return ("(?(cat)|dog)", "cat", RegexOptions.None, 0, 3, true, string.Empty);
                yield return ("(?(cat)|dog)", "catdog", RegexOptions.None, 0, 6, true, string.Empty);
                yield return ("(?(cat)|dog)", "oof", RegexOptions.None, 0, 3, false, string.Empty);
                yield return ("(?(cat)dog1|dog2)", "catdog1", RegexOptions.None, 0, 7, false, string.Empty);
                yield return ("(?(cat)dog1|dog2)", "catdog2", RegexOptions.None, 0, 7, true, "dog2");
                yield return ("(?(cat)dog1|dog2)", "catdog1dog2", RegexOptions.None, 0, 11, true, "dog2");
                yield return (@"(?(\w+)\w+)dog", "catdog", RegexOptions.None, 0, 6, true, "catdog");
                yield return (@"(?(abc)\w+|\w{0,2})dog", "catdog", RegexOptions.None, 0, 6, true, "atdog");
                yield return (@"(?(abc)cat|\w{0,2})dog", "catdog", RegexOptions.None, 0, 6, true, "atdog");
                yield return ("(a|ab|abc|abcd)d", "abcd", RegexOptions.RightToLeft, 0, 4, true, "abcd");

                yield return ("(?>(?:a|ab|abc|abcd))d", "abcd", RegexOptions.None, 0, 4, false, string.Empty);
                yield return ("(?>(?:a|ab|abc|abcd))d", "abcd", RegexOptions.RightToLeft, 0, 4, true, "abcd");

                yield return (@"(...)(?(1)\w*)[a1 ]", "zabcaaaaaaa", RegexOptions.None, 0, 11, true, "zabcaaaaaaa");
                yield return (@"(...)(?(1)\w*)[a1 ]", "zabcaaaaaaa", RegexOptions.RightToLeft, 0, 11, true, "aaaa");
                yield return (@"(...)(?(1)\w*)[a1 ]", "           ", RegexOptions.None, 0, 11, true, "    ");
                yield return (@"(...)(?(1)\w*)[a1 ]", "           ", RegexOptions.RightToLeft, 0, 11, true, "    ");
                yield return (@"(...)(?(1)\w*|\s*)[a1 ]", "zabcaaaaaaa", RegexOptions.None, 0, 11, true, "zabcaaaaaaa");
                yield return (@"(...)(?(1)\w*|\s*)[a1 ]", "----       ", RegexOptions.None, 0, 11, true, "--- ");
                yield return (@"(...)(?(1)\w*|\s*)[a1 ]", "zabcaaaaaaa", RegexOptions.RightToLeft, 0, 11, true, "aaaa");
                yield return (@"(...)(?(1)\w*|\s*)[a1 ]", "----       ", RegexOptions.RightToLeft, 0, 11, true, "---       ");
            }

            // Character Class Substraction

            // No Negation
            yield return ("[abcd-[abcd]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return ("[1234-[1234]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // All Negation
            yield return ("[^abcd-[^abcd]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return ("[^1234-[^1234]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // No Negation
            yield return ("[a-z-[a-z]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return ("[0-9-[0-9]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // All Negation
            yield return ("[^a-z-[^a-z]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return ("[^0-9-[^0-9]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // No Negation
            yield return (@"[\w-[\w]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\W-[\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\s-[\s]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\S-[\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\d-[\d]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\D-[\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // All Negation
            yield return (@"[^\w-[^\w]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\W-[^\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\s-[^\s]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\S-[^\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\d-[^\d]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\D-[^\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // MixedNegation
            yield return (@"[^\w-[\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\w-[^\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\s-[\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\s-[^\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\d-[\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\d-[^\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // No Negation
            yield return (@"[\p{Ll}-[\p{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\P{Ll}-[\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\p{Lu}-[\p{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\P{Lu}-[\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\p{Nd}-[\p{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\P{Nd}-[\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            // All Negation
            yield return (@"[^\p{Ll}-[^\p{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\P{Ll}-[^\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\p{Lu}-[^\p{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\P{Lu}-[^\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\p{Nd}-[^\p{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\P{Nd}-[^\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\p{IsGreek}\p{IsGreekExtended}]", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, true, "a");

            // MixedNegation
            yield return (@"[^\p{Ll}-[\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\p{Ll}-[^\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\p{Lu}-[\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\p{Lu}-[^\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[^\p{Nd}-[\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);
            yield return (@"[\p{Nd}-[^\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty);

            yield return (@"[ab\-\[cd-[-[]]]]", "[]]", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"[ab\-\[cd-[-[]]]]", "-]]", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"[ab\-\[cd-[-[]]]]", "`]]", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"[ab\-\[cd-[-[]]]]", "e]]", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"[ab\-\[cd-[[]]]]", "']]", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"[ab\-\[cd-[[]]]]", "e]]", RegexOptions.None, 0, 3, false, string.Empty);
            yield return (@"[a-[a-f]]", "abcdefghijklmnopqrstuvwxyz", RegexOptions.None, 0, 26, false, string.Empty);

            // \c
            if (!PlatformDetection.IsNetFramework) // missing fix for https://github.com/dotnet/runtime/issues/24759
            {
                yield return (@"(cat)(\c[*)(dog)", "asdlkcat\u00FFdogiwod", RegexOptions.None, 0, 15, false, string.Empty);
            }

            // Surrogate pairs split up into UTF-16 code units.
            yield return (@"(\uD82F[\uDCA0-\uDCA3])", "\uD82F\uDCA2", RegexOptions.CultureInvariant, 0, 2, true, "\uD82F\uDCA2");

            // Unicode text
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.RightToLeft, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })
            {
                if (engine != RegexEngine.NonBacktracking || options != RegexOptions.RightToLeft)
                {
                    yield return ("\u05D0\u05D1\u05D2\u05D3(\u05D4\u05D5|\u05D6\u05D7|\u05D8)", "abc\u05D0\u05D1\u05D2\u05D3\u05D4\u05D5def", options, 3, 6, true, "\u05D0\u05D1\u05D2\u05D3\u05D4\u05D5");
                    yield return ("\u05D0(\u05D4\u05D5|\u05D6\u05D7|\u05D8)", "\u05D0\u05D8", options, 0, 2, true, "\u05D0\u05D8");
                    yield return ("\u05D0(?:\u05D1|\u05D2|\u05D3)", "\u05D0\u05D2", options, 0, 2, true, "\u05D0\u05D2");
                    yield return ("\u05D0(?:\u05D1|\u05D2|\u05D3)", "\u05D0\u05D4", options, 0, 0, false, "");
                }
            }

            // .* : Case sensitive
            yield return (@".*\nfoo", "This shouldn't match", RegexOptions.None, 0, 20, false, "");
            yield return (@"a.*\nfoo", "This shouldn't match", RegexOptions.None, 0, 20, false, "");
            yield return (@".*\nFoo", $"\nFooThis should match", RegexOptions.None, 0, 21, true, "\nFoo");
            yield return (@".*\nfoo", "\nfooThis should match", RegexOptions.None, 4, 17, false, "");

            yield return (@".*?\nfoo", "This shouldn't match", RegexOptions.None, 0, 20, false, "");
            yield return (@"a.*?\nfoo", "This shouldn't match", RegexOptions.None, 0, 20, false, "");
            yield return (@".*?\nFoo", $"\nFooThis should match", RegexOptions.None, 0, 21, true, "\nFoo");
            yield return (@".*?\nfoo", "\nfooThis should match", RegexOptions.None, 4, 17, false, "");

            yield return (@".*\dfoo", "This shouldn't match", RegexOptions.None, 0, 20, false, "");
            yield return (@".*\dFoo", "This1Foo should match", RegexOptions.None, 0, 21, true, "This1Foo");
            yield return (@".*\dFoo", "This1foo should 2Foo match", RegexOptions.None, 0, 26, true, "This1foo should 2Foo");
            yield return (@".*\dFoo", "This1foo shouldn't 2foo match", RegexOptions.None, 0, 29, false, "");
            yield return (@".*\dfoo", "This1foo shouldn't 2foo match", RegexOptions.None, 24, 5, false, "");

            yield return (@".*?\dfoo", "This shouldn't match", RegexOptions.None, 0, 20, false, "");
            yield return (@".*?\dFoo", "This1Foo should match", RegexOptions.None, 0, 21, true, "This1Foo");
            yield return (@".*?\dFoo", "This1foo should 2Foo match", RegexOptions.None, 0, 26, true, "This1foo should 2Foo");
            yield return (@".*?\dFoo", "This1foo shouldn't 2foo match", RegexOptions.None, 0, 29, false, "");
            yield return (@".*?\dfoo", "This1foo shouldn't 2foo match", RegexOptions.None, 24, 5, false, "");

            yield return (@".*\dfoo", "1fooThis1foo should 1foo match", RegexOptions.None, 4, 9, true, "This1foo");
            yield return (@".*\dfoo", "This shouldn't match 1foo", RegexOptions.None, 0, 20, false, "");

            yield return (@".*?\dfoo", "1fooThis1foo should 1foo match", RegexOptions.None, 4, 9, true, "This1foo");
            yield return (@".*?\dfoo", "This shouldn't match 1foo", RegexOptions.None, 0, 20, false, "");

            // Turkish case sensitivity
            yield return (@"[\u0120-\u0130]", "\u0130", RegexOptions.None, 0, 1, true, "\u0130");

            // .* : Case insensitive
            yield return (@".*\nFoo", "\nfooThis should match", RegexOptions.IgnoreCase, 0, 21, true, "\nfoo");
            yield return (@".*\dFoo", "This1foo should match", RegexOptions.IgnoreCase, 0, 21, true, "This1foo");
            yield return (@".*\dFoo", "This1foo should 2FoO match", RegexOptions.IgnoreCase, 0, 26, true, "This1foo should 2FoO");
            yield return (@".*\dFoo", "This1Foo should 2fOo match", RegexOptions.IgnoreCase, 0, 26, true, "This1Foo should 2fOo");
            yield return (@".*\dfoo", "1fooThis1FOO should 1foo match", RegexOptions.IgnoreCase, 4, 9, true, "This1FOO");

            yield return (@".*?\nFoo", "\nfooThis should match", RegexOptions.IgnoreCase, 0, 21, true, "\nfoo");
            yield return (@".*?\dFoo", "This1foo should match", RegexOptions.IgnoreCase, 0, 21, true, "This1foo");
            yield return (@".*?\dFoo", "This1foo should 2FoO match", RegexOptions.IgnoreCase, 0, 26, true, "This1foo");
            yield return (@".*?\dFoo", "This1Foo should 2fOo match", RegexOptions.IgnoreCase, 0, 26, true, "This1Foo");
            yield return (@".*?\dFo{2}", "This1foo should 2FoO match", RegexOptions.IgnoreCase, 0, 26, true, "This1foo");
            yield return (@".*?\dFo{2}", "This1Foo should 2fOo match", RegexOptions.IgnoreCase, 0, 26, true, "This1Foo");
            yield return (@".*?\dfoo", "1fooThis1FOO should 1foo match", RegexOptions.IgnoreCase, 4, 9, true, "This1FOO");

            // Earliest match, not match with earliest end
            yield return (@".{5}Foo|Bar", "FooBarFoo", RegexOptions.None, 1, 8, true, "ooBarFoo");

            if (!RegexHelpers.IsNonBacktracking(engine))
            {
                // RightToLeft
                yield return (@"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 0, 32, true, "foo4567890");
                yield return (@"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 22, true, "foo4567890");
                yield return (@"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 4, true, "foo4");
                yield return (@"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 3, false, string.Empty);
                yield return (@"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 11, 21, false, string.Empty);

                yield return (@"foo\d+?", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 0, 32, true, "foo4567890");
                yield return (@"foo\d+?", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 22, true, "foo4567890");
                yield return (@"foo\d+?", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 4, true, "foo4");
                yield return (@"foo\d+?", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 3, false, string.Empty);
                yield return (@"foo\d+?", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 11, 21, false, string.Empty);

                yield return (@"\s+\d+", "sdf 12sad", RegexOptions.RightToLeft, 0, 9, true, " 12");
                yield return (@"\s+\d+", " asdf12 ", RegexOptions.RightToLeft, 0, 6, false, string.Empty);
                yield return ("aaa", "aaabbb", RegexOptions.None, 3, 3, false, string.Empty);
                yield return ("abc|def", "123def456", RegexOptions.RightToLeft | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 9, true, "def");

                // .* : RTL, Case-sensitive
                yield return (@".*\nfoo", "This shouldn't match", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");
                yield return (@".*\nfoo", "This should matchfoo\n", RegexOptions.None | RegexOptions.RightToLeft, 4, 13, false, "");
                yield return (@"a.*\nfoo", "This shouldn't match", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");
                yield return (@".*\nFoo", $"This should match\nFoo", RegexOptions.None | RegexOptions.RightToLeft, 0, 21, true, "This should match\nFoo");

                yield return (@".*?\nfoo", "This shouldn't match", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");
                yield return (@".*?\nfoo", "This should matchfoo\n", RegexOptions.None | RegexOptions.RightToLeft, 4, 13, false, "");
                yield return (@"a.*?\nfoo", "This shouldn't match", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");
                yield return (@".*?\nFoo", $"This should match\nFoo", RegexOptions.None | RegexOptions.RightToLeft, 0, 21, true, "\nFoo");

                yield return (@".*\dfoo", "This shouldn't match", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");
                yield return (@".*\dFoo", "This1Foo should match", RegexOptions.None | RegexOptions.RightToLeft, 0, 21, true, "This1Foo");
                yield return (@".*\dFoo", "This1foo should 2Foo match", RegexOptions.None | RegexOptions.RightToLeft, 0, 26, true, "This1foo should 2Foo");
                yield return (@".*\dFoo", "This1foo shouldn't 2foo match", RegexOptions.None | RegexOptions.RightToLeft, 0, 29, false, "");
                yield return (@".*\dfoo", "This1foo shouldn't 2foo match", RegexOptions.None | RegexOptions.RightToLeft, 19, 0, false, "");

                yield return (@".*?\dfoo", "This shouldn't match", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");
                yield return (@".*?\dFoo", "This1Foo should match", RegexOptions.None | RegexOptions.RightToLeft, 0, 21, true, "1Foo");
                yield return (@".*?\dFoo", "This1foo should 2Foo match", RegexOptions.None | RegexOptions.RightToLeft, 0, 26, true, "2Foo");
                yield return (@".*?\dFoo", "This1foo shouldn't 2foo match", RegexOptions.None | RegexOptions.RightToLeft, 0, 29, false, "");
                yield return (@".*?\dfoo", "This1foo shouldn't 2foo match", RegexOptions.None | RegexOptions.RightToLeft, 19, 0, false, "");

                yield return (@".*\dfoo", "1fooThis2foo should 1foo match", RegexOptions.None | RegexOptions.RightToLeft, 8, 4, true, "2foo");
                yield return (@".*\dfoo", "This shouldn't match 1foo", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");

                yield return (@".*?\dfoo", "1fooThis2foo should 1foo match", RegexOptions.None | RegexOptions.RightToLeft, 8, 4, true, "2foo");
                yield return (@".*?\dfoo", "This shouldn't match 1foo", RegexOptions.None | RegexOptions.RightToLeft, 0, 20, false, "");

                // .* : RTL, case insensitive
                yield return (@".*\nFoo", "\nfooThis should match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 21, true, "\nfoo");
                yield return (@".*\dFoo", "This1foo should match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 21, true, "This1foo");
                yield return (@".*\dFoo", "This1foo should 2FoO match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 26, true, "This1foo should 2FoO");
                yield return (@".*\dFoo", "This1Foo should 2fOo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 26, true, "This1Foo should 2fOo");
                yield return (@".*\dfoo", "1fooThis2FOO should 1foo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 8, 4, true, "2FOO");
                yield return (@"[\w\s].*", "1fooThis2FOO should 1foo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 30, true, "1fooThis2FOO should 1foo match");
                yield return (@"i.*", "1fooThis2FOO should 1foo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 30, true, "is2FOO should 1foo match");

                yield return (@".*?\nFoo", "\nfooThis should match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 21, true, "\nfoo");
                yield return (@".*?\dFoo", "This1foo should match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 21, true, "1foo");
                yield return (@".*?\dFoo", "This1foo should 2FoO match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 26, true, "2FoO");
                yield return (@".*?\dFoo", "This1Foo should 2fOo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 26, true, "2fOo");
                yield return (@".*?\dfoo", "1fooThis2FOO should 1foo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 8, 4, true, "2FOO");
                yield return (@"[\w\s].*?", "1fooThis2FOO should 1foo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 30, true, "h");
                yield return (@"i.*?", "1fooThis2FOO should 1foo match", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, 0, 30, true, "is2FOO should 1foo match");
            }

            if (PlatformDetection.IsNetCore)
            {
                // Unicode symbols in character ranges.
                yield return (@"^(?i:[\u00D7-\u00D8])$", '\u00F7'.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "");
                yield return (@"^(?i:[\u00C0-\u00DE])$", '\u00F7'.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "");
                yield return (@"^(?i:[\u00C0-\u00DE])$", ((char)('\u00C0' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u00C0' + 32)).ToString());
                yield return (@"^(?i:[\u00C0-\u00DE])$", ((char)('\u00DE' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u00DE' + 32)).ToString());
                yield return (@"^(?i:[\u0391-\u03AB])$", ((char)('\u03A2' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "");
                yield return (@"^(?i:[\u0391-\u03AB])$", ((char)('\u0391' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u0391' + 32)).ToString());
                yield return (@"^(?i:[\u0391-\u03AB])$", ((char)('\u03AB' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u03AB' + 32)).ToString());
                yield return (@"^(?i:[\u1F18-\u1F1F])$", ((char)('\u1F1F' - 8)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "");
                yield return (@"^(?i:[\u1F18-\u1F1F])$", ((char)('\u1F18' - 8)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u1F18' - 8)).ToString());
                yield return (@"^(?i:[\u10A0-\u10C5])$", ((char)('\u10A0' + 7264)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u10A0' + 7264)).ToString());
                yield return (@"^(?i:[\u10A0-\u10C5])$", ((char)('\u1F1F' + 48)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "");
                yield return (@"^(?i:[\u24B6-\u24D0])$", ((char)('\u24D0' + 26)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "");
                yield return (@"^(?i:[\u24B6-\u24D0])$", ((char)('\u24CF' + 26)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u24CF' + 26)).ToString());
            }

            // Long inputs
            string longCharacterRange = string.Concat(Enumerable.Range(1, 0x2000).Select(c => (char)c));
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.IgnoreCase })
            {
                yield return ("\u1000", longCharacterRange, options, 0, 0x2000, true, "\u1000");
                yield return ("[\u1000-\u1001]", longCharacterRange, options, 0, 0x2000, true, "\u1000");
                yield return ("[\u0FF0-\u0FFF][\u1000-\u1001]", longCharacterRange, options, 0, 0x2000, true, "\u0FFF\u1000");

                yield return ("\uA640", longCharacterRange, options, 0, 0x2000, false, "");
                yield return ("[\u3000-\u3001]", longCharacterRange, options, 0, 0x2000, false, "");
                yield return ("[\uA640-\uA641][\u3000-\u3010]", longCharacterRange, options, 0, 0x2000, false, "");

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    yield return ("\u1000", longCharacterRange, options | RegexOptions.RightToLeft, 0, 0x2000, true, "\u1000");
                    yield return ("[\u1000-\u1001]", longCharacterRange, options | RegexOptions.RightToLeft, 0, 0x2000, true, "\u1001");
                    yield return ("[\u1000][\u1001-\u1010]", longCharacterRange, options, 0, 0x2000, true, "\u1000\u1001");

                    yield return ("\uA640", longCharacterRange, options | RegexOptions.RightToLeft, 0, 0x2000, false, "");
                    yield return ("[\u3000-\u3001][\uA640-\uA641]", longCharacterRange, options | RegexOptions.RightToLeft, 0, 0x2000, false, "");
                }
            }

            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Singleline })
            {
                yield return (@"\W.*?\D", "seq 012 of 3 digits", options, 0, 19, true, " 012 ");
                yield return (@"\W.+?\D", "seq 012 of 3 digits", options, 0, 19, true, " 012 ");
                yield return (@"\W.{1,7}?\D", "seq 012 of 3 digits", options, 0, 19, true, " 012 ");
                yield return (@"\W.{1,2}?\D", "seq 012 of 3 digits", options, 0, 19, true, " of");
                yield return (@"\W.*?\b", "digits:0123456789", options, 0, 17, true, ":");
                yield return (@"\B.*?\B", "e.g:abc", options, 0, 7, true, "");
                yield return (@"\B\W+?", "e.g:abc", options, 0, 7, false, "");
                yield return (@"\B\W*?", "e.g:abc", options, 0, 7, true, "");

                // While not lazy loops themselves, variants of the prior case that should give same results here
                yield return (@"\B\W*", "e.g:abc", options, 0, 7, true, "");
                yield return (@"\B\W?", "e.g:abc", options, 0, 7, true, "");

                //mixed lazy and eager counting
                yield return ("z(a{0,5}|a{0,10}?)", "xyzaaaaaaaaaxyz", options, 0, 15, true, "zaaaaa");
            }
        }

        [OuterLoop("Takes several seconds to run")]
        [Fact]
        public async Task AllEnginesProduceTheSameCaptures()
        {
            // Some of the match test cases have captures.  For those that do, validate the captures are the same across engines.
            (string Pattern, string Input, RegexOptions Options, int Beginning, int Length, bool ExpectedSuccess, string ExpectedValue)[] interpreterTests =
                Match_MemberData_Cases(RegexEngine.Interpreter)
                .Where(i => i.ExpectedSuccess)
                .ToArray();

            (RegexEngine e, HashSet<(string Pattern, string Input, RegexOptions Options, int Beginning, int Length, bool ExpectedSuccess, string ExpectedValue)>)[] otherTests =
                RegexHelpers.AvailableEngines
                .Where(e => e != RegexEngine.Interpreter)
                .Select(e => (e, Match_MemberData_Cases(e).ToHashSet())).ToArray();

            foreach ((string Pattern, string Input, RegexOptions Options, int Beginning, int Length, bool ExpectedSuccess, string ExpectedValue) test in interpreterTests)
            {
                Regex interpreterRegex = await RegexHelpers.GetRegexAsync(RegexEngine.Interpreter, test.Pattern, test.Options);
                if (interpreterRegex.GetGroupNumbers().Length > 1)
                {
                    Match expectedMatch = interpreterRegex.Match(test.Input, test.Beginning, test.Length);

                    foreach ((RegexEngine, HashSet<(string Pattern, string Input, RegexOptions Options, int Beginning, int Length, bool ExpectedSuccess, string ExpectedValue)>) other in otherTests)
                    {
                        if (other.Item2.Contains(test))
                        {
                            Regex otherRegex = await RegexHelpers.GetRegexAsync(other.Item1, test.Pattern, test.Options);
                            Assert.Equal(interpreterRegex.GetGroupNames(), otherRegex.GetGroupNames());
                            Assert.Equal(interpreterRegex.GetGroupNumbers(), otherRegex.GetGroupNumbers());

                            Match otherMatch = otherRegex.Match(test.Input, test.Beginning, test.Length);
                            for (int i = 0; i < expectedMatch.Groups.Count; i++)
                            {
                                Assert.Equal(expectedMatch.Groups[i].Value, otherMatch.Groups[i].Value);

                                // NonBacktracking currently only stores the last capture in a group.
                                if (!RegexHelpers.IsNonBacktracking(other.Item1) ||
                                    expectedMatch.Groups[i].Captures.Count == 1)
                                {
                                    for (int j = 0; j < expectedMatch.Groups[i].Captures.Count; j++)
                                    {
                                        Assert.Equal(expectedMatch.Groups[i].Captures[j].Value, otherMatch.Groups[i].Captures[j].Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Match_MemberData))]
        public void Match(RegexEngine engine, string pattern, string input, RegexOptions options, Regex r, int beginning, int length, bool expectedSuccess, string expectedValue)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, beginning);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, length);

            // Test instance method overloads
            if (isDefaultStart && isDefaultCount)
            {
                VerifyMatch(r.Match(input));
                VerifyIsMatch(r, input, expectedSuccess, Regex.InfiniteMatchTimeout);
            }
            if (beginning + length == input.Length && (options & RegexOptions.RightToLeft) == 0)
            {
                VerifyMatch(r.Match(input, beginning));
            }
            VerifyMatch(r.Match(input, beginning, length));

            // Test static method overloads
            if (isDefaultStart && isDefaultCount)
            {
                switch (engine)
                {
                    case RegexEngine.Interpreter:
                    case RegexEngine.Compiled:
                    case RegexEngine.NonBacktracking:
                        VerifyMatch(Regex.Match(input, pattern, options | RegexHelpers.OptionsFromEngine(engine)));
                        VerifyIsMatch(null, input, expectedSuccess, Regex.InfiniteMatchTimeout, pattern, options | RegexHelpers.OptionsFromEngine(engine));
                        break;
                }
            }

            void VerifyMatch(Match match)
            {
                Assert.Equal(expectedSuccess, match.Success);
                RegexAssert.Equal(expectedValue, match);

                // Groups can never be empty
                Assert.True(match.Groups.Count >= 1);
                Assert.Equal(expectedSuccess, match.Groups[0].Success);
                RegexAssert.Equal(expectedValue, match.Groups[0]);
            }
        }

        private async Task CreateAndMatch(RegexEngine engine, string pattern, string input, RegexOptions options, int beginning, int length, bool expectedSuccess, string expectedValue)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);
            Match(engine, pattern, input, options, r, beginning, length, expectedSuccess, expectedValue);
        }

        public static IEnumerable<object[]> Match_VaryingLengthStrings_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                foreach (int length in new[] { 2, 3, 7, 8, 9, 64 })
                {
                    yield return new object[] { engine, RegexOptions.None, length };
                    yield return new object[] { engine, RegexOptions.IgnoreCase, length };
                    yield return new object[] { engine, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, length };
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Takes several minutes on .NET Framework")]
        [Theory]
        [MemberData(nameof(Match_VaryingLengthStrings_MemberData))]
        public async Task Match_VaryingLengthStrings(RegexEngine engine, RegexOptions options, int length)
        {
            bool caseInsensitive = (options & RegexOptions.IgnoreCase) != 0;
            string pattern = "[123]" + string.Concat(Enumerable.Range(0, length).Select(i => (char)('A' + (i % 26))));
            string input = "2" + string.Concat(Enumerable.Range(0, length).Select(i => (char)((caseInsensitive ? 'a' : 'A') + (i % 26))));
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);
            Match(engine, pattern, input, options, r, 0, input.Length, expectedSuccess: true, expectedValue: input);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Takes several minutes on .NET Framework")]
        [OuterLoop("Takes several seconds")]
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Match_VaryingLengthStrings_Huge(RegexEngine engine)
        {
            Func<string, Task> func = static async engineStr =>
            {
                RegexEngine engine = (RegexEngine)Enum.Parse(typeof(RegexEngine), engineStr);

                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    RegexHelpers.SetSafeSizeThreshold(100_002);
                }

                try
                {
                    await new RegexMatchTests().Match_VaryingLengthStrings(engine, RegexOptions.None, 100_000);
                }
                finally
                {
                    if (RegexHelpers.IsNonBacktracking(engine))
                    {
                        RegexHelpers.RestoreSafeSizeThresholdToDefault();
                    }
                }
            };

            if (RegexHelpers.IsNonBacktracking(engine))
            {
                RemoteExecutor.Invoke(func, engine.ToString()).Dispose();
            }
            else
            {
                await func(engine.ToString());
            }
        }

        public static IEnumerable<object[]> Match_DeepNesting_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    // expression uses atomic group
                    continue;
                }

                yield return new object[] { engine, 1 };
                yield return new object[] { engine, 10 };
                yield return new object[] { engine, 100 };
            }
        }

        [Theory]
        [MemberData(nameof(Match_DeepNesting_MemberData))]
        public async void Match_DeepNesting(RegexEngine engine, int count)
        {
            const string Start = @"((?>abc|(?:def[ghi]", End = @")))";
            const string Match = "defg";

            string pattern = string.Concat(Enumerable.Repeat(Start, count)) + string.Concat(Enumerable.Repeat(End, count));
            string input = string.Concat(Enumerable.Repeat(Match, count));

            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern);
            Match m = r.Match(input);

            Assert.True(m.Success);
            RegexAssert.Equal(input, m);
            Assert.Equal(count + 1, m.Groups.Count);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Match_Timeout(RegexEngine engine)
        {
            Regex regex = await RegexHelpers.GetRegexAsync(engine, @"\p{Lu}", RegexOptions.IgnoreCase, TimeSpan.FromHours(1));
            Match match = regex.Match("abc");
            Assert.True(match.Success);
            RegexAssert.Equal("a", match);
        }

        /// <summary>
        /// Test that timeout exception is being thrown.
        /// </summary>
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        private async Task Match_TestThatTimeoutHappens(RegexEngine engine)
        {
            var rnd = new Random(42);
            var chars = new char[1_000_000];
            for (int i = 0; i < chars.Length; i++)
            {
                byte b = (byte)rnd.Next(0, 256);
                chars[i] = b < 200 ? 'a' : (char)b;
            }
            string input = new string(chars);

            Regex re = await RegexHelpers.GetRegexAsync(engine, @"a.{20}^", RegexOptions.None, TimeSpan.FromMilliseconds(10));
            Assert.Throws<RegexMatchTimeoutException>(() => { re.Match(input); });
        }

        public static IEnumerable<object[]> Match_Timeout_Throws_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    // NonBacktracking provides O(n) matching performance and thus doesn't have all
                    // the timeout checks the backtracking engines require.
                    continue;
                }

                string a50 = new string('a', 50);
                string a64 = new string('a', 64);
                string a100 = new string('a', 100);
                string a1_000_000 = new string('a', 1_000_000);
                string b50 = new string('b', 50);

                // These aren't catastrophic backtracking, but they will result in O(M*N) work, evaluating
                // each lookaround at every iteration of the loop and without any backtracking.  We still
                // want timeouts here at least every O(N) work.

                // Lookarounds
                yield return new object[] { engine, @"((?=(?>a*))a)+", a1_000_000 };
                yield return new object[] { engine, @"((?<=(?>a*))a)+", a1_000_000 };
                yield return new object[] { engine, @"((?!(?>[^a]*))a)+", a1_000_000 };
                yield return new object[] { engine, @"((?<!(?>[^a]*))a)+", a1_000_000 };

                // All of the below tests have catastrophic backtracking...

                // Loops
                foreach (string lazyInner in new[] { "", "?" })
                {
                    foreach (string lazyOuter in new[] { "", "?" })
                    {
                        // Loop around a single-char loop
                        yield return new object[] { engine, @$"(a+{lazyInner})+{lazyOuter}$", $"{a50}b" };
                        yield return new object[] { engine, @$"([^a]+{lazyInner})+{lazyOuter}$", $"{b50}a" };
                        yield return new object[] { engine, @$"(\w+{lazyInner})+{lazyOuter}$", $"{a50}!" };

                        // Loop around a loop (w/ and w/out inner capture)
                        yield return new object[] { engine, @$"((?:aa)+{lazyInner})+{lazyOuter}$", $"{a100}b" };
                        yield return new object[] { engine, @$"((aa)+{lazyInner})+{lazyOuter}$", $"{a100}b" };
                    }
                }

                // Excessive alternations
                yield return new object[] { engine, string.Concat(Enumerable.Repeat(@"(?:a||\w)", 64)) + "$", $"{a64}b" };
            }
        }

        [Theory]
        [MemberData(nameof(Match_Timeout_Throws_MemberData))]
        public async Task Match_Timeout_Throws(RegexEngine engine, string pattern, string input)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(1));
            Assert.Throws<RegexMatchTimeoutException>(() => r.Match(input));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/67886", ~RuntimeConfiguration.Release)]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/67886", RuntimeTestModes.JitMinOpts)]
        public void Match_InstanceMethods_DefaultTimeout_Throws(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // Test relies on backtracking triggering timeout checks
                return;
            }

            RemoteExecutor.Invoke(async engineString =>
            {
                RegexEngine engine = (RegexEngine)int.Parse(engineString, CultureInfo.InvariantCulture);

                const string Pattern = @"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@(([0-9a-zA-Z])+([-\w]*[0-9a-zA-Z])*\.)+[a-zA-Z]{2,9})$";
                string input = new string('a', 50) + "@a.a";

                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.FromMilliseconds(100));

                Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);
                Assert.Throws<RegexMatchTimeoutException>(() => r.Match(input));
                Assert.Throws<RegexMatchTimeoutException>(() => r.IsMatch(input));
                Assert.Throws<RegexMatchTimeoutException>(() => r.Matches(input).Count);

            }, ((int)engine).ToString(CultureInfo.InvariantCulture)).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Match_StaticMethods_DefaultTimeout_Throws(RegexOptions options)
        {
            RemoteExecutor.Invoke(optionsString =>
            {
                RegexOptions options = (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture);

                const string Pattern = @"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@(([0-9a-zA-Z])+([-\w]*[0-9a-zA-Z])*\.)+[a-zA-Z]{2,9})$";
                string input = new string('a', 50) + "@a.a";

                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.FromMilliseconds(100));

                if (options == RegexOptions.None)
                {
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.Match(input, Pattern));
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.IsMatch(input, Pattern));
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.Matches(input, Pattern).Count);
                }

                Assert.Throws<RegexMatchTimeoutException>(() => Regex.Match(input, Pattern, options));
                Assert.Throws<RegexMatchTimeoutException>(() => Regex.IsMatch(input, Pattern, options));
                Assert.Throws<RegexMatchTimeoutException>(() => Regex.Matches(input, Pattern, options).Count);

            }, ((int)options).ToString(CultureInfo.InvariantCulture)).Dispose();
        }

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Match_CachedPattern_NewTimeoutApplies(RegexOptions options)
        {
            const string PatternLeadingToLotsOfBacktracking = @"^(\w+\s?)*$";
            VerifyIsMatch(null, "", true, TimeSpan.FromDays(1), PatternLeadingToLotsOfBacktracking, options);
            var sw = Stopwatch.StartNew();
            VerifyIsMatchThrows<RegexMatchTimeoutException>(null, "An input string that takes a very very very very very very very very very very very long time!", TimeSpan.FromMilliseconds(1), PatternLeadingToLotsOfBacktracking, options);
            Assert.InRange(sw.Elapsed.TotalSeconds, 0, 10); // arbitrary upper bound that should be well above what's needed with a 1ms timeout
        }

        public static IEnumerable<object[]> Match_Advanced_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                // \B special character escape: ".*\\B(SUCCESS)\\B.*"
                yield return new object[]
                {
                    engine,
                    @".*\B(SUCCESS)\B.*", "adfadsfSUCCESSadsfadsf", RegexOptions.None, 0, 22,
                    new CaptureData[]
                    {
                        new CaptureData("adfadsfSUCCESSadsfadsf", 0, 22),
                        new CaptureData("SUCCESS", 7, 7)
                    }
                };

                // Using |, (), ^, $, .: Actual - "^aaa(bb.+)(d|c)$"
                yield return new object[]
                {
                    engine,
                    "^aaa(bb.+)(d|c)$", "aaabb.cc", RegexOptions.None, 0, 8,
                    new CaptureData[]
                    {
                        new CaptureData("aaabb.cc", 0, 8),
                        new CaptureData("bb.c", 3, 4),
                        new CaptureData("c", 7, 1)
                    }
                };

                // Using greedy quantifiers: Actual - "(a+)(b*)(c?)"
                yield return new object[]
                {
                    engine,
                    "(a+)(b*)(c?)", "aaabbbccc", RegexOptions.None, 0, 9,
                    new CaptureData[]
                    {
                        new CaptureData("aaabbbc", 0, 7),
                        new CaptureData("aaa", 0, 3),
                        new CaptureData("bbb", 3, 3),
                        new CaptureData("c", 6, 1)
                    }
                };

                // Using lazy quantifiers: Actual - "(d+?)(e*?)(f??)"
                // Interesting match from this pattern and input. If needed to go to the end of the string change the ? to + in the last lazy quantifier
                yield return new object[]
                {
                    engine,
                    "(d+?)(e*?)(f??)", "dddeeefff", RegexOptions.None, 0, 9,
                    new CaptureData[]
                    {
                        new CaptureData("d", 0, 1),
                        new CaptureData("d", 0, 1),
                        new CaptureData(string.Empty, 1, 0),
                        new CaptureData(string.Empty, 1, 0)
                    }
                };
                yield return new object[]
                {
                    engine,
                    "(d+?)(e*?)(f+)", "dddeeefff", RegexOptions.None, 0, 9,
                    new CaptureData[]
                    {
                        new CaptureData("dddeeefff", 0, 9),
                        new CaptureData("ddd", 0, 3),
                        new CaptureData("eee", 3, 3),
                        new CaptureData("fff", 6, 3),
                    }
                };

                // Noncapturing group : Actual - "(a+)(?:b*)(ccc)"
                yield return new object[]
                {
                    engine,
                    "(a+)(?:b*)(ccc)", "aaabbbccc", RegexOptions.None, 0, 9,
                    new CaptureData[]
                    {
                        new CaptureData("aaabbbccc", 0, 9),
                        new CaptureData("aaa", 0, 3),
                        new CaptureData("ccc", 6, 3),
                    }
                };

                // Alternation constructs: Actual - "(111|aaa)"
                yield return new object[]
                {
                    engine,
                    "(111|aaa)", "aaa", RegexOptions.None, 0, 3,
                    new CaptureData[]
                    {
                        new CaptureData("aaa", 0, 3),
                        new CaptureData("aaa", 0, 3)
                    }
                };

                // Using "n" Regex option. Only explicitly named groups should be captured: Actual - "([0-9]*)\\s(?<s>[a-z_A-Z]+)", "n"
                yield return new object[]
                {
                    engine,
                    @"([0-9]*)\s(?<s>[a-z_A-Z]+)", "200 dollars", RegexOptions.ExplicitCapture, 0, 11,
                    new CaptureData[]
                    {
                        new CaptureData("200 dollars", 0, 11),
                        new CaptureData("dollars", 4, 7)
                    }
                };

                // Single line mode "s". Includes new line character: Actual - "([^/]+)","s"
                yield return new object[]
                {
                    engine,
                    "(.*)", "abc\nsfc", RegexOptions.Singleline, 0, 7,
                    new CaptureData[]
                    {
                        new CaptureData("abc\nsfc", 0, 7),
                        new CaptureData("abc\nsfc", 0, 7),
                    }
                };

                // "([0-9]+(\\.[0-9]+){3})"
                yield return new object[]
                {
                    engine,
                    @"([0-9]+(\.[0-9]+){3})", "209.25.0.111", RegexOptions.None, 0, 12,
                    new CaptureData[]
                    {
                        new CaptureData("209.25.0.111", 0, 12),
                        new CaptureData("209.25.0.111", 0, 12),
                        new CaptureData(".111", 8, 4, new CaptureData[]
                        {
                            new CaptureData(".25", 3, 3),
                            new CaptureData(".0", 6, 2),
                            new CaptureData(".111", 8, 4),
                        }),
                    }
                };

                // Groups and captures
                yield return new object[]
                {
                    engine,
                    @"(?<A1>a*)(?<A2>b*)(?<A3>c*)", "aaabbccccccccccaaaabc", RegexOptions.None, 0, 21,
                    new CaptureData[]
                    {
                        new CaptureData("aaabbcccccccccc", 0, 15),
                        new CaptureData("aaa", 0, 3),
                        new CaptureData("bb", 3, 2),
                        new CaptureData("cccccccccc", 5, 10)
                    }
                };

                yield return new object[]
                {
                    engine,
                    @"(?<A1>A*)(?<A2>B*)(?<A3>C*)", "aaabbccccccccccaaaabc", RegexOptions.IgnoreCase, 0, 21,
                    new CaptureData[]
                    {
                        new CaptureData("aaabbcccccccccc", 0, 15),
                        new CaptureData("aaa", 0, 3),
                        new CaptureData("bb", 3, 2),
                        new CaptureData("cccccccccc", 5, 10)
                    }
                };

                // Using |, (), ^, $, .: Actual - "^aaa(bb.+)(d|c)$"
                yield return new object[]
                {
                    engine,
                    "^aaa(bb.+)(d|c)$", "aaabb.cc", RegexOptions.None, 0, 8,
                    new CaptureData[]
                    {
                        new CaptureData("aaabb.cc", 0, 8),
                        new CaptureData("bb.c", 3, 4),
                        new CaptureData("c", 7, 1)
                    }
                };

                // Actual - ".*\\b(\\w+)\\b"
                yield return new object[]
                {
                    engine,
                    @".*\b(\w+)\b", "XSP_TEST_FAILURE SUCCESS", RegexOptions.None, 0, 24,
                    new CaptureData[]
                    {
                        new CaptureData("XSP_TEST_FAILURE SUCCESS", 0, 24),
                        new CaptureData("SUCCESS", 17, 7)
                    }
                };

                // Multiline
                yield return new object[]
                {
                    engine,
                    "(line2$\n)line3", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                    new CaptureData[]
                    {
                        new CaptureData("line2\nline3", 6, 11),
                        new CaptureData("line2\n", 6, 6)
                    }
                };

                // Multiline
                yield return new object[]
                {
                    engine,
                    "(line2\n^)line3", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                    new CaptureData[]
                    {
                        new CaptureData("line2\nline3", 6, 11),
                        new CaptureData("line2\n", 6, 6)
                    }
                };

                // Multiline
                yield return new object[]
                {
                    engine,
                    "(line3\n$\n)line4", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                    new CaptureData[]
                    {
                        new CaptureData("line3\n\nline4", 12, 12),
                        new CaptureData("line3\n\n", 12, 7)
                    }
                };

                // Multiline
                yield return new object[]
                {
                    engine,
                    "(line3\n^\n)line4", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                    new CaptureData[]
                    {
                        new CaptureData("line3\n\nline4", 12, 12),
                        new CaptureData("line3\n\n", 12, 7)
                    }
                };

                // Multiline
                yield return new object[]
                {
                    engine,
                    "(line2$\n^)line3", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                    new CaptureData[]
                    {
                        new CaptureData("line2\nline3", 6, 11),
                        new CaptureData("line2\n", 6, 6)
                    }
                };

                // Validate captures after backtracking constructs are uncaptured when backtracking
                foreach (string lazy in new[] { "", "?" })
                {
                    yield return new object[]
                    {
                        engine,
                        $"^a+{lazy}(a)$", "aaaa", RegexOptions.None, 0, 4,
                        new CaptureData[]
                        {
                            new CaptureData("aaaa", 0, 4),
                            new CaptureData("a", 3, 1)
                        }
                    };

                    yield return new object[]
                    {
                        engine,
                        $"^(a)+{lazy}(a)$", "aaaa", RegexOptions.None, 0, 4,
                        new CaptureData[]
                        {
                            new CaptureData("aaaa", 0, 4),
                            new CaptureData("a", 2, 1, new CaptureData[]
                            {
                                new CaptureData("a", 0, 1),
                                new CaptureData("a", 1, 1),
                                new CaptureData("a", 2, 1),
                            }),
                            new CaptureData("a", 3, 1)
                        }
                    };
                }
                yield return new object[]
                {
                    engine,
                    $"^(|a)aa(a)$", "aaaa", RegexOptions.None, 0, 4,
                    new CaptureData[]
                    {
                        new CaptureData("aaaa", 0, 4),
                        new CaptureData("a", 0, 1),
                        new CaptureData("a", 3, 1)
                    }
                };

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    // Zero-width positive lookahead assertion: Actual - "abc(?=XXX)\\w+"
                    yield return new object[]
                    {
                        engine,
                        @"abc(?=XXX)\w+", "abcXXXdef", RegexOptions.None, 0, 9,
                        new CaptureData[]
                        {
                            new CaptureData("abcXXXdef", 0, 9)
                        }
                    };

                    // Backreferences : Actual - "(\\w)\\1"
                    yield return new object[]
                    {
                        engine,
                        @"(\w)\1", "aa", RegexOptions.None, 0, 2,
                        new CaptureData[]
                        {
                            new CaptureData("aa", 0, 2),
                            new CaptureData("a", 0, 1),
                        }
                    };

                    // Actual - "(?<1>\\d+)abc(?(1)222|111)"
                    yield return new object[]
                    {
                        engine,
                        @"(?<MyDigits>\d+)abc(?(MyDigits)222|111)", "111abc222", RegexOptions.None, 0, 9,
                        new CaptureData[]
                        {
                            new CaptureData("111abc222", 0, 9),
                            new CaptureData("111", 0, 3)
                        }
                    };

                    // RightToLeft
                    yield return new object[]
                    {
                        engine,
                        "aaa", "aaabbb", RegexOptions.RightToLeft, 3, 3,
                        new CaptureData[]
                        {
                            new CaptureData("aaa", 0, 3)
                        }
                    };

                    // RightToLeft with anchor
                    yield return new object[]
                    {
                        engine,
                        "^aaa", "aaabbb", RegexOptions.RightToLeft, 3, 3,
                        new CaptureData[]
                        {
                            new CaptureData("aaa", 0, 3)
                        }
                    };
                    yield return new object[]
                    {
                        engine,
                        "bbb$", "aaabbb", RegexOptions.RightToLeft, 0, 3,
                        new CaptureData[]
                        {
                            new CaptureData("bbb", 0, 3)
                        }
                    };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Match_Advanced_TestData))]
        public async Task Match_Advanced(RegexEngine engine, string pattern, string input, RegexOptions options, int beginning, int length, CaptureData[] expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, beginning);
            bool isDefaultCount = RegexHelpers.IsDefaultStart(input, options, length);

            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            if (isDefaultStart && isDefaultCount)
            {
                // Use Match(string) or Match(string, string, RegexOptions)
                VerifyMatch(r.Match(input));
                VerifyMatch(Regex.Match(input, pattern, options));

                VerifyIsMatch(null, input, true, Regex.InfiniteMatchTimeout, pattern, options);
            }

            if (beginning + length == input.Length)
            {
                // Use Match(string, int)
                VerifyMatch(r.Match(input, beginning));
            }

            if ((options & RegexOptions.RightToLeft) == 0)
            {
                // Use Match(string, int, int)
                VerifyMatch(r.Match(input, beginning, length));
            }

            void VerifyMatch(Match match)
            {
                Assert.True(match.Success);

                RegexAssert.Equal(expected[0].Value, match);
                Assert.Equal(expected[0].Index, match.Index);
                Assert.Equal(expected[0].Length, match.Length);

                Assert.Equal(1, match.Captures.Count);
                RegexAssert.Equal(expected[0].Value, match.Captures[0]);
                Assert.Equal(expected[0].Index, match.Captures[0].Index);
                Assert.Equal(expected[0].Length, match.Captures[0].Length);

                Assert.Equal(expected.Length, match.Groups.Count);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    Assert.True(match.Groups[i].Success);

                    RegexAssert.Equal(expected[i].Value, match.Groups[i]);
                    Assert.Equal(expected[i].Index, match.Groups[i].Index);
                    Assert.Equal(expected[i].Length, match.Groups[i].Length);

                    if (!RegexHelpers.IsNonBacktracking(engine))
                    {
                        Assert.Equal(expected[i].Captures.Length, match.Groups[i].Captures.Count);
                        for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                        {
                            RegexAssert.Equal(expected[i].Captures[j].Value, match.Groups[i].Captures[j]);
                            Assert.Equal(expected[i].Captures[j].Index, match.Groups[i].Captures[j].Index);
                            Assert.Equal(expected[i].Captures[j].Length, match.Groups[i].Captures[j].Length);
                        }
                    }
                    else
                    {
                        // NonBacktracking does not support multiple captures
                        Assert.Equal(1, match.Groups[i].Captures.Count);
                        int lastExpected = expected[i].Captures.Length - 1;
                        RegexAssert.Equal(expected[i].Captures[lastExpected].Value, match.Groups[i].Captures[0]);
                        Assert.Equal(expected[i].Captures[lastExpected].Index, match.Groups[i].Captures[0].Index);
                        Assert.Equal(expected[i].Captures[lastExpected].Length, match.Groups[i].Captures[0].Length);
                    }
                }
            }
        }

        public static IEnumerable<object[]> Match_StartatDiffersFromBeginning_MemberData()
        {
            // (RegexEngine engine, string pattern, string input, RegexOptions options,
            // int startat, bool expectedSuccessStartAt, bool expectedSuccessBeginning,
            // int expectedIndex)
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Singleline, RegexOptions.Multiline, RegexOptions.Singleline | RegexOptions.Multiline })
                {
                    // Anchors
                    yield return new object[] { engine, @"^.*", "abc", options, 0, 3, true, true, "abc", 0 };
                    yield return new object[] { engine, @"^.*", "abc", options, 1, 2, false, true, "bc", 1 };
                    yield return new object[] { engine, @"\Aabc\z", "1abc2", options, 1, 3, false, true, "abc", 1 };
                    yield return new object[] { engine, @"\Aabc\z", "1abc2", options, 1, 4, false, false, "", 0 };
                }

                // When matching with (string, int, int), an input substring is
                // used, so e.g, anchors such as ^ and \A will match at the start;
                // when matching with (string, int) the input outside the range is not ignored,
                // so e.g., anchors such as ^ and \A do not necessarily match at the start
                yield return new object[] { engine, @"^abc$", "1abc2", RegexOptions.None, 1, 3, false, true, "abc", 1 };
                yield return new object[] { engine, @"^abc2$", "1abc2", RegexOptions.None, 1, 4, false, true, "abc2", 1 };

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    // Positive and negative lookbehinds
                    yield return new object[] { engine, @"(?<=abc)def", "abcdef", RegexOptions.None, 3, 3, true, false, "def", 3 };
                    yield return new object[] { engine, @"(?<!abc)def", "abcdef", RegexOptions.None, 3, 3, false, true, "def", 3 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Match_StartatDiffersFromBeginning_MemberData))]
        public async Task Match_StartatDiffersFromBeginning(RegexEngine engine, string pattern, string input, RegexOptions options, int startat, int length, bool expectedSuccessStartAt, bool expectedSuccessBeginning, string expectedValue, int expectedIndex)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            Assert.Equal(expectedSuccessStartAt, r.IsMatch(input, startat));

            // Normal matching, but any match before startat is ignored.
            Match match = r.Match(input, startat);
            Assert.Equal(expectedSuccessStartAt, match.Success);
            Assert.Equal(expectedSuccessStartAt ? expectedValue : "", match.Value);
            Assert.Equal(expectedSuccessStartAt ? expectedIndex : 0, match.Index);

            // Substring means that anchors and assertions can't examine the string
            // beyond the specified range
            match = r.Match(input.Substring(startat, length));
            Assert.Equal(expectedSuccessBeginning, match.Success);
            Assert.Equal(expectedSuccessBeginning ? expectedValue : "", match.Value);
            Assert.Equal(expectedSuccessBeginning ? expectedIndex - startat : 0, match.Index);

            // (input, startat, length) behavior is identical to substring case,
            // except that index of any match is relative to the original input.
            match = r.Match(input, startat, length);
            Assert.Equal(expectedSuccessBeginning, match.Success);
            Assert.Equal(expectedSuccessBeginning ? expectedValue : "", match.Value);
            Assert.Equal(expectedSuccessBeginning ? expectedIndex : 0, match.Index);
        }

        [Theory]
        [InlineData(@"(?<1>\d{1,2})/(?<2>\d{1,2})/(?<3>\d{2,4})\s(?<time>\S+)", "08/10/99 16:00", "${time}", "16:00")]
        [InlineData(@"(?<1>\d{1,2})/(?<2>\d{1,2})/(?<3>\d{2,4})\s(?<time>\S+)", "08/10/99 16:00", "${1}", "08")]
        [InlineData(@"(?<1>\d{1,2})/(?<2>\d{1,2})/(?<3>\d{2,4})\s(?<time>\S+)", "08/10/99 16:00", "${2}", "10")]
        [InlineData(@"(?<1>\d{1,2})/(?<2>\d{1,2})/(?<3>\d{2,4})\s(?<time>\S+)", "08/10/99 16:00", "${3}", "99")]
        [InlineData("abc", "abc", "abc", "abc")]
        public void Result(string pattern, string input, string replacement, string expected)
        {
            Assert.Equal(expected, new Regex(pattern).Match(input).Result(replacement));
        }

        [Fact]
        public void Result_Invalid()
        {
            Match match = Regex.Match("foo", "foo");
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => match.Result(null));

            Assert.Throws<NotSupportedException>(() => RegularExpressions.Match.Empty.Result("any"));
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Match_SpecialUnicodeCharacters_enUS(RegexEngine engine)
        {
            using (new ThreadCultureChange("en-US"))
            {
                await CreateAndMatch(engine, "\u0131", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                await CreateAndMatch(engine, "\u0131", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Match_SpecialUnicodeCharacters_Invariant(RegexEngine engine)
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                await CreateAndMatch(engine, "\u0131", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                await CreateAndMatch(engine, "\u0131", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                await CreateAndMatch(engine, "\u0130", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                await CreateAndMatch(engine, "\u0130", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
            }
        }

        private static bool IsNotArmProcessAndRemoteExecutorSupported => PlatformDetection.IsNotArmProcess && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(IsNotArmProcessAndRemoteExecutorSupported))] // times out on ARM
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework does not have fix for https://github.com/dotnet/runtime/issues/24749")]
        [SkipOnCoreClr("Long running tests: https://github.com/dotnet/runtime/issues/10680", ~RuntimeConfiguration.Release)]
        [SkipOnCoreClr("Long running tests: https://github.com/dotnet/runtime/issues/10680", RuntimeTestModes.JitMinOpts)]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public void Match_ExcessPrefix(RegexEngine engine)
        {
            RemoteExecutor.Invoke(async engineString =>
            {
                var engine = (RegexEngine)Enum.Parse(typeof(RegexEngine), engineString);

                // Should not throw out of memory

                // Repeaters
                VerifyIsMatch((await RegexHelpers.GetRegexAsync(engine, @"a{2147483647,}")), "a", false, Regex.InfiniteMatchTimeout);
                VerifyIsMatch((await RegexHelpers.GetRegexAsync(engine, @"a{50,}")), "a", false, Regex.InfiniteMatchTimeout);
                VerifyIsMatch((await RegexHelpers.GetRegexAsync(engine, @"a{50_000,}")), "a", false, Regex.InfiniteMatchTimeout); // cutoff for Boyer-Moore prefix in release

                // Multis
                foreach (int length in new[] { 50, 50_000, char.MaxValue + 1 })
                {
                    // The large counters are too slow for counting a's in NonBacktracking engine
                    // They will incur a constant of size length because in .*a{k} after reading n a's the
                    // state will be .*a{k}|a{k-1}|...|a{k-n} which could be compacted to
                    // .*a{k}|a{k-n,k-1} but is not currently being compacted
                    if (!RegexHelpers.IsNonBacktracking(engine) || length < 50_000)
                    {
                        string s = "bcd" + new string('a', length) + "efg";
                        VerifyIsMatch((await RegexHelpers.GetRegexAsync(engine, @$"a{{{length}}}")), s, true, Regex.InfiniteMatchTimeout);
                    }
                }
            }, engine.ToString()).Dispose();
        }

        [Fact]
        public void Match_Invalid()
        {
            var r = new Regex("pattern");

            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Match(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Match(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Match(null, "pattern", RegexOptions.None, TimeSpan.FromSeconds(1)));

            AssertExtensions.Throws<ArgumentNullException>("input", () => r.Match(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => r.Match(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("input", () => r.Match(null, 0, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Match("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Match("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Match("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Start is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Match("input", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Match("input", -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Match("input", 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Match("input", 6, 0));

            // Length is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => r.Match("input", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => r.Match("input", 0, 6));
        }

        [Fact]
        public void IsMatch_Invalid()
        {
            var r = new Regex("pattern");

            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.IsMatch(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.IsMatch(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.IsMatch(null, "pattern", RegexOptions.None, TimeSpan.FromSeconds(1)));

            AssertExtensions.Throws<ArgumentNullException>("input", () => r.IsMatch(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => r.IsMatch(null, 0));

            // Pattern is null
            VerifyIsMatchThrows<ArgumentNullException>(null, "input", Regex.InfiniteMatchTimeout, pattern: null);
            VerifyIsMatchThrows<ArgumentNullException>(null, "input", Regex.InfiniteMatchTimeout, pattern: null, RegexOptions.None);
            VerifyIsMatchThrows<ArgumentNullException>(null, "input", TimeSpan.FromSeconds(1), pattern: null, RegexOptions.None);

            // Start is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => r.IsMatch("input", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.IsMatch("input", 6));
        }

        public static IEnumerable<object[]> IsMatch_SucceedQuicklyDueToLoopReduction_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"(?:\w*)+\.", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false };
                yield return new object[] { engine, @"(?:a+)+b", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false };
                yield return new object[] { engine, @"(?:x+x+)+y", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", false };
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // take too long due to backtracking
        [Theory]
        [MemberData(nameof(IsMatch_SucceedQuicklyDueToLoopReduction_MemberData))]
        public async Task IsMatch_SucceedQuicklyDueToLoopReduction(RegexEngine engine, string pattern, string input, bool expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern);
            VerifyIsMatch(r, input, expected, Regex.InfiniteMatchTimeout);
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task TestCharIsLowerCultureEdgeCasesAroundTurkishCharacters(RegexEngine engine)
        {
            Regex r1 = await RegexHelpers.GetRegexAsync(engine, "[\u012F-\u0130]", RegexOptions.IgnoreCase);
            Regex r2 = await RegexHelpers.GetRegexAsync(engine, "[\u012F\u0130]", RegexOptions.IgnoreCase);
            Assert.Equal(r1.IsMatch("\u0130"), r2.IsMatch("\u0130"));
#if NET7_0_OR_GREATER
            Assert.Equal(r1.IsMatch("\u0130".AsSpan()), r2.IsMatch("\u0130".AsSpan()));
#endif

        }

        [Fact]
        public void Synchronized()
        {
            var m = new Regex("abc").Match("abc");
            Assert.True(m.Success);
            RegexAssert.Equal("abc", m);

            var m2 = System.Text.RegularExpressions.Match.Synchronized(m);
            Assert.Same(m, m2);
            Assert.True(m2.Success);
            RegexAssert.Equal("abc", m2);

            AssertExtensions.Throws<ArgumentNullException>("inner", () => System.Text.RegularExpressions.Match.Synchronized(null));
        }

        /// <summary>
        /// Tests current inconsistent treatment of \b and \w.
        /// The match fails because \u200c and \u200d do not belong to \w.
        /// At the same time \u200c and \u200d are considered as word characters for the \b and \B anchors.
        /// The test checks that the same behavior applies to all backends.
        /// </summary>
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Match_Boundary(RegexEngine engine)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"\b\w+\b");
            VerifyIsMatch(r, " AB\u200cCD ", false, Regex.InfiniteMatchTimeout);
            VerifyIsMatch(r, " AB\u200dCD ", false, Regex.InfiniteMatchTimeout);
        }

        public static IEnumerable<object[]> Match_Count_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"\b\w+\b", "one two three", 3 };
                yield return new object[] { engine, @"\b\w+\b", "on\u200ce two three", 2 };
                yield return new object[] { engine, @"\b\w+\b", "one tw\u200do three", 2 };
            }

            string b1 = @"((?<=\w)(?!\w)|(?<!\w)(?=\w))";
            string b2 = @"((?<=\w)(?=\W)|(?<=\W)(?=\w))";
            // Lookarounds are currently not supported in the NonBacktracking engine
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (engine == RegexEngine.NonBacktracking) continue;

                // b1 is semantically identical to \b except for \u200c and \u200d
                yield return new object[] { engine, $@"{b1}\w+{b1}", "one two three", 3 };
                yield return new object[] { engine, $@"{b1}\w+{b1}", "on\u200ce two three", 4 };
                // contrast between using \W = [^\w] vs negative lookaround !\w 
                yield return new object[] { engine, $@"{b2}\w+{b2}", "one two three", 1 };
                yield return new object[] { engine, $@"{b2}\w+{b2}", "one two", 0 };
            }
        }

        [Theory]
        [MemberData(nameof(Match_Count_TestData))]
        public async Task Match_Count(RegexEngine engine, string pattern, string input, int expectedCount)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern);
            Assert.Equal(expectedCount,r.Matches(input).Count);
        }

        public static IEnumerable<object[]> StressTestDeepNestingOfConcat_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "[a-z]", "", "abcde", 2000, 400 };
                yield return new object[] { engine, "[a-e]*", "$", "abcde", 2000, 20 };
                yield return new object[] { engine, "[a-e]*?", "$", "abcde", 2000, 20 };
                yield return new object[] { engine, "[a-d]?[a-e]?[a-f]?[a-g]?[a-h]?", "$", "abcda", 400, 4 };
                yield return new object[] { engine, "(a|A)", "", "aAaAa", 2000, 400 };
            }
        }

        [OuterLoop("Can take over a minute")]
        [Theory]
        [MemberData(nameof(StressTestDeepNestingOfConcat_TestData))]
        public async Task StressTestDeepNestingOfConcat(RegexEngine engine, string pattern, string anchor, string input, int pattern_repetition, int input_repetition)
        {
            string fullpattern = string.Concat(string.Concat(Enumerable.Repeat($"({pattern}", pattern_repetition).Concat(Enumerable.Repeat(")", pattern_repetition))), anchor);
            string fullinput = string.Concat(Enumerable.Repeat(input, input_repetition));

            Func<string, string, string, Task> func = static async (engineStr, fullpattern, fullinput) =>
            {
                RegexEngine engine = (RegexEngine)Enum.Parse(typeof(RegexEngine), engineStr);

                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    RegexHelpers.SetSafeSizeThreshold(10_005);
                }

                Regex re;
                try
                {
                    re = await RegexHelpers.GetRegexAsync(engine, fullpattern);
                }
                finally
                {
                    if (RegexHelpers.IsNonBacktracking(engine))
                    {
                        RegexHelpers.RestoreSafeSizeThresholdToDefault();
                    }
                }

                Assert.True(re.Match(fullinput).Success);
            };

            if (RegexHelpers.IsNonBacktracking(engine))
            {
                RemoteExecutor.Invoke(func, engine.ToString(), fullpattern, fullinput).Dispose();
            }
            else
            {
                await func(engine.ToString(), fullpattern, fullinput);
            }
        }

        public static IEnumerable<object[]> StressTestDeepNestingOfLoops_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "(", "a", ")*", "a", 2000, 1000 };
                yield return new object[] { engine, "(", "[aA]", ")+", "aA", 2000, 3000 };
                yield return new object[] { engine, "(", "ab", "){0,1}", "ab", 2000, 1000 };
            }
        }

        [OuterLoop("Can take over 10 seconds")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))] // consumes a lot of memory
        [MemberData(nameof(StressTestDeepNestingOfLoops_TestData))]
        public async Task StressTestDeepNestingOfLoops(RegexEngine engine, string begin, string inner, string end, string input, int pattern_repetition, int input_repetition)
        {
            string fullpattern = string.Concat(Enumerable.Repeat(begin, pattern_repetition)) + inner + string.Concat(Enumerable.Repeat(end, pattern_repetition));
            string fullinput = string.Concat(Enumerable.Repeat(input, input_repetition));

            Func<string, string, string, Task> func = static async (engineStr, fullpattern, fullinput) =>
            {
                RegexEngine engine = (RegexEngine)Enum.Parse(typeof(RegexEngine), engineStr);

                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    RegexHelpers.SetSafeSizeThreshold(int.MaxValue);
                }

                Regex re;
                try
                {
                    re = await RegexHelpers.GetRegexAsync(engine, fullpattern);
                }
                finally
                {
                    if (RegexHelpers.IsNonBacktracking(engine))
                    {
                        RegexHelpers.RestoreSafeSizeThresholdToDefault();
                    }
                }

                Assert.True(re.Match(fullinput).Success);
            };

            if (RegexHelpers.IsNonBacktracking(engine))
            {
                RemoteExecutor.Invoke(func, engine.ToString(), fullpattern, fullinput).Dispose();
            }
            else
            {
                await func(engine.ToString(), fullpattern, fullinput);
            }
        }

        public static IEnumerable<object[]> StressTestNfaMode_TestData()
        {
            yield return new object[] { "(?:a|aa|[abc]?[ab]?[abcd]).{20}$", "aaa01234567890123456789", 23 };
            yield return new object[] { "(?:a|AA|BCD).{20}$", "a01234567890123456789", 21 };
            yield return new object[] { "(?:a.{20}|a.{10})bc$", "a01234567890123456789bc", 23 };
        }

        /// <summary>
        /// Causes NonBacktracking engine to switch to NFA mode internally.
        /// NFA mode is otherwise never triggered by typical cases.
        /// </summary>
        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [MemberData(nameof(StressTestNfaMode_TestData))]
        public async Task StressTestNfaMode(string pattern, string input_suffix, int expected_matchlength)
        {
            Random random = new Random(0);
            byte[] buffer = new byte[50_000];
            random.NextBytes(buffer);
            // Consider a random string of 50_000 a's and b's
            var input = new string(Array.ConvertAll(buffer, b => (b <= 0x7F ? 'a' : 'b')));
            input += input_suffix;
            Regex re = await RegexHelpers.GetRegexAsync(RegexEngine.NonBacktracking, pattern, RegexOptions.Singleline);
            Match m = re.Match(input);
            Assert.True(m.Success);
            Assert.Equal(buffer.Length, m.Index);
            Assert.Equal(expected_matchlength, m.Length);
        }

        public static IEnumerable<object[]> AllMatches_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                // Basic
                yield return new object[] { engine, @"a+", RegexOptions.None, "xxxxxaaaaxxxxxxxxxxaaaaaa", new (int, int, string)[] { (5, 4, "aaaa"), (19, 6, "aaaaaa") } };
                yield return new object[] { engine, @"(...)+", RegexOptions.None, "abcd\nfghijklm", new (int, int, string)[] { (0, 3, "abc"), (5, 6, "fghijk") } };
                yield return new object[] { engine, @"something", RegexOptions.None, "nothing", null };
                yield return new object[] { engine, "(a|ba)c", RegexOptions.None, "bac", new (int, int, string)[] { (0, 3, "bac") } };
                yield return new object[] { engine, "(a|ba)c", RegexOptions.None, "ac", new (int, int, string)[] { (0, 2, "ac") } };
                yield return new object[] { engine, "(a|ba)c", RegexOptions.None, "baacd", new (int, int, string)[] { (2, 2, "ac") } };
                yield return new object[] { engine, "\n", RegexOptions.None, "\n", new (int, int, string)[] { (0, 1, "\n") } };
                yield return new object[] { engine, "[^a]", RegexOptions.None, "\n", new (int, int, string)[] { (0, 1, "\n") } };

                // In Singleline mode . includes all characters, also \n
                yield return new object[] { engine, @"(...)+", RegexOptions.None | RegexOptions.Singleline, "abcd\nfghijklm", new (int, int, string)[] { (0, 12, "abcd\nfghijkl") } };

                // Ignoring case
                yield return new object[] { engine, @"a+", RegexOptions.None | RegexOptions.IgnoreCase, "xxxxxaAAaxxxxxxxxxxaaaaAa", new (int, int, string)[] { (5, 4, "aAAa"), (19, 6, "aaaaAa") } };

                // NonASCII characters
                yield return new object[] { engine, @"(\uFFFE\uFFFF)+", RegexOptions.None, "=====\uFFFE\uFFFF\uFFFE\uFFFF\uFFFE====",
                    new (int, int, string)[] { (5, 4, "\uFFFE\uFFFF\uFFFE\uFFFF") } };
                yield return new object[] { engine, @"\d\s\w+", RegexOptions.None, "=====1\v\u212A4==========1\ta\u0130Aa",
                    new (int, int, string)[] { (5, 4, "1\v\u212A4"), (19, 6, "1\ta\u0130Aa") } };
                yield return new object[] { engine, @"\u221E|\u2713", RegexOptions.None, "infinity \u221E and checkmark \u2713 are contained here",
                    new (int, int, string)[] { (9, 1, "\u221E"), (25, 1, "\u2713") } };

                // Whitespace
                yield return new object[] { engine, @"\s+", RegexOptions.None, "===== \n\t\v\r ====", new (int, int, string)[] { (5, 6, " \n\t\v\r ") } };

                // Unicode character classes, the input string uses the first element of each character class 
                yield return new object[] {
                        engine,
                        @"\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Mn}\p{Mc}\p{Me}\p{Nd}\p{Nl}", RegexOptions.None,
                        "=====Aa\u01C5\u02B0\u01BB\u0300\u0903\u04880\u16EE===",
                        new (int, int, string)[] { (5, 10, "Aa\u01C5\u02B0\u01BB\u0300\u0903\u04880\u16EE") }
                    };
                yield return new object[] {
                        engine,
                        @"\p{No}\p{Zs}\p{Zl}\p{Zp}\p{Cc}\p{Cf}\p{Cs}\p{Co}\p{Pc}\p{Pd}",
                        RegexOptions.None,
                        "=====\u00B2 \u2028\u2029\0\u0600\uD800\uE000_\u002D===",
                        new (int, int, string)[] { (5, 10, "\u00B2 \u2028\u2029\0\u0600\uD800\uE000_\u002D") }
                    };
                yield return new object[] {
                        engine,
                        @"\p{Ps}\p{Pe}\p{Pi}\p{Pf}\p{Po}\p{Sm}\p{Sc}\p{Sk}\p{So}\p{Cn}",
                        RegexOptions.None,
                        "=====()\xAB\xBB!+$^\xA6\u0378===",
                        new (int, int, string)[] { (5, 10, "()\xAB\xBB!+$^\xA6\u0378") }
                    };
                yield return new object[] {
                        engine,
                        @"\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Mn}\p{Mc}\p{Me}\p{Nd}\p{Nl}\p{No}\p{Zs}\p{Zl}\p{Zp}\p{Cc}\p{Cf}\p{Cs}\p{Co}\p{Pc}\p{Pd}\p{Ps}\p{Pe}\p{Pi}\p{Pf}\p{Po}\p{Sm}\p{Sc}\p{Sk}\p{So}\p{Cn}",
                        RegexOptions.None,
                        "=====Aa\u01C5\u02B0\u01BB\u0300\u0903\u04880\u16EE\xB2 \u2028\u2029\0\u0600\uD800\uE000_\x2D()\xAB\xBB!+$^\xA6\u0378===",
                        new (int, int, string)[] { (5, 30, "Aa\u01C5\u02B0\u01BB\u0300\u0903\u04880\u16EE\xB2 \u2028\u2029\0\u0600\uD800\uE000_\x2D()\xAB\xBB!+$^\xA6\u0378") }
                    };

                // Case insensitive cases by using ?i and some non-ASCII characters like Kelvin sign and applying ?i over negated character classes
                yield return new object[] { engine, "(?i:[a-d\u00D5]+k*)", RegexOptions.None, "xyxaB\u00F5c\u212AKAyy", new (int, int, string)[] { (3, 6, "aB\u00F5c\u212AK"), (9, 1, "A") } };
                yield return new object[] { engine, "(?i:[a-d]+)", RegexOptions.None, "xyxaBcyy", new (int, int, string)[] { (3, 3, "aBc") } };
                yield return new object[] { engine, "(?i:[\0-@B-\uFFFF]+)", RegexOptions.None, "xaAaAy", new (int, int, string)[] { (0, 6, "xaAaAy") } }; // this is the same as .+
                yield return new object[] { engine, "(?i:[\0-ac-\uFFFF])", RegexOptions.None, "b", new (int, int, string)[] { (0, 1, "b") } };
                yield return new object[] { engine, "(?i:[\0-PR-\uFFFF])", RegexOptions.None, "Q", new (int, int, string)[] { (0, 1, "Q") } };
                yield return new object[] { engine, "(?i:[\0-pr-\uFFFF])", RegexOptions.None, "q", new (int, int, string)[] { (0, 1, "q") } };
                yield return new object[] { engine, "(?i:[^a])", RegexOptions.None, "aAaA", null };             // this correponds to not{a,A}
                yield return new object[] { engine, "(?i:[\0-\uFFFF-[A]])", RegexOptions.None, "aAaA", null };  // this correponds to not{a,A}
                yield return new object[] { engine, "(?i:[^Q])", RegexOptions.None, "q", null };
                yield return new object[] { engine, "(?i:[^b])", RegexOptions.None, "b", null };

                // Use of anchors
                yield return new object[] { engine, @"\b\w+nn\b", RegexOptions.None, "both Anne and Ann are names that contain nn", new (int, int, string)[] { (14, 3, "Ann") } };
                yield return new object[] { engine, @"\B x", RegexOptions.None, " xx", new (int, int, string)[] { (0, 2, " x") } };
                yield return new object[] { engine, @"\bxx\b", RegexOptions.None, " zxx:xx", new (int, int, string)[] { (5, 2, "xx") } };
                yield return new object[] { engine, @"^abc*\B", RegexOptions.None | RegexOptions.Multiline, "\nabcc \nabcccd\n", new (int, int, string)[] { (1, 3, "abc"), (7, 5, "abccc") } };
                yield return new object[] { engine, "^abc", RegexOptions.None, "abcccc", new (int, int, string)[] { (0, 3, "abc") } };
                yield return new object[] { engine, "^abc", RegexOptions.None, "aabcccc", null };
                yield return new object[] { engine, "abc$", RegexOptions.None, "aabcccc", null };
                yield return new object[] { engine, @"abc\z", RegexOptions.None, "aabc\n", null };
                yield return new object[] { engine, @"abc\Z", RegexOptions.None, "aabc\n", new (int, int, string)[] { (1, 3, "abc") } };
                yield return new object[] { engine, "abc$", RegexOptions.None, "aabc\nabc", new (int, int, string)[] { (5, 3, "abc") } };
                yield return new object[] { engine, "abc$", RegexOptions.None | RegexOptions.Multiline, "aabc\nabc", new (int, int, string)[] { (1, 3, "abc"), (5, 3, "abc") } };
                yield return new object[] { engine, @"a\bb", RegexOptions.None, "ab", null };
                yield return new object[] { engine, @"a\Bb", RegexOptions.None, "ab", new (int, int, string)[] { (0, 2, "ab") } };
                yield return new object[] { engine, @"(a\Bb|a\bb)", RegexOptions.None, "ab", new (int, int, string)[] { (0, 2, "ab") } };
                yield return new object[] { engine, @"a$", RegexOptions.None | RegexOptions.Multiline, "b\na", new (int, int, string)[] { (2, 1, "a") } };

                // Various loop constructs
                yield return new object[] { engine, "a[bcd]{4,5}(.)", RegexOptions.None, "acdbcdbe", new (int, int, string)[] { (0, 7, "acdbcdb") } };
                yield return new object[] { engine, "a[bcd]{4,5}?(.)", RegexOptions.None, "acdbcdbe", new (int, int, string)[] { (0, 6, "acdbcd") } };
                yield return new object[] { engine, "(x{3})+", RegexOptions.None, "abcxxxxxxxxacacaca", new (int, int, string)[] { (3, 6, "xxxxxx") } };
                yield return new object[] { engine, "(x{3})+?", RegexOptions.None, "abcxxxxxxxxacacaca", new (int, int, string)[] { (3, 3, "xxx"), (6, 3, "xxx") } };
                yield return new object[] { engine, "a[0-9]+0", RegexOptions.None, "ababca123000xyz", new (int, int, string)[] { (5, 7, "a123000") } };
                yield return new object[] { engine, "a[0-9]+?0", RegexOptions.None, "ababca123000xyz", new (int, int, string)[] { (5, 5, "a1230") } };
                // Mixed lazy/eager loop
                yield return new object[] { engine, "a[0-9]+?0|b[0-9]+0", RegexOptions.None, "ababca123000xyzababcb123000xyz", new (int, int, string)[] { (5, 5, "a1230"), (20, 7, "b123000") } };
                // Loops around alternations
                yield return new object[] { engine, "^(?:aaa|aa)*$", RegexOptions.None, "aaaaaaaa", new (int, int, string)[] { (0, 8, "aaaaaaaa") } };
                yield return new object[] { engine, "^(?:aaa|aa)*?$", RegexOptions.None, "aaaaaaaa", new (int, int, string)[] { (0, 8, "aaaaaaaa") } };
                yield return new object[] { engine, "^(?:aaa|aa){1,5}$", RegexOptions.None, "aaaaaaaa", new (int, int, string)[] { (0, 8, "aaaaaaaa") } };
                yield return new object[] { engine, "^(?:aaa|aa){1,5}?$", RegexOptions.None, "aaaaaaaa", new (int, int, string)[] { (0, 8, "aaaaaaaa") } };
                yield return new object[] { engine, "^(?:aaa|aa){4}$", RegexOptions.None, "aaaaaaaa", new (int, int, string)[] { (0, 8, "aaaaaaaa") } };
                yield return new object[] { engine, "^(?:aaa|aa){4}?$", RegexOptions.None, "aaaaaaaa", new (int, int, string)[] { (0, 8, "aaaaaaaa") } };

                // Mostly empty matches using unusual regexes consisting mostly of anchors only
                yield return new object[] { engine, "^", RegexOptions.None, "", new (int, int, string)[] { (0, 0, "") } };
                yield return new object[] { engine, "$", RegexOptions.None, "", new (int, int, string)[] { (0, 0, "") } };
                yield return new object[] { engine, "^$", RegexOptions.None, "", new (int, int, string)[] { (0, 0, "") } };
                yield return new object[] { engine, "$^", RegexOptions.None, "", new (int, int, string)[] { (0, 0, "") } };
                yield return new object[] { engine, "$^$$^^$^$", RegexOptions.None, "", new (int, int, string)[] { (0, 0, "") } };
                yield return new object[] { engine, "a*", RegexOptions.None, "bbb", new (int, int, string)[] { (0, 0, ""), (1, 0, ""), (2, 0, ""), (3, 0, "") } };
                yield return new object[] { engine, "a*", RegexOptions.None, "baaabb", new (int, int, string)[] { (0, 0, ""), (1, 3, "aaa"), (4, 0, ""), (5, 0, ""), (6, 0, "") } };
                yield return new object[] { engine, @"\b", RegexOptions.None, "hello--world", new (int, int, string)[] { (0, 0, ""), (5, 0, ""), (7, 0, ""), (12, 0, "") } };
                yield return new object[] { engine, @"\B", RegexOptions.None, "hello--world",
                    new (int, int, string)[] { (1, 0, ""), (2, 0, ""), (3, 0, ""), (4, 0, ""), (6, 0, ""), (8, 0, ""), (9, 0, ""), (10, 0, ""), (11, 0, "") } };

                // Involving many different characters in the same regex
                yield return new object[] { engine, @"(abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>:;@)+", RegexOptions.None,
                    "=====abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>:;@abcdefg======",
                    new (int, int, string)[] { (5, 67, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>:;@") } };

                //this will need a total of 2x70 + 2 parts in the partition of NonBacktracking
                string pattern_orig = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>:;&@%!";
                string pattern_WL = new String(Array.ConvertAll(pattern_orig.ToCharArray(), c => (char)((int)c + 0xFF00 - 32)));
                string pattern = "(" + pattern_orig + "===" + pattern_WL + ")+";
                string input = "=====" + pattern_orig + "===" + pattern_WL + pattern_orig + "===" + pattern_WL + "===" + pattern_orig + "===" + pattern_orig;
                int length = 2 * (pattern_orig.Length + 3 + pattern_WL.Length);
                yield return new object[] { engine, pattern, RegexOptions.None, input, new (int, int, string)[]{(5, length, input.Substring(5, length)) } };
            }
        }

        /// <summary>
        /// Test all top level matches for given pattern and options.
        /// </summary>
        [Theory]
        [MemberData(nameof(AllMatches_TestData))]
        public async Task AllMatches(RegexEngine engine, string pattern, RegexOptions options, string input, (int, int, string)[] matches)
        {
            Regex re = await RegexHelpers.GetRegexAsync(engine, pattern, options);
            Match m = re.Match(input);
            if (matches == null)
            {
                Assert.False(m.Success);
            }
            else
            {
                int i = 0;
                do
                {
                    Assert.True(m.Success);
                    Assert.True(i < matches.Length);
                    Assert.Equal(matches[i].Item1, m.Index);
                    Assert.Equal(matches[i].Item2, m.Length);
                    Assert.Equal(matches[i++].Item3, m.Value);
                    m = m.NextMatch();
                }
                while (m.Success);
                Assert.Equal(matches.Length, i);
            }
        }

        [Theory]
        [InlineData(@"\w")]
        [InlineData(@"\s")]
        [InlineData(@"\d")]
        public async Task StandardCharSets_SameMeaningAcrossAllEngines(string singleCharPattern)
        {
            var regexes = new List<Regex>();
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                regexes.Add(await RegexHelpers.GetRegexAsync(engine, singleCharPattern));
            }

            if (regexes.Count < 2)
            {
                return;
            }

            for (int c = '\0'; c <= '\uFFFF'; c++)
            {
                string s = ((char)c).ToString();
                bool baseline = regexes[0].IsMatch(s);
                for (int i = 1; i < regexes.Count; i++)
                {
                    VerifyIsMatch(regexes[i], s, baseline, Regex.InfiniteMatchTimeout);
                }
            }
        }

        private static void VerifyIsMatchThrows<T>(Regex? r, string input, TimeSpan timeout, string? pattern = null, RegexOptions options = RegexOptions.None)
            where T : Exception
        {
            if (r == null)
            {
                Assert.Throws<T>(() => timeout == Regex.InfiniteMatchTimeout ? Regex.IsMatch(input, pattern, options) : Regex.IsMatch(input, pattern, options, timeout));
#if NET7_0_OR_GREATER
                Assert.Throws<T>(() => timeout == Regex.InfiniteMatchTimeout ? Regex.IsMatch(input.AsSpan(), pattern, options) : Regex.IsMatch(input.AsSpan(), pattern, options, timeout));
#endif
            }
            else
            {
                Assert.Throws<T>(() => r.IsMatch(input));
#if NET7_0_OR_GREATER
                Assert.Throws<T>(() => r.IsMatch(input.AsSpan()));
#endif
            }
        }

        private static void VerifyIsMatch(Regex? r, string input, bool expected, TimeSpan timeout, string? pattern = null, RegexOptions options = RegexOptions.None)
        {
            if (r == null)
            {
                Assert.Equal(expected, timeout == Regex.InfiniteMatchTimeout ? Regex.IsMatch(input, pattern, options) : Regex.IsMatch(input, pattern, options, timeout));
                if (options == RegexOptions.None)
                {
                    Assert.Equal(expected, Regex.IsMatch(input, pattern));
                }
#if NET7_0_OR_GREATER
                Assert.Equal(expected, timeout == Regex.InfiniteMatchTimeout ? Regex.IsMatch(input.AsSpan(), pattern, options) : Regex.IsMatch(input.AsSpan(), pattern, options, timeout));
                if (options == RegexOptions.None)
                {
                    Assert.Equal(expected, Regex.IsMatch(input.AsSpan(), pattern));
                }
#endif
            }
            else
            {
                Assert.Equal(expected, r.IsMatch(input));
#if NET7_0_OR_GREATER
                Assert.Equal(expected, r.IsMatch(input.AsSpan()));
#endif
            }
        }

        public static IEnumerable<object[]> Match_DisjunctionOverCounting_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "a[abc]{0,10}", "a[abc]{0,3}", "xxxabbbbbbbyyy", true, "abbbbbbb" };
                yield return new object[] { engine, "a[abc]{0,10}?", "a[abc]{0,3}?", "xxxabbbbbbbyyy", true, "a" };
            }
        }

        [Theory]
        [MemberData(nameof(Match_DisjunctionOverCounting_TestData))]
        public async Task Match_DisjunctionOverCounting(RegexEngine engine, string disjunct1, string disjunct2, string input, bool success, string match)
        {
            Regex re = await RegexHelpers.GetRegexAsync(engine, disjunct1 + "|" + disjunct2);
            Match m = re.Match(input);
            Assert.Equal(success, m.Success);
            Assert.Equal(match, m.Value);
        }

        public static IEnumerable<object[]> MatchAmbiguousRegexes_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "(a|ab|c|bcd){0,}d*", "ababcd", (0, 1) };
                yield return new object[] { engine, "(a|ab|c|bcd){0,10}d*", "ababcd", (0, 1) };
                yield return new object[] { engine, "(a|ab|c|bcd)*d*", "ababcd", (0, 1) };
                yield return new object[] { engine, @"(the)\s*([12][0-9]|3[01]|0?[1-9])", "it is the 10:00 time", (6, 6) };
                yield return new object[] { engine, "(ab|a|bcd|c){0,}d*", "ababcd", (0, 6) };
                yield return new object[] { engine, "(ab|a|bcd|c){0,10}d*", "ababcd", (0, 6) };
                yield return new object[] { engine, "(ab|a|bcd|c)*d*", "ababcd", (0, 6) };
                yield return new object[] { engine, @"(the)\s*(0?[1-9]|[12][0-9]|3[01])", "it is the 10:00 time", (6, 5) };
            }
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [MemberData(nameof(MatchAmbiguousRegexes_TestData))]
        public async Task MatchAmbiguousRegexes(RegexEngine engine, string pattern, string input, (int,int) expected_match)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern);
            var match = r.Match(input);
            Assert.Equal(expected_match.Item1, match.Index);
            Assert.Equal(expected_match.Item2, match.Length);
        }

        public static IEnumerable<object[]> UseRegexConcurrently_ThreadSafe_Success_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, Timeout.InfiniteTimeSpan };
                yield return new object[] { engine, TimeSpan.FromMinutes(1) };
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop("Takes several seconds")]
        [MemberData(nameof(UseRegexConcurrently_ThreadSafe_Success_MemberData))]
        public async Task UseRegexConcurrently_ThreadSafe_Success(RegexEngine engine, TimeSpan timeout)
        {
            const string Input = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Maecenas porttitor congue massa. Fusce posuere, magna sed pulvinar ultricies, purus lectus malesuada libero, sit amet commodo magna eros quis urna. Nunc viverra imperdiet enim. Fusce est. Vivamus a tellus. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Proin pharetra nonummy pede. Mauris et orci. Aenean nec lorem. In porttitor. abcdefghijklmnx Donec laoreet nonummy augue. Suspendisse dui purus, scelerisque at, vulputate vitae, pretium mattis, nunc. Mauris eget neque at sem venenatis eleifend. Ut nonummy. Fusce aliquet pede non pede. Suspendisse dapibus lorem pellentesque magna. Integer nulla. Donec blandit feugiat ligula. Donec hendrerit, felis et imperdiet euismod, purus ipsum pretium metus, in lacinia nulla nisl eget sapien. Donec ut est in lectus consequat consequat. Etiam eget dui. Aliquam erat volutpat. Sed at lorem in nunc porta tristique. Proin nec augue. Quisque aliquam tempor magna. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Nunc ac magna. Maecenas odio dolor, vulputate vel, auctor ac, accumsan id, felis. Pellentesque cursus sagittis felis. Pellentesque porttitor, velit lacinia egestas auctor, diam eros tempus arcu, nec vulputate augue magna vel risus.nmlkjihgfedcbax";
            const int Trials = 100;
            const int IterationsPerTask = 10;

            using var b = new Barrier(Environment.ProcessorCount);

            for (int trial = 0; trial < Trials; trial++)
            {
                Regex r = await RegexHelpers.GetRegexAsync(engine, "[a-q][^u-z]{13}x", RegexOptions.None, timeout);
                Task.WaitAll(Enumerable.Range(0, b.ParticipantCount).Select(_ => Task.Factory.StartNew(() =>
                             {
                                 b.SignalAndWait();
                                 for (int i = 0; i < IterationsPerTask; i++)
                                 {
                                     Match m = r.Match(Input);
                                     Assert.NotNull(m);
                                     Assert.True(m.Success);
                                     Assert.Equal("abcdefghijklmnx", m.Value);

                                     m = m.NextMatch();
                                     Assert.NotNull(m);
                                     Assert.True(m.Success);
                                     Assert.Equal("nmlkjihgfedcbax", m.Value);

                                     m = m.NextMatch();
                                     Assert.NotNull(m);
                                     Assert.False(m.Success);
                                 }
                             }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(MatchWordsInAnchoredRegexes_TestData))]
        public async Task MatchWordsInAnchoredRegexes(RegexEngine engine, RegexOptions options, string pattern, string input, (int, int)[] matches)
        {
            // The aim of these test is to test corner cases of matches involving anchors
            // For NonBacktracking these tests are meant to
            // cover most contexts in _nullabilityForContext in SymbolicRegexNode
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);
            MatchCollection ms = r.Matches(input);
            Assert.Equal(matches.Length, ms.Count);
            for (int i = 0; i < matches.Length; i++)
            {
                Assert.Equal(ms[i].Index, matches[i].Item1);
                Assert.Equal(ms[i].Length, matches[i].Item2);
            }
        }

        public static IEnumerable<object[]> MatchWordsInAnchoredRegexes_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, RegexOptions.None, @"\b\w{10,}\b", "this is a complicated word in a\nnontrivial sentence", new (int, int)[] { (10, 11), (32, 10) } };
                yield return new object[] { engine, RegexOptions.Multiline, @"^\w{10,}\b", "this is a\ncomplicated word in a\nnontrivial sentence", new (int, int)[] { (10, 11), (32, 10) } };
                yield return new object[] { engine, RegexOptions.None, @"\b\d{1,2}\/\d{1,2}\/\d{2,4}\b", "date 10/12/1966 and 10/12/66 are the same", new (int, int)[] { (5, 10), (20, 8) } };
                yield return new object[] { engine, RegexOptions.Multiline, @"\b\d{1,2}\/\d{1,2}\/\d{2,4}$", "date 10/12/1966\nand 10/12/66\nare the same", new (int, int)[] { (5, 10), (20, 8) } };
            }
        }
    }
}
