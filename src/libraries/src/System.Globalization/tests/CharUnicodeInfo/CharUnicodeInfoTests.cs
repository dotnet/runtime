// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class CharUnicodeInfoGetUnicodeCategoryTests
    {
        [Fact]
        public void GetUnicodeCategory()
        {
            foreach (CharUnicodeInfoTestCase testCase in CharUnicodeInfoTestData.TestCases)
            {
                if (testCase.Utf32CodeValue.Length == 1)
                {
                    // Test the char overload for a single char
                    GetUnicodeCategory(testCase.Utf32CodeValue[0], testCase.GeneralCategory);
                }
                // Test the string overload for a surrogate pair or a single char
                GetUnicodeCategory(testCase.Utf32CodeValue, new UnicodeCategory[] { testCase.GeneralCategory });
            }
        }

        [Theory]
        [InlineData('\uFFFF', UnicodeCategory.OtherNotAssigned)]
        public void GetUnicodeCategory(char ch, UnicodeCategory expected)
        {
            UnicodeCategory actual = CharUnicodeInfo.GetUnicodeCategory(ch);
            Assert.True(actual == expected, ErrorMessage(ch, expected, actual));
        }

        [Theory]
        [InlineData("aA1!", new UnicodeCategory[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.UppercaseLetter, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherPunctuation })]
        [InlineData("\uD808\uDF6C", new UnicodeCategory[] { UnicodeCategory.OtherLetter, UnicodeCategory.Surrogate })]
        [InlineData("a\uD808\uDF6Ca", new UnicodeCategory[] { UnicodeCategory.LowercaseLetter, UnicodeCategory.OtherLetter, UnicodeCategory.Surrogate, UnicodeCategory.LowercaseLetter })]
        public void GetUnicodeCategory(string s, UnicodeCategory[] expected)
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
            GetUnicodeCategory("\uD808\uD808", new UnicodeCategory[] { UnicodeCategory.Surrogate, UnicodeCategory.Surrogate });

            // Low, low surrogate pair
            GetUnicodeCategory("\uDF6C\uDF6C", new UnicodeCategory[] { UnicodeCategory.Surrogate, UnicodeCategory.Surrogate });

            // Low, high surrogate pair
            GetUnicodeCategory("\uDF6C\uD808", new UnicodeCategory[] { UnicodeCategory.Surrogate, UnicodeCategory.Surrogate });
        }

        [Fact]
        public void GetUnicodeCategory_Invalid()
        {
            Assert.Throws<ArgumentNullException>("s", () => CharUnicodeInfo.GetUnicodeCategory(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetUnicodeCategory("abc", -1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetUnicodeCategory("abc", 3));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetUnicodeCategory("", 0));
        }

        [Fact]
        public void GetNumericValue()
        {
            foreach (CharUnicodeInfoTestCase testCase in CharUnicodeInfoTestData.TestCases)
            {
                if (testCase.Utf32CodeValue.Length == 1)
                {
                    // Test the char overload for a single char
                    GetNumericValue(testCase.Utf32CodeValue[0], testCase.NumericValue);
                }
                // Test the string overload for a surrogate pair
                GetNumericValue(testCase.Utf32CodeValue, new double[] { testCase.NumericValue });
            }
        }

        [Theory]
        [InlineData("\uFFFF", -1)]
        public void GetNumericValue(char ch, double expected)
        {
            double actual = CharUnicodeInfo.GetNumericValue(ch);
            Assert.True(expected == actual, ErrorMessage(ch, expected, actual));
        }

        [Theory]
        [InlineData("aA1!", new double[] { -1, -1, 1, -1 })]
        // Numeric surrogate (CUNEIFORM NUMERIC SIGN FIVE BAN2 VARIANT FORM)
        [InlineData("\uD809\uDC55", new double[] { 5, -1 })]
        [InlineData("a\uD809\uDC55a", new double[] { -1, 5, -1 , -1 })]
        // Non-numeric surrogate (CUNEIFORM SIGN ZU5 TIMES A)
        [InlineData("\uD808\uDF6C", new double[] { -1, -1 })]
        [InlineData("a\uD808\uDF6Ca", new double[] { -1, -1, -1, -1 })]
        public void GetNumericValue(string s, double[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], CharUnicodeInfo.GetNumericValue(s, i));
            }
        }

        [Fact]
        public void GetNumericValue_Invalid()
        {
            Assert.Throws<ArgumentNullException>("s", () => CharUnicodeInfo.GetNumericValue(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetNumericValue("abc", -1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetNumericValue("abc", 3));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => CharUnicodeInfo.GetNumericValue("", 0));
        }

        private static string ErrorMessage(char ch, object expected, object actual)
        {
            return $"CodeValue: {((int)ch).ToString("X")}; Expected: {expected}; Actual: {actual}";
        }
    }
}
