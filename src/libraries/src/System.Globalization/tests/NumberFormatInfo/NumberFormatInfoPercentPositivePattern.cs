// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentPositivePattern
    {
        // TestDefault: Verify default value of property PercentPositivePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int expected = nfi.PercentPositivePattern;
            Assert.Equal(0, expected);
        }

        // TestSetValue: Verify set value of property PercentPositivePattern
        [Fact]
        public void TestSetValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            for (int i = 0; i <= 3; i++)
            {
                nfi.PercentPositivePattern = i;
                Assert.Equal(i, nfi.PercentPositivePattern);
            }
        }

        // TestArgumentOutOfRange: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void TestArgumentOutOfRange()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(4);
        }

        // TestInvalidOperation: InvalidOperationException is not thrown
        [Fact]
        public void TestInvalidOperation()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.PercentPositivePattern = 1;
            });
        }

        // TestPercentPositivePatternLocale: Verify value of property PercentPositivePattern for specific locales
        [Theory]
        [InlineData("en-US", 0, 1)]
        public void TestPercentPositivePatternLocale(string locale, int expectedWindows, int expectedIcu)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.PercentPositivePattern;
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;
            // todo: determine why some values are different
            Assert.Equal(expected, actual);
        }

        // TestPercentPositivePatternLocale2: Verify value of property PercentPositivePattern for specific locales
        [Theory]
        [InlineData("en-MY", 1)]
        [InlineData("tr", 2)]
        public void TestPercentPositivePatternLocale2(string locale, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int actual = nfi.PercentPositivePattern;
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.PercentPositivePattern = i;
                int actual = nfi.PercentPositivePattern;
            });
        }
    }
}
