// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal class NumberFormatterBase
    {
        protected const int MaxAlphabeticValue = int.MaxValue;     // Maximum value that can be represented
        private const int MaxAlphabeticLength = 7;                // Number of letters needed to represent the maximum value

        public static void ConvertToAlphabetic(StringBuilder sb, double val, char firstChar, int totalChars)
        {
            Debug.Assert(1 <= val && val <= MaxAlphabeticValue);
            Debug.Assert(Math.Pow(totalChars, MaxAlphabeticLength) >= MaxAlphabeticValue);

            char[] letters = new char[MaxAlphabeticLength];
            int idx = MaxAlphabeticLength;
            int number = (int)val;

            while (number > totalChars)
            {
                int quot = --number / totalChars;
                letters[--idx] = (char)(firstChar + (number - quot * totalChars));
                number = quot;
            }
            letters[--idx] = (char)(firstChar + --number);
            sb.Append(letters, idx, MaxAlphabeticLength - idx);
        }

        protected const int MaxRomanValue = 32767;
        private const string RomanDigitsUC = "IIVIXXLXCCDCM";
        private const string RomanDigitsLC = "iivixxlxccdcm";

        //                            RomanDigit       = { I  IV   V  IX   X  XL   L  XC    C   CD    D   CM     M }
        private static readonly int[] s_romanDigitValue = { 1, 4, 5, 9, 10, 40, 50, 90, 100, 400, 500, 900, 1000 };

        public static void ConvertToRoman(StringBuilder sb, double val, bool upperCase)
        {
            Debug.Assert(1 <= val && val <= MaxRomanValue);

            int number = (int)val;
            string digits = upperCase ? RomanDigitsUC : RomanDigitsLC;

            for (int idx = s_romanDigitValue.Length; idx-- != 0;)
            {
                while (number >= s_romanDigitValue[idx])
                {
                    number -= s_romanDigitValue[idx];
                    sb.Append(digits, idx, 1 + (idx & 1));
                }
            }
        }

        // Most of tables here were taken from MSXML sources and compared with the last
        // CSS3 proposal available at http://www.w3.org/TR/2002/WD-css3-lists-20021107/

        // MSXML-, CSS3+
        // CSS3 inserts two new characters U+3090, U+3091 before U+3092
        private const string hiraganaAiueo =
            "\u3042\u3044\u3046\u3048\u304a\u304b\u304d\u304f\u3051\u3053" +
            "\u3055\u3057\u3059\u305b\u305d\u305f\u3061\u3064\u3066\u3068" +
            "\u306a\u306b\u306c\u306d\u306e\u306f\u3072\u3075\u3078\u307b" +
            "\u307e\u307f\u3080\u3081\u3082\u3084\u3086\u3088\u3089\u308a" +
            "\u308b\u308c\u308d\u308f\u3092\u3093";

        // MSXML-, CSS3+
        private const string hiraganaIroha =
            "\u3044\u308d\u306f\u306b\u307b\u3078\u3068\u3061\u308a\u306c" +
            "\u308b\u3092\u308f\u304b\u3088\u305f\u308c\u305d\u3064\u306d" +
            "\u306a\u3089\u3080\u3046\u3090\u306e\u304a\u304f\u3084\u307e" +
            "\u3051\u3075\u3053\u3048\u3066\u3042\u3055\u304d\u3086\u3081" +
            "\u307f\u3057\u3091\u3072\u3082\u305b\u3059";

        // MSXML+, CSS3+
        // CSS3 inserts two new characters U+30F0, U+30F1 before U+30F2
        private const string katakanaAiueo =
            "\u30a2\u30a4\u30a6\u30a8\u30aa\u30ab\u30ad\u30af\u30b1\u30b3" +
            "\u30b5\u30b7\u30b9\u30bb\u30bd\u30bf\u30c1\u30c4\u30c6\u30c8" +
            "\u30ca\u30cb\u30cc\u30cd\u30ce\u30cf\u30d2\u30d5\u30d8\u30db" +
            "\u30de\u30df\u30e0\u30e1\u30e2\u30e4\u30e6\u30e8\u30e9\u30ea" +
            "\u30eb\u30ec\u30ed\u30ef\u30f2\u30f3";

        // MSXML+, CSS3+
        // CSS3 removes last U+30F3 character
        private const string katakanaIroha =
            "\u30a4\u30ed\u30cf\u30cb\u30db\u30d8\u30c8\u30c1\u30ea\u30cc" +
            "\u30eb\u30f2\u30ef\u30ab\u30e8\u30bf\u30ec\u30bd\u30c4\u30cd" +
            "\u30ca\u30e9\u30e0\u30a6\u30f0\u30ce\u30aa\u30af\u30e4\u30de" +
            "\u30b1\u30d5\u30b3\u30a8\u30c6\u30a2\u30b5\u30ad\u30e6\u30e1" +
            "\u30df\u30b7\u30f1\u30d2\u30e2\u30bb\u30b9\u30f3";

        // MSXML+, CSS3-
        private const string katakanaAiueoHw =
            "\uff71\uff72\uff73\uff74\uff75\uff76\uff77\uff78\uff79\uff7a" +
            "\uff7b\uff7c\uff7d\uff7e\uff7f\uff80\uff81\uff82\uff83\uff84" +
            "\uff85\uff86\uff87\uff88\uff89\uff8a\uff8b\uff8c\uff8d\uff8e" +
            "\uff8f\uff90\uff91\uff92\uff93\uff94\uff95\uff96\uff97\uff98" +
            "\uff99\uff9a\uff9b\uff9c\uff66\uff9d";

        // MSXML+, CSS3-
        private const string katakanaIrohaHw =
            "\uff72\uff9b\uff8a\uff86\uff8e\uff8d\uff84\uff81\uff98\uff87" +
            "\uff99\uff66\uff9c\uff76\uff96\uff80\uff9a\uff7f\uff82\uff88" +
            "\uff85\uff97\uff91\uff73\u30f0\uff89\uff75\uff78\uff94\uff8f" +
            "\uff79\uff8c\uff7a\uff74\uff83\uff71\uff7b\uff77\uff95\uff92" +
            "\uff90\uff7c\u30f1\uff8b\uff93\uff7e\uff7d\uff9d";

        // MSXML+, CSS3-
        // Unicode 4.0.0 spec: When used to represent numbers in decimal notation, zero
        // is represented by U+3007. Otherwise, zero is represented by U+96F6.
        private const string cjkIdeographic =
            "\u3007\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d";
    }
}
