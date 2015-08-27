// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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

        // TestLocale1: Verify value of property NumberDecimalDigits for specific locale
        [Fact]
        public void TestLocale()
        {
            CultureInfo myTestCulture = new CultureInfo("en-US");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.NumberDecimalDigits;
            // todo: determine why some values are different
            Assert.True(expected == 3 || expected == 2); //ICU=3, 2=Windows
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
