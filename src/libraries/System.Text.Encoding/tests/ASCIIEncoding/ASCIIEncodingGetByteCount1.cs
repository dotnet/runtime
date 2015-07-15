// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Calculates the number of bytes produced by encoding the characters in the specified String. 
    public class ASCIIEncodingGetByteCount1
    {
        private const int c_MIN_STRING_LENGTH = 2;
        private const int c_MAX_STRING_LENGTH = 260;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The specified string is string.Empty.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(string.Empty, 0);
        }

        // PosTest2: The specified string is a random string.
        [Fact]
        public void PosTest2()
        {
            string source;
            int expectedValue;

            source = _generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            expectedValue = source.Length;
            DoPosTest(source, expectedValue);
        }

        private void DoPosTest(string source, int expectedValue)
        {
            ASCIIEncoding ascii;
            int actualValue;
            ascii = new ASCIIEncoding();
            actualValue = ascii.GetByteCount(source);
            Assert.Equal(expectedValue, actualValue);
        }

        // NegTest1: source string is a null reference (Nothing in Visual Basic).
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            string source = null;
            ascii = new ASCIIEncoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                ascii.GetByteCount(source);
            });
        }
    }
}
