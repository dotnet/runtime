// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberDecimalDigits
    {
        // PosTest1: Verify property NumberDecimalDigits default value
        [Fact]
        public void PosTest1()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Equal(2, nfi.NumberDecimalDigits);
        }

        // PosTest2: Verify set value of property NumberDecimalDigits
        [Fact]
        public void PosTest2()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();

            for (int i = 0; i < 100; i++)
            {
                nfi.NumberDecimalDigits = i;
                Assert.Equal(i, nfi.NumberDecimalDigits);
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
                nfiReadOnly.NumberDecimalDigits = 1;
            });
        }

        // TestCurrencyNumberDecimalDigitsLocale: Verify value of property NumberDecimalDigits for specific locales
        [Theory]
        [InlineData("en-US", 2, 3)]
        public void TestCurrencyNumberDecimalDigitsLocale(string locale, int expectedWindows, int expectedIcu)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.NumberDecimalDigits;
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;

            // todo: determine if Windows version needs to support "accounting" currency explictly which contains parenthesis
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.NumberDecimalDigits = i;
                int actual = nfi.NumberDecimalDigits;
            });
        }
    }
}
