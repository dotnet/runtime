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
    public class RegexGroupTests
    {
        private static readonly CultureInfo s_enUSCulture = new CultureInfo("en-US");
        private static readonly CultureInfo s_invariantCulture = new CultureInfo("");
        private static readonly CultureInfo s_czechCulture = new CultureInfo("cs-CZ");
        private static readonly CultureInfo s_danishCulture = new CultureInfo("da-DK");
        private static readonly CultureInfo s_turkishCulture = new CultureInfo("tr-TR");
        private static readonly CultureInfo s_azeriLatinCulture = new CultureInfo("az-Latn-AZ");

        public static IEnumerable<object[]> Groups_Basic_TestData()
        {
            // (A - B) B is a subset of A(ie B only contains chars that are in A)
            yield return new object[] { "[abcd-[d]]+", "dddaabbccddd", RegexOptions.None, new string[] { "aabbcc" } };

            yield return new object[] { @"[\d-[357]]+", "33312468955", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { @"[\d-[357]]+", "51246897", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { @"[\d-[357]]+", "3312468977", RegexOptions.None, new string[] { "124689" } };

            yield return new object[] { @"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { @"[\w-[\d]]+", "0AZaz9", RegexOptions.None, new string[] { "AZaz" } };
            yield return new object[] { @"[\w-[\p{Ll}]]+", "a09AZz", RegexOptions.None, new string[] { "09AZ" } };

            yield return new object[] { @"[\d-[13579]]+", "1024689", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { @"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { @"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" } };

            yield return new object[] { @"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { @"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };
            yield return new object[] { @"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { @"[\p{Ll}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { @"[\p{Nd}-[2468]]+", "20135798", RegexOptions.None, new string[] { "013579" } };

            yield return new object[] { @"[\P{Lu}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { @"[\P{Nd}-[\p{Ll}]]+", "az09AZ'[]", RegexOptions.None, new string[] { "AZ'[]" } };

            // (A - B) B is a superset of A (ie B contains chars that are in A plus other chars that are not in A)
            yield return new object[] { "[abcd-[def]]+", "fedddaabbccddd", RegexOptions.None, new string[] { "aabbcc" } };

            yield return new object[] { @"[\d-[357a-z]]+", "az33312468955", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { @"[\d-[de357fgA-Z]]+", "AZ51246897", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { @"[\d-[357\p{Ll}]]+", "az3312468977", RegexOptions.None, new string[] { "124689" } };

            yield return new object[] { @"[\w-[b-y\s]]+", " \tbbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { @"[\w-[\d\p{Po}]]+", "!#0AZaz9", RegexOptions.None, new string[] { "AZaz" } };
            yield return new object[] { @"[\w-[\p{Ll}\s]]+", "a09AZz", RegexOptions.None, new string[] { "09AZ" } };

            yield return new object[] { @"[\d-[13579a-zA-Z]]+", "AZ1024689", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { @"[\d-[13579abcd]]+", "abcd\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { @"[\d-[13579\s]]+", " \t\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" } };

            yield return new object[] { @"[\w-[b-y\p{Po}]]+", "!#bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { @"[\w-[b-y!.,]]+", "!.,bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };
            yield return new object[] { "[\\w-[b-y\x00-\x0F]]+", "\0bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { @"[\p{Ll}-[ae-z0-9]]+", "09aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { @"[\p{Nd}-[2468az]]+", "az20135798", RegexOptions.None, new string[] { "013579" } };

            yield return new object[] { @"[\P{Lu}-[ae-zA-Z]]+", "AZaaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { @"[\P{Nd}-[\p{Ll}0123456789]]+", "09az09AZ'[]", RegexOptions.None, new string[] { "AZ'[]" } };

            // (A - B) B only contains chars that are not in A
            yield return new object[] { "[abc-[defg]]+", "dddaabbccddd", RegexOptions.None, new string[] { "aabbcc" } };

            yield return new object[] { @"[\d-[abc]]+", "abc09abc", RegexOptions.None, new string[] { "09" } };
            yield return new object[] { @"[\d-[a-zA-Z]]+", "az09AZ", RegexOptions.None, new string[] { "09" } };
            yield return new object[] { @"[\d-[\p{Ll}]]+", "az09az", RegexOptions.None, new string[] { "09" } };

            yield return new object[] { @"[\w-[\x00-\x0F]]+", "bbbaaaABYZ09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABYZ09zzzyyy" } };

            yield return new object[] { @"[\w-[\s]]+", "0AZaz9", RegexOptions.None, new string[] { "0AZaz9" } };
            yield return new object[] { @"[\w-[\W]]+", "0AZaz9", RegexOptions.None, new string[] { "0AZaz9" } };
            yield return new object[] { @"[\w-[\p{Po}]]+", "#a09AZz!", RegexOptions.None, new string[] { "a09AZz" } };

            yield return new object[] { @"[\d-[\D]]+", "azAZ1024689", RegexOptions.ECMAScript, new string[] { "1024689" } };
            yield return new object[] { @"[\d-[a-zA-Z]]+", "azAZ\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { @"[\d-[\p{Ll}]]+", "\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" } };

            yield return new object[] { @"[a-zA-Z0-9-[\s]]+", " \tazAZ09", RegexOptions.None, new string[] { "azAZ09" } };

            yield return new object[] { @"[a-zA-Z0-9-[\W]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABCD09zzzyyy" } };
            yield return new object[] { @"[a-zA-Z0-9-[^a-zA-Z0-9]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABCD09zzzyyy" } };

            yield return new object[] { @"[\p{Ll}-[A-Z]]+", "AZaz09", RegexOptions.None, new string[] { "az" } };
            yield return new object[] { @"[\p{Nd}-[a-z]]+", "az09", RegexOptions.None, new string[] { "09" } };

            yield return new object[] { @"[\P{Lu}-[\p{Lu}]]+", "AZazAZ", RegexOptions.None, new string[] { "az" } };
            yield return new object[] { @"[\P{Lu}-[A-Z]]+", "AZazAZ", RegexOptions.None, new string[] { "az" } };
            yield return new object[] { @"[\P{Nd}-[\p{Nd}]]+", "azAZ09", RegexOptions.None, new string[] { "azAZ" } };
            yield return new object[] { @"[\P{Nd}-[2-8]]+", "1234567890azAZ1234567890", RegexOptions.None, new string[] { "azAZ" } };

            // Alternating construct
            yield return new object[] { @"([ ]|[\w-[0-9]])+", "09az AZ90", RegexOptions.None, new string[] { "az AZ", "Z" } };
            yield return new object[] { @"([0-9-[02468]]|[0-9-[13579]])+", "az1234567890za", RegexOptions.None, new string[] { "1234567890", "0" } };
            yield return new object[] { @"([^0-9-[a-zAE-Z]]|[\w-[a-zAF-Z]])+", "azBCDE1234567890BCDEFza", RegexOptions.None, new string[] { "BCDE1234567890BCDE", "E" } };
            yield return new object[] { @"([\p{Ll}-[aeiou]]|[^\w-[\s]])+", "aeiobcdxyz!@#aeio", RegexOptions.None, new string[] { "bcdxyz!@#", "#" } };

            // Multiple character classes using character class subtraction
            yield return new object[] { @"98[\d-[9]][\d-[8]][\d-[0]]", "98911 98881 98870 98871", RegexOptions.None, new string[] { "98871" } };
            yield return new object[] { @"m[\w-[^aeiou]][\w-[^aeiou]]t", "mbbt mect meet", RegexOptions.None, new string[] { "meet" } };

            // Negation with character class subtraction
            yield return new object[] { "[abcdef-[^bce]]+", "adfbcefda", RegexOptions.None, new string[] { "bce" } };
            yield return new object[] { "[^cde-[ag]]+", "agbfxyzga", RegexOptions.None, new string[] { "bfxyz" } };

            // Misc The idea here is come up with real world examples of char class subtraction. Things that
            // would be difficult to define without it
            yield return new object[] { @"[\p{L}-[^\p{Lu}]]+", "09',.abcxyzABCXYZ", RegexOptions.None, new string[] { "ABCXYZ" } };

            yield return new object[] { @"[\p{IsGreek}-[\P{Lu}]]+", "\u0390\u03FE\u0386\u0388\u03EC\u03EE\u0400", RegexOptions.None, new string[] { "\u03FE\u0386\u0388\u03EC\u03EE" } };
            yield return new object[] { @"[\p{IsBasicLatin}-[G-L]]+", "GAFMZL", RegexOptions.None, new string[] { "AFMZ" } };

            yield return new object[] { "[a-zA-Z-[aeiouAEIOU]]+", "aeiouAEIOUbcdfghjklmnpqrstvwxyz", RegexOptions.None, new string[] { "bcdfghjklmnpqrstvwxyz" } };

            // The following is an overly complex way of matching an ip address using char class subtraction
            yield return new object[] { @"^
            (?<octet>^
                (
                    (
                        (?<Octet2xx>[\d-[013-9]])
                        |
                        [\d-[2-9]]
                    )
                    (?(Octet2xx)
                        (
                            (?<Octet25x>[\d-[01-46-9]])
                            |
                            [\d-[5-9]]
                        )
                        (
                            (?(Octet25x)
                                [\d-[6-9]]
                                |
                                [\d]
                            )
                        )
                        |
                        [\d]{2}
                    )
                )
                |
                ([\d][\d])
                |
                [\d]
            )$"
            , "255", RegexOptions.IgnorePatternWhitespace, new string[] { "255", "255", "2", "5", "5", "", "255", "2", "5" } };

            // Character Class Substraction
            yield return new object[] { @"[abcd\-d-[bc]]+", "bbbaaa---dddccc", RegexOptions.None, new string[] { "aaa---ddd" } };
            yield return new object[] { @"[abcd\-d-[bc]]+", "bbbaaa---dddccc", RegexOptions.None, new string[] { "aaa---ddd" } };
            yield return new object[] { @"[^a-f-[\x00-\x60\u007B-\uFFFF]]+", "aaafffgggzzz{{{", RegexOptions.None, new string[] { "gggzzz" } };
            yield return new object[] { @"[\[\]a-f-[[]]+", "gggaaafff]]][[[", RegexOptions.None, new string[] { "aaafff]]]" } };
            yield return new object[] { @"[\[\]a-f-[]]]+", "gggaaafff[[[]]]", RegexOptions.None, new string[] { "aaafff[[[" } };

            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "a]]", RegexOptions.None, new string[] { "a]]" } };
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "b]]", RegexOptions.None, new string[] { "b]]" } };
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "c]]", RegexOptions.None, new string[] { "c]]" } };
            yield return new object[] { @"[ab\-\[cd-[-[]]]]", "d]]", RegexOptions.None, new string[] { "d]]" } };

            yield return new object[] { @"[ab\-\[cd-[[]]]]", "a]]", RegexOptions.None, new string[] { "a]]" } };
            yield return new object[] { @"[ab\-\[cd-[[]]]]", "b]]", RegexOptions.None, new string[] { "b]]" } };
            yield return new object[] { @"[ab\-\[cd-[[]]]]", "c]]", RegexOptions.None, new string[] { "c]]" } };
            yield return new object[] { @"[ab\-\[cd-[[]]]]", "d]]", RegexOptions.None, new string[] { "d]]" } };
            yield return new object[] { @"[ab\-\[cd-[[]]]]", "-]]", RegexOptions.None, new string[] { "-]]" } };

            yield return new object[] { @"[a-[c-e]]+", "bbbaaaccc", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { @"[a-[c-e]]+", "```aaaccc", RegexOptions.None, new string[] { "aaa" } };

            yield return new object[] { @"[a-d\--[bc]]+", "cccaaa--dddbbb", RegexOptions.None, new string[] { "aaa--ddd" } };

            // Not Character class substraction
            yield return new object[] { @"[\0- [bc]+", "!!!\0\0\t\t  [[[[bbbcccaaa", RegexOptions.None, new string[] { "\0\0\t\t  [[[[bbbccc" } };
            yield return new object[] { "[[abcd]-[bc]]+", "a-b]", RegexOptions.None, new string[] { "a-b]" } };
            yield return new object[] { "[-[e-g]+", "ddd[[[---eeefffggghhh", RegexOptions.None, new string[] { "[[[---eeefffggg" } };
            yield return new object[] { "[-e-g]+", "ddd---eeefffggghhh", RegexOptions.None, new string[] { "---eeefffggg" } };
            yield return new object[] { "[-e-g]+", "ddd---eeefffggghhh", RegexOptions.None, new string[] { "---eeefffggg" } };
            yield return new object[] { "[a-e - m-p]+", "---a b c d e m n o p---", RegexOptions.None, new string[] { "a b c d e m n o p" } };
            yield return new object[] { "[^-[bc]]", "b] c] -] aaaddd]", RegexOptions.None, new string[] { "d]" } };
            yield return new object[] { "[^-[bc]]", "b] c] -] aaa]ddd]", RegexOptions.None, new string[] { "a]" } };

            // Make sure we correctly handle \-
            yield return new object[] { @"[a\-[bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" } };
            yield return new object[] { @"[a\-[\-\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" } };
            yield return new object[] { @"[a\-\[\-\[\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" } };
            yield return new object[] { @"[abc\--[b]]+", "[[[```bbbaaa---cccddd", RegexOptions.None, new string[] { "aaa---ccc" } };
            yield return new object[] { @"[abc\-z-[b]]+", "```aaaccc---zzzbbb", RegexOptions.None, new string[] { "aaaccc---zzz" } };
            yield return new object[] { @"[a-d\-[b]+", "```aaabbbcccddd----[[[[]]]", RegexOptions.None, new string[] { "aaabbbcccddd----[[[[" } };
            yield return new object[] { @"[abcd\-d\-[bc]+", "bbbaaa---[[[dddccc", RegexOptions.None, new string[] { "bbbaaa---[[[dddccc" } };

            // Everything works correctly with option RegexOptions.IgnorePatternWhitespace
            yield return new object[] { "[a - c - [ b ] ]+", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace, new string[] { " ]]]" } };
            yield return new object[] { "[a - c - [ b ] +", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace, new string[] { "aaa   ccc [[[[ bbb " } };

            // Unicode Char Classes
            yield return new object[] { @"(\p{Lu}\w*)\s(\p{Lu}\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { @"(\p{Lu}\p{Ll}*)\s(\p{Lu}\p{Ll}*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { @"(\P{Ll}\p{Ll}*)\s(\P{Ll}\p{Ll}*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { @"(\P{Lu}+\p{Lu})\s(\P{Lu}+\p{Lu})", "hellO worlD", RegexOptions.None, new string[] { "hellO worlD", "hellO", "worlD" } };
            yield return new object[] { @"(\p{Lt}\w*)\s(\p{Lt}*\w*)", "\u01C5ello \u01C5orld", RegexOptions.None, new string[] { "\u01C5ello \u01C5orld", "\u01C5ello", "\u01C5orld" } };
            yield return new object[] { @"(\P{Lt}\w*)\s(\P{Lt}*\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };

            // Character ranges IgnoreCase
            yield return new object[] { @"[@-D]+", "eE?@ABCDabcdeE", RegexOptions.IgnoreCase, new string[] { "@ABCDabcd" } };
            yield return new object[] { @"[>-D]+", "eE=>?@ABCDabcdeE", RegexOptions.IgnoreCase, new string[] { ">?@ABCDabcd" } };
            yield return new object[] { @"[\u0554-\u0557]+", "\u0583\u0553\u0554\u0555\u0556\u0584\u0585\u0586\u0557\u0558", RegexOptions.IgnoreCase, new string[] { "\u0554\u0555\u0556\u0584\u0585\u0586\u0557" } };
            yield return new object[] { @"[X-\]]+", "wWXYZxyz[\\]^", RegexOptions.IgnoreCase, new string[] { "XYZxyz[\\]" } };
            yield return new object[] { @"[X-\u0533]+", "\u0551\u0554\u0560AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563\u0564", RegexOptions.IgnoreCase, new string[] { "AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563" } };
            yield return new object[] { @"[X-a]+", "wWAXYZaxyz", RegexOptions.IgnoreCase, new string[] { "AXYZaxyz" } };
            yield return new object[] { @"[X-c]+", "wWABCXYZabcxyz", RegexOptions.IgnoreCase, new string[] { "ABCXYZabcxyz" } };
            yield return new object[] { @"[X-\u00C0]+", "\u00C1\u00E1\u00C0\u00E0wWABCXYZabcxyz", RegexOptions.IgnoreCase, new string[] { "\u00C0\u00E0wWABCXYZabcxyz" } };
            yield return new object[] { @"[\u0100\u0102\u0104]+", "\u00FF \u0100\u0102\u0104\u0101\u0103\u0105\u0106", RegexOptions.IgnoreCase, new string[] { "\u0100\u0102\u0104\u0101\u0103\u0105" } };
            yield return new object[] { @"[B-D\u0130]+", "aAeE\u0129\u0131\u0068 BCDbcD\u0130\u0069\u0070", RegexOptions.IgnoreCase, new string[] { "BCDbcD\u0130\u0069" } };
            yield return new object[] { @"[\u013B\u013D\u013F]+", "\u013A\u013B\u013D\u013F\u013C\u013E\u0140\u0141", RegexOptions.IgnoreCase, new string[] { "\u013B\u013D\u013F\u013C\u013E\u0140" } };

            // Escape Chars
            yield return new object[] { "(Cat)\r(Dog)", "Cat\rDog", RegexOptions.None, new string[] { "Cat\rDog", "Cat", "Dog" } };
            yield return new object[] { "(Cat)\t(Dog)", "Cat\tDog", RegexOptions.None, new string[] { "Cat\tDog", "Cat", "Dog" } };
            yield return new object[] { "(Cat)\f(Dog)", "Cat\fDog", RegexOptions.None, new string[] { "Cat\fDog", "Cat", "Dog" } };

            // Miscellaneous { witout matching }
            yield return new object[] { @"{5", "hello {5 world", RegexOptions.None, new string[] { "{5" } };
            yield return new object[] { @"{5,", "hello {5, world", RegexOptions.None, new string[] { "{5," } };
            yield return new object[] { @"{5,6", "hello {5,6 world", RegexOptions.None, new string[] { "{5,6" } };

            // Miscellaneous inline options
            yield return new object[] { @"(?n:(?<cat>cat)(\s+)(?<dog>dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "cat", "dog" } };
            yield return new object[] { @"(?n:(cat)(\s+)(dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog" } };
            yield return new object[] { @"(?n:(cat)(?<SpaceChars>\s+)(dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "   " } };
            yield return new object[] { @"(?x:
                            (?<cat>cat) # Cat statement
                            (\s+) # Whitespace chars
                            (?<dog>dog # Dog statement
                            ))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "   ", "cat", "dog" } };
            yield return new object[] { @"(?+i:cat)", "CAT", RegexOptions.None, new string[] { "CAT" } };

            // \d, \D, \s, \S, \w, \W, \P, \p inside character range
            yield return new object[] { @"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.None, new string[] { "cat230927dog", "230927" } };
            yield return new object[] { @"([\D]*)dog", "65498catdog58719", RegexOptions.None, new string[] { "catdog", "cat" } };
            yield return new object[] { @"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.None, new string[] { "cat   dog", "   " } };
            yield return new object[] { @"cat([\S]*)", "sfdcatdog    3270", RegexOptions.None, new string[] { "catdog", "dog" } };
            yield return new object[] { @"cat([\w]*)", "sfdcatdog    3270", RegexOptions.None, new string[] { "catdog", "dog" } };
            yield return new object[] { @"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.None, new string[] { "cat   dog", "   " } };
            yield return new object[] { @"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { @"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };

            // \x, \u, \a, \b, \e, \f, \n, \r, \t, \v, \c, inside character range
            yield return new object[] { @"(cat)([\x41]*)(dog)", "catAAAdog", RegexOptions.None, new string[] { "catAAAdog", "cat", "AAA", "dog" } };
            yield return new object[] { @"(cat)([\u0041]*)(dog)", "catAAAdog", RegexOptions.None, new string[] { "catAAAdog", "cat", "AAA", "dog" } };
            yield return new object[] { @"(cat)([\a]*)(dog)", "cat\a\a\adog", RegexOptions.None, new string[] { "cat\a\a\adog", "cat", "\a\a\a", "dog" } };
            yield return new object[] { @"(cat)([\b]*)(dog)", "cat\b\b\bdog", RegexOptions.None, new string[] { "cat\b\b\bdog", "cat", "\b\b\b", "dog" } };
            yield return new object[] { @"(cat)([\e]*)(dog)", "cat\u001B\u001B\u001Bdog", RegexOptions.None, new string[] { "cat\u001B\u001B\u001Bdog", "cat", "\u001B\u001B\u001B", "dog" } };
            yield return new object[] { @"(cat)([\f]*)(dog)", "cat\f\f\fdog", RegexOptions.None, new string[] { "cat\f\f\fdog", "cat", "\f\f\f", "dog" } };
            yield return new object[] { @"(cat)([\r]*)(dog)", "cat\r\r\rdog", RegexOptions.None, new string[] { "cat\r\r\rdog", "cat", "\r\r\r", "dog" } };
            yield return new object[] { @"(cat)([\v]*)(dog)", "cat\v\v\vdog", RegexOptions.None, new string[] { "cat\v\v\vdog", "cat", "\v\v\v", "dog" } };

            // \d, \D, \s, \S, \w, \W, \P, \p inside character range ([0-5]) with ECMA Option
            yield return new object[] { @"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript, new string[] { "cat230927dog", "230927" } };
            yield return new object[] { @"([\D]*)dog", "65498catdog58719", RegexOptions.ECMAScript, new string[] { "catdog", "cat" } };
            yield return new object[] { @"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "   " } };
            yield return new object[] { @"cat([\S]*)", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "dog" } };
            yield return new object[] { @"cat([\w]*)", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "dog" } };
            yield return new object[] { @"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "   " } };
            yield return new object[] { @"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { @"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "Hello", "World" } };

            // \d, \D, \s, \S, \w, \W, \P, \p outside character range ([0-5]) with ECMA Option
            yield return new object[] { @"(cat)\d*dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript, new string[] { "cat230927dog", "cat" } };
            yield return new object[] { @"\D*(dog)", "65498catdog58719", RegexOptions.ECMAScript, new string[] { "catdog", "dog" } };
            yield return new object[] { @"(cat)\s*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\S*", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "cat" } };
            yield return new object[] { @"(cat)\w*", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "cat" } };
            yield return new object[] { @"(cat)\W*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "cat", "dog" } };
            yield return new object[] { @"\p{Lu}(\w*)\s\p{Lu}(\w*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "ello", "orld" } };
            yield return new object[] { @"\P{Ll}\p{Ll}*\s\P{Ll}\p{Ll}*", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World" } };

            // Use < in a group
            yield return new object[] { @"cat(?<dog121>dog)", "catcatdogdogcat", RegexOptions.None, new string[] { "catdog", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s*(?<cat>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" } };
            yield return new object[] { @"(?<1>cat)\s*(?<1>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" } };
            yield return new object[] { @"(?<2048>cat)\s*(?<2048>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" } };
            yield return new object[] { @"(?<cat>cat)\w+(?<dog-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };
            yield return new object[] { @"(?<cat>cat)\w+(?<-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "" } };
            yield return new object[] { @"(?<cat>cat)\w+(?<cat-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "_Hello_World_" } };
            yield return new object[] { @"(?<1>cat)\w+(?<dog-1>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };
            yield return new object[] { @"(?<cat>cat)\w+(?<2-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };
            yield return new object[] { @"(?<1>cat)\w+(?<2-1>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };

            // Quantifiers
            yield return new object[] { @"(?<cat>cat){", "STARTcat{", RegexOptions.None, new string[] { "cat{", "cat" } };
            yield return new object[] { @"(?<cat>cat){fdsa", "STARTcat{fdsa", RegexOptions.None, new string[] { "cat{fdsa", "cat" } };
            yield return new object[] { @"(?<cat>cat){1", "STARTcat{1", RegexOptions.None, new string[] { "cat{1", "cat" } };
            yield return new object[] { @"(?<cat>cat){1END", "STARTcat{1END", RegexOptions.None, new string[] { "cat{1END", "cat" } };
            yield return new object[] { @"(?<cat>cat){1,", "STARTcat{1,", RegexOptions.None, new string[] { "cat{1,", "cat" } };
            yield return new object[] { @"(?<cat>cat){1,END", "STARTcat{1,END", RegexOptions.None, new string[] { "cat{1,END", "cat" } };
            yield return new object[] { @"(?<cat>cat){1,2", "STARTcat{1,2", RegexOptions.None, new string[] { "cat{1,2", "cat" } };
            yield return new object[] { @"(?<cat>cat){1,2END", "STARTcat{1,2END", RegexOptions.None, new string[] { "cat{1,2END", "cat" } };

            // Use IgnorePatternWhitespace
            yield return new object[] { @"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog
                            ", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" } };
            yield return new object[] { @"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" } };
            yield return new object[] { @"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace) (dog)  (?#followed by dog)", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" } };

            // Back Reference
            yield return new object[] { @"(?<cat>cat)(?<dog>dog)\k<cat>", "asdfcatdogcatdog", RegexOptions.None, new string[] { "catdogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\k<cat>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\k'cat'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\<cat>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\'cat'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };

            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\k<1>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\k'1'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\<1>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\'1'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.ECMAScript, new string[] { "cat   dogcat", "cat", "dog" } };

            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\k<dog>", "asdfcat   dogdog   dog", RegexOptions.None, new string[] { "cat   dogdog", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.None, new string[] { "cat   dogdog", "cat", "dog" } };
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.ECMAScript, new string[] { "cat   dogdog", "cat", "dog" } };

            // Octal
            yield return new object[] { @"(cat)(\077)", "hellocat?dogworld", RegexOptions.None, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { @"(cat)(\77)", "hellocat?dogworld", RegexOptions.None, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { @"(cat)(\176)", "hellocat~dogworld", RegexOptions.None, new string[] { "cat~", "cat", "~" } };
            yield return new object[] { @"(cat)(\400)", "hellocat\0dogworld", RegexOptions.None, new string[] { "cat\0", "cat", "\0" } };
            yield return new object[] { @"(cat)(\300)", "hellocat\u00C0dogworld", RegexOptions.None, new string[] { "cat\u00C0", "cat", "\u00C0" } };
            yield return new object[] { @"(cat)(\300)", "hellocat\u00C0dogworld", RegexOptions.None, new string[] { "cat\u00C0", "cat", "\u00C0" } };
            yield return new object[] { @"(cat)(\477)", "hellocat\u003Fdogworld", RegexOptions.None, new string[] { "cat\u003F", "cat", "\u003F" } };
            yield return new object[] { @"(cat)(\777)", "hellocat\u00FFdogworld", RegexOptions.None, new string[] { "cat\u00FF", "cat", "\u00FF" } };
            yield return new object[] { @"(cat)(\7770)", "hellocat\u00FF0dogworld", RegexOptions.None, new string[] { "cat\u00FF0", "cat", "\u00FF0" } };

            yield return new object[] { @"(cat)(\077)", "hellocat?dogworld", RegexOptions.ECMAScript, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { @"(cat)(\77)", "hellocat?dogworld", RegexOptions.ECMAScript, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { @"(cat)(\7)", "hellocat\adogworld", RegexOptions.ECMAScript, new string[] { "cat\a", "cat", "\a" } };
            yield return new object[] { @"(cat)(\40)", "hellocat dogworld", RegexOptions.ECMAScript, new string[] { "cat ", "cat", " " } };
            yield return new object[] { @"(cat)(\040)", "hellocat dogworld", RegexOptions.ECMAScript, new string[] { "cat ", "cat", " " } };
            yield return new object[] { @"(cat)(\176)", "hellocatcat76dogworld", RegexOptions.ECMAScript, new string[] { "catcat76", "cat", "cat76" } };
            yield return new object[] { @"(cat)(\377)", "hellocat\u00FFdogworld", RegexOptions.ECMAScript, new string[] { "cat\u00FF", "cat", "\u00FF" } };
            yield return new object[] { @"(cat)(\400)", "hellocat 0Fdogworld", RegexOptions.ECMAScript, new string[] { "cat 0", "cat", " 0" } };

            // Decimal
            yield return new object[] { @"(cat)\s+(?<2147483646>dog)", "asdlkcat  dogiwod", RegexOptions.None, new string[] { "cat  dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(?<2147483647>dog)", "asdlkcat  dogiwod", RegexOptions.None, new string[] { "cat  dog", "cat", "dog" } };

            // Hex
            yield return new object[] { @"(cat)(\x2a*)(dog)", "asdlkcat***dogiwod", RegexOptions.None, new string[] { "cat***dog", "cat", "***", "dog" } };
            yield return new object[] { @"(cat)(\x2b*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None, new string[] { "cat+++dog", "cat", "+++", "dog" } };
            yield return new object[] { @"(cat)(\x2c*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None, new string[] { "cat,,,dog", "cat", ",,,", "dog" } };
            yield return new object[] { @"(cat)(\x2d*)(dog)", "asdlkcat---dogiwod", RegexOptions.None, new string[] { "cat---dog", "cat", "---", "dog" } };
            yield return new object[] { @"(cat)(\x2e*)(dog)", "asdlkcat...dogiwod", RegexOptions.None, new string[] { "cat...dog", "cat", "...", "dog" } };
            yield return new object[] { @"(cat)(\x2f*)(dog)", "asdlkcat///dogiwod", RegexOptions.None, new string[] { "cat///dog", "cat", "///", "dog" } };

            yield return new object[] { @"(cat)(\x2A*)(dog)", "asdlkcat***dogiwod", RegexOptions.None, new string[] { "cat***dog", "cat", "***", "dog" } };
            yield return new object[] { @"(cat)(\x2B*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None, new string[] { "cat+++dog", "cat", "+++", "dog" } };
            yield return new object[] { @"(cat)(\x2C*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None, new string[] { "cat,,,dog", "cat", ",,,", "dog" } };
            yield return new object[] { @"(cat)(\x2D*)(dog)", "asdlkcat---dogiwod", RegexOptions.None, new string[] { "cat---dog", "cat", "---", "dog" } };
            yield return new object[] { @"(cat)(\x2E*)(dog)", "asdlkcat...dogiwod", RegexOptions.None, new string[] { "cat...dog", "cat", "...", "dog" } };
            yield return new object[] { @"(cat)(\x2F*)(dog)", "asdlkcat///dogiwod", RegexOptions.None, new string[] { "cat///dog", "cat", "///", "dog" } };

            // ScanControl
            yield return new object[] { @"(cat)(\c@*)(dog)", "asdlkcat\0\0dogiwod", RegexOptions.None, new string[] { "cat\0\0dog", "cat", "\0\0", "dog" } };
            yield return new object[] { @"(cat)(\cA*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None, new string[] { "cat\u0001dog", "cat", "\u0001", "dog" } };
            yield return new object[] { @"(cat)(\ca*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None, new string[] { "cat\u0001dog", "cat", "\u0001", "dog" } };

            yield return new object[] { @"(cat)(\cC*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None, new string[] { "cat\u0003dog", "cat", "\u0003", "dog" } };
            yield return new object[] { @"(cat)(\cc*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None, new string[] { "cat\u0003dog", "cat", "\u0003", "dog" } };

            yield return new object[] { @"(cat)(\cD*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None, new string[] { "cat\u0004dog", "cat", "\u0004", "dog" } };
            yield return new object[] { @"(cat)(\cd*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None, new string[] { "cat\u0004dog", "cat", "\u0004", "dog" } };

            yield return new object[] { @"(cat)(\cX*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None, new string[] { "cat\u0018dog", "cat", "\u0018", "dog" } };
            yield return new object[] { @"(cat)(\cx*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None, new string[] { "cat\u0018dog", "cat", "\u0018", "dog" } };

            yield return new object[] { @"(cat)(\cZ*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None, new string[] { "cat\u001adog", "cat", "\u001a", "dog" } };
            yield return new object[] { @"(cat)(\cz*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None, new string[] { "cat\u001adog", "cat", "\u001a", "dog" } };

            yield return new object[] { @"(cat)(\c[*)(dog)", "asdlkcat\u001bdogiwod", RegexOptions.None, new string[] { "cat\u001bdog", "cat", "\u001b", "dog" } };
            yield return new object[] { @"(cat)(\c[*)(dog)", "asdlkcat\u001Bdogiwod", RegexOptions.None, new string[] { "cat\u001Bdog", "cat", "\u001B", "dog" } };

            // Atomic Zero-Width Assertions \A \Z \z \G \b \B
            //\A
            yield return new object[] { @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\Z
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\z
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\b
            yield return new object[] { @"\b@cat", "123START123@catEND", RegexOptions.None, new string[] { "@cat" } };
            yield return new object[] { @"\b\<cat", "123START123<catEND", RegexOptions.None, new string[] { "<cat" } };
            yield return new object[] { @"\b,cat", "satwe,,,START,catEND", RegexOptions.None, new string[] { ",cat" } };
            yield return new object[] { @"\b\[cat", "`12START123[catEND", RegexOptions.None, new string[] { "[cat" } };

            //\B
            yield return new object[] { @"\B@cat", "123START123;@catEND", RegexOptions.None, new string[] { "@cat" } };
            yield return new object[] { @"\B\<cat", "123START123'<catEND", RegexOptions.None, new string[] { "<cat" } };
            yield return new object[] { @"\B,cat", "satwe,,,START',catEND", RegexOptions.None, new string[] { ",cat" } };
            yield return new object[] { @"\B\[cat", "`12START123'[catEND", RegexOptions.None, new string[] { "[cat" } };

            // \w matching \p{Lm} (Letter, Modifier)
            yield return new object[] { @"(\w+)\s+(\w+)", "cat\u02b0 dog\u02b1", RegexOptions.None, new string[] { "cat\u02b0 dog\u02b1", "cat\u02b0", "dog\u02b1" } };
            yield return new object[] { @"(cat\w+)\s+(dog\w+)", "STARTcat\u30FC dog\u3005END", RegexOptions.None, new string[] { "cat\u30FC dog\u3005END", "cat\u30FC", "dog\u3005END" } };
            yield return new object[] { @"(cat\w+)\s+(dog\w+)", "STARTcat\uff9e dog\uff9fEND", RegexOptions.None, new string[] { "cat\uff9e dog\uff9fEND", "cat\uff9e", "dog\uff9fEND" } };

            // Positive and negative character classes [a-c]|[^b-c]
            yield return new object[] { @"[^a]|d", "d", RegexOptions.None, new string[] { "d" } };
            yield return new object[] { @"([^a]|[d])*", "Hello Worlddf", RegexOptions.None, new string[] { "Hello Worlddf", "f" } };
            yield return new object[] { @"([^{}]|\n)+", "{{{{Hello\n World \n}END", RegexOptions.None, new string[] { "Hello\n World \n", "\n" } };
            yield return new object[] { @"([a-d]|[^abcd])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" } };
            yield return new object[] { @"([^a]|[a])*", "once upon a time", RegexOptions.None, new string[] { "once upon a time", "e" } };
            yield return new object[] { @"([a-d]|[^abcd]|[x-z]|^wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" } };
            yield return new object[] { @"([a-d]|[e-i]|[^e]|wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" } };

            // Canonical and noncanonical char class, where one group is in it's
            // simplest form [a-e] and another is more complex.
            yield return new object[] { @"^(([^b]+ )|(.* ))$", "aaa ", RegexOptions.None, new string[] { "aaa ", "aaa ", "aaa ", "" } };
            yield return new object[] { @"^(([^b]+ )|(.*))$", "aaa", RegexOptions.None, new string[] { "aaa", "aaa", "", "aaa" } };
            yield return new object[] { @"^(([^b]+ )|(.* ))$", "bbb ", RegexOptions.None, new string[] { "bbb ", "bbb ", "", "bbb " } };
            yield return new object[] { @"^(([^b]+ )|(.*))$", "bbb", RegexOptions.None, new string[] { "bbb", "bbb", "", "bbb" } };
            yield return new object[] { @"^((a*)|(.*))$", "aaa", RegexOptions.None, new string[] { "aaa", "aaa", "aaa", "" } };
            yield return new object[] { @"^((a*)|(.*))$", "aaabbb", RegexOptions.None, new string[] { "aaabbb", "aaabbb", "", "aaabbb" } };

            yield return new object[] { @"(([0-9])|([a-z])|([A-Z]))*", "{hello 1234567890 world}", RegexOptions.None, new string[] { "", "", "", "", "" } };
            yield return new object[] { @"(([0-9])|([a-z])|([A-Z]))+", "{hello 1234567890 world}", RegexOptions.None, new string[] { "hello", "o", "", "o", "" } };
            yield return new object[] { @"(([0-9])|([a-z])|([A-Z]))*", "{HELLO 1234567890 world}", RegexOptions.None, new string[] { "", "", "", "", "" } };
            yield return new object[] { @"(([0-9])|([a-z])|([A-Z]))+", "{HELLO 1234567890 world}", RegexOptions.None, new string[] { "HELLO", "O", "", "", "O" } };
            yield return new object[] { @"(([0-9])|([a-z])|([A-Z]))*", "{1234567890 hello  world}", RegexOptions.None, new string[] { "", "", "", "", "" } };
            yield return new object[] { @"(([0-9])|([a-z])|([A-Z]))+", "{1234567890 hello world}", RegexOptions.None, new string[] { "1234567890", "0", "0", "", "" } };

            yield return new object[] { @"^(([a-d]*)|([a-z]*))$", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccdddeeefff", "aaabbbcccdddeeefff", "", "aaabbbcccdddeeefff" } };
            yield return new object[] { @"^(([d-f]*)|([c-e]*))$", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "", "dddeeeccceee" } };
            yield return new object[] { @"^(([c-e]*)|([d-f]*))$", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" } };

            yield return new object[] { @"(([a-d]*)|([a-z]*))", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", "" } };
            yield return new object[] { @"(([d-f]*)|([c-e]*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeee", "dddeee", "dddeee", "" } };
            yield return new object[] { @"(([c-e]*)|([d-f]*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" } };

            yield return new object[] { @"(([a-d]*)|(.*))", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", "" } };
            yield return new object[] { @"(([d-f]*)|(.*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeee", "dddeee", "dddeee", "" } };
            yield return new object[] { @"(([c-e]*)|(.*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" } };

            // \p{Pi} (Punctuation Initial quote) \p{Pf} (Punctuation Final quote)
            yield return new object[] { @"\p{Pi}(\w*)\p{Pf}", "\u00ABCat\u00BB   \u00BBDog\u00AB'", RegexOptions.None, new string[] { "\u00ABCat\u00BB", "Cat" } };
            yield return new object[] { @"\p{Pi}(\w*)\p{Pf}", "\u2018Cat\u2019   \u2019Dog\u2018'", RegexOptions.None, new string[] { "\u2018Cat\u2019", "Cat" } };

            // ECMAScript
            yield return new object[] { @"(?<cat>cat)\s+(?<dog>dog)\s+\123\s+\234", "asdfcat   dog     cat23    dog34eia", RegexOptions.ECMAScript, new string[] { "cat   dog     cat23    dog34", "cat", "dog" } };

            // Balanced Matching
            yield return new object[] { @"<div> 
            (?> 
                <div>(?<DEPTH>) |   
                </div> (?<-DEPTH>) |  
                .?
            )*?
            (?(DEPTH)(?!)) 
            </div>", "<div>this is some <div>red</div> text</div></div></div>", RegexOptions.IgnorePatternWhitespace, new string[] { "<div>this is some <div>red</div> text</div>", "" } };

            yield return new object[] { @"(
            ((?'open'<+)[^<>]*)+
            ((?'close-open'>+)[^<>]*)+
            )+", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "<02deep_03<03deep_03>>>", "<03deep_03", ">>>", "<", "03deep_03" } };

            yield return new object[] { @"(
            (?<start><)?
            [^<>]?
            (?<end-start>>)?
            )*", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "", "", "01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>" } };

            yield return new object[] { @"(
            (?<start><[^/<>]*>)?
            [^<>]?
            (?<end-start></[^/<>]*>)?
            )*", "<b><a>Cat</a></b>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b><a>Cat</a></b>", "", "", "<a>Cat</a>" } };

            yield return new object[] { @"(
            (?<start><(?<TagName>[^/<>]*)>)?
            [^<>]?
            (?<end-start></\k<TagName>>)?
            )*", "<b>cat</b><a>dog</a>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b>cat</b><a>dog</a>", "", "", "a", "dog" } };

            // Balanced Matching With Backtracking
            yield return new object[] { @"(
            (?<start><[^/<>]*>)?
            .?
            (?<end-start></[^/<>]*>)?
            )*
            (?(start)(?!)) ", "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", "", "", "<a>Cat" } };

            // Character Classes and Lazy quantifier
            yield return new object[] { @"([0-9]+?)([\w]+?)", "55488aheiaheiad", RegexOptions.ECMAScript, new string[] { "55", "5", "5" } };
            yield return new object[] { @"([0-9]+?)([a-z]+?)", "55488aheiaheiad", RegexOptions.ECMAScript, new string[] { "55488a", "55488", "a" } };

            // Miscellaneous/Regression scenarios
            yield return new object[] { @"(?<openingtag>1)(?<content>.*?)(?=2)", "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine + "2", RegexOptions.Singleline | RegexOptions.ExplicitCapture,
            new string[] { "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine, "1", Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>"+ Environment.NewLine } };

            yield return new object[] { @"\G<%#(?<code>.*?)?%>", @"<%# DataBinder.Eval(this, ""MyNumber"") %>", RegexOptions.Singleline, new string[] { @"<%# DataBinder.Eval(this, ""MyNumber"") %>", @" DataBinder.Eval(this, ""MyNumber"") " } };

            // Nested Quantifiers
            yield return new object[] { @"^[abcd]{0,0x10}*$", "a{0,0x10}}}", RegexOptions.None, new string[] { "a{0,0x10}}}" } };

            // Lazy operator Backtracking
            yield return new object[] { @"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/", "http://www.msn.com/", RegexOptions.IgnoreCase, new string[] { "http://www.msn.com/", "com", string.Empty } };
            yield return new object[] { @"http://([a-zA-Z0-9\-]*\.?)*?/", @"http://www.google.com/", RegexOptions.IgnoreCase, new string[] { "http://www.google.com/", "com" } };

            yield return new object[] { @"([a-z]*?)([\w])", "cat", RegexOptions.IgnoreCase, new string[] { "c", string.Empty, "c" } };
            yield return new object[] { @"^([a-z]*?)([\w])$", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" } };

            // Backtracking
            yield return new object[] { @"([a-z]*)([\w])", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" } };
            yield return new object[] { @"^([a-z]*)([\w])$", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" } };

            // Quantifiers
            yield return new object[] { @"(cat){", "cat{", RegexOptions.None, new string[] { "cat{", "cat" } };
            yield return new object[] { @"(cat){}", "cat{}", RegexOptions.None, new string[] { "cat{}", "cat" } };
            yield return new object[] { @"(cat){,", "cat{,", RegexOptions.None, new string[] { "cat{,", "cat" } };
            yield return new object[] { @"(cat){,}", "cat{,}", RegexOptions.None, new string[] { "cat{,}", "cat" } };
            yield return new object[] { @"(cat){cat}", "cat{cat}", RegexOptions.None, new string[] { "cat{cat}", "cat" } };
            yield return new object[] { @"(cat){cat,5}", "cat{cat,5}", RegexOptions.None, new string[] { "cat{cat,5}", "cat" } };
            yield return new object[] { @"(cat){5,dog}", "cat{5,dog}", RegexOptions.None, new string[] { "cat{5,dog}", "cat" } };
            yield return new object[] { @"(cat){cat,dog}", "cat{cat,dog}", RegexOptions.None, new string[] { "cat{cat,dog}", "cat" } };
            yield return new object[] { @"(cat){,}?", "cat{,}?", RegexOptions.None, new string[] { "cat{,}", "cat" } };
            yield return new object[] { @"(cat){cat}?", "cat{cat}?", RegexOptions.None, new string[] { "cat{cat}", "cat" } };
            yield return new object[] { @"(cat){cat,5}?", "cat{cat,5}?", RegexOptions.None, new string[] { "cat{cat,5}", "cat" } };
            yield return new object[] { @"(cat){5,dog}?", "cat{5,dog}?", RegexOptions.None, new string[] { "cat{5,dog}", "cat" } };
            yield return new object[] { @"(cat){cat,dog}?", "cat{cat,dog}?", RegexOptions.None, new string[] { "cat{cat,dog}", "cat" } };

            // Grouping Constructs Invalid Regular Expressions
            yield return new object[] { @"()", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty } };
            yield return new object[] { @"(?<cat>)", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty } };
            yield return new object[] { @"(?'cat')", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty } };
            yield return new object[] { @"(?:)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { @"(?imn)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { @"(?imn)cat", "(?imn)cat", RegexOptions.None, new string[] { "cat" } };
            yield return new object[] { @"(?=)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { @"(?<=)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { @"(?>)", "cat", RegexOptions.None, new string[] { string.Empty } };

            // Alternation construct Invalid Regular Expressions
            yield return new object[] { @"(?()|)", "(?()|)", RegexOptions.None, new string[] { "" } };

            yield return new object[] { @"(?(cat)|)", "cat", RegexOptions.None, new string[] { "" } };
            yield return new object[] { @"(?(cat)|)", "dog", RegexOptions.None, new string[] { "" } };

            yield return new object[] { @"(?(cat)catdog|)", "catdog", RegexOptions.None, new string[] { "catdog" } };
            yield return new object[] { @"(?(cat)catdog|)", "dog", RegexOptions.None, new string[] { "" } };
            yield return new object[] { @"(?(cat)dog|)", "dog", RegexOptions.None, new string[] { "" } };
            yield return new object[] { @"(?(cat)dog|)", "cat", RegexOptions.None, new string[] { "" } };

            yield return new object[] { @"(?(cat)|catdog)", "cat", RegexOptions.None, new string[] { "" } };
            yield return new object[] { @"(?(cat)|catdog)", "catdog", RegexOptions.None, new string[] { "" } };
            yield return new object[] { @"(?(cat)|dog)", "dog", RegexOptions.None, new string[] { "dog" } };

            // Invalid unicode
            yield return new object[] { "([\u0000-\uFFFF-[azAZ09]]|[\u0000-\uFFFF-[^azAZ09]])+", "azAZBCDE1234567890BCDEFAZza", RegexOptions.None, new string[] { "azAZBCDE1234567890BCDEFAZza", "a" } };
            yield return new object[] { "[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]+", "abcxyzABCXYZ123890", RegexOptions.None, new string[] { "bcxyzABCXYZ123890" } };
            yield return new object[] { "[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]]+", "bcxyzABCXYZ123890a", RegexOptions.None, new string[] { "a" } };
            yield return new object[] { "[\u0000-\uFFFF-[\\p{P}\\p{S}\\p{C}]]+", "!@`';.,$+<>=\x0001\x001FazAZ09", RegexOptions.None, new string[] { "azAZ09" } };

            yield return new object[] { @"[\uFFFD-\uFFFF]+", "\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase, new string[] { "\uFFFD\uFFFE\uFFFF" } };
            yield return new object[] { @"[\uFFFC-\uFFFE]+", "\uFFFB\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase, new string[] { "\uFFFC\uFFFD\uFFFE" } };

            // Empty Match
            yield return new object[] { @"([a*]*)+?$", "ab", RegexOptions.None, new string[] { "", "" } };
            yield return new object[] { @"(a*)+?$", "b", RegexOptions.None, new string[] { "", "" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_enUS()
        {
            yield return new object[] { "CH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
            yield return new object[] { "cH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
            yield return new object[] { "AA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
            yield return new object[] { "aA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
            yield return new object[] { "\u0130", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" } };
            yield return new object[] { "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_Czech()
        {
            yield return new object[] { "CH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
            yield return new object[] { "cH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
        }


        public static IEnumerable<object[]> Groups_CustomCulture_TestData_Danish()
        {
            yield return new object[] { "AA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
            yield return new object[] { "aA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_Turkish()
        {
            yield return new object[] { "\u0131", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" } };
            yield return new object[] { "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_AzeriLatin()
        {
            yield return new object[] { "\u0131", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" } };
            yield return new object[] { "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" } };
        }

        private static CultureInfo GetDefaultCultureForTests()
        {
            CultureInfo defaultCulture = CultureInfo.CurrentCulture;

            // In invariant culture, the unicode char matches differ from expected values provided.
            if (defaultCulture.Equals(CultureInfo.InvariantCulture))
            {
                defaultCulture = new CultureInfo("en-US");
            }
            
            return defaultCulture;
        }

        public void Groups(string pattern, string input, RegexOptions options, string[] expectedGroups)
        {
            Regex regex = new Regex(pattern, options);
            Match match = regex.Match(input);
            Assert.True(match.Success, $"match.Success. pattern=/{pattern}/  input=[[[{input}]]]  culture={CultureInfo.CurrentCulture.Name}");

            Assert.Equal(expectedGroups.Length, match.Groups.Count);
            Assert.True(expectedGroups[0] == match.Value, string.Format("Culture used: {0}", CultureInfo.CurrentCulture));

            int[] groupNumbers = regex.GetGroupNumbers();
            string[] groupNames = regex.GetGroupNames();
            for (int i = 0; i < expectedGroups.Length; i++)
            {
                Assert.Equal(expectedGroups[i], match.Groups[groupNumbers[i]].Value);
                Assert.Equal(match.Groups[groupNumbers[i]], match.Groups[groupNames[i]]);

                Assert.Equal(groupNumbers[i], regex.GroupNumberFromName(groupNames[i]));
                Assert.Equal(groupNames[i], regex.GroupNameFromNumber(groupNumbers[i]));
            }
        }

        private void GroupsTest(object[] testCase)
        {
            Groups((string)testCase[0], (string)testCase[1], (RegexOptions)testCase[2], (string[])testCase[3]);
        }


        [Fact]
        public void GroupsEnUS()
        {
            RemoteExecutor.Invoke(() => {
                CultureInfo.CurrentCulture = s_enUSCulture;
                foreach (object[] testCase in Groups_CustomCulture_TestData_enUS())
                {
                    GroupsTest(testCase);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GroupsCzech()
        {
            RemoteExecutor.Invoke(() => {
                CultureInfo.CurrentCulture = s_czechCulture;
                foreach (object[] testCase in Groups_CustomCulture_TestData_Czech())
                {
                    GroupsTest(testCase);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GroupsDanish()
        {
            RemoteExecutor.Invoke(() => {
                CultureInfo.CurrentCulture = s_danishCulture;
                foreach (object[] testCase in Groups_CustomCulture_TestData_Danish())
                {
                    GroupsTest(testCase);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GroupsTurkish()
        {
            RemoteExecutor.Invoke(() => {
                CultureInfo.CurrentCulture = s_turkishCulture;
                foreach (object[] testCase in Groups_CustomCulture_TestData_Turkish())
                {
                    GroupsTest(testCase);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GroupsAzeriLatin()
        {
            RemoteExecutor.Invoke(() => {
                CultureInfo.CurrentCulture = s_azeriLatinCulture;
                foreach (object[] testCase in Groups_CustomCulture_TestData_AzeriLatin())
                {
                    GroupsTest(testCase);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GroupsBasic()
        {
            RemoteExecutor.Invoke(() => {
                CultureInfo.CurrentCulture = GetDefaultCultureForTests();
                foreach (object[] testCase in Groups_Basic_TestData())
                {
                    GroupsTest(testCase);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Synchronized_NullGroup_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("inner", () => Group.Synchronized(null));
        }

        [Theory]
        [InlineData(@"(cat)([\v]*)(dog)", "cat\v\v\vdog")]
        [InlineData("abc", "def")] // no match
        public void Synchronized_ValidGroup_Success(string pattern, string input)
        {
            Match match = Regex.Match(input, pattern);

            Group synchronizedGroup = Group.Synchronized(match.Groups[0]);
            Assert.NotNull(synchronizedGroup);
        }
    }
}
