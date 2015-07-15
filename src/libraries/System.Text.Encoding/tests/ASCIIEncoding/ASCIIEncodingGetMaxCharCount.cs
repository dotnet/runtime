// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Calculates the maximum number of characters produced by decoding the specified number of bytes.  
    // ASCIIEncoding.GetMaxCharCount(int)
    public class ASCIIEncodingGetMaxCharCount
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The specified number of bytes is zero.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(new ASCIIEncoding(), 0, 0);
        }

        // PosTest2: The specified number of bytes is a random non-negative Int32 value.
        [Fact]
        public void PosTest2()
        {
            ASCIIEncoding ascii;
            int byteCount;
            int expectedValue;

            ascii = new ASCIIEncoding();
            byteCount = _generator.GetInt32(-55);
            expectedValue = byteCount;
            DoPosTest(ascii, byteCount, expectedValue);
        }

        private void DoPosTest(ASCIIEncoding ascii, int byteCount, int expectedValue)
        {
            int actualValue;
            ascii = new ASCIIEncoding();
            actualValue = ascii.GetMaxCharCount(byteCount);
            Assert.Equal(expectedValue, actualValue);
        }

        // NegTest1: count of characters is less than zero.
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            int byteCount;

            ascii = new ASCIIEncoding();
            byteCount = -1 * _generator.GetInt32(-55) - 1;

            DoNegAOORTest(ascii, byteCount);
        }

        private void DoNegAOORTest(ASCIIEncoding ascii, int byteCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ascii.GetMaxCharCount(byteCount);
            });
        }
    }
}
