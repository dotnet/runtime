// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization.Tests;
using Xunit;

namespace GB18030.Tests;

[SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
public class CharTests
{
    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void Convert(CharUnicodeInfoTestCase testCase)
    {
        Assert.Equal(testCase.CodePoint, char.ConvertToUtf32(char.ConvertFromUtf32(testCase.CodePoint), 0));

        string utf32String = testCase.Utf32CodeValue;
        if (char.IsSurrogate(utf32String[0]))
        {
            Assert.Equal(2, utf32String.Length);
            Assert.Equal(testCase.CodePoint, char.ConvertToUtf32(utf32String[0], utf32String[1]));
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void Parse(CharUnicodeInfoTestCase testCase)
    {
        string utf32String = testCase.Utf32CodeValue;
        if (utf32String.Length > 1)
        {
            Assert.False(char.TryParse(utf32String, out _));
            return;
        }

        char c = char.Parse(utf32String);
        Assert.Equal(testCase.CodePoint, c);

        bool succeed = char.TryParse(utf32String, out c);
        Assert.True(succeed);
        Assert.Equal(testCase.CodePoint, c);
    }

    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void IsSurrogate(CharUnicodeInfoTestCase testCase)
    {
        string utf32String = testCase.Utf32CodeValue;
        if (utf32String.Length > 1)
        {
            Assert.Equal(2, utf32String.Length);
            char high = utf32String[0];
            char low = utf32String[1];
            Assert.True(char.IsSurrogate(low));
            Assert.True(char.IsSurrogate(high));
            Assert.True(char.IsLowSurrogate(low));
            Assert.True(char.IsHighSurrogate(high));
            Assert.True(char.IsSurrogatePair(high, low));
        }
        else
        {
            char c = utf32String[0];
            Assert.False(char.IsSurrogate(c));
            Assert.False(char.IsLowSurrogate(c));
            Assert.False(char.IsHighSurrogate(c));
            Assert.False(char.IsSurrogatePair(c, c));
        }
    }

    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void IsLetter(CharUnicodeInfoTestCase testCase)
    {
        string utf32String = testCase.Utf32CodeValue;
        Assert.True(char.IsLetter(utf32String, 0));
        Assert.True(char.IsLetterOrDigit(utf32String, 0));

        if (utf32String.Length < 2)
        {
            Assert.True(char.IsLetter(utf32String[0]));
            Assert.True(char.IsLetterOrDigit(utf32String[0]));
        }
    }


    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void IsNonLetter_False(CharUnicodeInfoTestCase testCase)
    {
        string utf32String = testCase.Utf32CodeValue;
        Assert.False(char.IsControl(utf32String, 0));
        Assert.False(char.IsDigit(utf32String, 0));
        Assert.False(char.IsLower(utf32String, 0));
        Assert.False(char.IsNumber(utf32String, 0));
        Assert.False(char.IsPunctuation(utf32String, 0));
        Assert.False(char.IsSeparator(utf32String, 0));
        Assert.False(char.IsSymbol(utf32String, 0));
        Assert.False(char.IsUpper(utf32String, 0));
        Assert.False(char.IsWhiteSpace(utf32String, 0));

        if (utf32String.Length < 2)
        {
            char c = utf32String[0];
#if !NETFRAMEWORK
            Assert.False(char.IsAscii(c));
            Assert.False(char.IsAsciiDigit(c));
            Assert.False(char.IsAsciiHexDigit(c));
            Assert.False(char.IsAsciiHexDigitLower(c));
            Assert.False(char.IsAsciiHexDigitUpper(c));
            Assert.False(char.IsAsciiLetter(c));
            Assert.False(char.IsAsciiLetterOrDigit(c));
            Assert.False(char.IsAsciiLetterLower(c));
            Assert.False(char.IsAsciiLetterUpper(c));
#endif
            Assert.False(char.IsControl(c));
            Assert.False(char.IsDigit(c));
            Assert.False(char.IsLower(c));
            Assert.False(char.IsNumber(c));
            Assert.False(char.IsPunctuation(c));
            Assert.False(char.IsSeparator(c));
            Assert.False(char.IsSymbol(c));
            Assert.False(char.IsUpper(c));
            Assert.False(char.IsWhiteSpace(c));
        }
    }
}
