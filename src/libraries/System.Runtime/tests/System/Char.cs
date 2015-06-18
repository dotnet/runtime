// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
public static class CharTests
{
    [Fact]
    public static void TestCompareTo()
    {
        // Int32 Char.CompareTo(Char)
        char h = 'h';
        Assert.True(h.CompareTo('h') == 0);
        Assert.True(h.CompareTo('a') > 0);
        Assert.True(h.CompareTo('z') < 0);
    }

    [Fact]
    public static void TestSystemIComparableCompareTo()
    {
        // Int32 Char.System.IComparable.CompareTo(Object)
        IComparable h = 'h';
        Assert.True(h.CompareTo('h') == 0);
        Assert.True(h.CompareTo('a') > 0);
        Assert.True(h.CompareTo('z') < 0);
        Assert.True(h.CompareTo(null) > 0);

        Assert.Throws<ArgumentException>(() => h.CompareTo("H"));
    }

    private static void ValidateConvertFromUtf32(int i, string expected)
    {
        try
        {
            string s = char.ConvertFromUtf32(i);
            Assert.Equal(expected, s);
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.True(expected == null, "Expected an ArgumentOutOfRangeException");
        }
    }

    [Fact]
    public static void TestConvertFromUtf32()
    {
        // String Char.ConvertFromUtf32(Int32)
        ValidateConvertFromUtf32(0x10000, "\uD800\uDC00");
        ValidateConvertFromUtf32(0x103FF, "\uD800\uDFFF");
        ValidateConvertFromUtf32(0xFFFFF, "\uDBBF\uDFFF");
        ValidateConvertFromUtf32(0x10FC00, "\uDBFF\uDC00");
        ValidateConvertFromUtf32(0x10FFFF, "\uDBFF\uDFFF");
        ValidateConvertFromUtf32(0, "\0");
        ValidateConvertFromUtf32(0x3FF, "\u03FF");
        ValidateConvertFromUtf32(0xE000, "\uE000");
        ValidateConvertFromUtf32(0xFFFF, "\uFFFF");
        ValidateConvertFromUtf32(0xD800, null);
        ValidateConvertFromUtf32(0xDC00, null);
        ValidateConvertFromUtf32(0xDFFF, null);
        ValidateConvertFromUtf32(0x110000, null);
        ValidateConvertFromUtf32(-1, null);
        ValidateConvertFromUtf32(Int32.MaxValue, null);
        ValidateConvertFromUtf32(Int32.MinValue, null);
    }

    private static void ValidateconverToUtf32<T>(string s, int i, int expected) where T : Exception
    {
        try
        {
            int result = char.ConvertToUtf32(s, i);
            Assert.Equal(result, expected);
        }
        catch (T)
        {
            Assert.True(expected == Int32.MinValue, "Expected an exception to be thrown");
        }
    }

    [Fact]
    public static void TestConvertToUtf32StrInt()
    {
        // Int32 Char.ConvertToUtf32(String, Int32)
        ValidateconverToUtf32<Exception>("\uD800\uDC00", 0, 0x10000);
        ValidateconverToUtf32<Exception>("\uD800\uD800\uDFFF", 1, 0x103FF);
        ValidateconverToUtf32<Exception>("\uDBBF\uDFFF", 0, 0xFFFFF);
        ValidateconverToUtf32<Exception>("\uDBFF\uDC00", 0, 0x10FC00);
        ValidateconverToUtf32<Exception>("\uDBFF\uDFFF", 0, 0x10FFFF);
        // Not surrogate pairs
        ValidateconverToUtf32<Exception>("\u0000\u0001", 0, 0);
        ValidateconverToUtf32<Exception>("\u0000\u0001", 1, 1);
        ValidateconverToUtf32<Exception>("\u0000", 0, 0);
        ValidateconverToUtf32<Exception>("\u0020\uD7FF", 0, 32);
        ValidateconverToUtf32<Exception>("\u0020\uD7FF", 1, 0xD7FF);
        ValidateconverToUtf32<Exception>("abcde", 4, (int)'e');
        ValidateconverToUtf32<Exception>("\uD800\uD7FF", 1, 0xD7FF);  // high, non-surrogate
        ValidateconverToUtf32<Exception>("\uD800\u0000", 1, 0);  // high, non-surrogate
        ValidateconverToUtf32<Exception>("\uDF01\u0000", 1, 0);  // low, non-surrogate
        // Invalid inputs
        ValidateconverToUtf32<ArgumentException>("\uD800\uD800", 0, Int32.MinValue);  // high, high
        ValidateconverToUtf32<ArgumentException>("\uD800\uD7FF", 0, Int32.MinValue);  // high, non-surrogate
        ValidateconverToUtf32<ArgumentException>("\uD800\u0000", 0, Int32.MinValue);  // high, non-surrogate
        ValidateconverToUtf32<ArgumentException>("\uDC01\uD940", 0, Int32.MinValue);  // low, high
        ValidateconverToUtf32<ArgumentException>("\uDD00\uDE00", 0, Int32.MinValue);  // low, low
        ValidateconverToUtf32<ArgumentException>("\uDF01\u0000", 0, Int32.MinValue);  // low, non-surrogate
        ValidateconverToUtf32<ArgumentException>("\uD800\uD800", 1, Int32.MinValue);  // high, high
        ValidateconverToUtf32<ArgumentException>("\uDC01\uD940", 1, Int32.MinValue);  // low, high
        ValidateconverToUtf32<ArgumentException>("\uDD00\uDE00", 1, Int32.MinValue);  // low, low
        ValidateconverToUtf32<ArgumentNullException>(null, 0, Int32.MinValue);  // null string
        ValidateconverToUtf32<ArgumentOutOfRangeException>("", 0, Int32.MinValue);  // index out of range
        ValidateconverToUtf32<ArgumentOutOfRangeException>("", -1, Int32.MinValue);  // index out of range
        ValidateconverToUtf32<ArgumentOutOfRangeException>("abcde", -1, Int32.MinValue);  // index out of range
        ValidateconverToUtf32<ArgumentOutOfRangeException>("abcde", 5, Int32.MinValue);  // index out of range
    }

    private static void ValidateconverToUtf32<T>(char c1, char c2, int expected) where T : Exception
    {
        try
        {
            int result = char.ConvertToUtf32(c1, c2);
            Assert.Equal(result, expected);
        }
        catch (T)
        {
            Assert.True(expected == Int32.MinValue, "Expected an exception to be thrown");
        }
    }

    [Fact]
    public static void TestConvertToUtf32()
    {
        // Int32 Char.ConvertToUtf32(Char, Char)
        ValidateconverToUtf32<Exception>('\uD800', '\uDC00', 0x10000);
        ValidateconverToUtf32<Exception>('\uD800', '\uDFFF', 0x103FF);
        ValidateconverToUtf32<Exception>('\uDBBF', '\uDFFF', 0xFFFFF);
        ValidateconverToUtf32<Exception>('\uDBFF', '\uDC00', 0x10FC00);
        ValidateconverToUtf32<Exception>('\uDBFF', '\uDFFF', 0x10FFFF);

        ValidateconverToUtf32<ArgumentOutOfRangeException>('\uD800', '\uD800', Int32.MinValue);  // high, high
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\uD800', '\uD7FF', Int32.MinValue);  // high, non-surrogate
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\uD800', '\u0000', Int32.MinValue);  // high, non-surrogate
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\uDC01', '\uD940', Int32.MinValue);  // low, high
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\uDD00', '\uDE00', Int32.MinValue);  // low, low
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\uDF01', '\u0000', Int32.MinValue);  // low, non-surrogate
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\u0032', '\uD7FF', Int32.MinValue);  // both non-surrogate
        ValidateconverToUtf32<ArgumentOutOfRangeException>('\u0000', '\u0000', Int32.MinValue);  // both non-surrogate
    }

    [Fact]
    public static void TestEquals()
    {
        // Boolean Char.Equals(Char)
        char a = 'a';

        Assert.True(a.Equals('a'));
        Assert.False(a.Equals('b'));
        Assert.False(a.Equals('A'));
    }

    [Fact]
    public static void TestEqualsObj()
    {
        // Boolean Char.Equals(Object)
        char a = 'a';
        Assert.True(a.Equals((object)'a'));
        Assert.False(a.Equals((object)'b'));
        Assert.False(a.Equals((object)'A'));
        Assert.False(a.Equals(null));

        int i = (int)'a';
        Assert.False(a.Equals(i));
        Assert.False(a.Equals("a"));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        // Int32 Char.GetHashCode()
        char a = 'a';
        char b = 'b';
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public static void TestGetNumericValueStrInt()
    {
        Assert.Equal(Char.GetNumericValue("\uD800\uDD07", 0), 1);
        Assert.Equal(Char.GetNumericValue("9", 0), 9);
        Assert.Equal(Char.GetNumericValue("Test 7", 5), 7);
        Assert.Equal(Char.GetNumericValue("T", 0), -1);
    }

    [Fact]
    public static void TestGetNumericValue()
    {
        Assert.Equal(Char.GetNumericValue('9'), 9);
        Assert.Equal(Char.GetNumericValue('z'), -1);
    }

    [Fact]
    public static void TestIsControl()
    {
        // Boolean Char.IsControl(Char)
        foreach (var c in GetTestChars(UnicodeCategory.Control))
            Assert.True(char.IsControl(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.Control))
            Assert.False(char.IsControl(c));
    }

    [Fact]
    public static void TestIsControlStrInt()
    {
        // Boolean Char.IsControl(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.Control))
            Assert.True(char.IsControl(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.Control))
            Assert.False(char.IsControl(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsControl(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsControl("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsControl("abc", 4));
    }

    [Fact]
    public static void TestIsDigit()
    {
        // Boolean Char.IsDigit(Char)
        foreach (var c in GetTestChars(UnicodeCategory.DecimalDigitNumber))
            Assert.True(char.IsDigit(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.DecimalDigitNumber))
            Assert.False(char.IsDigit(c));
    }

    [Fact]
    public static void TestIsDigitStrInt()
    {
        // Boolean Char.IsDigit(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.DecimalDigitNumber))
            Assert.True(char.IsDigit(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.DecimalDigitNumber))
            Assert.False(char.IsDigit(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsDigit(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsDigit("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsDigit("abc", 4));
    }

    [Fact]
    public static void TestIsLetter()
    {
        // Boolean Char.IsLetter(Char)
        foreach (var c in GetTestChars(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter))
            Assert.True(char.IsLetter(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter))
            Assert.False(char.IsLetter(c));
    }

    [Fact]
    public static void TestIsLetterStrInt()
    {
        // Boolean Char.IsLetter(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter))
            Assert.True(char.IsLetter(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter))
            Assert.False(char.IsLetter(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsLetter(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLetter("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLetter("abc", 4));
    }

    [Fact]
    public static void TestIsLetterOrDigit()
    {
        // Boolean Char.IsLetterOrDigit(Char)
        foreach (var c in GetTestChars(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter,
                UnicodeCategory.DecimalDigitNumber))
            Assert.True(char.IsLetterOrDigit(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter,
                UnicodeCategory.DecimalDigitNumber))
            Assert.False(char.IsLetterOrDigit(c));
    }

    [Fact]
    public static void TestIsLetterOrDigitStrInt()
    {
        // Boolean Char.IsLetterOrDigit(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter,
                UnicodeCategory.DecimalDigitNumber))
            Assert.True(char.IsLetterOrDigit(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.ModifierLetter,
                UnicodeCategory.OtherLetter,
                UnicodeCategory.DecimalDigitNumber))
            Assert.False(char.IsLetterOrDigit(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsLetterOrDigit(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLetterOrDigit("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLetterOrDigit("abc", 4));
    }

    [Fact]
    public static void TestIsLower()
    {
        // Boolean Char.IsLower(Char)
        foreach (var c in GetTestChars(UnicodeCategory.LowercaseLetter))
            Assert.True(char.IsLower(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter))
            Assert.False(char.IsLower(c));
    }

    [Fact]
    public static void TestIsLowerStrInt()
    {
        // Boolean Char.IsLower(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.LowercaseLetter))
            Assert.True(char.IsLower(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter))
            Assert.False(char.IsLower(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsLower(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLower("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLower("abc", 4));
    }

    [Fact]
    public static void TestIsNumber()
    {
        // Boolean Char.IsNumber(Char)
        foreach (var c in GetTestChars(UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber, UnicodeCategory.OtherNumber))
            Assert.True(char.IsNumber(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber, UnicodeCategory.OtherNumber))
            Assert.False(char.IsNumber(c));
    }

    [Fact]
    public static void TestIsNumberStrInt()
    {
        // Boolean Char.IsNumber(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber, UnicodeCategory.OtherNumber))
            Assert.True(char.IsNumber(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber, UnicodeCategory.OtherNumber))
            Assert.False(char.IsNumber(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsNumber(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsNumber("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsNumber("abc", 4));
    }

    [Fact]
    public static void TestIsPunctuation()
    {
        // Boolean Char.IsPunctuation(Char)
        foreach (var c in GetTestChars(UnicodeCategory.ConnectorPunctuation,
              UnicodeCategory.DashPunctuation,
              UnicodeCategory.OpenPunctuation,
              UnicodeCategory.ClosePunctuation,
              UnicodeCategory.InitialQuotePunctuation,
              UnicodeCategory.FinalQuotePunctuation,
              UnicodeCategory.OtherPunctuation))
            Assert.True(char.IsPunctuation(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.ConnectorPunctuation,
              UnicodeCategory.DashPunctuation,
              UnicodeCategory.OpenPunctuation,
              UnicodeCategory.ClosePunctuation,
              UnicodeCategory.InitialQuotePunctuation,
              UnicodeCategory.FinalQuotePunctuation,
              UnicodeCategory.OtherPunctuation))
            Assert.False(char.IsPunctuation(c));
    }

    [Fact]
    public static void TestIsPunctuationStrInt()
    {
        // Boolean Char.IsPunctuation(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.ConnectorPunctuation,
              UnicodeCategory.DashPunctuation,
              UnicodeCategory.OpenPunctuation,
              UnicodeCategory.ClosePunctuation,
              UnicodeCategory.InitialQuotePunctuation,
              UnicodeCategory.FinalQuotePunctuation,
              UnicodeCategory.OtherPunctuation))
            Assert.True(char.IsPunctuation(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.ConnectorPunctuation,
              UnicodeCategory.DashPunctuation,
              UnicodeCategory.OpenPunctuation,
              UnicodeCategory.ClosePunctuation,
              UnicodeCategory.InitialQuotePunctuation,
              UnicodeCategory.FinalQuotePunctuation,
              UnicodeCategory.OtherPunctuation))
            Assert.False(char.IsPunctuation(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsPunctuation(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsPunctuation("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsPunctuation("abc", 4));
    }

    [Fact]
    public static void TestIsSeparator()
    {
        // Boolean Char.IsSeparator(Char)
        foreach (var c in GetTestChars(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
            Assert.True(char.IsSeparator(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
            Assert.False(char.IsSeparator(c));
    }

    [Fact]
    public static void TestIsSeparatorStrInt()
    {
        // Boolean Char.IsSeparator(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
            Assert.True(char.IsSeparator(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
            Assert.False(char.IsSeparator(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsSeparator(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsSeparator("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsSeparator("abc", 4));
    }

    [Fact]
    public static void TestIsLowSurrogate()
    {
        // Boolean Char.IsLowSurrogate(Char)
        foreach (char c in s_lowSurrogates)
            Assert.True(char.IsLowSurrogate(c));

        foreach (char c in s_highSurrogates)
            Assert.False(char.IsLowSurrogate(c));

        foreach (char c in s_nonSurrogates)
            Assert.False(char.IsLowSurrogate(c));
    }

    [Fact]
    public static void TestIsLowSurrogateStrInt()
    {
        // Boolean Char.IsLowSurrogate(String, Int32)
        foreach (char c in s_lowSurrogates)
            Assert.True(char.IsLowSurrogate(c.ToString(), 0));

        foreach (char c in s_highSurrogates)
            Assert.False(char.IsLowSurrogate(c.ToString(), 0));

        foreach (char c in s_nonSurrogates)
            Assert.False(char.IsLowSurrogate(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsLowSurrogate(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLowSurrogate("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsLowSurrogate("abc", 4));
    }

    [Fact]
    public static void TestIsHighSurrogate()
    {
        // Boolean Char.IsHighSurrogate(Char)
        foreach (char c in s_highSurrogates)
            Assert.True(char.IsHighSurrogate(c));

        foreach (char c in s_lowSurrogates)
            Assert.False(char.IsHighSurrogate(c));

        foreach (char c in s_nonSurrogates)
            Assert.False(char.IsHighSurrogate(c));
    }

    [Fact]
    public static void TestIsHighSurrogateStrInt()
    {
        // Boolean Char.IsHighSurrogate(String, Int32)
        foreach (char c in s_highSurrogates)
            Assert.True(char.IsHighSurrogate(c.ToString(), 0));

        foreach (char c in s_lowSurrogates)
            Assert.False(char.IsHighSurrogate(c.ToString(), 0));

        foreach (char c in s_nonSurrogates)
            Assert.False(char.IsHighSurrogate(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsHighSurrogate(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsHighSurrogate("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsHighSurrogate("abc", 4));
    }

    [Fact]
    public static void TestIsSurrogate()
    {
        // Boolean Char.IsSurrogate(Char)
        foreach (char c in s_highSurrogates)
            Assert.True(char.IsSurrogate(c));

        foreach (char c in s_lowSurrogates)
            Assert.True(char.IsSurrogate(c));

        foreach (char c in s_nonSurrogates)
            Assert.False(char.IsSurrogate(c));
    }

    [Fact]
    public static void TestIsSurrogateStrInt()
    {
        // Boolean Char.IsSurrogate(String, Int32)
        foreach (char c in s_highSurrogates)
            Assert.True(char.IsSurrogate(c.ToString(), 0));

        foreach (char c in s_lowSurrogates)
            Assert.True(char.IsSurrogate(c.ToString(), 0));

        foreach (char c in s_nonSurrogates)
            Assert.False(char.IsSurrogate(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsSurrogate(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsSurrogate("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsSurrogate("abc", 4));
    }

    [Fact]
    public static void TestIsSurrogatePair()
    {
        // Boolean Char.IsSurrogatePair(Char, Char)
        foreach (char hs in s_highSurrogates)
            foreach (char ls in s_lowSurrogates)
                Assert.True(Char.IsSurrogatePair(hs, ls));

        foreach (char hs in s_nonSurrogates)
            foreach (char ls in s_lowSurrogates)
                Assert.False(Char.IsSurrogatePair(hs, ls));

        foreach (char hs in s_highSurrogates)
            foreach (char ls in s_nonSurrogates)
                Assert.False(Char.IsSurrogatePair(hs, ls));
    }

    [Fact]
    public static void TestIsSurrogatePairStrInt()
    {
        // Boolean Char.IsSurrogatePair(String, Int32)
        foreach (char hs in s_highSurrogates)
            foreach (char ls in s_lowSurrogates)
                Assert.True(Char.IsSurrogatePair(hs.ToString() + ls, 0));

        foreach (char hs in s_nonSurrogates)
            foreach (char ls in s_lowSurrogates)
                Assert.False(Char.IsSurrogatePair(hs.ToString() + ls, 0));

        foreach (char hs in s_highSurrogates)
            foreach (char ls in s_nonSurrogates)
                Assert.False(Char.IsSurrogatePair(hs.ToString() + ls, 0));
    }

    [Fact]
    public static void TestIsSymbol()
    {
        // Boolean Char.IsSymbol(Char)
        foreach (var c in GetTestChars(UnicodeCategory.MathSymbol,
                UnicodeCategory.ModifierSymbol,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.OtherSymbol))
            Assert.True(char.IsSymbol(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.MathSymbol,
                UnicodeCategory.ModifierSymbol,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.OtherSymbol))
            Assert.False(char.IsSymbol(c));
    }

    [Fact]
    public static void TestIsSymbolStrInt()
    {
        // Boolean Char.IsSymbol(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.MathSymbol,
                UnicodeCategory.ModifierSymbol,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.OtherSymbol))
            Assert.True(char.IsSymbol(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.MathSymbol,
                UnicodeCategory.ModifierSymbol,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.OtherSymbol))
            Assert.False(char.IsSymbol(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsSymbol(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsSymbol("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsSymbol("abc", 4));
    }

    [Fact]
    public static void TestIsUpper()
    {
        // Boolean Char.IsUpper(Char)
        foreach (var c in GetTestChars(UnicodeCategory.UppercaseLetter))
            Assert.True(char.IsUpper(c));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter))
            Assert.False(char.IsUpper(c));
    }

    [Fact]
    public static void TestIsUpperStrInt()
    {
        // Boolean Char.IsUpper(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.UppercaseLetter))
            Assert.True(char.IsUpper(c.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter))
            Assert.False(char.IsUpper(c.ToString(), 0));

        Assert.Throws<ArgumentNullException>(() => char.IsUpper(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsUpper("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsUpper("abc", 4));
    }

    [Fact]
    public static void TestIsWhiteSpace()
    {
        // Boolean Char.IsWhiteSpace(Char)
        foreach (var c in GetTestChars(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
            Assert.True(char.IsWhiteSpace(c));

        // Some control chars are also considered whitespace for legacy reasons.
        //if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085')
        Assert.True(char.IsWhiteSpace('\u000b'));
        Assert.True(char.IsWhiteSpace('\u0085'));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
        {
            // Need to special case some control chars that are treated as whitespace
            if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085') continue;
            Assert.False(char.IsWhiteSpace(c));
        }
    }

    [Fact]
    public static void TestIsWhiteSpaceStrInt()
    {
        // Boolean Char.IsWhiteSpace(String, Int32)
        foreach (var c in GetTestChars(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
            Assert.True(char.IsWhiteSpace(c.ToString(), 0));

        // Some control chars are also considered whitespace for legacy reasons.
        //if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085')
        Assert.True(char.IsWhiteSpace('\u000b'.ToString(), 0));
        Assert.True(char.IsWhiteSpace('\u0085'.ToString(), 0));

        foreach (var c in GetTestCharsNotInCategory(UnicodeCategory.SpaceSeparator,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.ParagraphSeparator))
        {
            // Need to special case some control chars that are treated as whitespace
            if ((c >= '\x0009' && c <= '\x000d') || c == '\x0085') continue;
            Assert.False(char.IsWhiteSpace(c.ToString(), 0));
        }

        Assert.Throws<ArgumentNullException>(() => char.IsWhiteSpace(null, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsWhiteSpace("abc", -1));

        Assert.Throws<ArgumentOutOfRangeException>(() => char.IsWhiteSpace("abc", 4));
    }

    [Fact]
    public static void TestMaxValue()
    {
        // Char Char.MaxValue
        Assert.Equal(0xffff, char.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        // Char Char.MinValue
        Assert.Equal(0, char.MinValue);
    }

    [Fact]
    public static void TestToLower()
    {
        // Char Char.ToLower(Char)
        Assert.Equal('a', char.ToLower('A'));
        Assert.Equal('a', char.ToLower('a'));

        foreach (char c in GetTestChars(UnicodeCategory.UppercaseLetter))
        {
            char lc = char.ToLower(c);
            Assert.NotEqual(c, lc);
            Assert.True(char.IsLower(lc));
        }

        // TitlecaseLetter can have a lower case form (e.g. \u01C8 'Lj' letter which will be 'lj')
        // LetterNumber can have a lower case form (e.g. \u2162 'III' letter which will be 'iii')
        foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
        {
            char lc = char.ToLower(c);
            Assert.Equal(c, lc);
        }
    }

    [Fact]
    public static void TestToLowerInvariant()
    {
        // Char Char.ToLowerInvariant(Char)
        Assert.Equal('a', char.ToLowerInvariant('A'));
        Assert.Equal('a', char.ToLowerInvariant('a'));

        foreach (char c in GetTestChars(UnicodeCategory.UppercaseLetter))
        {
            char lc = char.ToLowerInvariant(c);
            Assert.NotEqual(c, lc);
            Assert.True(char.IsLower(lc));
        }

        // TitlecaseLetter can have a lower case form (e.g. \u01C8 'Lj' letter which will be 'lj')
        // LetterNumber can have a lower case form (e.g. \u2162 'III' letter which will be 'iii')
        foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.UppercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
        {
            char lc = char.ToLowerInvariant(c);
            Assert.Equal(c, lc);
        }
    }

    [Fact]
    public static void TestToString()
    {
        // String Char.ToString()
        Assert.Equal(new string('a', 1), 'a'.ToString());
        Assert.Equal(new string('\uabcd', 1), '\uabcd'.ToString());
    }

    [Fact]
    public static void TestToStringChar()
    {
        // String Char.ToString(Char)
        Assert.Equal(new string('a', 1), char.ToString('a'));
        Assert.Equal(new string('\uabcd', 1), char.ToString('\uabcd'));
    }

    [Fact]
    public static void TestToUpper()
    {
        // Char Char.ToUpper(Char)
        Assert.Equal('A', char.ToUpper('A'));
        Assert.Equal('A', char.ToUpper('a'));

        foreach (char c in GetTestChars(UnicodeCategory.LowercaseLetter))
        {
            char lc = char.ToUpper(c);
            Assert.NotEqual(c, lc);
            Assert.True(char.IsUpper(lc));
        }

        // TitlecaseLetter can have a uppercase form (e.g. \u01C8 'Lj' letter which will be 'LJ')
        // LetterNumber can have a uppercase form (e.g. \u2172 'iii' letter which will be 'III')
        foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
        {
            char lc = char.ToUpper(c);
            Assert.Equal(c, lc);
        }
    }

    [Fact]
    public static void TestToUpperInvariant()
    {
        // Char Char.ToUpperInvariant(Char)
        Assert.Equal('A', char.ToUpperInvariant('A'));
        Assert.Equal('A', char.ToUpperInvariant('a'));

        foreach (char c in GetTestChars(UnicodeCategory.LowercaseLetter))
        {
            char lc = char.ToUpperInvariant(c);
            Assert.NotEqual(c, lc);
            Assert.True(char.IsUpper(lc));
        }

        // TitlecaseLetter can have a uppercase form (e.g. \u01C8 'Lj' letter which will be 'LJ')
        // LetterNumber can have a uppercase form (e.g. \u2172 'iii' letter which will be 'III')
        foreach (char c in GetTestCharsNotInCategory(UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.LetterNumber))
        {
            char lc = char.ToUpperInvariant(c);
            Assert.Equal(c, lc);
        }
    }

    private static void ValidateTryParse(string s, char expected, bool shouldSucceed)
    {
        char result;
        Assert.Equal(shouldSucceed, char.TryParse(s, out result));
        if (shouldSucceed)
            Assert.Equal(expected, result);
    }

    [Fact]
    public static void TestTryParse()
    {
        // Boolean Char.TryParse(String, Char)
        ValidateTryParse("a", 'a', true);
        ValidateTryParse("4", '4', true);
        ValidateTryParse(" ", ' ', true);
        ValidateTryParse("\n", '\n', true);
        ValidateTryParse("\0", '\0', true);
        ValidateTryParse("\u0135", '\u0135', true);
        ValidateTryParse("\u05d9", '\u05d9', true);
        ValidateTryParse("\ud801", '\ud801', true);  // high surrogate
        ValidateTryParse("\udc01", '\udc01', true);  // low surrogate
        ValidateTryParse("\ue001", '\ue001', true);  // private use codepoint

        // Fail cases
        ValidateTryParse(null, '\0', false);
        ValidateTryParse("", '\0', false);
        ValidateTryParse("\n\r", '\0', false);
        ValidateTryParse("kj", '\0', false);
        ValidateTryParse(" a", '\0', false);
        ValidateTryParse("a ", '\0', false);
        ValidateTryParse("\\u0135", '\0', false);
        ValidateTryParse("\u01356", '\0', false);
        ValidateTryParse("\ud801\udc01", '\0', false);   // surrogate pair
    }

    private static IEnumerable<char> GetTestCharsNotInCategory(params UnicodeCategory[] categories)
    {
        Assert.Equal(s_latinTestSet.Length, s_unicodeTestSet.Length);

        for (int i = 0; i < s_latinTestSet.Length; i++)
        {
            if (Array.Exists(categories, uc => uc == (UnicodeCategory)i))
                continue;

            char[] latinSet = s_latinTestSet[i];
            for (int j = 0; j < latinSet.Length; j++)
                yield return latinSet[j];

            char[] unicodeSet = s_unicodeTestSet[i];
            for (int k = 0; k < unicodeSet.Length; k++)
                yield return unicodeSet[k];
        }
    }

    private static IEnumerable<char> GetTestChars(params UnicodeCategory[] categories)
    {
        for (int i = 0; i < categories.Length; i++)
        {
            char[] latinSet = s_latinTestSet[(int)categories[i]];
            for (int j = 0; j < latinSet.Length; j++)
                yield return latinSet[j];

            char[] unicodeSet = s_unicodeTestSet[(int)categories[i]];
            for (int k = 0; k < unicodeSet.Length; k++)
                yield return unicodeSet[k];
        }
    }

    private static char[][] s_latinTestSet = new char[][] {
        new char[] {'\u0047','\u004c','\u0051','\u0056','\u00c0','\u00c5','\u00ca','\u00cf','\u00d4','\u00da'}, // UnicodeCategory.UppercaseLetter
        new char[] {'\u0062','\u0068','\u006e','\u0074','\u007a','\u00e1','\u00e7','\u00ed','\u00f3','\u00fa'}, // UnicodeCategory.LowercaseLetter
        new char[] {}, // UnicodeCategory.TitlecaseLetter
        new char[] {}, // UnicodeCategory.ModifierLetter
        new char[] {}, // UnicodeCategory.OtherLetter
        new char[] {}, // UnicodeCategory.NonSpacingMark
        new char[] {}, // UnicodeCategory.SpacingCombiningMark
        new char[] {}, // UnicodeCategory.EnclosingMark
        new char[] {'\u0030','\u0031','\u0032','\u0033','\u0034','\u0035','\u0036','\u0037','\u0038','\u0039'}, // UnicodeCategory.DecimalDigitNumber
        new char[] {}, // UnicodeCategory.LetterNumber
        new char[] {'\u00b2','\u00b3','\u00b9','\u00bc','\u00bd','\u00be'}, // UnicodeCategory.OtherNumber
        new char[] {'\u0020','\u00a0'}, // UnicodeCategory.SpaceSeparator
        new char[] {}, // UnicodeCategory.LineSeparator
        new char[] {}, // UnicodeCategory.ParagraphSeparator
        new char[] {'\u0005','\u000b','\u0011','\u0017','\u001d','\u0082','\u0085','\u008e','\u0094','\u009a'}, // UnicodeCategory.Control
        new char[] {}, // UnicodeCategory.Format
        new char[] {}, // UnicodeCategory.Surrogate
        new char[] {}, // UnicodeCategory.PrivateUse
        new char[] {'\u005f'}, // UnicodeCategory.ConnectorPunctuation
        new char[] {'\u002d','\u00ad'}, // UnicodeCategory.DashPunctuation
        new char[] {'\u0028','\u005b','\u007b'}, // UnicodeCategory.OpenPunctuation
        new char[] {'\u0029','\u005d','\u007d'}, // UnicodeCategory.ClosePunctuation
        new char[] {'\u00ab'}, // UnicodeCategory.InitialQuotePunctuation
        new char[] {'\u00bb'}, // UnicodeCategory.FinalQuotePunctuation
        new char[] {'\u002e','\u002f','\u003a','\u003b','\u003f','\u0040','\u005c','\u00a1','\u00b7','\u00bf'}, // UnicodeCategory.OtherPunctuation
        new char[] {'\u002b','\u003c','\u003d','\u003e','\u007c','\u007e','\u00ac','\u00b1','\u00d7','\u00f7'}, // UnicodeCategory.MathSymbol
        new char[] {'\u0024','\u00a2','\u00a3','\u00a4','\u00a5'}, // UnicodeCategory.CurrencySymbol
        new char[] {'\u005e','\u0060','\u00a8','\u00af','\u00b4','\u00b8'}, // UnicodeCategory.ModifierSymbol
        new char[] {'\u00a6','\u00a7','\u00a9','\u00ae','\u00b0','\u00b6'}, // UnicodeCategory.OtherSymbol
        new char[] {}, // UnicodeCategory.OtherNotAssigned
    };

    private static char[][] s_unicodeTestSet = new char[][] {
        new char[] {'\u0102','\u01ac','\u0392','\u0428','\u0508','\u10c4','\u1eb4','\u1fba','\u2c28','\ua668'}, // UnicodeCategory.UppercaseLetter
        new char[] { '\u0107', '\u012D', '\u0140', '\u0151', '\u013A', '\u01A1', '\u01F9', '\u022D', '\u1E09','\uFF45' }, // UnicodeCategory.LowercaseLetter
        new char[] {'\u01c8','\u1f88','\u1f8b','\u1f8e','\u1f99','\u1f9c','\u1f9f','\u1faa','\u1fad','\u1fbc'}, // UnicodeCategory.TitlecaseLetter
        new char[] {'\u02b7','\u02cd','\u07f4','\u1d2f','\u1d41','\u1d53','\u1d9d','\u1daf','\u2091','\u30fe'}, // UnicodeCategory.ModifierLetter
        new char[] {'\u01c0','\u37be','\u4970','\u5b6c','\u6d1e','\u7ed0','\u9082','\ua271','\ub985','\ucb37'}, // UnicodeCategory.OtherLetter
        new char[] {'\u0303','\u034e','\u05b5','\u0738','\u0a4d','\u0e49','\u0fad','\u180b','\u1dd5','\u2dfd'}, // UnicodeCategory.NonSpacingMark
        new char[] {'\u0982','\u0b03','\u0c41','\u0d40','\u0df3','\u1083','\u1925','\u19b9','\u1b44','\ua8b5'}, // UnicodeCategory.SpacingCombiningMark
        new char[] {'\u20dd','\u20de','\u20df','\u20e0','\u20e2','\u20e3','\u20e4','\ua670','\ua671','\ua672'}, // UnicodeCategory.EnclosingMark
        new char[] {'\u0660','\u0966','\u0ae6','\u0c66','\u0e50','\u1040','\u1810','\u1b50','\u1c50','\ua900'}, // UnicodeCategory.DecimalDigitNumber
        new char[] {'\u2162','\u2167','\u216c','\u2171','\u2176','\u217b','\u2180','\u2187','\u3023','\u3028'}, // UnicodeCategory.LetterNumber
        new char[] {'\u0c78','\u136b','\u17f7','\u2158','\u2471','\u248a','\u24f1','\u2780','\u3220','\u3280'}, // UnicodeCategory.OtherNumber
        new char[] {'\u2004','\u2005','\u2006','\u2007','\u2008','\u2009','\u200a','\u202f','\u205f','\u3000'}, // UnicodeCategory.SpaceSeparator
        new char[] {'\u2028'}, // UnicodeCategory.LineSeparator
        new char[] {'\u2029'}, // UnicodeCategory.ParagraphSeparator
        new char[] {}, // UnicodeCategory.Control
        new char[] {'\u0603','\u17b4','\u200c','\u200f','\u202c','\u2060','\u2063','\u206b','\u206e','\ufff9'}, // UnicodeCategory.Format
        new char[] {'\ud808','\ud8d4','\ud9a0','\uda6c','\udb38','\udc04','\udcd0','\udd9c','\ude68','\udf34'}, // UnicodeCategory.Surrogate
        new char[] {'\ue000','\ue280','\ue500','\ue780','\uea00','\uec80','\uef00','\uf180','\uf400','\uf680'}, // UnicodeCategory.PrivateUse
        new char[] {'\u203f','\u2040','\u2054','\ufe33','\ufe34','\ufe4d','\ufe4e','\ufe4f','\uff3f'}, // UnicodeCategory.ConnectorPunctuation
        new char[] {'\u2e17','\u2e1a','\u301c','\u3030','\u30a0','\ufe31','\ufe32','\ufe58','\ufe63','\uff0d'}, // UnicodeCategory.DashPunctuation
        new char[] {'\u2768','\u2774','\u27ee','\u298d','\u29d8','\u2e28','\u3014','\ufe17','\ufe3f','\ufe5d'}, // UnicodeCategory.OpenPunctuation
        new char[] {'\u276b','\u27c6','\u2984','\u2990','\u29db','\u3009','\u3017','\ufe18','\ufe40','\ufe5e'}, // UnicodeCategory.ClosePunctuation
        new char[] {'\u201b','\u201c','\u201f','\u2039','\u2e02','\u2e04','\u2e09','\u2e0c','\u2e1c','\u2e20'}, // UnicodeCategory.InitialQuotePunctuation
        new char[] {'\u2019','\u201d','\u203a','\u2e03','\u2e05','\u2e0a','\u2e0d','\u2e1d','\u2e21'}, // UnicodeCategory.FinalQuotePunctuation
        new char[] {'\u0589','\u0709','\u0f10','\u16ec','\u1b5b','\u2034','\u2058','\u2e16','\ua8cf','\ufe55'}, // UnicodeCategory.OtherPunctuation
        new char[] {'\u2052','\u2234','\u2290','\u22ec','\u27dd','\u2943','\u29b5','\u2a17','\u2a73','\u2acf'}, // UnicodeCategory.MathSymbol
        new char[] {'\u17db','\u20a2','\u20a5','\u20a8','\u20ab','\u20ae','\u20b1','\u20b4','\ufe69','\uffe1'}, // UnicodeCategory.CurrencySymbol
        new char[] {'\u02c5','\u02da','\u02e8','\u02f3','\u02fc','\u1fc0','\u1fee','\ua703','\ua70c','\ua715'}, // UnicodeCategory.ModifierSymbol
        new char[] {'\u0bf3','\u2316','\u24ac','\u25b2','\u26af','\u285c','\u2e8f','\u2f8c','\u3292','\u3392'}, // UnicodeCategory.OtherSymbol
        new char[] {'\u037f','\u09c6','\u0dfa','\u2e5c','\ua9f9','\uabbd'}, // UnicodeCategory.OtherNotAssigned
    };

    private static char[] s_highSurrogates = new char[] { '\ud800', '\udaaa', '\udbff' }; // range from '\ud800' to '\udbff'
    private static char[] s_lowSurrogates = new char[] { '\udc00', '\udeee', '\udfff' }; // range from '\udc00' to '\udfff'
    private static char[] s_nonSurrogates = new char[] { '\u0000', '\ud7ff', '\ue000', '\uffff' };
}
