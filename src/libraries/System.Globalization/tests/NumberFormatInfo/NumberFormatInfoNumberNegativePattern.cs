// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberNegativePattern
    {
        // TestDefault: Verify default value of property NumberNegativePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int expected = nfi.NumberNegativePattern;
            Assert.Equal(1, expected);
        }

        // TestSetValue: Verify set value of property NumberNegativePattern
        [Fact]
        public void TestSetValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            for (int i = 0; i <= 4; i++)
            {
                nfi.NumberNegativePattern = i;
                Assert.Equal(i, nfi.NumberNegativePattern);
            }
        }

        // TestArgumentOutOfRangeException: ArgumentOutOfRangeException is thrown
        [Fact]
        public void TestArgumentOutOfRangeException()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(5);
        }

        // TestInvalidOperationException: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperationException()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.NumberNegativePattern = 1;
            });
        }

        // TestLocale11: Verify value of property NumberNegativePattern for specific locale
        [Fact]
        public void TestLocale11()
        {
            CultureInfo myTestCulture = new CultureInfo("en-US");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.NumberNegativePattern; //1="-n"
            Assert.Equal(1, expected);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.NumberNegativePattern = i;
                int actual = nfi.NumberNegativePattern;
            });
        }
    }
}
