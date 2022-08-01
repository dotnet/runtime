// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 *	Glenn Fowler <gsf@research.att.com>
 *	AT&T Research
 *
 * PLEASE: publish your tests so everyone can benefit
 *
 * The following license covers testregex.c and all associated test data.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of THIS SOFTWARE FILE (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do
 * so, subject to the following disclaimer:
 *
 * THIS SOFTWARE IS PROVIDED BY AT&T ``AS IS'' AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL AT&T BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

// .dat files from http://gsf.cococlyde.org/download converted to [InputData(...)] with:
// -------------------------------------
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// 
// class Program
// {
//     static void Main()
//     {
//         using var writer = new StreamWriter(@"output.txt");
//         string[][] tests = File.ReadLines(@"C:\Users\stoub\Desktop\New folder\interpretation.dat")
//             .Select(s => s.Trim())
//             .Where(s => s.Length > 0 && !s.StartsWith("NOTE") && !s.StartsWith("#"))
//             .Select(s => s.Split('\t', StringSplitOptions.RemoveEmptyEntries))
//             .Where(s => s.Length >= 4 && !s[3].Contains("?"))
//             .Where(s => !s[1].StartsWith("[["))
//             .ToArray();
// 
//         var seen = new HashSet<string>();
//         foreach (string[] test in tests)
//         {
//             string pattern = test[1].Replace("\\", "\\\\").Replace("\\\\n", "\\n").Replace("?-u", "?-i");
//             string input = test[2].Replace("\\", "\\\\").Replace("\\\\n", "\\n").Replace("\\\\x", "\\x00");
//             string captures = test[3];
//             if (seen.Add(pattern + input + captures))
//             {
//                 writer.WriteLine($"yield return new object[] { \"{pattern}\", \"{input}\", \"{captures}\" };");
//             }
//         }
//     }
// }
// -------------------------------------
// Then some inputs were deleted / tweaked based on expected differences in behavior.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class AttRegexTests
    {
        public static IEnumerable<object[]> Inputs()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Multiline })
                {
                    // basic.dat
                    yield return new object[] { engine, options, "abracadabra$", "abracadabracadabra", "(7,18)" };
                    yield return new object[] { engine, options, "a...b", "abababbb", "(2,7)" };
                    yield return new object[] { engine, options, "XXXXXX", "..XXXXXX", "(2,8)" };
                    yield return new object[] { engine, options, "\\)", "()", "(1,2)" };
                    yield return new object[] { engine, options, "a]", "a]a", "(0,2)" };
                    yield return new object[] { engine, options, "}", "}", "(0,1)" };
                    yield return new object[] { engine, options, "\\}", "}", "(0,1)" };
                    yield return new object[] { engine, options, "\\]", "]", "(0,1)" };
                    yield return new object[] { engine, options, "]", "]", "(0,1)" };
                    yield return new object[] { engine, options, "{", "{", "(0,1)" };
                    yield return new object[] { engine, options, "^a", "ax", "(0,1)" };
                    yield return new object[] { engine, options, "\\^a", "a^a", "(1,3)" };
                    yield return new object[] { engine, options, "a\\^", "a^", "(0,2)" };
                    yield return new object[] { engine, options, "a$", "aa", "(1,2)" };
                    yield return new object[] { engine, options, "a\\$", "a$", "(0,2)" };
                    yield return new object[] { engine, options, "^$", "NULL", "(0,0)" };
                    yield return new object[] { engine, options, "$^", "NULL", "(0,0)" };
                    yield return new object[] { engine, options, "a($)", "aa", "(1,2)(2,2)" };
                    yield return new object[] { engine, options, "a*(^a)", "aa", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(..)*(...)*", "a", "(0,0)" };
                    yield return new object[] { engine, options, "(..)*(...)*", "abcd", "(0,4)(2,4)" };
                    yield return new object[] { engine, options, "(ab|a)(bc|c)", "abc", "(0,3)(0,2)(2,3)" };
                    yield return new object[] { engine, options, "(ab)c|abc", "abc", "(0,3)(0,2)" };
                    yield return new object[] { engine, options, "a{0}b", "ab", "(1,2)" };
                    yield return new object[] { engine, options, "(a*)(b?)(b+)b{3}", "aaabbbbbbb", "(0,10)(0,3)(3,4)(4,7)" };
                    yield return new object[] { engine, options, "(a*)(b{0,1})(b{1,})b{3}", "aaabbbbbbb", "(0,10)(0,3)(3,4)(4,7)" };
                    yield return new object[] { engine, options, "a{9876543210}", "NULL", "BADBR" };
                    yield return new object[] { engine, options, "((a|a)|a)", "a", "(0,1)(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a*)(a|aa)", "aaaa", "(0,4)(0,3)(3,4)" };
                    yield return new object[] { engine, options, "a*(a.|aa)", "aaaa", "(0,4)(2,4)" };
                    yield return new object[] { engine, options, "(a|b)?.*", "b", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a|b)c|a(b|c)", "ac", "(0,2)(0,1)" };
                    yield return new object[] { engine, options, "(a|b)*c|(a|ab)*c", "abc", "(0,3)(1,2)" };
                    yield return new object[] { engine, options, "(a|b)*c|(a|ab)*c", "xc", "(1,2)" };
                    yield return new object[] { engine, options, "(.a|.b).*|.*(.a|.b)", "xa", "(0,2)(0,2)" };
                    yield return new object[] { engine, options, "a?(ab|ba)ab", "abab", "(0,4)(0,2)" };
                    yield return new object[] { engine, options, "a?(ac{0}b|ba)ab", "abab", "(0,4)(0,2)" };
                    yield return new object[] { engine, options, "ab|abab", "abbabab", "(0,2)" };
                    yield return new object[] { engine, options, "aba|bab|bba", "baaabbbaba", "(5,8)" };
                    yield return new object[] { engine, options, "aba|bab", "baaabbbaba", "(6,9)" };
                    yield return new object[] { engine, options, "(aa|aaa)*|(a|aaaaa)", "aa", "(0,2)(0,2)" };
                    yield return new object[] { engine, options, "(a.|.a.)*|(a|.a...)", "aa", "(0,2)(0,2)" };
                    yield return new object[] { engine, options, "ab|a", "xabc", "(1,3)" };
                    yield return new object[] { engine, options, "ab|a", "xxabc", "(2,4)" };
                    yield return new object[] { engine, options, "(?i)(Ab|cD)*", "aBcD", "(0,4)(2,4)" };
                    yield return new object[] { engine, options, "[^-]", "--a", "(2,3)" };
                    yield return new object[] { engine, options, "[a-]*", "--a", "(0,3)" };
                    yield return new object[] { engine, options, "[a-m-]*", "--amoma--", "(0,4)" };
                    yield return new object[] { engine, options, ":::1:::0:|:::1:1:0:", ":::0:::1:::1:::0:", "(8,17)" };
                    yield return new object[] { engine, options, ":::1:::0:|:::1:1:1:", ":::0:::1:::1:::0:", "(8,17)" };
                    yield return new object[] { engine, options, "\n", "\n", "(0,1)" };
                    yield return new object[] { engine, options, "[^a]", "\n", "(0,1)" };
                    yield return new object[] { engine, options, "\na", "\na", "(0,2)" };
                    yield return new object[] { engine, options, "(a)(b)(c)", "abc", "(0,3)(0,1)(1,2)(2,3)" };
                    yield return new object[] { engine, options, "xxx", "xxx", "(0,3)" };
                    yield return new object[] { engine, options, "(^|[ (,;])((([Ff]eb[^ ]* *|0*2/|\\* */?)0*[6-7]))([^0-9]|$)", "feb 6,", "(0,6)" };
                    yield return new object[] { engine, options, "(^|[ (,;])((([Ff]eb[^ ]* *|0*2/|\\* */?)0*[6-7]))([^0-9]|$)", "2/7", "(0,3)" };
                    yield return new object[] { engine, options, "(^|[ (,;])((([Ff]eb[^ ]* *|0*2/|\\* */?)0*[6-7]))([^0-9]|$)", "feb 1,Feb 6", "(5,11)" };
                    yield return new object[] { engine, options, "((((((((((((((((((((((((((((((x))))))))))))))))))))))))))))))", "x", "(0,1)(0,1)(0,1)" };
                    yield return new object[] { engine, options, "((((((((((((((((((((((((((((((x))))))))))))))))))))))))))))))*", "xx", "(0,2)(1,2)(1,2)" };
                    yield return new object[] { engine, options, "a?(ab|ba)*", "ababababababababababababababababababababababababababababababababababababababababa", "(0,81)(79,81)" };
                    yield return new object[] { engine, options, "abaa|abbaa|abbbaa|abbbbaa", "ababbabbbabbbabbbbabbbbaa", "(18,25)" };
                    yield return new object[] { engine, options, "abaa|abbaa|abbbaa|abbbbaa", "ababbabbbabbbabbbbabaa", "(18,22)" };
                    yield return new object[] { engine, options, "aaac|aabc|abac|abbc|baac|babc|bbac|bbbc", "baaabbbabac", "(7,11)" };
                    yield return new object[] { engine, options, ".*", "\x0001\x00ff", "(0,2)" };
                    yield return new object[] { engine, options, "aaaa|bbbb|cccc|ddddd|eeeeee|fffffff|gggg|hhhh|iiiii|jjjjj|kkkkk|llll", "XaaaXbbbXcccXdddXeeeXfffXgggXhhhXiiiXjjjXkkkXlllXcbaXaaaa", "(53,57)" };
                    yield return new object[] { engine, options, "aaaa\nbbbb\ncccc\nddddd\neeeeee\nfffffff\ngggg\nhhhh\niiiii\njjjjj\nkkkkk\nllll", "XaaaXbbbXcccXdddXeeeXfffXgggXhhhXiiiXjjjXkkkXlllXcbaXaaaa", "NOMATCH" };
                    yield return new object[] { engine, options, "a*a*a*a*a*b", "aaaaaaaaab", "(0,10)" };
                    yield return new object[] { engine, options, "^", "NULL", "(0,0)" };
                    yield return new object[] { engine, options, "$", "NULL", "(0,0)" };
                    yield return new object[] { engine, options, "^a$", "a", "(0,1)" };
                    yield return new object[] { engine, options, "abc", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "abc", "xabcy", "(1,4)" };
                    yield return new object[] { engine, options, "abc", "ababc", "(2,5)" };
                    yield return new object[] { engine, options, "ab*c", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "ab*bc", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "ab*bc", "abbc", "(0,4)" };
                    yield return new object[] { engine, options, "ab*bc", "abbbbc", "(0,6)" };
                    yield return new object[] { engine, options, "ab+bc", "abbc", "(0,4)" };
                    yield return new object[] { engine, options, "ab+bc", "abbbbc", "(0,6)" };
                    yield return new object[] { engine, options, "ab?bc", "abbc", "(0,4)" };
                    yield return new object[] { engine, options, "ab?bc", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "ab?c", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "^abc$", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "^abc", "abcc", "(0,3)" };
                    yield return new object[] { engine, options, "abc$", "aabc", "(1,4)" };
                    yield return new object[] { engine, options, "^", "abc", "(0,0)" };
                    yield return new object[] { engine, options, "$", "abc", "(3,3)" };
                    yield return new object[] { engine, options, "a.c", "abc", "(0,3)" };
                    yield return new object[] { engine, options, "a.c", "axc", "(0,3)" };
                    yield return new object[] { engine, options, "a.*c", "axyzc", "(0,5)" };
                    yield return new object[] { engine, options, "a[bc]d", "abd", "(0,3)" };
                    yield return new object[] { engine, options, "a[b-d]e", "ace", "(0,3)" };
                    yield return new object[] { engine, options, "a[b-d]", "aac", "(1,3)" };
                    yield return new object[] { engine, options, "a[-b]", "a-", "(0,2)" };
                    yield return new object[] { engine, options, "a[b-]", "a-", "(0,2)" };
                    yield return new object[] { engine, options, "a]", "a]", "(0,2)" };
                    yield return new object[] { engine, options, "a[]]b", "a]b", "(0,3)" };
                    yield return new object[] { engine, options, "a[^bc]d", "aed", "(0,3)" };
                    yield return new object[] { engine, options, "a[^-b]c", "adc", "(0,3)" };
                    yield return new object[] { engine, options, "a[^]b]c", "adc", "(0,3)" };
                    yield return new object[] { engine, options, "ab|cd", "abc", "(0,2)" };
                    yield return new object[] { engine, options, "ab|cd", "abcd", "(0,2)" };
                    yield return new object[] { engine, options, "a\\(b", "a(b", "(0,3)" };
                    yield return new object[] { engine, options, "a\\(*b", "ab", "(0,2)" };
                    yield return new object[] { engine, options, "a\\(*b", "a((b", "(0,4)" };
                    yield return new object[] { engine, options, "((a))", "abc", "(0,1)(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a)b(c)", "abc", "(0,3)(0,1)(2,3)" };
                    yield return new object[] { engine, options, "a+b+c", "aabbabc", "(4,7)" };
                    yield return new object[] { engine, options, "a*", "aaa", "(0,3)" };
                    yield return new object[] { engine, options, "(a*)*", "-", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "(a*)+", "-", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "(a*|b)*", "-", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "(a+|b)*", "ab", "(0,2)(1,2)" };
                    yield return new object[] { engine, options, "(a+|b)+", "ab", "(0,2)(1,2)" };
                    yield return new object[] { engine, options, "(a+|b)?", "ab", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "[^ab]*", "cde", "(0,3)" };
                    yield return new object[] { engine, options, "(^)*", "-", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "a*", "NULL", "(0,0)" };
                    yield return new object[] { engine, options, "([abc])*d", "abbbcd", "(0,6)(4,5)" };
                    yield return new object[] { engine, options, "([abc])*bcd", "abcd", "(0,4)(0,1)" };
                    yield return new object[] { engine, options, "a|b|c|d|e", "e", "(0,1)" };
                    yield return new object[] { engine, options, "(a|b|c|d|e)f", "ef", "(0,2)(0,1)" };
                    yield return new object[] { engine, options, "((a*|b))*", "-", "(0,0)(0,0)(0,0)" };
                    yield return new object[] { engine, options, "abcd*efg", "abcdefg", "(0,7)" };
                    yield return new object[] { engine, options, "ab*", "xabyabbbz", "(1,3)" };
                    yield return new object[] { engine, options, "ab*", "xayabbbz", "(1,2)" };
                    yield return new object[] { engine, options, "(ab|cd)e", "abcde", "(2,5)(2,4)" };
                    yield return new object[] { engine, options, "[abhgefdc]ij", "hij", "(0,3)" };
                    yield return new object[] { engine, options, "(a|b)c*d", "abcd", "(1,4)(1,2)" };
                    yield return new object[] { engine, options, "(ab|ab*)bc", "abc", "(0,3)(0,1)" };
                    yield return new object[] { engine, options, "a([bc]*)c*", "abc", "(0,3)(1,3)" };
                    yield return new object[] { engine, options, "a([bc]*)(c*d)", "abcd", "(0,4)(1,3)(3,4)" };
                    yield return new object[] { engine, options, "a([bc]+)(c*d)", "abcd", "(0,4)(1,3)(3,4)" };
                    yield return new object[] { engine, options, "a([bc]*)(c+d)", "abcd", "(0,4)(1,2)(2,4)" };
                    yield return new object[] { engine, options, "a[bcd]*dcdcde", "adcdcde", "(0,7)" };
                    yield return new object[] { engine, options, "(ab|a)b*c", "abc", "(0,3)(0,2)" };
                    yield return new object[] { engine, options, "((a)(b)c)(d)", "abcd", "(0,4)(0,3)(0,1)(1,2)(3,4)" };
                    yield return new object[] { engine, options, "[A-Za-z_][A-Za-z0-9_]*", "alpha", "(0,5)" };
                    yield return new object[] { engine, options, "^a(bc+|b[eh])g|.h$", "abh", "(1,3)" };
                    yield return new object[] { engine, options, "(bc+d$|ef*g.|h?i(j|k))", "effgz", "(0,5)(0,5)" };
                    yield return new object[] { engine, options, "(bc+d$|ef*g.|h?i(j|k))", "ij", "(0,2)(0,2)(1,2)" };
                    yield return new object[] { engine, options, "(bc+d$|ef*g.|h?i(j|k))", "reffgz", "(1,6)(1,6)" };
                    yield return new object[] { engine, options, "(((((((((a)))))))))", "a", "(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)" };
                    yield return new object[] { engine, options, "multiple words", "multiple words yeah", "(0,14)" };
                    yield return new object[] { engine, options, "(.*)c(.*)", "abcde", "(0,5)(0,2)(3,5)" };
                    yield return new object[] { engine, options, "abcd", "abcd", "(0,4)" };
                    yield return new object[] { engine, options, "a(bc)d", "abcd", "(0,4)(1,3)" };
                    yield return new object[] { engine, options, "a[-]?c", "ac", "(0,3)" };
                    yield return new object[] { engine, options, "a+(b|c)*d+", "aabcdd", "(0,6)(3,4)" };
                    yield return new object[] { engine, options, "^.+$", "vivi", "(0,4)" };
                    yield return new object[] { engine, options, "^(.+)$", "vivi", "(0,4)(0,4)" };
                    yield return new object[] { engine, options, "^([^!.]+).att.com!(.+)$", "gryphon.att.com!eby", "(0,19)(0,7)(16,19)" };
                    yield return new object[] { engine, options, "^([^!]+!)?([^!]+)$", "bar!bas", "(0,7)(0,4)(4,7)" };
                    yield return new object[] { engine, options, "^([^!]+!)?([^!]+)$", "foo!bas", "(0,7)(0,4)(4,7)" };
                    yield return new object[] { engine, options, "^.+!([^!]+!)([^!]+)$", "foo!bar!bas", "(0,11)(4,8)(8,11)" };
                    yield return new object[] { engine, options, "((foo)|(bar))!bas", "foo!bas", "(0,7)(0,3)(0,3)" };
                    yield return new object[] { engine, options, "((foo)|bar)!bas", "bar!bas", "(0,7)(0,3)" };
                    yield return new object[] { engine, options, "((foo)|bar)!bas", "foo!bar!bas", "(4,11)(4,7)" };
                    yield return new object[] { engine, options, "((foo)|bar)!bas", "foo!bas", "(0,7)(0,3)(0,3)" };
                    yield return new object[] { engine, options, "(foo|(bar))!bas", "bar!bas", "(0,7)(0,3)(0,3)" };
                    yield return new object[] { engine, options, "(foo|(bar))!bas", "foo!bar!bas", "(4,11)(4,7)(4,7)" };
                    yield return new object[] { engine, options, "(foo|(bar))!bas", "foo!bas", "(0,7)(0,3)" };
                    yield return new object[] { engine, options, "(foo|bar)!bas", "bar!bas", "(0,7)(0,3)" };
                    yield return new object[] { engine, options, "(foo|bar)!bas", "foo!bar!bas", "(4,11)(4,7)" };
                    yield return new object[] { engine, options, "(foo|bar)!bas", "foo!bas", "(0,7)(0,3)" };
                    yield return new object[] { engine, options, "^([^!]+!)?([^!]+)$|^.+!([^!]+!)([^!]+)$", "bar!bas", "(0,7)(0,4)(4,7)" };
                    yield return new object[] { engine, options, "^([^!]+!)?([^!]+)$|^.+!([^!]+!)([^!]+)$", "foo!bas", "(0,7)(0,4)(4,7)" };
                    yield return new object[] { engine, options, "^(([^!]+!)?([^!]+)|.+!([^!]+!)([^!]+))$", "bar!bas", "(0,7)(0,7)(0,4)(4,7)" };
                    yield return new object[] { engine, options, "^(([^!]+!)?([^!]+)|.+!([^!]+!)([^!]+))$", "foo!bas", "(0,7)(0,7)(0,4)(4,7)" };
                    yield return new object[] { engine, options, ".*(/XXX).*", "/XXX", "(0,4)(0,4)" };
                    yield return new object[] { engine, options, ".*(\\\\XXX).*", "\\XXX", "(0,4)(0,4)" };
                    yield return new object[] { engine, options, "\\\\XXX", "\\XXX", "(0,4)" };
                    yield return new object[] { engine, options, ".*(/000).*", "/000", "(0,4)(0,4)" };
                    yield return new object[] { engine, options, ".*(\\\\000).*", "\\000", "(0,4)(0,4)" };
                    yield return new object[] { engine, options, "\\\\000", "\\000", "(0,4)" };

                    // repetition.dat
                    yield return new object[] { engine, options, "((..)|(.))", "NULL", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.))((..)|(.))", "NULL", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.))((..)|(.))((..)|(.))", "NULL", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.)){1}", "NULL", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.)){2}", "NULL", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.)){3}", "NULL", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.))*", "NULL", "(0,0)" };
                    yield return new object[] { engine, options, "((..)|(.))((..)|(.))", "a", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.))((..)|(.))((..)|(.))", "a", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.)){2}", "a", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.)){3}", "a", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.))((..)|(.))((..)|(.))", "aa", "NOMATCH" };
                    yield return new object[] { engine, options, "((..)|(.)){3}", "aa", "NOMATCH" };
                    yield return new object[] { engine, options, "X(.?){0,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){1,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){2,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){3,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){4,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){5,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){6,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){7,}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){8,}Y", "X1234567Y", "(0,9)(8,8)" };
                    yield return new object[] { engine, options, "X(.?){0,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){1,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){2,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){3,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){4,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){5,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){6,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){7,8}Y", "X1234567Y", "(0,9)(8,8)" }; // was "(0,9)(7,8)"
                    yield return new object[] { engine, options, "X(.?){8,8}Y", "X1234567Y", "(0,9)(8,8)" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd){0,}(d*)", "ababcd", "(0,1)(1,1)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(a|ab|c|bcd){1,}(d*)", "ababcd", "(0,1)(1,1)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(a|ab|c|bcd){2,}(d*)", "ababcd", "(0,6)(3,6)(6,6)" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd){3,}(d*)", "ababcd", "(0,6)(3,6)(6,6)" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd){4,}(d*)", "ababcd", "NOMATCH" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd){0,10}(d*)", "ababcd", "(0,1)(1,1)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(a|ab|c|bcd){1,10}(d*)", "ababcd", "(0,1)(1,1)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(a|ab|c|bcd){2,10}(d*)", "ababcd", "(0,6)(3,6)(6,6)" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd){3,10}(d*)", "ababcd", "(0,6)(3,6)(6,6)" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd){4,10}(d*)", "ababcd", "NOMATCH" };
                    yield return new object[] { engine, options, "(a|ab|c|bcd)*(d*)", "ababcd", "(0,1)(1,1)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(a|ab|c|bcd)+(d*)", "ababcd", "(0,1)(1,1)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){0,}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){1,}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){2,}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){3,}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){4,}(d*)", "ababcd", "NOMATCH" };
                    yield return new object[] { engine, options, "(ab|a|c|bcd){0,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){1,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){2,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){3,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd){4,10}(d*)", "ababcd", "NOMATCH" };
                    yield return new object[] { engine, options, "(ab|a|c|bcd)*(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"
                    yield return new object[] { engine, options, "(ab|a|c|bcd)+(d*)", "ababcd", "(0,6)(4,5)(5,6)" }; // was "(0,6)(3,6)(6,6)"

                    // unknownassoc.dat
                    yield return new object[] { engine, options, "(a|ab)(c|bcd)(d*)", "abcd", "(0,4)(0,1)(1,4)(4,4)" };
                    yield return new object[] { engine, options, "(a|ab)(bcd|c)(d*)", "abcd", "(0,4)(0,1)(1,4)(4,4)" };
                    yield return new object[] { engine, options, "(ab|a)(c|bcd)(d*)", "abcd", "(0,4)(0,2)(2,3)(3,4)" };
                    yield return new object[] { engine, options, "(ab|a)(bcd|c)(d*)", "abcd", "(0,4)(0,2)(2,3)(3,4)" };
                    yield return new object[] { engine, options, "(a*)(b|abc)(c*)", "abc", "(0,3)(0,1)(1,2)(2,3)" };
                    yield return new object[] { engine, options, "(a*)(abc|b)(c*)", "abc", "(0,3)(0,1)(1,2)(2,3)" };
                    yield return new object[] { engine, options, "(a|ab)(c|bcd)(d|.*)", "abcd", "(0,4)(0,1)(1,4)(4,4)" };
                    yield return new object[] { engine, options, "(a|ab)(bcd|c)(d|.*)", "abcd", "(0,4)(0,1)(1,4)(4,4)" };
                    yield return new object[] { engine, options, "(ab|a)(c|bcd)(d|.*)", "abcd", "(0,4)(0,2)(2,3)(3,4)" };
                    yield return new object[] { engine, options, "(ab|a)(bcd|c)(d|.*)", "abcd", "(0,4)(0,2)(2,3)(3,4)" };

                    // nullsubexpr.dat
                    yield return new object[] { engine, options, "(a*)*", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a*)*", "x", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "(a*)*", "aaaaaa", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "(a*)*", "aaaaaax", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "(a*)+", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a+)*", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a+)*", "x", "(0,0)" };
                    yield return new object[] { engine, options, "(a+)+", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a+)+", "x", "NOMATCH" };
                    yield return new object[] { engine, options, "([a]*)*", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "([a]*)+", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "([^b]*)*", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "([^b]*)*", "b", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "([^b]*)*", "aaaaaab", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "([ab]*)*", "a", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "([ab]*)*", "ababab", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "([ab]*)*", "bababa", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "([ab]*)*", "b", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "([ab]*)*", "bbbbbb", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "([ab]*)*", "aaaabcde", "(0,5)(0,5)" };
                    yield return new object[] { engine, options, "([^a]*)*", "b", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "([^a]*)*", "aaaaaa", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "([^ab]*)*", "ccccxx", "(0,6)(0,6)" };
                    yield return new object[] { engine, options, "([^ab]*)*", "ababab", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "((z)+|a)*", "zabcde", "(0,2)(1,2)" };
                    yield return new object[] { engine, options, "a+?", "aaaaaa", "(0,1)" };
                    yield return new object[] { engine, options, "(a)", "aaa", "(0,1)(0,1)" };
                    yield return new object[] { engine, options, "(a*?)", "aaa", "(0,0)(0,0)" };
                    yield return new object[] { engine, options, "(a)*?", "aaa", "(0,0)" };
                    yield return new object[] { engine, options, "(a*?)*?", "aaa", "(0,0)" };
                    yield return new object[] { engine, options, "(a*)*(x)", "x", "(0,1)(0,0)(0,1)" };
                    yield return new object[] { engine, options, "(a*)*(x)(\\1)", "x", "(0,1)(0,0)(0,1)(1,1)", true };
                    yield return new object[] { engine, options, "(a*)*(x)(\\1)", "ax", "(0,2)(1,1)(1,2)(2,2)", true };
                    yield return new object[] { engine, options, "(a*)*(x)(\\1)", "axa", "(0,2)(1,1)(1,2)(2,2)", true }; // was "(0,3)(0,1)(1,2)(2,3)"
                    yield return new object[] { engine, options, "(a*)*(x)(\\1)(x)", "axax", "(0,4)(0,1)(1,2)(2,3)(3,4)", true };
                    yield return new object[] { engine, options, "(a*)*(x)(\\1)(x)", "axxa", "(0,3)(1,1)(1,2)(2,2)(2,3)", true };
                    yield return new object[] { engine, options, "(a*)*(x)", "ax", "(0,2)(1,1)(1,2)" };
                    yield return new object[] { engine, options, "(a*)*(x)", "axa", "(0,2)(1,1)(1,2)" }; // was "(0,2)(0,1)(1,2)"
                    yield return new object[] { engine, options, "(a*)+(x)", "x", "(0,1)(0,0)(0,1)" };
                    yield return new object[] { engine, options, "(a*)+(x)", "ax", "(0,2)(1,1)(1,2)" }; // was "(0,2)(0,1)(1,2)"
                    yield return new object[] { engine, options, "(a*)+(x)", "axa", "(0,2)(1,1)(1,2)" }; // was "(0,2)(0,1)(1,2)"
                    yield return new object[] { engine, options, "(a*){2}(x)", "x", "(0,1)(0,0)(0,1)" };
                    yield return new object[] { engine, options, "(a*){2}(x)", "ax", "(0,2)(1,1)(1,2)" };
                    yield return new object[] { engine, options, "(a*){2}(x)", "axa", "(0,2)(1,1)(1,2)" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Inputs))]
        public async Task Test(RegexEngine engine, RegexOptions options, string pattern, string input, string expected, bool skipNonBacktracking = false)
        {
            if (input == "NULL")
            {
                input = "";
            }

            if (expected == "BADBR")
            {
                await Assert.ThrowsAnyAsync<ArgumentException>(async () => await RegexHelpers.GetRegexAsync(engine, pattern, options));
                return;
            }

            if (engine == RegexEngine.NonBacktracking && skipNonBacktracking)
            {
                // In particular: backreferences are not supported in NonBacktracking mode
                await Assert.ThrowsAnyAsync<NotSupportedException>(() => RegexHelpers.GetRegexAsync(engine, pattern, options));
                return;
            }

            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            if (expected == "NOMATCH")
            {
                Assert.False(r.IsMatch(input));
                return;
            }

            Match match = r.Match(input);
            Assert.True(match.Success);

            var expectedSet = new HashSet<(int start, int end)>(
                expected
                .Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Split(','))
                .Select(s => (start: int.Parse(s[0]), end: int.Parse(s[1]))));

            var actualSet = new HashSet<(int start, int end)>(
                match.Groups
                .Cast<Group>()
                .Select(g => (start: g.Index, end: g.Index + g.Length)));

            // The .NET implementation sometimes has extra captures beyond what the original data specifies, so we assert a subset.
            if (!expectedSet.IsSubsetOf(actualSet))
            {
                throw new Xunit.Sdk.XunitException($"Actual: {string.Join(", ", actualSet)}{Environment.NewLine}Expected: {string.Join(", ", expected)}");
            }
        }
    }
}
