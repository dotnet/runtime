// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// assembly:    System_test
// namespace:    MonoTests.System.Text.RegularExpressions
// file:    PerlTrials.cs
//
// author:    Dan Lewis (dlewis@gmx.co.uk)
//         (c) 2002

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class MonoTests
    {
        // Ported from https://github.com/mono/mono/blob/0f2995e95e98e082c7c7039e17175cf2c6a00034/mcs/class/System/Test/System.Text.RegularExpressions/PerlTrials.cs
        // Which in turn ported from perl-5.6.1/t/op/re_tests

        [Theory]
        [MemberData(nameof(ValidateRegex_MemberData))]
        public void ValidateRegex(RegexEngine engine, string pattern, RegexOptions options, Regex re, string input, string expected)
        {
            // Provided to the test for diagnostic purposes only
            _ = engine;
            _ = pattern;
            _ = options;

            Match m = re.Match(input);
            string result = "Fail.";
            if (m.Success)
            {
                result = "Pass.";
                int[] groupNums = re.GetGroupNumbers();
                for (int i = 0; i < m.Groups.Count; ++i)
                {
                    int gid = groupNums[i];
                    result += $" Group[{gid}]=";
                    foreach (Capture cap in m.Groups[gid].Captures)
                    {
                        result += $"({cap.Index},{cap.Length})";
                    }
                }
            }

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> ValidateRegex_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                (string Pattern, RegexOptions Options, string Input, string Expected)[] allEngineCases = Cases(engine).ToArray();

                Regex[] results = RegexHelpers.GetRegexesAsync(engine, allEngineCases.Select(c => (c.Pattern, (RegexOptions?)c.Options, (TimeSpan?)null)).ToArray()).Result;
                for (int i = 0; i < results.Length; i++)
                {
                    string expected = allEngineCases[i].Expected;
                    if (RegexHelpers.IsNonBacktracking(engine))
                    {
                        // NonBacktracking doesn't support captures other than top-level. Remove the rest from the expected results.
                        int j = expected.IndexOf(')');
                        if (j >= 0)
                        {
                            expected = expected.Substring(0, j + 1);
                        }
                    }

                    (string Pattern, RegexOptions Options, string Input, string Expected) testCase = allEngineCases[i];
                    yield return new object[] { engine, testCase.Pattern, testCase.Options, results[i], testCase.Input, expected };
                    yield return new object[] { engine, testCase.Pattern, testCase.Options | RegexOptions.CultureInvariant, results[i], testCase.Input, expected };
                }
            }

            static IEnumerable<(string Pattern, RegexOptions Options, string Input, string Result)> Cases(RegexEngine engine)
            {
                yield return (@"abc", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"abc", RegexOptions.None, "xbc", "Fail.");
                yield return (@"abc", RegexOptions.None, "axc", "Fail.");
                yield return (@"abc", RegexOptions.None, "abx", "Fail.");
                yield return (@"abc", RegexOptions.None, "xabcy", "Pass. Group[0]=(1,3)");
                yield return (@"abc", RegexOptions.None, "ababc", "Pass. Group[0]=(2,3)");
                yield return (@"ab*c", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"ab*bc", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"ab*bc", RegexOptions.None, "abbc", "Pass. Group[0]=(0,4)");
                yield return (@"ab*bc", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,6)");
                yield return (@".{1}", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,1)");
                yield return (@".{3,4}", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,4)");
                yield return (@"ab{0,}bc", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,6)");
                yield return (@"ab+bc", RegexOptions.None, "abbc", "Pass. Group[0]=(0,4)");
                yield return (@"ab+bc", RegexOptions.None, "abc", "Fail.");
                yield return (@"ab+bc", RegexOptions.None, "abq", "Fail.");
                yield return (@"ab{1,}bc", RegexOptions.None, "abq", "Fail.");
                yield return (@"ab+bc", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,6)");
                yield return (@"ab{1,}bc", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,6)");
                yield return (@"ab{1,3}bc", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,6)");
                yield return (@"ab{3,4}bc", RegexOptions.None, "abbbbc", "Pass. Group[0]=(0,6)");
                yield return (@"ab{4,5}bc", RegexOptions.None, "abbbbc", "Fail.");
                yield return (@"ab?bc", RegexOptions.None, "abbc", "Pass. Group[0]=(0,4)");
                yield return (@"ab?bc", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"ab{0,1}bc", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"ab?bc", RegexOptions.None, "abbbbc", "Fail.");
                yield return (@"ab?c", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"ab{0,1}c", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"^abc$", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"^abc$", RegexOptions.None, "abcc", "Fail.");
                yield return (@"^abc", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                yield return (@"^abc$", RegexOptions.None, "aabc", "Fail.");
                yield return (@"abc$", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                yield return (@"abc$", RegexOptions.None, "aabcd", "Fail.");
                yield return (@"^", RegexOptions.None, "abc", "Pass. Group[0]=(0,0)");
                yield return (@"$", RegexOptions.None, "abc", "Pass. Group[0]=(3,0)");
                yield return (@"a.c", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"a.c", RegexOptions.None, "axc", "Pass. Group[0]=(0,3)");
                yield return (@"a.*c", RegexOptions.None, "axyzc", "Pass. Group[0]=(0,5)");
                yield return (@"a.*c", RegexOptions.None, "axyzd", "Fail.");
                yield return (@"a[bc]d", RegexOptions.None, "abc", "Fail.");
                yield return (@"a[bc]d", RegexOptions.None, "abd", "Pass. Group[0]=(0,3)");
                yield return (@"a[b-d]e", RegexOptions.None, "abd", "Fail.");
                yield return (@"a[b-d]e", RegexOptions.None, "ace", "Pass. Group[0]=(0,3)");
                yield return (@"a[b-d]", RegexOptions.None, "aac", "Pass. Group[0]=(1,2)");
                yield return (@"a[-b]", RegexOptions.None, "a-", "Pass. Group[0]=(0,2)");
                yield return (@"a[b-]", RegexOptions.None, "a-", "Pass. Group[0]=(0,2)");
                yield return (@"a]", RegexOptions.None, "a]", "Pass. Group[0]=(0,2)");
                yield return (@"a[]]b", RegexOptions.None, "a]b", "Pass. Group[0]=(0,3)");
                yield return (@"a[^bc]d", RegexOptions.None, "aed", "Pass. Group[0]=(0,3)");
                yield return (@"a[^bc]d", RegexOptions.None, "abd", "Fail.");
                yield return (@"a[^-b]c", RegexOptions.None, "adc", "Pass. Group[0]=(0,3)");
                yield return (@"a[^-b]c", RegexOptions.None, "a-c", "Fail.");
                yield return (@"a[^]b]c", RegexOptions.None, "a]c", "Fail.");
                yield return (@"a[^]b]c", RegexOptions.None, "adc", "Pass. Group[0]=(0,3)");
                yield return (@"\ba\b", RegexOptions.None, "a-", "Pass. Group[0]=(0,1)");
                yield return (@"\ba\b", RegexOptions.None, "-a", "Pass. Group[0]=(1,1)");
                yield return (@"\ba\b", RegexOptions.None, "-a-", "Pass. Group[0]=(1,1)");
                yield return (@"\by\b", RegexOptions.None, "xy", "Fail.");
                yield return (@"\by\b", RegexOptions.None, "yz", "Fail.");
                yield return (@"\by\b", RegexOptions.None, "xyz", "Fail.");
                yield return (@"\Ba\B", RegexOptions.None, "a-", "Fail.");
                yield return (@"\Ba\B", RegexOptions.None, "-a", "Fail.");
                yield return (@"\Ba\B", RegexOptions.None, "-a-", "Fail.");
                yield return (@"\By\b", RegexOptions.None, "xy", "Pass. Group[0]=(1,1)");
                yield return (@"\by\B", RegexOptions.None, "yz", "Pass. Group[0]=(0,1)");
                yield return (@"\By\B", RegexOptions.None, "xyz", "Pass. Group[0]=(1,1)");
                yield return (@"\w", RegexOptions.None, "a", "Pass. Group[0]=(0,1)");
                yield return (@"\w", RegexOptions.None, "-", "Fail.");
                yield return (@"\W", RegexOptions.None, "a", "Fail.");
                yield return (@"\W", RegexOptions.None, "-", "Pass. Group[0]=(0,1)");
                yield return (@"a\sb", RegexOptions.None, "a b", "Pass. Group[0]=(0,3)");
                yield return (@"a\sb", RegexOptions.None, "a-b", "Fail.");
                yield return (@"a\Sb", RegexOptions.None, "a b", "Fail.");
                yield return (@"a\Sb", RegexOptions.None, "a-b", "Pass. Group[0]=(0,3)");
                yield return (@"\d", RegexOptions.None, "1", "Pass. Group[0]=(0,1)");
                yield return (@"\d", RegexOptions.None, "-", "Fail.");
                yield return (@"\D", RegexOptions.None, "1", "Fail.");
                yield return (@"\D", RegexOptions.None, "-", "Pass. Group[0]=(0,1)");
                yield return (@"[\w]", RegexOptions.None, "a", "Pass. Group[0]=(0,1)");
                yield return (@"[\w]", RegexOptions.None, "-", "Fail.");
                yield return (@"[\W]", RegexOptions.None, "a", "Fail.");
                yield return (@"[\W]", RegexOptions.None, "-", "Pass. Group[0]=(0,1)");
                yield return (@"a[\s]b", RegexOptions.None, "a b", "Pass. Group[0]=(0,3)");
                yield return (@"a[\s]b", RegexOptions.None, "a-b", "Fail.");
                yield return (@"a[\S]b", RegexOptions.None, "a b", "Fail.");
                yield return (@"a[\S]b", RegexOptions.None, "a-b", "Pass. Group[0]=(0,3)");
                yield return (@"[\d]", RegexOptions.None, "1", "Pass. Group[0]=(0,1)");
                yield return (@"[\d]", RegexOptions.None, "-", "Fail.");
                yield return (@"[\D]", RegexOptions.None, "1", "Fail.");
                yield return (@"[\D]", RegexOptions.None, "-", "Pass. Group[0]=(0,1)");
                yield return (@"ab|cd", RegexOptions.None, "abc", "Pass. Group[0]=(0,2)");
                yield return (@"ab|cd", RegexOptions.None, "abcd", "Pass. Group[0]=(0,2)");
                yield return (@"()ef", RegexOptions.None, "def", "Pass. Group[0]=(1,2) Group[1]=(1,0)");
                yield return (@"$b", RegexOptions.None, "b", "Fail.");
                yield return (@"a\(b", RegexOptions.None, "a(b", "Pass. Group[0]=(0,3)");
                yield return (@"a\(*b", RegexOptions.None, "ab", "Pass. Group[0]=(0,2)");
                yield return (@"a\(*b", RegexOptions.None, "a((b", "Pass. Group[0]=(0,4)");
                yield return (@"a\\b", RegexOptions.None, "a\\b", "Pass. Group[0]=(0,3)");
                yield return (@"((a))", RegexOptions.None, "abc", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1)");
                yield return (@"(a)b(c)", RegexOptions.None, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(2,1)");
                yield return (@"a+b+c", RegexOptions.None, "aabbabc", "Pass. Group[0]=(4,3)");
                yield return (@"a{1,}b{1,}c", RegexOptions.None, "aabbabc", "Pass. Group[0]=(4,3)");
                yield return (@"a.+?c", RegexOptions.None, "abcabc", "Pass. Group[0]=(0,3)");
                yield return (@"(a+|b)*", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b){0,}", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b)+", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b){1,}", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b)?", RegexOptions.None, "ab", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"(a+|b){0,1}", RegexOptions.None, "ab", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"[^ab]*", RegexOptions.None, "cde", "Pass. Group[0]=(0,3)");
                yield return (@"abc", RegexOptions.None, "", "Fail.");
                yield return (@"a*", RegexOptions.None, "", "Pass. Group[0]=(0,0)");
                yield return (@"([abc])*d", RegexOptions.None, "abbbcd", "Pass. Group[0]=(0,6) Group[1]=(0,1)(1,1)(2,1)(3,1)(4,1)");
                yield return (@"([abc])*bcd", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(0,1)");
                yield return (@"a|b|c|d|e", RegexOptions.None, "e", "Pass. Group[0]=(0,1)");
                yield return (@"(a|b|c|d|e)f", RegexOptions.None, "ef", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"abcd*efg", RegexOptions.None, "abcdefg", "Pass. Group[0]=(0,7)");
                yield return (@"ab*", RegexOptions.None, "xabyabbbz", "Pass. Group[0]=(1,2)");
                yield return (@"ab*", RegexOptions.None, "xayabbbz", "Pass. Group[0]=(1,1)");
                yield return (@"(ab|cd)e", RegexOptions.None, "abcde", "Pass. Group[0]=(2,3) Group[1]=(2,2)");
                yield return (@"[abhgefdc]ij", RegexOptions.None, "hij", "Pass. Group[0]=(0,3)");
                yield return (@"^(ab|cd)e", RegexOptions.None, "abcde", "Fail.");
                yield return (@"(abc|)ef", RegexOptions.None, "abcdef", "Pass. Group[0]=(4,2) Group[1]=(4,0)");
                yield return (@"(a|b)c*d", RegexOptions.None, "abcd", "Pass. Group[0]=(1,3) Group[1]=(1,1)");
                yield return (@"(ab|ab*)bc", RegexOptions.None, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,1)");
                yield return (@"a([bc]*)c*", RegexOptions.None, "abc", "Pass. Group[0]=(0,3) Group[1]=(1,2)");
                yield return (@"a([bc]*)(c*d)", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,2) Group[2]=(3,1)");
                yield return (@"a([bc]+)(c*d)", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,2) Group[2]=(3,1)");
                yield return (@"a([bc]*)(c+d)", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,1) Group[2]=(2,2)");
                yield return (@"a[bcd]*dcdcde", RegexOptions.None, "adcdcde", "Pass. Group[0]=(0,7)");
                yield return (@"a[bcd]+dcdcde", RegexOptions.None, "adcdcde", "Fail.");
                yield return (@"(ab|a)b*c", RegexOptions.None, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,2)");
                yield return (@"((a)(b)c)(d)", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(0,3) Group[2]=(0,1) Group[3]=(1,1) Group[4]=(3,1)");
                yield return (@"[a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.None, "alpha", "Pass. Group[0]=(0,5)");
                yield return (@"^a(bc+|b[eh])g|.h$", RegexOptions.None, "abh", "Pass. Group[0]=(1,2) Group[1]=");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.None, "effgz", "Pass. Group[0]=(0,5) Group[1]=(0,5) Group[2]=");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.None, "ij", "Pass. Group[0]=(0,2) Group[1]=(0,2) Group[2]=(1,1)");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.None, "effg", "Fail.");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.None, "bcdd", "Fail.");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.None, "reffgz", "Pass. Group[0]=(1,5) Group[1]=(1,5) Group[2]=");
                yield return (@"((((((((((a))))))))))", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                yield return (@"((((((((((a))))))))))!", RegexOptions.None, "aa", "Fail.");
                yield return (@"((((((((((a))))))))))!", RegexOptions.None, "a!", "Pass. Group[0]=(0,2) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                yield return (@"(((((((((a)))))))))", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1)");
                yield return (@"multiple words of text", RegexOptions.None, "uh-uh", "Fail.");
                yield return (@"multiple words", RegexOptions.None, "multiple words, yeah", "Pass. Group[0]=(0,14)");
                yield return (@"(.*)c(.*)", RegexOptions.None, "abcde", "Pass. Group[0]=(0,5) Group[1]=(0,2) Group[2]=(3,2)");
                yield return (@"\((.*), (.*)\)", RegexOptions.None, "(a, b)", "Pass. Group[0]=(0,6) Group[1]=(1,1) Group[2]=(4,1)");
                yield return (@"[k]", RegexOptions.None, "ab", "Fail.");
                yield return (@"abcd", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4)");
                yield return (@"a(bc)d", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,2)");
                yield return (@"a[-]?c", RegexOptions.None, "ac", "Pass. Group[0]=(0,2)");
                yield return (@"abc", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"abc", RegexOptions.IgnoreCase, "XBC", "Fail.");
                yield return (@"abc", RegexOptions.IgnoreCase, "AXC", "Fail.");
                yield return (@"abc", RegexOptions.IgnoreCase, "ABX", "Fail.");
                yield return (@"abc", RegexOptions.IgnoreCase, "XABCY", "Pass. Group[0]=(1,3)");
                yield return (@"abc", RegexOptions.IgnoreCase, "ABABC", "Pass. Group[0]=(2,3)");
                yield return (@"ab*c", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"ab*bc", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"ab*bc", RegexOptions.IgnoreCase, "ABBC", "Pass. Group[0]=(0,4)");
                yield return (@"ab*?bc", RegexOptions.IgnoreCase, "ABBBBC", "Pass. Group[0]=(0,6)");
                yield return (@"ab{0,}?bc", RegexOptions.IgnoreCase, "ABBBBC", "Pass. Group[0]=(0,6)");
                yield return (@"ab+?bc", RegexOptions.IgnoreCase, "ABBC", "Pass. Group[0]=(0,4)");
                yield return (@"ab+bc", RegexOptions.IgnoreCase, "ABC", "Fail.");
                yield return (@"ab+bc", RegexOptions.IgnoreCase, "ABQ", "Fail.");
                yield return (@"ab{1,}bc", RegexOptions.IgnoreCase, "ABQ", "Fail.");
                yield return (@"ab+bc", RegexOptions.IgnoreCase, "ABBBBC", "Pass. Group[0]=(0,6)");
                yield return (@"ab{1,}?bc", RegexOptions.IgnoreCase, "ABBBBC", "Pass. Group[0]=(0,6)");
                yield return (@"ab{1,3}?bc", RegexOptions.IgnoreCase, "ABBBBC", "Pass. Group[0]=(0,6)");
                yield return (@"ab{3,4}?bc", RegexOptions.IgnoreCase, "ABBBBC", "Pass. Group[0]=(0,6)");
                yield return (@"ab{4,5}?bc", RegexOptions.IgnoreCase, "ABBBBC", "Fail.");
                yield return (@"ab??bc", RegexOptions.IgnoreCase, "ABBC", "Pass. Group[0]=(0,4)");
                yield return (@"ab??bc", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"ab{0,1}?bc", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"ab??bc", RegexOptions.IgnoreCase, "ABBBBC", "Fail.");
                yield return (@"ab??c", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"ab{0,1}?c", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"^abc$", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"^abc$", RegexOptions.IgnoreCase, "ABCC", "Fail.");
                yield return (@"^abc", RegexOptions.IgnoreCase, "ABCC", "Pass. Group[0]=(0,3)");
                yield return (@"^abc$", RegexOptions.IgnoreCase, "AABC", "Fail.");
                yield return (@"abc$", RegexOptions.IgnoreCase, "AABC", "Pass. Group[0]=(1,3)");
                yield return (@"^", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,0)");
                yield return (@"$", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(3,0)");
                yield return (@"a.c", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3)");
                yield return (@"a.c", RegexOptions.IgnoreCase, "AXC", "Pass. Group[0]=(0,3)");
                yield return (@"a.*?c", RegexOptions.IgnoreCase, "AXYZC", "Pass. Group[0]=(0,5)");
                yield return (@"a.*c", RegexOptions.IgnoreCase, "AXYZD", "Fail.");
                yield return (@"a[bc]d", RegexOptions.IgnoreCase, "ABC", "Fail.");
                yield return (@"a[bc]d", RegexOptions.IgnoreCase, "ABD", "Pass. Group[0]=(0,3)");
                yield return (@"a[b-d]e", RegexOptions.IgnoreCase, "ABD", "Fail.");
                yield return (@"a[b-d]e", RegexOptions.IgnoreCase, "ACE", "Pass. Group[0]=(0,3)");
                yield return (@"a[b-d]", RegexOptions.IgnoreCase, "AAC", "Pass. Group[0]=(1,2)");
                yield return (@"a[-b]", RegexOptions.IgnoreCase, "A-", "Pass. Group[0]=(0,2)");
                yield return (@"a[b-]", RegexOptions.IgnoreCase, "A-", "Pass. Group[0]=(0,2)");
                yield return (@"a]", RegexOptions.IgnoreCase, "A]", "Pass. Group[0]=(0,2)");
                yield return (@"a[]]b", RegexOptions.IgnoreCase, "A]B", "Pass. Group[0]=(0,3)");
                yield return (@"a[^bc]d", RegexOptions.IgnoreCase, "AED", "Pass. Group[0]=(0,3)");
                yield return (@"a[^bc]d", RegexOptions.IgnoreCase, "ABD", "Fail.");
                yield return (@"a[^-b]c", RegexOptions.IgnoreCase, "ADC", "Pass. Group[0]=(0,3)");
                yield return (@"a[^-b]c", RegexOptions.IgnoreCase, "A-C", "Fail.");
                yield return (@"a[^]b]c", RegexOptions.IgnoreCase, "A]C", "Fail.");
                yield return (@"a[^]b]c", RegexOptions.IgnoreCase, "ADC", "Pass. Group[0]=(0,3)");
                yield return (@"ab|cd", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,2)");
                yield return (@"ab|cd", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,2)");
                yield return (@"()ef", RegexOptions.IgnoreCase, "DEF", "Pass. Group[0]=(1,2) Group[1]=(1,0)");
                yield return (@"$b", RegexOptions.IgnoreCase, "B", "Fail.");
                yield return (@"a\(b", RegexOptions.IgnoreCase, "A(B", "Pass. Group[0]=(0,3)");
                yield return (@"a\(*b", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,2)");
                yield return (@"a\(*b", RegexOptions.IgnoreCase, "A((B", "Pass. Group[0]=(0,4)");
                yield return (@"a\\b", RegexOptions.IgnoreCase, "A\\B", "Pass. Group[0]=(0,3)");
                yield return (@"((a))", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1)");
                yield return (@"(a)b(c)", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(2,1)");
                yield return (@"a+b+c", RegexOptions.IgnoreCase, "AABBABC", "Pass. Group[0]=(4,3)");
                yield return (@"a{1,}b{1,}c", RegexOptions.IgnoreCase, "AABBABC", "Pass. Group[0]=(4,3)");
                yield return (@"a.+?c", RegexOptions.IgnoreCase, "ABCABC", "Pass. Group[0]=(0,3)");
                yield return (@"a.*?c", RegexOptions.IgnoreCase, "ABCABC", "Pass. Group[0]=(0,3)");
                yield return (@"a.{0,5}?c", RegexOptions.IgnoreCase, "ABCABC", "Pass. Group[0]=(0,3)");
                yield return (@"(a+|b)*", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b){0,}", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b)+", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b){1,}", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,2) Group[1]=(0,1)(1,1)");
                yield return (@"(a+|b)?", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"(a+|b){0,1}", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"(a+|b){0,1}?", RegexOptions.IgnoreCase, "AB", "Pass. Group[0]=(0,0) Group[1]=");
                yield return (@"[^ab]*", RegexOptions.IgnoreCase, "CDE", "Pass. Group[0]=(0,3)");
                yield return (@"abc", RegexOptions.IgnoreCase, "", "Fail.");
                yield return (@"a*", RegexOptions.IgnoreCase, "", "Pass. Group[0]=(0,0)");
                yield return (@"([abc])*d", RegexOptions.IgnoreCase, "ABBBCD", "Pass. Group[0]=(0,6) Group[1]=(0,1)(1,1)(2,1)(3,1)(4,1)");
                yield return (@"([abc])*bcd", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4) Group[1]=(0,1)");
                yield return (@"a|b|c|d|e", RegexOptions.IgnoreCase, "E", "Pass. Group[0]=(0,1)");
                yield return (@"(a|b|c|d|e)f", RegexOptions.IgnoreCase, "EF", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"abcd*efg", RegexOptions.IgnoreCase, "ABCDEFG", "Pass. Group[0]=(0,7)");
                yield return (@"ab*", RegexOptions.IgnoreCase, "XABYABBBZ", "Pass. Group[0]=(1,2)");
                yield return (@"ab*", RegexOptions.IgnoreCase, "XAYABBBZ", "Pass. Group[0]=(1,1)");
                yield return (@"(ab|cd)e", RegexOptions.IgnoreCase, "ABCDE", "Pass. Group[0]=(2,3) Group[1]=(2,2)");
                yield return (@"[abhgefdc]ij", RegexOptions.IgnoreCase, "HIJ", "Pass. Group[0]=(0,3)");
                yield return (@"^(ab|cd)e", RegexOptions.IgnoreCase, "ABCDE", "Fail.");
                yield return (@"(abc|)ef", RegexOptions.IgnoreCase, "ABCDEF", "Pass. Group[0]=(4,2) Group[1]=(4,0)");
                yield return (@"(a|b)c*d", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(1,3) Group[1]=(1,1)");
                yield return (@"(ab|ab*)bc", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3) Group[1]=(0,1)");
                yield return (@"a([bc]*)c*", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3) Group[1]=(1,2)");
                yield return (@"a([bc]*)(c*d)", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4) Group[1]=(1,2) Group[2]=(3,1)");
                yield return (@"a([bc]+)(c*d)", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4) Group[1]=(1,2) Group[2]=(3,1)");
                yield return (@"a([bc]*)(c+d)", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4) Group[1]=(1,1) Group[2]=(2,2)");
                yield return (@"a[bcd]*dcdcde", RegexOptions.IgnoreCase, "ADCDCDE", "Pass. Group[0]=(0,7)");
                yield return (@"a[bcd]+dcdcde", RegexOptions.IgnoreCase, "ADCDCDE", "Fail.");
                yield return (@"(ab|a)b*c", RegexOptions.IgnoreCase, "ABC", "Pass. Group[0]=(0,3) Group[1]=(0,2)");
                yield return (@"((a)(b)c)(d)", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4) Group[1]=(0,3) Group[2]=(0,1) Group[3]=(1,1) Group[4]=(3,1)");
                yield return (@"[a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.IgnoreCase, "ALPHA", "Pass. Group[0]=(0,5)");
                yield return (@"^a(bc+|b[eh])g|.h$", RegexOptions.IgnoreCase, "ABH", "Pass. Group[0]=(1,2) Group[1]=");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.IgnoreCase, "EFFGZ", "Pass. Group[0]=(0,5) Group[1]=(0,5) Group[2]=");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.IgnoreCase, "IJ", "Pass. Group[0]=(0,2) Group[1]=(0,2) Group[2]=(1,1)");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.IgnoreCase, "EFFG", "Fail.");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.IgnoreCase, "BCDD", "Fail.");
                yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.IgnoreCase, "REFFGZ", "Pass. Group[0]=(1,5) Group[1]=(1,5) Group[2]=");
                yield return (@"((((((((((a))))))))))", RegexOptions.IgnoreCase, "A", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                yield return (@"((((((((((a))))))))))!", RegexOptions.IgnoreCase, "AA", "Fail.");
                yield return (@"((((((((((a))))))))))!", RegexOptions.IgnoreCase, "A!", "Pass. Group[0]=(0,2) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                yield return (@"(((((((((a)))))))))", RegexOptions.IgnoreCase, "A", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1)");
                yield return (@"(?:(?:(?:(?:(?:(?:(?:(?:(?:(a))))))))))", RegexOptions.IgnoreCase, "A", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"(?:(?:(?:(?:(?:(?:(?:(?:(?:(a|b|c))))))))))", RegexOptions.IgnoreCase, "C", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"multiple words of text", RegexOptions.IgnoreCase, "UH-UH", "Fail.");
                yield return (@"multiple words", RegexOptions.IgnoreCase, "MULTIPLE WORDS, YEAH", "Pass. Group[0]=(0,14)");
                yield return (@"(.*)c(.*)", RegexOptions.IgnoreCase, "ABCDE", "Pass. Group[0]=(0,5) Group[1]=(0,2) Group[2]=(3,2)");
                yield return (@"\((.*), (.*)\)", RegexOptions.IgnoreCase, "(A, B)", "Pass. Group[0]=(0,6) Group[1]=(1,1) Group[2]=(4,1)");
                yield return (@"[k]", RegexOptions.IgnoreCase, "AB", "Fail.");
                yield return (@"abcd", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4)");
                yield return (@"a(bc)d", RegexOptions.IgnoreCase, "ABCD", "Pass. Group[0]=(0,4) Group[1]=(1,2)");
                yield return (@"a[-]?c", RegexOptions.IgnoreCase, "AC", "Pass. Group[0]=(0,2)");
                yield return (@"a(?:b|c|d)(.)", RegexOptions.None, "ace", "Pass. Group[0]=(0,3) Group[1]=(2,1)");
                yield return (@"a(?:b|c|d)*(.)", RegexOptions.None, "ace", "Pass. Group[0]=(0,3) Group[1]=(2,1)");
                yield return (@"a(?:b|c|d)+?(.)", RegexOptions.None, "ace", "Pass. Group[0]=(0,3) Group[1]=(2,1)");
                yield return (@"a(?:b|c|d)+?(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,3) Group[1]=(2,1)");
                yield return (@"a(?:b|c|d)+(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,8) Group[1]=(7,1)");
                yield return (@"a(?:b|c|d){2}(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,4) Group[1]=(3,1)");
                yield return (@"a(?:b|c|d){4,5}(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,7) Group[1]=(6,1)");
                yield return (@"a(?:b|c|d){4,5}?(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,6) Group[1]=(5,1)");
                yield return (@"((foo)|(bar))*", RegexOptions.None, "foobar", "Pass. Group[0]=(0,6) Group[1]=(0,3)(3,3) Group[2]=(0,3) Group[3]=(3,3)");
                yield return (@"a(?:b|c|d){6,7}(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,8) Group[1]=(7,1)");
                yield return (@"a(?:b|c|d){6,7}?(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,8) Group[1]=(7,1)");
                yield return (@"a(?:b|c|d){5,6}(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,8) Group[1]=(7,1)");
                yield return (@"a(?:b|c|d){5,6}?(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,7) Group[1]=(6,1)");
                yield return (@"a(?:b|c|d){5,7}(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,8) Group[1]=(7,1)");
                yield return (@"a(?:b|c|d){5,7}?(.)", RegexOptions.None, "acdbcdbe", "Pass. Group[0]=(0,7) Group[1]=(6,1)");
                yield return (@"a(?:b|(c|e){1,2}?|d)+?(.)", RegexOptions.None, "ace", "Pass. Group[0]=(0,3) Group[1]=(1,1) Group[2]=(2,1)");
                yield return (@"^(.+)?B", RegexOptions.None, "AB", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"^([^a-z])|(\^)$", RegexOptions.None, ".", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=");
                yield return (@"^[<>]&", RegexOptions.None, "<&OUT", "Pass. Group[0]=(0,2)");
                yield return (@"((a{4})+)", RegexOptions.None, "aaaaaaaaa", "Pass. Group[0]=(0,8) Group[1]=(0,8) Group[2]=(0,4)(4,4)");
                yield return (@"(((aa){2})+)", RegexOptions.None, "aaaaaaaaaa", "Pass. Group[0]=(0,8) Group[1]=(0,8) Group[2]=(0,4)(4,4) Group[3]=(0,2)(2,2)(4,2)(6,2)");
                yield return (@"(((a{2}){2})+)", RegexOptions.None, "aaaaaaaaaa", "Pass. Group[0]=(0,8) Group[1]=(0,8) Group[2]=(0,4)(4,4) Group[3]=(0,2)(2,2)(4,2)(6,2)");
                yield return (@"(?:(f)(o)(o)|(b)(a)(r))*", RegexOptions.None, "foobar", "Pass. Group[0]=(0,6) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(3,1) Group[5]=(4,1) Group[6]=(5,1)");
                yield return (@"(?:..)*a", RegexOptions.None, "aba", "Pass. Group[0]=(0,3)");
                yield return (@"(?:..)*?a", RegexOptions.None, "aba", "Pass. Group[0]=(0,1)");
                yield return (@"^(){3,5}", RegexOptions.None, "abc", "Pass. Group[0]=(0,0) Group[1]=(0,0)(0,0)(0,0)");
                yield return (@"^(a+)*ax", RegexOptions.None, "aax", "Pass. Group[0]=(0,3) Group[1]=(0,1)");
                yield return (@"^((a|b)+)*ax", RegexOptions.None, "aax", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(0,1)");
                yield return (@"^((a|bc)+)*ax", RegexOptions.None, "aax", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(0,1)");
                yield return (@"(a|x)*ab", RegexOptions.None, "cab", "Pass. Group[0]=(1,2) Group[1]=");
                yield return (@"(a)*ab", RegexOptions.None, "cab", "Pass. Group[0]=(1,2) Group[1]=");
                yield return (@"(?:(?i)a)b", RegexOptions.None, "ab", "Pass. Group[0]=(0,2)");
                yield return (@"((?i)a)b", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?:(?i)a)b", RegexOptions.None, "Ab", "Pass. Group[0]=(0,2)");
                yield return (@"((?i)a)b", RegexOptions.None, "Ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?:(?i)a)b", RegexOptions.None, "aB", "Fail.");
                yield return (@"((?i)a)b", RegexOptions.None, "aB", "Fail.");
                yield return (@"(?i:a)b", RegexOptions.None, "ab", "Pass. Group[0]=(0,2)");
                yield return (@"((?i:a))b", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?i:a)b", RegexOptions.None, "Ab", "Pass. Group[0]=(0,2)");
                yield return (@"((?i:a))b", RegexOptions.None, "Ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?i:a)b", RegexOptions.None, "aB", "Fail.");
                yield return (@"((?i:a))b", RegexOptions.None, "aB", "Fail.");
                yield return (@"(?:(?-i)a)b", RegexOptions.IgnoreCase, "ab", "Pass. Group[0]=(0,2)");
                yield return (@"((?-i)a)b", RegexOptions.IgnoreCase, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"((?-i)a)b", RegexOptions.IgnoreCase, "aB", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?:(?-i)a)b", RegexOptions.IgnoreCase, "Ab", "Fail.");
                yield return (@"((?-i)a)b", RegexOptions.IgnoreCase, "Ab", "Fail.");
                yield return (@"(?:(?-i)a)b", RegexOptions.IgnoreCase, "aB", "Pass. Group[0]=(0,2)");
                yield return (@"(?:(?-i)a)b", RegexOptions.IgnoreCase, "AB", "Fail.");
                yield return (@"((?-i)a)b", RegexOptions.IgnoreCase, "AB", "Fail.");
                yield return (@"(?-i:a)b", RegexOptions.IgnoreCase, "ab", "Pass. Group[0]=(0,2)");
                yield return (@"((?-i:a))b", RegexOptions.IgnoreCase, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?-i:a)b", RegexOptions.IgnoreCase, "aB", "Pass. Group[0]=(0,2)");
                yield return (@"((?-i:a))b", RegexOptions.IgnoreCase, "aB", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"(?-i:a)b", RegexOptions.IgnoreCase, "Ab", "Fail.");
                yield return (@"((?-i:a))b", RegexOptions.IgnoreCase, "Ab", "Fail.");
                yield return (@"(?-i:a)b", RegexOptions.IgnoreCase, "AB", "Fail.");
                yield return (@"((?-i:a))b", RegexOptions.IgnoreCase, "AB", "Fail.");
                yield return (@"((?-i:a.))b", RegexOptions.IgnoreCase, "a\nB", "Fail.");
                yield return (@"((?s-i:a.))b", RegexOptions.IgnoreCase, "a\nB", "Pass. Group[0]=(0,3) Group[1]=(0,2)");
                yield return (@"((?s-i:a.))b", RegexOptions.IgnoreCase, "B\nB", "Fail.");
                yield return (@"(?:c|d)(?:)(?:a(?:)(?:b)(?:b(?:))(?:b(?:)(?:b)))", RegexOptions.None, "cabbbb", "Pass. Group[0]=(0,6)");
                yield return (@"(?:c|d)(?:)(?:aaaaaaaa(?:)(?:bbbbbbbb)(?:bbbbbbbb(?:))(?:bbbbbbbb(?:)(?:bbbbbbbb)))", RegexOptions.None, "caaaaaaaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "Pass. Group[0]=(0,41)");
                yield return (@"foo\w*\d{4}baz", RegexOptions.None, "foobar1234baz", "Pass. Group[0]=(0,13)");
                yield return (@"x(~~)*(?:(?:F)?)?", RegexOptions.None, "x~~", "Pass. Group[0]=(0,3) Group[1]=(1,2)");
                yield return (@"^a(?#xxx){3}c", RegexOptions.None, "aaac", "Pass. Group[0]=(0,4)");
                yield return (@"^(?:a?b?)*$", RegexOptions.None, "a--", "Fail.");
                yield return (@"((?s)^a(.))((?m)^b$)", RegexOptions.None, "a\nb\nc\n", "Pass. Group[0]=(0,3) Group[1]=(0,2) Group[2]=(1,1) Group[3]=(2,1)");
                yield return (@"((?m)^b$)", RegexOptions.None, "a\nb\nc\n", "Pass. Group[0]=(2,1) Group[1]=(2,1)");
                yield return (@"(?m)^b", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(2,1)");
                yield return (@"(?m)^(b)", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(2,1) Group[1]=(2,1)");
                yield return (@"((?m)^b)", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(2,1) Group[1]=(2,1)");
                yield return (@"\n((?m)^b)", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(1,2) Group[1]=(2,1)");
                yield return (@"^b", RegexOptions.None, "a\nb\nc\n", "Fail.");
                yield return (@"()^b", RegexOptions.None, "a\nb\nc\n", "Fail.");
                yield return (@"((?m)^b)", RegexOptions.None, "a\nb\nc\n", "Pass. Group[0]=(2,1) Group[1]=(2,1)");
                yield return (@"([\w:]+::)?(\w+)$", RegexOptions.None, "abcd:", "Fail.");
                yield return (@"([\w:]+::)?(\w+)$", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]= Group[2]=(0,4)");
                yield return (@"([\w:]+::)?(\w+)$", RegexOptions.None, "xy:z:::abcd", "Pass. Group[0]=(0,11) Group[1]=(0,7) Group[2]=(7,4)");
                yield return (@"^[^bcd]*(c+)", RegexOptions.None, "aexycd", "Pass. Group[0]=(0,5) Group[1]=(4,1)");
                yield return (@"(a*)b+", RegexOptions.None, "caab", "Pass. Group[0]=(1,3) Group[1]=(1,2)");
                yield return (@"(>a+)ab", RegexOptions.None, "aaab", "Fail.");
                yield return (@"(\w+:)+", RegexOptions.None, "one:", "Pass. Group[0]=(0,4) Group[1]=(0,4)");
                yield return (@"([[:]+)", RegexOptions.None, "a:[b]:", "Pass. Group[0]=(1,2) Group[1]=(1,2)");
                yield return (@"([[=]+)", RegexOptions.None, "a=[b]=", "Pass. Group[0]=(1,2) Group[1]=(1,2)");
                yield return (@"([[.]+)", RegexOptions.None, "a.[b].", "Pass. Group[0]=(1,2) Group[1]=(1,2)");
                yield return (@"[a[:]b[:c]", RegexOptions.None, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"\Z", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(3,0)");
                yield return (@"\z", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(4,0)");
                yield return (@"$", RegexOptions.None, "a\nb\n", "Pass. Group[0]=(3,0)");
                yield return (@"\Z", RegexOptions.None, "b\na\n", "Pass. Group[0]=(3,0)");
                yield return (@"\z", RegexOptions.None, "b\na\n", "Pass. Group[0]=(4,0)");
                yield return (@"$", RegexOptions.None, "b\na\n", "Pass. Group[0]=(3,0)");
                yield return (@"\Z", RegexOptions.None, "b\na", "Pass. Group[0]=(3,0)");
                yield return (@"\z", RegexOptions.None, "b\na", "Pass. Group[0]=(3,0)");
                yield return (@"$", RegexOptions.None, "b\na", "Pass. Group[0]=(3,0)");
                yield return (@"\Z", RegexOptions.Multiline, "a\nb\n", "Pass. Group[0]=(3,0)");
                yield return (@"\z", RegexOptions.Multiline, "a\nb\n", "Pass. Group[0]=(4,0)");
                yield return (@"$", RegexOptions.Multiline, "a\nb\n", "Pass. Group[0]=(1,0)");
                yield return (@"\Z", RegexOptions.Multiline, "b\na\n", "Pass. Group[0]=(3,0)");
                yield return (@"\z", RegexOptions.Multiline, "b\na\n", "Pass. Group[0]=(4,0)");
                yield return (@"$", RegexOptions.Multiline, "b\na\n", "Pass. Group[0]=(1,0)");
                yield return (@"\Z", RegexOptions.Multiline, "b\na", "Pass. Group[0]=(3,0)");
                yield return (@"\z", RegexOptions.Multiline, "b\na", "Pass. Group[0]=(3,0)");
                yield return (@"$", RegexOptions.Multiline, "b\na", "Pass. Group[0]=(1,0)");
                yield return (@"a\Z", RegexOptions.None, "a\nb\n", "Fail.");
                yield return (@"a\z", RegexOptions.None, "a\nb\n", "Fail.");
                yield return (@"a$", RegexOptions.None, "a\nb\n", "Fail.");
                yield return (@"a\Z", RegexOptions.None, "b\na\n", "Pass. Group[0]=(2,1)");
                yield return (@"a\z", RegexOptions.None, "b\na\n", "Fail.");
                yield return (@"a$", RegexOptions.None, "b\na\n", "Pass. Group[0]=(2,1)");
                yield return (@"a\Z", RegexOptions.None, "b\na", "Pass. Group[0]=(2,1)");
                yield return (@"a\z", RegexOptions.None, "b\na", "Pass. Group[0]=(2,1)");
                yield return (@"a$", RegexOptions.None, "b\na", "Pass. Group[0]=(2,1)");
                yield return (@"a\z", RegexOptions.Multiline, "a\nb\n", "Fail.");
                yield return (@"a$", RegexOptions.Multiline, "a\nb\n", "Pass. Group[0]=(0,1)");
                yield return (@"a\Z", RegexOptions.Multiline, "b\na\n", "Pass. Group[0]=(2,1)");
                yield return (@"a\z", RegexOptions.Multiline, "b\na\n", "Fail.");
                yield return (@"a$", RegexOptions.Multiline, "b\na\n", "Pass. Group[0]=(2,1)");
                yield return (@"a\Z", RegexOptions.Multiline, "b\na", "Pass. Group[0]=(2,1)");
                yield return (@"a\z", RegexOptions.Multiline, "b\na", "Pass. Group[0]=(2,1)");
                yield return (@"a$", RegexOptions.Multiline, "b\na", "Pass. Group[0]=(2,1)");
                yield return (@"aa\Z", RegexOptions.None, "aa\nb\n", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "aa\nb\n", "Fail.");
                yield return (@"aa$", RegexOptions.None, "aa\nb\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.None, "b\naa\n", "Pass. Group[0]=(2,2)");
                yield return (@"aa\z", RegexOptions.None, "b\naa\n", "Fail.");
                yield return (@"aa$", RegexOptions.None, "b\naa\n", "Pass. Group[0]=(2,2)");
                yield return (@"aa\Z", RegexOptions.None, "b\naa", "Pass. Group[0]=(2,2)");
                yield return (@"aa\z", RegexOptions.None, "b\naa", "Pass. Group[0]=(2,2)");
                yield return (@"aa$", RegexOptions.None, "b\naa", "Pass. Group[0]=(2,2)");
                yield return (@"aa\z", RegexOptions.Multiline, "aa\nb\n", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "aa\nb\n", "Pass. Group[0]=(0,2)");
                yield return (@"aa\Z", RegexOptions.Multiline, "b\naa\n", "Pass. Group[0]=(2,2)");
                yield return (@"aa\z", RegexOptions.Multiline, "b\naa\n", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "b\naa\n", "Pass. Group[0]=(2,2)");
                yield return (@"aa\Z", RegexOptions.Multiline, "b\naa", "Pass. Group[0]=(2,2)");
                yield return (@"aa\z", RegexOptions.Multiline, "b\naa", "Pass. Group[0]=(2,2)");
                yield return (@"aa$", RegexOptions.Multiline, "b\naa", "Pass. Group[0]=(2,2)");
                yield return (@"aa\Z", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"aa$", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"aa$", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"aa$", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"aa\Z", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"aa\z", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"aa\z", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"aa\z", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"aa\Z", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"aa$", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"aa$", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"aa\z", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"aa$", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"aa\Z", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"aa\z", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"aa\z", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"aa\Z", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"aa\z", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"aa$", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "ab\nb\n", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "ab\nb\n", "Fail.");
                yield return (@"ab$", RegexOptions.None, "ab\nb\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "b\nab\n", "Pass. Group[0]=(2,2)");
                yield return (@"ab\z", RegexOptions.None, "b\nab\n", "Fail.");
                yield return (@"ab$", RegexOptions.None, "b\nab\n", "Pass. Group[0]=(2,2)");
                yield return (@"ab\Z", RegexOptions.None, "b\nab", "Pass. Group[0]=(2,2)");
                yield return (@"ab\z", RegexOptions.None, "b\nab", "Pass. Group[0]=(2,2)");
                yield return (@"ab$", RegexOptions.None, "b\nab", "Pass. Group[0]=(2,2)");
                yield return (@"ab\z", RegexOptions.Multiline, "ab\nb\n", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "ab\nb\n", "Pass. Group[0]=(0,2)");
                yield return (@"ab\Z", RegexOptions.Multiline, "b\nab\n", "Pass. Group[0]=(2,2)");
                yield return (@"ab\z", RegexOptions.Multiline, "b\nab\n", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "b\nab\n", "Pass. Group[0]=(2,2)");
                yield return (@"ab\Z", RegexOptions.Multiline, "b\nab", "Pass. Group[0]=(2,2)");
                yield return (@"ab\z", RegexOptions.Multiline, "b\nab", "Pass. Group[0]=(2,2)");
                yield return (@"ab$", RegexOptions.Multiline, "b\nab", "Pass. Group[0]=(2,2)");
                yield return (@"ab\Z", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"ab$", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"ab$", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"ab$", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"ab\Z", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"ab\z", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"ab\z", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"ab\z", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"ab$", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"ab$", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"ab\z", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"ab$", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"ab\Z", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"ab\z", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"ab\z", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"ab\Z", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"ab\z", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"ab$", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "abb\nb\n", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "abb\nb\n", "Fail.");
                yield return (@"abb$", RegexOptions.None, "abb\nb\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "b\nabb\n", "Pass. Group[0]=(2,3)");
                yield return (@"abb\z", RegexOptions.None, "b\nabb\n", "Fail.");
                yield return (@"abb$", RegexOptions.None, "b\nabb\n", "Pass. Group[0]=(2,3)");
                yield return (@"abb\Z", RegexOptions.None, "b\nabb", "Pass. Group[0]=(2,3)");
                yield return (@"abb\z", RegexOptions.None, "b\nabb", "Pass. Group[0]=(2,3)");
                yield return (@"abb$", RegexOptions.None, "b\nabb", "Pass. Group[0]=(2,3)");
                yield return (@"abb\z", RegexOptions.Multiline, "abb\nb\n", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "abb\nb\n", "Pass. Group[0]=(0,3)");
                yield return (@"abb\Z", RegexOptions.Multiline, "b\nabb\n", "Pass. Group[0]=(2,3)");
                yield return (@"abb\z", RegexOptions.Multiline, "b\nabb\n", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "b\nabb\n", "Pass. Group[0]=(2,3)");
                yield return (@"abb\Z", RegexOptions.Multiline, "b\nabb", "Pass. Group[0]=(2,3)");
                yield return (@"abb\z", RegexOptions.Multiline, "b\nabb", "Pass. Group[0]=(2,3)");
                yield return (@"abb$", RegexOptions.Multiline, "b\nabb", "Pass. Group[0]=(2,3)");
                yield return (@"abb\Z", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"abb$", RegexOptions.None, "ac\nb\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"abb$", RegexOptions.None, "b\nac\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"abb$", RegexOptions.None, "b\nac", "Fail.");
                yield return (@"abb\Z", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"abb\z", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "ac\nb\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"abb\z", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "b\nac\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"abb\z", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "b\nac", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"abb$", RegexOptions.None, "ca\nb\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"abb$", RegexOptions.None, "b\nca\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"abb\z", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"abb$", RegexOptions.None, "b\nca", "Fail.");
                yield return (@"abb\Z", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"abb\z", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "ca\nb\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"abb\z", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "b\nca\n", "Fail.");
                yield return (@"abb\Z", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"abb\z", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"abb$", RegexOptions.Multiline, "b\nca", "Fail.");
                yield return (@"(^|x)(c)", RegexOptions.None, "ca", "Pass. Group[0]=(0,1) Group[1]=(0,0) Group[2]=(0,1)");
                yield return (@"a*abc?xyz+pqr{3}ab{2,}xy{4,5}pq{0,6}AB{0,}zz", RegexOptions.None, "x", "Fail.");
                yield return (@"foo.bart", RegexOptions.None, "foo.bart", "Pass. Group[0]=(0,8)");
                yield return (@"^d[x][x][x]", RegexOptions.Multiline, "abcd\ndxxx", "Pass. Group[0]=(5,4)");
                yield return (@".X(.+)+X", RegexOptions.None, "bbbbXcXaaaaaaaa", "Pass. Group[0]=(3,4) Group[1]=(5,1)");
                yield return (@".X(.+)+XX", RegexOptions.None, "bbbbXcXXaaaaaaaa", "Pass. Group[0]=(3,5) Group[1]=(5,1)");
                yield return (@".XX(.+)+X", RegexOptions.None, "bbbbXXcXaaaaaaaa", "Pass. Group[0]=(3,5) Group[1]=(6,1)");
                yield return (@".X(.+)+X", RegexOptions.None, "bbbbXXaaaaaaaaa", "Fail.");
                yield return (@".X(.+)+XX", RegexOptions.None, "bbbbXXXaaaaaaaaa", "Fail.");
                yield return (@".XX(.+)+X", RegexOptions.None, "bbbbXXXaaaaaaaaa", "Fail.");
                yield return (@".X(.+)+[X]", RegexOptions.None, "bbbbXcXaaaaaaaa", "Pass. Group[0]=(3,4) Group[1]=(5,1)");
                yield return (@".X(.+)+[X][X]", RegexOptions.None, "bbbbXcXXaaaaaaaa", "Pass. Group[0]=(3,5) Group[1]=(5,1)");
                yield return (@".XX(.+)+[X]", RegexOptions.None, "bbbbXXcXaaaaaaaa", "Pass. Group[0]=(3,5) Group[1]=(6,1)");
                yield return (@".X(.+)+[X]", RegexOptions.None, "bbbbXXaaaaaaaaa", "Fail.");
                yield return (@".X(.+)+[X][X]", RegexOptions.None, "bbbbXXXaaaaaaaaa", "Fail.");
                yield return (@".XX(.+)+[X]", RegexOptions.None, "bbbbXXXaaaaaaaaa", "Fail.");
                yield return (@".[X](.+)+[X]", RegexOptions.None, "bbbbXcXaaaaaaaa", "Pass. Group[0]=(3,4) Group[1]=(5,1)");
                yield return (@".[X](.+)+[X][X]", RegexOptions.None, "bbbbXcXXaaaaaaaa", "Pass. Group[0]=(3,5) Group[1]=(5,1)");
                yield return (@".[X][X](.+)+[X]", RegexOptions.None, "bbbbXXcXaaaaaaaa", "Pass. Group[0]=(3,5) Group[1]=(6,1)");
                yield return (@".[X](.+)+[X]", RegexOptions.None, "bbbbXXaaaaaaaaa", "Fail.");
                yield return (@".[X](.+)+[X][X]", RegexOptions.None, "bbbbXXXaaaaaaaaa", "Fail.");
                yield return (@".[X][X](.+)+[X]", RegexOptions.None, "bbbbXXXaaaaaaaaa", "Fail.");
                yield return (@"tt+$", RegexOptions.None, "xxxtt", "Pass. Group[0]=(3,2)");
                yield return (@"([\d-z]+)", RegexOptions.None, "a0-za", "Pass. Group[0]=(1,3) Group[1]=(1,3)");
                yield return (@"([\d-\s]+)", RegexOptions.None, "a0- z", "Pass. Group[0]=(1,3) Group[1]=(1,3)");
                yield return (@"(\d+\.\d+)", RegexOptions.None, "3.1415926", "Pass. Group[0]=(0,9) Group[1]=(0,9)");
                yield return (@"(\ba.{0,10}br)", RegexOptions.None, "have a web browser", "Pass. Group[0]=(5,8) Group[1]=(5,8)");
                yield return (@"\.c(pp|xx|c)?$", RegexOptions.IgnoreCase, "Changes", "Fail.");
                yield return (@"\.c(pp|xx|c)?$", RegexOptions.IgnoreCase, "IO.c", "Pass. Group[0]=(2,2) Group[1]=");
                yield return (@"(\.c(pp|xx|c)?$)", RegexOptions.IgnoreCase, "IO.c", "Pass. Group[0]=(2,2) Group[1]=(2,2) Group[2]=");
                yield return (@"^([a-z]:)", RegexOptions.None, "C:/", "Fail.");
                yield return (@"^\S\s+aa$", RegexOptions.Multiline, "\nx aa", "Pass. Group[0]=(1,4)");
                yield return (@"(^|a)b", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                yield return (@"^([ab]*?)(b)?(c)$", RegexOptions.None, "abac", "Pass. Group[0]=(0,4) Group[1]=(0,3) Group[2]= Group[3]=(3,1)");
                yield return (@"^(?:.,){2}c", RegexOptions.None, "a,b,c", "Pass. Group[0]=(0,5)");
                yield return (@"^(.,){2}c", RegexOptions.None, "a,b,c", "Pass. Group[0]=(0,5) Group[1]=(0,2)(2,2)");
                yield return (@"^(?:[^,]*,){2}c", RegexOptions.None, "a,b,c", "Pass. Group[0]=(0,5)");
                yield return (@"^([^,]*,){2}c", RegexOptions.None, "a,b,c", "Pass. Group[0]=(0,5) Group[1]=(0,2)(2,2)");
                yield return (@"^([^,]*,){3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]*,){3,}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]*,){0,3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{1,3},){3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{1,3},){3,}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{1,3},){0,3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{1,},){3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{1,},){3,}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{1,},){0,3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{0,3},){3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{0,3},){3,}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"^([^,]{0,3},){0,3}d", RegexOptions.None, "aaa,b,c,d", "Pass. Group[0]=(0,9) Group[1]=(0,4)(4,2)(6,2)");
                yield return (@"(?i)", RegexOptions.None, "", "Pass. Group[0]=(0,0)");
                yield return (@"^(a(b)?)+$", RegexOptions.None, "aba", "Pass. Group[0]=(0,3) Group[1]=(0,2)(2,1) Group[2]=(1,1)");
                yield return (@"^(aa(bb)?)+$", RegexOptions.None, "aabbaa", "Pass. Group[0]=(0,6) Group[1]=(0,4)(4,2) Group[2]=(2,2)");
                yield return (@"^.{9}abc.*\n", RegexOptions.Multiline, "123\nabcabcabcabc\n", "Pass. Group[0]=(4,13)");
                yield return (@"^(a)?a$", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=");
                yield return (@"^(0+)?(?:x(1))?", RegexOptions.None, "x1", "Pass. Group[0]=(0,2) Group[1]= Group[2]=(1,1)");
                yield return (@"^([0-9a-fA-F]+)(?:x([0-9a-fA-F]+)?)(?:x([0-9a-fA-F]+))?", RegexOptions.None, "012cxx0190", "Pass. Group[0]=(0,10) Group[1]=(0,4) Group[2]= Group[3]=(6,4)");
                yield return (@"^(b+?|a){1,2}c", RegexOptions.None, "bbbac", "Pass. Group[0]=(0,5) Group[1]=(0,3)(3,1)");
                yield return (@"^(b+?|a){1,2}c", RegexOptions.None, "bbbbac", "Pass. Group[0]=(0,6) Group[1]=(0,4)(4,1)");
                yield return (@"\((\w\. \w+)\)", RegexOptions.None, "cd. (A. Tw)", "Pass. Group[0]=(4,7) Group[1]=(5,5)");
                yield return (@"((?:aaaa|bbbb)cccc)?", RegexOptions.None, "aaaacccc", "Pass. Group[0]=(0,8) Group[1]=(0,8)");
                yield return (@"((?:aaaa|bbbb)cccc)?", RegexOptions.None, "bbbbcccc", "Pass. Group[0]=(0,8) Group[1]=(0,8)");
                yield return (@"^(foo)|(bar)$", RegexOptions.None, "foobar", "Pass. Group[0]=(0,3) Group[1]=(0,3) Group[2]=");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[n]", "Pass. Group[0]=(0,3) Group[1]=(1,1)");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "n", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "n[i]e", "Fail.");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[n", "Fail.");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "]n]", "Fail.");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, @"\[n\]", "Fail.");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, @"[n\]", "Pass. Group[0]=(0,4) Group[1]=(1,2)");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, @"[n\[]", "Pass. Group[0]=(0,5) Group[1]=(1,3)");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, @"[[n]", "Pass. Group[0]=(0,4) Group[1]=(1,2)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[s] . [n]", "Pass. Group[0]=(0,9) Group[1]=(1,1) Group[2]=(7,1)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[s] . n", "Pass. Group[0]=(0,7) Group[1]=(1,1) Group[2]=(6,1)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "s.[ n ]", "Pass. Group[0]=(0,7) Group[1]=(0,1) Group[2]=(3,3)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, " . n", "Pass. Group[0]=(0,4) Group[1]=(0,1) Group[2]=(3,1)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "s. ", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(2,1)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[.]. ", "Pass. Group[0]=(0,5) Group[1]=(1,1) Group[2]=(4,1)");
                yield return (@"^((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[c].[s].[n]", "Pass. Group[0]=(0,11) Group[1]=(1,1) Group[2]=(5,1) Group[3]=(9,1)");
                yield return (@"^((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, " c . s . n ", "Pass. Group[0]=(0,11) Group[1]=(0,3) Group[2]=(5,2) Group[3]=(9,2)");
                yield return (@"^((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, " . [.] . [ ]", "Pass. Group[0]=(0,12) Group[1]=(0,1) Group[2]=(4,1) Group[3]=(10,1)");
                yield return (@"^((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "c.n", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(2,1)");
                yield return (@"^((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[c] .[n]", "Pass. Group[0]=(0,8) Group[1]=(1,1) Group[2]=(6,1)");
                yield return (@"^((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "c.n.", "Fail.");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "s.c.n", "Pass. Group[0]=(0,5) Group[1]=(0,1) Group[2]=(2,1) Group[3]=(4,1)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[s].[c].[n]", "Pass. Group[0]=(0,11) Group[1]=(1,1) Group[2]=(5,1) Group[3]=(9,1)");
                yield return (@"^((\[(?<SCHEMA>[^\]]+)\])|(?<SCHEMA>[^\.\[\]]+))\s*\.\s*((\[(?<CATALOG>[^\]]+)\])|(?<CATALOG>[^\.\[\]]+))\s*\.\s*((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[s].[c].", "Fail.");
                yield return (@"^((\[(?<ColName>.+)\])|(?<ColName>\S+))([ ]+(?<Order>ASC|DESC))?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "[id]]", "Pass. Group[0]=(0,5) Group[1]=(1,3) Group[2]=");
                yield return (@"a{1,2147483647}", RegexOptions.None, "a", "Pass. Group[0]=(0,1)");
                yield return (@"^((\[(?<NAME>[^\]]+)\])|(?<NAME>[^\.\[\]]+))$", RegexOptions.None, "[a]", "Pass. Group[0]=(0,3) Group[1]=(0,3) Group[2]=(0,3) Group[3]=(1,1)");

                // Ported from https://github.com/mono/mono/blob/0f2995e95e98e082c7c7039e17175cf2c6a00034/mcs/class/System/Test/System.Text.RegularExpressions/RegexMatchTests.cs
                yield return (@"(a)(?<1>b)(?'1'c)", RegexOptions.ExplicitCapture, "abc", "Pass. Group[0]=(0,3) Group[1]=(1,1)(2,1)");
                yield return (@"(a)(b)(c)", RegexOptions.ExplicitCapture, "abc", "Pass. Group[0]=(0,3)");
                yield return (@"(a)(?<1>b)(c)", RegexOptions.ExplicitCapture, "abc", "Pass. Group[0]=(0,3) Group[1]=(1,1)");
                yield return (@"(a)(?<2>b)(c)", RegexOptions.None, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(1,1)(2,1)");
                yield return (@"(a)(?<foo>b)(c)", RegexOptions.ExplicitCapture, "abc", "Pass. Group[0]=(0,3) Group[1]=(1,1)");
                yield return (@"\P{IsHebrew}", RegexOptions.None, "\u05D0a", "Pass. Group[0]=(1,1)");
                yield return (@"\p{IsHebrew}", RegexOptions.None, "abc\u05D0def", "Pass. Group[0]=(3,1)");
                yield return (@"\4400", RegexOptions.None, "asdf 012", "Pass. Group[0]=(4,2)");
                yield return (@"\4400", RegexOptions.None, "asdf$0012", "Fail.");
                yield return (@"(?<2>ab)(?<c>c)(?<d>d)", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(2,1) Group[2]=(0,2) Group[3]=(3,1)");// 61
                yield return (@"(?<1>ab)(c)", RegexOptions.None, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,2)(2,1)");
                yield return (@"(?<44>a)", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[44]=(0,1)");
                yield return (@"(?<44>a)(?<8>b)", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[8]=(1,1) Group[44]=(0,1)");
                yield return (@"(?<44>a)(?<8>b)(?<1>c)(d)", RegexOptions.None, "abcd", "Pass. Group[0]=(0,4) Group[1]=(2,1)(3,1) Group[8]=(1,1) Group[44]=(0,1)");
                yield return (@"(?<44>a)(?<44>b)", RegexOptions.None, "ab", "Pass. Group[0]=(0,2) Group[44]=(0,1)(1,1)");
                yield return (@"(?<44>a)\440", RegexOptions.None, "a ", "Pass. Group[0]=(0,2) Group[44]=(0,1)");
                yield return (@"(?<44>a)\440", RegexOptions.None, "aa0", "Fail.");

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    yield return (@"((((((((((a))))))))))\10", RegexOptions.None, "aa", "Pass. Group[0]=(0,2) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                    yield return (@"(abc)\1", RegexOptions.None, "abcabc", "Pass. Group[0]=(0,6) Group[1]=(0,3)");
                    yield return (@"([a-c]*)\1", RegexOptions.None, "abcabc", "Pass. Group[0]=(0,6) Group[1]=(0,3)");
                    yield return (@"(a)|\1", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                    yield return (@"(a)|\1", RegexOptions.None, "x", "Fail.");
                    yield return (@"(([a-c])b*?\2)*", RegexOptions.None, "ababbbcbc", "Pass. Group[0]=(0,5) Group[1]=(0,3)(3,2) Group[2]=(0,1)(3,1)");
                    yield return (@"(([a-c])b*?\2){3}", RegexOptions.None, "ababbbcbc", "Pass. Group[0]=(0,9) Group[1]=(0,3)(3,3)(6,3) Group[2]=(0,1)(3,1)(6,1)");
                    yield return (@"((\3|b)\2(a)x)+", RegexOptions.None, "aaxabxbaxbbx", "Fail.");
                    yield return (@"((\3|b)\2(a)x)+", RegexOptions.None, "aaaxabaxbaaxbbax", "Pass. Group[0]=(12,4) Group[1]=(12,4) Group[2]=(12,1) Group[3]=(14,1)");
                    yield return (@"((\3|b)\2(a)){2,}", RegexOptions.None, "bbaababbabaaaaabbaaaabba", "Pass. Group[0]=(15,9) Group[1]=(15,3)(18,3)(21,3) Group[2]=(15,1)(18,1)(21,1) Group[3]=(17,1)(20,1)(23,1)");
                    yield return (@"((((((((((a))))))))))\10", RegexOptions.IgnoreCase, "AA", "Pass. Group[0]=(0,2) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                    yield return (@"(abc)\1", RegexOptions.IgnoreCase, "ABCABC", "Pass. Group[0]=(0,6) Group[1]=(0,3)");
                    yield return (@"([a-c]*)\1", RegexOptions.IgnoreCase, "ABCABC", "Pass. Group[0]=(0,6) Group[1]=(0,3)");
                    yield return (@"a(?!b).", RegexOptions.None, "abad", "Pass. Group[0]=(2,2)");
                    yield return (@"a(?=d).", RegexOptions.None, "abad", "Pass. Group[0]=(2,2)");
                    yield return (@"a(?=c|d).", RegexOptions.None, "abad", "Pass. Group[0]=(2,2)");
                    yield return (@"^(a\1?){4}$", RegexOptions.None, "aaaaaaaaaa", "Pass. Group[0]=(0,10) Group[1]=(0,1)(1,2)(3,3)(6,4)");
                    yield return (@"^(a\1?){4}$", RegexOptions.None, "aaaaaaaaa", "Fail.");
                    yield return (@"^(a\1?){4}$", RegexOptions.None, "aaaaaaaaaaa", "Fail.");
                    yield return (@"^(a(?(1)\1)){4}$", RegexOptions.None, "aaaaaaaaaa", "Pass. Group[0]=(0,10) Group[1]=(0,1)(1,2)(3,3)(6,4)");
                    yield return (@"^(a(?(1)\1)){4}$", RegexOptions.None, "aaaaaaaaa", "Fail.");
                    yield return (@"^(a(?(1)\1)){4}$", RegexOptions.None, "aaaaaaaaaaa", "Fail.");
                    yield return (@"(?<=a)b", RegexOptions.None, "ab", "Pass. Group[0]=(1,1)");
                    yield return (@"(?<=a)b", RegexOptions.None, "cb", "Fail.");
                    yield return (@"(?<=a)b", RegexOptions.None, "b", "Fail.");
                    yield return (@"(?<!c)b", RegexOptions.None, "ab", "Pass. Group[0]=(1,1)");
                    yield return (@"(?<!c)b", RegexOptions.None, "cb", "Fail.");
                    yield return (@"(?<!c)b", RegexOptions.None, "b", "Pass. Group[0]=(0,1)");
                    yield return (@"^(?:b|a(?=(.)))*\1", RegexOptions.None, "abc", "Pass. Group[0]=(0,2) Group[1]=(1,1)");
                    yield return (@"(ab)\d\1", RegexOptions.IgnoreCase, "Ab4ab", "Pass. Group[0]=(0,5) Group[1]=(0,2)");
                    yield return (@"(ab)\d\1", RegexOptions.IgnoreCase, "ab4Ab", "Pass. Group[0]=(0,5) Group[1]=(0,2)");
                    yield return (@"(?<![cd])b", RegexOptions.None, "dbcb", "Fail.");
                    yield return (@"(?<![cd])[ab]", RegexOptions.None, "dbaacb", "Pass. Group[0]=(2,1)");
                    yield return (@"(?<!(c|d))b", RegexOptions.None, "dbcb", "Fail.");
                    yield return (@"(?<!(c|d))[ab]", RegexOptions.None, "dbaacb", "Pass. Group[0]=(2,1) Group[1]=");
                    yield return (@"(?<!cd)[ab]", RegexOptions.None, "cdaccb", "Pass. Group[0]=(5,1)");
                    yield return (@"((?s).)c(?!.)", RegexOptions.None, "a\nb\nc\n", "Pass. Group[0]=(3,2) Group[1]=(3,1)");
                    yield return (@"((?s)b.)c(?!.)", RegexOptions.None, "a\nb\nc\n", "Pass. Group[0]=(2,3) Group[1]=(2,2)");
                    yield return (@"(x)?(?(1)a|b)", RegexOptions.None, "a", "Fail.");
                    yield return (@"(x)?(?(1)b|a)", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=");
                    yield return (@"()?(?(1)b|a)", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=");
                    yield return (@"()(?(1)b|a)", RegexOptions.None, "a", "Fail.");
                    yield return (@"()?(?(1)a|b)", RegexOptions.None, "a", "Pass. Group[0]=(0,1) Group[1]=(0,0)");
                    yield return (@"^(\()?blah(?(1)(\)))$", RegexOptions.None, "(blah)", "Pass. Group[0]=(0,6) Group[1]=(0,1) Group[2]=(5,1)");
                    yield return (@"^(\()?blah(?(1)(\)))$", RegexOptions.None, "blah", "Pass. Group[0]=(0,4) Group[1]= Group[2]=");
                    yield return (@"^(\()?blah(?(1)(\)))$", RegexOptions.None, "blah)", "Fail.");
                    yield return (@"^(\()?blah(?(1)(\)))$", RegexOptions.None, "(blah", "Fail.");
                    yield return (@"^(\(+)?blah(?(1)(\)))$", RegexOptions.None, "(blah)", "Pass. Group[0]=(0,6) Group[1]=(0,1) Group[2]=(5,1)");
                    yield return (@"^(\(+)?blah(?(1)(\)))$", RegexOptions.None, "blah", "Pass. Group[0]=(0,4) Group[1]= Group[2]=");
                    yield return (@"^(\(+)?blah(?(1)(\)))$", RegexOptions.None, "blah)", "Fail.");
                    yield return (@"^(\(+)?blah(?(1)(\)))$", RegexOptions.None, "(blah", "Fail.");
                    yield return (@"(?(?!a)a|b)", RegexOptions.None, "a", "Fail.");
                    yield return (@"(?(?!a)b|a)", RegexOptions.None, "a", "Pass. Group[0]=(0,1)");
                    yield return (@"(?(?=a)b|a)", RegexOptions.None, "a", "Fail.");
                    yield return (@"(?(?=a)a|b)", RegexOptions.None, "a", "Pass. Group[0]=(0,1)");
                    yield return (@"(?=(a+?))(\1ab)", RegexOptions.None, "aaab", "Pass. Group[0]=(1,3) Group[1]=(1,1) Group[2]=(1,3)");
                    yield return (@"^(?=(a+?))\1ab", RegexOptions.None, "aaab", "Fail.");
                    yield return (@"$(?<=^(a))", RegexOptions.None, "a", "Pass. Group[0]=(1,0) Group[1]=(0,1)");
                    yield return (@"(?>a+)b", RegexOptions.None, "aaab", "Pass. Group[0]=(0,4)");
                    yield return (@"((?>a+)b)", RegexOptions.None, "aaab", "Pass. Group[0]=(0,4) Group[1]=(0,4)");
                    yield return (@"(?>(a+))b", RegexOptions.None, "aaab", "Pass. Group[0]=(0,4) Group[1]=(0,3)");
                    yield return (@"((?>[^()]+)|\([^()]*\))+", RegexOptions.None, "((abc(ade)ufh()()x", "Pass. Group[0]=(2,16) Group[1]=(2,3)(5,5)(10,3)(13,2)(15,2)(17,1)");
                    yield return (@"(?<=x+)", RegexOptions.None, "xxxxy", "Pass. Group[0]=(1,0)");
                    yield return (@"round\(((?>[^()]+))\)", RegexOptions.None, "_I(round(xs * sz),1)", "Pass. Group[0]=(3,14) Group[1]=(9,7)");
                    yield return (@"(\w)?(abc)\1b", RegexOptions.None, "abcab", "Fail.");
                    yield return (@"\GX.*X", RegexOptions.None, "aaaXbX", "Fail.");
                    yield return (@"(?!\A)x", RegexOptions.Multiline, "a\nxb\n", "Pass. Group[0]=(2,1)");
                    yield return (@"^(a)?(?(1)a|b)+$", RegexOptions.None, "a", "Fail.");
                    yield return (@"^(a\1?)(a\1?)(a\2?)(a\3?)$", RegexOptions.None, "aaaaaa", "Pass. Group[0]=(0,6) Group[1]=(0,1) Group[2]=(1,2) Group[3]=(3,1) Group[4]=(4,2)");
                    yield return (@"^(a\1?){4}$", RegexOptions.None, "aaaaaa", "Pass. Group[0]=(0,6) Group[1]=(0,1)(1,2)(3,1)(4,2)");
                    yield return (@"\((?>[^()]+|\((?<depth>)|\)(?<-depth>))*(?(depth)(?!))\)", RegexOptions.None, "((a(b))c)", "Pass. Group[0]=(0,9) Group[1]=");
                    yield return (@"^\((?>[^()]+|\((?<depth>)|\)(?<-depth>))*(?(depth)(?!))\)$", RegexOptions.None, "((a(b))c)", "Pass. Group[0]=(0,9) Group[1]=");
                    yield return (@"^\((?>[^()]+|\((?<depth>)|\)(?<-depth>))*(?(depth)(?!))\)$", RegexOptions.None, "((a(b))c", "Fail.");
                    yield return (@"^\((?>[^()]+|\((?<depth>)|\)(?<-depth>))*(?(depth)(?!))\)$", RegexOptions.None, "())", "Fail.");
                    yield return (@"(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))", RegexOptions.None, "((a(b))c)", "Pass. Group[0]=(0,9) Group[1]=(0,9) Group[2]=(0,1)(1,2)(3,2) Group[3]=(5,1)(6,2)(8,1) Group[4]= Group[5]=(4,1)(2,4)(1,7)");
                    yield return (@"^(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))$", RegexOptions.None, "((a(b))c)", "Pass. Group[0]=(0,9) Group[1]=(0,9) Group[2]=(0,1)(1,2)(3,2) Group[3]=(5,1)(6,2)(8,1) Group[4]= Group[5]=(4,1)(2,4)(1,7)");
                    yield return (@"(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))", RegexOptions.None, "x(a((b)))b)x", "Pass. Group[0]=(1,9) Group[1]=(1,9) Group[2]=(1,2)(3,1)(4,2) Group[3]=(6,1)(7,1)(8,2) Group[4]= Group[5]=(5,1)(4,3)(2,6)");
                    yield return (@"(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))", RegexOptions.None, "x((a((b)))x", "Pass. Group[0]=(2,9) Group[1]=(2,9) Group[2]=(2,2)(4,1)(5,2) Group[3]=(7,1)(8,1)(9,2) Group[4]= Group[5]=(6,1)(5,3)(3,6)");
                    yield return (@"^(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))$", RegexOptions.None, "((a(b))c", "Fail.");
                    yield return (@"^(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))$", RegexOptions.None, "((a(b))c))", "Fail.");
                    yield return (@"^(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))$", RegexOptions.None, ")(", "Fail.");
                    yield return (@"^(((?<foo>\()[^()]*)+((?<bar-foo>\))[^()]*)+)+(?(foo)(?!))$", RegexOptions.None, "((a((b))c)", "Fail.");

                    yield return (@"\1", RegexOptions.ECMAScript, "-", "Fail.");
                    yield return (@"\2", RegexOptions.ECMAScript, "-", "Fail.");
                    yield return (@"(a)|\2", RegexOptions.ECMAScript, "-", "Fail.");
                    yield return (@"\4400", RegexOptions.ECMAScript, "asdf 012", "Fail.");
                    yield return (@"\4400", RegexOptions.ECMAScript, "asdf$0012", "Pass. Group[0]=(4,3)");
                    yield return (@"(?<44>a)\440", RegexOptions.ECMAScript, "a ", "Fail.");
                    yield return (@"(?<44>a)\440", RegexOptions.ECMAScript, "aa0", "Pass. Group[0]=(0,3) Group[44]=(0,1)");

                    yield return (@"^(foo)|(bar)$", RegexOptions.RightToLeft, "foobar", "Pass. Group[0]=(3,3) Group[1]= Group[2]=(3,3)");
                    yield return (@"b", RegexOptions.RightToLeft, "babaaa", "Pass. Group[0]=(2,1)");
                    yield return (@"bab", RegexOptions.RightToLeft, "babababaa", "Pass. Group[0]=(4,3)");
                    yield return (@"abb", RegexOptions.RightToLeft, "abb", "Pass. Group[0]=(0,3)");
                    yield return (@"b$", RegexOptions.RightToLeft | RegexOptions.Multiline, "aab\naab", "Pass. Group[0]=(6,1)");
                    yield return (@"^a", RegexOptions.RightToLeft | RegexOptions.Multiline, "aab\naab", "Pass. Group[0]=(4,1)");
                    yield return (@"^aaab", RegexOptions.RightToLeft | RegexOptions.Multiline, "aaab\naab", "Pass. Group[0]=(0,4)");
                    yield return (@"abb{2}", RegexOptions.RightToLeft, "abbb", "Pass. Group[0]=(0,4)");
                    yield return (@"abb{1,2}", RegexOptions.RightToLeft, "abbb", "Pass. Group[0]=(0,4)");
                    yield return (@"abb{1,2}", RegexOptions.RightToLeft, "abbbbbaaaa", "Pass. Group[0]=(0,4)");
                    yield return (@"\Ab", RegexOptions.RightToLeft, "bab\naaa", "Pass. Group[0]=(0,1)");
                    yield return (@"\Abab$", RegexOptions.RightToLeft, "bab", "Pass. Group[0]=(0,3)");
                    yield return (@"b\Z", RegexOptions.RightToLeft, "bab\naaa", "Fail.");
                    yield return (@"b\Z", RegexOptions.RightToLeft, "babaaab", "Pass. Group[0]=(6,1)");
                    yield return (@"b\z", RegexOptions.RightToLeft, "babaaa", "Fail.");
                    yield return (@"b\z", RegexOptions.RightToLeft, "babaaab", "Pass. Group[0]=(6,1)");
                    yield return (@"a\G", RegexOptions.RightToLeft, "babaaa", "Pass. Group[0]=(5,1)");
                    yield return (@"\Abaaa\G", RegexOptions.RightToLeft, "baaa", "Pass. Group[0]=(0,4)");
                    yield return (@"\bc", RegexOptions.RightToLeft, "aaa c aaa c a", "Pass. Group[0]=(10,1)");
                    yield return (@"\bc", RegexOptions.RightToLeft, "c aaa c", "Pass. Group[0]=(6,1)");
                    yield return (@"\bc", RegexOptions.RightToLeft, "aaa ac", "Fail.");
                    yield return (@"\bc", RegexOptions.RightToLeft, "c aaa", "Pass. Group[0]=(0,1)");
                    yield return (@"\bc", RegexOptions.RightToLeft, "aaacaaa", "Fail.");
                    yield return (@"\bc", RegexOptions.RightToLeft, "aaac aaa", "Fail.");
                    yield return (@"\bc", RegexOptions.RightToLeft, "aaa ca caaa", "Pass. Group[0]=(7,1)");
                    yield return (@"\Bc", RegexOptions.RightToLeft, "ac aaa ac", "Pass. Group[0]=(8,1)");
                    yield return (@"\Bc", RegexOptions.RightToLeft, "aaa c", "Fail.");
                    yield return (@"\Bc", RegexOptions.RightToLeft, "ca aaa", "Fail.");
                    yield return (@"\Bc", RegexOptions.RightToLeft, "aaa c aaa", "Fail.");
                    yield return (@"\Bc", RegexOptions.RightToLeft, " acaca ", "Pass. Group[0]=(4,1)");
                    yield return (@"\Bc", RegexOptions.RightToLeft, "aaac aaac", "Pass. Group[0]=(8,1)");
                    yield return (@"\Bc", RegexOptions.RightToLeft, "aaa caaa", "Fail.");
                    yield return (@"b(a?)b", RegexOptions.RightToLeft, "aabababbaaababa", "Pass. Group[0]=(11,3) Group[1]=(12,1)");
                    yield return (@"b{4}", RegexOptions.RightToLeft, "abbbbaabbbbaabbb", "Pass. Group[0]=(7,4)");
                    yield return (@"b\1aa(.)", RegexOptions.RightToLeft, "bBaaB", "Pass. Group[0]=(0,5) Group[1]=(4,1)");
                    yield return (@"b(.)aa\1", RegexOptions.RightToLeft, "bBaaB", "Fail.");
                    yield return (@"^(a\1?){4}$", RegexOptions.RightToLeft, "aaaaaa", "Pass. Group[0]=(0,6) Group[1]=(5,1)(3,2)(2,1)(0,2)");
                    yield return (@"^([0-9a-fA-F]+)(?:x([0-9a-fA-F]+)?)(?:x([0-9a-fA-F]+))?", RegexOptions.RightToLeft, "012cxx0190", "Pass. Group[0]=(0,10) Group[1]=(0,4) Group[2]= Group[3]=(6,4)");
                    yield return (@"^(b+?|a){1,2}c", RegexOptions.RightToLeft, "bbbac", "Pass. Group[0]=(0,5) Group[1]=(3,1)(0,3)");
                    yield return (@"\((\w\. \w+)\)", RegexOptions.RightToLeft, "cd. (A. Tw)", "Pass. Group[0]=(4,7) Group[1]=(5,5)");
                    yield return (@"((?:aaaa|bbbb)cccc)?", RegexOptions.RightToLeft, "aaaacccc", "Pass. Group[0]=(0,8) Group[1]=(0,8)");
                    yield return (@"((?:aaaa|bbbb)cccc)?", RegexOptions.RightToLeft, "bbbbcccc", "Pass. Group[0]=(0,8) Group[1]=(0,8)");
                    yield return (@"(?<=a)b", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(1,1)");
                    yield return (@"(?<=a)b", RegexOptions.RightToLeft, "cb", "Fail.");
                    yield return (@"(?<=a)b", RegexOptions.RightToLeft, "b", "Fail.");
                    yield return (@"(?<!c)b", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(1,1)");
                    yield return (@"(?<!c)b", RegexOptions.RightToLeft, "cb", "Fail.");
                    yield return (@"(?<!c)b", RegexOptions.RightToLeft, "b", "Pass. Group[0]=(0,1)");
                    yield return (@"a(?=d).", RegexOptions.RightToLeft, "adabad", "Pass. Group[0]=(4,2)");
                    yield return (@"a(?=c|d).", RegexOptions.RightToLeft, "adabad", "Pass. Group[0]=(4,2)");
                    yield return (@"ab*c", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"ab*bc", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"ab*bc", RegexOptions.RightToLeft, "abbc", "Pass. Group[0]=(0,4)");
                    yield return (@"ab*bc", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(0,6)");
                    yield return (@".{1}", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(5,1)");
                    yield return (@".{3,4}", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(2,4)");
                    yield return (@"ab{0,}bc", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(0,6)");
                    yield return (@"ab+bc", RegexOptions.RightToLeft, "abbc", "Pass. Group[0]=(0,4)");
                    yield return (@"ab+bc", RegexOptions.RightToLeft, "abc", "Fail.");
                    yield return (@"ab+bc", RegexOptions.RightToLeft, "abq", "Fail.");
                    yield return (@"ab{1,}bc", RegexOptions.RightToLeft, "abq", "Fail.");
                    yield return (@"ab+bc", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(0,6)");
                    yield return (@"ab{1,}bc", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(0,6)");
                    yield return (@"ab{1,3}bc", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(0,6)");
                    yield return (@"ab{3,4}bc", RegexOptions.RightToLeft, "abbbbc", "Pass. Group[0]=(0,6)");
                    yield return (@"ab{4,5}bc", RegexOptions.RightToLeft, "abbbbc", "Fail.");
                    yield return (@"ab?bc", RegexOptions.RightToLeft, "abbc", "Pass. Group[0]=(0,4)");
                    yield return (@"ab?bc", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"ab{0,1}bc", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"ab?bc", RegexOptions.RightToLeft, "abbbbc", "Fail.");
                    yield return (@"ab?c", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"ab{0,1}c", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"^abc$", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"^abc$", RegexOptions.RightToLeft, "abcc", "Fail.");
                    yield return (@"^abc", RegexOptions.RightToLeft, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"^abc$", RegexOptions.RightToLeft, "aabc", "Fail.");
                    yield return (@"abc$", RegexOptions.RightToLeft, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"abc$", RegexOptions.RightToLeft, "aabcd", "Fail.");
                    yield return (@"^", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,0)");
                    yield return (@"$", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(3,0)");
                    yield return (@"a.c", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3)");
                    yield return (@"a.c", RegexOptions.RightToLeft, "axc", "Pass. Group[0]=(0,3)");
                    yield return (@"a.*c", RegexOptions.RightToLeft, "axyzc", "Pass. Group[0]=(0,5)");
                    yield return (@"a.*c", RegexOptions.RightToLeft, "axyzd", "Fail.");
                    yield return (@"a[bc]d", RegexOptions.RightToLeft, "abc", "Fail.");
                    yield return (@"a[bc]d", RegexOptions.RightToLeft, "abd", "Pass. Group[0]=(0,3)");
                    yield return (@"a[b-d]e", RegexOptions.RightToLeft, "abd", "Fail.");
                    yield return (@"a[b-d]e", RegexOptions.RightToLeft, "ace", "Pass. Group[0]=(0,3)");
                    yield return (@"a[b-d]", RegexOptions.RightToLeft, "aac", "Pass. Group[0]=(1,2)");
                    yield return (@"a[-b]", RegexOptions.RightToLeft, "a-", "Pass. Group[0]=(0,2)");
                    yield return (@"a[b-]", RegexOptions.RightToLeft, "a-", "Pass. Group[0]=(0,2)");
                    yield return (@"a]", RegexOptions.RightToLeft, "a]", "Pass. Group[0]=(0,2)");
                    yield return (@"a[]]b", RegexOptions.RightToLeft, "a]b", "Pass. Group[0]=(0,3)");
                    yield return (@"a[^bc]d", RegexOptions.RightToLeft, "aed", "Pass. Group[0]=(0,3)");
                    yield return (@"a[^bc]d", RegexOptions.RightToLeft, "abd", "Fail.");
                    yield return (@"a[^-b]c", RegexOptions.RightToLeft, "adc", "Pass. Group[0]=(0,3)");
                    yield return (@"a[^-b]c", RegexOptions.RightToLeft, "a-c", "Fail.");
                    yield return (@"a[^]b]c", RegexOptions.RightToLeft, "a]c", "Fail.");
                    yield return (@"a[^]b]c", RegexOptions.RightToLeft, "adc", "Pass. Group[0]=(0,3)");
                    yield return (@"\ba\b", RegexOptions.RightToLeft, "a-", "Pass. Group[0]=(0,1)");
                    yield return (@"\ba\b", RegexOptions.RightToLeft, "-a", "Pass. Group[0]=(1,1)");
                    yield return (@"\ba\b", RegexOptions.RightToLeft, "-a-", "Pass. Group[0]=(1,1)");
                    yield return (@"\by\b", RegexOptions.RightToLeft, "xy", "Fail.");
                    yield return (@"\by\b", RegexOptions.RightToLeft, "yz", "Fail.");
                    yield return (@"\by\b", RegexOptions.RightToLeft, "xyz", "Fail.");
                    yield return (@"\Ba\B", RegexOptions.RightToLeft, "a-", "Fail.");
                    yield return (@"\Ba\B", RegexOptions.RightToLeft, "-a", "Fail.");
                    yield return (@"\Ba\B", RegexOptions.RightToLeft, "-a-", "Fail.");
                    yield return (@"\By\b", RegexOptions.RightToLeft, "xy", "Pass. Group[0]=(1,1)");
                    yield return (@"\by\B", RegexOptions.RightToLeft, "yz", "Pass. Group[0]=(0,1)");
                    yield return (@"\By\B", RegexOptions.RightToLeft, "xyz", "Pass. Group[0]=(1,1)");
                    yield return (@"\w", RegexOptions.RightToLeft, "a", "Pass. Group[0]=(0,1)");
                    yield return (@"\w", RegexOptions.RightToLeft, "-", "Fail.");
                    yield return (@"\W", RegexOptions.RightToLeft, "a", "Fail.");
                    yield return (@"\W", RegexOptions.RightToLeft, "-", "Pass. Group[0]=(0,1)");
                    yield return (@"a\sb", RegexOptions.RightToLeft, "a b", "Pass. Group[0]=(0,3)");
                    yield return (@"a\sb", RegexOptions.RightToLeft, "a-b", "Fail.");
                    yield return (@"a\Sb", RegexOptions.RightToLeft, "a b", "Fail.");
                    yield return (@"a\Sb", RegexOptions.RightToLeft, "a-b", "Pass. Group[0]=(0,3)");
                    yield return (@"\d", RegexOptions.RightToLeft, "1", "Pass. Group[0]=(0,1)");
                    yield return (@"\d", RegexOptions.RightToLeft, "-", "Fail.");
                    yield return (@"\D", RegexOptions.RightToLeft, "1", "Fail.");
                    yield return (@"\D", RegexOptions.RightToLeft, "-", "Pass. Group[0]=(0,1)");
                    yield return (@"[\w]", RegexOptions.RightToLeft, "a", "Pass. Group[0]=(0,1)");
                    yield return (@"[\w]", RegexOptions.RightToLeft, "-", "Fail.");
                    yield return (@"[\W]", RegexOptions.RightToLeft, "a", "Fail.");
                    yield return (@"[\W]", RegexOptions.RightToLeft, "-", "Pass. Group[0]=(0,1)");
                    yield return (@"a[\s]b", RegexOptions.RightToLeft, "a b", "Pass. Group[0]=(0,3)");
                    yield return (@"a[\s]b", RegexOptions.RightToLeft, "a-b", "Fail.");
                    yield return (@"a[\S]b", RegexOptions.RightToLeft, "a b", "Fail.");
                    yield return (@"a[\S]b", RegexOptions.RightToLeft, "a-b", "Pass. Group[0]=(0,3)");
                    yield return (@"[\d]", RegexOptions.RightToLeft, "1", "Pass. Group[0]=(0,1)");
                    yield return (@"[\d]", RegexOptions.RightToLeft, "-", "Fail.");
                    yield return (@"[\D]", RegexOptions.RightToLeft, "1", "Fail.");
                    yield return (@"[\D]", RegexOptions.RightToLeft, "-", "Pass. Group[0]=(0,1)");
                    yield return (@"ab|cd", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,2)");
                    yield return (@"ab|cd", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(2,2)");
                    yield return (@"()ef", RegexOptions.RightToLeft, "def", "Pass. Group[0]=(1,2) Group[1]=(1,0)");
                    yield return (@"$b", RegexOptions.RightToLeft, "b", "Fail.");
                    yield return (@"a\(b", RegexOptions.RightToLeft, "a(b", "Pass. Group[0]=(0,3)");
                    yield return (@"a\(*b", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(0,2)");
                    yield return (@"a\(*b", RegexOptions.RightToLeft, "a((b", "Pass. Group[0]=(0,4)");
                    yield return (@"a\\b", RegexOptions.RightToLeft, "a\\b", "Pass. Group[0]=(0,3)");
                    yield return (@"((a))", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1)");
                    yield return (@"(a)b(c)", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,1) Group[2]=(2,1)");
                    yield return (@"a+b+c", RegexOptions.RightToLeft, "aabbabc", "Pass. Group[0]=(4,3)");
                    yield return (@"a{1,}b{1,}c", RegexOptions.RightToLeft, "aabbabc", "Pass. Group[0]=(4,3)");
                    yield return (@"a.+?c", RegexOptions.RightToLeft, "abcabc", "Pass. Group[0]=(3,3)");
                    yield return (@"(a+|b)*", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(0,2) Group[1]=(1,1)(0,1)");
                    yield return (@"(a+|b){0,}", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(0,2) Group[1]=(1,1)(0,1)");
                    yield return (@"(a+|b)+", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(0,2) Group[1]=(1,1)(0,1)");
                    yield return (@"(a+|b){1,}", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(0,2) Group[1]=(1,1)(0,1)");
                    yield return (@"(a+|b)?", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(1,1) Group[1]=(1,1)");
                    yield return (@"(a+|b){0,1}", RegexOptions.RightToLeft, "ab", "Pass. Group[0]=(1,1) Group[1]=(1,1)");
                    yield return (@"[^ab]*", RegexOptions.RightToLeft, "cde", "Pass. Group[0]=(0,3)");
                    yield return (@"abc", RegexOptions.RightToLeft, "", "Fail.");
                    yield return (@"a*", RegexOptions.RightToLeft, "", "Pass. Group[0]=(0,0)");
                    yield return (@"([abc])*d", RegexOptions.RightToLeft, "abbbcd", "Pass. Group[0]=(0,6) Group[1]=(4,1)(3,1)(2,1)(1,1)(0,1)");
                    yield return (@"([abc])*bcd", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4) Group[1]=(0,1)");
                    yield return (@"a|b|c|d|e", RegexOptions.RightToLeft, "e", "Pass. Group[0]=(0,1)");
                    yield return (@"(a|b|c|d|e)f", RegexOptions.RightToLeft, "ef", "Pass. Group[0]=(0,2) Group[1]=(0,1)");
                    yield return (@"abcd*efg", RegexOptions.RightToLeft, "abcdefg", "Pass. Group[0]=(0,7)");
                    yield return (@"ab*", RegexOptions.RightToLeft, "xabyabbbz", "Pass. Group[0]=(4,4)");
                    yield return (@"ab*", RegexOptions.RightToLeft, "xayabbbz", "Pass. Group[0]=(3,4)");
                    yield return (@"(ab|cd)e", RegexOptions.RightToLeft, "abcde", "Pass. Group[0]=(2,3) Group[1]=(2,2)");
                    yield return (@"[abhgefdc]ij", RegexOptions.RightToLeft, "hij", "Pass. Group[0]=(0,3)");
                    yield return (@"^(ab|cd)e", RegexOptions.RightToLeft, "abcde", "Fail.");
                    yield return (@"(abc|)ef", RegexOptions.RightToLeft, "abcdef", "Pass. Group[0]=(4,2) Group[1]=(4,0)");
                    yield return (@"(a|b)c*d", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(1,3) Group[1]=(1,1)");
                    yield return (@"(ab|ab*)bc", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,1)");
                    yield return (@"a([bc]*)c*", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3) Group[1]=(1,1)");
                    yield return (@"a([bc]*)(c*d)", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,1) Group[2]=(2,2)");
                    yield return (@"a([bc]+)(c*d)", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,1) Group[2]=(2,2)");
                    yield return (@"a([bc]*)(c+d)", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,1) Group[2]=(2,2)");
                    yield return (@"a[bcd]*dcdcde", RegexOptions.RightToLeft, "adcdcde", "Pass. Group[0]=(0,7)");
                    yield return (@"a[bcd]+dcdcde", RegexOptions.RightToLeft, "adcdcde", "Fail.");
                    yield return (@"(ab|a)b*c", RegexOptions.RightToLeft, "abc", "Pass. Group[0]=(0,3) Group[1]=(0,1)");
                    yield return (@"((a)(b)c)(d)", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4) Group[1]=(0,3) Group[2]=(0,1) Group[3]=(1,1) Group[4]=(3,1)");
                    yield return (@"[a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.RightToLeft, "alpha", "Pass. Group[0]=(0,5)");
                    yield return (@"^a(bc+|b[eh])g|.h$", RegexOptions.RightToLeft, "abh", "Pass. Group[0]=(1,2) Group[1]=");
                    yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.RightToLeft, "effgz", "Pass. Group[0]=(0,5) Group[1]=(0,5) Group[2]=");
                    yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.RightToLeft, "ij", "Pass. Group[0]=(0,2) Group[1]=(0,2) Group[2]=(1,1)");
                    yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.RightToLeft, "effg", "Fail.");
                    yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.RightToLeft, "bcdd", "Fail.");
                    yield return (@"(bc+d$|ef*g.|h?i(j|k))", RegexOptions.RightToLeft, "reffgz", "Pass. Group[0]=(1,5) Group[1]=(1,5) Group[2]=");
                    yield return (@"((((((((((a))))))))))", RegexOptions.RightToLeft, "a", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                    yield return (@"((((((((((a))))))))))\10", RegexOptions.RightToLeft, "aa", "Fail.");
                    yield return (@"\10((((((((((a))))))))))", RegexOptions.RightToLeft, "aa", "Pass. Group[0]=(0,2) Group[1]=(1,1) Group[2]=(1,1) Group[3]=(1,1) Group[4]=(1,1) Group[5]=(1,1) Group[6]=(1,1) Group[7]=(1,1) Group[8]=(1,1) Group[9]=(1,1) Group[10]=(1,1)");
                    yield return (@"((((((((((a))))))))))!", RegexOptions.RightToLeft, "aa", "Fail.");
                    yield return (@"((((((((((a))))))))))!", RegexOptions.RightToLeft, "a!", "Pass. Group[0]=(0,2) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1) Group[10]=(0,1)");
                    yield return (@"(((((((((a)))))))))", RegexOptions.RightToLeft, "a", "Pass. Group[0]=(0,1) Group[1]=(0,1) Group[2]=(0,1) Group[3]=(0,1) Group[4]=(0,1) Group[5]=(0,1) Group[6]=(0,1) Group[7]=(0,1) Group[8]=(0,1) Group[9]=(0,1)");
                    yield return (@"multiple words of text", RegexOptions.RightToLeft, "uh-uh", "Fail.");
                    yield return (@"multiple words", RegexOptions.RightToLeft, "multiple words, yeah", "Pass. Group[0]=(0,14)");
                    yield return (@"(.*)c(.*)", RegexOptions.RightToLeft, "abcde", "Pass. Group[0]=(0,5) Group[1]=(0,2) Group[2]=(3,2)");
                    yield return (@"\((.*), (.*)\)", RegexOptions.RightToLeft, "(a, b)", "Pass. Group[0]=(0,6) Group[1]=(1,1) Group[2]=(4,1)");
                    yield return (@"[k]", RegexOptions.RightToLeft, "ab", "Fail.");
                    yield return (@"abcd", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4)");
                    yield return (@"a(bc)d", RegexOptions.RightToLeft, "abcd", "Pass. Group[0]=(0,4) Group[1]=(1,2)");
                    yield return (@"a[-]?c", RegexOptions.RightToLeft, "ac", "Pass. Group[0]=(0,2)");
                    yield return (@"(abc)\1", RegexOptions.RightToLeft, "abcabc", "Fail.");
                    yield return (@"\1(abc)", RegexOptions.RightToLeft, "abcabc", "Pass. Group[0]=(0,6) Group[1]=(3,3)");
                    yield return (@"([a-c]*)\1", RegexOptions.RightToLeft, "abcabc", "Fail.");
                    yield return (@"\1([a-c]*)", RegexOptions.RightToLeft, "abcabc", "Pass. Group[0]=(0,6) Group[1]=(3,3)");
                    yield return (@"(a)|\1", RegexOptions.RightToLeft, "a", "Pass. Group[0]=(0,1) Group[1]=(0,1)");
                    yield return (@"(a)|\1", RegexOptions.RightToLeft, "x", "Fail.");
                    yield return (@"(([a-c])b*?\2)*", RegexOptions.RightToLeft, "ababbbcbc", "Pass. Group[0]=(9,0) Group[1]= Group[2]=");
                    yield return (@"(([a-c])b*?\2){3}", RegexOptions.RightToLeft, "ababbbcbc", "Fail.");
                    yield return (@"((\3|b)\2(a)x)+", RegexOptions.RightToLeft, "aaxabxbaxbbx", "Fail.");
                    yield return (@"((\3|b)\2(a)x)+", RegexOptions.RightToLeft, "aaaxabaxbaaxbbax", "Fail.");
                    yield return (@"((\3|b)\2(a)){2,}", RegexOptions.RightToLeft, "bbaababbabaaaaabbaaaabba", "Fail.");

                    // Ported from https://github.com/mono/mono/blob/0f2995e95e98e082c7c7039e17175cf2c6a00034/mcs/class/System/Test/System.Text.RegularExpressions/RegexMatchTests.cs
                    yield return (@"(F)(2)(3)(4)(5)(6)(7)(8)(9)(10)(L)\11", RegexOptions.None, "F2345678910LL", "Pass. Group[0]=(0,13) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(3,1) Group[5]=(4,1) Group[6]=(5,1) Group[7]=(6,1) Group[8]=(7,1) Group[9]=(8,1) Group[10]=(9,2) Group[11]=(11,1)");
                    yield return (@"(F)(2)(3)(4)(5)(6)(7)(8)(9)(10)(L)\11", RegexOptions.ExplicitCapture, "F2345678910LL", "Fail.");
                    yield return (@"(F)(2)(3)(4)(5)(6)(?<S>7)(8)(9)(10)(L)\1", RegexOptions.None, "F2345678910L71", "Fail.");
                    yield return (@"(F)(2)(3)(4)(5)(6)(7)(8)(9)(10)(L)\11", RegexOptions.None, "F2345678910LF1", "Fail.");
                    yield return (@"(F)(2)(3)(4)(5)(6)(?<S>7)(8)(9)(10)(L)\11", RegexOptions.None, "F2345678910L71", "Pass. Group[0]=(0,13) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(3,1) Group[5]=(4,1) Group[6]=(5,1) Group[7]=(7,1) Group[8]=(8,1) Group[9]=(9,2) Group[10]=(11,1) Group[11]=(6,1)");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)\10", RegexOptions.None, "F2345678910L71", "Pass. Group[0]=(0,13) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(4,1) Group[5]=(5,1) Group[6]=(7,1) Group[7]=(8,1) Group[8]=(9,2) Group[9]=(11,1) Group[10]=(3,1)(6,1)");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)\10", RegexOptions.ExplicitCapture, "F2345678910L70", "Fail.");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)\1", RegexOptions.ExplicitCapture, "F2345678910L70", "Pass. Group[0]=(0,13) Group[1]=(3,1)(6,1)");
                    yield return (@"(?n:(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)\1)", RegexOptions.None, "F2345678910L70", "Pass. Group[0]=(0,13) Group[1]=(3,1)(6,1)");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)(?(10)\10)", RegexOptions.None, "F2345678910L70", "Pass. Group[0]=(0,13) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(4,1) Group[5]=(5,1) Group[6]=(7,1) Group[7]=(8,1) Group[8]=(9,2) Group[9]=(11,1) Group[10]=(3,1)(6,1)");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)(?(S)|\10)", RegexOptions.None, "F2345678910L70", "Pass. Group[0]=(0,12) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(4,1) Group[5]=(5,1) Group[6]=(7,1) Group[7]=(8,1) Group[8]=(9,2) Group[9]=(11,1) Group[10]=(3,1)(6,1)");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)(?(7)|\10)", RegexOptions.None, "F2345678910L70", "Pass. Group[0]=(0,12) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(4,1) Group[5]=(5,1) Group[6]=(7,1) Group[7]=(8,1) Group[8]=(9,2) Group[9]=(11,1) Group[10]=(3,1)(6,1)");
                    yield return (@"(F)(2)(3)(?<S>4)(5)(6)(?'S'7)(8)(9)(10)(L)(?(K)|\10)", RegexOptions.None, "F2345678910L70", "Pass. Group[0]=(0,13) Group[1]=(0,1) Group[2]=(1,1) Group[3]=(2,1) Group[4]=(4,1) Group[5]=(5,1) Group[6]=(7,1) Group[7]=(8,1) Group[8]=(9,2) Group[9]=(11,1) Group[10]=(3,1)(6,1)");
                    yield return (@"(?<=a+)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<=a*)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(0,4)");
                    yield return (@"(?<=a{1,5})(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<=a{1})(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<=a{1,})(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<=a+?)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<=a*?)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(0,4)");
                    yield return (@"(?<=a{1,5}?)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<=a{1}?)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(1,3)");
                    yield return (@"(?<!a+)(?:a)*bc", RegexOptions.None, "aabc", "Pass. Group[0]=(0,4)");
                    yield return (@"(?<!a*)(?:a)*bc", RegexOptions.None, "aabc", "Fail.");
                    yield return (@"abc*(?=c*)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,4)");
                    yield return (@"abc*(?=c+)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c{1})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c{1,5})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c{1,})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c*?)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,4)");
                    yield return (@"abc*(?=c+?)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c{1}?)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c{1,5}?)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*(?=c{1,}?)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,3)");
                    yield return (@"abc*?(?=c*)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,2)");
                    yield return (@"abc*?(?=c+)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,2)");
                    yield return (@"abc*?(?=c{1})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,2)");
                    yield return (@"abc*?(?=c{1,5})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,2)");
                    yield return (@"abc*?(?=c{1,})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,2)");
                    yield return (@"abc*(?!c*)", RegexOptions.None, "abcc", "Fail.");
                    yield return (@"abc*(?!c+)", RegexOptions.None, "abcc", "Pass. Group[0]=(0,4)");
                    yield return (@"abc*(?!c{1})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,4)");
                    yield return (@"abc*(?!c{1,5})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,4)");
                    yield return (@"abc*(?!c{1,})", RegexOptions.None, "abcc", "Pass. Group[0]=(0,4)");
                    yield return (@"(?>a*).", RegexOptions.ExplicitCapture, "aaaa", "Fail.");
                    yield return (@"(?<ab>ab)c\1", RegexOptions.None, "abcabc", "Pass. Group[0]=(0,5) Group[1]=(0,2)");
                }
            }
        }

        [Theory]
        [InlineData(@"a[b-a]", RegexOptions.None)]
        [InlineData(@"a[]b", RegexOptions.None)]
        [InlineData(@"a[", RegexOptions.None)]
        [InlineData(@"*a", RegexOptions.None)]
        [InlineData(@"(*)b", RegexOptions.None)]
        [InlineData(@"a\", RegexOptions.None)]
        [InlineData(@"abc)", RegexOptions.None)]
        [InlineData(@"(abc", RegexOptions.None)]
        [InlineData(@"a**", RegexOptions.None)]
        [InlineData(@")(", RegexOptions.None)]
        [InlineData(@"\1", RegexOptions.None)]
        [InlineData(@"\2", RegexOptions.None)]
        [InlineData(@"(a)|\2", RegexOptions.None)]
        [InlineData(@"a[b-a]", RegexOptions.IgnoreCase)]
        [InlineData(@"a[]b", RegexOptions.IgnoreCase)]
        [InlineData(@"a[", RegexOptions.IgnoreCase)]
        [InlineData(@"*a", RegexOptions.IgnoreCase)]
        [InlineData(@"(*)b", RegexOptions.IgnoreCase)]
        [InlineData(@"a\", RegexOptions.IgnoreCase)]
        [InlineData(@"abc)", RegexOptions.IgnoreCase)]
        [InlineData(@"(abc", RegexOptions.IgnoreCase)]
        [InlineData(@"a**", RegexOptions.IgnoreCase)]
        [InlineData(@")(", RegexOptions.IgnoreCase)]
        [InlineData(@":(?:", RegexOptions.None)]
        [InlineData(@"(?<%)b", RegexOptions.None)]
        [InlineData(@"(?(1)a|b|c)", RegexOptions.None)]
        [InlineData(@"a{37,17}", RegexOptions.None)]
        [InlineData(@"a[b-a]", RegexOptions.RightToLeft)]
        [InlineData(@"a[]b", RegexOptions.RightToLeft)]
        [InlineData(@"a[", RegexOptions.RightToLeft)]
        [InlineData(@"*a", RegexOptions.RightToLeft)]
        [InlineData(@"(*)b", RegexOptions.RightToLeft)]
        [InlineData(@"a\", RegexOptions.RightToLeft)]
        [InlineData(@"abc)", RegexOptions.RightToLeft)]
        [InlineData(@"(abc", RegexOptions.RightToLeft)]
        [InlineData(@"a**", RegexOptions.RightToLeft)]
        [InlineData(@")(", RegexOptions.RightToLeft)]
        [InlineData(@"\1", RegexOptions.RightToLeft)]
        [InlineData(@"\2", RegexOptions.RightToLeft)]
        [InlineData(@"(a)|\2", RegexOptions.RightToLeft)]
        public void ParseFailures(string pattern, RegexOptions options)
        {
            Assert.ThrowsAny<ArgumentException>(() => new Regex(pattern, options));
        }
    }
}
