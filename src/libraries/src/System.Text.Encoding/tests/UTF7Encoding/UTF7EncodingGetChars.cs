// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetChars
    {
        // PosTest1: Verify method GetChars with non-null chars
        [Fact]
        public void PosTest1()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            chars = new Char[charCount];
            int charsDecodedCount = UTF7.GetChars(bytes, 2, 8, chars, 0);
        }

        // PosTest2: Verify method GetChars with null chars
        [Fact]
        public void PosTest2()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] { };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 0, 0);
            chars = new Char[] { };
            int charsDecodedCount = UTF7.GetChars(bytes, 0, 0, chars, 0);
            Assert.Equal(0, charsDecodedCount);
        }

        // NegTest1: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Char[] chars;
            Byte[] bytes = null;
            UTF7Encoding UTF7 = new UTF7Encoding();
            chars = new Char[] { };
            Assert.Throws<ArgumentNullException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 0, 0, chars, 0);
            });
        }

        // NegTest2: ArgumentNullException is not thrown when chars is a null reference
        [Fact]
        public void NegTest2()
        {
            Char[] chars = null;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 2, 8, chars, 0);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when byteIndex is less than zero
        [Fact]
        public void NegTest3()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            chars = new Char[charCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, -2, 8, chars, 0);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when byteCount is less than zero  
        [Fact]
        public void NegTest4()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            chars = new Char[charCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 2, -8, chars, 0);
            });
        }

        // NegTest5: ArgumentOutOfRangeException is not thrown when charIndex is less than zero.
        [Fact]
        public void NegTest5()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            chars = new Char[charCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 2, 8, chars, -1);
            });
        }

        // NegTest6: ArgumentOutOfRangeException is not thrown when byteIndex and byteCount do not denote a valid range in chars.
        [Fact]
        public void NegTest6()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 12);
            chars = new Char[charCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 2, 28, chars, 0);
            });
        }

        // This test will fail due to bug 54232
        // NegTest7:charIndex is not a valid index in chars.
        [Fact]
        public void NegTest7()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            chars = new Char[charCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 2, 8, chars, bytes.Length + 1);
            });
        }

        // NegTest8: ArgumentException is not thrown when chars does not have enough capacity from charIndex 
        // to the end of the array to accommodate the resulting characters
        [Fact]
        public void NegTest8()
        {
            Char[] chars;
            Byte[] bytes = new Byte[] {
                 85,  84,  70,  55,  32,  69, 110,
                 99, 111, 100, 105, 110, 103,  32,
                 69, 120,  97, 109, 112, 108, 101
            };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            chars = new Char[charCount];
            Assert.Throws<ArgumentException>(() =>
            {
                int charsDecodedCount = UTF7.GetChars(bytes, 2, 8, chars, charCount - 7);
            });
        }
    }
}
