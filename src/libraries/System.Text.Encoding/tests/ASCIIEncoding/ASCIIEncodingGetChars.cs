// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Decodes a sequence of bytes from the specified byte array into the specified character array.   
    // ASCIIEncoding.GetChars(byte[], int, int, char[], int)
    public class ASCIIEncodingGetChars
    {
        private const int c_MIN_STRING_LENGTH = 2;
        private const int c_MAX_STRING_LENGTH = 260;

        private const char c_MIN_ASCII_CHAR = (char)0x0;
        private const char c_MAX_ASCII_CHAR = (char)0x7f;

        // PosTest1: zero-length byte array.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(new ASCIIEncoding(), new byte[0], 0, 0, new char[0], 0);
        }

        // PosTest2: random byte array.
        [Fact]
        public void PosTest2()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;
            string source;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            bytes = new byte[ascii.GetByteCount(source)];
            ascii.GetBytes(source, 0, source.Length, bytes, 0);
            byteIndex = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            byteCount = TestLibrary.Generator.GetInt32(-55) % (bytes.Length - byteIndex) + 1;
            chars = new char[byteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = TestLibrary.Generator.GetInt32(-55) % (chars.Length - byteCount + 1);

            DoPosTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        private void DoPosTest(ASCIIEncoding ascii, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int actualValue = ascii.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            Assert.True(VerifyGetCharsResult(ascii, bytes, byteIndex, byteCount, chars, charIndex, actualValue));
        }

        private bool VerifyGetCharsResult(ASCIIEncoding ascii,
                                          byte[] bytes, int byteIndex, int byteCount,
                                          char[] chars, int charIndex, int actualValue)
        {
            if (actualValue != byteCount) return false;
            //Assume that the character array has enough capacity to accommodate the resulting characters
            //i current index of byte array, j current index of character array
            for (int byteEnd = byteIndex + byteCount, i = byteIndex, j = charIndex; i < byteEnd; ++i, ++j)
            {
                if (bytes[i] != (byte)chars[j]) return false;
            }

            return true;
        }

        // NegTest1: count of bytes is less than zero.
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };

            byteCount = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
            byteIndex = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            int actualByteCount = bytes.Length - byteIndex;
            chars = new char[actualByteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = TestLibrary.Generator.GetInt32(-55) % (chars.Length - actualByteCount + 1);

            DoNegAOORTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        // NegTest2: The start index of bytes is less than zero.
        [Fact]
        public void NegTest2()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            byteCount = TestLibrary.Generator.GetInt32(-55) % bytes.Length + 1;
            byteIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
            chars = new char[byteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = TestLibrary.Generator.GetInt32(-55) % (chars.Length - byteCount + 1);

            DoNegAOORTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        // NegTest3: count of bytes is too large.
        [Fact]
        public void NegTest3()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            byteIndex = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            byteCount = bytes.Length - byteIndex + 1 +
                        TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - bytes.Length + byteIndex);
            int actualByteCount = bytes.Length - byteIndex;
            chars = new char[actualByteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = TestLibrary.Generator.GetInt32(-55) % (chars.Length - actualByteCount + 1);

            DoNegAOORTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        // NegTest4: The start index of bytes is too large.
        [Fact]
        public void NegTest4()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            byteCount = TestLibrary.Generator.GetInt32(-55) % bytes.Length + 1;
            byteIndex = bytes.Length - byteCount + 1 +
                        TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - bytes.Length + byteCount);
            chars = new char[byteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = TestLibrary.Generator.GetInt32(-55) % (chars.Length - byteCount + 1);

            DoNegAOORTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        // NegTest5: bytes is a null reference
        [Fact]
        public void NegTest5()
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] bytes = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                ascii.GetChars(bytes, 0, 0, new char[0], 0);
            });
        }

        // NegTest6: character array is a null reference
        [Fact]
        public void NegTest6()
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                ascii.GetChars(bytes, 0, 0, null, 0);
            });
        }

        // NegTest7: The start index of character array is less than zero.
        [Fact]
        public void NegTest7()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            byteIndex = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            byteCount = TestLibrary.Generator.GetInt32(-55) % (bytes.Length - byteIndex) + 1;
            chars = new char[byteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

            DoNegAOORTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        // NegTest8: The start index of character array is too large.
        [Fact]
        public void NegTest8()
        {
            ASCIIEncoding ascii;
            byte[] bytes;
            int byteIndex, byteCount;
            char[] chars;
            int charIndex;

            ascii = new ASCIIEncoding();
            bytes = new byte[]
            {
             65,  83,  67,  73,  73,  32,  69,
            110,  99, 111, 100, 105, 110, 103,
             32,  69, 120,  97, 109, 112, 108
            };
            byteIndex = TestLibrary.Generator.GetInt32(-55) % bytes.Length;
            byteCount = TestLibrary.Generator.GetInt32(-55) % (bytes.Length - byteIndex) + 1;
            chars = new char[byteCount + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LENGTH];
            charIndex = chars.Length - byteCount + 1 +
                TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - chars.Length + byteCount);

            DoNegAOORTest(ascii, bytes, byteIndex, byteCount, chars, charIndex);
        }

        private void DoNegAOORTest(ASCIIEncoding ascii, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ascii.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            });
        }
    }
}
