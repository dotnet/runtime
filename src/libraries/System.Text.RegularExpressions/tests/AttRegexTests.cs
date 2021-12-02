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
//                 writer.WriteLine($"[InlineData(\"{pattern}\", \"{input}\", \"{captures}\")]");
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
        [Theory]

        // basic.dat
        [InlineData("abracadabra$", "abracadabracadabra", "(7,18)")]
        [InlineData("a...b", "abababbb", "(2,7)")]
        [InlineData("XXXXXX", "..XXXXXX", "(2,8)")]
        [InlineData("\\)", "()", "(1,2)")]
        [InlineData("a]", "a]a", "(0,2)")]
        [InlineData("}", "}", "(0,1)")]
        [InlineData("\\}", "}", "(0,1)")]
        [InlineData("\\]", "]", "(0,1)")]
        [InlineData("]", "]", "(0,1)")]
        [InlineData("{", "{", "(0,1)")]
        [InlineData("^a", "ax", "(0,1)")]
        [InlineData("\\^a", "a^a", "(1,3)")]
        [InlineData("a\\^", "a^", "(0,2)")]
        [InlineData("a$", "aa", "(1,2)")]
        [InlineData("a\\$", "a$", "(0,2)")]
        [InlineData("^$", "NULL", "(0,0)")]
        [InlineData("$^", "NULL", "(0,0)")]
        [InlineData("a($)", "aa", "(1,2)(2,2)")]
        [InlineData("a*(^a)", "aa", "(0,1)(0,1)")]
        [InlineData("(..)*(...)*", "a", "(0,0)")]
        [InlineData("(..)*(...)*", "abcd", "(0,4)(2,4)")]
        [InlineData("(ab|a)(bc|c)", "abc", "(0,3)(0,2)(2,3)")]
        [InlineData("(ab)c|abc", "abc", "(0,3)(0,2)")]
        [InlineData("a{0}b", "ab", "(1,2)")]
        [InlineData("(a*)(b?)(b+)b{3}", "aaabbbbbbb", "(0,10)(0,3)(3,4)(4,7)")]
        [InlineData("(a*)(b{0,1})(b{1,})b{3}", "aaabbbbbbb", "(0,10)(0,3)(3,4)(4,7)")]
        [InlineData("a{9876543210}", "NULL", "BADBR")]
        [InlineData("((a|a)|a)", "a", "(0,1)(0,1)(0,1)")]
        [InlineData("(a*)(a|aa)", "aaaa", "(0,4)(0,3)(3,4)")]
        [InlineData("a*(a.|aa)", "aaaa", "(0,4)(2,4)")]
        [InlineData("(a|b)?.*", "b", "(0,1)(0,1)")]
        [InlineData("(a|b)c|a(b|c)", "ac", "(0,2)(0,1)")]
        [InlineData("(a|b)*c|(a|ab)*c", "abc", "(0,3)(1,2)")]
        [InlineData("(a|b)*c|(a|ab)*c", "xc", "(1,2)")]
        [InlineData("(.a|.b).*|.*(.a|.b)", "xa", "(0,2)(0,2)")]
        [InlineData("a?(ab|ba)ab", "abab", "(0,4)(0,2)")]
        [InlineData("a?(ac{0}b|ba)ab", "abab", "(0,4)(0,2)")]
        [InlineData("ab|abab", "abbabab", "(0,2)")]
        [InlineData("aba|bab|bba", "baaabbbaba", "(5,8)")]
        [InlineData("aba|bab", "baaabbbaba", "(6,9)")]
        [InlineData("(aa|aaa)*|(a|aaaaa)", "aa", "(0,2)(0,2)")]
        [InlineData("(a.|.a.)*|(a|.a...)", "aa", "(0,2)(0,2)")]
        [InlineData("ab|a", "xabc", "(1,3)", "(1,2)")]
        [InlineData("ab|a", "xxabc", "(2,4)", "(2,3)")]
        [InlineData("(?i)(Ab|cD)*", "aBcD", "(0,4)(2,4)")]
        [InlineData("[^-]", "--a", "(2,3)")]
        [InlineData("[a-]*", "--a", "(0,3)")]
        [InlineData("[a-m-]*", "--amoma--", "(0,4)")]
        [InlineData(":::1:::0:|:::1:1:0:", ":::0:::1:::1:::0:", "(8,17)")]
        [InlineData(":::1:::0:|:::1:1:1:", ":::0:::1:::1:::0:", "(8,17)")]
        [InlineData("\n", "\n", "(0,1)")]
        [InlineData("[^a]", "\n", "(0,1)")]
        [InlineData("\na", "\na", "(0,2)")]
        [InlineData("(a)(b)(c)", "abc", "(0,3)(0,1)(1,2)(2,3)")]
        [InlineData("xxx", "xxx", "(0,3)")]
        [InlineData("(^|[ (,;])((([Ff]eb[^ ]* *|0*2/|\\* */?)0*[6-7]))([^0-9]|$)", "feb 6,", "(0,6)")]
        [InlineData("(^|[ (,;])((([Ff]eb[^ ]* *|0*2/|\\* */?)0*[6-7]))([^0-9]|$)", "2/7", "(0,3)")]
        [InlineData("(^|[ (,;])((([Ff]eb[^ ]* *|0*2/|\\* */?)0*[6-7]))([^0-9]|$)", "feb 1,Feb 6", "(5,11)")]
        [InlineData("((((((((((((((((((((((((((((((x))))))))))))))))))))))))))))))", "x", "(0,1)(0,1)(0,1)")]
        [InlineData("((((((((((((((((((((((((((((((x))))))))))))))))))))))))))))))*", "xx", "(0,2)(1,2)(1,2)")]
        [InlineData("a?(ab|ba)*", "ababababababababababababababababababababababababababababababababababababababababa", "(0,81)(79,81)")]
        [InlineData("abaa|abbaa|abbbaa|abbbbaa", "ababbabbbabbbabbbbabbbbaa", "(18,25)")]
        [InlineData("abaa|abbaa|abbbaa|abbbbaa", "ababbabbbabbbabbbbabaa", "(18,22)")]
        [InlineData("aaac|aabc|abac|abbc|baac|babc|bbac|bbbc", "baaabbbabac", "(7,11)")]
        [InlineData(".*", "\x0001\x00ff", "(0,2)")]
        [InlineData("aaaa|bbbb|cccc|ddddd|eeeeee|fffffff|gggg|hhhh|iiiii|jjjjj|kkkkk|llll", "XaaaXbbbXcccXdddXeeeXfffXgggXhhhXiiiXjjjXkkkXlllXcbaXaaaa", "(53,57)")]
        [InlineData("aaaa\nbbbb\ncccc\nddddd\neeeeee\nfffffff\ngggg\nhhhh\niiiii\njjjjj\nkkkkk\nllll", "XaaaXbbbXcccXdddXeeeXfffXgggXhhhXiiiXjjjXkkkXlllXcbaXaaaa", "NOMATCH")]
        [InlineData("a*a*a*a*a*b", "aaaaaaaaab", "(0,10)")]
        [InlineData("^", "NULL", "(0,0)")]
        [InlineData("$", "NULL", "(0,0)")]
        [InlineData("^a$", "a", "(0,1)")]
        [InlineData("abc", "abc", "(0,3)")]
        [InlineData("abc", "xabcy", "(1,4)")]
        [InlineData("abc", "ababc", "(2,5)")]
        [InlineData("ab*c", "abc", "(0,3)")]
        [InlineData("ab*bc", "abc", "(0,3)")]
        [InlineData("ab*bc", "abbc", "(0,4)")]
        [InlineData("ab*bc", "abbbbc", "(0,6)")]
        [InlineData("ab+bc", "abbc", "(0,4)")]
        [InlineData("ab+bc", "abbbbc", "(0,6)")]
        [InlineData("ab?bc", "abbc", "(0,4)")]
        [InlineData("ab?bc", "abc", "(0,3)")]
        [InlineData("ab?c", "abc", "(0,3)")]
        [InlineData("^abc$", "abc", "(0,3)")]
        [InlineData("^abc", "abcc", "(0,3)")]
        [InlineData("abc$", "aabc", "(1,4)")]
        [InlineData("^", "abc", "(0,0)")]
        [InlineData("$", "abc", "(3,3)")]
        [InlineData("a.c", "abc", "(0,3)")]
        [InlineData("a.c", "axc", "(0,3)")]
        [InlineData("a.*c", "axyzc", "(0,5)")]
        [InlineData("a[bc]d", "abd", "(0,3)")]
        [InlineData("a[b-d]e", "ace", "(0,3)")]
        [InlineData("a[b-d]", "aac", "(1,3)")]
        [InlineData("a[-b]", "a-", "(0,2)")]
        [InlineData("a[b-]", "a-", "(0,2)")]
        [InlineData("a]", "a]", "(0,2)")]
        [InlineData("a[]]b", "a]b", "(0,3)")]
        [InlineData("a[^bc]d", "aed", "(0,3)")]
        [InlineData("a[^-b]c", "adc", "(0,3)")]
        [InlineData("a[^]b]c", "adc", "(0,3)")]
        [InlineData("ab|cd", "abc", "(0,2)")]
        [InlineData("ab|cd", "abcd", "(0,2)")]
        [InlineData("a\\(b", "a(b", "(0,3)")]
        [InlineData("a\\(*b", "ab", "(0,2)")]
        [InlineData("a\\(*b", "a((b", "(0,4)")]
        [InlineData("((a))", "abc", "(0,1)(0,1)(0,1)")]
        [InlineData("(a)b(c)", "abc", "(0,3)(0,1)(2,3)")]
        [InlineData("a+b+c", "aabbabc", "(4,7)")]
        [InlineData("a*", "aaa", "(0,3)")]
        [InlineData("(a*)*", "-", "(0,0)(0,0)")]
        [InlineData("(a*)+", "-", "(0,0)(0,0)")]
        [InlineData("(a*|b)*", "-", "(0,0)(0,0)")]
        [InlineData("(a+|b)*", "ab", "(0,2)(1,2)")]
        [InlineData("(a+|b)+", "ab", "(0,2)(1,2)")]
        [InlineData("(a+|b)?", "ab", "(0,1)(0,1)")]
        [InlineData("[^ab]*", "cde", "(0,3)")]
        [InlineData("(^)*", "-", "(0,0)(0,0)")]
        [InlineData("a*", "NULL", "(0,0)")]
        [InlineData("([abc])*d", "abbbcd", "(0,6)(4,5)")]
        [InlineData("([abc])*bcd", "abcd", "(0,4)(0,1)")]
        [InlineData("a|b|c|d|e", "e", "(0,1)")]
        [InlineData("(a|b|c|d|e)f", "ef", "(0,2)(0,1)")]
        [InlineData("((a*|b))*", "-", "(0,0)(0,0)(0,0)")]
        [InlineData("abcd*efg", "abcdefg", "(0,7)")]
        [InlineData("ab*", "xabyabbbz", "(1,3)")]
        [InlineData("ab*", "xayabbbz", "(1,2)")]
        [InlineData("(ab|cd)e", "abcde", "(2,5)(2,4)")]
        [InlineData("[abhgefdc]ij", "hij", "(0,3)")]
        [InlineData("(a|b)c*d", "abcd", "(1,4)(1,2)")]
        [InlineData("(ab|ab*)bc", "abc", "(0,3)(0,1)")]
        [InlineData("a([bc]*)c*", "abc", "(0,3)(1,3)")]
        [InlineData("a([bc]*)(c*d)", "abcd", "(0,4)(1,3)(3,4)")]
        [InlineData("a([bc]+)(c*d)", "abcd", "(0,4)(1,3)(3,4)")]
        [InlineData("a([bc]*)(c+d)", "abcd", "(0,4)(1,2)(2,4)")]
        [InlineData("a[bcd]*dcdcde", "adcdcde", "(0,7)")]
        [InlineData("(ab|a)b*c", "abc", "(0,3)(0,2)")]
        [InlineData("((a)(b)c)(d)", "abcd", "(0,4)(0,3)(0,1)(1,2)(3,4)")]
        [InlineData("[A-Za-z_][A-Za-z0-9_]*", "alpha", "(0,5)")]
        [InlineData("^a(bc+|b[eh])g|.h$", "abh", "(1,3)")]
        [InlineData("(bc+d$|ef*g.|h?i(j|k))", "effgz", "(0,5)(0,5)")]
        [InlineData("(bc+d$|ef*g.|h?i(j|k))", "ij", "(0,2)(0,2)(1,2)")]
        [InlineData("(bc+d$|ef*g.|h?i(j|k))", "reffgz", "(1,6)(1,6)")]
        [InlineData("(((((((((a)))))))))", "a", "(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)(0,1)")]
        [InlineData("multiple words", "multiple words yeah", "(0,14)")]
        [InlineData("(.*)c(.*)", "abcde", "(0,5)(0,2)(3,5)")]
        [InlineData("abcd", "abcd", "(0,4)")]
        [InlineData("a(bc)d", "abcd", "(0,4)(1,3)")]
        [InlineData("a[-]?c", "ac", "(0,3)")]
        [InlineData("a+(b|c)*d+", "aabcdd", "(0,6)(3,4)")]
        [InlineData("^.+$", "vivi", "(0,4)")]
        [InlineData("^(.+)$", "vivi", "(0,4)(0,4)")]
        [InlineData("^([^!.]+).att.com!(.+)$", "gryphon.att.com!eby", "(0,19)(0,7)(16,19)")]
        [InlineData("^([^!]+!)?([^!]+)$", "bar!bas", "(0,7)(0,4)(4,7)")]
        [InlineData("^([^!]+!)?([^!]+)$", "foo!bas", "(0,7)(0,4)(4,7)")]
        [InlineData("^.+!([^!]+!)([^!]+)$", "foo!bar!bas", "(0,11)(4,8)(8,11)")]
        [InlineData("((foo)|(bar))!bas", "foo!bas", "(0,7)(0,3)(0,3)")]
        [InlineData("((foo)|bar)!bas", "bar!bas", "(0,7)(0,3)")]
        [InlineData("((foo)|bar)!bas", "foo!bar!bas", "(4,11)(4,7)")]
        [InlineData("((foo)|bar)!bas", "foo!bas", "(0,7)(0,3)(0,3)")]
        [InlineData("(foo|(bar))!bas", "bar!bas", "(0,7)(0,3)(0,3)")]
        [InlineData("(foo|(bar))!bas", "foo!bar!bas", "(4,11)(4,7)(4,7)")]
        [InlineData("(foo|(bar))!bas", "foo!bas", "(0,7)(0,3)")]
        [InlineData("(foo|bar)!bas", "bar!bas", "(0,7)(0,3)")]
        [InlineData("(foo|bar)!bas", "foo!bar!bas", "(4,11)(4,7)")]
        [InlineData("(foo|bar)!bas", "foo!bas", "(0,7)(0,3)")]
        [InlineData("^([^!]+!)?([^!]+)$|^.+!([^!]+!)([^!]+)$", "bar!bas", "(0,7)(0,4)(4,7)")]
        [InlineData("^([^!]+!)?([^!]+)$|^.+!([^!]+!)([^!]+)$", "foo!bas", "(0,7)(0,4)(4,7)")]
        [InlineData("^(([^!]+!)?([^!]+)|.+!([^!]+!)([^!]+))$", "bar!bas", "(0,7)(0,7)(0,4)(4,7)")]
        [InlineData("^(([^!]+!)?([^!]+)|.+!([^!]+!)([^!]+))$", "foo!bas", "(0,7)(0,7)(0,4)(4,7)")]
        [InlineData(".*(/XXX).*", "/XXX", "(0,4)(0,4)")]
        [InlineData(".*(\\\\XXX).*", "\\XXX", "(0,4)(0,4)")]
        [InlineData("\\\\XXX", "\\XXX", "(0,4)")]
        [InlineData(".*(/000).*", "/000", "(0,4)(0,4)")]
        [InlineData(".*(\\\\000).*", "\\000", "(0,4)(0,4)")]
        [InlineData("\\\\000", "\\000", "(0,4)")]

        // repetition.dat
        [InlineData("((..)|(.))", "NULL", "NOMATCH")]
        [InlineData("((..)|(.))((..)|(.))", "NULL", "NOMATCH")]
        [InlineData("((..)|(.))((..)|(.))((..)|(.))", "NULL", "NOMATCH")]
        [InlineData("((..)|(.)){1}", "NULL", "NOMATCH")]
        [InlineData("((..)|(.)){2}", "NULL", "NOMATCH")]
        [InlineData("((..)|(.)){3}", "NULL", "NOMATCH")]
        [InlineData("((..)|(.))*", "NULL", "(0,0)")]
        [InlineData("((..)|(.))((..)|(.))", "a", "NOMATCH")]
        [InlineData("((..)|(.))((..)|(.))((..)|(.))", "a", "NOMATCH")]
        [InlineData("((..)|(.)){2}", "a", "NOMATCH")]
        [InlineData("((..)|(.)){3}", "a", "NOMATCH")]
        [InlineData("((..)|(.))((..)|(.))((..)|(.))", "aa", "NOMATCH")]
        [InlineData("((..)|(.)){3}", "aa", "NOMATCH")]
        [InlineData("X(.?){0,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){1,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){2,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){3,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){4,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){5,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){6,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){7,}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){8,}Y", "X1234567Y", "(0,9)(8,8)")]
        [InlineData("X(.?){0,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){1,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){2,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){3,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){4,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){5,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){6,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){7,8}Y", "X1234567Y", "(0,9)(8,8)")] // was "(0,9)(7,8)"
        [InlineData("X(.?){8,8}Y", "X1234567Y", "(0,9)(8,8)")]
        [InlineData("(a|ab|c|bcd){0,}(d*)", "ababcd", "(0,1)(1,1)", "(0,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(a|ab|c|bcd){1,}(d*)", "ababcd", "(0,1)(1,1)", "(0,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(a|ab|c|bcd){2,}(d*)", "ababcd", "(0,6)(3,6)(6,6)")]
        [InlineData("(a|ab|c|bcd){3,}(d*)", "ababcd", "(0,6)(3,6)(6,6)")]
        [InlineData("(a|ab|c|bcd){4,}(d*)", "ababcd", "NOMATCH")]
        [InlineData("(a|ab|c|bcd){0,10}(d*)", "ababcd", "(0,1)(1,1)", "(0,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(a|ab|c|bcd){1,10}(d*)", "ababcd", "(0,1)(1,1)", "(0,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(a|ab|c|bcd){2,10}(d*)", "ababcd", "(0,6)(3,6)(6,6)")]
        [InlineData("(a|ab|c|bcd){3,10}(d*)", "ababcd", "(0,6)(3,6)(6,6)")]
        [InlineData("(a|ab|c|bcd){4,10}(d*)", "ababcd", "NOMATCH")]
        [InlineData("(a|ab|c|bcd)*(d*)", "ababcd", "(0,1)(1,1)", "(0,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(a|ab|c|bcd)+(d*)", "ababcd", "(0,1)(1,1)", "(0,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){0,}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){1,}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){2,}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){3,}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){4,}(d*)", "ababcd", "NOMATCH")]
        [InlineData("(ab|a|c|bcd){0,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){1,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){2,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){3,10}(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd){4,10}(d*)", "ababcd", "NOMATCH")]
        [InlineData("(ab|a|c|bcd)*(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"
        [InlineData("(ab|a|c|bcd)+(d*)", "ababcd", "(0,6)(4,5)(5,6)")] // was "(0,6)(3,6)(6,6)"

        // unknownassoc.dat
        [InlineData("(a|ab)(c|bcd)(d*)", "abcd", "(0,4)(0,1)(1,4)(4,4)")]
        [InlineData("(a|ab)(bcd|c)(d*)", "abcd", "(0,4)(0,1)(1,4)(4,4)")]
        [InlineData("(ab|a)(c|bcd)(d*)", "abcd", "(0,4)(0,2)(2,3)(3,4)")]
        [InlineData("(ab|a)(bcd|c)(d*)", "abcd", "(0,4)(0,2)(2,3)(3,4)")]
        [InlineData("(a*)(b|abc)(c*)", "abc", "(0,3)(0,1)(1,2)(2,3)")]
        [InlineData("(a*)(abc|b)(c*)", "abc", "(0,3)(0,1)(1,2)(2,3)")]
        [InlineData("(a|ab)(c|bcd)(d|.*)", "abcd", "(0,4)(0,1)(1,4)(4,4)")]
        [InlineData("(a|ab)(bcd|c)(d|.*)", "abcd", "(0,4)(0,1)(1,4)(4,4)")]
        [InlineData("(ab|a)(c|bcd)(d|.*)", "abcd", "(0,4)(0,2)(2,3)(3,4)")]
        [InlineData("(ab|a)(bcd|c)(d|.*)", "abcd", "(0,4)(0,2)(2,3)(3,4)")]

        // nullsubexpr.dat
        [InlineData("(a*)*", "a", "(0,1)(0,1)")]
        [InlineData("(a*)*", "x", "(0,0)(0,0)")]
        [InlineData("(a*)*", "aaaaaa", "(0,6)(0,6)")]
        [InlineData("(a*)*", "aaaaaax", "(0,6)(0,6)")]
        [InlineData("(a*)+", "a", "(0,1)(0,1)")]
        [InlineData("(a+)*", "a", "(0,1)(0,1)")]
        [InlineData("(a+)*", "x", "(0,0)")]
        [InlineData("(a+)+", "a", "(0,1)(0,1)")]
        [InlineData("(a+)+", "x", "NOMATCH")]
        [InlineData("([a]*)*", "a", "(0,1)(0,1)")]
        [InlineData("([a]*)+", "a", "(0,1)(0,1)")]
        [InlineData("([^b]*)*", "a", "(0,1)(0,1)")]
        [InlineData("([^b]*)*", "b", "(0,0)(0,0)")]
        [InlineData("([^b]*)*", "aaaaaab", "(0,6)(0,6)")]
        [InlineData("([ab]*)*", "a", "(0,1)(0,1)")]
        [InlineData("([ab]*)*", "ababab", "(0,6)(0,6)")]
        [InlineData("([ab]*)*", "bababa", "(0,6)(0,6)")]
        [InlineData("([ab]*)*", "b", "(0,1)(0,1)")]
        [InlineData("([ab]*)*", "bbbbbb", "(0,6)(0,6)")]
        [InlineData("([ab]*)*", "aaaabcde", "(0,5)(0,5)")]
        [InlineData("([^a]*)*", "b", "(0,1)(0,1)")]
        [InlineData("([^a]*)*", "aaaaaa", "(0,0)(0,0)")]
        [InlineData("([^ab]*)*", "ccccxx", "(0,6)(0,6)")]
        [InlineData("([^ab]*)*", "ababab", "(0,0)(0,0)")]
        [InlineData("((z)+|a)*", "zabcde", "(0,2)(1,2)")]
        [InlineData("a+?", "aaaaaa", "(0,1)")]
        [InlineData("(a)", "aaa", "(0,1)(0,1)")]
        [InlineData("(a*?)", "aaa", "(0,0)(0,0)")]
        [InlineData("(a)*?", "aaa", "(0,0)")]
        [InlineData("(a*?)*?", "aaa", "(0,0)")]
        [InlineData("(a*)*(x)", "x", "(0,1)(0,0)(0,1)")]
        [InlineData("(a*)*(x)(\\1)", "x", "(0,1)(0,0)(0,1)(1,1)", "NONBACKTRACKINGINCOMPATIBLE")]
        [InlineData("(a*)*(x)(\\1)", "ax", "(0,2)(1,1)(1,2)(2,2)", "NONBACKTRACKINGINCOMPATIBLE")]
        [InlineData("(a*)*(x)(\\1)", "axa", "(0,2)(1,1)(1,2)(2,2)", "NONBACKTRACKINGINCOMPATIBLE")] // was "(0,3)(0,1)(1,2)(2,3)"
        [InlineData("(a*)*(x)(\\1)(x)", "axax", "(0,4)(0,1)(1,2)(2,3)(3,4)", "NONBACKTRACKINGINCOMPATIBLE")]
        [InlineData("(a*)*(x)(\\1)(x)", "axxa", "(0,3)(1,1)(1,2)(2,2)(2,3)", "NONBACKTRACKINGINCOMPATIBLE")]
        [InlineData("(a*)*(x)", "ax", "(0,2)(1,1)(1,2)")]
        [InlineData("(a*)*(x)", "axa", "(0,2)(1,1)(1,2)")] // was "(0,2)(0,1)(1,2)"
        [InlineData("(a*)+(x)", "x", "(0,1)(0,0)(0,1)")]
        [InlineData("(a*)+(x)", "ax", "(0,2)(1,1)(1,2)")] // was "(0,2)(0,1)(1,2)"
        [InlineData("(a*)+(x)", "axa", "(0,2)(1,1)(1,2)")] // was "(0,2)(0,1)(1,2)"
        [InlineData("(a*){2}(x)", "x", "(0,1)(0,0)(0,1)")]
        [InlineData("(a*){2}(x)", "ax", "(0,2)(1,1)(1,2)")]
        [InlineData("(a*){2}(x)", "axa", "(0,2)(1,1)(1,2)")]
        public async Task Test(string pattern, string input, string captures, string nonBacktrackingCaptures = null)
        {
            if (input == "NULL")
            {
                input = "";
            }

            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Multiline })
                {
                    bool nonBacktracking = engine == RegexEngine.NonBacktracking;
                    string expected = nonBacktracking && nonBacktrackingCaptures != null ?
                        nonBacktrackingCaptures : // nonBacktrackingCaptures value overrides the expected result in NonBacktracking mode
                        captures;

                    if (expected == "BADBR")
                    {
                        await Assert.ThrowsAnyAsync<ArgumentException>(async () => await RegexHelpers.GetRegexAsync(engine, pattern, options));
                        return;
                    }

                    if (nonBacktracking && nonBacktrackingCaptures == "NONBACKTRACKINGINCOMPATIBLE")
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
                        .Select(s => (start: int.Parse(s[0]), end: int.Parse(s[1])))
                        .Distinct()
                        .OrderBy(c => c.start)
                        .ThenBy(c => c.end));

                    var actualSet = new HashSet<(int start, int end)>(
                        match.Groups
                        .Cast<Group>()
                        .Select(g => (start: g.Index, end: g.Index + g.Length))
                        .Distinct()
                        .OrderBy(g => g.start)
                        .ThenBy(g => g.end));

                    // NonBacktracking mode only provides the top-level match.
                    // The .NET implementation sometimes has extra captures beyond what the original data specifies, so we assert a subset.
                    if (nonBacktracking ? !actualSet.IsSubsetOf(expectedSet) : !expectedSet.IsSubsetOf(actualSet))
                    {
                        throw new Xunit.Sdk.XunitException($"Actual: {string.Join(", ", actualSet)}{Environment.NewLine}Expected: {string.Join(", ", expected)}");
                    }
                }
            }
        }
    }
}
