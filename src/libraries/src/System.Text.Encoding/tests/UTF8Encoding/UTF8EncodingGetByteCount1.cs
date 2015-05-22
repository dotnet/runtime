// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetByteCount(System.Char[],System.Int32,System.Int32)
    public class UTF8EncodingGetByteCount1
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount(Char[],Int32,Int32) with non-null char[]
        [Fact]
        public void PosTest1()
        {
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);
        }

        // PosTest2: Verify method GetByteCount(Char[],Int32,Int32) with null char[]
        [Fact]
        public void PosTest2()
        {
            Char[] chars = new Char[] { };
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 0, 0);
            Assert.Equal(0, byteCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException is not thrown when chars is a null reference
        [Fact]
        public void NegTest1()
        {
            Char[] chars = null;
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                int byteCount = utf8.GetByteCount(chars, 0, 0);
            });
        }

        // NegTest2: ArgumentOutOfRangeException is not thrown when index is less than zero
        [Fact]
        public void NegTest2()
        {
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int byteCount = utf8.GetByteCount(chars, -1, 2);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when count is less than zero
        [Fact]
        public void NegTest3()
        {
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int byteCount = utf8.GetByteCount(chars, 1, -2);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in chars
        [Fact]
        public void NegTest4()
        {
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int byteCount = utf8.GetByteCount(chars, 1, chars.Length);
            });
        }
        #endregion
    }
}
