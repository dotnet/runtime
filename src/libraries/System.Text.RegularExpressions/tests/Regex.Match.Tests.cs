// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexMatchTests
    {
        public static IEnumerable<object[]> Match_Basic_TestData()
        {
            // pattern, input, options, beginning, length, expectedSuccess, expectedValue
            yield return new object[] { @"H#", "#H#", RegexOptions.IgnoreCase, 0, 3, true, "H#" }; // https://github.com/dotnet/runtime/issues/39390
            yield return new object[] { @"H#", "#H#", RegexOptions.None, 0, 3, true, "H#" };

            // Testing octal sequence matches: "\\060(\\061)?\\061"
            // Octal \061 is ASCII 49 ('1')
            yield return new object[] { @"\060(\061)?\061", "011", RegexOptions.None, 0, 3, true, "011" };

            // Testing hexadecimal sequence matches: "(\\x30\\x31\\x32)"
            // Hex \x31 is ASCII 49 ('1')
            yield return new object[] { @"(\x30\x31\x32)", "012", RegexOptions.None, 0, 3, true, "012" };

            // Testing control character escapes???: "2", "(\u0032)"
            yield return new object[] { "(\u0034)", "4", RegexOptions.None, 0, 1, true, "4", };

            // Using long loop prefix
            yield return new object[] { @"a{10}", new string('a', 10), RegexOptions.None, 0, 10, true, new string('a', 10) };
            yield return new object[] { @"a{100}", new string('a', 100), RegexOptions.None, 0, 100, true, new string('a', 100) };

            yield return new object[] { @"a{10}b", new string('a', 10) + "bc", RegexOptions.None, 0, 12, true, new string('a', 10) + "b" };
            yield return new object[] { @"a{100}b", new string('a', 100) + "bc", RegexOptions.None, 0, 102, true, new string('a', 100) + "b" };

            yield return new object[] { @"a{11}b", new string('a', 10) + "bc", RegexOptions.None, 0, 12, false, string.Empty };
            yield return new object[] { @"a{101}b", new string('a', 100) + "bc", RegexOptions.None, 0, 102, false, string.Empty };

            yield return new object[] { @"a{1,3}b", "bc", RegexOptions.None, 0, 2, false, string.Empty };
            yield return new object[] { @"a{1,3}b", "abc", RegexOptions.None, 0, 3, true, "ab" };
            yield return new object[] { @"a{1,3}b", "aaabc", RegexOptions.None, 0, 5, true, "aaab" };
            yield return new object[] { @"a{1,3}b", "aaaabc", RegexOptions.None, 0, 6, true, "aaab" };

            yield return new object[] { @"a{2,}b", "abc", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { @"a{2,}b", "aabc", RegexOptions.None, 0, 4, true, "aab" };

            // {,n} is treated as a literal rather than {0,n} as it should be
            yield return new object[] { @"a{,3}b", "a{,3}bc", RegexOptions.None, 0, 6, true, "a{,3}b" };
            yield return new object[] { @"a{,3}b", "aaabc", RegexOptions.None, 0, 5, false, string.Empty };

            // Using [a-z], \s, \w: Actual - "([a-zA-Z]+)\\s(\\w+)"
            yield return new object[] { @"([a-zA-Z]+)\s(\w+)", "David Bau", RegexOptions.None, 0, 9, true, "David Bau" };

            // \\S, \\d, \\D, \\W: Actual - "(\\S+):\\W(\\d+)\\s(\\D+)"
            yield return new object[] { @"(\S+):\W(\d+)\s(\D+)", "Price: 5 dollars", RegexOptions.None, 0, 16, true, "Price: 5 dollars" };

            // \\S, \\d, \\D, \\W: Actual - "[^0-9]+(\\d+)"
            yield return new object[] { @"[^0-9]+(\d+)", "Price: 30 dollars", RegexOptions.None, 0, 17, true, "Price: 30" };

            // Zero-width negative lookahead assertion: Actual - "abc(?!XXX)\\w+"
            yield return new object[] { @"abc(?!XXX)\w+", "abcXXXdef", RegexOptions.None, 0, 9, false, string.Empty };

            // Zero-width positive lookbehind assertion: Actual - "(\\w){6}(?<=XXX)def"
            yield return new object[] { @"(\w){6}(?<=XXX)def", "abcXXXdef", RegexOptions.None, 0, 9, true, "abcXXXdef" };

            // Zero-width negative lookbehind assertion: Actual - "(\\w){6}(?<!XXX)def"
            yield return new object[] { @"(\w){6}(?<!XXX)def", "XXXabcdef", RegexOptions.None, 0, 9, true, "XXXabcdef" };

            // Nonbacktracking subexpression: Actual - "[^0-9]+(?>[0-9]+)3"
            // The last 3 causes the match to fail, since the non backtracking subexpression does not give up the last digit it matched
            // for it to be a success. For a correct match, remove the last character, '3' from the pattern
            yield return new object[] { "[^0-9]+(?>[0-9]+)3", "abc123", RegexOptions.None, 0, 6, false, string.Empty };
            yield return new object[] { "[^0-9]+(?>[0-9]+)", "abc123", RegexOptions.None, 0, 6, true, "abc123" };

            // More nonbacktracking expressions
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.IgnoreCase })
            {
                string Case(string s) => (options & RegexOptions.IgnoreCase) != 0 ? s.ToUpper() : s;

                yield return new object[] { Case("(?>[0-9]+)abc"), "abc12345abc", options, 3, 8, true, "12345abc" };
                yield return new object[] { Case("(?>(?>[0-9]+))abc"), "abc12345abc", options, 3, 8, true, "12345abc" };
                yield return new object[] { Case("(?>[0-9]*)abc"), "abc12345abc", options, 3, 8, true, "12345abc" };
                yield return new object[] { Case("(?>[^z]+)z"), "zzzzxyxyxyz123", options, 4, 9, true, "xyxyxyz" };
                yield return new object[] { Case("(?>(?>[^z]+))z"), "zzzzxyxyxyz123", options, 4, 9, true, "xyxyxyz" };
                yield return new object[] { Case("(?>[^z]*)z123"), "zzzzxyxyxyz123", options, 4, 10, true, "xyxyxyz123" };
                yield return new object[] { Case("(?>a+)123"), "aa1234", options, 0, 5, true, "aa123" };
                yield return new object[] { Case("(?>a*)123"), "aa1234", options, 0, 5, true, "aa123" };
                yield return new object[] { Case("(?>(?>a*))123"), "aa1234", options, 0, 5, true, "aa123" };
                yield return new object[] { Case("(?>a+?)a"), "aaaaa", options, 0, 2, true, "aa" };
                yield return new object[] { Case("(?>a*?)a"), "aaaaa", options, 0, 1, true, "a" };
                yield return new object[] { Case("(?>hi|hello|hey)hi"), "hellohi", options, 0, 0, false, string.Empty };
                yield return new object[] { Case("(?:hi|hello|hey)hi"), "hellohi", options, 0, 7, true, "hellohi" }; // allow backtracking and it succeeds
                yield return new object[] { Case("(?>hi|hello|hey)hi"), "hihi", options, 0, 4, true, "hihi" };
                yield return new object[] { Case(@"a[^wyz]*w"), "abczw", RegexOptions.IgnoreCase, 0, 0, false, string.Empty };
            }

            // Loops at beginning of expressions
            yield return new object[] { @"a+", "aaa", RegexOptions.None, 0, 3, true, "aaa" };
            yield return new object[] { @"a+\d+", "a1", RegexOptions.None, 0, 2, true, "a1" };
            yield return new object[] { @".+\d+", "a1", RegexOptions.None, 0, 2, true, "a1" };
            yield return new object[] { ".+\nabc", "a\nabc", RegexOptions.None, 0, 5, true, "a\nabc" };
            yield return new object[] { @"\d+", "abcd123efg", RegexOptions.None, 0, 10, true, "123" };
            yield return new object[] { @"\d+\d+", "abcd123efg", RegexOptions.None, 0, 10, true, "123" };
            yield return new object[] { @"\w+123\w+", "abcd123efg", RegexOptions.None, 0, 10, true, "abcd123efg" };
            yield return new object[] { @"\d+\w+", "abcd123efg", RegexOptions.None, 0, 10, true, "123efg" };
            yield return new object[] { @"\w+@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"\w{3,}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"\w{4,}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, false, string.Empty };
            yield return new object[] { @"\w{2,5}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"\w{3}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"\w{0,3}@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"\w{0,2}c@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"\w*@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"(\w+)@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"((\w+))@\w+.com", "abc@def.com", RegexOptions.None, 0, 11, true, "abc@def.com" };
            yield return new object[] { @"(\w+)c@\w+.com", "abc@def.comabcdef", RegexOptions.None, 0, 17, true, "abc@def.com" };
            yield return new object[] { @"(\w+)c@\w+.com\1", "abc@def.comabcdef", RegexOptions.None, 0, 17, true, "abc@def.comab" };
            yield return new object[] { @"(\w+)@def.com\1", "abc@def.comab", RegexOptions.None, 0, 13, false, string.Empty };
            yield return new object[] { @"(\w+)@def.com\1", "abc@def.combc", RegexOptions.None, 0, 13, true, "bc@def.combc" };
            yield return new object[] { @"(\w*)@def.com\1", "abc@def.com", RegexOptions.None, 0, 11, true, "@def.com" };
            yield return new object[] { @"\w+(?<!a)", "a", RegexOptions.None, 0, 1, false, string.Empty };
            yield return new object[] { @"\w+(?<!a)", "aa", RegexOptions.None, 0, 2, false, string.Empty };
            yield return new object[] { @"(?>\w+)(?<!a)", "a", RegexOptions.None, 0, 1, false, string.Empty };
            yield return new object[] { @"(?>\w+)(?<!a)", "aa", RegexOptions.None, 0, 2, false, string.Empty };
            yield return new object[] { @".+a", "baa", RegexOptions.None, 0, 3, true, "baa" };
            yield return new object[] { @"[ab]+a", "cacbaac", RegexOptions.None, 0, 7, true, "baa" };
            foreach (RegexOptions lineOption in new[] { RegexOptions.None, RegexOptions.Singleline, RegexOptions.Multiline })
            {
                yield return new object[] { @".*", "abc", lineOption, 1, 2, true, "bc" };
                yield return new object[] { @".*c", "abc", lineOption, 1, 2, true, "bc" };
                yield return new object[] { @"b.*", "abc", lineOption, 1, 2, true, "bc" };
                yield return new object[] { @".*", "abc", lineOption, 2, 1, true, "c" };
            }

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return new object[] { @"\Aaaa\w+zzz\Z", "aaaasdfajsdlfjzzz", RegexOptions.IgnoreCase, 0, 17, true, "aaaasdfajsdlfjzzz" };
            yield return new object[] { @"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.IgnoreCase, 0, 4, false, string.Empty };
            yield return new object[] { @"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.RightToLeft, 0, 4, false, string.Empty };
            yield return new object[] { @"\Aaaaaa\w+zzzzz\Z", "aaaa", RegexOptions.RightToLeft, 0, 4, false, string.Empty };
            yield return new object[] { @"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 0, 4, false, string.Empty };
            yield return new object[] { @"abc\Adef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty };
            yield return new object[] { @"abc\adef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty };
            yield return new object[] { @"abc\Gdef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty };
            yield return new object[] { @"abc^def", "abcdef", RegexOptions.None, 0, 0, false, string.Empty };
            yield return new object[] { @"abc\Zef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty };
            yield return new object[] { @"abc\zef", "abcdef", RegexOptions.None, 0, 0, false, string.Empty };

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return new object[] { @"\Aaaa\w+zzz\Z", "aaaasdfajsdlfjzzza", RegexOptions.None, 0, 18, false, string.Empty };

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return new object[] { @"\A(line2\n)line3\Z", "line2\nline3\n", RegexOptions.Multiline, 0, 12, true, "line2\nline3" };

            // Using beginning/end of string chars ^: Actual - "^b"
            yield return new object[] { "^b", "ab", RegexOptions.None, 0, 2, false, string.Empty };

            // Actual - "(?<char>\\w)\\<char>"
            yield return new object[] { @"(?<char>\w)\<char>", "aa", RegexOptions.None, 0, 2, true, "aa" };

            // Actual - "(?<43>\\w)\\43"
            yield return new object[] { @"(?<43>\w)\43", "aa", RegexOptions.None, 0, 2, true, "aa" };

            // Actual - "abc(?(1)111|222)"
            yield return new object[] { "(abbc)(?(1)111|222)", "abbc222", RegexOptions.None, 0, 7, false, string.Empty };

            // "x" option. Removes unescaped whitespace from the pattern: Actual - " ([^/]+) ","x"
            yield return new object[] { "            ((.)+) #comment     ", "abc", RegexOptions.IgnorePatternWhitespace, 0, 3, true, "abc" };

            // "x" option. Removes unescaped whitespace from the pattern. : Actual - "\x20([^/]+)\x20","x"
            yield return new object[] { "\x20([^/]+)\x20\x20\x20\x20\x20\x20\x20", " abc       ", RegexOptions.IgnorePatternWhitespace, 0, 10, true, " abc      " };

            // Turning on case insensitive option in mid-pattern : Actual - "aaa(?i:match this)bbb"
            if ("i".ToUpper() == "I")
            {
                yield return new object[] { "aaa(?i:match this)bbb", "aaaMaTcH ThIsbbb", RegexOptions.None, 0, 16, true, "aaaMaTcH ThIsbbb" };
            }

            // Turning off case insensitive option in mid-pattern : Actual - "aaa(?-i:match this)bbb", "i"
            yield return new object[] { "aAa(?-i:match this)bbb", "AaAmatch thisBBb", RegexOptions.IgnoreCase, 0, 16, true, "AaAmatch thisBBb" };

            // Turning on/off all the options at once : Actual - "aaa(?imnsx-imnsx:match this)bbb", "i"
            yield return new object[] { "aaa(?imnsx-imnsx:match this)bbb", "AaAmatcH thisBBb", RegexOptions.IgnoreCase, 0, 16, false, string.Empty };

            // Actual - "aaa(?#ignore this completely)bbb"
            yield return new object[] { "aAa(?#ignore this completely)bbb", "aAabbb", RegexOptions.None, 0, 6, true, "aAabbb" };

            // Trying empty string: Actual "[a-z0-9]+", ""
            yield return new object[] { "[a-z0-9]+", "", RegexOptions.None, 0, 0, false, string.Empty };

            // Numbering pattern slots: "(?<1>\\d{3})(?<2>\\d{3})(?<3>\\d{4})"
            yield return new object[] { @"(?<1>\d{3})(?<2>\d{3})(?<3>\d{4})", "8885551111", RegexOptions.None, 0, 10, true, "8885551111" };
            yield return new object[] { @"(?<1>\d{3})(?<2>\d{3})(?<3>\d{4})", "Invalid string", RegexOptions.None, 0, 14, false, string.Empty };

            // Not naming pattern slots at all: "^(cat|chat)"
            yield return new object[] { "^(cat|chat)", "cats are bad", RegexOptions.None, 0, 12, true, "cat" };

            yield return new object[] { "abc", "abc", RegexOptions.None, 0, 3, true, "abc" };
            yield return new object[] { "abc", "aBc", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { "abc", "aBc", RegexOptions.IgnoreCase, 0, 3, true, "aBc" };

            // Using *, +, ?, {}: Actual - "a+\\.?b*\\.?c{2}"
            yield return new object[] { @"a+\.?b*\.+c{2}", "ab.cc", RegexOptions.None, 0, 5, true, "ab.cc" };

            // RightToLeft
            yield return new object[] { @"\s+\d+", "sdf 12sad", RegexOptions.RightToLeft, 0, 9, true, " 12" };
            yield return new object[] { @"\s+\d+", " asdf12 ", RegexOptions.RightToLeft, 0, 6, false, string.Empty };
            yield return new object[] { "aaa", "aaabbb", RegexOptions.None, 3, 3, false, string.Empty };
            yield return new object[] { "abc|def", "123def456", RegexOptions.RightToLeft | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 9, true, "def" };

            yield return new object[] { @"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 3, false, string.Empty };
            yield return new object[] { @"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 11, 21, false, string.Empty };

            // IgnoreCase
            yield return new object[] { "AAA", "aaabbb", RegexOptions.IgnoreCase, 0, 6, true, "aaa" };
            yield return new object[] { @"\p{Lu}", "1bc", RegexOptions.IgnoreCase, 0, 3, true, "b" };
            yield return new object[] { @"\p{Ll}", "1bc", RegexOptions.IgnoreCase, 0, 3, true, "b" };
            yield return new object[] { @"\p{Lt}", "1bc", RegexOptions.IgnoreCase, 0, 3, true, "b" };
            yield return new object[] { @"\p{Lo}", "1bc", RegexOptions.IgnoreCase, 0, 3, false, string.Empty };

            // "\D+"
            yield return new object[] { @"\D+", "12321", RegexOptions.None, 0, 5, false, string.Empty };

            // Groups
            yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", "David Bau", RegexOptions.None, 0, 9, true, "David Bau" };

            // "^b"
            yield return new object[] { "^b", "abc", RegexOptions.None, 0, 3, false, string.Empty };

            // RightToLeft
            yield return new object[] { @"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 0, 32, true, "foo4567890" };
            yield return new object[] { @"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 22, true, "foo4567890" };
            yield return new object[] { @"foo\d+", "0123456789foo4567890foo         ", RegexOptions.RightToLeft, 10, 4, true, "foo4" };

            // Trim leading and trailing whitespace
            yield return new object[] { @"\s*(.*?)\s*$", " Hello World ", RegexOptions.None, 0, 13, true, " Hello World " };

            // < in group
            yield return new object[] { @"(?<cat>cat)\w+(?<dog-0>dog)", "cat_Hello_World_dog", RegexOptions.None, 0, 19, false, string.Empty };

            // Atomic Zero-Width Assertions \A \Z \z \G \b \B
            yield return new object[] { @"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", RegexOptions.None, 0, 20, false, string.Empty };
            yield return new object[] { @"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", RegexOptions.Multiline, 0, 20, false, string.Empty };
            yield return new object[] { @"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", RegexOptions.ECMAScript, 0, 20, false, string.Empty };

            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat", RegexOptions.None, 0, 15, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat     ", RegexOptions.Multiline, 0, 20, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat     ", RegexOptions.ECMAScript, 0, 20, false, string.Empty };

            yield return new object[] { @"(cat)\s+(dog)\z", "cat   dog\n\n\ncat", RegexOptions.None, 0, 15, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   dog\n\n\ncat     ", RegexOptions.Multiline, 0, 20, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   dog\n\n\ncat     ", RegexOptions.ECMAScript, 0, 20, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", RegexOptions.None, 0, 16, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, 0, 16, false, string.Empty };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, 0, 16, false, string.Empty };

            yield return new object[] { @"\b@cat", "123START123;@catEND", RegexOptions.None, 0, 19, false, string.Empty };
            yield return new object[] { @"\b<cat", "123START123'<catEND", RegexOptions.None, 0, 19, false, string.Empty };
            yield return new object[] { @"\b,cat", "satwe,,,START',catEND", RegexOptions.None, 0, 21, false, string.Empty };
            yield return new object[] { @"\b\[cat", "`12START123'[catEND", RegexOptions.None, 0, 19, false, string.Empty };

            yield return new object[] { @"\B@cat", "123START123@catEND", RegexOptions.None, 0, 18, false, string.Empty };
            yield return new object[] { @"\B<cat", "123START123<catEND", RegexOptions.None, 0, 18, false, string.Empty };
            yield return new object[] { @"\B,cat", "satwe,,,START,catEND", RegexOptions.None, 0, 20, false, string.Empty };
            yield return new object[] { @"\B\[cat", "`12START123[catEND", RegexOptions.None, 0, 18, false, string.Empty };

            // Lazy operator Backtracking
            yield return new object[] { @"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/", "http://www.msn.com", RegexOptions.IgnoreCase, 0, 18, false, string.Empty };

            // Grouping Constructs Invalid Regular Expressions
            yield return new object[] { "(?!)", "(?!)cat", RegexOptions.None, 0, 7, false, string.Empty };
            yield return new object[] { "(?<!)", "(?<!)cat", RegexOptions.None, 0, 8, false, string.Empty };

            // Alternation construct
            yield return new object[] { "(?(cat)|dog)", "cat", RegexOptions.None, 0, 3, true, string.Empty };
            yield return new object[] { "(?(cat)|dog)", "catdog", RegexOptions.None, 0, 6, true, string.Empty };
            yield return new object[] { "(?(cat)dog1|dog2)", "catdog1", RegexOptions.None, 0, 7, false, string.Empty };
            yield return new object[] { "(?(cat)dog1|dog2)", "catdog2", RegexOptions.None, 0, 7, true, "dog2" };
            yield return new object[] { "(?(cat)dog1|dog2)", "catdog1dog2", RegexOptions.None, 0, 11, true, "dog2" };
            yield return new object[] { "(?(dog2))", "dog2", RegexOptions.None, 0, 4, true, string.Empty };
            yield return new object[] { "(?(cat)|dog)", "oof", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { "(?(a:b))", "a", RegexOptions.None, 0, 1, true, string.Empty };
            yield return new object[] { "(?(a:))", "a", RegexOptions.None, 0, 1, true, string.Empty };

            // No Negation
            yield return new object[] { "[abcd-[abcd]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { "[1234-[1234]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // All Negation
            yield return new object[] { "[^abcd-[^abcd]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { "[^1234-[^1234]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // No Negation
            yield return new object[] { "[a-z-[a-z]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { "[0-9-[0-9]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // All Negation
            yield return new object[] { "[^a-z-[^a-z]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { "[^0-9-[^0-9]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // No Negation
            yield return new object[] { @"[\w-[\w]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\W-[\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\s-[\s]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\S-[\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\d-[\d]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\D-[\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // All Negation
            yield return new object[] { @"[^\w-[^\w]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\W-[^\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\s-[^\s]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\S-[^\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\d-[^\d]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\D-[^\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // MixedNegation
            yield return new object[] { @"[^\w-[\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\w-[^\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\s-[\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\s-[^\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\d-[\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\d-[^\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // No Negation
            yield return new object[] { @"[\p{Ll}-[\p{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\P{Ll}-[\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\p{Lu}-[\p{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\P{Lu}-[\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\p{Nd}-[\p{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\P{Nd}-[\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // All Negation
            yield return new object[] { @"[^\p{Ll}-[^\p{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\P{Ll}-[^\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\p{Lu}-[^\p{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\P{Lu}-[^\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\p{Nd}-[^\p{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\P{Nd}-[^\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // MixedNegation
            yield return new object[] { @"[^\p{Ll}-[\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\p{Ll}-[^\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\p{Lu}-[\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\p{Lu}-[^\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[^\p{Nd}-[\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };
            yield return new object[] { @"[\p{Nd}-[^\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n", RegexOptions.None, 0, 30, false, string.Empty };

            // Character Class Substraction
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "[]]", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "-]]", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "`]]", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "e]]", RegexOptions.None, 0, 3, false, string.Empty };

            yield return new object[] { @"[ab\-\[cd-[[]]]]", "']]", RegexOptions.None, 0, 3, false, string.Empty };
            yield return new object[] { @"[ab\-\[cd-[[]]]]", "e]]", RegexOptions.None, 0, 3, false, string.Empty };

            yield return new object[] { @"[a-[a-f]]", "abcdefghijklmnopqrstuvwxyz", RegexOptions.None, 0, 26, false, string.Empty };

            // \c
            if (!PlatformDetection.IsNetFramework) // missing fix for https://github.com/dotnet/runtime/issues/24759
            {
                yield return new object[] { @"(cat)(\c[*)(dog)", "asdlkcat\u00FFdogiwod", RegexOptions.None, 0, 15, false, string.Empty };
            }

            // Surrogate pairs split up into UTF-16 code units.
            yield return new object[] { @"(\uD82F[\uDCA0-\uDCA3])", "\uD82F\uDCA2", RegexOptions.CultureInvariant, 0, 2, true, "\uD82F\uDCA2" };

            // Unicode text
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.RightToLeft, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })
            {
                yield return new object[] { "\u05D0\u05D1\u05D2\u05D3(\u05D4\u05D5|\u05D6\u05D7|\u05D8)", "abc\u05D0\u05D1\u05D2\u05D3\u05D4\u05D5def", options, 3, 6, true, "\u05D0\u05D1\u05D2\u05D3\u05D4\u05D5" };
                yield return new object[] { "\u05D0(\u05D4\u05D5|\u05D6\u05D7|\u05D8)", "\u05D0\u05D8", options, 0, 2, true, "\u05D0\u05D8" };
                yield return new object[] { "\u05D0(?:\u05D1|\u05D2|\u05D3)", "\u05D0\u05D2", options, 0, 2, true, "\u05D0\u05D2" };
                yield return new object[] { "\u05D0(?:\u05D1|\u05D2|\u05D3)", "\u05D0\u05D4", options, 0, 0, false, "" };
            }
        }

        public static IEnumerable<object[]> Match_Basic_TestData_NetCore()
        {
            // Unicode symbols in character ranges. These are chars whose lowercase values cannot be found by using the offsets specified in s_lcTable.
            yield return new object[] { @"^(?i:[\u00D7-\u00D8])$", '\u00F7'.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "" };
            yield return new object[] { @"^(?i:[\u00C0-\u00DE])$", '\u00F7'.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "" };
            yield return new object[] { @"^(?i:[\u00C0-\u00DE])$", ((char)('\u00C0' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u00C0' + 32)).ToString() };
            yield return new object[] { @"^(?i:[\u00C0-\u00DE])$", ((char)('\u00DE' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u00DE' + 32)).ToString() };
            yield return new object[] { @"^(?i:[\u0391-\u03AB])$", ((char)('\u03A2' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "" };
            yield return new object[] { @"^(?i:[\u0391-\u03AB])$", ((char)('\u0391' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u0391' + 32)).ToString() };
            yield return new object[] { @"^(?i:[\u0391-\u03AB])$", ((char)('\u03AB' + 32)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u03AB' + 32)).ToString() };
            yield return new object[] { @"^(?i:[\u1F18-\u1F1F])$", ((char)('\u1F1F' - 8)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "" };
            yield return new object[] { @"^(?i:[\u1F18-\u1F1F])$", ((char)('\u1F18' - 8)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u1F18' - 8)).ToString() };
            yield return new object[] { @"^(?i:[\u10A0-\u10C5])$", ((char)('\u10A0' + 7264)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u10A0' + 7264)).ToString() };
            yield return new object[] { @"^(?i:[\u10A0-\u10C5])$", ((char)('\u1F1F' + 48)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "" };
            yield return new object[] { @"^(?i:[\u24B6-\u24D0])$", ((char)('\u24D0' + 26)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, false, "" };
            yield return new object[] { @"^(?i:[\u24B6-\u24D0])$", ((char)('\u24CF' + 26)).ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 0, 1, true, ((char)('\u24CF' + 26)).ToString() };
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Theory]
        [MemberData(nameof(Match_Basic_TestData_NetCore))]
        public void Match_NetCore(string pattern, string input, RegexOptions options, int beginning, int length, bool expectedSuccess, string expectedValue)
        {
            Match(pattern, input, options, beginning, length, expectedSuccess, expectedValue);
        }

        [Theory]
        [MemberData(nameof(Match_Basic_TestData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Match_Basic_TestData), 2, MemberType = typeof(RegexCompilationHelper))]
        public void Match(string pattern, string input, RegexOptions options, int beginning, int length, bool expectedSuccess, string expectedValue)
        {
            Regex r;

            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, beginning);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, length);

            if (options == RegexOptions.None)
            {
                r = new Regex(pattern);

                if (isDefaultStart && isDefaultCount)
                {
                    // Use Match(string) or Match(string, string)
                    VerifyMatch(r.Match(input), expectedSuccess, expectedValue);
                    VerifyMatch(Regex.Match(input, pattern), expectedSuccess, expectedValue);

                    Assert.Equal(expectedSuccess, r.IsMatch(input));
                    Assert.Equal(expectedSuccess, Regex.IsMatch(input, pattern));
                }
                if (beginning + length == input.Length)
                {
                    // Use Match(string, int)
                    VerifyMatch(r.Match(input, beginning), expectedSuccess, expectedValue);

                    Assert.Equal(expectedSuccess, r.IsMatch(input, beginning));
                }
                // Use Match(string, int, int)
                VerifyMatch(r.Match(input, beginning, length), expectedSuccess, expectedValue);
            }

            r = new Regex(pattern, options);

            if (isDefaultStart && isDefaultCount)
            {
                // Use Match(string) or Match(string, string, RegexOptions)
                VerifyMatch(r.Match(input), expectedSuccess, expectedValue);
                VerifyMatch(Regex.Match(input, pattern, options), expectedSuccess, expectedValue);

                Assert.Equal(expectedSuccess, r.IsMatch(input));
                Assert.Equal(expectedSuccess, Regex.IsMatch(input, pattern, options));
            }

            if (beginning + length == input.Length && (options & RegexOptions.RightToLeft) == 0)
            {
                // Use Match(string, int)
                VerifyMatch(r.Match(input, beginning), expectedSuccess, expectedValue);
            }

            // Use Match(string, int, int)
            VerifyMatch(r.Match(input, beginning, length), expectedSuccess, expectedValue);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Takes several minutes on .NET Framework")]
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        [InlineData(RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        [InlineData(RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        public void Match_VaryingLengthStrings(RegexOptions options)
        {
            var lengths = new List<int>() { 2, 3, 4, 5, 6, 7, 8, 9, 31, 32, 33, 63, 64, 65 };
            if ((options & RegexOptions.IgnoreCase) == 0)
            {
                lengths.Add(100_000); // currently produces too large a compiled method for case-insensitive
            }

            bool caseInsensitive = (options & RegexOptions.IgnoreCase) != 0;
            foreach (int length in lengths)
            {
                string pattern = "[123]" + string.Concat(Enumerable.Range(0, length).Select(i => (char)('A' + (i % 26))));
                string input = "2" + string.Concat(Enumerable.Range(0, length).Select(i => (char)((caseInsensitive ? 'a' : 'A') + (i % 26))));
                Match(pattern, input, options, 0, input.Length, expectedSuccess: true, expectedValue: input);
            }
        }

        private static void VerifyMatch(Match match, bool expectedSuccess, string expectedValue)
        {
            Assert.Equal(expectedSuccess, match.Success);
            Assert.Equal(expectedValue, match.Value);

            // Groups can never be empty
            Assert.True(match.Groups.Count >= 1);
            Assert.Equal(expectedSuccess, match.Groups[0].Success);
            Assert.Equal(expectedValue, match.Groups[0].Value);
        }

        [Theory]
        [InlineData(RegexOptions.None, 1)]
        [InlineData(RegexOptions.None, 10)]
        [InlineData(RegexOptions.None, 100)]
        [InlineData(RegexOptions.Compiled, 1)]
        [InlineData(RegexOptions.Compiled, 10)]
        [InlineData(RegexOptions.Compiled, 100)]
        public void Match_DeepNesting(RegexOptions options, int count)
        {
            const string Start = @"((?>abc|(?:def[ghi]", End = @")))";
            const string Match = "defg";

            string pattern = string.Concat(Enumerable.Repeat(Start, count)) + string.Concat(Enumerable.Repeat(End, count));
            string input = string.Concat(Enumerable.Repeat(Match, count));

            var r = new Regex(pattern, options);
            Match m = r.Match(input);

            Assert.True(m.Success);
            Assert.Equal(input, m.Value);
            Assert.Equal(count + 1, m.Groups.Count);
        }

        [Fact]
        public void Match_Timeout()
        {
            Regex regex = new Regex(@"\p{Lu}", RegexOptions.IgnoreCase, TimeSpan.FromHours(1));
            Match match = regex.Match("abc");
            Assert.True(match.Success);
            Assert.Equal("a", match.Value);
        }

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.None | (RegexOptions)0x80 /* Debug */)]
        [InlineData(RegexOptions.Compiled)]
        [InlineData(RegexOptions.Compiled | (RegexOptions)0x80 /* Debug */)]
        public void Match_Timeout_Throws(RegexOptions options)
        {
            const string Pattern = @"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@(([0-9a-zA-Z])+([-\w]*[0-9a-zA-Z])*\.)+[a-zA-Z]{2,9})$";
            string input = new string('a', 50) + "@a.a";

            Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern, options, TimeSpan.FromMilliseconds(100)).Match(input));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.None | (RegexOptions)0x80 /* Debug */)]
        [InlineData(RegexOptions.Compiled)]
        [InlineData(RegexOptions.Compiled | (RegexOptions)0x80 /* Debug */)]
        public void Match_DefaultTimeout_Throws(RegexOptions options)
        {
            RemoteExecutor.Invoke(optionsString =>
            {
                const string Pattern = @"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@(([0-9a-zA-Z])+([-\w]*[0-9a-zA-Z])*\.)+[a-zA-Z]{2,9})$";
                string input = new string('a', 50) + "@a.a";

                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.FromMilliseconds(100));

                if ((RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture) == RegexOptions.None)
                {
                    Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern).Match(input));
                    Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern).IsMatch(input));
                    Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern).Matches(input).Count);

                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.Match(input, Pattern));
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.IsMatch(input, Pattern));
                    Assert.Throws<RegexMatchTimeoutException>(() => Regex.Matches(input, Pattern).Count);
                }

                Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern, (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture)).Match(input));
                Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern, (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture)).IsMatch(input));
                Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern, (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture)).Matches(input).Count);

                Assert.Throws<RegexMatchTimeoutException>(() => Regex.Match(input, Pattern, (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture)));
                Assert.Throws<RegexMatchTimeoutException>(() => Regex.IsMatch(input, Pattern, (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture)));
                Assert.Throws<RegexMatchTimeoutException>(() => Regex.Matches(input, Pattern, (RegexOptions)int.Parse(optionsString, CultureInfo.InvariantCulture)).Count);
            }, ((int)options).ToString(CultureInfo.InvariantCulture)).Dispose();
        }

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.None | (RegexOptions)0x80 /* Debug */)]
        [InlineData(RegexOptions.Compiled)]
        [InlineData(RegexOptions.Compiled | (RegexOptions)0x80 /* Debug */)]
        public void Match_CachedPattern_NewTimeoutApplies(RegexOptions options)
        {
            const string PatternLeadingToLotsOfBacktracking = @"^(\w+\s?)*$";
            Assert.True(Regex.IsMatch("", PatternLeadingToLotsOfBacktracking, options, TimeSpan.FromDays(1)));
            var sw = Stopwatch.StartNew();
            Assert.Throws<RegexMatchTimeoutException>(() => Regex.IsMatch("An input string that takes a very very very very very very very very very very very long time!", PatternLeadingToLotsOfBacktracking, options, TimeSpan.FromMilliseconds(1)));
            Assert.InRange(sw.Elapsed.TotalSeconds, 0, 10); // arbitrary upper bound that should be well above what's needed with a 1ms timeout
        }

        // On 32-bit we can't test these high inputs as they cause OutOfMemoryExceptions.
        // On Linux, we may get killed by the OOM Killer; on Windows, it will swap instead
        [OuterLoop("Can take several seconds")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess), nameof(PlatformDetection.IsWindows))]
        [InlineData(@"a\s+", RegexOptions.None)]
        [InlineData(@"a\s+", RegexOptions.Compiled)]
        [InlineData(@"a\s+ ", RegexOptions.None)]
        [InlineData(@"a\s+ ", RegexOptions.Compiled)]
        public void Match_Timeout_Loop_Throws(string pattern, RegexOptions options)
        {
            var regex = new Regex(pattern, options, TimeSpan.FromSeconds(1));
            string input = "a" + new string(' ', 800_000_000) + " ";
            Assert.Throws<RegexMatchTimeoutException>(() => regex.Match(input));
        }

        // On 32-bit we can't test these high inputs as they cause OutOfMemoryExceptions.
        // On Linux, we may get killed by the OOM Killer; on Windows, it will swap instead
        [OuterLoop("Can take several seconds")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess), nameof(PlatformDetection.IsWindows))]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Match_Timeout_Repetition_Throws(RegexOptions options)
        {
            int repetitionCount = 800_000_000;
            var regex = new Regex(@"a\s{" + repetitionCount+ "}", options, TimeSpan.FromSeconds(1));
            string input = @"a" + new string(' ', repetitionCount) + @"b";
            Assert.Throws<RegexMatchTimeoutException>(() => regex.Match(input));
        }

        public static IEnumerable<object[]> Match_Advanced_TestData()
        {
            // \B special character escape: ".*\\B(SUCCESS)\\B.*"
            yield return new object[]
            {
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
                "(d+?)(e*?)(f??)", "dddeeefff", RegexOptions.None, 0, 9,
                new CaptureData[]
                {
                    new CaptureData("d", 0, 1),
                    new CaptureData("d", 0, 1),
                    new CaptureData(string.Empty, 1, 0),
                    new CaptureData(string.Empty, 1, 0)
                }
            };

            // Noncapturing group : Actual - "(a+)(?:b*)(ccc)"
            yield return new object[]
            {
                "(a+)(?:b*)(ccc)", "aaabbbccc", RegexOptions.None, 0, 9,
                new CaptureData[]
                {
                    new CaptureData("aaabbbccc", 0, 9),
                    new CaptureData("aaa", 0, 3),
                    new CaptureData("ccc", 6, 3),
                }
            };

            // Zero-width positive lookahead assertion: Actual - "abc(?=XXX)\\w+"
            yield return new object[]
            {
                @"abc(?=XXX)\w+", "abcXXXdef", RegexOptions.None, 0, 9,
                new CaptureData[]
                {
                    new CaptureData("abcXXXdef", 0, 9)
                }
            };

            // Backreferences : Actual - "(\\w)\\1"
            yield return new object[]
            {
                @"(\w)\1", "aa", RegexOptions.None, 0, 2,
                new CaptureData[]
                {
                    new CaptureData("aa", 0, 2),
                    new CaptureData("a", 0, 1),
                }
            };

            // Alternation constructs: Actual - "(111|aaa)"
            yield return new object[]
            {
                "(111|aaa)", "aaa", RegexOptions.None, 0, 3,
                new CaptureData[]
                {
                    new CaptureData("aaa", 0, 3),
                    new CaptureData("aaa", 0, 3)
                }
            };

            // Actual - "(?<1>\\d+)abc(?(1)222|111)"
            yield return new object[]
            {
                @"(?<MyDigits>\d+)abc(?(MyDigits)222|111)", "111abc222", RegexOptions.None, 0, 9,
                new CaptureData[]
                {
                    new CaptureData("111abc222", 0, 9),
                    new CaptureData("111", 0, 3)
                }
            };

            // Using "n" Regex option. Only explicitly named groups should be captured: Actual - "([0-9]*)\\s(?<s>[a-z_A-Z]+)", "n"
            yield return new object[]
            {
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
                @".*\b(\w+)\b", "XSP_TEST_FAILURE SUCCESS", RegexOptions.None, 0, 24,
                new CaptureData[]
                {
                    new CaptureData("XSP_TEST_FAILURE SUCCESS", 0, 24),
                    new CaptureData("SUCCESS", 17, 7)
                }
            };

            // Mutliline
            yield return new object[]
            {
                "(line2$\n)line3", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                new CaptureData[]
                {
                    new CaptureData("line2\nline3", 6, 11),
                    new CaptureData("line2\n", 6, 6)
                }
            };

            // Mutliline
            yield return new object[]
            {
                "(line2\n^)line3", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                new CaptureData[]
                {
                    new CaptureData("line2\nline3", 6, 11),
                    new CaptureData("line2\n", 6, 6)
                }
            };

            // Mutliline
            yield return new object[]
            {
                "(line3\n$\n)line4", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                new CaptureData[]
                {
                    new CaptureData("line3\n\nline4", 12, 12),
                    new CaptureData("line3\n\n", 12, 7)
                }
            };

            // Mutliline
            yield return new object[]
            {
                "(line3\n^\n)line4", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                new CaptureData[]
                {
                    new CaptureData("line3\n\nline4", 12, 12),
                    new CaptureData("line3\n\n", 12, 7)
                }
            };

            // Mutliline
            yield return new object[]
            {
                "(line2$\n^)line3", "line1\nline2\nline3\n\nline4", RegexOptions.Multiline, 0, 24,
                new CaptureData[]
                {
                    new CaptureData("line2\nline3", 6, 11),
                    new CaptureData("line2\n", 6, 6)
                }
            };

            // RightToLeft
            yield return new object[]
            {
                "aaa", "aaabbb", RegexOptions.RightToLeft, 3, 3,
                new CaptureData[]
                {
                    new CaptureData("aaa", 0, 3)
                }
            };

            // RightToLeft with anchor
            yield return new object[]
            {
                "^aaa", "aaabbb", RegexOptions.RightToLeft, 3, 3,
                new CaptureData[]
                {
                    new CaptureData("aaa", 0, 3)
                }
            };
            yield return new object[]
            {
                "bbb$", "aaabbb", RegexOptions.RightToLeft, 0, 3,
                new CaptureData[]
                {
                    new CaptureData("bbb", 0, 3)
                }
            };
        }

        [Theory]
        [MemberData(nameof(Match_Advanced_TestData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Match_Advanced_TestData), 2, MemberType = typeof(RegexCompilationHelper))]
        public void Match_Advanced(string pattern, string input, RegexOptions options, int beginning, int length, CaptureData[] expected)
        {
            Regex r;

            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, beginning);
            bool isDefaultCount = RegexHelpers.IsDefaultStart(input, options, length);

            if (options == RegexOptions.None)
            {
                r = new Regex(pattern);

                if (isDefaultStart && isDefaultCount)
                {
                    // Use Match(string) or Match(string, string)
                    VerifyMatch(r.Match(input), true, expected);
                    VerifyMatch(Regex.Match(input, pattern), true, expected);

                    Assert.True(r.IsMatch(input));
                    Assert.True(Regex.IsMatch(input, pattern));
                }

                // Note: this block will fail if any inputs attempt to look for anchors or lookbehinds at the initial position,
                // as there is a difference between Match(input, beginning) and Match(input, beginning, input.Length - beginning)
                // in that the former doesn't modify from 0 what the engine sees as the beginning of the input whereas the latter
                // is equivalent to taking a substring and then matching on that.  However, as we currently don't have any such inputs,
                // it's currently a viable way to test the additional overload.  Same goes for the similar case below with options.
                if (beginning + length == input.Length)
                {
                    // Use Match(string, int)
                    VerifyMatch(r.Match(input, beginning), true, expected);

                    Assert.True(r.IsMatch(input, beginning));
                }

                // Use Match(string, int, int)
                VerifyMatch(r.Match(input, beginning, length), true, expected);
            }

            r = new Regex(pattern, options);

            if (isDefaultStart && isDefaultCount)
            {
                // Use Match(string) or Match(string, string, RegexOptions)
                VerifyMatch(r.Match(input), true, expected);
                VerifyMatch(Regex.Match(input, pattern, options), true, expected);

                Assert.True(Regex.IsMatch(input, pattern, options));
            }

            if (beginning + length == input.Length)
            {
                // Use Match(string, int)
                VerifyMatch(r.Match(input, beginning), true, expected);
            }

            if ((options & RegexOptions.RightToLeft) == 0)
            {
                // Use Match(string, int, int)
                VerifyMatch(r.Match(input, beginning, length), true, expected);
            }
        }

        public static IEnumerable<object[]> Match_StartatDiffersFromBeginning_MemberData()
        {
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Singleline, RegexOptions.Multiline })
            {
                // Anchors
                yield return new object[] { @"^.*", "abc", options, 0, true, true };
                yield return new object[] { @"^.*", "abc", options, 1, false, true };

                // Positive Lookbehinds
                yield return new object[] { @"(?<=abc)def", "abcdef", options, 3, true, false };

                // Negative Lookbehinds
                yield return new object[] { @"(?<!abc)def", "abcdef", options, 3, false, true };
            }
        }

        [Theory]
        [MemberData(nameof(Match_StartatDiffersFromBeginning_MemberData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Match_StartatDiffersFromBeginning_MemberData), 2, MemberType = typeof(RegexCompilationHelper))]
        public void Match_StartatDiffersFromBeginning(string pattern, string input, RegexOptions options, int startat, bool expectedSuccessStartAt, bool expectedSuccessBeginning)
        {
            var r = new Regex(pattern, options);

            Assert.Equal(expectedSuccessStartAt, r.IsMatch(input, startat));
            Assert.Equal(expectedSuccessStartAt, r.Match(input, startat).Success);

            Assert.Equal(expectedSuccessBeginning, r.Match(input.Substring(startat)).Success);
            Assert.Equal(expectedSuccessBeginning, r.Match(input, startat, input.Length - startat).Success);
        }

        private static void VerifyMatch(Match match, bool expectedSuccess, CaptureData[] expected)
        {
            Assert.Equal(expectedSuccess, match.Success);

            Assert.Equal(expected[0].Value, match.Value);
            Assert.Equal(expected[0].Index, match.Index);
            Assert.Equal(expected[0].Length, match.Length);

            Assert.Equal(1, match.Captures.Count);
            Assert.Equal(expected[0].Value, match.Captures[0].Value);
            Assert.Equal(expected[0].Index, match.Captures[0].Index);
            Assert.Equal(expected[0].Length, match.Captures[0].Length);

            Assert.Equal(expected.Length, match.Groups.Count);
            for (int i = 0; i < match.Groups.Count; i++)
            {
                Assert.Equal(expectedSuccess, match.Groups[i].Success);

                Assert.Equal(expected[i].Value, match.Groups[i].Value);
                Assert.Equal(expected[i].Index, match.Groups[i].Index);
                Assert.Equal(expected[i].Length, match.Groups[i].Length);

                Assert.Equal(expected[i].Captures.Length, match.Groups[i].Captures.Count);
                for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                {
                    Assert.Equal(expected[i].Captures[j].Value, match.Groups[i].Captures[j].Value);
                    Assert.Equal(expected[i].Captures[j].Index, match.Groups[i].Captures[j].Index);
                    Assert.Equal(expected[i].Captures[j].Length, match.Groups[i].Captures[j].Length);
                }
            }
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

        [Fact]
        public void Match_SpecialUnicodeCharacters_enUS()
        {
            using (new ThreadCultureChange("en-US"))
            {
                Match("\u0131", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0131", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
            }
        }

        [Fact]
        public void Match_SpecialUnicodeCharacters_Invariant()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                Match("\u0131", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0131", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0130", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0130", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
            }
        }

        private static bool IsNotArmProcessAndRemoteExecutorSupported => PlatformDetection.IsNotArmProcess && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(IsNotArmProcessAndRemoteExecutorSupported))] // times out on ARM
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework does not have fix for https://github.com/dotnet/runtime/issues/24749")]
        [SkipOnCoreClr("Long running tests: https://github.com/dotnet/runtime/issues/10680", RuntimeConfiguration.Checked, RuntimeTestModes.JitMinOpts)]
        public void Match_ExcessPrefix(RegexOptions options)
        {
            RemoteExecutor.Invoke(optionsString =>
            {
                var options = (RegexOptions)Enum.Parse(typeof(RegexOptions), optionsString);

                // Should not throw out of memory

                // Repeaters
                Assert.False(Regex.IsMatch("a", @"a{2147483647,}", options));
                Assert.False(Regex.IsMatch("a", @"a{50,}", options)); // cutoff for Boyer-Moore prefix in debug
                Assert.False(Regex.IsMatch("a", @"a{51,}", options));
                Assert.False(Regex.IsMatch("a", @"a{50_000,}", options)); // cutoff for Boyer-Moore prefix in release
                Assert.False(Regex.IsMatch("a", @"a{50_001,}", options));

                // Multis
                foreach (int length in new[] { 50, 51, 50_000, 50_001, char.MaxValue + 1 }) // based on knowledge of cut-offs used in Boyer-Moore
                {
                    string s = "bcd" + new string('a', length) + "efg";
                    Assert.True(Regex.IsMatch(s, @$"a{{{length}}}", options));
                }
            }, options.ToString()).Dispose();
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
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.IsMatch("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.IsMatch("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.IsMatch("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Start is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => r.IsMatch("input", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.IsMatch("input", 6));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // take too long due to backtracking
        [Theory]
        [InlineData(@"(\w*)+\.", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
        [InlineData(@"(a+)+b", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
        [InlineData(@"(x+x+)+y", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", false)]
        public void IsMatch_SucceedQuicklyDueToAutoAtomicity(string regex, string input, bool expected)
        {
            Assert.Equal(expected, Regex.IsMatch(input, regex, RegexOptions.None));
            Assert.Equal(expected, Regex.IsMatch(input, regex, RegexOptions.Compiled));
        }

        [Fact]
        public void Synchronized()
        {
            var m = new Regex("abc").Match("abc");
            Assert.True(m.Success);
            Assert.Equal("abc", m.Value);

            var m2 = System.Text.RegularExpressions.Match.Synchronized(m);
            Assert.Same(m, m2);
            Assert.True(m2.Success);
            Assert.Equal("abc", m2.Value);

            AssertExtensions.Throws<ArgumentNullException>("inner", () => System.Text.RegularExpressions.Match.Synchronized(null));
        }
    }
}
