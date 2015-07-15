// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Encodes a set of characters from the specified character array into the specified byte array. 
    // for method: ASCIIEncoding.GetBytes(char[], Int32, Int32, Byte[], Int32)
    public class ASCIIEncodingGetBytes2
    {
        private const int c_MIN_STRING_LENGTH = 2;
        private const int c_MAX_STRING_LENGTH = 260;

        private const char c_MIN_ASCII_CHAR = (char)0x0;
        private const char c_MAX_ASCII_CHAR = (char)0x7f;

        private const int c_MAX_ARRAY_LENGTH = 260;

        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The specified string is string.Empty.
        [Fact]
        public void PosTest1()
        {
            DoPosTest(new ASCIIEncoding(), new char[0], 0, 0, new byte[0], 0);
        }

        // PosTest2: The specified string is a random string.
        [Fact]
        public void PosTest2()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = _generator.GetInt32(-55) % chars.Length;
            count = _generator.GetInt32(-55) % (chars.Length - charIndex) + 1;

            int minLength = ascii.GetByteCount(chars, charIndex, count);
            int length = minLength + _generator.GetInt32(-55) % (Int16.MaxValue - minLength);
            bytes = new byte[length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = 0;
            }
            byteIndex = _generator.GetInt32(-55) % (bytes.Length - minLength + 1);

            DoPosTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        private void DoPosTest(ASCIIEncoding ascii, char[] chars, int charIndex, int count, byte[] bytes, int byteIndex)
        {
            int actualValue;
            actualValue = ascii.GetBytes(chars, charIndex, count, bytes, byteIndex);
            Assert.True(VerifyASCIIEncodingGetBytesResult(ascii, chars, charIndex, count, bytes, byteIndex, actualValue));
        }

        private bool VerifyASCIIEncodingGetBytesResult(
                               ASCIIEncoding ascii,
                               char[] chars, int charIndex, int count,
                               byte[] bytes, int byteIndex,
                               int actualValue)
        {
            if (0 == chars.Length) return 0 == actualValue;

            int currentCharIndex; //index of current encoding character
            int currentByteIndex; //index of current encoding byte
            int charEndIndex = charIndex + count - 1;

            for (currentCharIndex = charIndex, currentByteIndex = byteIndex;
                 currentCharIndex <= charEndIndex; ++currentCharIndex)
            {
                if (chars[currentCharIndex] <= c_MAX_ASCII_CHAR &&
                    chars[currentCharIndex] >= c_MIN_ASCII_CHAR)
                { //verify normal ASCII encoding character
                    if ((int)chars[currentCharIndex] != (int)bytes[currentByteIndex])
                    {
                        return false;
                    }
                    ++currentByteIndex;
                }
                else //Verify ASCII encoder replacment fallback
                {
                    ++currentByteIndex;
                }
            }

            return actualValue == (currentByteIndex - byteIndex);
        }

        // NegTest1: Character array is a null reference
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            char[] chars = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                ascii.GetBytes(chars, 0, 0, new byte[1], 0);
            });
        }

        // NegTest2: charIndex is less than zero.
        [Fact]
        public void NegTest2()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = -1 * _generator.GetInt32(-55) - 1;
            count = _generator.GetInt32(-55) % chars.Length + 1;
            bytes = new byte[count];
            byteIndex = 0;

            DoNegAOORTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        // NegTest3: charCount is less than zero.
        [Fact]
        public void NegTest3()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = _generator.GetInt32(-55) % chars.Length;
            count = -1 * _generator.GetInt32(-55) % chars.Length - 1;
            bytes = new byte[chars.Length];
            byteIndex = 0;

            DoNegAOORTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        // NegTest4: byteIndex is less than zero.
        [Fact]
        public void NegTest4()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = _generator.GetInt32(-55) % chars.Length;
            count = _generator.GetInt32(-55) % (chars.Length - charIndex) + 1;
            bytes = new byte[count];
            byteIndex = -1 * _generator.GetInt32(-55) % chars.Length - 1;

            DoNegAOORTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        // NegTest5: charIndex is greater than or equal the length of string.
        [Fact]
        public void NegTest5()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = chars.Length + _generator.GetInt32(-55) % (int.MaxValue - chars.Length);
            count = 0;
            bytes = new byte[0];
            byteIndex = 0;

            DoNegAOORTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        // NegTest6: count does not denote a valid range in chars string.
        [Fact]
        public void NegTest6()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = _generator.GetInt32(-55) % chars.Length;
            count = chars.Length - charIndex + 1 +
                _generator.GetInt32(-55) % (int.MaxValue - chars.Length + charIndex);
            bytes = new byte[1];
            byteIndex = 0;

            DoNegAOORTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        // NegTest7: count does not denote a valid range in chars string.
        [Fact]
        public void NegTest7()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = _generator.GetInt32(-55) % chars.Length;
            count = _generator.GetInt32(-55) % (chars.Length - charIndex) + 1;
            int minLength = ascii.GetByteCount(chars, charIndex, count);
            bytes = new byte[minLength];
            byteIndex = bytes.Length + _generator.GetInt32(-55) % (int.MaxValue - bytes.Length);

            DoNegAOORTest(ascii, chars, charIndex, count, bytes, byteIndex);
        }

        // NegTest8: bytes does not have enough capacity from byteIndex to the end of the array to accommodate the resulting bytes.
        [Fact]
        public void NegTest8()
        {
            ASCIIEncoding ascii;
            char[] chars;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            InitializeCharacterArray(out chars);
            charIndex = _generator.GetInt32(-55) % chars.Length;
            count = _generator.GetInt32(-55) % (chars.Length - charIndex) + 1;
            int minLength = ascii.GetByteCount(chars, charIndex, count);
            bytes = new byte[_generator.GetInt32(-55) % minLength];
            byteIndex = 0;

            Assert.Throws<ArgumentException>(() =>
            {
                ascii.GetBytes(chars, charIndex, count, bytes, byteIndex);
            });
        }

        private void DoNegAOORTest(ASCIIEncoding ascii, char[] chars, int charIndex, int count, byte[] bytes, int byteIndex)
        {
            ascii = new ASCIIEncoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ascii.GetBytes(chars, charIndex, count, bytes, byteIndex);
            });
        }

        //Initialize the character array using random values
        private void InitializeCharacterArray(out char[] chars)
        {
            //Get a character array whose length is beween 1 and c_MAX_ARRAY_LENGTH
            int length = _generator.GetInt32(-55) % c_MAX_ARRAY_LENGTH + 1;
            chars = new char[length];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = _generator.GetChar(-55);
            }
        }
    }
}
