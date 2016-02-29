// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using System.Globalization;
using Xunit;

public class RegexLangElementsCoverageTests
{
    // This class mainly exists to hit language elements that were missed in other test cases.
    [Fact]
    public static void RegexLangElementsCoverage()
    {
        for (int i = 0; i < s_regexTests.Length; i++)
        {
            Assert.True(s_regexTests[i].Run());
        }
    }

    //private const int GERMAN_PHONEBOOK = 0x10407;
    //private const int ENGLISH_US = 0x0409;
    //private const int INVARIANT = 0x007F;
    //private const int CZECH = 0x0405;
    //private const int DANISH = 0x0406;
    //private const int TURKISH = 0x041F;
    //private const int LATIN_AZERI = 0x042C;
    private static CultureInfo _GERMAN_PHONEBOOK = new CultureInfo("de-DE");
    private static CultureInfo _ENGLISH_US = new CultureInfo("en-US");
    private static CultureInfo _INVARIANT = new CultureInfo("");
    private static CultureInfo _CZECH = new CultureInfo("cs-CZ");
    private static CultureInfo _DANISH = new CultureInfo("da-DK");
    private static CultureInfo _TURKISH = new CultureInfo("tr-TR");
    private static CultureInfo _LATIN_AZERI = new CultureInfo("az-Latn-AZ");

    private static RegexTestCase[] s_regexTests = new RegexTestCase[] {

        /*********************************************************
        Unicode Char Classes
        *********************************************************/
        new RegexTestCase(@"(\p{Lu}\w*)\s(\p{Lu}\w*)", "Hello World", new string[] {"Hello World", "Hello", "World"}),
        new RegexTestCase(@"(\p{Lu}\p{Ll}*)\s(\p{Lu}\p{Ll}*)", "Hello World", new string[] {"Hello World", "Hello", "World"}),
        new RegexTestCase(@"(\P{Ll}\p{Ll}*)\s(\P{Ll}\p{Ll}*)", "Hello World", new string[] {"Hello World", "Hello", "World"}),
        new RegexTestCase(@"(\P{Lu}+\p{Lu})\s(\P{Lu}+\p{Lu})", "hellO worlD", new string[] {"hellO worlD", "hellO", "worlD"}),
        new RegexTestCase(@"(\p{Lt}\w*)\s(\p{Lt}*\w*)", "\u01C5ello \u01C5orld", new string[] {"\u01C5ello \u01C5orld", "\u01C5ello", "\u01C5orld"}),
        new RegexTestCase(@"(\P{Lt}\w*)\s(\P{Lt}*\w*)", "Hello World", new string[] {"Hello World", "Hello", "World"}),


        /*********************************************************
            Character ranges IgnoreCase
        *********************************************************/
        new RegexTestCase(@"[@-D]+", RegexOptions.IgnoreCase, "eE?@ABCDabcdeE", new string[] {"@ABCDabcd"}),
        new RegexTestCase(@"[>-D]+", RegexOptions.IgnoreCase, "eE=>?@ABCDabcdeE", new string[] {">?@ABCDabcd"}),
        new RegexTestCase(@"[\u0554-\u0557]+", RegexOptions.IgnoreCase, "\u0583\u0553\u0554\u0555\u0556\u0584\u0585\u0586\u0557\u0558", new string[] {"\u0554\u0555\u0556\u0584\u0585\u0586\u0557"}),
        new RegexTestCase(@"[X-\]]+", RegexOptions.IgnoreCase, "wWXYZxyz[\\]^", new string[] {"XYZxyz[\\]"}),
        new RegexTestCase(@"[X-\u0533]+", RegexOptions.IgnoreCase, "\u0551\u0554\u0560AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563\u0564", new string[] {"AXYZaxyz\u0531\u0532\u0533\u0561\u0562\u0563"}),
        new RegexTestCase(@"[X-a]+", RegexOptions.IgnoreCase, "wWAXYZaxyz", new string[] {"AXYZaxyz"}),
        new RegexTestCase(@"[X-c]+", RegexOptions.IgnoreCase, "wWABCXYZabcxyz", new string[] {"ABCXYZabcxyz"}),
        new RegexTestCase(@"[X-\u00C0]+", RegexOptions.IgnoreCase, "\u00C1\u00E1\u00C0\u00E0wWABCXYZabcxyz", new string[] {"\u00C0\u00E0wWABCXYZabcxyz"}),
        new RegexTestCase(@"[\u0100\u0102\u0104]+", RegexOptions.IgnoreCase, "\u00FF \u0100\u0102\u0104\u0101\u0103\u0105\u0106", new string[] {"\u0100\u0102\u0104\u0101\u0103\u0105"}),
        new RegexTestCase(@"[B-D\u0130]+", RegexOptions.IgnoreCase, "aAeE\u0129\u0131\u0068 BCDbcD\u0130\u0069\u0070", new string[] {"BCDbcD\u0130\u0069"}),
        new RegexTestCase(@"[\u013B\u013D\u013F]+", RegexOptions.IgnoreCase, "\u013A\u013B\u013D\u013F\u013C\u013E\u0140\u0141", new string[] {"\u013B\u013D\u013F\u013C\u013E\u0140"}),
        new RegexTestCase(@"[\uFFFD-\uFFFF]+", RegexOptions.IgnoreCase, "\uFFFC\uFFFD\uFFFE\uFFFF", new string[] {"\uFFFD\uFFFE\uFFFF"}),
        new RegexTestCase(@"[\uFFFC-\uFFFE]+", RegexOptions.IgnoreCase, "\uFFFB\uFFFC\uFFFD\uFFFE\uFFFF", new string[] {"\uFFFC\uFFFD\uFFFE"}),



        /*********************************************************
            Escape Chars
            *********************************************************/
        new RegexTestCase("(Cat)\r(Dog)", "Cat\rDog", new string[] {"Cat\rDog", "Cat", "Dog"}),
        new RegexTestCase("(Cat)\t(Dog)", "Cat\tDog", new string[] {"Cat\tDog", "Cat", "Dog"}),
        new RegexTestCase("(Cat)\f(Dog)", "Cat\fDog", new string[] {"Cat\fDog", "Cat", "Dog"}),


        /*********************************************************
            Miscellaneous { witout matching }
            *********************************************************/
        new RegexTestCase(@"\p{klsak", typeof(ArgumentException)),
        new RegexTestCase(@"{5", "hello {5 world", new string[] {"{5"}),
        new RegexTestCase(@"{5,", "hello {5, world", new string[] {"{5,"}),
        new RegexTestCase(@"{5,6", "hello {5,6 world", new string[] {"{5,6"}),


        /*********************************************************
            Miscellaneous inline options
            *********************************************************/
        new RegexTestCase(@"(?r:cat)", typeof(ArgumentException)),
        new RegexTestCase(@"(?c:cat)", typeof(ArgumentException)),
        new RegexTestCase(@"(?n:(?<cat>cat)(\s+)(?<dog>dog))", "cat   dog", new string[] {"cat   dog", "cat", "dog"}),
        new RegexTestCase(@"(?n:(cat)(\s+)(dog))", "cat   dog", new string[] {"cat   dog"}),
        new RegexTestCase(@"(?n:(cat)(?<SpaceChars>\s+)(dog))", "cat   dog", new string[] {"cat   dog", "   "}),
        new RegexTestCase(@"(?x:
                            (?<cat>cat) # Cat statement
                            (\s+) # Whitespace chars
                            (?<dog>dog # Dog statement
                            ))", "cat   dog", new string[] {"cat   dog", "   ",  "cat", "dog"}),
        new RegexTestCase(@"(?e:cat)", typeof(ArgumentException)),
        new RegexTestCase(@"(?+i:cat)", "CAT", new string[] {"CAT"}),


        /*********************************************************
            \d, \D, \s, \S, \w, \W, \P, \p inside character range 
            *********************************************************/
        new RegexTestCase(@"cat([\d]*)dog", "hello123cat230927dog1412d", new string[] {"cat230927dog", "230927"}),
        new RegexTestCase(@"([\D]*)dog", "65498catdog58719", new string[] {"catdog", "cat"}),
        new RegexTestCase(@"cat([\s]*)dog", "wiocat   dog3270", new string[] {"cat   dog", "   "}),
        new RegexTestCase(@"cat([\S]*)", "sfdcatdog    3270", new string[] {"catdog", "dog"}),
        new RegexTestCase(@"cat([\w]*)", "sfdcatdog    3270", new string[] {"catdog", "dog"}),
        new RegexTestCase(@"cat([\W]*)dog", "wiocat   dog3270", new string[] {"cat   dog", "   "}),
        new RegexTestCase(@"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", "Hello World", new string[] {"Hello World", "Hello", "World"}),
        new RegexTestCase(@"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", "Hello World", new string[] {"Hello World", "Hello", "World"}),

        new RegexTestCase(@"cat([a-\d]*)dog", typeof(ArgumentException)),
        new RegexTestCase(@"([5-\D]*)dog", typeof(ArgumentException)),
        new RegexTestCase(@"cat([6-\s]*)dog", typeof(ArgumentException)),
        new RegexTestCase(@"cat([c-\S]*)", typeof(ArgumentException)),
        new RegexTestCase(@"cat([7-\w]*)", typeof(ArgumentException)),
        new RegexTestCase(@"cat([a-\W]*)dog", typeof(ArgumentException)),
        new RegexTestCase(@"([f-\p{Lu}]\w*)\s([\p{Lu}]\w*)", typeof(ArgumentException)),
        new RegexTestCase(@"([1-\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", typeof(ArgumentException)),
        new RegexTestCase(@"[\p]", typeof(ArgumentException)),
        new RegexTestCase(@"[\P]", typeof(ArgumentException)),
        new RegexTestCase(@"([\pcat])", typeof(ArgumentException)),
        new RegexTestCase(@"([\Pcat])", typeof(ArgumentException)),
        new RegexTestCase(@"(\p{", typeof(ArgumentException)),
        new RegexTestCase(@"(\p{Ll", typeof(ArgumentException)),



        /*********************************************************
            \x, \u, \a, \b, \e, \f, \n, \r, \t, \v, \c, inside character range 
            *********************************************************/
       new RegexTestCase(@"(cat)([\x41]*)(dog)", "catAAAdog", new string[] {"catAAAdog", "cat", "AAA", "dog"}),
       new RegexTestCase(@"(cat)([\u0041]*)(dog)", "catAAAdog", new string[] {"catAAAdog", "cat", "AAA", "dog"}),
       new RegexTestCase(@"(cat)([\a]*)(dog)", "cat\a\a\adog", new string[] {"cat\a\a\adog", "cat", "\a\a\a", "dog"}),
       new RegexTestCase(@"(cat)([\b]*)(dog)", "cat\b\b\bdog", new string[] {"cat\b\b\bdog", "cat", "\b\b\b", "dog"}),
       new RegexTestCase(@"(cat)([\e]*)(dog)", "cat\u001B\u001B\u001Bdog", new string[] {"cat\u001B\u001B\u001Bdog", "cat", "\u001B\u001B\u001B", "dog"}),
       new RegexTestCase(@"(cat)([\f]*)(dog)", "cat\f\f\fdog", new string[] {"cat\f\f\fdog", "cat", "\f\f\f", "dog"}),
       new RegexTestCase(@"(cat)([\r]*)(dog)", "cat\r\r\rdog", new string[] {"cat\r\r\rdog", "cat", "\r\r\r", "dog"}),
       new RegexTestCase(@"(cat)([\v]*)(dog)", "cat\v\v\vdog", new string[] {"cat\v\v\vdog", "cat", "\v\v\v", "dog"}),
       new RegexTestCase(@"(cat)([\o]*)(dog)", typeof(ArgumentException)),


        /*********************************************************
        \d, \D, \s, \S, \w, \W, \P, \p inside character range ([0-5]) with ECMA Option
        *********************************************************/
       new RegexTestCase(@"cat([\d]*)dog", RegexOptions.ECMAScript, "hello123cat230927dog1412d", new string[] {"cat230927dog", "230927"}),
       new RegexTestCase(@"([\D]*)dog", RegexOptions.ECMAScript, "65498catdog58719", new string[] {"catdog", "cat"}),
       new RegexTestCase(@"cat([\s]*)dog", RegexOptions.ECMAScript, "wiocat   dog3270", new string[] {"cat   dog", "   "}),
       new RegexTestCase(@"cat([\S]*)", RegexOptions.ECMAScript, "sfdcatdog    3270", new string[] {"catdog", "dog"}),
       new RegexTestCase(@"cat([\w]*)", RegexOptions.ECMAScript, "sfdcatdog    3270", new string[] {"catdog", "dog"}),
       new RegexTestCase(@"cat([\W]*)dog", RegexOptions.ECMAScript, "wiocat   dog3270", new string[] {"cat   dog", "   "}),
       new RegexTestCase(@"([\p{Lu}]\w*)\s([\p{Lu}]\w*)", RegexOptions.ECMAScript, "Hello World", new string[] {"Hello World", "Hello", "World"}),
       new RegexTestCase(@"([\P{Ll}][\p{Ll}]*)\s([\P{Ll}][\p{Ll}]*)", RegexOptions.ECMAScript, "Hello World", new string[] {"Hello World", "Hello", "World"}),

        /*********************************************************
        \d, \D, \s, \S, \w, \W, \P, \p outside character range ([0-5]) with ECMA Option
        *********************************************************/
       new RegexTestCase(@"(cat)\d*dog", RegexOptions.ECMAScript, "hello123cat230927dog1412d", new string[] {"cat230927dog", "cat"}),
       new RegexTestCase(@"\D*(dog)", RegexOptions.ECMAScript, "65498catdog58719", new string[] {"catdog", "dog"}),
       new RegexTestCase(@"(cat)\s*(dog)", RegexOptions.ECMAScript, "wiocat   dog3270", new string[] {"cat   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\S*", RegexOptions.ECMAScript, "sfdcatdog    3270", new string[] {"catdog", "cat"}),
       new RegexTestCase(@"(cat)\w*", RegexOptions.ECMAScript, "sfdcatdog    3270", new string[] {"catdog", "cat"}),
       new RegexTestCase(@"(cat)\W*(dog)", RegexOptions.ECMAScript, "wiocat   dog3270", new string[] {"cat   dog", "cat", "dog"}),
       new RegexTestCase(@"\p{Lu}(\w*)\s\p{Lu}(\w*)", RegexOptions.ECMAScript, "Hello World", new string[] {"Hello World", "ello", "orld"}),
       new RegexTestCase(@"\P{Ll}\p{Ll}*\s\P{Ll}\p{Ll}*", RegexOptions.ECMAScript, "Hello World", new string[] {"Hello World"}),

        /*********************************************************
        Use < in a group
        *********************************************************/
       new RegexTestCase(@"cat(?<0>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<1dog>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<dog)_*>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<dog!>)_*>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<dog >)_*>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<dog<>)_*>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<->dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?<dog121>dog)", "catcatdogdogcat", new string[] {"catdog", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s*(?<cat>dog)", "catcat    dogdogcat", new string[] {"cat    dog", "dog"}),
       new RegexTestCase(@"(?<1>cat)\s*(?<1>dog)", "catcat    dogdogcat", new string[] {"cat    dog", "dog"}),
       new RegexTestCase(@"(?<2048>cat)\s*(?<2048>dog)", "catcat    dogdogcat", new string[] {"cat    dog", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\w+(?<dog-cat>dog)", "cat_Hello_World_dog", new string[] {"cat_Hello_World_dog", "", "_Hello_World_"}),
       new RegexTestCase(@"(?<cat>cat)\w+(?<-cat>dog)", "cat_Hello_World_dog", new string[] {"cat_Hello_World_dog", ""}),
       new RegexTestCase(@"(?<cat>cat)\w+(?<cat-cat>dog)", "cat_Hello_World_dog", new string[] {"cat_Hello_World_dog", "_Hello_World_"}),
       new RegexTestCase(@"(?<1>cat)\w+(?<dog-1>dog)", "cat_Hello_World_dog", new string[] {"cat_Hello_World_dog", "", "_Hello_World_"}),
       new RegexTestCase(@"(?<cat>cat)\w+(?<2-cat>dog)", "cat_Hello_World_dog", new string[] {"cat_Hello_World_dog", "", "_Hello_World_"}),
       new RegexTestCase(@"(?<1>cat)\w+(?<2-1>dog)", "cat_Hello_World_dog", new string[] {"cat_Hello_World_dog", "", "_Hello_World_"}),
       new RegexTestCase(@"(?<cat>cat)\w+(?<dog-16>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\w+(?<dog-1uosn>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\w+(?<dog-catdog>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\w+(?<dog-()*!@>dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\w+(?<dog-0>dog)", "cat_Hello_World_dog", null),

        /*********************************************************
        Quantifiers
        *********************************************************/
       new RegexTestCase(@"(?<cat>cat){", "STARTcat{", new string[] {"cat{", "cat"}),
       new RegexTestCase(@"(?<cat>cat){fdsa", "STARTcat{fdsa", new string[] {"cat{fdsa", "cat"}),
       new RegexTestCase(@"(?<cat>cat){1", "STARTcat{1", new string[] {"cat{1", "cat"}),
       new RegexTestCase(@"(?<cat>cat){1END", "STARTcat{1END", new string[] {"cat{1END", "cat"}),
       new RegexTestCase(@"(?<cat>cat){1,", "STARTcat{1,", new string[] {"cat{1,", "cat"}),
       new RegexTestCase(@"(?<cat>cat){1,END", "STARTcat{1,END", new string[] {"cat{1,END", "cat"}),
       new RegexTestCase(@"(?<cat>cat){1,2", "STARTcat{1,2", new string[] {"cat{1,2", "cat"}),
       new RegexTestCase(@"(?<cat>cat){1,2END", "STARTcat{1,2END", new string[] {"cat{1,2END", "cat"}),

        /*********************************************************
        Use (? in a group
        *********************************************************/
       new RegexTestCase(@"cat(?(?#COMMENT)cat)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?(?'cat'cat)dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?(?<cat>cat)dog)", typeof(ArgumentException)),
       new RegexTestCase(@"cat(?(?afdcat)dog)", typeof(ArgumentException)),

        /*********************************************************
        Use IgnorePatternWhitespace
        *********************************************************/
       new RegexTestCase(@"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog
                            ", RegexOptions.IgnorePatternWhitespace, "cat    dog", new string[] {"cat    dog", "cat", "dog" }),
       new RegexTestCase(@"(cat) #cat
                            \s+ #followed by 1 or more whitespace
                            (dog)  #followed by dog", RegexOptions.IgnorePatternWhitespace, "cat    dog", new string[] {"cat    dog", "cat", "dog" }),
       new RegexTestCase(@"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace) (dog)  (?#followed by dog)",
           RegexOptions.IgnorePatternWhitespace, "cat    dog", new string[] {"cat    dog", "cat", "dog" }),
       new RegexTestCase(@"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace",
           RegexOptions.IgnorePatternWhitespace, typeof(ArgumentException)),

        /*********************************************************
        Without IgnorePatternWhitespace
        *********************************************************/
       new RegexTestCase(@"(cat) (?#cat)    \s+ (?#followed by 1 or more whitespace", typeof(ArgumentException)),

        /*********************************************************
        Back Reference
        *********************************************************/
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k<cat>", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k'cat'", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\<cat>", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\'cat'", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),

       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k<1>", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k'1'", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\<1>", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\'1'", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\1", "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\1", RegexOptions.ECMAScript, "asdfcat   dogcat   dog", new string[] {"cat   dogcat", "cat", "dog"}),

       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k<dog>", "asdfcat   dogdog   dog", new string[] {"cat   dogdog", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\2", "asdfcat   dogdog   dog", new string[] {"cat   dogdog", "cat", "dog"}),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\2", RegexOptions.ECMAScript, "asdfcat   dogdog   dog", new string[] {"cat   dogdog", "cat", "dog"}),

       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\kcat", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k<cat2>", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k<8>cat", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k<8>cat", RegexOptions.ECMAScript, typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k8", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\k8", RegexOptions.ECMAScript, typeof(ArgumentException)),

        /*********************************************************
        Octal
        *********************************************************/
       new RegexTestCase(@"(cat)(\077)", "hellocat?dogworld", new string[] {"cat?", "cat", "?"}),
       new RegexTestCase(@"(cat)(\77)", "hellocat?dogworld", new string[] {"cat?", "cat", "?"}),
       new RegexTestCase(@"(cat)(\176)", "hellocat~dogworld", new string[] {"cat~", "cat", "~"}),
       new RegexTestCase(@"(cat)(\400)", "hellocat\0dogworld", new string[] {"cat\0", "cat", "\0"}),
       new RegexTestCase(@"(cat)(\300)", "hellocat\u00C0dogworld", new string[] {"cat\u00C0", "cat", "\u00C0"}),
       new RegexTestCase(@"(cat)(\300)", "hellocat\u00C0dogworld", new string[] {"cat\u00C0", "cat", "\u00C0"}),
       new RegexTestCase(@"(cat)(\477)", "hellocat\u003Fdogworld", new string[] {"cat\u003F", "cat", "\u003F"}),
       new RegexTestCase(@"(cat)(\777)", "hellocat\u00FFdogworld", new string[] {"cat\u00FF", "cat", "\u00FF"}),
       new RegexTestCase(@"(cat)(\7770)", "hellocat\u00FF0dogworld", new string[] {"cat\u00FF0", "cat", "\u00FF0"}),
       new RegexTestCase(@"(cat)(\7)", typeof(ArgumentException)),

       new RegexTestCase(@"(cat)(\077)", RegexOptions.ECMAScript, "hellocat?dogworld", new string[] {"cat?", "cat", "?"}),
       new RegexTestCase(@"(cat)(\77)", RegexOptions.ECMAScript, "hellocat?dogworld", new string[] {"cat?", "cat", "?"}),
       new RegexTestCase(@"(cat)(\7)", RegexOptions.ECMAScript, "hellocat\adogworld", new string[] {"cat\a", "cat", "\a"}),
       new RegexTestCase(@"(cat)(\40)", RegexOptions.ECMAScript, "hellocat dogworld", new string[] {"cat ", "cat", " "}),
       new RegexTestCase(@"(cat)(\040)", RegexOptions.ECMAScript, "hellocat dogworld", new string[] {"cat ", "cat", " "}),
       new RegexTestCase(@"(cat)(\176)", RegexOptions.ECMAScript, "hellocatcat76dogworld", new string[] {"catcat76", "cat", "cat76"}),
       new RegexTestCase(@"(cat)(\377)", RegexOptions.ECMAScript, "hellocat\u00FFdogworld", new string[] {"cat\u00FF", "cat", "\u00FF"}),
       new RegexTestCase(@"(cat)(\400)", RegexOptions.ECMAScript, "hellocat 0Fdogworld", new string[] {"cat 0", "cat", " 0"}),

        /*********************************************************
        Decimal
        *********************************************************/
       new RegexTestCase(@"(cat)\s+(?<2147483646>dog)", "asdlkcat  dogiwod", new string[] {"cat  dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(?<2147483647>dog)",  "asdlkcat  dogiwod", new string[] {"cat  dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(?<2147483648>dog)", typeof(System.ArgumentException)),
       new RegexTestCase(@"(cat)\s+(?<21474836481097>dog)", typeof(System.ArgumentException)),

        /*********************************************************
        Hex
        *********************************************************/
       new RegexTestCase(@"(cat)(\x2a*)(dog)", "asdlkcat***dogiwod", new string[] {"cat***dog", "cat", "***", "dog"}),
       new RegexTestCase(@"(cat)(\x2b*)(dog)", "asdlkcat+++dogiwod", new string[] {"cat+++dog", "cat", "+++", "dog"}),
       new RegexTestCase(@"(cat)(\x2c*)(dog)", "asdlkcat,,,dogiwod", new string[] {"cat,,,dog", "cat", ",,,", "dog"}),
       new RegexTestCase(@"(cat)(\x2d*)(dog)", "asdlkcat---dogiwod", new string[] {"cat---dog", "cat", "---", "dog"}),
       new RegexTestCase(@"(cat)(\x2e*)(dog)", "asdlkcat...dogiwod", new string[] {"cat...dog", "cat", "...", "dog"}),
       new RegexTestCase(@"(cat)(\x2f*)(dog)", "asdlkcat///dogiwod", new string[] {"cat///dog", "cat", "///", "dog"}),

       new RegexTestCase(@"(cat)(\x2A*)(dog)", "asdlkcat***dogiwod", new string[] {"cat***dog", "cat", "***", "dog"}),
       new RegexTestCase(@"(cat)(\x2B*)(dog)", "asdlkcat+++dogiwod", new string[] {"cat+++dog", "cat", "+++", "dog"}),
       new RegexTestCase(@"(cat)(\x2C*)(dog)", "asdlkcat,,,dogiwod", new string[] {"cat,,,dog", "cat", ",,,", "dog"}),
       new RegexTestCase(@"(cat)(\x2D*)(dog)", "asdlkcat---dogiwod", new string[] {"cat---dog", "cat", "---", "dog"}),
       new RegexTestCase(@"(cat)(\x2E*)(dog)", "asdlkcat...dogiwod", new string[] {"cat...dog", "cat", "...", "dog"}),
       new RegexTestCase(@"(cat)(\x2F*)(dog)", "asdlkcat///dogiwod", new string[] {"cat///dog", "cat", "///", "dog"}),

        /*********************************************************
        ScanControl
        *********************************************************/
       new RegexTestCase(@"(cat)(\c*)(dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(cat)\c", typeof(ArgumentException)),
       new RegexTestCase(@"(cat)(\c *)(dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(cat)(\c?*)(dog)", typeof(ArgumentException)),
       new RegexTestCase("(cat)(\\c\0*)(dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(cat)(\c`*)(dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(cat)(\c\|*)(dog)", typeof(ArgumentException)),
       new RegexTestCase(@"(cat)(\c\[*)(dog)", typeof(ArgumentException)),

       new RegexTestCase(@"(cat)(\c@*)(dog)", "asdlkcat\0\0dogiwod", new string[] {"cat\0\0dog", "cat", "\0\0", "dog"}),
       new RegexTestCase(@"(cat)(\cA*)(dog)", "asdlkcat\u0001dogiwod", new string[] {"cat\u0001dog", "cat", "\u0001", "dog"}),
       new RegexTestCase(@"(cat)(\ca*)(dog)", "asdlkcat\u0001dogiwod", new string[] {"cat\u0001dog", "cat", "\u0001", "dog"}),

       new RegexTestCase(@"(cat)(\cC*)(dog)", "asdlkcat\u0003dogiwod", new string[] {"cat\u0003dog", "cat", "\u0003", "dog"}),
       new RegexTestCase(@"(cat)(\cc*)(dog)", "asdlkcat\u0003dogiwod", new string[] {"cat\u0003dog", "cat", "\u0003", "dog"}),

       new RegexTestCase(@"(cat)(\cD*)(dog)", "asdlkcat\u0004dogiwod", new string[] {"cat\u0004dog", "cat", "\u0004", "dog"}),
       new RegexTestCase(@"(cat)(\cd*)(dog)", "asdlkcat\u0004dogiwod", new string[] {"cat\u0004dog", "cat", "\u0004", "dog"}),

       new RegexTestCase(@"(cat)(\cX*)(dog)", "asdlkcat\u0018dogiwod", new string[] {"cat\u0018dog", "cat", "\u0018", "dog"}),
       new RegexTestCase(@"(cat)(\cx*)(dog)", "asdlkcat\u0018dogiwod", new string[] {"cat\u0018dog", "cat", "\u0018", "dog"}),

       new RegexTestCase(@"(cat)(\cZ*)(dog)", "asdlkcat\u001adogiwod", new string[] {"cat\u001adog", "cat", "\u001a", "dog"}),
       new RegexTestCase(@"(cat)(\cz*)(dog)", "asdlkcat\u001adogiwod", new string[] {"cat\u001adog", "cat", "\u001a", "dog"}),

        /*********************************************************
        Atomic Zero-Width Assertions \A \Z \z \G \b \B
        *********************************************************/
        //\A
        new RegexTestCase(@"\A(cat)\s+(dog)", "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"\A(cat)\s+(dog)", RegexOptions.Multiline, "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"\A(cat)\s+(dog)", RegexOptions.ECMAScript, "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),

       new RegexTestCase(@"\A(cat)\s+(dog)", "cat   \n\n\ncat     dog", null),
       new RegexTestCase(@"\A(cat)\s+(dog)", RegexOptions.Multiline, "cat   \n\n\ncat     dog", null),
       new RegexTestCase(@"\A(cat)\s+(dog)", RegexOptions.ECMAScript, "cat   \n\n\ncat     dog", null),

        //\Z
        new RegexTestCase(@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\Z", RegexOptions.Multiline, "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\Z", RegexOptions.ECMAScript, "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\Z", "cat   \n\n\n   dog\n", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\Z", RegexOptions.Multiline, "cat   \n\n\n   dog\n", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\Z", RegexOptions.ECMAScript, "cat   \n\n\n   dog\n", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),

       new RegexTestCase(@"(cat)\s+(dog)\Z", "cat   dog\n\n\ncat", null),
       new RegexTestCase(@"(cat)\s+(dog)\Z", RegexOptions.Multiline, "cat   dog\n\n\ncat     ", null),
       new RegexTestCase(@"(cat)\s+(dog)\Z", RegexOptions.ECMAScript, "cat   dog\n\n\ncat     ", null),

        //\z
        new RegexTestCase(@"(cat)\s+(dog)\z", "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\z", RegexOptions.Multiline, "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),
       new RegexTestCase(@"(cat)\s+(dog)\z", RegexOptions.ECMAScript, "cat   \n\n\n   dog", new string[] {"cat   \n\n\n   dog", "cat", "dog"}),

       new RegexTestCase(@"(cat)\s+(dog)\z", "cat   dog\n\n\ncat", null),
       new RegexTestCase(@"(cat)\s+(dog)\z", RegexOptions.Multiline, "cat   dog\n\n\ncat     ", null),
       new RegexTestCase(@"(cat)\s+(dog)\z", RegexOptions.ECMAScript, "cat   dog\n\n\ncat     ", null),
       new RegexTestCase(@"(cat)\s+(dog)\z", "cat   \n\n\n   dog\n", null),
       new RegexTestCase(@"(cat)\s+(dog)\z", RegexOptions.Multiline, "cat   \n\n\n   dog\n", null),
       new RegexTestCase(@"(cat)\s+(dog)\z", RegexOptions.ECMAScript, "cat   \n\n\n   dog\n", null),

        //\b
        new RegexTestCase(@"\b@cat", "123START123@catEND", new string[] {"@cat"}),
       new RegexTestCase(@"\b\<cat", "123START123<catEND", new string[] {"<cat"}),
       new RegexTestCase(@"\b,cat", "satwe,,,START,catEND", new string[] {",cat"}),
       new RegexTestCase(@"\b\[cat", "`12START123[catEND", new string[] {"[cat"}),

       new RegexTestCase(@"\b@cat", "123START123;@catEND", null),
       new RegexTestCase(@"\b\<cat", "123START123'<catEND", null),
       new RegexTestCase(@"\b,cat", "satwe,,,START',catEND", null),
       new RegexTestCase(@"\b\[cat", "`12START123'[catEND", null),

        //\B
        new RegexTestCase(@"\B@cat", "123START123;@catEND", new string[] {"@cat"}),
       new RegexTestCase(@"\B\<cat", "123START123'<catEND", new string[] {"<cat"}),
       new RegexTestCase(@"\B,cat", "satwe,,,START',catEND", new string[] {",cat"}),
       new RegexTestCase(@"\B\[cat", "`12START123'[catEND", new string[] {"[cat"}),

       new RegexTestCase(@"\B@cat", "123START123@catEND", null),
       new RegexTestCase(@"\B\<cat", "123START123<catEND", null),
       new RegexTestCase(@"\B,cat", "satwe,,,START,catEND", null),
       new RegexTestCase(@"\B\[cat", "`12START123[catEND", null),

        /*********************************************************
        \w matching \p{Lm} (Letter, Modifier)
        *********************************************************/
       new RegexTestCase(@"(\w+)\s+(\w+)", "cat\u02b0 dog\u02b1", new string[] {"cat\u02b0 dog\u02b1", "cat\u02b0", "dog\u02b1"}),
       new RegexTestCase(@"(cat\w+)\s+(dog\w+)", "STARTcat\u30FC dog\u3005END", new string[] {"cat\u30FC dog\u3005END", "cat\u30FC", "dog\u3005END"}),
       new RegexTestCase(@"(cat\w+)\s+(dog\w+)", "STARTcat\uff9e dog\uff9fEND", new string[] {"cat\uff9e dog\uff9fEND", "cat\uff9e", "dog\uff9fEND"}),

        /*********************************************************
        positive and negative character classes [a-c]|[^b-c]
        *********************************************************/
       new RegexTestCase(@"[^a]|d", "d", new string[] {"d"}),
       new RegexTestCase(@"([^a]|[d])*", "Hello Worlddf", new string[] {"Hello Worlddf", "f"}),
       new RegexTestCase(@"([^{}]|\n)+", "{{{{Hello\n World \n}END", new string[] {"Hello\n World \n", "\n"}),
       new RegexTestCase(@"([a-d]|[^abcd])+", "\tonce\n upon\0 a- ()*&^%#time?", new string[] {"\tonce\n upon\0 a- ()*&^%#time?", "?"}),
       new RegexTestCase(@"([^a]|[a])*", "once upon a time", new string[] {"once upon a time", "e"}),
       new RegexTestCase(@"([a-d]|[^abcd]|[x-z]|^wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", new string[] {"\tonce\n upon\0 a- ()*&^%#time?", "?"}),
       new RegexTestCase(@"([a-d]|[e-i]|[^e]|wxyz])+", "\tonce\n upon\0 a- ()*&^%#time?", new string[] {"\tonce\n upon\0 a- ()*&^%#time?", "?"}),

        /*********************************************************
        canonical and noncanonical char class, where one group is in it's
        simplest form [a-e] and another is more complex .
        *********************************************************/
       new RegexTestCase(@"^(([^b]+ )|(.* ))$", "aaa ", new string[] {"aaa ", "aaa ", "aaa ", ""}),
       new RegexTestCase(@"^(([^b]+ )|(.*))$", "aaa", new string[] {"aaa", "aaa", "", "aaa"}),
       new RegexTestCase(@"^(([^b]+ )|(.* ))$", "bbb ", new string[] {"bbb ", "bbb ", "", "bbb "}),
       new RegexTestCase(@"^(([^b]+ )|(.*))$", "bbb", new string[] {"bbb", "bbb", "", "bbb"}),
       new RegexTestCase(@"^((a*)|(.*))$", "aaa", new string[] {"aaa", "aaa", "aaa", ""}),
       new RegexTestCase(@"^((a*)|(.*))$", "aaabbb", new string[] {"aaabbb", "aaabbb", "", "aaabbb"}),

       new RegexTestCase(@"(([0-9])|([a-z])|([A-Z]))*", "{hello 1234567890 world}", new string[] {"", "", "", "", ""}),
       new RegexTestCase(@"(([0-9])|([a-z])|([A-Z]))+", "{hello 1234567890 world}", new string[] {"hello", "o", "", "o", ""}),
       new RegexTestCase(@"(([0-9])|([a-z])|([A-Z]))*", "{HELLO 1234567890 world}", new string[] {"", "", "", "", ""}),
       new RegexTestCase(@"(([0-9])|([a-z])|([A-Z]))+", "{HELLO 1234567890 world}", new string[] {"HELLO", "O", "", "", "O"}),
       new RegexTestCase(@"(([0-9])|([a-z])|([A-Z]))*", "{1234567890 hello  world}", new string[] {"", "", "", "", ""}),
       new RegexTestCase(@"(([0-9])|([a-z])|([A-Z]))+", "{1234567890 hello world}", new string[] {"1234567890", "0", "0", "", ""}),

       new RegexTestCase(@"^(([a-d]*)|([a-z]*))$", "aaabbbcccdddeeefff", new string[] {"aaabbbcccdddeeefff", "aaabbbcccdddeeefff", "", "aaabbbcccdddeeefff"}),
       new RegexTestCase(@"^(([d-f]*)|([c-e]*))$", "dddeeeccceee", new string[] {"dddeeeccceee", "dddeeeccceee", "", "dddeeeccceee"}),
       new RegexTestCase(@"^(([c-e]*)|([d-f]*))$", "dddeeeccceee", new string[] {"dddeeeccceee", "dddeeeccceee", "dddeeeccceee", ""}),

       new RegexTestCase(@"(([a-d]*)|([a-z]*))", "aaabbbcccdddeeefff", new string[] {"aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", ""}),
       new RegexTestCase(@"(([d-f]*)|([c-e]*))", "dddeeeccceee", new string[] {"dddeee", "dddeee", "dddeee", ""}),
       new RegexTestCase(@"(([c-e]*)|([d-f]*))", "dddeeeccceee", new string[] {"dddeeeccceee", "dddeeeccceee", "dddeeeccceee", ""}),

       new RegexTestCase(@"(([a-d]*)|(.*))", "aaabbbcccdddeeefff", new string[] {"aaabbbcccddd", "aaabbbcccddd", "aaabbbcccddd", ""}),
       new RegexTestCase(@"(([d-f]*)|(.*))", "dddeeeccceee", new string[] {"dddeee", "dddeee", "dddeee", ""}),
       new RegexTestCase(@"(([c-e]*)|(.*))", "dddeeeccceee", new string[] {"dddeeeccceee", "dddeeeccceee", "dddeeeccceee", ""}),

        /*********************************************************
        \p{Pi} (Punctuation Initial quote) \p{Pf} (Punctuation Final quote)
        *********************************************************/
       new RegexTestCase(@"\p{Pi}(\w*)\p{Pf}", "\u00ABCat\u00BB   \u00BBDog\u00AB'", new string[] {"\u00ABCat\u00BB", "Cat"}),
       new RegexTestCase(@"\p{Pi}(\w*)\p{Pf}", "\u2018Cat\u2019   \u2019Dog\u2018'", new string[] {"\u2018Cat\u2019", "Cat"}),



        /*********************************************************
Use special unicode characters
        *********************************************************/
        /*        new RegexTest(@"AE", "\u00C4", new string[] {"Hello World", "Hello", "World"}, GERMAN_PHONEBOOK),
                new RegexTest(@"oe", "\u00F6", new string[] {"Hello World", "Hello", "World"}, GERMAN_PHONEBOOK),

                new RegexTest("\u00D1", "\u004E\u0303", new string[] {"Hello World", "Hello", "World"}, ENGLISH_US),
                new RegexTest("\u00D1", "\u004E\u0303", new string[] {"Hello World", "Hello", "World"}, INVARIANT),
                new RegexTest("\u00D1", RegexOptions.IgnoreCase, "\u006E\u0303", new string[] {"Hello World", "Hello", "World"}, ENGLISH_US),
                new RegexTest("\u00D1", RegexOptions.IgnoreCase, "\u006E\u0303", new string[] {"Hello World", "Hello", "World"}, INVARIANT),

                new RegexTest("\u00F1", RegexOptions.IgnoreCase, "\u004E\u0303", new string[] {"Hello World", "Hello", "World"}, ENGLISH_US),
                new RegexTest("\u00F1", RegexOptions.IgnoreCase, "\u004E\u0303", new string[] {"Hello World", "Hello", "World"}, INVARIANT),
                new RegexTest("\u00F1", "\u006E\u0303", new string[] {"Hello World", "Hello", "World"}, ENGLISH_US),
                new RegexTest("\u00F1", "\u006E\u0303", new string[] {"Hello World", "Hello", "World"}, ENGLISH_US),
        */
       new RegexTestCase("CH", RegexOptions.IgnoreCase, "Ch", new string[] {"Ch"}, _ENGLISH_US),
       new RegexTestCase("CH", RegexOptions.IgnoreCase, "Ch", new string[] {"Ch"}, _CZECH),
       new RegexTestCase("cH", RegexOptions.IgnoreCase, "Ch", new string[] {"Ch"}, _ENGLISH_US),
       new RegexTestCase("cH", RegexOptions.IgnoreCase, "Ch", new string[] {"Ch"}, _CZECH),

       new RegexTestCase("AA", RegexOptions.IgnoreCase, "Aa", new string[] {"Aa"}, _ENGLISH_US),
       new RegexTestCase("AA", RegexOptions.IgnoreCase, "Aa", new string[] {"Aa"}, _DANISH),
       new RegexTestCase("aA", RegexOptions.IgnoreCase, "Aa", new string[] {"Aa"}, _ENGLISH_US),
       new RegexTestCase("aA", RegexOptions.IgnoreCase, "Aa", new string[] {"Aa"}, _DANISH),

       new RegexTestCase("\u0131", RegexOptions.IgnoreCase, "\u0049", new string[] {"\u0049"}, _TURKISH),
       new RegexTestCase("\u0130", RegexOptions.IgnoreCase, "\u0069", new string[] {"\u0069"}, _TURKISH),
       new RegexTestCase("\u0131", RegexOptions.IgnoreCase, "\u0049", new string[] {"\u0049"}, _LATIN_AZERI),
       new RegexTestCase("\u0130", RegexOptions.IgnoreCase, "\u0069", new string[] {"\u0069"}, _LATIN_AZERI),

       new RegexTestCase("\u0131", RegexOptions.IgnoreCase, "\u0049", null, _ENGLISH_US),
       new RegexTestCase("\u0131", RegexOptions.IgnoreCase, "\u0069", null, _ENGLISH_US),

        new RegexTestCase("\u0130", RegexOptions.IgnoreCase, "\u0049", new string[] {"\u0049"}, _ENGLISH_US),
        new RegexTestCase("\u0130", RegexOptions.IgnoreCase, "\u0069", new string[] {"\u0069"}, _ENGLISH_US),

       new RegexTestCase("\u0131", RegexOptions.IgnoreCase, "\u0049", null, _INVARIANT),
       new RegexTestCase("\u0131", RegexOptions.IgnoreCase, "\u0069", null, _INVARIANT),
       new RegexTestCase("\u0130", RegexOptions.IgnoreCase, "\u0049", null, _INVARIANT),
       new RegexTestCase("\u0130", RegexOptions.IgnoreCase, "\u0069", null, _INVARIANT),

        /*********************************************************
        ECMAScript
        *********************************************************/
       new RegexTestCase(@"(?<cat>cat)\s+(?<dog>dog)\s+\123\s+\234", RegexOptions.ECMAScript, "asdfcat   dog     cat23    dog34eia", new string[] {"cat   dog     cat23    dog34", "cat", "dog"}),


        /*********************************************************
        Balanced Matching
        *********************************************************/
       new RegexTestCase(@"<div> 
            (?> 
                <div>(?<DEPTH>) |   
                </div> (?<-DEPTH>) |  
                .?
            )*?
            (?(DEPTH)(?!)) 
            </div>", RegexOptions.IgnorePatternWhitespace,
           "<div>this is some <div>red</div> text</div></div></div>",
           new string[] {"<div>this is some <div>red</div> text</div>", ""}),

       new RegexTestCase(@"(
            ((?'open'<+)[^<>]*)+
            ((?'close-open'>+)[^<>]*)+
            )+", RegexOptions.IgnorePatternWhitespace,
           "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>",
           new string[] {"<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "<02deep_03<03deep_03>>>",
               "<03deep_03", ">>>", "<", "03deep_03"}),

       new RegexTestCase(@"(
            (?<start><)?
            [^<>]?
            (?<end-start>>)?
            )*", RegexOptions.IgnorePatternWhitespace,
           "<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>",
           new string[] {"<01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>>", "", "",
               "01deep_01<02deep_01<03deep_01>><02deep_02><02deep_03<03deep_03>>"}),

       new RegexTestCase(@"(
            (?<start><[^/<>]*>)?
            [^<>]?
            (?<end-start></[^/<>]*>)?
            )*", RegexOptions.IgnorePatternWhitespace,
           "<b><a>Cat</a></b>",
           new string[] {"<b><a>Cat</a></b>", "", "", "<a>Cat</a>"}),

       new RegexTestCase(@"(
            (?<start><(?<TagName>[^/<>]*)>)?
            [^<>]?
            (?<end-start></\k<TagName>>)?
            )*", RegexOptions.IgnorePatternWhitespace,
           "<b>cat</b><a>dog</a>",
           new string[] {"<b>cat</b><a>dog</a>", "", "", "a", "dog"}),

        /*********************************************************
        Balanced Matching With Backtracking
        *********************************************************/
       new RegexTestCase(@"(
            (?<start><[^/<>]*>)?
            .?
            (?<end-start></[^/<>]*>)?
            )*
            (?(start)(?!)) ", RegexOptions.IgnorePatternWhitespace,
           "<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>",
           new string[] {"<b><a>Cat</a></b><<<<c>>>><<d><e<f>><g><<<>>>>", "", "", "<a>Cat"}),

        /*********************************************************
        Character Classes and Lazy quantifier
        *********************************************************/
       new RegexTestCase(@"([0-9]+?)([\w]+?)", RegexOptions.ECMAScript, "55488aheiaheiad", new string[] {"55", "5", "5"}),
       new RegexTestCase(@"([0-9]+?)([a-z]+?)", RegexOptions.ECMAScript, "55488aheiaheiad", new string[] {"55488a", "55488", "a"}),

        /*********************************************************
        Miscellaneous/Regression scenarios
        *********************************************************/
       new RegexTestCase(@"(?<openingtag>1)(?<content>.*?)(?=2)", RegexOptions.Singleline | RegexOptions.ExplicitCapture,
           "1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine + "2", 
           new string[] {"1" + Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>" + Environment.NewLine, "1",
               Environment.NewLine + "<Projecaa DefaultTargets=\"x\"/>"+ Environment.NewLine }),

       new RegexTestCase(@"\G<%#(?<code>.*?)?%>", RegexOptions.Singleline,
           @"<%# DataBinder.Eval(this, ""MyNumber"") %>", new string[] {@"<%# DataBinder.Eval(this, ""MyNumber"") %>", @" DataBinder.Eval(this, ""MyNumber"") "}),

        /*********************************************************
        Nested Quantifiers
        *********************************************************/
       new RegexTestCase(@"^[abcd]{0,0x10}*$", "a{0,0x10}}}", new string[] {"a{0,0x10}}}"}),
       new RegexTestCase(@"^[abcd]{0,16}*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]{1,}*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]{1}*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]{0,16}?*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]{1,}?*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]{1}?*$", typeof(ArgumentException)),

       new RegexTestCase(@"^[abcd]*+$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]+*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]?*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]*?+$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]+?*$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]??*$", typeof(ArgumentException)),

       new RegexTestCase(@"^[abcd]*{0,5}$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]+{0,5}$", typeof(ArgumentException)),
       new RegexTestCase(@"^[abcd]?{0,5}$", typeof(ArgumentException)),

        /*********************************************************
        Lazy operator Backtracking
        *********************************************************/
       new RegexTestCase(@"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/",  RegexOptions.IgnoreCase, "http://www.msn.com", null),
       new RegexTestCase(@"http://([a-zA-z0-9\-]*\.?)*?(:[0-9]*)??/",  RegexOptions.IgnoreCase, "http://www.msn.com/", new string[] {"http://www.msn.com/", "com", string.Empty}),
       new RegexTestCase(@"http://([a-zA-Z0-9\-]*\.?)*?/", RegexOptions.IgnoreCase, @"http://www.google.com/", new string[] {"http://www.google.com/", "com"}),

       new RegexTestCase(@"([a-z]*?)([\w])",  RegexOptions.IgnoreCase, "cat", new string[] {"c", string.Empty, "c"}),
       new RegexTestCase(@"^([a-z]*?)([\w])$",  RegexOptions.IgnoreCase, "cat", new string[] {"cat", "ca", "t"}),

        // TODO: Come up with more scenarios here

        /*********************************************************
        Backtracking
        *********************************************************/
       new RegexTestCase(@"([a-z]*)([\w])",  RegexOptions.IgnoreCase, "cat", new string[] {"cat", "ca", "t"}),
       new RegexTestCase(@"^([a-z]*)([\w])$",  RegexOptions.IgnoreCase, "cat", new string[] {"cat", "ca", "t"}),

        // TODO: Come up with more scenarios here

        /*********************************************************
        Character Escapes Invalid Regular Expressions
        *********************************************************/
       new RegexTestCase(@"\u", typeof(ArgumentException)),
       new RegexTestCase(@"\ua", typeof(ArgumentException)),
       new RegexTestCase(@"\u0", typeof(ArgumentException)),
       new RegexTestCase(@"\x", typeof(ArgumentException)),
       new RegexTestCase(@"\x2", typeof(ArgumentException)),


        /*********************************************************
        Character class Invalid Regular Expressions
        *********************************************************/
       new RegexTestCase(@"[", typeof(ArgumentException)),
       new RegexTestCase(@"[]", typeof(ArgumentException)),
       new RegexTestCase(@"[a", typeof(ArgumentException)),
       new RegexTestCase(@"[^", typeof(ArgumentException)),
       new RegexTestCase(@"[cat", typeof(ArgumentException)),
       new RegexTestCase(@"[^cat", typeof(ArgumentException)),
       new RegexTestCase(@"[a-", typeof(ArgumentException)),
       new RegexTestCase(@"[a-]+", "ba-b", new string[] {"a-"}),
       new RegexTestCase(@"\p{", typeof(ArgumentException)),
       new RegexTestCase(@"\p{cat", typeof(ArgumentException)),
       new RegexTestCase(@"\p{cat}", typeof(ArgumentException)),
       new RegexTestCase(@"\P{", typeof(ArgumentException)),
       new RegexTestCase(@"\P{cat", typeof(ArgumentException)),
       new RegexTestCase(@"\P{cat}", typeof(ArgumentException)),

        /*********************************************************
        Quantifiers
        *********************************************************/
       new RegexTestCase(@"(cat){", "cat{", new string[] {"cat{", "cat"}),
       new RegexTestCase(@"(cat){}", "cat{}", new string[] {"cat{}", "cat"}),
       new RegexTestCase(@"(cat){,", "cat{,", new string[] {"cat{,", "cat"}),
       new RegexTestCase(@"(cat){,}", "cat{,}", new string[] {"cat{,}", "cat"}),
       new RegexTestCase(@"(cat){cat}", "cat{cat}", new string[] {"cat{cat}", "cat"}),
       new RegexTestCase(@"(cat){cat,5}", "cat{cat,5}", new string[] {"cat{cat,5}", "cat"}),
       new RegexTestCase(@"(cat){5,dog}", "cat{5,dog}", new string[] {"cat{5,dog}", "cat"}),
       new RegexTestCase(@"(cat){cat,dog}", "cat{cat,dog}", new string[] {"cat{cat,dog}", "cat"}),
       new RegexTestCase(@"(cat){,}?", "cat{,}?", new string[] {"cat{,}", "cat"}),
       new RegexTestCase(@"(cat){cat}?", "cat{cat}?", new string[] {"cat{cat}", "cat"}),
       new RegexTestCase(@"(cat){cat,5}?", "cat{cat,5}?", new string[] {"cat{cat,5}", "cat"}),
       new RegexTestCase(@"(cat){5,dog}?", "cat{5,dog}?", new string[] {"cat{5,dog}", "cat"}),
       new RegexTestCase(@"(cat){cat,dog}?", "cat{cat,dog}?", new string[] {"cat{cat,dog}", "cat"}),

        /*********************************************************
        Grouping Constructs Invalid Regular Expressions
        *********************************************************/
       new RegexTestCase(@"(", typeof(ArgumentException)),
       new RegexTestCase(@"(?", typeof(ArgumentException)),
       new RegexTestCase(@"(?<", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>", typeof(ArgumentException)),
       new RegexTestCase(@"(?'", typeof(ArgumentException)),
       new RegexTestCase(@"(?'cat'", typeof(ArgumentException)),
       new RegexTestCase(@"(?:", typeof(ArgumentException)),
       new RegexTestCase(@"(?imn", typeof(ArgumentException)),
       new RegexTestCase(@"(?imn )", typeof(ArgumentException)),
       new RegexTestCase(@"(?=", typeof(ArgumentException)),
       new RegexTestCase(@"(?!", typeof(ArgumentException)),
       new RegexTestCase(@"(?<=", typeof(ArgumentException)),
       new RegexTestCase(@"(?<!", typeof(ArgumentException)),
       new RegexTestCase(@"(?>", typeof(ArgumentException)),

       new RegexTestCase(@"()", "cat", new string[] { string.Empty, string.Empty}),
       new RegexTestCase(@"(?)", typeof(ArgumentException)),
       new RegexTestCase(@"(?<)", typeof(ArgumentException)),
       new RegexTestCase(@"(?<cat>)", "cat", new string[] { string.Empty, string.Empty}),
       new RegexTestCase(@"(?')", typeof(ArgumentException)),
       new RegexTestCase(@"(?'cat')", "cat", new string[] { string.Empty, string.Empty}),
       new RegexTestCase(@"(?:)", "cat", new string[] { string.Empty}),
       new RegexTestCase(@"(?imn)", "cat", new string[] { string.Empty}),
       new RegexTestCase(@"(?imn)cat", "(?imn)cat", new string[] {"cat"}),
       new RegexTestCase(@"(?=)", "cat", new string[] { string.Empty}),
       new RegexTestCase(@"(?!)", "(?!)cat"),
       new RegexTestCase(@"(?<=)", "cat", new string[] { string.Empty}),
       new RegexTestCase(@"(?<!)", "(?<!)cat"),
       new RegexTestCase(@"(?>)", "cat", new string[] { string.Empty}),

        /*********************************************************
        Grouping Constructs Invalid Regular Expressions
        *********************************************************/
       new RegexTestCase(@"\1", typeof(ArgumentException)),
       new RegexTestCase(@"\1", typeof(ArgumentException)),
       new RegexTestCase(@"\k", typeof(ArgumentException)),
       new RegexTestCase(@"\k<", typeof(ArgumentException)),
       new RegexTestCase(@"\k<1", typeof(ArgumentException)),
       new RegexTestCase(@"\k<cat", typeof(ArgumentException)),
       new RegexTestCase(@"\k<>", typeof(ArgumentException)),

        /*********************************************************
        Alternation construct Invalid Regular Expressions
        *********************************************************/
       new RegexTestCase(@"(?(", typeof(ArgumentException)),
       new RegexTestCase(@"(?()|", typeof(ArgumentException)),
       new RegexTestCase(@"(?()|)", "(?()|)", new string[] {""}),

       new RegexTestCase(@"(?(cat", typeof(ArgumentException)),
       new RegexTestCase(@"(?(cat)|", typeof(ArgumentException)),

       new RegexTestCase(@"(?(cat)|)", "cat", new string[] {""}),
       new RegexTestCase(@"(?(cat)|)", "dog", new string[] {""}),

       new RegexTestCase(@"(?(cat)catdog|)", "catdog", new string[] {"catdog"}),
       new RegexTestCase(@"(?(cat)catdog|)", "dog", new string[] {""}),
       new RegexTestCase(@"(?(cat)dog|)", "dog", new string[] {""}),
       new RegexTestCase(@"(?(cat)dog|)", "cat", new string[] {""}),

       new RegexTestCase(@"(?(cat)|catdog)", "cat", new string[] {""}),
       new RegexTestCase(@"(?(cat)|catdog)", "catdog", new string[] {""}),
       new RegexTestCase(@"(?(cat)|dog)", "dog", new string[] {"dog"}),
       new RegexTestCase(@"(?(cat)|dog)", "oof"),

        /*********************************************************
        Empty Match
        *********************************************************/
       new RegexTestCase(@"([a*]*)+?$", "ab", new string[] {"", ""}),
       new RegexTestCase(@"(a*)+?$", "b", new string[] {"", ""}),
    };
}
