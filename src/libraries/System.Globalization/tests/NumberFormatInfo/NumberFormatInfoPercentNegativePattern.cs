// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
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

        // TestPercentNegativePatternLocale: Verify value of property PercentNegativePattern for specific locales
        [Theory]
        [InlineData("en-US", 0, 1)]
        public void TestPercentNegativePatternLocale(string locale, int expectedWindows, int expectedIcu)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.PercentNegativePattern;
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;
            // todo: determine why some values are different
            Assert.Equal(expected, actual);
        }

        // TestPercentNegativePatternLocale2: Verify value of property PercentNegativePattern for specific locales
        [Theory]
        [InlineData("en-MY", 1)]
        [InlineData("tr", 2)]
        public void TestPercentNegativePatternLocale2(string locale, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.PercentNegativePattern;
            Assert.Equal(expected, actual);
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
