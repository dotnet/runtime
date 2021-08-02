// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexGroupTests
    {
        public static IEnumerable<object[]> Groups_Basic_TestData()
        {
            // (A - B) B is a subset of A(ie B only contains chars that are in A)
            yield return new object[] { null, "[abcd-[d]]+", "dddaabbccddd", RegexOptions.None, new string[] { "aabbcc" } };

            yield return new object[] { null, @"[\d-[357]]+", "33312468955", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { null, @"[\d-[357]]+", "51246897", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { null, @"[\d-[357]]+", "3312468977", RegexOptions.None, new string[] { "124689" } };

            yield return new object[] { null, @"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { null, @"[\w-[\d]]+", "0AZaz9", RegexOptions.None, new string[] { "AZaz" } };
            yield return new object[] { null, @"[\w-[\p{Ll}]]+", "a09AZz", RegexOptions.None, new string[] { "09AZ" } };

            yield return new object[] { null, @"[\d-[13579]]+", "1024689", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { null, @"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { null, @"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" } };

            yield return new object[] { null, @"[\p{Ll}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { null, @"[\p{Nd}-[2468]]+", "20135798", RegexOptions.None, new string[] { "013579" } };

            yield return new object[] { null, @"[\P{Lu}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { null, @"[\P{Nd}-[\p{Ll}]]+", "az09AZ'[]", RegexOptions.None, new string[] { "AZ'[]" } };

            // (A - B) B is a superset of A (ie B contains chars that are in A plus other chars that are not in A)
            yield return new object[] { null, "[abcd-[def]]+", "fedddaabbccddd", RegexOptions.None, new string[] { "aabbcc" } };

            yield return new object[] { null, @"[\d-[357a-z]]+", "az33312468955", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { null, @"[\d-[de357fgA-Z]]+", "AZ51246897", RegexOptions.None, new string[] { "124689" } };
            yield return new object[] { null, @"[\d-[357\p{Ll}]]+", "az3312468977", RegexOptions.None, new string[] { "124689" } };

            yield return new object[] { null, @"[\w-[b-y\s]]+", " \tbbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { null, @"[\w-[\d\p{Po}]]+", "!#0AZaz9", RegexOptions.None, new string[] { "AZaz" } };
            yield return new object[] { null, @"[\w-[\p{Ll}\s]]+", "a09AZz", RegexOptions.None, new string[] { "09AZ" } };

            yield return new object[] { null, @"[\d-[13579a-zA-Z]]+", "AZ1024689", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { null, @"[\d-[13579abcd]]+", "abcd\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { null, @"[\d-[13579\s]]+", " \t\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" } };

            yield return new object[] { null, @"[\w-[b-y\p{Po}]]+", "!#bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { null, @"[\w-[b-y!.,]]+", "!.,bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };
            yield return new object[] { null, "[\\w-[b-y\x00-\x0F]]+", "\0bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" } };

            yield return new object[] { null, @"[\p{Ll}-[ae-z0-9]]+", "09aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { null, @"[\p{Nd}-[2468az]]+", "az20135798", RegexOptions.None, new string[] { "013579" } };

            yield return new object[] { null, @"[\P{Lu}-[ae-zA-Z]]+", "AZaaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" } };
            yield return new object[] { null, @"[\P{Nd}-[\p{Ll}0123456789]]+", "09az09AZ'[]", RegexOptions.None, new string[] { "AZ'[]" } };

            // (A - B) B only contains chars that are not in A
            yield return new object[] { null, "[abc-[defg]]+", "dddaabbccddd", RegexOptions.None, new string[] { "aabbcc" } };

            yield return new object[] { null, @"[\d-[abc]]+", "abc09abc", RegexOptions.None, new string[] { "09" } };
            yield return new object[] { null, @"[\d-[a-zA-Z]]+", "az09AZ", RegexOptions.None, new string[] { "09" } };
            yield return new object[] { null, @"[\d-[\p{Ll}]]+", "az09az", RegexOptions.None, new string[] { "09" } };

            yield return new object[] { null, @"[\w-[\x00-\x0F]]+", "bbbaaaABYZ09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABYZ09zzzyyy" } };

            yield return new object[] { null, @"[\w-[\s]]+", "0AZaz9", RegexOptions.None, new string[] { "0AZaz9" } };
            yield return new object[] { null, @"[\w-[\W]]+", "0AZaz9", RegexOptions.None, new string[] { "0AZaz9" } };
            yield return new object[] { null, @"[\w-[\p{Po}]]+", "#a09AZz!", RegexOptions.None, new string[] { "a09AZz" } };

            yield return new object[] { null, @"[\d-[\D]]+", "azAZ1024689", RegexOptions.ECMAScript, new string[] { "1024689" } };
            yield return new object[] { null, @"[\d-[a-zA-Z]]+", "azAZ\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" } };
            yield return new object[] { null, @"[\d-[\p{Ll}]]+", "\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" } };

            yield return new object[] { null, @"[a-zA-Z0-9-[\s]]+", " \tazAZ09", RegexOptions.None, new string[] { "azAZ09" } };

            yield return new object[] { null, @"[a-zA-Z0-9-[\W]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABCD09zzzyyy" } };
            yield return new object[] { null, @"[a-zA-Z0-9-[^a-zA-Z0-9]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABCD09zzzyyy" } };

            yield return new object[] { null, @"[\p{Ll}-[A-Z]]+", "AZaz09", RegexOptions.None, new string[] { "az" } };
            yield return new object[] { null, @"[\p{Nd}-[a-z]]+", "az09", RegexOptions.None, new string[] { "09" } };

            yield return new object[] { null, @"[\P{Lu}-[\p{Lu}]]+", "AZazAZ", RegexOptions.None, new string[] { "az" } };
            yield return new object[] { null, @"[\P{Lu}-[A-Z]]+", "AZazAZ", RegexOptions.None, new string[] { "az" } };
            yield return new object[] { null, @"[\P{Nd}-[\p{Nd}]]+", "azAZ09", RegexOptions.None, new string[] { "azAZ" } };
            yield return new object[] { null, @"[\P{Nd}-[2-8]]+", "1234567890azAZ1234567890", RegexOptions.None, new string[] { "azAZ" } };

            // Alternating construct
            yield return new object[] { null, @"([ ]|[\w-[0-9]])+", "09az AZ90", RegexOptions.None, new string[] { "az AZ", "Z" } };
            yield return new object[] { null, @"([0-9-[02468]]|[0-9-[13579]])+", "az1234567890za", RegexOptions.None, new string[] { "1234567890", "0" } };
            yield return new object[] { null, @"([^0-9-[a-zAE-Z]]|[\w-[a-zAF-Z]])+", "azBCDE1234567890BCDEFza", RegexOptions.None, new string[] { "BCDE1234567890BCDE", "E" } };
            yield return new object[] { null, @"([\p{Ll}-[aeiou]]|[^\w-[\s]])+", "aeiobcdxyz!@#aeio", RegexOptions.None, new string[] { "bcdxyz!@#", "#" } };
            yield return new object[] { null, @"(?:hello|hi){1,3}", "hello", RegexOptions.None, new string[] { "hello" } };
            yield return new object[] { null, @"(hello|hi){1,3}", "hellohihey", RegexOptions.None, new string[] { "hellohi", "hi" } };
            yield return new object[] { null, @"(?:hello|hi){1,3}", "hellohihey", RegexOptions.None, new string[] { "hellohi" } };
            yield return new object[] { null, @"(?:hello|hi){2,2}", "hellohihey", RegexOptions.None, new string[] { "hellohi" } };
            yield return new object[] { null, @"(?:hello|hi){2,2}?", "hellohihihello", RegexOptions.None, new string[] { "hellohi" } };
            yield return new object[] { null, @"(?:abc|def|ghi|hij|klm|no){1,4}", "this is a test nonoabcxyz this is only a test", RegexOptions.None, new string[] { "nonoabc" } };
            yield return new object[] { null, @"xyz(abc|def)xyz", "abcxyzdefxyzabc", RegexOptions.None, new string[] { "xyzdefxyz", "def" } };
            yield return new object[] { null, @"abc|(?:def|ghi)", "ghi", RegexOptions.None, new string[] { "ghi" } };
            yield return new object[] { null, @"abc|(def|ghi)", "def", RegexOptions.None, new string[] { "def", "def" } };

            // Multiple character classes using character class subtraction
            yield return new object[] { null, @"98[\d-[9]][\d-[8]][\d-[0]]", "98911 98881 98870 98871", RegexOptions.None, new string[] { "98871" } };
            yield return new object[] { null, @"m[\w-[^aeiou]][\w-[^aeiou]]t", "mbbt mect meet", RegexOptions.None, new string[] { "meet" } };

            // Negation with character class subtraction
            yield return new object[] { null, "[abcdef-[^bce]]+", "adfbcefda", RegexOptions.None, new string[] { "bce" } };
            yield return new object[] { null, "[^cde-[ag]]+", "agbfxyzga", RegexOptions.None, new string[] { "bfxyz" } };

            // Misc The idea here is come up with real world examples of char class subtraction. Things that
            // would be difficult to define without it
            yield return new object[] { null, @"[\p{L}-[^\p{Lu}]]+", "09',.abcxyzABCXYZ", RegexOptions.None, new string[] { "ABCXYZ" } };

            yield return new object[] { null, @"[\p{IsGreek}-[\P{Lu}]]+", "\u0390\u03FE\u0386\u0388\u03EC\u03EE\u0400", RegexOptions.None, new string[] { "\u03FE\u0386\u0388\u03EC\u03EE" } };
            yield return new object[] { null, @"[\p{IsBasicLatin}-[G-L]]+", "GAFMZL", RegexOptions.None, new string[] { "AFMZ" } };

            yield return new object[] { null, "[a-zA-Z-[aeiouAEIOU]]+", "aeiouAEIOUbcdfghjklmnpqrstvwxyz", RegexOptions.None, new string[] { "bcdfghjklmnpqrstvwxyz" } };

            // The following is an overly complex way of matching an ip address using char class subtraction
            yield return new object[] { null, @"^
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
            yield return new object[] { null, @"[abcd\-d-[bc]]+", "bbbaaa---dddccc", RegexOptions.None, new string[] { "aaa---ddd" } };
            yield return new object[] { null, @"[^a-f-[\x00-\x60\u007B-\uFFFF]]+", "aaafffgggzzz{{{", RegexOptions.None, new string[] { "gggzzz" } };
            yield return new object[] { null, @"[\[\]a-f-[[]]+", "gggaaafff]]][[[", RegexOptions.None, new string[] { "aaafff]]]" } };
            yield return new object[] { null, @"[\[\]a-f-[]]]+", "gggaaafff[[[]]]", RegexOptions.None, new string[] { "aaafff[[[" } };

            yield return new object[] { null, @"[ab\-\[cd-[-[]]]]", "a]]", RegexOptions.None, new string[] { "a]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[-[]]]]", "b]]", RegexOptions.None, new string[] { "b]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[-[]]]]", "c]]", RegexOptions.None, new string[] { "c]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[-[]]]]", "d]]", RegexOptions.None, new string[] { "d]]" } };

            yield return new object[] { null, @"[ab\-\[cd-[[]]]]", "a]]", RegexOptions.None, new string[] { "a]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[[]]]]", "b]]", RegexOptions.None, new string[] { "b]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[[]]]]", "c]]", RegexOptions.None, new string[] { "c]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[[]]]]", "d]]", RegexOptions.None, new string[] { "d]]" } };
            yield return new object[] { null, @"[ab\-\[cd-[[]]]]", "-]]", RegexOptions.None, new string[] { "-]]" } };

            yield return new object[] { null, @"[a-[c-e]]+", "bbbaaaccc", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"[a-[c-e]]+", "```aaaccc", RegexOptions.None, new string[] { "aaa" } };

            yield return new object[] { null, @"[a-d\--[bc]]+", "cccaaa--dddbbb", RegexOptions.None, new string[] { "aaa--ddd" } };

            // Not Character class substraction
            yield return new object[] { null, @"[\0- [bc]+", "!!!\0\0\t\t  [[[[bbbcccaaa", RegexOptions.None, new string[] { "\0\0\t\t  [[[[bbbccc" } };
            yield return new object[] { null, "[[abcd]-[bc]]+", "a-b]", RegexOptions.None, new string[] { "a-b]" } };
            yield return new object[] { null, "[-[e-g]+", "ddd[[[---eeefffggghhh", RegexOptions.None, new string[] { "[[[---eeefffggg" } };
            yield return new object[] { null, "[-e-g]+", "ddd---eeefffggghhh", RegexOptions.None, new string[] { "---eeefffggg" } };
            yield return new object[] { null, "[a-e - m-p]+", "---a b c d e m n o p---", RegexOptions.None, new string[] { "a b c d e m n o p" } };
            yield return new object[] { null, "[^-[bc]]", "b] c] -] aaaddd]", RegexOptions.None, new string[] { "d]" } };
            yield return new object[] { null, "[^-[bc]]", "b] c] -] aaa]ddd]", RegexOptions.None, new string[] { "a]" } };

            // Make sure we correctly handle \-
            yield return new object[] { null, @"[a\-[bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" } };
            yield return new object[] { null, @"[a\-[\-\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" } };
            yield return new object[] { null, @"[a\-\[\-\[\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" } };
            yield return new object[] { null, @"[abc\--[b]]+", "[[[```bbbaaa---cccddd", RegexOptions.None, new string[] { "aaa---ccc" } };
            yield return new object[] { null, @"[abc\-z-[b]]+", "```aaaccc---zzzbbb", RegexOptions.None, new string[] { "aaaccc---zzz" } };
            yield return new object[] { null, @"[a-d\-[b]+", "```aaabbbcccddd----[[[[]]]", RegexOptions.None, new string[] { "aaabbbcccddd----[[[[" } };
            yield return new object[] { null, @"[abcd\-d\-[bc]+", "bbbaaa---[[[dddccc", RegexOptions.None, new string[] { "bbbaaa---[[[dddccc" } };

            // Everything works correctly with option RegexOptions.IgnorePatternWhitespace
            yield return new object[] { null, "[a - c - [ b ] ]+", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace, new string[] { " ]]]" } };
            yield return new object[] { null, "[a - c - [ b ] +", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace, new string[] { "aaa   ccc [[[[ bbb " } };

            // Unicode Char Classes
            yield return new object[] { null, @"(\p{Lu}\w*)\s(\p{Lu}\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { null, @"(\p{Lu}\p{Ll}*)\s(\p{Lu}\p{Ll}*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { null, @"(\P{Ll}\p{Ll}*)\s(\P{Ll}\p{Ll}*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { null, @"(\P{Lu}+\p{Lu})\s(\P{Lu}+\p{Lu})", "hellO worlD", RegexOptions.None, new string[] { "hellO worlD", "hellO", "worlD" } };
            yield return new object[] { null, @"(\p{Lt}\w*)\s(\p{Lt}*\w*)", "\u01C5ello \u01C5orld", RegexOptions.None, new string[] { "\u01C5ello \u01C5orld", "\u01C5ello", "\u01C5orld" } };
            yield return new object[] { null, @"(\P{Lt}\w*)\s(\P{Lt}*\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };

            // Character ranges IgnoreCase
            yield return new object[] { null, @"[@-D]+", "eE?@ABCDabcdeE", RegexOptions.IgnoreCase, new string[] { "@ABCDabcd" } };
            yield return new object[] { null, @"[>-D]+", "eE=>?@ABCDabcdeE", RegexOptions.IgnoreCase, new string[] { ">?@ABCDabcd" } };
            yield return new object[] { null, @"[\u0554-\u0557]+", "\u0583\u0553\u0554\u0555\u0556\u0584\u0585\u0586\u0557\u0558", RegexOptions.IgnoreCase, new string[] { "\u0554\u0555\u0556\u0584\u0585\u0586\u0557" } };
            yield return new object[] { null, @"[X-\]]+", "wWXYZxyz[\\]^", RegexOptions.IgnoreCase, new string[] { "XYZxyz[\\]" } };
            yield return new object[] { null, @"[X-\u0533]+", "\u0551\u0554\u0560AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563\u0564", RegexOptions.IgnoreCase, new string[] { "AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563" } };
            yield return new object[] { null, @"[X-a]+", "wWAXYZaxyz", RegexOptions.IgnoreCase, new string[] { "AXYZaxyz" } };
            yield return new object[] { null, @"[X-c]+", "wWABCXYZabcxyz", RegexOptions.IgnoreCase, new string[] { "ABCXYZabcxyz" } };
            yield return new object[] { null, @"[X-\u00C0]+", "\u00C1\u00E1\u00C0\u00E0wWABCXYZabcxyz", RegexOptions.IgnoreCase, new string[] { "\u00C0\u00E0wWABCXYZabcxyz" } };
            yield return new object[] { null, @"[\u0100\u0102\u0104]+", "\u00FF \u0100\u0102\u0104\u0101\u0103\u0105\u0106", RegexOptions.IgnoreCase, new string[] { "\u0100\u0102\u0104\u0101\u0103\u0105" } };
            yield return new object[] { null, @"[B-D\u0130]+", "aAeE\u0129\u0131\u0068 BCDbcD\u0130\u0069\u0070", RegexOptions.IgnoreCase, new string[] { "BCDbcD\u0130\u0069" } };
            yield return new object[] { null, @"[\u013B\u013D\u013F]+", "\u013A\u013B\u013D\u013F\u013C\u013E\u0140\u0141", RegexOptions.IgnoreCase, new string[] { "\u013B\u013D\u013F\u013C\u013E\u0140" } };

            // Escape Chars
            yield return new object[] { null, "(Cat)\r(Dog)", "Cat\rDog", RegexOptions.None, new string[] { "Cat\rDog", "Cat", "Dog" } };
            yield return new object[] { null, "(Cat)\t(Dog)", "Cat\tDog", RegexOptions.None, new string[] { "Cat\tDog", "Cat", "Dog" } };
            yield return new object[] { null, "(Cat)\f(Dog)", "Cat\fDog", RegexOptions.None, new string[] { "Cat\fDog", "Cat", "Dog" } };

            // Miscellaneous { witout matching }
            yield return new object[] { null, @"{5", "hello {5 world", RegexOptions.None, new string[] { "{5" } };
            yield return new object[] { null, @"{5,", "hello {5, world", RegexOptions.None, new string[] { "{5," } };
            yield return new object[] { null, @"{5,6", "hello {5,6 world", RegexOptions.None, new string[] { "{5,6" } };

            // Miscellaneous inline options
            yield return new object[] { null, @"(?n:(?<cat>cat)(\s+)(?<dog>dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "cat", "dog" } };
            yield return new object[] { null, @"(?n:(cat)(\s+)(dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog" } };
            yield return new object[] { null, @"(?n:(cat)(?<SpaceChars>\s+)(dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "   " } };
            yield return new object[] { null, @"(?x:
                            (?<cat>cat) # Cat statement
                            (\s+) # Whitespace chars
                            (?<dog>dog # Dog statement
                            ))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "   ", "cat", "dog" } };
            yield return new object[] { null, @"(?+i:cat)", "CAT", RegexOptions.None, new string[] { "CAT" } };

            // \d, \D, \s, \S, \w, \W, \P, \p inside character range
            yield return new object[] { null, @"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.None, new string[] { "cat230927dog", "230927" } };
            yield return new object[] { null, @"([\D]*)dog", "65498catdog58719", RegexOptions.None, new string[] { "catdog", "cat" } };
            yield return new object[] { null, @"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.None, new string[] { "cat   dog", "   " } };
            yield return new object[] { null, @"cat([\S]*)", "sfdcatdog    3270", RegexOptions.None, new string[] { "catdog", "dog" } };
            yield return new object[] { null, @"cat([\w]*)", "sfdcatdog    3270", RegexOptions.None, new string[] { "catdog", "dog" } };
            yield return new object[] { null, @"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.None, new string[] { "cat   dog", "   " } };
            yield return new object[] { null, @"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { null, @"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" } };

            // \x, \u, \a, \b, \e, \f, \n, \r, \t, \v, \c, inside character range
            yield return new object[] { null, @"(cat)([\x41]*)(dog)", "catAAAdog", RegexOptions.None, new string[] { "catAAAdog", "cat", "AAA", "dog" } };
            yield return new object[] { null, @"(cat)([\u0041]*)(dog)", "catAAAdog", RegexOptions.None, new string[] { "catAAAdog", "cat", "AAA", "dog" } };
            yield return new object[] { null, @"(cat)([\a]*)(dog)", "cat\a\a\adog", RegexOptions.None, new string[] { "cat\a\a\adog", "cat", "\a\a\a", "dog" } };
            yield return new object[] { null, @"(cat)([\b]*)(dog)", "cat\b\b\bdog", RegexOptions.None, new string[] { "cat\b\b\bdog", "cat", "\b\b\b", "dog" } };
            yield return new object[] { null, @"(cat)([\e]*)(dog)", "cat\u001B\u001B\u001Bdog", RegexOptions.None, new string[] { "cat\u001B\u001B\u001Bdog", "cat", "\u001B\u001B\u001B", "dog" } };
            yield return new object[] { null, @"(cat)([\f]*)(dog)", "cat\f\f\fdog", RegexOptions.None, new string[] { "cat\f\f\fdog", "cat", "\f\f\f", "dog" } };
            yield return new object[] { null, @"(cat)([\r]*)(dog)", "cat\r\r\rdog", RegexOptions.None, new string[] { "cat\r\r\rdog", "cat", "\r\r\r", "dog" } };
            yield return new object[] { null, @"(cat)([\v]*)(dog)", "cat\v\v\vdog", RegexOptions.None, new string[] { "cat\v\v\vdog", "cat", "\v\v\v", "dog" } };

            // \d, \D, \s, \S, \w, \W, \P, \p inside character range ([0-5]) with ECMA Option
            yield return new object[] { null, @"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript, new string[] { "cat230927dog", "230927" } };
            yield return new object[] { null, @"([\D]*)dog", "65498catdog58719", RegexOptions.ECMAScript, new string[] { "catdog", "cat" } };
            yield return new object[] { null, @"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "   " } };
            yield return new object[] { null, @"cat([\S]*)", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "dog" } };
            yield return new object[] { null, @"cat([\w]*)", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "dog" } };
            yield return new object[] { null, @"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "   " } };
            yield return new object[] { null, @"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "Hello", "World" } };
            yield return new object[] { null, @"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "Hello", "World" } };

            // \d, \D, \s, \S, \w, \W, \P, \p outside character range ([0-5]) with ECMA Option
            yield return new object[] { null, @"(cat)\d*dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript, new string[] { "cat230927dog", "cat" } };
            yield return new object[] { null, @"\D*(dog)", "65498catdog58719", RegexOptions.ECMAScript, new string[] { "catdog", "dog" } };
            yield return new object[] { null, @"(cat)\s*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\S*", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "cat" } };
            yield return new object[] { null, @"(cat)\w*", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "cat" } };
            yield return new object[] { null, @"(cat)\W*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "cat", "dog" } };
            yield return new object[] { null, @"\p{Lu}(\w*)\s\p{Lu}(\w*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "ello", "orld" } };
            yield return new object[] { null, @"\P{Ll}\p{Ll}*\s\P{Ll}\p{Ll}*", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World" } };

            // Use < in a group
            yield return new object[] { null, @"cat(?<dog121>dog)", "catcatdogdogcat", RegexOptions.None, new string[] { "catdog", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s*(?<cat>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" } };
            yield return new object[] { null, @"(?<1>cat)\s*(?<1>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" } };
            yield return new object[] { null, @"(?<2048>cat)\s*(?<2048>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\w+(?<dog-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };
            yield return new object[] { null, @"(?<cat>cat)\w+(?<-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "" } };
            yield return new object[] { null, @"(?<cat>cat)\w+(?<cat-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "_Hello_World_" } };
            yield return new object[] { null, @"(?<1>cat)\w+(?<dog-1>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };
            yield return new object[] { null, @"(?<cat>cat)\w+(?<2-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };
            yield return new object[] { null, @"(?<1>cat)\w+(?<2-1>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" } };

            // Quantifiers
            yield return new object[] { null, @"(?<cat>cat){", "STARTcat{", RegexOptions.None, new string[] { "cat{", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){fdsa", "STARTcat{fdsa", RegexOptions.None, new string[] { "cat{fdsa", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){1", "STARTcat{1", RegexOptions.None, new string[] { "cat{1", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){1END", "STARTcat{1END", RegexOptions.None, new string[] { "cat{1END", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){1,", "STARTcat{1,", RegexOptions.None, new string[] { "cat{1,", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){1,END", "STARTcat{1,END", RegexOptions.None, new string[] { "cat{1,END", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){1,2", "STARTcat{1,2", RegexOptions.None, new string[] { "cat{1,2", "cat" } };
            yield return new object[] { null, @"(?<cat>cat){1,2END", "STARTcat{1,2END", RegexOptions.None, new string[] { "cat{1,2END", "cat" } };

            // Use IgnorePatternWhitespace
            yield return new object[] { null, @"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog
                            ", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace) (dog)  (?#followed by dog)", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" } };

            // Back Reference
            yield return new object[] { null, @"(?<cat>cat)(?<dog>dog)\k<cat>", "asdfcatdogcatdog", RegexOptions.None, new string[] { "catdogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\k<cat>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\k'cat'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\<cat>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\'cat'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };

            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\k<1>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\k'1'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\<1>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\'1'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.ECMAScript, new string[] { "cat   dogcat", "cat", "dog" } };

            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\k<dog>", "asdfcat   dogdog   dog", RegexOptions.None, new string[] { "cat   dogdog", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.None, new string[] { "cat   dogdog", "cat", "dog" } };
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.ECMAScript, new string[] { "cat   dogdog", "cat", "dog" } };

            // Octal
            yield return new object[] { null, @"(cat)(\077)", "hellocat?dogworld", RegexOptions.None, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { null, @"(cat)(\77)", "hellocat?dogworld", RegexOptions.None, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { null, @"(cat)(\176)", "hellocat~dogworld", RegexOptions.None, new string[] { "cat~", "cat", "~" } };
            yield return new object[] { null, @"(cat)(\400)", "hellocat\0dogworld", RegexOptions.None, new string[] { "cat\0", "cat", "\0" } };
            yield return new object[] { null, @"(cat)(\300)", "hellocat\u00C0dogworld", RegexOptions.None, new string[] { "cat\u00C0", "cat", "\u00C0" } };
            yield return new object[] { null, @"(cat)(\477)", "hellocat\u003Fdogworld", RegexOptions.None, new string[] { "cat\u003F", "cat", "\u003F" } };
            yield return new object[] { null, @"(cat)(\777)", "hellocat\u00FFdogworld", RegexOptions.None, new string[] { "cat\u00FF", "cat", "\u00FF" } };
            yield return new object[] { null, @"(cat)(\7770)", "hellocat\u00FF0dogworld", RegexOptions.None, new string[] { "cat\u00FF0", "cat", "\u00FF0" } };

            yield return new object[] { null, @"(cat)(\077)", "hellocat?dogworld", RegexOptions.ECMAScript, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { null, @"(cat)(\77)", "hellocat?dogworld", RegexOptions.ECMAScript, new string[] { "cat?", "cat", "?" } };
            yield return new object[] { null, @"(cat)(\7)", "hellocat\adogworld", RegexOptions.ECMAScript, new string[] { "cat\a", "cat", "\a" } };
            yield return new object[] { null, @"(cat)(\40)", "hellocat dogworld", RegexOptions.ECMAScript, new string[] { "cat ", "cat", " " } };
            yield return new object[] { null, @"(cat)(\040)", "hellocat dogworld", RegexOptions.ECMAScript, new string[] { "cat ", "cat", " " } };
            yield return new object[] { null, @"(cat)(\176)", "hellocatcat76dogworld", RegexOptions.ECMAScript, new string[] { "catcat76", "cat", "cat76" } };
            yield return new object[] { null, @"(cat)(\377)", "hellocat\u00FFdogworld", RegexOptions.ECMAScript, new string[] { "cat\u00FF", "cat", "\u00FF" } };
            yield return new object[] { null, @"(cat)(\400)", "hellocat 0Fdogworld", RegexOptions.ECMAScript, new string[] { "cat 0", "cat", " 0" } };

            // Decimal
            yield return new object[] { null, @"(cat)\s+(?<2147483646>dog)", "asdlkcat  dogiwod", RegexOptions.None, new string[] { "cat  dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(?<2147483647>dog)", "asdlkcat  dogiwod", RegexOptions.None, new string[] { "cat  dog", "cat", "dog" } };

            // Hex
            yield return new object[] { null, @"(cat)(\x2a*)(dog)", "asdlkcat***dogiwod", RegexOptions.None, new string[] { "cat***dog", "cat", "***", "dog" } };
            yield return new object[] { null, @"(cat)(\x2b*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None, new string[] { "cat+++dog", "cat", "+++", "dog" } };
            yield return new object[] { null, @"(cat)(\x2c*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None, new string[] { "cat,,,dog", "cat", ",,,", "dog" } };
            yield return new object[] { null, @"(cat)(\x2d*)(dog)", "asdlkcat---dogiwod", RegexOptions.None, new string[] { "cat---dog", "cat", "---", "dog" } };
            yield return new object[] { null, @"(cat)(\x2e*)(dog)", "asdlkcat...dogiwod", RegexOptions.None, new string[] { "cat...dog", "cat", "...", "dog" } };
            yield return new object[] { null, @"(cat)(\x2f*)(dog)", "asdlkcat///dogiwod", RegexOptions.None, new string[] { "cat///dog", "cat", "///", "dog" } };

            yield return new object[] { null, @"(cat)(\x2A*)(dog)", "asdlkcat***dogiwod", RegexOptions.None, new string[] { "cat***dog", "cat", "***", "dog" } };
            yield return new object[] { null, @"(cat)(\x2B*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None, new string[] { "cat+++dog", "cat", "+++", "dog" } };
            yield return new object[] { null, @"(cat)(\x2C*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None, new string[] { "cat,,,dog", "cat", ",,,", "dog" } };
            yield return new object[] { null, @"(cat)(\x2D*)(dog)", "asdlkcat---dogiwod", RegexOptions.None, new string[] { "cat---dog", "cat", "---", "dog" } };
            yield return new object[] { null, @"(cat)(\x2E*)(dog)", "asdlkcat...dogiwod", RegexOptions.None, new string[] { "cat...dog", "cat", "...", "dog" } };
            yield return new object[] { null, @"(cat)(\x2F*)(dog)", "asdlkcat///dogiwod", RegexOptions.None, new string[] { "cat///dog", "cat", "///", "dog" } };

            // ScanControl
            yield return new object[] { null, @"(cat)(\c@*)(dog)", "asdlkcat\0\0dogiwod", RegexOptions.None, new string[] { "cat\0\0dog", "cat", "\0\0", "dog" } };
            yield return new object[] { null, @"(cat)(\cA*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None, new string[] { "cat\u0001dog", "cat", "\u0001", "dog" } };
            yield return new object[] { null, @"(cat)(\ca*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None, new string[] { "cat\u0001dog", "cat", "\u0001", "dog" } };

            yield return new object[] { null, @"(cat)(\cC*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None, new string[] { "cat\u0003dog", "cat", "\u0003", "dog" } };
            yield return new object[] { null, @"(cat)(\cc*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None, new string[] { "cat\u0003dog", "cat", "\u0003", "dog" } };

            yield return new object[] { null, @"(cat)(\cD*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None, new string[] { "cat\u0004dog", "cat", "\u0004", "dog" } };
            yield return new object[] { null, @"(cat)(\cd*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None, new string[] { "cat\u0004dog", "cat", "\u0004", "dog" } };

            yield return new object[] { null, @"(cat)(\cX*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None, new string[] { "cat\u0018dog", "cat", "\u0018", "dog" } };
            yield return new object[] { null, @"(cat)(\cx*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None, new string[] { "cat\u0018dog", "cat", "\u0018", "dog" } };

            yield return new object[] { null, @"(cat)(\cZ*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None, new string[] { "cat\u001adog", "cat", "\u001a", "dog" } };
            yield return new object[] { null, @"(cat)(\cz*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None, new string[] { "cat\u001adog", "cat", "\u001a", "dog" } };

            if (!PlatformDetection.IsNetFramework) // missing fix for https://github.com/dotnet/runtime/issues/24759
            {
                yield return new object[] { null, @"(cat)(\c[*)(dog)", "asdlkcat\u001bdogiwod", RegexOptions.None, new string[] { "cat\u001bdog", "cat", "\u001b", "dog" } };
            }

            // Atomic Zero-Width Assertions \A \G ^ \Z \z \b \B
            //\A
            yield return new object[] { null, @"\Acat\s+dog", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"\Acat\s+dog", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\G
            yield return new object[] { null, @"\Gcat\s+dog", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"\Gcat\s+dog", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"\Gcat\s+dog", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"\G(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"\G(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"\G(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //^
            yield return new object[] { null, @"^cat\s+dog", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"^cat\s+dog", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"mouse\s\n^cat\s+dog", "mouse\n\ncat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "mouse\n\ncat   \n\n\n   dog" } };
            yield return new object[] { null, @"^cat\s+dog", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"^(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"^(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(mouse)\s\n^(cat)\s+(dog)", "mouse\n\ncat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "mouse\n\ncat   \n\n\n   dog", "mouse", "cat", "dog" } };
            yield return new object[] { null, @"^(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\Z
            yield return new object[] { null, @"cat\s+dog\Z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\Z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\Z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\Z", "cat   \n\n\n   dog\n", RegexOptions.None, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\Z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\Z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\z
            yield return new object[] { null, @"cat\s+dog\z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"cat\s+dog\z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };
            yield return new object[] { null, @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" } };

            //\b
            yield return new object[] { null, @"\bcat\b", "cat", RegexOptions.None, new string[] { "cat" } };
            yield return new object[] { null, @"\bcat\b", "dog cat mouse", RegexOptions.None, new string[] { "cat" } };
            yield return new object[] { null, @"\bcat\b", "cat", RegexOptions.ECMAScript, new string[] { "cat" } };
            yield return new object[] { null, @"\bcat\b", "dog cat mouse", RegexOptions.ECMAScript, new string[] { "cat" } };
            yield return new object[] { null, @".*\bcat\b", "cat", RegexOptions.None, new string[] { "cat" } };
            yield return new object[] { null, @".*\bcat\b", "dog cat mouse", RegexOptions.None, new string[] { "dog cat" } };
            yield return new object[] { null, @".*\bcat\b", "cat", RegexOptions.ECMAScript, new string[] { "cat" } };
            yield return new object[] { null, @".*\bcat\b", "dog cat mouse", RegexOptions.ECMAScript, new string[] { "dog cat" } };
            yield return new object[] { null, @"\b@cat", "123START123@catEND", RegexOptions.None, new string[] { "@cat" } };
            yield return new object[] { null, @"\b\<cat", "123START123<catEND", RegexOptions.None, new string[] { "<cat" } };
            yield return new object[] { null, @"\b,cat", "satwe,,,START,catEND", RegexOptions.None, new string[] { ",cat" } };
            yield return new object[] { null, @"\b\[cat", "`12START123[catEND", RegexOptions.None, new string[] { "[cat" } };

            //\B
            yield return new object[] { null, @"\Bcat\B", "dogcatmouse", RegexOptions.None, new string[] { "cat" } };
            yield return new object[] { null, @"dog\Bcat\B", "dogcatmouse", RegexOptions.None, new string[] { "dogcat" } };
            yield return new object[] { null, @".*\Bcat\B", "dogcatmouse", RegexOptions.None, new string[] { "dogcat" } };
            yield return new object[] { null, @"\Bcat\B", "dogcatmouse", RegexOptions.ECMAScript, new string[] { "cat" } };
            yield return new object[] { null, @"dog\Bcat\B", "dogcatmouse", RegexOptions.ECMAScript, new string[] { "dogcat" } };
            yield return new object[] { null, @".*\Bcat\B", "dogcatmouse", RegexOptions.ECMAScript, new string[] { "dogcat" } };
            yield return new object[] { null, @"\B@cat", "123START123;@catEND", RegexOptions.None, new string[] { "@cat" } };
            yield return new object[] { null, @"\B\<cat", "123START123'<catEND", RegexOptions.None, new string[] { "<cat" } };
            yield return new object[] { null, @"\B,cat", "satwe,,,START',catEND", RegexOptions.None, new string[] { ",cat" } };
            yield return new object[] { null, @"\B\[cat", "`12START123'[catEND", RegexOptions.None, new string[] { "[cat" } };

            // \w matching \p{Lm} (Letter, Modifier)
            yield return new object[] { null, @"\w+\s+\w+", "cat\u02b0 dog\u02b1", RegexOptions.None, new string[] { "cat\u02b0 dog\u02b1" } };
            yield return new object[] { null, @"cat\w+\s+dog\w+", "STARTcat\u30FC dog\u3005END", RegexOptions.None, new string[] { "cat\u30FC dog\u3005END" } };
            yield return new object[] { null, @"cat\w+\s+dog\w+", "STARTcat\uff9e dog\uff9fEND", RegexOptions.None, new string[] { "cat\uff9e dog\uff9fEND" } };
            yield return new object[] { null, @"(\w+)\s+(\w+)", "cat\u02b0 dog\u02b1", RegexOptions.None, new string[] { "cat\u02b0 dog\u02b1", "cat\u02b0", "dog\u02b1" } };
            yield return new object[] { null, @"(cat\w+)\s+(dog\w+)", "STARTcat\u30FC dog\u3005END", RegexOptions.None, new string[] { "cat\u30FC dog\u3005END", "cat\u30FC", "dog\u3005END" } };
            yield return new object[] { null, @"(cat\w+)\s+(dog\w+)", "STARTcat\uff9e dog\uff9fEND", RegexOptions.None, new string[] { "cat\uff9e dog\uff9fEND", "cat\uff9e", "dog\uff9fEND" } };

            // Positive and negative character classes [a-c]|[^b-c]
            yield return new object[] { null, @"[^a]|d", "d", RegexOptions.None, new string[] { "d" } };
            yield return new object[] { null, @"([^a]|[d])*", "Hello Worlddf", RegexOptions.None, new string[] { "Hello Worlddf", "f" } };
            yield return new object[] { null, @"([^{}]|\n)+", "{{{{Hello\n World \n}END", RegexOptions.None, new string[] { "Hello\n World \n", "\n" } };
            yield return new object[] { null, @"([a-d]|[^abcd])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" } };
            yield return new object[] { null, @"([^a]|[a])*", "once upon a time", RegexOptions.None, new string[] { "once upon a time", "e" } };
            yield return new object[] { null, @"([a-d]|[^abcd]|[x-z]|^wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" } };
            yield return new object[] { null, @"([a-d]|[e-i]|[^e]|wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" } };

            // Canonical and noncanonical char class, where one group is in it's
            // simplest form [a-e] and another is more complex.
            yield return new object[] { null, @"^(([^b]+ )|(.* ))$", "aaa ", RegexOptions.None, new string[] { "aaa ", "aaa ", "aaa ", "" } };
            yield return new object[] { null, @"^(([^b]+ )|(.*))$", "aaa", RegexOptions.None, new string[] { "aaa", "aaa", "", "aaa" } };
            yield return new object[] { null, @"^(([^b]+ )|(.* ))$", "bbb ", RegexOptions.None, new string[] { "bbb ", "bbb ", "", "bbb " } };
            yield return new object[] { null, @"^(([^b]+ )|(.*))$", "bbb", RegexOptions.None, new string[] { "bbb", "bbb", "", "bbb" } };
            yield return new object[] { null, @"^((a*)|(.*))$", "aaa", RegexOptions.None, new string[] { "aaa", "aaa", "aaa", "" } };
            yield return new object[] { null, @"^((a*)|(.*))$", "aaabbb", RegexOptions.None, new string[] { "aaabbb", "aaabbb", "", "aaabbb" } };

            yield return new object[] { null, @"(([0-9])|([a-z])|([A-Z]))*", "{hello 1234567890 world}", RegexOptions.None, new string[] { "", "", "", "", "" } };
            yield return new object[] { null, @"(([0-9])|([a-z])|([A-Z]))+", "{hello 1234567890 world}", RegexOptions.None, new string[] { "hello", "o", "", "o", "" } };
            yield return new object[] { null, @"(([0-9])|([a-z])|([A-Z]))*", "{HELLO 1234567890 world}", RegexOptions.None, new string[] { "", "", "", "", "" } };
            yield return new object[] { null, @"(([0-9])|([a-z])|([A-Z]))+", "{HELLO 1234567890 world}", RegexOptions.None, new string[] { "HELLO", "O", "", "", "O" } };
            yield return new object[] { null, @"(([0-9])|([a-z])|([A-Z]))*", "{1234567890 hello  world}", RegexOptions.None, new string[] { "", "", "", "", "" } };
            yield return new object[] { null, @"(([0-9])|([a-z])|([A-Z]))+", "{1234567890 hello world}", RegexOptions.None, new string[] { "1234567890", "0", "0", "", "" } };

            yield return new object[] { null, @"^(([a-d]*)|([a-z]*))$", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccdddeeefff", "aaabbbcccdddeeefff", "", "aaabbbcccdddeeefff" } };
            yield return new object[] { null, @"^(([d-f]*)|([c-e]*))$", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "", "dddeeeccceee" } };
            yield return new object[] { null, @"^(([c-e]*)|([d-f]*))$", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" } };

            yield return new object[] { null, @"(([a-d]*)|([a-z]*))", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", "" } };
            yield return new object[] { null, @"(([d-f]*)|([c-e]*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeee", "dddeee", "dddeee", "" } };
            yield return new object[] { null, @"(([c-e]*)|([d-f]*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" } };

            yield return new object[] { null, @"(([a-d]*)|(.*))", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", "" } };
            yield return new object[] { null, @"(([d-f]*)|(.*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeee", "dddeee", "dddeee", "" } };
            yield return new object[] { null, @"(([c-e]*)|(.*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" } };

            // \p{Pi} (Punctuation Initial quote) \p{Pf} (Punctuation Final quote)
            yield return new object[] { null, @"\p{Pi}(\w*)\p{Pf}", "\u00ABCat\u00BB   \u00BBDog\u00AB'", RegexOptions.None, new string[] { "\u00ABCat\u00BB", "Cat" } };
            yield return new object[] { null, @"\p{Pi}(\w*)\p{Pf}", "\u2018Cat\u2019   \u2019Dog\u2018'", RegexOptions.None, new string[] { "\u2018Cat\u2019", "Cat" } };

            // ECMAScript
            yield return new object[] { null, @"(?<cat>cat)\s+(?<dog>dog)\s+\123\s+\234", "asdfcat   dog     cat23    dog34eia", RegexOptions.ECMAScript, new string[] { "cat   dog     cat23    dog34", "cat", "dog" } };

            // Balanced Matching
            yield return new object[] { null, @"<div>
            (?>
                <div>(?<DEPTH>) |
                </div> (?<-DEPTH>) |
                .?
            )*?
            (?(DEPTH)(?!))
            </div>", "<div>this is some <div>red</div> text</div></div></div>", RegexOptions.IgnorePatternWhitespace, new string[] { "<div>this is some <div>red</div> text</div>", "" } };

            yield return new object[] { null, @"(
            ((?'open'<+)[^<>]*)+
            ((?'close-open'>+)[^<>]*)+
            )+", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "<02deep_03<03deep_03>>>", "<03deep_03", ">>>", "<", "03deep_03" } };

            yield return new object[] { null, @"(
            (?<start><)?
            [^<>]?
            (?<end-start>>)?
            )*", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "", "", "01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>" } };

            yield return new object[] { null, @"(
            (?<start><[^/<>]*>)?
            [^<>]?
            (?<end-start></[^/<>]*>)?
            )*", "<b><a>Cat</a></b>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b><a>Cat</a></b>", "", "", "<a>Cat</a>" } };

            yield return new object[] { null, @"(
            (?<start><(?<TagName>[^/<>]*)>)?
            [^<>]?
            (?<end-start></\k<TagName>>)?
            )*", "<b>cat</b><a>dog</a>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b>cat</b><a>dog</a>", "", "", "a", "dog" } };

            // Balanced Matching With Backtracking
            yield return new object[] { null, @"(
            (?<start><[^/<>]*>)?
            .?
            (?<end-start></[^/<>]*>)?
            )*
            (?(start)(?!)) ", "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", "", "", "<a>Cat" } };

            // Character Classes and Lazy quantifier
            yield return new object[] { null, @"([0-9]+?)([\w]+?)", "55488aheiaheiad", RegexOptions.ECMAScript, new string[] { "55", "5", "5" } };
            yield return new object[] { null, @"([0-9]+?)([a-z]+?)", "55488aheiaheiad", RegexOptions.ECMAScript, new string[] { "55488a", "55488", "a" } };

            // Miscellaneous/Regression scenarios
            yield return new object[] { null, @"(?<openingtag>1)(?<content>.*?)(?=2)", "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine + "2", RegexOptions.Singleline | RegexOptions.ExplicitCapture,
            new string[] { "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine, "1", Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>"+ Environment.NewLine } };

            yield return new object[] { null, @"\G<%#(?<code>.*?)?%>", @"<%# DataBinder.Eval(this, ""MyNumber"") %>", RegexOptions.Singleline, new string[] { @"<%# DataBinder.Eval(this, ""MyNumber"") %>", @" DataBinder.Eval(this, ""MyNumber"") " } };

            // Nested Quantifiers
            yield return new object[] { null, @"^[abcd]{0,0x10}*$", "a{0,0x10}}}", RegexOptions.None, new string[] { "a{0,0x10}}}" } };

            // Lazy operator Backtracking
            yield return new object[] { null, @"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/", "http://www.msn.com/", RegexOptions.IgnoreCase, new string[] { "http://www.msn.com/", "com", string.Empty } };
            yield return new object[] { null, @"http://([a-zA-Z0-9\-]*\.?)*?/", @"http://www.google.com/", RegexOptions.IgnoreCase, new string[] { "http://www.google.com/", "com" } };

            yield return new object[] { null, @"([a-z]*?)([\w])", "cat", RegexOptions.IgnoreCase, new string[] { "c", string.Empty, "c" } };
            yield return new object[] { null, @"^([a-z]*?)([\w])$", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" } };

            // Backtracking
            yield return new object[] { null, @"([a-z]*)([\w])", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" } };
            yield return new object[] { null, @"^([a-z]*)([\w])$", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" } };

            // Backtracking with multiple (.*) groups -- important ASP.NET scenario
            yield return new object[] { null, @"(.*)/(.*).aspx", "/.aspx", RegexOptions.None, new string[] { "/.aspx", string.Empty, string.Empty } };
            yield return new object[] { null, @"(.*)/(.*).aspx", "/homepage.aspx", RegexOptions.None, new string[] { "/homepage.aspx", string.Empty, "homepage" } };
            yield return new object[] { null, @"(.*)/(.*).aspx", "pages/.aspx", RegexOptions.None, new string[] { "pages/.aspx", "pages", string.Empty } };
            yield return new object[] { null, @"(.*)/(.*).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" } };
            yield return new object[] { null, @"(.*)/(.*).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" } };
            yield return new object[] { null, @"(.*)/(.*).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" } };
            yield return new object[] { null, @"(.*)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" } };
            yield return new object[] { null, @"(.*)/(.*)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" } };

            // Backtracking with multiple (.+) groups
            yield return new object[] { null, @"(.+)/(.+).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" } };
            yield return new object[] { null, @"(.+)/(.+).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" } };
            yield return new object[] { null, @"(.+)/(.+).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" } };
            yield return new object[] { null, @"(.+)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" } };
            yield return new object[] { null, @"(.+)/(.+)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" } };

            // Backtracking with (.+) group followed by (.*)
            yield return new object[] { null, @"(.+)/(.*).aspx", "pages/.aspx", RegexOptions.None, new string[] { "pages/.aspx", "pages", string.Empty } };
            yield return new object[] { null, @"(.+)/(.*).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" } };
            yield return new object[] { null, @"(.+)/(.*).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" } };
            yield return new object[] { null, @"(.+)/(.*).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" } };
            yield return new object[] { null, @"(.+)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" } };
            yield return new object[] { null, @"(.+)/(.*)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" } };

            // Backtracking with (.*) group followed by (.+)
            yield return new object[] { null, @"(.*)/(.+).aspx", "/homepage.aspx", RegexOptions.None, new string[] { "/homepage.aspx", string.Empty, "homepage" } };
            yield return new object[] { null, @"(.*)/(.+).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" } };
            yield return new object[] { null, @"(.*)/(.+).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" } };
            yield return new object[] { null, @"(.*)/(.+).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" } };
            yield return new object[] { null, @"(.*)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" } };
            yield return new object[] { null, @"(.*)/(.+)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" } };

            // Quantifiers
            yield return new object[] { null, @"a*", "", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"a*", "a", RegexOptions.None, new string[] { "a" } };
            yield return new object[] { null, @"a*", "aa", RegexOptions.None, new string[] { "aa" } };
            yield return new object[] { null, @"a*", "aaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"a*?", "", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"a*?", "a", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"a*?", "aa", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"a+?", "aa", RegexOptions.None, new string[] { "a" } };
            yield return new object[] { null, @"a{1,", "a{1,", RegexOptions.None, new string[] { "a{1," } };
            yield return new object[] { null, @"a{1,3}", "aaaaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"a{1,3}?", "aaaaa", RegexOptions.None, new string[] { "a" } };
            yield return new object[] { null, @"a{2,2}", "aaaaa", RegexOptions.None, new string[] { "aa" } };
            yield return new object[] { null, @"a{2,2}?", "aaaaa", RegexOptions.None, new string[] { "aa" } };
            yield return new object[] { null, @".{1,3}", "bb\nba", RegexOptions.None, new string[] { "bb" } };
            yield return new object[] { null, @".{1,3}?", "bb\nba", RegexOptions.None, new string[] { "b" } };
            yield return new object[] { null, @".{2,2}", "bbb\nba", RegexOptions.None, new string[] { "bb" } };
            yield return new object[] { null, @".{2,2}?", "bbb\nba", RegexOptions.None, new string[] { "bb" } };
            yield return new object[] { null, @"[abc]{1,3}", "ccaba", RegexOptions.None, new string[] { "cca" } };
            yield return new object[] { null, @"[abc]{1,3}?", "ccaba", RegexOptions.None, new string[] { "c" } };
            yield return new object[] { null, @"[abc]{2,2}", "ccaba", RegexOptions.None, new string[] { "cc" } };
            yield return new object[] { null, @"[abc]{2,2}?", "ccaba", RegexOptions.None, new string[] { "cc" } };
            yield return new object[] { null, @"(?:[abc]def){1,3}xyz", "cdefxyz", RegexOptions.None, new string[] { "cdefxyz" } };
            yield return new object[] { null, @"(?:[abc]def){1,3}xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "adefbdefcdefxyz" } };
            yield return new object[] { null, @"(?:[abc]def){1,3}?xyz", "cdefxyz", RegexOptions.None, new string[] { "cdefxyz" } };
            yield return new object[] { null, @"(?:[abc]def){1,3}?xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "adefbdefcdefxyz" } };
            yield return new object[] { null, @"(?:[abc]def){2,2}xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "bdefcdefxyz" } };
            yield return new object[] { null, @"(?:[abc]def){2,2}?xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "bdefcdefxyz" } };
            foreach (string prefix in new[] { "", "xyz" })
            {
                yield return new object[] { null, prefix + @"(?:[abc]def){1,3}", prefix + "cdef", RegexOptions.None, new string[] { prefix + "cdef" } };
                yield return new object[] { null, prefix + @"(?:[abc]def){1,3}", prefix + "cdefadefbdef", RegexOptions.None, new string[] { prefix + "cdefadefbdef" } };
                yield return new object[] { null, prefix + @"(?:[abc]def){1,3}", prefix + "cdefadefbdefadef", RegexOptions.None, new string[] { prefix + "cdefadefbdef" } };
                yield return new object[] { null, prefix + @"(?:[abc]def){1,3}?", prefix + "cdef", RegexOptions.None, new string[] { prefix + "cdef" } };
                yield return new object[] { null, prefix + @"(?:[abc]def){1,3}?", prefix + "cdefadefbdef", RegexOptions.None, new string[] { prefix + "cdef" } };
                yield return new object[] { null, prefix + @"(?:[abc]def){2,2}", prefix + "cdefadefbdefadef", RegexOptions.None, new string[] { prefix + "cdefadef" } };
                yield return new object[] { null, prefix + @"(?:[abc]def){2,2}?", prefix + "cdefadefbdefadef", RegexOptions.None, new string[] { prefix + "cdefadef" } };
            }
            yield return new object[] { null, @"(cat){", "cat{", RegexOptions.None, new string[] { "cat{", "cat" } };
            yield return new object[] { null, @"(cat){}", "cat{}", RegexOptions.None, new string[] { "cat{}", "cat" } };
            yield return new object[] { null, @"(cat){,", "cat{,", RegexOptions.None, new string[] { "cat{,", "cat" } };
            yield return new object[] { null, @"(cat){,}", "cat{,}", RegexOptions.None, new string[] { "cat{,}", "cat" } };
            yield return new object[] { null, @"(cat){cat}", "cat{cat}", RegexOptions.None, new string[] { "cat{cat}", "cat" } };
            yield return new object[] { null, @"(cat){cat,5}", "cat{cat,5}", RegexOptions.None, new string[] { "cat{cat,5}", "cat" } };
            yield return new object[] { null, @"(cat){5,dog}", "cat{5,dog}", RegexOptions.None, new string[] { "cat{5,dog}", "cat" } };
            yield return new object[] { null, @"(cat){cat,dog}", "cat{cat,dog}", RegexOptions.None, new string[] { "cat{cat,dog}", "cat" } };
            yield return new object[] { null, @"(cat){,}?", "cat{,}?", RegexOptions.None, new string[] { "cat{,}", "cat" } };
            yield return new object[] { null, @"(cat){cat}?", "cat{cat}?", RegexOptions.None, new string[] { "cat{cat}", "cat" } };
            yield return new object[] { null, @"(cat){cat,5}?", "cat{cat,5}?", RegexOptions.None, new string[] { "cat{cat,5}", "cat" } };
            yield return new object[] { null, @"(cat){5,dog}?", "cat{5,dog}?", RegexOptions.None, new string[] { "cat{5,dog}", "cat" } };
            yield return new object[] { null, @"(cat){cat,dog}?", "cat{cat,dog}?", RegexOptions.None, new string[] { "cat{cat,dog}", "cat" } };

            // Atomic subexpressions
            // Implicitly upgrading (or not) oneloop to be atomic
            yield return new object[] { null, @"a*b", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*b+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*b+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*(?>b+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*[^a]", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*[^a]+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*[^a]+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*(?>[^a]+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*bcd", "aaabcd", RegexOptions.None, new string[] { "aaabcd" } };
            yield return new object[] { null, @"a*[bcd]", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*[bcd]+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*[bcd]+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*(?>[bcd]+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*[bcd]{1,3}", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"a*([bcd]ab|[bef]cd){1,3}", "aaababecdcac", RegexOptions.ExplicitCapture, new string[] { "aaababecd" } };
            yield return new object[] { null, @"a*([bcd]|[aef]){1,3}", "befb", RegexOptions.ExplicitCapture, new string[] { "bef" } }; // can't upgrade
            yield return new object[] { null, @"a*$", "aaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"a*$", "aaa", RegexOptions.Multiline, new string[] { "aaa" } };
            yield return new object[] { null, @"a*\b", "aaa bbb", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"a*\b", "aaa bbb", RegexOptions.ECMAScript, new string[] { "aaa" } };
            yield return new object[] { null, @"@*\B", "@@@", RegexOptions.None, new string[] { "@@@" } };
            yield return new object[] { null, @"@*\B", "@@@", RegexOptions.ECMAScript, new string[] { "@@@" } };
            yield return new object[] { null, @"(?:abcd*|efgh)i", "efghi", RegexOptions.None, new string[] { "efghi" } };
            yield return new object[] { null, @"(?:abcd|efgh*)i", "efgi", RegexOptions.None, new string[] { "efgi" } };
            yield return new object[] { null, @"(?:abcd|efghj{2,}|j[klm]o+)i", "efghjjjjji", RegexOptions.None, new string[] { "efghjjjjji" } };
            yield return new object[] { null, @"(?:abcd|efghi{2,}|j[klm]o+)i", "efghiii", RegexOptions.None, new string[] { "efghiii" } };
            yield return new object[] { null, @"(?:abcd|efghi{2,}|j[klm]o+)i", "efghiiiiiiii", RegexOptions.None, new string[] { "efghiiiiiiii" } };
            yield return new object[] { null, @"a?ba?ba?ba?b", "abbabab", RegexOptions.None, new string[] { "abbabab" } };
            yield return new object[] { null, @"a?ba?ba?ba?b", "abBAbab", RegexOptions.IgnoreCase, new string[] { "abBAbab" } };
            // Implicitly upgrading (or not) notoneloop to be atomic
            yield return new object[] { null, @"[^b]*b", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[^b]*b+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[^b]*b+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[^b]*(?>b+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[^b]*bac", "aaabac", RegexOptions.None, new string[] { "aaabac" } };
            yield return new object[] { null, @"[^b]*", "aaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"(?:abc[^b]*|efgh)i", "efghi", RegexOptions.None, new string[] { "efghi" } }; // can't upgrade
            yield return new object[] { null, @"(?:abcd|efg[^b]*)b", "efgb", RegexOptions.None, new string[] { "efgb" } };
            yield return new object[] { null, @"(?:abcd|efg[^b]*)i", "efgi", RegexOptions.None, new string[] { "efgi" } }; // can't upgrade
            yield return new object[] { null, @"[^a]?a[^a]?a[^a]?a[^a]?a", "baababa", RegexOptions.None, new string[] { "baababa" } };
            yield return new object[] { null, @"[^a]?a[^a]?a[^a]?a[^a]?a", "BAababa", RegexOptions.IgnoreCase, new string[] { "BAababa" } };
            // Implicitly upgrading (or not) setloop to be atomic
            yield return new object[] { null, @"[ac]*", "aaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"[ac]*b", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*b+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*b+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*(?>b+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*[^a]", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*[^a]+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*[^a]+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*(?>[^a]+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*bcd", "aaabcd", RegexOptions.None, new string[] { "aaabcd" } };
            yield return new object[] { null, @"[ac]*[bd]", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*[bd]+", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*[bd]+?", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*(?>[bd]+)", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*[bd]{1,3}", "aaab", RegexOptions.None, new string[] { "aaab" } };
            yield return new object[] { null, @"[ac]*$", "aaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"[ac]*$", "aaa", RegexOptions.Multiline, new string[] { "aaa" } };
            yield return new object[] { null, @"[ac]*\b", "aaa bbb", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"[ac]*\b", "aaa bbb", RegexOptions.ECMAScript, new string[] { "aaa" } };
            yield return new object[] { null, @"[@']*\B", "@@@", RegexOptions.None, new string[] { "@@@" } };
            yield return new object[] { null, @"[@']*\B", "@@@", RegexOptions.ECMAScript, new string[] { "@@@" } };
            yield return new object[] { null, @".*.", "@@@", RegexOptions.Singleline, new string[] { "@@@" } };
            yield return new object[] { null, @"(?:abcd|efg[hij]*)h", "efgh", RegexOptions.None, new string[] { "efgh" } }; // can't upgrade
            yield return new object[] { null, @"(?:abcd|efg[hij]*)ih", "efgjih", RegexOptions.None, new string[] { "efgjih" } }; // can't upgrade
            yield return new object[] { null, @"(?:abcd|efg[hij]*)k", "efgjk", RegexOptions.None, new string[] { "efgjk" } };
            yield return new object[] { null, @"[ace]?b[ace]?b[ace]?b[ace]?b", "cbbabeb", RegexOptions.None, new string[] { "cbbabeb" } };
            yield return new object[] { null, @"[ace]?b[ace]?b[ace]?b[ace]?b", "cBbAbEb", RegexOptions.IgnoreCase, new string[] { "cBbAbEb" } };
            yield return new object[] { null, @"a[^wz]*w", "abcdcdcdwz", RegexOptions.None, new string[] { "abcdcdcdw" } };
            yield return new object[] { null, @"a[^wyz]*w", "abcdcdcdwz", RegexOptions.None, new string[] { "abcdcdcdw" } };
            yield return new object[] { null, @"a[^wyz]*W", "abcdcdcdWz", RegexOptions.IgnoreCase, new string[] { "abcdcdcdW" } };
            // Implicitly upgrading (or not) concat loops to be atomic
            yield return new object[] { null, @"(?:[ab]c[de]f)*", "", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?:[ab]c[de]f)*", "acdf", RegexOptions.None, new string[] { "acdf" } };
            yield return new object[] { null, @"(?:[ab]c[de]f)*", "acdfbcef", RegexOptions.None, new string[] { "acdfbcef" } };
            yield return new object[] { null, @"(?:[ab]c[de]f)*", "cdfbcef", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?:[ab]c[de]f)+", "cdfbcef", RegexOptions.None, new string[] { "bcef" } };
            yield return new object[] { null, @"(?:[ab]c[de]f)*", "bcefbcdfacfe", RegexOptions.None, new string[] { "bcefbcdf" } };
            // Implicitly upgrading (or not) nested loops to be atomic
            yield return new object[] { null, @"(?:a){3}", "aaaaaaaaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"(?:a){3}?", "aaaaaaaaa", RegexOptions.None, new string[] { "aaa" } };
            yield return new object[] { null, @"(?:a{2}){3}", "aaaaaaaaa", RegexOptions.None, new string[] { "aaaaaa" } };
            yield return new object[] { null, @"(?:a{2}?){3}?", "aaaaaaaaa", RegexOptions.None, new string[] { "aaaaaa" } };
            yield return new object[] { null, @"(?:(?:[ab]c[de]f){3}){2}", "acdfbcdfacefbcefbcefbcdfacdef", RegexOptions.None, new string[] { "acdfbcdfacefbcefbcefbcdf" } };
            yield return new object[] { null, @"(?:(?:[ab]c[de]f){3}hello){2}", "aaaaaacdfbcdfacefhellobcefbcefbcdfhellooooo", RegexOptions.None, new string[] { "acdfbcdfacefhellobcefbcefbcdfhello" } };
            yield return new object[] { null, @"CN=(.*[^,]+).*", "CN=localhost", RegexOptions.Singleline, new string[] { "CN=localhost", "localhost" } };
            // Nested atomic
            yield return new object[] { null, @"(?>abc[def]gh(i*))", "123abceghiii456", RegexOptions.None, new string[] { "abceghiii", "iii" } };
            yield return new object[] { null, @"(?>(?:abc)*)", "abcabcabc", RegexOptions.None, new string[] { "abcabcabc" } };

            // Anchoring loops beginning with .* / .+
            yield return new object[] { null, @".*", "", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @".*", "\n\n\n\n", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @".*", "\n\n\n\n", RegexOptions.Singleline, new string[] { "\n\n\n\n" } };
            yield return new object[] { null, @".*[1a]", "\n\n\n\n1", RegexOptions.None, new string[] { "1" } };
            yield return new object[] { null, @"(?s).*(?-s)[1a]", "1\n\n\n\n", RegexOptions.None, new string[] { "1" } };
            yield return new object[] { null, @"(?s).*(?-s)[1a]", "\n\n\n\n1", RegexOptions.None, new string[] { "\n\n\n\n1" } };
            yield return new object[] { null, @".*|.*|.*", "", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @".*123|abc", "abc\n123", RegexOptions.None, new string[] { "abc" } };
            yield return new object[] { null, @".*123|abc", "abc\n123", RegexOptions.Singleline, new string[] { "abc\n123" } };
            yield return new object[] { null, @".*", "\n", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @".*\n", "\n", RegexOptions.None, new string[] { "\n" } };
            yield return new object[] { null, @".*", "\n", RegexOptions.Singleline, new string[] { "\n" } };
            yield return new object[] { null, @".*\n", "\n", RegexOptions.Singleline, new string[] { "\n" } };
            yield return new object[] { null, @".*", "abc", RegexOptions.None, new string[] { "abc" } };
            yield return new object[] { null, @".*abc", "abc", RegexOptions.None, new string[] { "abc" } };
            yield return new object[] { null, @".*abc|ghi", "ghi", RegexOptions.None, new string[] { "ghi" } };
            yield return new object[] { null, @".*abc|.*ghi", "abcghi", RegexOptions.None, new string[] { "abc" } };
            yield return new object[] { null, @".*abc|.*ghi", "bcghi", RegexOptions.None, new string[] { "bcghi" } };
            yield return new object[] { null, @".*abc|.+c", " \n   \n   bc", RegexOptions.None, new string[] { "   bc" } };
            yield return new object[] { null, @".*abc", "12345 abc", RegexOptions.None, new string[] { "12345 abc" } };
            yield return new object[] { null, @".*abc", "12345\n abc", RegexOptions.None, new string[] { " abc" } };
            yield return new object[] { null, @".*abc", "12345\n abc", RegexOptions.Singleline, new string[] { "12345\n abc" } };
            yield return new object[] { null, @"(.*)abc\1", "\n12345abc12345", RegexOptions.Singleline, new string[] { "12345abc12345", "12345" } };
            yield return new object[] { null, @".*\nabc", "\n123\nabc", RegexOptions.None, new string[] { "123\nabc" } };
            yield return new object[] { null, @".*\nabc", "\n123\nabc", RegexOptions.Singleline, new string[] { "\n123\nabc" } };
            yield return new object[] { null, @".*abc", "abc abc abc \nabc", RegexOptions.None, new string[] { "abc abc abc" } };
            yield return new object[] { null, @".*abc", "abc abc abc \nabc", RegexOptions.Singleline, new string[] { "abc abc abc \nabc" } };
            yield return new object[] { null, @".*?abc", "abc abc abc \nabc", RegexOptions.None, new string[] { "abc" } };
            yield return new object[] { null, @"[^\n]*abc", "123abc\n456abc\n789abc", RegexOptions.None, new string[] { "123abc" } };
            yield return new object[] { null, @"[^\n]*abc", "123abc\n456abc\n789abc", RegexOptions.Singleline, new string[] { "123abc" } };
            yield return new object[] { null, @"[^\n]*abc", "123ab\n456abc\n789abc", RegexOptions.None, new string[] { "456abc" } };
            yield return new object[] { null, @"[^\n]*abc", "123ab\n456abc\n789abc", RegexOptions.Singleline, new string[] { "456abc" } };
            yield return new object[] { null, @".+", "a", RegexOptions.None, new string[] { "a" } };
            yield return new object[] { null, @".+", "\nabc", RegexOptions.None, new string[] { "abc" } };
            yield return new object[] { null, @".+", "\n", RegexOptions.Singleline, new string[] { "\n" } };
            yield return new object[] { null, @".+", "\nabc", RegexOptions.Singleline, new string[] { "\nabc" } };
            yield return new object[] { null, @".+abc", "aaaabc", RegexOptions.None, new string[] { "aaaabc" } };
            yield return new object[] { null, @".+abc", "12345 abc", RegexOptions.None, new string[] { "12345 abc" } };
            yield return new object[] { null, @".+abc", "12345\n abc", RegexOptions.None, new string[] { " abc" } };
            yield return new object[] { null, @".+abc", "12345\n abc", RegexOptions.Singleline, new string[] { "12345\n abc" } };
            yield return new object[] { null, @"(.+)abc\1", "\n12345abc12345", RegexOptions.Singleline, new string[] { "12345abc12345", "12345" } };

            // Unanchored .*
            yield return new object[] { null, @"\A\s*(?<name>\w+)(\s*\((?<arguments>.*)\))?\s*\Z", "Match(Name)", RegexOptions.None, new string[] { "Match(Name)", "(Name)", "Match", "Name" } };
            yield return new object[] { null, @"\A\s*(?<name>\w+)(\s*\((?<arguments>.*)\))?\s*\Z", "Match(Na\nme)", RegexOptions.Singleline, new string[] { "Match(Na\nme)", "(Na\nme)", "Match", "Na\nme" } };
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Singleline })
            {
                yield return new object[] { null, @"abcd.*", @"abcabcd", options, new string[] { "abcd" } };
                yield return new object[] { null, @"abcd.*", @"abcabcde", options, new string[] { "abcde" } };
                yield return new object[] { null, @"abcd.*", @"abcabcdefg", options, new string[] { "abcdefg" } };
                yield return new object[] { null, @"abcd(.*)", @"ababcd", options, new string[] { "abcd", "" } };
                yield return new object[] { null, @"abcd(.*)", @"aabcde", options, new string[] { "abcde", "e" } };
                yield return new object[] { null, @"abcd(.*)", @"abcabcdefg", options, new string[] { "abcdefg", "efg" } };
                yield return new object[] { null, @"abcd(.*)e", @"abcabcdefg", options, new string[] { "abcde", "" } };
                yield return new object[] { null, @"abcd(.*)f", @"abcabcdefg", options, new string[] { "abcdef", "e" } };
            }

            // Grouping Constructs Invalid Regular Expressions
            yield return new object[] { null, @"()", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty } };
            yield return new object[] { null, @"(?<cat>)", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty } };
            yield return new object[] { null, @"(?'cat')", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty } };
            yield return new object[] { null, @"(?:)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { null, @"(?imn)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { null, @"(?imn)cat", "(?imn)cat", RegexOptions.None, new string[] { "cat" } };
            yield return new object[] { null, @"(?=)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { null, @"(?<=)", "cat", RegexOptions.None, new string[] { string.Empty } };
            yield return new object[] { null, @"(?>)", "cat", RegexOptions.None, new string[] { string.Empty } };

            // Alternation construct Invalid Regular Expressions
            yield return new object[] { null, @"(?()|)", "(?()|)", RegexOptions.None, new string[] { "" } };

            yield return new object[] { null, @"(?(cat)|)", "cat", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?(cat)|)", "dog", RegexOptions.None, new string[] { "" } };

            yield return new object[] { null, @"(?(cat)catdog|)", "catdog", RegexOptions.None, new string[] { "catdog" } };
            yield return new object[] { null, @"(?(cat)catdog|)", "dog", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?(cat)dog|)", "dog", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?(cat)dog|)", "cat", RegexOptions.None, new string[] { "" } };

            yield return new object[] { null, @"(?(cat)|catdog)", "cat", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?(cat)|catdog)", "catdog", RegexOptions.None, new string[] { "" } };
            yield return new object[] { null, @"(?(cat)|dog)", "dog", RegexOptions.None, new string[] { "dog" } };

            // Invalid unicode
            yield return new object[] { null, "([\u0000-\uFFFF-[azAZ09]]|[\u0000-\uFFFF-[^azAZ09]])+", "azAZBCDE1234567890BCDEFAZza", RegexOptions.None, new string[] { "azAZBCDE1234567890BCDEFAZza", "a" } };
            yield return new object[] { null, "[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]+", "abcxyzABCXYZ123890", RegexOptions.None, new string[] { "bcxyzABCXYZ123890" } };
            yield return new object[] { null, "[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]]+", "bcxyzABCXYZ123890a", RegexOptions.None, new string[] { "a" } };
            yield return new object[] { null, "[\u0000-\uFFFF-[\\p{P}\\p{S}\\p{C}]]+", "!@`';.,$+<>=\x0001\x001FazAZ09", RegexOptions.None, new string[] { "azAZ09" } };

            yield return new object[] { null, @"[\uFFFD-\uFFFF]+", "\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase, new string[] { "\uFFFD\uFFFE\uFFFF" } };
            yield return new object[] { null, @"[\uFFFC-\uFFFE]+", "\uFFFB\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase, new string[] { "\uFFFC\uFFFD\uFFFE" } };

            // Empty Match
            yield return new object[] { null, @"([a*]*)+?$", "ab", RegexOptions.None, new string[] { "", "" } };
            yield return new object[] { null, @"(a*)+?$", "b", RegexOptions.None, new string[] { "", "" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_enUS()
        {
            yield return new object[] { "en-US", "CH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
            yield return new object[] { "en-US", "cH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
            yield return new object[] { "en-US", "AA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
            yield return new object[] { "en-US", "aA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
            yield return new object[] { "en-US", "\u0130", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" } };
            yield return new object[] { "en-US", "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_Czech()
        {
            yield return new object[] { "cs-CZ", "CH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
            yield return new object[] { "cs-CZ", "cH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" } };
        }


        public static IEnumerable<object[]> Groups_CustomCulture_TestData_Danish()
        {
            yield return new object[] { "da-DK", "AA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
            yield return new object[] { "da-DK", "aA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_Turkish()
        {
            yield return new object[] { "tr-TR", "\u0131", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" } };
            yield return new object[] { "tr-TR", "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" } };
        }

        public static IEnumerable<object[]> Groups_CustomCulture_TestData_AzeriLatin()
        {
            if (PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { "az-Latn-AZ", "\u0131", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" } };
                yield return new object[] { "az-Latn-AZ", "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" } };
            }
        }

        [Theory]
        [MemberData(nameof(Groups_Basic_TestData))]
        [MemberData(nameof(Groups_CustomCulture_TestData_enUS))]
        [MemberData(nameof(Groups_CustomCulture_TestData_Czech))]
        [MemberData(nameof(Groups_CustomCulture_TestData_Danish))]
        [MemberData(nameof(Groups_CustomCulture_TestData_Turkish))]
        [MemberData(nameof(Groups_CustomCulture_TestData_AzeriLatin))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56407", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36900", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Groups(string cultureName, string pattern, string input, RegexOptions options, string[] expectedGroups)
        {
            if (cultureName is null)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                cultureName = culture.Equals(CultureInfo.InvariantCulture) ? "en-US" : culture.Name;
            }

            using (new ThreadCultureChange(cultureName))
            {
                Groups(pattern, input, options, expectedGroups);
                Groups(pattern, input, RegexOptions.Compiled | options, expectedGroups);
            }

            static void Groups(string pattern, string input, RegexOptions options, string[] expectedGroups)
            {
                Regex regex = new Regex(pattern, options);
                Match match = regex.Match(input);
                Assert.True(match.Success);

                Assert.Equal(expectedGroups.Length, match.Groups.Count);
                Assert.Equal(expectedGroups[0], match.Value);

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
