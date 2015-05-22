// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetChars(System.Byte[],System.Int32,System.Int32,System.Char[],System.Int32)
    public class UTF8EncodingGetChars
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetChars with non-null chars
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);
        }

        // PosTest2: Verify method GetChars with null chars
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes;
            Char[] chars = new Char[] { };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 0, 0);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 0, 0, chars, 0);
            Assert.Equal(0, charsEncodedCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);
            });
        }

        // NegTest2: ArgumentNullException is not thrown when chars is a null reference
        [Fact]
        public void NegTest2()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            chars = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when byteIndex is less than zero
        [Fact]
        public void NegTest3()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, -1, 2, chars, 0);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when byteCount is less than zero
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, 1, -2, chars, 0);
            });
        }

        // NegTest5: ArgumentOutOfRangeException is not thrown when charIndex is less than zero
        [Fact]
        public void NegTest5()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, -1);
            });
        }

        // NegTest6: ArgumentOutOfRangeException is not thrown when byteIndex and byteCount do not denote a valid range in chars
        [Fact]
        public void NegTest6()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, 2, 2, chars, 1);
            });
        }

        // NegTest7: ArgumentException is not thrown when chars does not have enough capacity from charIndex to the end of the 
        // array to accommodate the resulting characters
        [Fact]
        public void NegTest7()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentException>(() =>
            {
                int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, chars.Length);
            });
        }
        #endregion
    }
}
