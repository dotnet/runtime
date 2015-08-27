// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentNegativePattern
    {
        // PosTest1: Verify default value of property PercentNegativePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int expected = nfi.PercentNegativePattern;
            Assert.Equal(0, expected);
        }

        // PosTest2: Verify set value of property PercentNegativePattern
        [Fact]
        public void TestSetValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            for (int i = 0; i <= 11; i++)
            {
                nfi.PercentNegativePattern = i;
                Assert.Equal(i, nfi.PercentNegativePattern);
            }
        }

        // TestArgumentOutOfRange: ArgumentOutOfRangeException is thrown
        [Fact]
        public void TestArgumentOutOfRange()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(12);
        }

        // TestInvalidOperation: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperation()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.PercentNegativePattern = 1;
            });
        }

        // TestLocale0: Verify value of property PercentNegativePattern for specific locale
        [Fact]
        public void TestLocale0()
        {
            CultureInfo myTestCulture = new CultureInfo("en-US");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.PercentNegativePattern; //0="-n %"
            // todo: determine why some values are different
            Assert.True(expected == 1 || expected == 0); //ICU=1, 0=Windows
        }

        // TestLocale1: Verify value of property PercentNegativePattern for specific locale
        [Fact]
        public void TestLocale1()
        {
            CultureInfo myTestCulture = new CultureInfo("en-MY");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.PercentNegativePattern; //1="-n%"
            Assert.Equal(1, expected);
        }

        // TestLocale2: Verify value of property PercentNegativePattern for specific locale
        [Fact]
        public void TestLocale2()
        {
            CultureInfo myTestCulture = new CultureInfo("tr");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.PercentNegativePattern; //2="-%n"
            Assert.Equal(2, expected);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.PercentNegativePattern = i;
                int actual = nfi.PercentNegativePattern;
            });
        }
    }
}
