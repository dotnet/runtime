// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyNegativePattern
    {
        // PosTest1: Verify default value of property CurrencyNegativePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int expected = nfi.CurrencyNegativePattern;
            Assert.Equal(0, expected);
        }

        // PosTest2: Verify set value of property CurrencyNegativePattern
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

        // NegTest1: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void NegTest1()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(16);
        }

        // NegTest2: InvalidOperationException is not thrown
        [Fact]
        public void NegTest2()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyNegativePattern = 1;
            });
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
