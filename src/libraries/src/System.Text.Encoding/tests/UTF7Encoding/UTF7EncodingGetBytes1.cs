// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetBytes1
    {
        // PosTest1: Verify method GetBytes(Char[],Int32,Int32,Byte[],Int32) with non-null chars
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int bytesEncodedCount = UTF7.GetBytes(chars, 1, 2, bytes, 0);
        }

        // PosTest2: Verify method GetBytes(Char[],Int32,Int32,Byte[],Int32) with null chars
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes;
            Char[] chars = new Char[] { };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 0, 0);
            bytes = new Byte[byteCount];
            int bytesEncodedCount = UTF7.GetBytes(chars, 0, 0, bytes, 0);
            Assert.Equal(0, bytesEncodedCount);
        }

        // NegTest1: ArgumentNullException is not thrown when chars is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes;
            Char[] chars = null;
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = 10;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentNullException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, 1, 2, bytes, 0);
            });
        }

        // NegTest2: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest2()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, 1, 2, bytes, 0);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when charIndex is less than zero
        [Fact]
        public void NegTest3()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, -1, 2, bytes, 0);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when charCount is less than zero
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, 1, -2, bytes, 0);
            });
        }

        // NegTest5: ArgumentOutOfRangeException is not thrown when byteIndex is less than zero.
        [Fact]
        public void NegTest5()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, 1, 2, bytes, -1);
            });
        }

        // NegTest6: ArgumentOutOfRangeException is not thrown when charCount do not denote a valid range in chars.
        [Fact]
        public void NegTest6()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, 1, chars.Length, bytes, 1);
            });
        }

        // NegTest7: ArgumentException is not thrown when bytes does not have enough capacity from byteIndex to the end of the 
        // array to accommodate the resulting bytes.
        [Fact]
        public void NegTest7()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, 1, 2, bytes, bytes.Length);
            });
        }

        // NegTest8: ArgumentOutOfRangeException is not thrown when charIndex do not denote a valid range in chars.
        [Fact]
        public void NegTest8()
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023',
                            '\u0025',
                            '\u03a0',
                            '\u03a3'  };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = UTF7.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = UTF7.GetBytes(chars, chars.Length, 2, bytes, 1);
            });
        }
    }
}
