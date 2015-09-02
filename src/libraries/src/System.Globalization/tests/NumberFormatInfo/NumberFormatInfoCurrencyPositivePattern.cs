// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyPositivePattern
    {
        // PosTest1: Verify default value of property CurrencyPositivePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();

            int expected = nfi.CurrencyPositivePattern;
            Assert.Equal(0, expected);
        }

        // PosTest2: Verify set value of property CurrencyPositivePattern
        [Fact]
        public void TestSetValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            for (int i = 0; i <= 3; i++)
            {
                nfi.CurrencyPositivePattern = i;
                Assert.Equal(i, nfi.CurrencyPositivePattern);
            }
        }

        // TestArgumentOutOfRange: ArgumentOutOfRangeException is thrown
        [Fact]
        public void TestArgumentOutOfRange()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(4);
        }

        // TestInvalidOperation: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperation()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyPositivePattern = 1;
            });
        }

        // TestCurrencyPositivePatternLocale: Verify value of property CurrencyPositivePattern for specific locales
        [Theory]
        [InlineData("en-US", 0)]
        [InlineData("fr-FR", 3)]
        public void TestCurrencyPositivePatternLocale(string locale, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.CurrencyPositivePattern;
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.CurrencyPositivePattern = i;
                int actual = nfi.CurrencyPositivePattern;
            });
        }
    }
}
