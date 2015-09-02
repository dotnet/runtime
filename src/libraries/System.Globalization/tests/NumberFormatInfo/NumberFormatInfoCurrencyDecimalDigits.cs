// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyDecimalDigits
    {
        // PosTest1: Verify property CurrencyDecimalDigits default value
        [Fact]
        public void PosTest1()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Equal(2, nfi.CurrencyDecimalDigits);
        }

        // PosTest2: Verify set value of property CurrencyDecimalDigits
        [Fact]
        public void PosTest2()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();

            for (int i = 0; i < 100; i++)
            {
                nfi.CurrencyDecimalDigits = i;
                Assert.Equal(i, nfi.CurrencyDecimalDigits);
            }
        }

        // TestArgumentOutOfRange: ArgumentOutOfRangeException is thrown
        [Fact]
        public void TestArgumentOutOfRange()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(100);
        }

        // TestInvalidOperation: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperation()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyDecimalDigits = 1;
            });
        }

        // TestCurrencyDecimalDigitsLocale: Verify value of property CurrencyDecimalDigits for specific locale
        [Theory]
        [InlineData("en-US", 2, 3)]
        [InlineData("ko", 0, 2)]
        public void TestCurrencyDecimalDigitsLocale(string locale, int expectedWindows, int expectedIcu)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.CurrencyDecimalDigits;
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;

            // todo: determine why some values are different
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.CurrencyDecimalDigits = i;
                int actual = nfi.CurrencyDecimalDigits;
            });
        }
    }
}
