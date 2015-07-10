// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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

        //  NegTest1: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void NegTest1()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(100);
        }

        // NegTest2: InvalidOperationException is not thrown
        [Fact]
        public void NegTest2()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyDecimalDigits = 1;
            });
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
