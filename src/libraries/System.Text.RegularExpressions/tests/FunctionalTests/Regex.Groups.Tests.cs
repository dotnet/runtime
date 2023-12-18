// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Tests;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexGroupTests
    {
        public static IEnumerable<object[]> Groups_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                (CultureInfo Culture, string Pattern, string Input, RegexOptions Options, string[] Expected)[] cases = Groups_MemberData_Cases(engine).ToArray();
                Regex[] regexes = RegexHelpers.GetRegexesAsync(engine, cases.Select(c => (c.Pattern, c.Culture, (RegexOptions?)c.Options, (TimeSpan?)null)).ToArray()).Result;
                for (int i = 0; i < regexes.Length; i++)
                {
                    yield return new object[] { regexes[i], cases[i].Culture, cases[i].Input, cases[i].Expected };
                }
            }
        }

        private static IEnumerable<(CultureInfo Culture, string Pattern, string Input, RegexOptions Options, string[] Expected)> Groups_MemberData_Cases(RegexEngine engine)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            CultureInfo csCZ = new CultureInfo("cs-CZ");
            CultureInfo daDK = new CultureInfo("da-DK");
            CultureInfo trTR = new CultureInfo("tr-TR");
            CultureInfo azLatnAZ = new CultureInfo("az-Latn-AZ");

            // (A - B) B is a subset of A(ie B only contains chars that are in A)
            yield return (enUS, "[abcd-[d]]+", "dddaabbccddd", RegexOptions.None, new string[] { "aabbcc" });

            yield return (enUS, @"[\d-[357]]+", "33312468955", RegexOptions.None, new string[] { "124689" });
            yield return (enUS, @"[\d-[357]]+", "51246897", RegexOptions.None, new string[] { "124689" });
            yield return (enUS, @"[\d-[357]]+", "3312468977", RegexOptions.None, new string[] { "124689" });

            yield return (enUS, @"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" });

            yield return (enUS, @"[\w-[\d]]+", "0AZaz9", RegexOptions.None, new string[] { "AZaz" });
            yield return (enUS, @"[\w-[\p{Ll}]]+", "a09AZz", RegexOptions.None, new string[] { "09AZ" });

            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"[\d-[13579]]+", "1024689", RegexOptions.ECMAScript, new string[] { "02468" });
                yield return (enUS, @"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" });
            }
            yield return (enUS, @"[\d-[13579]]+", "\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" });

            yield return (enUS, @"[\p{Ll}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" });
            yield return (enUS, @"[\p{Nd}-[2468]]+", "20135798", RegexOptions.None, new string[] { "013579" });

            yield return (enUS, @"[\P{Lu}-[ae-z]]+", "aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" });
            yield return (enUS, @"[\P{Nd}-[\p{Ll}]]+", "az09AZ'[]", RegexOptions.None, new string[] { "AZ'[]" });

            // (A - B) B is a superset of A (ie B contains chars that are in A plus other chars that are not in A)
            yield return (enUS, "[abcd-[def]]+", "fedddaabbccddd", RegexOptions.None, new string[] { "aabbcc" });

            yield return (enUS, @"[\d-[357a-z]]+", "az33312468955", RegexOptions.None, new string[] { "124689" });
            yield return (enUS, @"[\d-[de357fgA-Z]]+", "AZ51246897", RegexOptions.None, new string[] { "124689" });
            yield return (enUS, @"[\d-[357\p{Ll}]]+", "az3312468977", RegexOptions.None, new string[] { "124689" });

            yield return (enUS, @"[\w-[b-y\s]]+", " \tbbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" });

            yield return (enUS, @"[\w-[\d\p{Po}]]+", "!#0AZaz9", RegexOptions.None, new string[] { "AZaz" });
            yield return (enUS, @"[\w-[\p{Ll}\s]]+", "a09AZz", RegexOptions.None, new string[] { "09AZ" });

            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"[\d-[13579a-zA-Z]]+", "AZ1024689", RegexOptions.ECMAScript, new string[] { "02468" });
                yield return (enUS, @"[\d-[13579abcd]]+", "abcd\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" });
            }
            yield return (enUS, @"[\d-[13579\s]]+", " \t\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" });

            yield return (enUS, @"[\w-[b-y\p{Po}]]+", "!#bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" });

            yield return (enUS, @"[\w-[b-y!.,]]+", "!.,bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" });
            yield return (enUS, "[\\w-[b-y\x00-\x0F]]+", "\0bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "aaaABCD09zzz" });

            yield return (enUS, @"[\p{Ll}-[ae-z0-9]]+", "09aaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" });
            yield return (enUS, @"[\p{Nd}-[2468az]]+", "az20135798", RegexOptions.None, new string[] { "013579" });

            yield return (enUS, @"[\P{Lu}-[ae-zA-Z]]+", "AZaaabbbcccdddeee", RegexOptions.None, new string[] { "bbbcccddd" });
            yield return (enUS, @"[\P{Nd}-[\p{Ll}0123456789]]+", "09az09AZ'[]", RegexOptions.None, new string[] { "AZ'[]" });

            // (A - B) B only contains chars that are not in A
            yield return (enUS, "[abc-[defg]]+", "dddaabbccddd", RegexOptions.None, new string[] { "aabbcc" });

            yield return (enUS, @"[\d-[abc]]+", "abc09abc", RegexOptions.None, new string[] { "09" });
            yield return (enUS, @"[\d-[a-zA-Z]]+", "az09AZ", RegexOptions.None, new string[] { "09" });
            yield return (enUS, @"[\d-[\p{Ll}]]+", "az09az", RegexOptions.None, new string[] { "09" });

            yield return (enUS, @"[\w-[\x00-\x0F]]+", "bbbaaaABYZ09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABYZ09zzzyyy" });

            yield return (enUS, @"[\w-[\s]]+", "0AZaz9", RegexOptions.None, new string[] { "0AZaz9" });
            yield return (enUS, @"[\w-[\W]]+", "0AZaz9", RegexOptions.None, new string[] { "0AZaz9" });
            yield return (enUS, @"[\w-[\p{Po}]]+", "#a09AZz!", RegexOptions.None, new string[] { "a09AZz" });

            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"[\d-[\D]]+", "azAZ1024689", RegexOptions.ECMAScript, new string[] { "1024689" });
                yield return (enUS, @"[\d-[a-zA-Z]]+", "azAZ\x066102468\x0660", RegexOptions.ECMAScript, new string[] { "02468" });
            }
            yield return (enUS, @"[\d-[\p{Ll}]]+", "\x066102468\x0660", RegexOptions.None, new string[] { "\x066102468\x0660" });

            yield return (enUS, @"[a-zA-Z0-9-[\s]]+", " \tazAZ09", RegexOptions.None, new string[] { "azAZ09" });

            yield return (enUS, @"[a-zA-Z0-9-[\W]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABCD09zzzyyy" });
            yield return (enUS, @"[a-zA-Z0-9-[^a-zA-Z0-9]]+", "bbbaaaABCD09zzzyyy", RegexOptions.None, new string[] { "bbbaaaABCD09zzzyyy" });

            yield return (enUS, @"[\p{Ll}-[A-Z]]+", "AZaz09", RegexOptions.None, new string[] { "az" });
            yield return (enUS, @"[\p{Nd}-[a-z]]+", "az09", RegexOptions.None, new string[] { "09" });

            yield return (enUS, @"[\P{Lu}-[\p{Lu}]]+", "AZazAZ", RegexOptions.None, new string[] { "az" });
            yield return (enUS, @"[\P{Lu}-[A-Z]]+", "AZazAZ", RegexOptions.None, new string[] { "az" });
            yield return (enUS, @"[\P{Nd}-[\p{Nd}]]+", "azAZ09", RegexOptions.None, new string[] { "azAZ" });
            yield return (enUS, @"[\P{Nd}-[2-8]]+", "1234567890azAZ1234567890", RegexOptions.None, new string[] { "azAZ" });

            // Alternating construct
            yield return (enUS, @"([ ]|[\w-[0-9]])+", "09az AZ90", RegexOptions.None, new string[] { "az AZ", "Z" });
            yield return (enUS, @"([0-9-[02468]]|[0-9-[13579]])+", "az1234567890za", RegexOptions.None, new string[] { "1234567890", "0" });
            yield return (enUS, @"([^0-9-[a-zAE-Z]]|[\w-[a-zAF-Z]])+", "azBCDE1234567890BCDEFza", RegexOptions.None, new string[] { "BCDE1234567890BCDE", "E" });
            yield return (enUS, @"([\p{Ll}-[aeiou]]|[^\w-[\s]])+", "aeiobcdxyz!@#aeio", RegexOptions.None, new string[] { "bcdxyz!@#", "#" });
            yield return (enUS, @"(?:hello|hi){1,3}", "hello", RegexOptions.None, new string[] { "hello" });
            yield return (enUS, @"(hello|hi){1,3}", "hellohihey", RegexOptions.None, new string[] { "hellohi", "hi" });
            yield return (enUS, @"(?:hello|hi){1,3}", "hellohihey", RegexOptions.None, new string[] { "hellohi" });
            yield return (enUS, @"(?:hello|hi){2,2}", "hellohihey", RegexOptions.None, new string[] { "hellohi" });
            yield return (enUS, @"(?:hello|hi){2,2}?", "hellohihihello", RegexOptions.None, new string[] { "hellohi" });
            yield return (enUS, @"(?:abc|def|ghi|hij|klm|no){1,4}", "this is a test nonoabcxyz this is only a test", RegexOptions.None, new string[] { "nonoabc" });
            yield return (enUS, @"xyz(abc|def)xyz", "abcxyzdefxyzabc", RegexOptions.None, new string[] { "xyzdefxyz", "def" });
            yield return (enUS, @"abc|(?:def|ghi)", "ghi", RegexOptions.None, new string[] { "ghi" });
            yield return (enUS, @"abc|(def|ghi)", "def", RegexOptions.None, new string[] { "def", "def" });

            // Multiple character classes using character class subtraction
            yield return (enUS, @"98[\d-[9]][\d-[8]][\d-[0]]", "98911 98881 98870 98871", RegexOptions.None, new string[] { "98871" });
            yield return (enUS, @"m[\w-[^aeiou]][\w-[^aeiou]]t", "mbbt mect meet", RegexOptions.None, new string[] { "meet" });

            // Negation with character class subtraction
            yield return (enUS, "[abcdef-[^bce]]+", "adfbcefda", RegexOptions.None, new string[] { "bce" });
            yield return (enUS, "[^cde-[ag]]+", "agbfxyzga", RegexOptions.None, new string[] { "bfxyz" });

            // Misc The idea here is come up with real world examples of char class subtraction. Things that
            // would be difficult to define without it
            yield return (enUS, @"[\p{L}-[^\p{Lu}]]+", "09',.abcxyzABCXYZ", RegexOptions.None, new string[] { "ABCXYZ" });

            yield return (enUS, @"[\p{IsGreek}-[\P{Lu}]]+", "\u0390\u03FE\u0386\u0388\u03EC\u03EE\u0400", RegexOptions.None, new string[] { "\u03FE\u0386\u0388\u03EC\u03EE" });
            yield return (enUS, @"[\p{IsBasicLatin}-[G-L]]+", "GAFMZL", RegexOptions.None, new string[] { "AFMZ" });

            yield return (enUS, "[a-zA-Z-[aeiouAEIOU]]+", "aeiouAEIOUbcdfghjklmnpqrstvwxyz", RegexOptions.None, new string[] { "bcdfghjklmnpqrstvwxyz" });

            // The following is an overly complex way of matching an ip address using char class subtraction
            if (!RegexHelpers.IsNonBacktracking(engine)) // conditionals not supported
            {
                yield return (enUS, @"^
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
                , "255", RegexOptions.IgnorePatternWhitespace, new string[] { "255", "255", "2", "5", "5", "", "255", "2", "5" });
            }

            // Character Class Subtraction
            yield return (enUS, @"[abcd\-d-[bc]]+", "bbbaaa---dddccc", RegexOptions.None, new string[] { "aaa---ddd" });
            yield return (enUS, @"[^a-f-[\x00-\x60\u007B-\uFFFF]]+", "aaafffgggzzz{{{", RegexOptions.None, new string[] { "gggzzz" });
            yield return (enUS, @"[\[\]a-f-[[]]+", "gggaaafff]]][[[", RegexOptions.None, new string[] { "aaafff]]]" });
            yield return (enUS, @"[\[\]a-f-[]]]+", "gggaaafff[[[]]]", RegexOptions.None, new string[] { "aaafff[[[" });

            yield return (enUS, @"[ab\-\[cd-[-[]]]]", "a]]", RegexOptions.None, new string[] { "a]]" });
            yield return (enUS, @"[ab\-\[cd-[-[]]]]", "b]]", RegexOptions.None, new string[] { "b]]" });
            yield return (enUS, @"[ab\-\[cd-[-[]]]]", "c]]", RegexOptions.None, new string[] { "c]]" });
            yield return (enUS, @"[ab\-\[cd-[-[]]]]", "d]]", RegexOptions.None, new string[] { "d]]" });

            yield return (enUS, @"[ab\-\[cd-[[]]]]", "a]]", RegexOptions.None, new string[] { "a]]" });
            yield return (enUS, @"[ab\-\[cd-[[]]]]", "b]]", RegexOptions.None, new string[] { "b]]" });
            yield return (enUS, @"[ab\-\[cd-[[]]]]", "c]]", RegexOptions.None, new string[] { "c]]" });
            yield return (enUS, @"[ab\-\[cd-[[]]]]", "d]]", RegexOptions.None, new string[] { "d]]" });
            yield return (enUS, @"[ab\-\[cd-[[]]]]", "-]]", RegexOptions.None, new string[] { "-]]" });

            yield return (enUS, @"[a-[c-e]]+", "bbbaaaccc", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"[a-[c-e]]+", "```aaaccc", RegexOptions.None, new string[] { "aaa" });

            yield return (enUS, @"[a-d\--[bc]]+", "cccaaa--dddbbb", RegexOptions.None, new string[] { "aaa--ddd" });

            // Not Character class subtraction
            yield return (enUS, @"[\0- [bc]+", "!!!\0\0\t\t  [[[[bbbcccaaa", RegexOptions.None, new string[] { "\0\0\t\t  [[[[bbbccc" });
            yield return (enUS, "[[abcd]-[bc]]+", "a-b]", RegexOptions.None, new string[] { "a-b]" });
            yield return (enUS, "[-[e-g]+", "ddd[[[---eeefffggghhh", RegexOptions.None, new string[] { "[[[---eeefffggg" });
            yield return (enUS, "[-e-g]+", "ddd---eeefffggghhh", RegexOptions.None, new string[] { "---eeefffggg" });
            yield return (enUS, "[a-e - m-p]+", "---a b c d e m n o p---", RegexOptions.None, new string[] { "a b c d e m n o p" });
            yield return (enUS, "[^-[bc]]", "b] c] -] aaaddd]", RegexOptions.None, new string[] { "d]" });
            yield return (enUS, "[^-[bc]]", "b] c] -] aaa]ddd]", RegexOptions.None, new string[] { "a]" });

            // Make sure we correctly handle \-
            yield return (enUS, @"[a\-[bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" });
            yield return (enUS, @"[a\-[\-\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" });
            yield return (enUS, @"[a\-\[\-\[\-bc]+", "```bbbaaa---[[[cccddd", RegexOptions.None, new string[] { "bbbaaa---[[[ccc" });
            yield return (enUS, @"[abc\--[b]]+", "[[[```bbbaaa---cccddd", RegexOptions.None, new string[] { "aaa---ccc" });
            yield return (enUS, @"[abc\-z-[b]]+", "```aaaccc---zzzbbb", RegexOptions.None, new string[] { "aaaccc---zzz" });
            yield return (enUS, @"[a-d\-[b]+", "```aaabbbcccddd----[[[[]]]", RegexOptions.None, new string[] { "aaabbbcccddd----[[[[" });
            yield return (enUS, @"[abcd\-d\-[bc]+", "bbbaaa---[[[dddccc", RegexOptions.None, new string[] { "bbbaaa---[[[dddccc" });

            // Everything works correctly with option RegexOptions.IgnorePatternWhitespace
            yield return (enUS, "[a - c - [ b ] ]+", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace, new string[] { " ]]]" });
            yield return (enUS, "[a - c - [ b ] +", "dddaaa   ccc [[[[ bbb ]]]", RegexOptions.IgnorePatternWhitespace, new string[] { "aaa   ccc [[[[ bbb " });

            // Unicode Char Classes
            yield return (enUS, @"(\p{Lu}\w*)\s(\p{Lu}\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" });
            yield return (enUS, @"(\p{Lu}\p{Ll}*)\s(\p{Lu}\p{Ll}*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" });
            yield return (enUS, @"(\P{Ll}\p{Ll}*)\s(\P{Ll}\p{Ll}*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" });
            yield return (enUS, @"(\P{Lu}+\p{Lu})\s(\P{Lu}+\p{Lu})", "hellO worlD", RegexOptions.None, new string[] { "hellO worlD", "hellO", "worlD" });
            yield return (enUS, @"(\p{Lt}\w*)\s(\p{Lt}*\w*)", "\u01C5ello \u01C5orld", RegexOptions.None, new string[] { "\u01C5ello \u01C5orld", "\u01C5ello", "\u01C5orld" });
            yield return (enUS, @"(\P{Lt}\w*)\s(\P{Lt}*\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" });

            // Character ranges IgnoreCase
            yield return (enUS, @"[@-D]+", "eE?@ABCDabcdeE", RegexOptions.IgnoreCase, new string[] { "@ABCDabcd" });
            yield return (enUS, @"[>-D]+", "eE=>?@ABCDabcdeE", RegexOptions.IgnoreCase, new string[] { ">?@ABCDabcd" });
            yield return (enUS, @"[\u0554-\u0557]+", "\u0583\u0553\u0554\u0555\u0556\u0584\u0585\u0586\u0557\u0558", RegexOptions.IgnoreCase, new string[] { "\u0554\u0555\u0556\u0584\u0585\u0586\u0557" });
            yield return (enUS, @"[X-\]]+", "wWXYZxyz[\\]^", RegexOptions.IgnoreCase, new string[] { "XYZxyz[\\]" });
            yield return (enUS, @"[X-\u0533]+", "\u0551\u0554\u0560AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563\u0564", RegexOptions.IgnoreCase, new string[] { "AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563" });
            yield return (enUS, @"[X-a]+", "wWAXYZaxyz", RegexOptions.IgnoreCase, new string[] { "AXYZaxyz" });
            yield return (enUS, @"[X-c]+", "wWABCXYZabcxyz", RegexOptions.IgnoreCase, new string[] { "ABCXYZabcxyz" });
            yield return (enUS, @"[X-\u00C0]+", "\u00C1\u00E1\u00C0\u00E0wWABCXYZabcxyz", RegexOptions.IgnoreCase, new string[] { "\u00C0\u00E0wWABCXYZabcxyz" });
            yield return (enUS, @"[\u0100\u0102\u0104]+", "\u00FF \u0100\u0102\u0104\u0101\u0103\u0105\u0106", RegexOptions.IgnoreCase, new string[] { "\u0100\u0102\u0104\u0101\u0103\u0105" });
            yield return (enUS, @"[B-D\u0130]+", "aAeE\u0129\u0131\u0068 BCDbcD\u0130\u0069\u0070", RegexOptions.IgnoreCase, new string[] { "BCDbcD\u0130\u0069" });
            yield return (enUS, @"[\u013B\u013D\u013F]+", "\u013A\u013B\u013D\u013F\u013C\u013E\u0140\u0141", RegexOptions.IgnoreCase, new string[] { "\u013B\u013D\u013F\u013C\u013E\u0140" });

            // Escape Chars
            yield return (enUS, "(Cat)\r(Dog)", "Cat\rDog", RegexOptions.None, new string[] { "Cat\rDog", "Cat", "Dog" });
            yield return (enUS, "(Cat)\t(Dog)", "Cat\tDog", RegexOptions.None, new string[] { "Cat\tDog", "Cat", "Dog" });
            yield return (enUS, "(Cat)\f(Dog)", "Cat\fDog", RegexOptions.None, new string[] { "Cat\fDog", "Cat", "Dog" });

            // Miscellaneous { witout matching }
            yield return (enUS, @"{5", "hello {5 world", RegexOptions.None, new string[] { "{5" });
            yield return (enUS, @"{5,", "hello {5, world", RegexOptions.None, new string[] { "{5," });
            yield return (enUS, @"{5,6", "hello {5,6 world", RegexOptions.None, new string[] { "{5,6" });

            // Miscellaneous inline options
            yield return (enUS, @"(?n:(?<cat>cat)(\s+)(?<dog>dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "cat", "dog" });
            yield return (enUS, @"(?n:(cat)(\s+)(dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog" });
            yield return (enUS, @"(?n:(cat)(?<SpaceChars>\s+)(dog))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "   " });
            yield return (enUS, @"(?x:
                            (?<cat>cat) # Cat statement
                            (\s+) # Whitespace chars
                            (?<dog>dog # Dog statement
                            ))", "cat   dog", RegexOptions.None, new string[] { "cat   dog", "   ", "cat", "dog" });
            yield return (enUS, @"(?+i:cat)", "CAT", RegexOptions.None, new string[] { "CAT" });

            // \d, \D, \s, \S, \w, \W, \P, \p inside character range
            yield return (enUS, @"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.None, new string[] { "cat230927dog", "230927" });
            yield return (enUS, @"([\D]*)dog", "65498catdog58719", RegexOptions.None, new string[] { "catdog", "cat" });
            yield return (enUS, @"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.None, new string[] { "cat   dog", "   " });
            yield return (enUS, @"cat([\S]*)", "sfdcatdog    3270", RegexOptions.None, new string[] { "catdog", "dog" });
            yield return (enUS, @"cat([\w]*)", "sfdcatdog    3270", RegexOptions.None, new string[] { "catdog", "dog" });
            yield return (enUS, @"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.None, new string[] { "cat   dog", "   " });
            yield return (enUS, @"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" });
            yield return (enUS, @"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.None, new string[] { "Hello World", "Hello", "World" });

            // \x, \u, \a, \b, \e, \f, \n, \r, \t, \v, \c, inside character range
            yield return (enUS, @"(cat)([\x41]*)(dog)", "catAAAdog", RegexOptions.None, new string[] { "catAAAdog", "cat", "AAA", "dog" });
            yield return (enUS, @"(cat)([\u0041]*)(dog)", "catAAAdog", RegexOptions.None, new string[] { "catAAAdog", "cat", "AAA", "dog" });
            yield return (enUS, @"(cat)([\a]*)(dog)", "cat\a\a\adog", RegexOptions.None, new string[] { "cat\a\a\adog", "cat", "\a\a\a", "dog" });
            yield return (enUS, @"(cat)([\b]*)(dog)", "cat\b\b\bdog", RegexOptions.None, new string[] { "cat\b\b\bdog", "cat", "\b\b\b", "dog" });
            yield return (enUS, @"(cat)([\e]*)(dog)", "cat\u001B\u001B\u001Bdog", RegexOptions.None, new string[] { "cat\u001B\u001B\u001Bdog", "cat", "\u001B\u001B\u001B", "dog" });
            yield return (enUS, @"(cat)([\f]*)(dog)", "cat\f\f\fdog", RegexOptions.None, new string[] { "cat\f\f\fdog", "cat", "\f\f\f", "dog" });
            yield return (enUS, @"(cat)([\r]*)(dog)", "cat\r\r\rdog", RegexOptions.None, new string[] { "cat\r\r\rdog", "cat", "\r\r\r", "dog" });
            yield return (enUS, @"(cat)([\v]*)(dog)", "cat\v\v\vdog", RegexOptions.None, new string[] { "cat\v\v\vdog", "cat", "\v\v\v", "dog" });

            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                // \d, \D, \s, \S, \w, \W, \P, \p inside character range ([0-5]) with ECMA Option
                yield return (enUS, @"cat([\d]*)dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript, new string[] { "cat230927dog", "230927" });
                yield return (enUS, @"([\D]*)dog", "65498catdog58719", RegexOptions.ECMAScript, new string[] { "catdog", "cat" });
                yield return (enUS, @"cat([\s]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "   " });
                yield return (enUS, @"cat([\S]*)", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "dog" });
                yield return (enUS, @"cat([\w]*)", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "dog" });
                yield return (enUS, @"cat([\W]*)dog", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "   " });
                yield return (enUS, @"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "Hello", "World" });
                yield return (enUS, @"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "Hello", "World" });

                // \d, \D, \s, \S, \w, \W, \P, \p outside character range ([0-5]) with ECMA Option
                yield return (enUS, @"(cat)\d*dog", "hello123cat230927dog1412d", RegexOptions.ECMAScript, new string[] { "cat230927dog", "cat" });
                yield return (enUS, @"\D*(dog)", "65498catdog58719", RegexOptions.ECMAScript, new string[] { "catdog", "dog" });
                yield return (enUS, @"(cat)\s*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "cat", "dog" });
                yield return (enUS, @"(cat)\S*", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "cat" });
                yield return (enUS, @"(cat)\w*", "sfdcatdog    3270", RegexOptions.ECMAScript, new string[] { "catdog", "cat" });
                yield return (enUS, @"(cat)\W*(dog)", "wiocat   dog3270", RegexOptions.ECMAScript, new string[] { "cat   dog", "cat", "dog" });
                yield return (enUS, @"\p{Lu}(\w*)\s\p{Lu}(\w*)", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World", "ello", "orld" });
                yield return (enUS, @"\P{Ll}\p{Ll}*\s\P{Ll}\p{Ll}*", "Hello World", RegexOptions.ECMAScript, new string[] { "Hello World" });
            }

            // Use < in a group
            yield return (enUS, @"cat(?<dog121>dog)", "catcatdogdogcat", RegexOptions.None, new string[] { "catdog", "dog" });
            yield return (enUS, @"(?<cat>cat)\s*(?<cat>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" });
            yield return (enUS, @"(?<1>cat)\s*(?<1>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" });
            yield return (enUS, @"(?<2048>cat)\s*(?<2048>dog)", "catcat    dogdogcat", RegexOptions.None, new string[] { "cat    dog", "dog" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // balancing groups not supported
            {
                yield return (enUS, @"(?<cat>cat)\w+(?<dog-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" });
                yield return (enUS, @"(?<cat>cat)\w+(?<-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "" });
                yield return (enUS, @"(?<cat>cat)\w+(?<cat-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "_Hello_World_" });
                yield return (enUS, @"(?<1>cat)\w+(?<dog-1>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" });
                yield return (enUS, @"(?<cat>cat)\w+(?<2-cat>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" });
                yield return (enUS, @"(?<1>cat)\w+(?<2-1>dog)", "cat_Hello_World_dog", RegexOptions.None, new string[] { "cat_Hello_World_dog", "", "_Hello_World_" });
            }

            // Quantifiers
            yield return (enUS, @"(?<cat>cat){", "STARTcat{", RegexOptions.None, new string[] { "cat{", "cat" });
            yield return (enUS, @"(?<cat>cat){fdsa", "STARTcat{fdsa", RegexOptions.None, new string[] { "cat{fdsa", "cat" });
            yield return (enUS, @"(?<cat>cat){1", "STARTcat{1", RegexOptions.None, new string[] { "cat{1", "cat" });
            yield return (enUS, @"(?<cat>cat){1END", "STARTcat{1END", RegexOptions.None, new string[] { "cat{1END", "cat" });
            yield return (enUS, @"(?<cat>cat){1,", "STARTcat{1,", RegexOptions.None, new string[] { "cat{1,", "cat" });
            yield return (enUS, @"(?<cat>cat){1,END", "STARTcat{1,END", RegexOptions.None, new string[] { "cat{1,END", "cat" });
            yield return (enUS, @"(?<cat>cat){1,2", "STARTcat{1,2", RegexOptions.None, new string[] { "cat{1,2", "cat" });
            yield return (enUS, @"(?<cat>cat){1,2END", "STARTcat{1,2END", RegexOptions.None, new string[] { "cat{1,2END", "cat" });

            // Use IgnorePatternWhitespace
            yield return (enUS, @"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog
                            ", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" });
            yield return (enUS, @"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" });
            yield return (enUS, @"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace) (dog)  (?#followed by dog)", "cat    dog", RegexOptions.IgnorePatternWhitespace, new string[] { "cat    dog", "cat", "dog" });

            // Back Reference
            if (!RegexHelpers.IsNonBacktracking(engine)) // back references not supported
            {
                yield return (enUS, @"(?<cat>cat)(?<dog>dog)\k<cat>", "asdfcatdogcatdog", RegexOptions.None, new string[] { "catdogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\k<cat>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\k'cat'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\<cat>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\'cat'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });

                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\k<1>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\k'1'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\<1>", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\'1'", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.None, new string[] { "cat   dogcat", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", RegexOptions.ECMAScript, new string[] { "cat   dogcat", "cat", "dog" });

                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\k<dog>", "asdfcat   dogdog   dog", RegexOptions.None, new string[] { "cat   dogdog", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.None, new string[] { "cat   dogdog", "cat", "dog" });
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", RegexOptions.ECMAScript, new string[] { "cat   dogdog", "cat", "dog" });
            }

            // Octal
            yield return (enUS, @"(cat)(\077)", "hellocat?dogworld", RegexOptions.None, new string[] { "cat?", "cat", "?" });
            yield return (enUS, @"(cat)(\77)", "hellocat?dogworld", RegexOptions.None, new string[] { "cat?", "cat", "?" });
            yield return (enUS, @"(cat)(\176)", "hellocat~dogworld", RegexOptions.None, new string[] { "cat~", "cat", "~" });
            yield return (enUS, @"(cat)(\400)", "hellocat\0dogworld", RegexOptions.None, new string[] { "cat\0", "cat", "\0" });
            yield return (enUS, @"(cat)(\300)", "hellocat\u00C0dogworld", RegexOptions.None, new string[] { "cat\u00C0", "cat", "\u00C0" });
            yield return (enUS, @"(cat)(\477)", "hellocat\u003Fdogworld", RegexOptions.None, new string[] { "cat\u003F", "cat", "\u003F" });
            yield return (enUS, @"(cat)(\777)", "hellocat\u00FFdogworld", RegexOptions.None, new string[] { "cat\u00FF", "cat", "\u00FF" });
            yield return (enUS, @"(cat)(\7770)", "hellocat\u00FF0dogworld", RegexOptions.None, new string[] { "cat\u00FF0", "cat", "\u00FF0" });

            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"(cat)(\077)", "hellocat?dogworld", RegexOptions.ECMAScript, new string[] { "cat?", "cat", "?" });
                yield return (enUS, @"(cat)(\77)", "hellocat?dogworld", RegexOptions.ECMAScript, new string[] { "cat?", "cat", "?" });
                yield return (enUS, @"(cat)(\7)", "hellocat\adogworld", RegexOptions.ECMAScript, new string[] { "cat\a", "cat", "\a" });
                yield return (enUS, @"(cat)(\40)", "hellocat dogworld", RegexOptions.ECMAScript, new string[] { "cat ", "cat", " " });
                yield return (enUS, @"(cat)(\040)", "hellocat dogworld", RegexOptions.ECMAScript, new string[] { "cat ", "cat", " " });
                yield return (enUS, @"(cat)(\176)", "hellocatcat76dogworld", RegexOptions.ECMAScript, new string[] { "catcat76", "cat", "cat76" });
                yield return (enUS, @"(cat)(\377)", "hellocat\u00FFdogworld", RegexOptions.ECMAScript, new string[] { "cat\u00FF", "cat", "\u00FF" });
                yield return (enUS, @"(cat)(\400)", "hellocat 0Fdogworld", RegexOptions.ECMAScript, new string[] { "cat 0", "cat", " 0" });
            }

            // Decimal
            yield return (enUS, @"(cat)\s+(?<2147483646>dog)", "asdlkcat  dogiwod", RegexOptions.None, new string[] { "cat  dog", "cat", "dog" });
            yield return (enUS, @"(cat)\s+(?<2147483647>dog)", "asdlkcat  dogiwod", RegexOptions.None, new string[] { "cat  dog", "cat", "dog" });

            // Hex
            yield return (enUS, @"(cat)(\x2a*)(dog)", "asdlkcat***dogiwod", RegexOptions.None, new string[] { "cat***dog", "cat", "***", "dog" });
            yield return (enUS, @"(cat)(\x2b*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None, new string[] { "cat+++dog", "cat", "+++", "dog" });
            yield return (enUS, @"(cat)(\x2c*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None, new string[] { "cat,,,dog", "cat", ",,,", "dog" });
            yield return (enUS, @"(cat)(\x2d*)(dog)", "asdlkcat---dogiwod", RegexOptions.None, new string[] { "cat---dog", "cat", "---", "dog" });
            yield return (enUS, @"(cat)(\x2e*)(dog)", "asdlkcat...dogiwod", RegexOptions.None, new string[] { "cat...dog", "cat", "...", "dog" });
            yield return (enUS, @"(cat)(\x2f*)(dog)", "asdlkcat///dogiwod", RegexOptions.None, new string[] { "cat///dog", "cat", "///", "dog" });

            yield return (enUS, @"(cat)(\x2A*)(dog)", "asdlkcat***dogiwod", RegexOptions.None, new string[] { "cat***dog", "cat", "***", "dog" });
            yield return (enUS, @"(cat)(\x2B*)(dog)", "asdlkcat+++dogiwod", RegexOptions.None, new string[] { "cat+++dog", "cat", "+++", "dog" });
            yield return (enUS, @"(cat)(\x2C*)(dog)", "asdlkcat,,,dogiwod", RegexOptions.None, new string[] { "cat,,,dog", "cat", ",,,", "dog" });
            yield return (enUS, @"(cat)(\x2D*)(dog)", "asdlkcat---dogiwod", RegexOptions.None, new string[] { "cat---dog", "cat", "---", "dog" });
            yield return (enUS, @"(cat)(\x2E*)(dog)", "asdlkcat...dogiwod", RegexOptions.None, new string[] { "cat...dog", "cat", "...", "dog" });
            yield return (enUS, @"(cat)(\x2F*)(dog)", "asdlkcat///dogiwod", RegexOptions.None, new string[] { "cat///dog", "cat", "///", "dog" });

            // ScanControl
            yield return (enUS, @"(cat)(\c@*)(dog)", "asdlkcat\0\0dogiwod", RegexOptions.None, new string[] { "cat\0\0dog", "cat", "\0\0", "dog" });
            yield return (enUS, @"(cat)(\cA*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None, new string[] { "cat\u0001dog", "cat", "\u0001", "dog" });
            yield return (enUS, @"(cat)(\ca*)(dog)", "asdlkcat\u0001dogiwod", RegexOptions.None, new string[] { "cat\u0001dog", "cat", "\u0001", "dog" });

            yield return (enUS, @"(cat)(\cC*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None, new string[] { "cat\u0003dog", "cat", "\u0003", "dog" });
            yield return (enUS, @"(cat)(\cc*)(dog)", "asdlkcat\u0003dogiwod", RegexOptions.None, new string[] { "cat\u0003dog", "cat", "\u0003", "dog" });

            yield return (enUS, @"(cat)(\cD*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None, new string[] { "cat\u0004dog", "cat", "\u0004", "dog" });
            yield return (enUS, @"(cat)(\cd*)(dog)", "asdlkcat\u0004dogiwod", RegexOptions.None, new string[] { "cat\u0004dog", "cat", "\u0004", "dog" });

            yield return (enUS, @"(cat)(\cX*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None, new string[] { "cat\u0018dog", "cat", "\u0018", "dog" });
            yield return (enUS, @"(cat)(\cx*)(dog)", "asdlkcat\u0018dogiwod", RegexOptions.None, new string[] { "cat\u0018dog", "cat", "\u0018", "dog" });

            yield return (enUS, @"(cat)(\cZ*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None, new string[] { "cat\u001adog", "cat", "\u001a", "dog" });
            yield return (enUS, @"(cat)(\cz*)(dog)", "asdlkcat\u001adogiwod", RegexOptions.None, new string[] { "cat\u001adog", "cat", "\u001a", "dog" });

            if (!PlatformDetection.IsNetFramework) // `\c[` was not handled in .NET Framework. See https://github.com/dotnet/runtime/issues/24759.
            {
                yield return (enUS, @"(cat)(\c[*)(dog)", "asdlkcat\u001bdogiwod", RegexOptions.None, new string[] { "cat\u001bdog", "cat", "\u001b", "dog" });
            }

            // Atomic Zero-Width Assertions \A \G ^ \Z \z \b \B
            //\A
            yield return (enUS, @"\Acat\s+dog", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"\Acat\s+dog", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"\A(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" });

            //\G
            if (!RegexHelpers.IsNonBacktracking(engine)) // contiguous matches nor ECMAScript supported
            {
                yield return (enUS, @"\Gcat\s+dog", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"\Gcat\s+dog", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"\G(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
                yield return (enUS, @"\G(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
                yield return (enUS, @"\Gcat\s+dog", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"\G(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            }

            //^
            yield return (enUS, @"^cat\s+dog", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"^cat\s+dog", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"mouse\s\n^cat\s+dog", "mouse\n\ncat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "mouse\n\ncat   \n\n\n   dog" });
            yield return (enUS, @"^(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"^(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"(mouse)\s\n^(cat)\s+(dog)", "mouse\n\ncat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "mouse\n\ncat   \n\n\n   dog", "mouse", "cat", "dog" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"^cat\s+dog", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"^(cat)\s+(dog)", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            }

            //\Z
            yield return (enUS, @"cat\s+dog\Z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"cat\s+dog\Z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"cat\s+dog\Z", "cat   \n\n\n   dog\n", RegexOptions.None, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"cat\s+dog\Z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"cat\s+dog\Z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"cat\s+dog\Z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
                yield return (enUS, @"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            }

            //\z
            yield return (enUS, @"cat\s+dog\z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"cat\s+dog\z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog" });
            yield return (enUS, @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.None, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            yield return (enUS, @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.Multiline, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"cat\s+dog\z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog" });
                yield return (enUS, @"(cat)\s+(dog)\z", "cat   \n\n\n   dog", RegexOptions.ECMAScript, new string[] { "cat   \n\n\n   dog", "cat", "dog" });
            }

            //\b
            yield return (enUS, @"\bcat\b", "cat", RegexOptions.None, new string[] { "cat" });
            yield return (enUS, @"\bcat\b", "dog cat mouse", RegexOptions.None, new string[] { "cat" });
            yield return (enUS, @".*\bcat\b", "cat", RegexOptions.None, new string[] { "cat" });
            yield return (enUS, @".*\bcat\b", "dog cat mouse", RegexOptions.None, new string[] { "dog cat" });
            yield return (enUS, @"\b@cat", "123START123@catEND", RegexOptions.None, new string[] { "@cat" });
            yield return (enUS, @"\b\<cat", "123START123<catEND", RegexOptions.None, new string[] { "<cat" });
            yield return (enUS, @"\b,cat", "satwe,,,START,catEND", RegexOptions.None, new string[] { ",cat" });
            yield return (enUS, @"\b\[cat", "`12START123[catEND", RegexOptions.None, new string[] { "[cat" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"\bcat\b", "cat", RegexOptions.ECMAScript, new string[] { "cat" });
                yield return (enUS, @"\bcat\b", "dog cat mouse", RegexOptions.ECMAScript, new string[] { "cat" });
                yield return (enUS, @".*\bcat\b", "cat", RegexOptions.ECMAScript, new string[] { "cat" });
                yield return (enUS, @".*\bcat\b", "dog cat mouse", RegexOptions.ECMAScript, new string[] { "dog cat" });
            }

            //\B
            yield return (enUS, @"\Bcat\B", "dogcatmouse", RegexOptions.None, new string[] { "cat" });
            yield return (enUS, @"dog\Bcat\B", "dogcatmouse", RegexOptions.None, new string[] { "dogcat" });
            yield return (enUS, @".*\Bcat\B", "dogcatmouse", RegexOptions.None, new string[] { "dogcat" });
            yield return (enUS, @"\B@cat", "123START123;@catEND", RegexOptions.None, new string[] { "@cat" });
            yield return (enUS, @"\B\<cat", "123START123'<catEND", RegexOptions.None, new string[] { "<cat" });
            yield return (enUS, @"\B,cat", "satwe,,,START',catEND", RegexOptions.None, new string[] { ",cat" });
            yield return (enUS, @"\B\[cat", "`12START123'[catEND", RegexOptions.None, new string[] { "[cat" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"\Bcat\B", "dogcatmouse", RegexOptions.ECMAScript, new string[] { "cat" });
                yield return (enUS, @"dog\Bcat\B", "dogcatmouse", RegexOptions.ECMAScript, new string[] { "dogcat" });
                yield return (enUS, @".*\Bcat\B", "dogcatmouse", RegexOptions.ECMAScript, new string[] { "dogcat" });
            }

            // \w matching \p{Lm} (Letter, Modifier)
            yield return (enUS, @"\w+\s+\w+", "cat\u02b0 dog\u02b1", RegexOptions.None, new string[] { "cat\u02b0 dog\u02b1" });
            yield return (enUS, @"cat\w+\s+dog\w+", "STARTcat\u30FC dog\u3005END", RegexOptions.None, new string[] { "cat\u30FC dog\u3005END" });
            yield return (enUS, @"cat\w+\s+dog\w+", "STARTcat\uff9e dog\uff9fEND", RegexOptions.None, new string[] { "cat\uff9e dog\uff9fEND" });
            yield return (enUS, @"(\w+)\s+(\w+)", "cat\u02b0 dog\u02b1", RegexOptions.None, new string[] { "cat\u02b0 dog\u02b1", "cat\u02b0", "dog\u02b1" });
            yield return (enUS, @"(cat\w+)\s+(dog\w+)", "STARTcat\u30FC dog\u3005END", RegexOptions.None, new string[] { "cat\u30FC dog\u3005END", "cat\u30FC", "dog\u3005END" });
            yield return (enUS, @"(cat\w+)\s+(dog\w+)", "STARTcat\uff9e dog\uff9fEND", RegexOptions.None, new string[] { "cat\uff9e dog\uff9fEND", "cat\uff9e", "dog\uff9fEND" });

            // Positive and negative character classes [a-c]|[^b-c]
            yield return (enUS, @"[^a]|d", "d", RegexOptions.None, new string[] { "d" });
            yield return (enUS, @"([^a]|[d])*", "Hello Worlddf", RegexOptions.None, new string[] { "Hello Worlddf", "f" });
            yield return (enUS, @"([^{}]|\n)+", "{{{{Hello\n World \n}END", RegexOptions.None, new string[] { "Hello\n World \n", "\n" });
            yield return (enUS, @"([a-d]|[^abcd])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" });
            yield return (enUS, @"([^a]|[a])*", "once upon a time", RegexOptions.None, new string[] { "once upon a time", "e" });
            yield return (enUS, @"([a-d]|[^abcd]|[x-z]|^wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" });
            yield return (enUS, @"([a-d]|[e-i]|[^e]|wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", RegexOptions.None, new string[] { "\tonce\n upon\0 a- ()*&^%#time?", "?" });

            // Canonical and noncanonical char class, where one group is in it's
            // simplest form [a-e] and another is more complex.
            yield return (enUS, @"^(([^b]+ )|(.* ))$", "aaa ", RegexOptions.None, new string[] { "aaa ", "aaa ", "aaa ", "" });
            yield return (enUS, @"^(([^b]+ )|(.*))$", "aaa", RegexOptions.None, new string[] { "aaa", "aaa", "", "aaa" });
            yield return (enUS, @"^(([^b]+ )|(.* ))$", "bbb ", RegexOptions.None, new string[] { "bbb ", "bbb ", "", "bbb " });
            yield return (enUS, @"^(([^b]+ )|(.*))$", "bbb", RegexOptions.None, new string[] { "bbb", "bbb", "", "bbb" });
            yield return (enUS, @"^((a*)|(.*))$", "aaa", RegexOptions.None, new string[] { "aaa", "aaa", "aaa", "" });
            yield return (enUS, @"^((a*)|(.*))$", "aaabbb", RegexOptions.None, new string[] { "aaabbb", "aaabbb", "", "aaabbb" });

            yield return (enUS, @"(([0-9])|([a-z])|([A-Z]))*", "{hello 1234567890 world}", RegexOptions.None, new string[] { "", "", "", "", "" });
            yield return (enUS, @"(([0-9])|([a-z])|([A-Z]))+", "{hello 1234567890 world}", RegexOptions.None, new string[] { "hello", "o", "", "o", "" });
            yield return (enUS, @"(([0-9])|([a-z])|([A-Z]))*", "{HELLO 1234567890 world}", RegexOptions.None, new string[] { "", "", "", "", "" });
            yield return (enUS, @"(([0-9])|([a-z])|([A-Z]))+", "{HELLO 1234567890 world}", RegexOptions.None, new string[] { "HELLO", "O", "", "", "O" });
            yield return (enUS, @"(([0-9])|([a-z])|([A-Z]))*", "{1234567890 hello  world}", RegexOptions.None, new string[] { "", "", "", "", "" });
            yield return (enUS, @"(([0-9])|([a-z])|([A-Z]))+", "{1234567890 hello world}", RegexOptions.None, new string[] { "1234567890", "0", "0", "", "" });

            yield return (enUS, @"^(([a-d]*)|([a-z]*))$", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccdddeeefff", "aaabbbcccdddeeefff", "", "aaabbbcccdddeeefff" });
            yield return (enUS, @"^(([d-f]*)|([c-e]*))$", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "", "dddeeeccceee" });
            yield return (enUS, @"^(([c-e]*)|([d-f]*))$", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" });

            yield return (enUS, @"(([a-d]*)|([a-z]*))", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", "" });
            yield return (enUS, @"(([d-f]*)|([c-e]*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeee", "dddeee", "dddeee", "" });
            yield return (enUS, @"(([c-e]*)|([d-f]*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" });

            yield return (enUS, @"(([a-d]*)|(.*))", "aaabbbcccdddeeefff", RegexOptions.None, new string[] { "aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", "" });
            yield return (enUS, @"(([d-f]*)|(.*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeee", "dddeee", "dddeee", "" });
            yield return (enUS, @"(([c-e]*)|(.*))", "dddeeeccceee", RegexOptions.None, new string[] { "dddeeeccceee", "dddeeeccceee", "dddeeeccceee", "" });

            // \p{Pi} (Punctuation Initial quote) \p{Pf} (Punctuation Final quote)
            yield return (enUS, @"\p{Pi}(\w*)\p{Pf}", "\u00ABCat\u00BB   \u00BBDog\u00AB'", RegexOptions.None, new string[] { "\u00ABCat\u00BB", "Cat" });
            yield return (enUS, @"\p{Pi}(\w*)\p{Pf}", "\u2018Cat\u2019   \u2019Dog\u2018'", RegexOptions.None, new string[] { "\u2018Cat\u2019", "Cat" });

            // ECMAScript
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"(?<cat>cat)\s+(?<dog>dog)\s+\123\s+\234", "asdfcat   dog     cat23    dog34eia", RegexOptions.ECMAScript, new string[] { "cat   dog     cat23    dog34", "cat", "dog" });
            }

            // Balanced Matching
            if (!RegexHelpers.IsNonBacktracking(engine)) // balancing groups not supported
            {
                yield return (enUS, @"<div>
                (?>
                    <div>(?<DEPTH>) |
                    </div> (?<-DEPTH>) |
                    .?
                )*?
                (?(DEPTH)(?!))
                </div>", "<div>this is some <div>red</div> text</div></div></div>", RegexOptions.IgnorePatternWhitespace, new string[] { "<div>this is some <div>red</div> text</div>", "" });

                yield return (enUS, @"(
                ((?'open'<+)[^<>]*)+
                ((?'close-open'>+)[^<>]*)+
                )+", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "<02deep_03<03deep_03>>>", "<03deep_03", ">>>", "<", "03deep_03" });

                yield return (enUS, @"(
                (?<start><)?
                [^<>]?
                (?<end-start>>)?
                )*", "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "", "", "01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>" });

                yield return (enUS, @"(
                (?<start><[^/<>]*>)?
                [^<>]?
                (?<end-start></[^/<>]*>)?
                )*", "<b><a>Cat</a></b>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b><a>Cat</a></b>", "", "", "<a>Cat</a>" });

                yield return (enUS, @"(
                (?<start><(?<TagName>[^/<>]*)>)?
                [^<>]?
                (?<end-start></\k<TagName>>)?
                )*", "<b>cat</b><a>dog</a>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b>cat</b><a>dog</a>", "", "", "a", "dog" });

                // Balanced Matching With Backtracking
                yield return (enUS, @"(
                (?<start><[^/<>]*>)?
                .?
                (?<end-start></[^/<>]*>)?
                )*
                (?(start)(?!)) ", "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", RegexOptions.IgnorePatternWhitespace, new string[] { "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", "", "", "<a>Cat" });
            }

            // Character Classes and Lazy quantifier
            if (!RegexHelpers.IsNonBacktracking(engine)) // ECMAScript not supported
            {
                yield return (enUS, @"([0-9]+?)([\w]+?)", "55488aheiaheiad", RegexOptions.ECMAScript, new string[] { "55", "5", "5" });
                yield return (enUS, @"([0-9]+?)([a-z]+?)", "55488aheiaheiad", RegexOptions.ECMAScript, new string[] { "55488a", "55488", "a" });
            }

            // Miscellaneous/Regression scenarios
            if (!RegexHelpers.IsNonBacktracking(engine)) // lookarounds not supported
            {
                yield return (enUS, @"(?<openingtag>1)(?<content>.*?)(?=2)", "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine + "2", RegexOptions.Singleline | RegexOptions.ExplicitCapture,
                new string[] { "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine, "1", Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine });

                yield return (enUS, @"\G<%#(?<code>.*?)?%>", @"<%# DataBinder.Eval(this, ""MyNumber"") %>", RegexOptions.Singleline, new string[] { @"<%# DataBinder.Eval(this, ""MyNumber"") %>", @" DataBinder.Eval(this, ""MyNumber"") " });
            }

            // Nested Quantifiers
            yield return (enUS, @"^[abcd]{0,0x10}*$", "a{0,0x10}}}", RegexOptions.None, new string[] { "a{0,0x10}}}" });

            // Lazy operator Backtracking
            yield return (enUS, @"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/", "http://www.msn.com/", RegexOptions.IgnoreCase, new string[] { "http://www.msn.com/", "com", string.Empty });
            yield return (enUS, @"http://([a-zA-Z0-9\-]*\.?)*?/", @"http://www.google.com/", RegexOptions.IgnoreCase, new string[] { "http://www.google.com/", "com" });

            yield return (enUS, @"([a-z]*?)([\w])", "cat", RegexOptions.IgnoreCase, new string[] { "c", string.Empty, "c" });
            yield return (enUS, @"^([a-z]*?)([\w])$", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" });

            // Backtracking
            yield return (enUS, @"([a-z]*)([\w])", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" });
            yield return (enUS, @"^([a-z]*)([\w])$", "cat", RegexOptions.IgnoreCase, new string[] { "cat", "ca", "t" });

            // Backtracking with multiple (.*) groups -- important ASP.NET scenario
            yield return (enUS, @"(.*)/(.*).aspx", "/.aspx", RegexOptions.None, new string[] { "/.aspx", string.Empty, string.Empty });
            yield return (enUS, @"(.*)/(.*).aspx", "/homepage.aspx", RegexOptions.None, new string[] { "/homepage.aspx", string.Empty, "homepage" });
            yield return (enUS, @"(.*)/(.*).aspx", "pages/.aspx", RegexOptions.None, new string[] { "pages/.aspx", "pages", string.Empty });
            yield return (enUS, @"(.*)/(.*).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" });
            yield return (enUS, @"(.*)/(.*).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" });
            yield return (enUS, @"(.*)/(.*).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" });
            yield return (enUS, @"(.*)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" });
            yield return (enUS, @"(.*)/(.*)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" });

            // Backtracking with multiple (.+) groups
            yield return (enUS, @"(.+)/(.+).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" });
            yield return (enUS, @"(.+)/(.+).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" });
            yield return (enUS, @"(.+)/(.+).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" });
            yield return (enUS, @"(.+)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" });
            yield return (enUS, @"(.+)/(.+)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" });

            // Backtracking with (.+) group followed by (.*)
            yield return (enUS, @"(.+)/(.*).aspx", "pages/.aspx", RegexOptions.None, new string[] { "pages/.aspx", "pages", string.Empty });
            yield return (enUS, @"(.+)/(.*).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" });
            yield return (enUS, @"(.+)/(.*).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" });
            yield return (enUS, @"(.+)/(.*).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" });
            yield return (enUS, @"(.+)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" });
            yield return (enUS, @"(.+)/(.*)/(.*).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" });

            // Backtracking with (.*) group followed by (.+)
            yield return (enUS, @"(.*)/(.+).aspx", "/homepage.aspx", RegexOptions.None, new string[] { "/homepage.aspx", string.Empty, "homepage" });
            yield return (enUS, @"(.*)/(.+).aspx", "pages/homepage.aspx", RegexOptions.None, new string[] { "pages/homepage.aspx", "pages", "homepage" });
            yield return (enUS, @"(.*)/(.+).aspx", "/pages/homepage.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx", "/pages", "homepage" });
            yield return (enUS, @"(.*)/(.+).aspx", "/pages/homepage/index.aspx", RegexOptions.None, new string[] { "/pages/homepage/index.aspx", "/pages/homepage", "index" });
            yield return (enUS, @"(.*)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages/homepage.aspx", "index" });
            yield return (enUS, @"(.*)/(.+)/(.+).aspx", "/pages/homepage.aspx/index.aspx", RegexOptions.None, new string[] { "/pages/homepage.aspx/index.aspx", "/pages", "homepage.aspx", "index" });

            // Captures inside varying constructs with backtracking needing to uncapture
            yield return (enUS, @"a(bc)d|abc(e)", "abce", RegexOptions.None, new string[] { "abce", "", "e" }); // alternation
            yield return (enUS, @"((ab){2}cd)*", "ababcdababcdababc", RegexOptions.None, new string[] { "ababcdababcd", "ababcd", "ab" }); // loop
            if (!RegexHelpers.IsNonBacktracking(engine)) // lookarounds not supported)
            {
                yield return (enUS, @"(ab(?=(\w)\w))*a", "aba", RegexOptions.None, new string[] { "a", "", "" }); // positive lookahead in a loop
                yield return (enUS, @"(ab(?=(\w)\w))*a", "ababa", RegexOptions.None, new string[] { "aba", "ab", "a" }); // positive lookahead in a loop
                yield return (enUS, @"(ab(?=(\w)\w))*a", "abababa", RegexOptions.None, new string[] { "ababa", "ab", "a" }); // positive lookahead in a loop
                yield return (enUS, @"\w\w(?!(\d)\d)", "aa..", RegexOptions.None, new string[] { "aa", "" }); // negative lookahead
                yield return (enUS, @"\w\w(?!(\d)\d)", "aa.3", RegexOptions.None, new string[] { "aa", "" }); // negative lookahead
            }

            // Quantifiers
            yield return (enUS, @"a*", "", RegexOptions.None, new string[] { "" });
            yield return (enUS, @"a*", "a", RegexOptions.None, new string[] { "a" });
            yield return (enUS, @"a*", "aa", RegexOptions.None, new string[] { "aa" });
            yield return (enUS, @"a*", "aaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"a*?", "", RegexOptions.None, new string[] { "" });
            yield return (enUS, @"a*?", "a", RegexOptions.None, new string[] { "" });
            yield return (enUS, @"a*?", "aa", RegexOptions.None, new string[] { "" });
            yield return (enUS, @"a+?", "aa", RegexOptions.None, new string[] { "a" });
            yield return (enUS, @"a{1,", "a{1,", RegexOptions.None, new string[] { "a{1," });
            yield return (enUS, @"a{1,3}", "aaaaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"a{1,3}?", "aaaaa", RegexOptions.None, new string[] { "a" });
            yield return (enUS, @"a{2,2}", "aaaaa", RegexOptions.None, new string[] { "aa" });
            yield return (enUS, @"a{2,2}?", "aaaaa", RegexOptions.None, new string[] { "aa" });
            yield return (enUS, @".{1,3}", "bb\nba", RegexOptions.None, new string[] { "bb" });
            yield return (enUS, @".{1,3}?", "bb\nba", RegexOptions.None, new string[] { "b" });
            yield return (enUS, @".{2,2}", "bbb\nba", RegexOptions.None, new string[] { "bb" });
            yield return (enUS, @".{2,2}?", "bbb\nba", RegexOptions.None, new string[] { "bb" });
            yield return (enUS, @"[abc]{1,3}", "ccaba", RegexOptions.None, new string[] { "cca" });
            yield return (enUS, @"[abc]{1,3}?", "ccaba", RegexOptions.None, new string[] { "c" });
            yield return (enUS, @"[abc]{2,2}", "ccaba", RegexOptions.None, new string[] { "cc" });
            yield return (enUS, @"[abc]{2,2}?", "ccaba", RegexOptions.None, new string[] { "cc" });
            yield return (enUS, @"(?:[abc]def){1,3}xyz", "cdefxyz", RegexOptions.None, new string[] { "cdefxyz" });
            yield return (enUS, @"(?:[abc]def){1,3}xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "adefbdefcdefxyz" });
            yield return (enUS, @"(?:[abc]def){1,3}?xyz", "cdefxyz", RegexOptions.None, new string[] { "cdefxyz" });
            yield return (enUS, @"(?:[abc]def){1,3}?xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "adefbdefcdefxyz" });
            yield return (enUS, @"(?:[abc]def){2,2}xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "bdefcdefxyz" });
            yield return (enUS, @"(?:[abc]def){2,2}?xyz", "adefbdefcdefxyz", RegexOptions.None, new string[] { "bdefcdefxyz" });
            foreach (string prefix in new[] { "", "xyz" })
            {
                yield return (enUS, prefix + @"(?:[abc]def){1,3}", prefix + "cdef", RegexOptions.None, new string[] { prefix + "cdef" });
                yield return (enUS, prefix + @"(?:[abc]def){1,3}", prefix + "cdefadefbdef", RegexOptions.None, new string[] { prefix + "cdefadefbdef" });
                yield return (enUS, prefix + @"(?:[abc]def){1,3}", prefix + "cdefadefbdefadef", RegexOptions.None, new string[] { prefix + "cdefadefbdef" });
                yield return (enUS, prefix + @"(?:[abc]def){1,3}?", prefix + "cdef", RegexOptions.None, new string[] { prefix + "cdef" });
                yield return (enUS, prefix + @"(?:[abc]def){1,3}?", prefix + "cdefadefbdef", RegexOptions.None, new string[] { prefix + "cdef" });
                yield return (enUS, prefix + @"(?:[abc]def){2,2}", prefix + "cdefadefbdefadef", RegexOptions.None, new string[] { prefix + "cdefadef" });
                yield return (enUS, prefix + @"(?:[abc]def){2,2}?", prefix + "cdefadefbdefadef", RegexOptions.None, new string[] { prefix + "cdefadef" });
            }
            yield return (enUS, @"(cat){", "cat{", RegexOptions.None, new string[] { "cat{", "cat" });
            yield return (enUS, @"(cat){}", "cat{}", RegexOptions.None, new string[] { "cat{}", "cat" });
            yield return (enUS, @"(cat){,", "cat{,", RegexOptions.None, new string[] { "cat{,", "cat" });
            yield return (enUS, @"(cat){,}", "cat{,}", RegexOptions.None, new string[] { "cat{,}", "cat" });
            yield return (enUS, @"(cat){cat}", "cat{cat}", RegexOptions.None, new string[] { "cat{cat}", "cat" });
            yield return (enUS, @"(cat){cat,5}", "cat{cat,5}", RegexOptions.None, new string[] { "cat{cat,5}", "cat" });
            yield return (enUS, @"(cat){5,dog}", "cat{5,dog}", RegexOptions.None, new string[] { "cat{5,dog}", "cat" });
            yield return (enUS, @"(cat){cat,dog}", "cat{cat,dog}", RegexOptions.None, new string[] { "cat{cat,dog}", "cat" });
            yield return (enUS, @"(cat){,}?", "cat{,}?", RegexOptions.None, new string[] { "cat{,}", "cat" });
            yield return (enUS, @"(cat){cat}?", "cat{cat}?", RegexOptions.None, new string[] { "cat{cat}", "cat" });
            yield return (enUS, @"(cat){cat,5}?", "cat{cat,5}?", RegexOptions.None, new string[] { "cat{cat,5}", "cat" });
            yield return (enUS, @"(cat){5,dog}?", "cat{5,dog}?", RegexOptions.None, new string[] { "cat{5,dog}", "cat" });
            yield return (enUS, @"(cat){cat,dog}?", "cat{cat,dog}?", RegexOptions.None, new string[] { "cat{cat,dog}", "cat" });

            // Atomic subexpressions
            // Implicitly upgrading (or not) oneloop to be atomic
            yield return (enUS, @"a*b", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*b+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*b+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*[^a]", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*[^a]+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*[^a]+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*bcd", "aaabcd", RegexOptions.None, new string[] { "aaabcd" });
            yield return (enUS, @"a*[bcd]", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*[bcd]+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*[bcd]+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*[bcd]{1,3}", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"a*([bcd]ab|[bef]cd){1,3}", "aaababecdcac", RegexOptions.ExplicitCapture, new string[] { "aaababecd" });
            yield return (enUS, @"a*([bcd]|[aef]){1,3}", "befb", RegexOptions.ExplicitCapture, new string[] { "bef" }); // can't upgrade
            yield return (enUS, @"a*$", "aaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"a*$", "aaa", RegexOptions.Multiline, new string[] { "aaa" });
            yield return (enUS, @"a*\b", "aaa bbb", RegexOptions.None, new string[] { "aaa" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // atomic nor ECMAScript supported
            {
                yield return (enUS, @"a*(?>b+)", "aaab", RegexOptions.None, new string[] { "aaab" });
                yield return (enUS, @"a*(?>[^a]+)", "aaab", RegexOptions.None, new string[] { "aaab" });
                yield return (enUS, @"a*(?>[bcd]+)", "aaab", RegexOptions.None, new string[] { "aaab" });
                yield return (enUS, @"a*\b", "aaa bbb", RegexOptions.ECMAScript, new string[] { "aaa" });
                yield return (enUS, @"@*\B", "@@@", RegexOptions.ECMAScript, new string[] { "@@@" });
            }
            yield return (enUS, @"@*\B", "@@@", RegexOptions.None, new string[] { "@@@" });
            yield return (enUS, @"(?:abcd*|efgh)i", "efghi", RegexOptions.None, new string[] { "efghi" });
            yield return (enUS, @"(?:abcd|efgh*)i", "efgi", RegexOptions.None, new string[] { "efgi" });
            yield return (enUS, @"(?:abcd|efghj{2,}|j[klm]o+)i", "efghjjjjji", RegexOptions.None, new string[] { "efghjjjjji" });
            yield return (enUS, @"(?:abcd|efghi{2,}|j[klm]o+)i", "efghiii", RegexOptions.None, new string[] { "efghiii" });
            yield return (enUS, @"(?:abcd|efghi{2,}|j[klm]o+)i", "efghiiiiiiii", RegexOptions.None, new string[] { "efghiiiiiiii" });
            yield return (enUS, @"a?ba?ba?ba?b", "abbabab", RegexOptions.None, new string[] { "abbabab" });
            yield return (enUS, @"a?ba?ba?ba?b", "abBAbab", RegexOptions.IgnoreCase, new string[] { "abBAbab" });
            // Implicitly upgrading (or not) notoneloop to be atomic
            yield return (enUS, @"[^b]*b", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[^b]*b+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[^b]*b+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // atomic not supported
            {
                yield return (enUS, @"[^b]*(?>b+)", "aaab", RegexOptions.None, new string[] { "aaab" });
            }
            yield return (enUS, @"[^b]*bac", "aaabac", RegexOptions.None, new string[] { "aaabac" });
            yield return (enUS, @"[^b]*", "aaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"(?:abc[^b]*|efgh)i", "efghi", RegexOptions.None, new string[] { "efghi" }); // can't upgrade
            yield return (enUS, @"(?:abcd|efg[^b]*)b", "efgb", RegexOptions.None, new string[] { "efgb" });
            yield return (enUS, @"(?:abcd|efg[^b]*)i", "efgi", RegexOptions.None, new string[] { "efgi" }); // can't upgrade
            yield return (enUS, @"[^a]?a[^a]?a[^a]?a[^a]?a", "baababa", RegexOptions.None, new string[] { "baababa" });
            yield return (enUS, @"[^a]?a[^a]?a[^a]?a[^a]?a", "BAababa", RegexOptions.IgnoreCase, new string[] { "BAababa" });
            // Implicitly upgrading (or not) setloop to be atomic
            yield return (enUS, @"[ac]*", "aaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"[ac]*b", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*b+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*b+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*[^a]", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*[^a]+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*[^a]+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*bcd", "aaabcd", RegexOptions.None, new string[] { "aaabcd" });
            yield return (enUS, @"[ac]*[bd]", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*[bd]+", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*[bd]+?", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*[bd]{1,3}", "aaab", RegexOptions.None, new string[] { "aaab" });
            yield return (enUS, @"[ac]*$", "aaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"[ac]*$", "aaa", RegexOptions.Multiline, new string[] { "aaa" });
            yield return (enUS, @"[ac]*\b", "aaa bbb", RegexOptions.None, new string[] { "aaa" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // atomic nor ECMAScript are supported
            {
                yield return (enUS, @"[ac]*(?>b+)", "aaab", RegexOptions.None, new string[] { "aaab" });
                yield return (enUS, @"[ac]*(?>[^a]+)", "aaab", RegexOptions.None, new string[] { "aaab" });
                yield return (enUS, @"[ac]*(?>[bd]+)", "aaab", RegexOptions.None, new string[] { "aaab" });
                yield return (enUS, @"[ac]*\b", "aaa bbb", RegexOptions.ECMAScript, new string[] { "aaa" });
                yield return (enUS, @"[@']*\B", "@@@", RegexOptions.ECMAScript, new string[] { "@@@" });
            }
            yield return (enUS, @"[@']*\B", "@@@", RegexOptions.None, new string[] { "@@@" });
            yield return (enUS, @".*.", "@@@", RegexOptions.Singleline, new string[] { "@@@" });
            yield return (enUS, @"(?:abcd|efg[hij]*)h", "efgh", RegexOptions.None, new string[] { "efgh" }); // can't upgrade
            yield return (enUS, @"(?:abcd|efg[hij]*)ih", "efgjih", RegexOptions.None, new string[] { "efgjih" }); // can't upgrade
            yield return (enUS, @"(?:abcd|efg[hij]*)k", "efgjk", RegexOptions.None, new string[] { "efgjk" });
            yield return (enUS, @"[ace]?b[ace]?b[ace]?b[ace]?b", "cbbabeb", RegexOptions.None, new string[] { "cbbabeb" });
            yield return (enUS, @"[ace]?b[ace]?b[ace]?b[ace]?b", "cBbAbEb", RegexOptions.IgnoreCase, new string[] { "cBbAbEb" });
            yield return (enUS, @"a[^wz]*w", "abcdcdcdwz", RegexOptions.None, new string[] { "abcdcdcdw" });
            yield return (enUS, @"a[^wyz]*w", "abcdcdcdwz", RegexOptions.None, new string[] { "abcdcdcdw" });
            yield return (enUS, @"a[^wyz]*W", "abcdcdcdWz", RegexOptions.IgnoreCase, new string[] { "abcdcdcdW" });
            // Implicitly upgrading (or not) concat loops to be atomic
            yield return (enUS, @"(?:[ab]c[de]f)*", "", RegexOptions.None, new string[] { "" });
            yield return (enUS, @"(?:[ab]c[de]f)*", "acdf", RegexOptions.None, new string[] { "acdf" });
            yield return (enUS, @"(?:[ab]c[de]f)*", "acdfbcef", RegexOptions.None, new string[] { "acdfbcef" });
            yield return (enUS, @"(?:[ab]c[de]f)*", "cdfbcef", RegexOptions.None, new string[] { "" });
            yield return (enUS, @"(?:[ab]c[de]f)+", "cdfbcef", RegexOptions.None, new string[] { "bcef" });
            yield return (enUS, @"(?:[ab]c[de]f)*", "bcefbcdfacfe", RegexOptions.None, new string[] { "bcefbcdf" });
            // Implicitly upgrading (or not) nested loops to be atomic
            yield return (enUS, @"(?:a){3}", "aaaaaaaaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"(?:a){3}?", "aaaaaaaaa", RegexOptions.None, new string[] { "aaa" });
            yield return (enUS, @"(?:a{2}){3}", "aaaaaaaaa", RegexOptions.None, new string[] { "aaaaaa" });
            yield return (enUS, @"(?:a{2}?){3}?", "aaaaaaaaa", RegexOptions.None, new string[] { "aaaaaa" });
            yield return (enUS, @"(?:(?:[ab]c[de]f){3}){2}", "acdfbcdfacefbcefbcefbcdfacdef", RegexOptions.None, new string[] { "acdfbcdfacefbcefbcefbcdf" });
            yield return (enUS, @"(?:(?:[ab]c[de]f){3}hello){2}", "aaaaaacdfbcdfacefhellobcefbcefbcdfhellooooo", RegexOptions.None, new string[] { "acdfbcdfacefhellobcefbcefbcdfhello" });
            yield return (enUS, @"CN=(.*[^,]+).*", "CN=localhost", RegexOptions.Singleline, new string[] { "CN=localhost", "localhost" });
            // Nested atomic
            if (!RegexHelpers.IsNonBacktracking(engine)) // atomic not supported
            {
                yield return (enUS, @"(?>abc[def]gh(i*))", "123abceghiii456", RegexOptions.None, new string[] { "abceghiii", "iii" });
                yield return (enUS, @"(?>(?:abc)*)", "abcabcabc", RegexOptions.None, new string[] { "abcabcabc" });
            }

            // Anchoring loops beginning with .* / .+
            yield return (enUS, @".*", "", RegexOptions.None, new string[] { "" });
            yield return (enUS, @".*", "\n\n\n\n", RegexOptions.None, new string[] { "" });
            yield return (enUS, @".*", "\n\n\n\n", RegexOptions.Singleline, new string[] { "\n\n\n\n" });
            yield return (enUS, @".*[1a]", "\n\n\n\n1", RegexOptions.None, new string[] { "1" });
            yield return (enUS, @"(?s).*(?-s)[1a]", "1\n\n\n\n", RegexOptions.None, new string[] { "1" });
            yield return (enUS, @"(?s).*(?-s)[1a]", "\n\n\n\n1", RegexOptions.None, new string[] { "\n\n\n\n1" });
            yield return (enUS, @".*|.*|.*", "", RegexOptions.None, new string[] { "" });
            yield return (enUS, @".*123|abc", "abc\n123", RegexOptions.None, new string[] { "abc" });
            yield return (enUS, @".*123|abc", "abc\n123", RegexOptions.Singleline, new string[] { "abc\n123" });
            yield return (enUS, @"abc|.*123", "abc\n123", RegexOptions.Singleline, new string[] { "abc" });
            yield return (enUS, @".*", "\n", RegexOptions.None, new string[] { "" });
            yield return (enUS, @".*\n", "\n", RegexOptions.None, new string[] { "\n" });
            yield return (enUS, @".*", "\n", RegexOptions.Singleline, new string[] { "\n" });
            yield return (enUS, @".*\n", "\n", RegexOptions.Singleline, new string[] { "\n" });
            yield return (enUS, @".*", "abc", RegexOptions.None, new string[] { "abc" });
            yield return (enUS, @".*abc", "abc", RegexOptions.None, new string[] { "abc" });
            yield return (enUS, @".*abc|ghi", "ghi", RegexOptions.None, new string[] { "ghi" });
            yield return (enUS, @".*abc|.*ghi", "abcghi", RegexOptions.None, new string[] { "abc" });
            yield return (enUS, @".*ghi|.*abc", "abcghi", RegexOptions.None, new string[] { "abcghi" });
            yield return (enUS, @".*abc|.*ghi", "bcghi", RegexOptions.None, new string[] { "bcghi" });
            yield return (enUS, @".*abc|.+c", " \n   \n   bc", RegexOptions.None, new string[] { "   bc" });
            yield return (enUS, @".*abc", "12345 abc", RegexOptions.None, new string[] { "12345 abc" });
            yield return (enUS, @".*abc", "12345\n abc", RegexOptions.None, new string[] { " abc" });
            yield return (enUS, @".*abc", "12345\n abc", RegexOptions.Singleline, new string[] { "12345\n abc" });
            yield return (enUS, @".*\nabc", "\n123\nabc", RegexOptions.None, new string[] { "123\nabc" });
            yield return (enUS, @".*\nabc", "\n123\nabc", RegexOptions.Singleline, new string[] { "\n123\nabc" });
            yield return (enUS, @".*abc", "abc abc abc \nabc", RegexOptions.None, new string[] { "abc abc abc" });
            yield return (enUS, @".*abc", "abc abc abc \nabc", RegexOptions.Singleline, new string[] { "abc abc abc \nabc" });
            yield return (enUS, @".*?abc", "abc abc abc \nabc", RegexOptions.None, new string[] { "abc" });
            yield return (enUS, @"[^\n]*abc", "123abc\n456abc\n789abc", RegexOptions.None, new string[] { "123abc" });
            yield return (enUS, @"[^\n]*abc", "123abc\n456abc\n789abc", RegexOptions.Singleline, new string[] { "123abc" });
            yield return (enUS, @"[^\n]*abc", "123ab\n456abc\n789abc", RegexOptions.None, new string[] { "456abc" });
            yield return (enUS, @"[^\n]*abc", "123ab\n456abc\n789abc", RegexOptions.Singleline, new string[] { "456abc" });
            yield return (enUS, @".+", "a", RegexOptions.None, new string[] { "a" });
            yield return (enUS, @".+", "\nabc", RegexOptions.None, new string[] { "abc" });
            yield return (enUS, @".+", "\n", RegexOptions.Singleline, new string[] { "\n" });
            yield return (enUS, @".+", "\nabc", RegexOptions.Singleline, new string[] { "\nabc" });
            yield return (enUS, @".+abc", "aaaabc", RegexOptions.None, new string[] { "aaaabc" });
            yield return (enUS, @".+abc", "12345 abc", RegexOptions.None, new string[] { "12345 abc" });
            yield return (enUS, @".+abc", "12345\n abc", RegexOptions.None, new string[] { " abc" });
            yield return (enUS, @".+abc", "12345\n abc", RegexOptions.Singleline, new string[] { "12345\n abc" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // backreferences not supported
            {
                yield return (enUS, @"(.*)abc\1", "\n12345abc12345", RegexOptions.Singleline, new string[] { "12345abc12345", "12345" });
                yield return (enUS, @"(.+)abc\1", "\n12345abc12345", RegexOptions.Singleline, new string[] { "12345abc12345", "12345" });
            }

            // Unanchored .*
            yield return (enUS, @"\A\s*(?<name>\w+)(\s*\((?<arguments>.*)\))?\s*\Z", "Match(Name)", RegexOptions.None, new string[] { "Match(Name)", "(Name)", "Match", "Name" });
            yield return (enUS, @"\A\s*(?<name>\w+)(\s*\((?<arguments>.*)\))?\s*\Z", "Match(Na\nme)", RegexOptions.Singleline, new string[] { "Match(Na\nme)", "(Na\nme)", "Match", "Na\nme" });
            foreach (RegexOptions options in new[] { RegexOptions.None, RegexOptions.Singleline })
            {
                yield return (enUS, @"abcd.*", @"abcabcd", options, new string[] { "abcd" });
                yield return (enUS, @"abcd.*", @"abcabcde", options, new string[] { "abcde" });
                yield return (enUS, @"abcd.*", @"abcabcdefg", options, new string[] { "abcdefg" });
                yield return (enUS, @"abcd(.*)", @"ababcd", options, new string[] { "abcd", "" });
                yield return (enUS, @"abcd(.*)", @"aabcde", options, new string[] { "abcde", "e" });
                yield return (enUS, @"abcd(.*)", @"abcabcdefg", options, new string[] { "abcdefg", "efg" });
                yield return (enUS, @"abcd(.*)e", @"abcabcdefg", options, new string[] { "abcde", "" });
                yield return (enUS, @"abcd(.*)f", @"abcabcdefg", options, new string[] { "abcdef", "e" });
            }

            // Grouping Constructs
            yield return (enUS, @"()", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty });
            yield return (enUS, @"(?<cat>)", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty });
            yield return (enUS, @"(?'cat')", "cat", RegexOptions.None, new string[] { string.Empty, string.Empty });
            yield return (enUS, @"(?:)", "cat", RegexOptions.None, new string[] { string.Empty });
            yield return (enUS, @"(?imn)", "cat", RegexOptions.None, new string[] { string.Empty });
            yield return (enUS, @"(?imn)cat", "(?imn)cat", RegexOptions.None, new string[] { "cat" });
            yield return (enUS, @"(?=)", "cat", RegexOptions.None, new string[] { string.Empty });
            yield return (enUS, @"(?<=)", "cat", RegexOptions.None, new string[] { string.Empty });
            if (!RegexHelpers.IsNonBacktracking(engine)) // atomic not supported
            {
                yield return (enUS, @"(?>)", "cat", RegexOptions.None, new string[] { string.Empty });
            }

            // Alternation construct
            if (!RegexHelpers.IsNonBacktracking(engine)) // conditionals not supported
            {
                yield return (enUS, @"(?()|)", "(?()|)", RegexOptions.None, new string[] { "" });

                yield return (enUS, @"(?(cat)|)", "cat", RegexOptions.None, new string[] { "" });
                yield return (enUS, @"(?(cat)|)", "dog", RegexOptions.None, new string[] { "" });

                yield return (enUS, @"(?(cat)catdog|)", "catdog", RegexOptions.None, new string[] { "catdog" });
                yield return (enUS, @"(?(cat)cat\w\w\w)*", "catdogcathog", RegexOptions.None, new string[] { "catdogcathog" });
                yield return (enUS, @"(?(?=cat)cat\w\w\w)*", "catdogcathog", RegexOptions.None, new string[] { "catdogcathog" });
                yield return (enUS, @"(?(cat)catdog|)", "dog", RegexOptions.None, new string[] { "" });
                yield return (enUS, @"(?(cat)dog|)", "dog", RegexOptions.None, new string[] { "" });
                yield return (enUS, @"(?(cat)dog|)", "cat", RegexOptions.None, new string[] { "" });

                yield return (enUS, @"(?(cat)|catdog)", "cat", RegexOptions.None, new string[] { "" });
                yield return (enUS, @"(?(cat)|catdog)", "catdog", RegexOptions.None, new string[] { "" });
                yield return (enUS, @"(?(cat)|dog)", "dog", RegexOptions.None, new string[] { "dog" });

                yield return (enUS, @"(?((\w{3}))\1\1|no)", "dogdogdog", RegexOptions.None, new string[] { "dogdog", "dog" });
                yield return (enUS, @"(?((\w{3}))\1\1|no)", "no", RegexOptions.None, new string[] { "no", "" });
            }

            // Special cases involving starting position search optimizations
            yield return (enUS, @"(\d*)(hello)(\d*)", "123hello456", RegexOptions.None, new string[] { "123hello456", "123", "hello", "456" });
            yield return (enUS, @"((\d*))[AaBbCc](\d*)", "1b", RegexOptions.None, new string[] { "1b", "1", "1", "" });
            yield return (enUS, @"((\d*))[AaBbCc](\d*)", "b1", RegexOptions.None, new string[] { "b1", "", "", "1" });
            yield return (enUS, @"(\w*)(hello)(\w*)", "hello", RegexOptions.None, new string[] { "hello", "", "hello", "" });
            if (!RegexHelpers.IsNonBacktracking(engine)) // atomic not supported
            {
                yield return (enUS, @"(?>(\d*))(hello)(\d*)", "123hello456", RegexOptions.None, new string[] { "123hello456", "123", "hello", "456" });
                yield return (enUS, @"((?>\d*))(hello)(\d*)", "123hello456", RegexOptions.None, new string[] { "123hello456", "123", "hello", "456" });
            }

            // Invalid unicode
            yield return (enUS, "([\u0000-\uFFFF-[azAZ09]]|[\u0000-\uFFFF-[^azAZ09]])+", "azAZBCDE1234567890BCDEFAZza", RegexOptions.None, new string[] { "azAZBCDE1234567890BCDEFAZza", "a" });
            yield return (enUS, "[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]+", "abcxyzABCXYZ123890", RegexOptions.None, new string[] { "bcxyzABCXYZ123890" });
            yield return (enUS, "[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]]+", "bcxyzABCXYZ123890a", RegexOptions.None, new string[] { "a" });
            yield return (enUS, "[\u0000-\uFFFF-[\\p{P}\\p{S}\\p{C}]]+", "!@`';.,$+<>=\x0001\x001FazAZ09", RegexOptions.None, new string[] { "azAZ09" });

            yield return (enUS, @"[\uFFFD-\uFFFF]+", "\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase, new string[] { "\uFFFD\uFFFE\uFFFF" });
            yield return (enUS, @"[\uFFFC-\uFFFE]+", "\uFFFB\uFFFC\uFFFD\uFFFE\uFFFF", RegexOptions.IgnoreCase, new string[] { "\uFFFC\uFFFD\uFFFE" });

            // Empty Match
            yield return (enUS, @"([a*]*)+?$", "ab", RegexOptions.None, new string[] { "", "" });
            yield return (enUS, @"(a*)+?$", "b", RegexOptions.None, new string[] { "", "" });

            // en-US
            yield return (enUS, "CH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" });
            yield return (enUS, "cH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" });
            yield return (enUS, "AA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" });
            yield return (enUS, "aA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" });
            yield return (enUS, "\u0130", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" });
            yield return (enUS, "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" });

            // cs-CZ
            yield return (csCZ, "CH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" });
            yield return (csCZ, "cH", "Ch", RegexOptions.IgnoreCase, new string[] { "Ch" });

            // da-DK
            yield return (daDK, "AA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" });
            yield return (daDK, "aA", "Aa", RegexOptions.IgnoreCase, new string[] { "Aa" });

            // tr-TR
            yield return (trTR, "\u0131", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" });
            yield return (trTR, "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" });

            // az-Latn-AZ
            if (PlatformDetection.IsNotBrowser)
            {
                yield return (azLatnAZ, "\u0131", "\u0049", RegexOptions.IgnoreCase, new string[] { "\u0049" });
                yield return (azLatnAZ, "\u0130", "\u0069", RegexOptions.IgnoreCase, new string[] { "\u0069" });
            }
        }

        [Theory]
        [MemberData(nameof(Groups_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56407", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36900", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Groups(Regex regex, CultureInfo culture, string input, string[] expectedGroups)
        {
            using (new ThreadCultureChange(culture))
            {
                foreach (string prefix in new[] { "", "IGNORETHIS" })
                {
                    Match match = prefix.Length == 0 ?
                        regex.Match(input) : // validate the original input
                        regex.Match(prefix + input, prefix.Length, input.Length); // validate we handle groups and beginning/length correctly

                    Assert.True(match.Success);
                    Assert.Equal(expectedGroups[0], match.Value);
                    Assert.Equal(expectedGroups.Length, match.Groups.Count);

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
