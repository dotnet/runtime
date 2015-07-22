// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Text.UnicodeEncoding.GetMaxByteCount(int)
    public class UnicodeEncodingGetMaxByteCount
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Tests
        // PosTest1:Invoke the method and set charCount as 0
        [Fact]
        public void PosTest1()
        {
            int expectedValue = 2;
            int actualValue;
            UnicodeEncoding uE = new UnicodeEncoding();

            actualValue = uE.GetMaxByteCount(0);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method and set charCount as 1
        [Fact]
        public void PosTest2()
        {
            int expectedValue = 4;
            int actualValue;
            UnicodeEncoding uE = new UnicodeEncoding();

            actualValue = uE.GetMaxByteCount(1);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method and set charCount as random integer
        [Fact]
        public void PosTest3()
        {
            int charCount = (_generator.GetInt32(-55) % Int32.MaxValue + 1) / 2;
            int expectedValue = (charCount + 1) * 2;
            int actualValue;
            UnicodeEncoding uE = new UnicodeEncoding();

            actualValue = uE.GetMaxByteCount(charCount);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negative Tests
        // NegTest1:Invoke the method and set charCount as -1
        [Fact]
        public void NegTest1()
        {
            int actualValue;
            UnicodeEncoding uE = new UnicodeEncoding();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uE.GetMaxByteCount(-1);
            });
        }

        // NegTest2:Invoke the method and set charCount as a large integer that lead the bytecount to overflow
        [Fact]
        public void NegTest2()
        {
            int actualValue;
            UnicodeEncoding uE = new UnicodeEncoding();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = uE.GetMaxByteCount(int.MaxValue / 2);
            });
        }
        #endregion
    }
}
