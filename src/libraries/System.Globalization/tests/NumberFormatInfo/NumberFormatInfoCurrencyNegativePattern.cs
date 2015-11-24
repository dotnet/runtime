// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyNegativePattern
    {
        // TestDefault: Verify default value of property CurrencyNegativePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int expected = nfi.CurrencyNegativePattern;
            Assert.Equal(0, expected);
        }

        // TestSetValue: Verify set value of property CurrencyNegativePattern
        [Fact]
        public void TestSetValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            for (int i = 0; i <= 15; i++)
            {
                nfi.CurrencyNegativePattern = i;
                Assert.Equal(i, nfi.CurrencyNegativePattern);
            }
        }

        // TestArgumentOutOfRangeException: ArgumentOutOfRangeException is thrown
        [Fact]
        public void TestArgumentOutOfRangeException()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(16);
        }

        // TestInvalidOperationException: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperationException()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyNegativePattern = 1;
            });
        }


        // TestCurrencyNegativePatternLocale: Verify value of property CurrencyNegativePattern for specific locales
        [Theory]
        [InlineData("en-US")]
        [InlineData("en-CA")]
        [InlineData("fa-IR")]
        [InlineData("fr-CD")]
        [InlineData("as")]
        [InlineData("es-BO")]
        [InlineData("fr-CA")]
        public void TestCurrencyNegativePatternLocale(string locale)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.CurrencyNegativePattern;

            int[] acceptablePatterns = NumberFormatInfoData.GetCurrencyNegativePatterns(myTestCulture);
            Assert.True(acceptablePatterns.Contains(actual),
                string.Format("'{0}' was not found in '[{1}]'", actual, string.Join(",", acceptablePatterns)));
        }

        // TestCurrencyNegativePatternLocale2: Verify value of property CurrencyNegativePattern for specific locales
        [Theory]
        [InlineData("bg-BG", 8)]
        public void TestCurrencyNegativePatternLocale2(string locale, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.CurrencyNegativePattern;
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.CurrencyNegativePattern = i;
                int actual = nfi.CurrencyNegativePattern;
            });
        }
    }
}
