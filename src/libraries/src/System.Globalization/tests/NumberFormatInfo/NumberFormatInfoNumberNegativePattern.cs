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

        // TestNumberNegativePatternLocale: Verify value of property NumberNegativePattern for specific locales
        [Theory]
        [InlineData("en-US", 1)]
        public void TestNumberNegativePatternLocale(string locale, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.NumberNegativePattern;
            Assert.Equal(expected, actual);
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
