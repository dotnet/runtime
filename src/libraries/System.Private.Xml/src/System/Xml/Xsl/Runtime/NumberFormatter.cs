// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Xml.Xsl.Runtime
{
    internal static class CharUtil
    {
        // Checks whether a given character is alphanumeric.  Alphanumeric means any character that has
        // a Unicode category of Nd (8), Nl (9), No (10), Lu (0), Ll (1), Lt (2), Lm (3) or Lo (4)
        // <spec>http://www.w3.org/TR/xslt.html#convert</spec>
        public static bool IsAlphaNumeric(char ch)
        {
            int category = (int)Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            return category <= 4 || (category <= 10 && category >= 8);
        }

        // Checks whether a given character has decimal digit value of 1.  The decimal digits are characters
        // having the Unicode category of Nd (8).  NOTE: We do not support Tamil and Ethiopic numbering systems
        // having no zeros.
        public static bool IsDecimalDigitOne(char ch)
        {
            int category = (int)Globalization.CharUnicodeInfo.GetUnicodeCategory(--ch);
            return category == 8 && char.GetNumericValue(ch) == 0;
        }
    }

    internal enum NumberingSequence
    {
        Nil = -1,
        FirstDecimal,
        Arabic = FirstDecimal,      // 0x0031 -- 1, 2, 3, 4, ...
        DArabic,                    // 0xff11 -- Combines DbChar w/ Arabic
        Hindi3,                     // 0x0967 -- Hindi numbers
        Thai2,                      // 0x0e51 -- Thai numbers
        FEDecimal,                  // 0x4e00 -- FE numbering style (decimal numbers)
        KorDbNum1,                  // 0xc77c -- Korea (decimal)
        LastNum = KorDbNum1,

        // Alphabetic numbering sequences (do not change order unless you also change _rgnfcToLab's order)
        FirstAlpha,
        UCLetter = FirstAlpha,      // 0x0041 -- A, B, C, D, ...
        LCLetter,                   // 0x0061 -- a, b, c, d, ...
        UCRus,                      // 0x0410 -- Upper case Russian alphabet
        LCRus,                      // 0x0430 -- Lower case Russian alphabet
        Thai1,                      // 0x0e01 -- Thai letters
        Hindi1,                     // 0x0915 -- Hindi vowels
        Hindi2,                     // 0x0905 -- Hindi consonants
        Aiueo,                      // 0xff71 -- Japan numbering style (SbChar)
        DAiueo,                     // 0x30a2 -- Japan - Combines DbChar w/ Aiueo
        Iroha,                      // 0xff72 -- Japan numbering style (SbChar)
        DIroha,                     // 0x30a4 -- Japan - Combines DbChar w/ Iroha//  New defines for 97...
        DChosung,                   // 0x3131 -- Korea Chosung (DbChar)
        Ganada,                     // 0xac00 -- Korea
        ArabicScript,               // 0x0623 -- BIDI AraAlpha for Arabic/Persian/Urdu
        LastAlpha = ArabicScript,

        // Special numbering sequences (includes peculiar alphabetic and numeric sequences)
        FirstSpecial,
        UCRoman = FirstSpecial,     // 0x0049 -- I, II, III, IV, ...
        LCRoman,                    // 0x0069 -- i, ii, iii, iv, ...
        Hebrew,                     // 0x05d0 -- BIDI Heb1 for Hebrew
        DbNum3,                     // 0x58f1 -- FE numbering style (similar to China2, some different characters)
        ChnCmplx,                   // 0x58f9 -- China (complex, traditional chinese, spell out numbers)
        KorDbNum3,                  // 0xd558 -- Korea (1-99)
        Zodiac1,                    // 0x7532 -- CJK-heavenly-stem (10 numbers)
        Zodiac2,                    // 0x5b50 -- CJK-earthly-branch (12 numbers)
        Zodiac3,                    // 0x7532 -- (Zodiac1 + Zodiac2 Combination)
        LastSpecial = Zodiac3,
    }

    internal abstract class NumberFormatterBase
    {
        protected const int MaxAlphabeticValue = int.MaxValue;     // Maximum value that can be represented
        private const int MaxAlphabeticLength = 7;                // Number of letters needed to represent the maximum value

        public static void ConvertToAlphabetic(StringBuilder sb, double val, char firstChar, int totalChars)
        {
            Debug.Assert(1 <= val && val <= MaxAlphabeticValue);
            Debug.Assert(Math.Pow(totalChars, MaxAlphabeticLength) >= MaxAlphabeticValue);

            Span<char> letters = stackalloc char[MaxAlphabeticLength];
            int idx = MaxAlphabeticLength;
            int number = (int)val;

            while (number > totalChars)
            {
                int quot = --number / totalChars;
                letters[--idx] = (char)(firstChar + (number - quot * totalChars));
                number = quot;
            }
            letters[--idx] = (char)(firstChar + --number);
            sb.Append(letters.Slice(idx, MaxAlphabeticLength - idx));
        }

        protected const int MaxRomanValue = 32767;
        private const string RomanDigitsUC = "IIVIXXLXCCDCM";
        private const string RomanDigitsLC = "iivixxlxccdcm";

        public static void ConvertToRoman(StringBuilder sb, double val, bool upperCase)
        {
            Debug.Assert(1 <= val && val <= MaxRomanValue);

            int number = (int)val;
            string digits = upperCase ? RomanDigitsUC : RomanDigitsLC;

            //                RomanDigit      ={ I IV  V IX   X  XL   L  XC    C   CD    D   CM     M }
            ReadOnlySpan<int> RomanDigitValue = [1, 4, 5, 9, 10, 40, 50, 90, 100, 400, 500, 900, 1000];

            for (int idx = RomanDigitValue.Length; idx-- != 0;)
            {
                while (number >= RomanDigitValue[idx])
                {
                    number -= RomanDigitValue[idx];
                    sb.Append(digits, idx, 1 + (idx & 1));
                }
            }
        }
    }
}
