// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class CharMiscTests
    {
        private static readonly UnicodeCategory[] s_categoryForLatin1 = 
        {
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0000 - 0007
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0008 - 000F
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0010 - 0017
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0018 - 001F
            UnicodeCategory.SpaceSeparator, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.CurrencySymbol, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation,    // 0020 - 0027
            UnicodeCategory.OpenPunctuation, UnicodeCategory.ClosePunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.OtherPunctuation, UnicodeCategory.DashPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation,    // 0028 - 002F
            UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber,    // 0030 - 0037
            UnicodeCategory.DecimalDigitNumber, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.MathSymbol, UnicodeCategory.MathSymbol, UnicodeCategory.OtherPunctuation,    // 0038 - 003F
            UnicodeCategory.OtherPunctuation, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 0040 - 0047
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 0048 - 004F
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 0050 - 0057
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.OpenPunctuation, UnicodeCategory.OtherPunctuation, UnicodeCategory.ClosePunctuation, UnicodeCategory.ModifierSymbol, UnicodeCategory.ConnectorPunctuation,    // 0058 - 005F
            UnicodeCategory.ModifierSymbol, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 0060 - 0067
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 0068 - 006F
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 0070 - 0077
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.OpenPunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.ClosePunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.Control,    // 0078 - 007F
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0080 - 0087
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0088 - 008F
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0090 - 0097
            UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control, UnicodeCategory.Control,    // 0098 - 009F
            UnicodeCategory.SpaceSeparator, UnicodeCategory.OtherPunctuation, UnicodeCategory.CurrencySymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.OtherSymbol, UnicodeCategory.OtherSymbol,    // 00A0 - 00A7
            UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherSymbol, UnicodeCategory.LowercaseLetter, UnicodeCategory.InitialQuotePunctuation, UnicodeCategory.MathSymbol, UnicodeCategory.DashPunctuation, UnicodeCategory.OtherSymbol, UnicodeCategory.ModifierSymbol,    // 00A8 - 00AF
            UnicodeCategory.OtherSymbol, UnicodeCategory.MathSymbol, UnicodeCategory.OtherNumber, UnicodeCategory.OtherNumber, UnicodeCategory.ModifierSymbol, UnicodeCategory.LowercaseLetter, UnicodeCategory.OtherSymbol, UnicodeCategory.OtherPunctuation,    // 00B0 - 00B7
            UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherNumber, UnicodeCategory.LowercaseLetter, UnicodeCategory.FinalQuotePunctuation, UnicodeCategory.OtherNumber, UnicodeCategory.OtherNumber, UnicodeCategory.OtherNumber, UnicodeCategory.OtherPunctuation,    // 00B8 - 00BF
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 00C0 - 00C7
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter,    // 00C8 - 00CF
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.MathSymbol,    // 00D0 - 00D7
            UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter,    // 00D8 - 00DF
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 00E0 - 00E7
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 00E8 - 00EF
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.MathSymbol,    // 00F0 - 00F7
            UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.LowercaseLetter,    // 00F8 - 00FF
        };

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower          upper    Culture
            yield return new object[] { 'a',          'A',      "en-US" };
            yield return new object[] { 'i',          'I',      "en-US" };
            yield return new object[] { '\u0131',     'I',      "tr-TR" };
            yield return new object[] { 'i',          '\u0130', "tr-TR" };
            yield return new object[] { '\u0660',     '\u0660', "en-US" };
        }

        [Fact]
        public static void LatinRangeTest()
        {
            StringBuilder sb = new StringBuilder(256);
            string latineString = sb.ToString();

            for (int i=0; i < latineString.Length; i++)
            {
                Assert.Equal(s_categoryForLatin1[i], Char.GetUnicodeCategory(latineString[i]));
                Assert.Equal(s_categoryForLatin1[i], Char.GetUnicodeCategory(latineString, i));
            }
        }

        [Fact]
        public static void NonLatinRangeTest()
        {
            for (int i=256; i <= 0xFFFF; i++)
            {
                Assert.Equal(CharUnicodeInfo.GetUnicodeCategory((char)i), Char.GetUnicodeCategory((char)i));
            }

            string nonLatinString = "\u0100\u0200\u0300\u0400\u0500\u0600\u0700\u0800\u0900\u0A00\u0B00\u0C00\u0D00\u0E00\u0F00" + 
                                    "\u1000\u2000\u3000\u4000\u5000\u6000\u7000\u8000\u9000\uA000\uB000\uC000\uD000\uE000\uF000";
            for (int i=0; i < nonLatinString.Length; i++)
            {
                Assert.Equal(CharUnicodeInfo.GetUnicodeCategory(nonLatinString[i]), Char.GetUnicodeCategory(nonLatinString, i));
            }
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CasingTest(char lowerForm, char upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(lowerForm, char.ToLower(upperForm, ci));
            Assert.Equal(upperForm, char.ToUpper(lowerForm, ci));
        }
    }
}