// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexMatchTests
    {
        public static IEnumerable<object[]> Match_Basic_TestData()
        {
            // pattern, input, options, beginning, length, expectedSuccess, expectedValue

            // Testing octal sequence matches: "\\060(\\061)?\\061"
            // Octal \061 is ASCII 49 ('1')
            yield return new object[] { @"\060(\061)?\061", "011", RegexOptions.None, 0, 3, true, "011" };

            // Testing hexadecimal sequence matches: "(\\x30\\x31\\x32)"
            // Hex \x31 is ASCII 49 ('1')
            yield return new object[] { @"(\x30\x31\x32)", "012", RegexOptions.None, 0, 3, true, "012" };

            // Testing control character escapes???: "2", "(\u0032)"
            yield return new object[] { "(\u0034)", "4", RegexOptions.None, 0, 1, true, "4", };

            // Using *, +, ?, {}: Actual - "a+\\.?b*\\.?c{2}"
            yield return new object[] { @"a+\.?b*\.+c{2}", "ab.cc", RegexOptions.None, 0, 5, true, "ab.cc" };

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

            // Using beginning/end of string chars \A, \Z: Actual - "\\Aaaa\\w+zzz\\Z"
            yield return new object[] { @"\Aaaa\w+zzz\Z", "aaaasdfajsdlfjzzz", RegexOptions.IgnoreCase, 0, 17, true, "aaaasdfajsdlfjzzz" };
            yield return new object[] { @"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.IgnoreCase, 0, 4, false, string.Empty };
            yield return new object[] { @"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.RightToLeft, 0, 4, false, string.Empty };
            yield return new object[] { @"\Aaaaaa\w+zzzzz\Z", "aaaa", RegexOptions.RightToLeft, 0, 4, false, string.Empty };
            yield return new object[] { @"\Aaaaaa\w+zzz\Z", "aaaa", RegexOptions.RightToLeft | RegexOptions.IgnoreCase, 0, 4, false, string.Empty };

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
            yield return new object[] { @"(cat)(\c[*)(dog)", "asdlkcat\u00FFdogiwod", RegexOptions.None, 0, 15, false, string.Empty };

            // Surrogate pairs splitted up into UTF-16 code units.
            yield return new object[] { @"(\uD82F[\uDCA0-\uDCA3])", "\uD82F\uDCA2", RegexOptions.CultureInvariant, 0, 2, true, "\uD82F\uDCA2" };
        }

        [Theory]
        [MemberData(nameof(Match_Basic_TestData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Match_Basic_TestData), 2, MemberType = typeof(RegexCompilationHelper))]
        public void Match(string pattern, string input, RegexOptions options, int beginning, int length, bool expectedSuccess, string expectedValue)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, beginning);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, length);
            if (options == RegexOptions.None)
            {
                if (isDefaultStart && isDefaultCount)
                {
                    // Use Match(string) or Match(string, string)
                    VerifyMatch(new Regex(pattern).Match(input), expectedSuccess, expectedValue);
                    VerifyMatch(Regex.Match(input, pattern), expectedSuccess, expectedValue);

                    Assert.Equal(expectedSuccess, new Regex(pattern).IsMatch(input));
                    Assert.Equal(expectedSuccess, Regex.IsMatch(input, pattern));
                }
                if (beginning + length == input.Length)
                {
                    // Use Match(string, int)
                    VerifyMatch(new Regex(pattern).Match(input, beginning), expectedSuccess, expectedValue);

                    Assert.Equal(expectedSuccess, new Regex(pattern).IsMatch(input, beginning));
                }
                // Use Match(string, int, int)
                VerifyMatch(new Regex(pattern).Match(input, beginning, length), expectedSuccess, expectedValue);
            }
            if (isDefaultStart && isDefaultCount)
            {
                // Use Match(string) or Match(string, string, RegexOptions)
                VerifyMatch(new Regex(pattern, options).Match(input), expectedSuccess, expectedValue);
                VerifyMatch(Regex.Match(input, pattern, options), expectedSuccess, expectedValue);

                Assert.Equal(expectedSuccess, Regex.IsMatch(input, pattern, options));
            }
            if (beginning + length == input.Length && (options & RegexOptions.RightToLeft) == 0)
            {
                // Use Match(string, int)
                VerifyMatch(new Regex(pattern, options).Match(input, beginning), expectedSuccess, expectedValue);
            }
            // Use Match(string, int, int)
            VerifyMatch(new Regex(pattern, options).Match(input, beginning, length), expectedSuccess, expectedValue);
        }

        public static void VerifyMatch(Match match, bool expectedSuccess, string expectedValue)
        {
            Assert.Equal(expectedSuccess, match.Success);
            Assert.Equal(expectedValue, match.Value);

            // Groups can never be empty
            Assert.True(match.Groups.Count >= 1);
            Assert.Equal(expectedSuccess, match.Groups[0].Success);
            Assert.Equal(expectedValue, match.Groups[0].Value);
        }

        [Fact]
        public void Match_Timeout()
        {
            Regex regex = new Regex(@"\p{Lu}", RegexOptions.IgnoreCase, TimeSpan.FromHours(1));
            Match match = regex.Match("abc");
            Assert.True(match.Success);
            Assert.Equal("a", match.Value);
        }

        [Fact]
        public void Match_Timeout_Throws()
        {
            RemoteExecutor.Invoke(() =>
            {
                const string Pattern = @"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@(([0-9a-zA-Z])+([-\w]*[0-9a-zA-Z])*\.)+[a-zA-Z]{2,9})$";
                string input = new string('a', 50) + "@a.a";

                AppDomain.CurrentDomain.SetData(RegexHelpers.DefaultMatchTimeout_ConfigKeyName, TimeSpan.FromMilliseconds(100));
                Assert.Throws<RegexMatchTimeoutException>(() => new Regex(Pattern).Match(input));

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        // On 32-bit we can't test these high inputs as they cause OutOfMemoryExceptions.
        [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [InlineData(RegexOptions.Compiled)]
        [InlineData(RegexOptions.None)]
        public void Match_Timeout_Loop_Throws(RegexOptions options)
        {
            var regex = new Regex(@"a\s+", options, TimeSpan.FromSeconds(1));
            string input = @"a" + new string(' ', 800_000_000) + @"b";

            Assert.Throws<RegexMatchTimeoutException>(() => regex.Match(input));
        }

        // On 32-bit we can't test these high inputs as they cause OutOfMemoryExceptions.
        [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [InlineData(RegexOptions.Compiled)]
        [InlineData(RegexOptions.None)]
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
        public void Match(string pattern, string input, RegexOptions options, int beginning, int length, CaptureData[] expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, beginning);
            bool isDefaultCount = RegexHelpers.IsDefaultStart(input, options, length);
            if (options == RegexOptions.None)
            {
                if (isDefaultStart && isDefaultCount)
                {
                    // Use Match(string) or Match(string, string)
                    VerifyMatch(new Regex(pattern).Match(input), true, expected);
                    VerifyMatch(Regex.Match(input, pattern), true, expected);

                    Assert.True(new Regex(pattern).IsMatch(input));
                    Assert.True(Regex.IsMatch(input, pattern));
                }
                if (beginning + length == input.Length)
                {
                    // Use Match(string, int)
                    VerifyMatch(new Regex(pattern).Match(input, beginning), true, expected);

                    Assert.True(new Regex(pattern).IsMatch(input, beginning));
                }
                else
                {
                    // Use Match(string, int, int)
                    VerifyMatch(new Regex(pattern).Match(input, beginning, length), true, expected);
                }
            }
            if (isDefaultStart && isDefaultCount)
            {
                // Use Match(string) or Match(string, string, RegexOptions)
                VerifyMatch(new Regex(pattern, options).Match(input), true, expected);
                VerifyMatch(Regex.Match(input, pattern, options), true, expected);

                Assert.True(Regex.IsMatch(input, pattern, options));
            }
            if (beginning + length == input.Length)
            {
                // Use Match(string, int)
                VerifyMatch(new Regex(pattern, options).Match(input, beginning), true, expected);
            }
            if ((options & RegexOptions.RightToLeft) == 0)
            {
                // Use Match(string, int, int)
                VerifyMatch(new Regex(pattern, options).Match(input, beginning, length), true, expected);
            }
        }

        public static void VerifyMatch(Match match, bool expectedSuccess, CaptureData[] expected)
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
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Match("\u0131", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0131", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Match_SpecialUnicodeCharacters_Invariant()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Match("\u0131", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0131", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0130", "\u0049", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);
                Match("\u0130", "\u0069", RegexOptions.IgnoreCase, 0, 1, false, string.Empty);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotArmProcess))] // times out on ARM
        public void Match_ExcessPrefix()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Should not throw out of memory
                Assert.False(Regex.IsMatch("a", @"a{2147483647,}"));
                Assert.False(Regex.IsMatch("a", @"a{1000001,}")); // 1 over the cutoff for Boyer-Moore prefix

                Assert.False(Regex.IsMatch("a", @"a{50000}")); // creates string for Boyer-Moore but not so large that tests fail and start paging
            }).Dispose();
        }

        [Fact]
        public void Match_Invalid()
        {
            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Match(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Match(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Match(null, "pattern", RegexOptions.None, TimeSpan.FromSeconds(1)));

            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Match(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Match(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Match(null, 0, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Match("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Match("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Match("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Start is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("pattern").Match("input", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("pattern").Match("input", -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("pattern").Match("input", 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("pattern").Match("input", 6, 0));

            // Length is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new Regex("pattern").Match("input", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => new Regex("pattern").Match("input", 0, 6));
        }

        [Fact]
        public void IsMatch_Invalid()
        {
            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.IsMatch(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.IsMatch(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.IsMatch(null, "pattern", RegexOptions.None, TimeSpan.FromSeconds(1)));

            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").IsMatch(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").IsMatch(null, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.IsMatch("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.IsMatch("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.IsMatch("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Start is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("pattern").IsMatch("input", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("pattern").IsMatch("input", 6));
        }
    }
}
