// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public partial class CharUnicodeInfoTests
    {
        private const int HIGHEST_CODE_POINT = 0x10_FFFF;

        [Fact]
        public void GetUnicodeCategory()
        {
            foreach (CharUnicodeInfoTestCase testCase in CharUnicodeInfoTestData.TestCases)
            {
                if (testCase.Utf32CodeValue.Length == 1)
                {
                    // Test the char overload for a single char
                    GetUnicodeCategoryTest_Char(testCase.Utf32CodeValue[0], testCase.GeneralCategory);
                }
                // Test the string overload for a surrogate pair or a single char
                GetUnicodeCategoryTest_String(testCase.Utf32CodeValue, new UnicodeCategory[] { testCase.GeneralCategory });
                Assert.Equal(testCase.GeneralCategory, CharUnicodeInfo.GetUnicodeCategory(testCase.CodePoint));
            }
        }

        [Theory]
        [InlineData('\uFFFF', UnicodeCategory.OtherNotAssigned)]
        public void GetUnicodeCategoryTest_Char(char ch, UnicodeCategory expected)
        {
            UnicodeCategory actual = CharUnicodeInfo.GetUnicodeCategory(ch);
            Assert.True(actual == expected, ErrorMessage(ch, expected, actual));
        }

        [Theory]
        [InlineData("aA1!", new UnicodeCategory[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherPunctuation })]
        [InlineData("\uD808\uDF6C", new UnicodeCategory[] { UnicodeCategory.OtherLetter, UnicodeCategory.Surrogate })]
        [InlineData("a\uD808\uDF6Ca", new UnicodeCategory[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.OtherLetter, UnicodeCategory.Surrogate, UnicodeCategory.LowercaseLetter })]
        public void GetUnicodeCategoryTest_String(string s, UnicodeCategory[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], CharUnicodeInfo.GetUnicodeCategory(s, i));
            }
        }

        [Fact]
        public void GetUnicodeCategory_String_InvalidSurrogatePairs()
        {
            // High, high surrogate pair
            GetUnicodeCategoryTest_String("\uD808\uD808", new UnicodeCategory[] { UnicodeCategory.Surrogate, UnicodeCategory.Surrogate });

            // Low, low surrogate pair
            GetUnicodeCategoryTest_String("\uDF6C\uDF6C", new UnicodeCategory[] { UnicodeCategory.Surrogate, UnicodeCategory.Surrogate });

            // Low, high surrogate pair
            GetUnicodeCategoryTest_String("\uDF6C\uD808", new UnicodeCategory[] { UnicodeCategory.Surrogate, UnicodeCategory.Surrogate });
        }

        [Fact]
        public void GetUnicodeCategory_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => CharUnicodeInfo.GetUnicodeCategory(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetUnicodeCategory("abc", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetUnicodeCategory("abc", 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetUnicodeCategory("", 0));
        }

        [Fact]
        public void GetNumericValue()
        {
            foreach (CharUnicodeInfoTestCase testCase in CharUnicodeInfoTestData.TestCases)
            {
                if (testCase.Utf32CodeValue.Length == 1)
                {
                    // Test the char overload for a single char
                    GetNumericValueTest_Char(testCase.Utf32CodeValue[0], testCase.NumericValue);
                }
                // Test the string overload for a surrogate pair
                GetNumericValueTest_String(testCase.Utf32CodeValue, new double[] { testCase.NumericValue });
            }
        }

        [Theory]
        [InlineData('\uFFFF', -1)]
        public void GetNumericValueTest_Char(char ch, double expected)
        {
            double actual = CharUnicodeInfo.GetNumericValue(ch);
            Assert.True(expected == actual, ErrorMessage(ch, expected, actual));
        }

        [Theory]
        [MemberData(nameof(s_GetNumericValueData))]
        public void GetNumericValueTest_String(string s, double[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], CharUnicodeInfo.GetNumericValue(s, i));
            }
        }

        public static readonly object[][] s_GetNumericValueData =
        {
            new object[] {"aA1!", new double[] { -1, -1, 1, -1 }},
            // Numeric supplementary plane code point (U+12455 CUNEIFORM NUMERIC SIGN FIVE BAN2 VARIANT FORM)
            new object[] {"\uD809\uDC55", new double[] { 5, -1 }},
            new object[] {"a\uD809\uDC55a", new double[] { -1, 5, -1 , -1 }},
            // Non-numeric supplementary plane code point (U+1236C CUNEIFORM SIGN ZU5 TIMES A)
            new object[] {"\uD808\uDF6C", new double[] { -1, -1 }},
            new object[] {"a\uD808\uDF6Ca", new double[] { -1, -1, -1, -1 }},
        };

        [Fact]
        public void GetNumericValue_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => CharUnicodeInfo.GetNumericValue(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetNumericValue("abc", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetNumericValue("abc", 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetNumericValue("", 0));
        }

        private static string ErrorMessage(char ch, object expected, object actual)
        {
            return $"CodeValue: {(int)ch:X}; Expected: {expected}; Actual: {actual}";
        }

        public static string s_numericsCodepoints =
            "\u0030\u0031\u0032\u0033\u0034\u0035\u0036\u0037\u0038\u0039" +
            "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669" +
            "\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9" +
            "\u07c0\u07c1\u07c2\u07c3\u07c4\u07c5\u07c6\u07c7\u07c8\u07c9" +
            "\u0966\u0967\u0968\u0969\u096a\u096b\u096c\u096d\u096e\u096f" +
            "\u09e6\u09e7\u09e8\u09e9\u09ea\u09eb\u09ec\u09ed\u09ee\u09ef" +
            "\u0a66\u0a67\u0a68\u0a69\u0a6a\u0a6b\u0a6c\u0a6d\u0a6e\u0a6f" +
            "\u0ae6\u0ae7\u0ae8\u0ae9\u0aea\u0aeb\u0aec\u0aed\u0aee\u0aef" +
            "\u0b66\u0b67\u0b68\u0b69\u0b6a\u0b6b\u0b6c\u0b6d\u0b6e\u0b6f" +
            "\u0be6\u0be7\u0be8\u0be9\u0bea\u0beb\u0bec\u0bed\u0bee\u0bef" +
            "\u0c66\u0c67\u0c68\u0c69\u0c6a\u0c6b\u0c6c\u0c6d\u0c6e\u0c6f" +
            "\u0ce6\u0ce7\u0ce8\u0ce9\u0cea\u0ceb\u0cec\u0ced\u0cee\u0cef" +
            "\u0d66\u0d67\u0d68\u0d69\u0d6a\u0d6b\u0d6c\u0d6d\u0d6e\u0d6f" +
            "\u0e50\u0e51\u0e52\u0e53\u0e54\u0e55\u0e56\u0e57\u0e58\u0e59" +
            "\u0ed0\u0ed1\u0ed2\u0ed3\u0ed4\u0ed5\u0ed6\u0ed7\u0ed8\u0ed9" +
            "\u0f20\u0f21\u0f22\u0f23\u0f24\u0f25\u0f26\u0f27\u0f28\u0f29" +
            "\u1040\u1041\u1042\u1043\u1044\u1045\u1046\u1047\u1048\u1049" +
            "\u1090\u1091\u1092\u1093\u1094\u1095\u1096\u1097\u1098\u1099" +
            "\u17e0\u17e1\u17e2\u17e3\u17e4\u17e5\u17e6\u17e7\u17e8\u17e9" +
            "\u1810\u1811\u1812\u1813\u1814\u1815\u1816\u1817\u1818\u1819" +
            "\u1946\u1947\u1948\u1949\u194a\u194b\u194c\u194d\u194e\u194f" +
            "\u19d0\u19d1\u19d2\u19d3\u19d4\u19d5\u19d6\u19d7\u19d8\u19d9" +
            "\u1a80\u1a81\u1a82\u1a83\u1a84\u1a85\u1a86\u1a87\u1a88\u1a89" +
            "\u1a90\u1a91\u1a92\u1a93\u1a94\u1a95\u1a96\u1a97\u1a98\u1a99" +
            "\u1b50\u1b51\u1b52\u1b53\u1b54\u1b55\u1b56\u1b57\u1b58\u1b59" +
            "\u1bb0\u1bb1\u1bb2\u1bb3\u1bb4\u1bb5\u1bb6\u1bb7\u1bb8\u1bb9" +
            "\u1c40\u1c41\u1c42\u1c43\u1c44\u1c45\u1c46\u1c47\u1c48\u1c49" +
            "\u1c50\u1c51\u1c52\u1c53\u1c54\u1c55\u1c56\u1c57\u1c58\u1c59" +
            "\ua620\ua621\ua622\ua623\ua624\ua625\ua626\ua627\ua628\ua629" +
            "\ua8d0\ua8d1\ua8d2\ua8d3\ua8d4\ua8d5\ua8d6\ua8d7\ua8d8\ua8d9" +
            "\ua900\ua901\ua902\ua903\ua904\ua905\ua906\ua907\ua908\ua909" +
            "\ua9d0\ua9d1\ua9d2\ua9d3\ua9d4\ua9d5\ua9d6\ua9d7\ua9d8\ua9d9" +
            "\uaa50\uaa51\uaa52\uaa53\uaa54\uaa55\uaa56\uaa57\uaa58\uaa59" +
            "\uabf0\uabf1\uabf2\uabf3\uabf4\uabf5\uabf6\uabf7\uabf8\uabf9" +
            "\uff10\uff11\uff12\uff13\uff14\uff15\uff16\uff17\uff18\uff19";

        public static string s_nonNumericsCodepoints =
            "abcdefghijklmnopqrstuvwxyz" +
            "\u1369\u136a\u136b\u136c\u136d\u136e\u136f\u1370\u1371\u1372\u1373" +
            "\u1374\u1375\u1376\u1377\u1378\u1379\u137a\u137b\u137c\u137d";

        public static string s_numericNonDecimalCodepoints =
            "\u00b2\u00b3\u00b9\u1369\u136a\u136b\u136c\u136d\u136e\u136f\u1370" +
            "\u1371\u19da\u2070\u2074\u2075\u2076\u2077\u2078\u2079\u2080\u2081" +
            "\u2082\u2083\u2084\u2085\u2086\u2087\u2088\u2089\u2460\u2461\u2462" +
            "\u2463\u2464\u2465\u2466\u2467\u2468\u2474\u2475\u2476\u2477\u2478" +
            "\u2479\u247a\u247b\u247c\u2488\u2489\u248a\u248b\u248c\u248d\u248e" +
            "\u248f\u2490\u24ea\u24f5\u24f6\u24f7\u24f8\u24f9\u24fa\u24fb\u24fc" +
            "\u24fd\u24ff\u2776\u2777\u2778\u2779\u277a\u277b\u277c\u277d\u277e" +
            "\u2780\u2781\u2782\u2783\u2784\u2785\u2786\u2787\u2788\u278a\u278b" +
            "\u278c\u278d\u278e\u278f\u2790\u2791\u2792";

        public static int [] s_numericNonDecimalValues = new int []
        {
            2, 3, 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 0, 4, 5, 6, 7, 8, 9, 0, 1, 2,
            3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1,
            2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6,
            7, 8, 9
        };

        [Fact]
        public static void DigitsDecimalTest()
        {
            Assert.Equal(0, s_numericsCodepoints.Length % 10);
            for (int i=0; i < s_numericsCodepoints.Length; i+= 10)
            {
                for (int j=0; j < 10; j++)
                {
                    Assert.Equal(j, CharUnicodeInfo.GetDecimalDigitValue(s_numericsCodepoints[i + j]));
                    Assert.Equal(j, CharUnicodeInfo.GetDecimalDigitValue(s_numericsCodepoints, i + j));
                }
            }
        }

        [Fact]
        public static void NegativeDigitsTest()
        {
            for (int i=0; i < s_nonNumericsCodepoints.Length; i++)
            {
                Assert.Equal(-1, CharUnicodeInfo.GetDecimalDigitValue(s_nonNumericsCodepoints[i]));
                Assert.Equal(-1, CharUnicodeInfo.GetDecimalDigitValue(s_nonNumericsCodepoints, i));
            }
        }

        [Fact]
        public static void DigitsTest()
        {
            Assert.Equal(s_numericNonDecimalCodepoints.Length, s_numericNonDecimalValues.Length);
            for (int i=0; i < s_numericNonDecimalCodepoints.Length; i++)
            {
                Assert.Equal(s_numericNonDecimalValues[i], CharUnicodeInfo.GetDigitValue(s_numericNonDecimalCodepoints[i]));
                Assert.Equal(s_numericNonDecimalValues[i], CharUnicodeInfo.GetDigitValue(s_numericNonDecimalCodepoints, i));
            }
        }
    }
}
