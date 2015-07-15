// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Calculates the maximum number of bytes produced by encoding the specified number of characters.  
    // ASCIIEncoding.GetMaxByteCount(int)
    public class ASCIIEncodingGetMaxByteCount
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The specified number of characters is zero.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(new ASCIIEncoding(), 0, 1);
        }

        // PosTest2: The specified number of characters is a random non-negative Int32 value.
        [Fact]
        public void PosTest2()
        {
            ASCIIEncoding ascii;
            int charCount;
            int expectedValue;

            ascii = new ASCIIEncoding();
            int replacementLength = 1;
            charCount = (replacementLength > 1) ?
                _generator.GetInt32(-55) % (int.MaxValue / replacementLength) :
                _generator.GetInt32(-55);
            expectedValue = replacementLength * charCount + 1;
            DoPosTest(ascii, charCount, expectedValue);
        }

        private void DoPosTest(ASCIIEncoding ascii, int charCount, int expectedValue)
        {
            int actualValue;

            ascii = new ASCIIEncoding();
            actualValue = ascii.GetMaxByteCount(charCount);
            Assert.Equal(expectedValue, actualValue);
        }

        // NegTest1: count of characters is less than zero.
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            int charCount;

            ascii = new ASCIIEncoding();
            charCount = -1 * _generator.GetInt32(-55) - 1;

            DoNegAOORTest(ascii, charCount);
        }

        // NegTest2: The resulting number of bytes is greater than the maximum number that can be returned as an int.
        [Fact]
        public void NegTest2()
        {
            ASCIIEncoding ascii;
            int charCount;

            ascii = new ASCIIEncoding();
            charCount = int.MaxValue;

            DoNegAOORTest(ascii, charCount);
        }

        private void DoNegAOORTest(ASCIIEncoding ascii, int charCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ascii.GetMaxByteCount(charCount);
            });
        }
    }
}
