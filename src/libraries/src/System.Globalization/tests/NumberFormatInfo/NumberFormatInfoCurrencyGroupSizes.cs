// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyGroupSizes
    {
        // PosTest1: Verify default value of property CurrencyGroupSizes
        [Fact]
        public void PosTest1()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int[] expected = nfi.CurrencyGroupSizes;
            Assert.Equal(1, expected.Length);
            Assert.Equal(3, expected[0]);
        }

        // PosTest2: Verify set value of property CurrencyGroupSizes
        [Fact]
        public void PosTest2()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int[] expected = { 2, 3, 4 };
            Assert.Equal(3, expected.Length);
            Assert.Equal(2, expected[0]);
            Assert.Equal(3, expected[1]);
            Assert.Equal(4, expected[2]);
        }

        // NegTest1: ArgumentNullException is not thrown
        [Fact]
        public void NegTest1()
        {
            int[] expected = null;
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<ArgumentNullException>(() =>
            {
                nfi.CurrencyGroupSizes = expected;
            });
        }

        // NegTest2: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void NegTest2()
        {
            VerificationHelper<ArgumentException>(new int[] { -1, 1, 2 });
            VerificationHelper<ArgumentException>(new int[] { 98, 99, 100 });
            VerificationHelper<ArgumentException>(new int[] { 0, 1, 2 });
        }

        // NegTest3: InvalidOperationException is not thrown
        [Fact]
        public void NegTest3()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyGroupSizes = new int[] { 2, 3, 4 };
            });
        }

        private void VerificationHelper<T>(int[] intArray) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.CurrencyGroupSizes = intArray;
                int actual = nfi.CurrencyDecimalDigits;
            });
        }
    }
}
