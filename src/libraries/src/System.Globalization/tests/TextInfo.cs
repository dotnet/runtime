// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public class TextInfoTests
{
    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("fr")]
    public static void ToUpper(string localeName)
    {
        TextInfo ti = new CultureInfo(localeName).TextInfo;

        Assert.Equal('A', ti.ToUpper('a'));
        Assert.Equal("ABC", ti.ToUpper("abc"));
        Assert.Equal('A', ti.ToUpper('A'));
        Assert.Equal("ABC", ti.ToUpper("ABC"));

        Assert.Equal("THIS IS A LONGER TEST CASE", ti.ToUpper("this is a longer test case"));
        Assert.Equal("THIS IS A LONGER MIXED CASE TEST CASE", ti.ToUpper("this Is A LONGER mIXEd casE test case"));

        Assert.Equal("THIS \t HAS \t SOME \t TABS", ti.ToUpper("this \t HaS \t somE \t TABS"));

        Assert.Equal('1', ti.ToUpper('1'));
        Assert.Equal("123", ti.ToUpper("123"));

        Assert.Equal("EMBEDDED\0NULL\0BYTE\0", ti.ToUpper("embedded\0NuLL\0Byte\0"));

        // LATIN SMALL LETTER O WITH ACUTE, which has an upper case variant.
        Assert.Equal('\u00D3', ti.ToUpper('\u00F3'));
        Assert.Equal("\u00D3", ti.ToUpper("\u00F3"));

        // SNOWMAN, which does not have an upper case variant.
        Assert.Equal('\u2603', ti.ToUpper('\u2603'));
        Assert.Equal("\u2603", ti.ToUpper("\u2603"));

        // DESERT SMALL LETTER LONG I has an upperc case variant.
        Assert.Equal("\U00010400", ti.ToUpper("\U00010428"));

        // RAINBOW (outside the BMP and does not case)
        Assert.Equal("\U0001F308", ti.ToLower("\U0001F308"));

        // These are cases where we have invalid UTF-16 in a string (mismatched surrogate pairs).  They should be
        // unchanged by casing.
        Assert.Equal("BE CAREFUL, \uD83C\uD83C, THIS ONE IS TRICKY", ti.ToUpper("be careful, \uD83C\uD83C, this one is tricky"));
        Assert.Equal("BE CAREFUL, \uDF08\uD83C, THIS ONE IS TRICKY", ti.ToUpper("be careful, \uDF08\uD83C, this one is tricky"));
        Assert.Equal("BE CAREFUL, \uDF08\uDF08, THIS ONE IS TRICKY", ti.ToUpper("be careful, \uDF08\uDF08, this one is tricky"));

        Assert.Throws<ArgumentNullException>(() => ti.ToUpper(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("fr")]
    public static void ToLower(string localeName)
    {
        TextInfo ti = new CultureInfo(localeName).TextInfo;

        Assert.Equal('a', ti.ToLower('A'));
        Assert.Equal("abc", ti.ToLower("ABC"));
        Assert.Equal('a', ti.ToLower('a'));
        Assert.Equal("abc", ti.ToLower("abc"));

        Assert.Equal("this is a longer test case", ti.ToLower("THIS IS A LONGER TEST CASE"));
        Assert.Equal("this is a longer mixed case test case", ti.ToLower("this Is A LONGER mIXEd casE test case"));

        Assert.Equal("this \t has \t some \t tabs", ti.ToLower("THIS \t hAs \t SOMe \t tabs"));

        Assert.Equal('1', ti.ToLower('1'));
        Assert.Equal("123", ti.ToLower("123"));

        Assert.Equal("embedded\0null\0byte\0", ti.ToLower("EMBEDDED\0NuLL\0Byte\0"));

        // LATIN CAPITAL LETTER O WITH ACUTE, which has a lower case variant.
        Assert.Equal('\u00F3', ti.ToLower('\u00D3'));
        Assert.Equal("\u00F3", ti.ToLower("\u00D3"));

        // SNOWMAN, which does not have a lower case variant.
        Assert.Equal('\u2603', ti.ToLower('\u2603'));
        Assert.Equal("\u2603", ti.ToLower("\u2603"));

        // DESERT CAPITAL LETTER LONG I has a lower case variant.
        Assert.Equal("\U00010428", ti.ToLower("\U00010400"));

        // RAINBOW (outside the BMP and does not case)
        Assert.Equal("\U0001F308", ti.ToLower("\U0001F308"));

        // These are cases where we have invalid UTF-16 in a string (mismatched surrogate pairs).  They should be
        // unchanged by casing.
        Assert.Equal("be careful, \uD83C\uD83C, this one is tricky", ti.ToLower("BE CAREFUL, \uD83C\uD83C, THIS ONE IS TRICKY"));
        Assert.Equal("be careful, \uDF08\uD83C, this one is tricky", ti.ToLower("BE CAREFUL, \uDF08\uD83C, THIS ONE IS TRICKY"));
        Assert.Equal("be careful, \uDF08\uDF08, this one is tricky", ti.ToLower("BE CAREFUL, \uDF08\uDF08, THIS ONE IS TRICKY"));

        Assert.Throws<ArgumentNullException>(() => ti.ToLower(null));
    }
    
    [Theory]
    [InlineData("tr")]
    [InlineData("tr-TR")]
    [InlineData("az")]
    [InlineData("az-Latn-AZ")]
    public static void TurkishICasing(string localeName)
    {
        TextInfo ti = new CultureInfo(localeName).TextInfo;

        Assert.Equal('i', ti.ToLower('\u0130'));
        Assert.Equal("i", ti.ToLower("\u0130"));

        Assert.Equal('\u0130', ti.ToUpper('i'));
        Assert.Equal("\u0130", ti.ToUpper("i"));

        Assert.Equal('\u0131', ti.ToLower('I'));
        Assert.Equal("\u0131", ti.ToLower("I"));

        Assert.Equal('I', ti.ToUpper('\u0131'));
        Assert.Equal("I", ti.ToUpper("\u0131"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public static void NoUnicodeSpecalCases(string localeName)
    {
        // Unicode defines some codepoints which expand into multiple codepoints
        // when cased (see SpecialCasing.txt from UNIDATA for some examples).  We have never done
        // these sorts of expansions, since it would cause string lengths to change when cased,
        // which is non-intuative.  In addition, there are some context sensitive mappings which
        // we also don't preform.
        
        TextInfo ti = new CultureInfo(localeName).TextInfo;

        // es-zed does not case to SS when upercased.
        Assert.Equal("\u00DF", ti.ToUpper("\u00DF"));

        // Ligatures do not expand when cased.
        Assert.Equal("\uFB00", ti.ToUpper("\uFB00"));

        // Precomposed character with no uppercase variaint, we don't want to "decompose" this
        // as part of casing.
        Assert.Equal("\u0149", ti.ToUpper("\u0149"));

        // Greek Capital Letter Sigma (does not to case to U+03C2 with "final sigma" rule).
        Assert.Equal("\u03C3", ti.ToLower("\u03A3"));
    }
}
