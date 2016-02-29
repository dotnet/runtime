// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class CharacterClassSubtraction
{
    [Fact]
    public static void CharacterClassSubtractionTestCase()
    {
        for (int i = 0; i < s_regexTests.Length; i++)
        {
            Assert.True(s_regexTests[i].Run());
        }
    }

    private static RegexTestCase[] s_regexTests = new RegexTestCase[] {
        /****************************************************************************
        (A - B) B is a subset of A (ie B only contains chars that are in A)
        *****************************************************************************/
        new RegexTestCase(@"[abcd-[d]]+", "dddaabbccddd", "aabbcc"),

         new RegexTestCase(@"[\d-[357]]+", "33312468955","124689"),
         new RegexTestCase(@"[\d-[357]]+", "51246897","124689"),
         new RegexTestCase(@"[\d-[357]]+", "3312468977","124689"),

         new RegexTestCase(@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy","aaaABCD09zzz"),

         new RegexTestCase(@"[\w-[\d]]+", "0AZaz9","AZaz"),
         new RegexTestCase(@"[\w-[\p{Ll}]]+", "a09AZz","09AZ"),

        new RegexTestCase(@"[\d-[13579]]+", RegexOptions.ECMAScript, "1024689", "02468"),
        new RegexTestCase(@"[\d-[13579]]+", RegexOptions.ECMAScript, "\x066102468\x0660", "02468"),
        new RegexTestCase(@"[\d-[13579]]+", "\x066102468\x0660", "\x066102468\x0660"),

         new RegexTestCase(@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy","aaaABCD09zzz"),

        new RegexTestCase(@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy","aaaABCD09zzz"),
        new RegexTestCase(@"[\w-[b-y]]+", "bbbaaaABCD09zzzyyy","aaaABCD09zzz"),

        new RegexTestCase(@"[\p{Ll}-[ae-z]]+", "aaabbbcccdddeee","bbbcccddd"),
        new RegexTestCase(@"[\p{Nd}-[2468]]+", "20135798","013579"),

        new RegexTestCase(@"[\P{Lu}-[ae-z]]+", "aaabbbcccdddeee","bbbcccddd"),
        new RegexTestCase(@"[\P{Nd}-[\p{Ll}]]+", "az09AZ'[]","AZ'[]"),

        /****************************************************************************
        (A - B) B is a superset of A (ie B contains chars that are in A plus other chars that are not in A)
        *****************************************************************************/
        new RegexTestCase(@"[abcd-[def]]+", "fedddaabbccddd", "aabbcc"),

         new RegexTestCase(@"[\d-[357a-z]]+", "az33312468955","124689"),
         new RegexTestCase(@"[\d-[de357fgA-Z]]+", "AZ51246897","124689"),
         new RegexTestCase(@"[\d-[357\p{Ll}]]+", "az3312468977","124689"),

         new RegexTestCase(@"[\w-[b-y\s]]+", " \tbbbaaaABCD09zzzyyy","aaaABCD09zzz"),

         new RegexTestCase(@"[\w-[\d\p{Po}]]+", "!#0AZaz9","AZaz"),
         new RegexTestCase(@"[\w-[\p{Ll}\s]]+", "a09AZz","09AZ"),

        new RegexTestCase(@"[\d-[13579a-zA-Z]]+", RegexOptions.ECMAScript, "AZ1024689", "02468"),
        new RegexTestCase(@"[\d-[13579abcd]]+", RegexOptions.ECMAScript, "abcd\x066102468\x0660", "02468"),
        new RegexTestCase(@"[\d-[13579\s]]+", " \t\x066102468\x0660", "\x066102468\x0660"),

         new RegexTestCase(@"[\w-[b-y\p{Po}]]+", "!#bbbaaaABCD09zzzyyy","aaaABCD09zzz"),

        new RegexTestCase(@"[\w-[b-y!.,]]+", "!.,bbbaaaABCD09zzzyyy","aaaABCD09zzz"),
        new RegexTestCase("[\\w-[b-y\x00-\x0F]]+", "\0bbbaaaABCD09zzzyyy","aaaABCD09zzz"),

        new RegexTestCase(@"[\p{Ll}-[ae-z0-9]]+", "09aaabbbcccdddeee","bbbcccddd"),
        new RegexTestCase(@"[\p{Nd}-[2468az]]+", "az20135798","013579"),

        new RegexTestCase(@"[\P{Lu}-[ae-zA-Z]]+", "AZaaabbbcccdddeee","bbbcccddd"),
        new RegexTestCase(@"[\P{Nd}-[\p{Ll}0123456789]]+", "09az09AZ'[]","AZ'[]"),

        /****************************************************************************
        (A - B) B only contains chars that are not in A
        *****************************************************************************/
        new RegexTestCase(@"[abc-[defg]]+", "dddaabbccddd", "aabbcc"),

         new RegexTestCase(@"[\d-[abc]]+", "abc09abc","09"),
         new RegexTestCase(@"[\d-[a-zA-Z]]+", "az09AZ","09"),
         new RegexTestCase(@"[\d-[\p{Ll}]]+", "az09az","09"),

         new RegexTestCase(@"[\w-[\x00-\x0F]]+", "bbbaaaABYZ09zzzyyy","bbbaaaABYZ09zzzyyy"),

         new RegexTestCase(@"[\w-[\s]]+", "0AZaz9","0AZaz9"),
         new RegexTestCase(@"[\w-[\W]]+", "0AZaz9","0AZaz9"),
         new RegexTestCase(@"[\w-[\p{Po}]]+", "#a09AZz!","a09AZz"),

        new RegexTestCase(@"[\d-[\D]]+", RegexOptions.ECMAScript, "azAZ1024689", "1024689"),
        new RegexTestCase(@"[\d-[a-zA-Z]]+", RegexOptions.ECMAScript, "azAZ\x066102468\x0660", "02468"),
        new RegexTestCase(@"[\d-[\p{Ll}]]+", "\x066102468\x0660", "\x066102468\x0660"),

         new RegexTestCase(@"[a-zA-Z0-9-[\s]]+", " \tazAZ09","azAZ09"),

        new RegexTestCase(@"[a-zA-Z0-9-[\W]]+", "bbbaaaABCD09zzzyyy","bbbaaaABCD09zzzyyy"),
        new RegexTestCase(@"[a-zA-Z0-9-[^a-zA-Z0-9]]+", "bbbaaaABCD09zzzyyy","bbbaaaABCD09zzzyyy"),

        new RegexTestCase(@"[\p{Ll}-[A-Z]]+", "AZaz09","az"),
        new RegexTestCase(@"[\p{Nd}-[a-z]]+", "az09","09"),

        new RegexTestCase(@"[\P{Lu}-[\p{Lu}]]+", "AZazAZ","az"),
        new RegexTestCase(@"[\P{Lu}-[A-Z]]+", "AZazAZ","az"),
        new RegexTestCase(@"[\P{Nd}-[\p{Nd}]]+", "azAZ09","azAZ"),
        new RegexTestCase(@"[\P{Nd}-[2-8]]+", "1234567890azAZ1234567890","azAZ"),

        /****************************************************************************
        (A - B) A and B are the same sets
        *****************************************************************************/
        //No Negation
        new RegexTestCase(@"[abcd-[abcd]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[1234-[1234]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        //All Negation
        new RegexTestCase(@"[^abcd-[^abcd]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^1234-[^1234]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),

        //No Negation        
        new RegexTestCase(@"[a-z-[a-z]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[0-9-[0-9]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        //All Negation
        new RegexTestCase(@"[^a-z-[^a-z]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^0-9-[^0-9]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),

        //No Negation
        new RegexTestCase(@"[\w-[\w]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\W-[\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\s-[\s]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\S-[\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\d-[\d]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\D-[\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        //All Negation
        new RegexTestCase(@"[^\w-[^\w]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\W-[^\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\s-[^\s]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\S-[^\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\d-[^\d]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\D-[^\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        //MixedNegation
        new RegexTestCase(@"[^\w-[\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\w-[^\W]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\s-[\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\s-[^\S]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\d-[\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\d-[^\D]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),

        //No Negation
        new RegexTestCase(@"[\p{Ll}-[\p{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\P{Ll}-[\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\p{Lu}-[\p{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\P{Lu}-[\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\p{Nd}-[\p{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\P{Nd}-[\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        //All Negation
        new RegexTestCase(@"[^\p{Ll}-[^\p{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\P{Ll}-[^\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\p{Lu}-[^\p{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\P{Lu}-[^\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\p{Nd}-[^\p{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\P{Nd}-[^\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        //MixedNegation
        new RegexTestCase(@"[^\p{Ll}-[\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\p{Ll}-[^\P{Ll}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\p{Lu}-[\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\p{Lu}-[^\P{Lu}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[^\p{Nd}-[\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),
        new RegexTestCase(@"[\p{Nd}-[^\P{Nd}]]+", "abcxyzABCXYZ`!@#$%^&*()_-+= \t\n"),

        /****************************************************************************
        Alternating construct
        *****************************************************************************/
        new RegexTestCase(@"([ ]|[\w-[0-9]])+", "09az AZ90", "az AZ", "Z"),
        new RegexTestCase(@"([0-9-[02468]]|[0-9-[13579]])+", "az1234567890za", "1234567890", "0"),
        new RegexTestCase(@"([^0-9-[a-zAE-Z]]|[\w-[a-zAF-Z]])+", "azBCDE1234567890BCDEFza", "BCDE1234567890BCDE", "E"),
        new RegexTestCase("([\u0000-\uFFFF-[azAZ09]]|[\u0000-\uFFFF-[^azAZ09]])+", "azAZBCDE1234567890BCDEFAZza", "azAZBCDE1234567890BCDEFAZza", "a"),
        new RegexTestCase(@"([\p{Ll}-[aeiou]]|[^\w-[\s]])+", "aeiobcdxyz!@#aeio", "bcdxyz!@#", "#"),

        /****************************************************************************
        Multiple character classes using character class subtraction
        *****************************************************************************/
        new RegexTestCase(@"98[\d-[9]][\d-[8]][\d-[0]]", "98911 98881 98870 98871", "98871"),
        new RegexTestCase(@"m[\w-[^aeiou]][\w-[^aeiou]]t", "mbbt mect meet", "meet"),

        /****************************************************************************
        Nested character class subtraction
        *****************************************************************************/
        new RegexTestCase("[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]+",
            "abcxyzABCXYZ123890", "bcxyzABCXYZ123890"),
        new RegexTestCase("[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[\u0000-\uFFFF-[a]]]]]]]+",
            "bcxyzABCXYZ123890a", "a"),


        /****************************************************************************
        Negation with character class subtraction
        *****************************************************************************/
        new RegexTestCase("[abcdef-[^bce]]+", "adfbcefda", "bce"),
        new RegexTestCase("[^cde-[ag]]+", "agbfxyzga", "bfxyz"),


        /****************************************************************************
        Misc The idea here is come up with real world examples of char class subtraction. Things that
        would be difficult to define without it
        *****************************************************************************/
        new RegexTestCase("[\u0000-\uFFFF-[\\p{P}\\p{S}\\p{C}]]+", "!@`';.,$+<>=\x0001\x001FazAZ09", "azAZ09"),
        new RegexTestCase(@"[\p{L}-[^\p{Lu}]]+", "09',.abcxyzABCXYZ", "ABCXYZ"),

        new RegexTestCase(@"[\p{IsGreek}-[\P{Lu}]]+", "\u0390\u03FE\u0386\u0388\u03EC\u03EE\u0400", "\u03FE\u0386\u0388\u03EC\u03EE"),
        new RegexTestCase(@"[\p{IsBasicLatin}-[G-L]]+", "GAFMZL", "AFMZ"),

        new RegexTestCase(@"[a-zA-Z-[aeiouAEIOU]]+", "aeiouAEIOUbcdfghjklmnpqrstvwxyz", "bcdfghjklmnpqrstvwxyz"),

        //The following is an overly complex way of matching an ip address using char class subtraction
        new RegexTestCase(@"^
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
        , RegexOptions.IgnorePatternWhitespace, "255",  "255", "255", "2", "5", "5", "", "255", "2", "5"),

        new RegexTestCase(@"^
        (?<octet>
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
        , RegexOptions.IgnorePatternWhitespace, "256"),

        new RegexTestCase(@"^
        (?<octet>
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
        , RegexOptions.IgnorePatternWhitespace, "261"),


        /****************************************************************************
        Parser Correctness
        *****************************************************************************/
        //Character Class Substraction
        new RegexTestCase(@"[abcd\-d-[bc]]+", "bbbaaa---dddccc", "aaa---ddd"),
        new RegexTestCase(@"[abcd\-d-[bc]]+", "bbbaaa---dddccc", "aaa---ddd"),
        new RegexTestCase(@"[^a-f-[\x00-\x60\u007B-\uFFFF]]+", "aaafffgggzzz{{{", "gggzzz"),
        new RegexTestCase(@"[a-f-[]]+", typeof(ArgumentException)),
        new RegexTestCase(@"[\[\]a-f-[[]]+", "gggaaafff]]][[[", "aaafff]]]"),
        new RegexTestCase(@"[\[\]a-f-[]]]+", "gggaaafff[[[]]]", "aaafff[[["),

        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "a]]", "a]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "b]]", "b]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "c]]", "c]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "d]]", "d]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "[]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "-]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "`]]"),
        new RegexTestCase(@"[ab\-\[cd-[-[]]]]", "e]]"),

        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "a]]", "a]]"),
        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "b]]", "b]]"),
        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "c]]", "c]]"),
        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "d]]", "d]]"),
        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "-]]", "-]]"),
        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "']]"),
        new RegexTestCase(@"[ab\-\[cd-[[]]]]", "e]]"),

        new RegexTestCase(@"[a-[a-f]]", "abcdefghijklmnopqrstuvwxyz"),
        new RegexTestCase(@"[a-[c-e]]+", "bbbaaaccc", "aaa"),
        new RegexTestCase(@"[a-[c-e]]+", "```aaaccc", "aaa"),

        new RegexTestCase(@"[a-d\--[bc]]+", "cccaaa--dddbbb", "aaa--ddd"),

        //NOT Character Class Substraction
        new RegexTestCase(@"[\0- [bc]+", "!!!\0\0\t\t  [[[[bbbcccaaa", "\0\0\t\t  [[[[bbbccc"),
        new RegexTestCase(@"[[abcd]-[bc]]+", "a-b]", "a-b]"),
        new RegexTestCase(@"[-[e-g]+", "ddd[[[---eeefffggghhh", "[[[---eeefffggg"),
        new RegexTestCase(@"[-e-g]+", "ddd---eeefffggghhh", "---eeefffggg"),
        new RegexTestCase(@"[-e-g]+", "ddd---eeefffggghhh", "---eeefffggg"),
        new RegexTestCase(@"[a-e - m-p]+", "---a b c d e m n o p---", "a b c d e m n o p"),
        new RegexTestCase(@"[^-[bc]]", "b] c] -] aaaddd]", "d]"),
        new RegexTestCase(@"[^-[bc]]", "b] c] -] aaa]ddd]", "a]"),
        new RegexTestCase(@"[A-[]+", typeof(ArgumentException)),

        //Make sure we correctly handle \-
        new RegexTestCase(@"[a\-[bc]+", "```bbbaaa---[[[cccddd", "bbbaaa---[[[ccc"),
        new RegexTestCase(@"[a\-[\-\-bc]+", "```bbbaaa---[[[cccddd", "bbbaaa---[[[ccc"),
        new RegexTestCase(@"[a\-\[\-\[\-bc]+", "```bbbaaa---[[[cccddd", "bbbaaa---[[[ccc"),
        new RegexTestCase(@"[abc\--[b]]+", "[[[```bbbaaa---cccddd", "aaa---ccc"),
        new RegexTestCase(@"[abc\-z-[b]]+", "```aaaccc---zzzbbb", "aaaccc---zzz"),
        new RegexTestCase(@"[a-d\-[b]+", "```aaabbbcccddd----[[[[]]]", "aaabbbcccddd----[[[["),
        new RegexTestCase(@"[abcd\-d\-[bc]+", "bbbaaa---[[[dddccc", "bbbaaa---[[[dddccc"),

        //Everything works correctly with option RegexOptions.IgnorePatternWhitespace
        new RegexTestCase(@"[a - c - [ b ] ]+", RegexOptions.IgnorePatternWhitespace, "dddaaa   ccc [[[[ bbb ]]]", " ]]]"),
        new RegexTestCase(@"[a - c - [ b ] +", RegexOptions.IgnorePatternWhitespace, "dddaaa   ccc [[[[ bbb ]]]", "aaa   ccc [[[[ bbb "),
    };
}
