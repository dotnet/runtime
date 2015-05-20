// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Encodes a set of characters from the specified String into the specified byte array. 
    public class ASCIIEncodingGetBytes1
    {
        private const int c_MIN_STRING_LENGTH = 2;
        private const int c_MAX_STRING_LENGTH = 260;

        private const char c_MIN_ASCII_CHAR = (char)0x0;
        private const char c_MAX_ASCII_CHAR = (char)0x7f;

        [Fact]
        public void PosTest1()
        {
            DoPosTest(new ASCIIEncoding(), string.Empty, 0, 0, new byte[0], 0);
        }

        [Fact]
        public void PosTest2()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = TestLibrary.Generator.GetInt32(-55) % source.Length;
            count = TestLibrary.Generator.GetInt32(-55) % (source.Length - charIndex) + 1;

            int minLength = ascii.GetByteCount(source.Substring(charIndex, count));
            int length = minLength + TestLibrary.Generator.GetInt32(-55) % (Int16.MaxValue - minLength);
            bytes = new byte[length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = 0;
            }
            byteIndex = TestLibrary.Generator.GetInt32(-55) % (bytes.Length - minLength + 1);

            DoPosTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        private void DoPosTest(ASCIIEncoding ascii, string source, int charIndex, int count, byte[] bytes, int byteIndex)
        {
            int actualValue;

            actualValue = ascii.GetBytes(source, charIndex, count, bytes, byteIndex);
            Assert.True(VerifyASCIIEncodingGetBytesResult(ascii, source, charIndex, count, bytes, byteIndex, actualValue));
        }

        private bool VerifyASCIIEncodingGetBytesResult(
                               ASCIIEncoding ascii,
                               string source, int charIndex, int count,
                               byte[] bytes, int byteIndex,
                               int actualValue)
        {
            if (0 == source.Length) return 0 == actualValue;

            int currentCharIndex; //index of current encoding character
            int currentByteIndex; //index of current encoding byte
            int charEndIndex = charIndex + count - 1;

            for (currentCharIndex = charIndex, currentByteIndex = byteIndex;
                 currentCharIndex <= charEndIndex; ++currentCharIndex)
            {
                if (source[currentCharIndex] <= c_MAX_ASCII_CHAR &&
                    source[currentCharIndex] >= c_MIN_ASCII_CHAR)
                { //verify normal ASCII encoding character
                    if ((int)source[currentCharIndex] != (int)bytes[currentByteIndex])
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

        // NegTest1: source string is a null reference
        [Fact]
        public void NegTest1()
        {
            ASCIIEncoding ascii;
            string source = null;

            ascii = new ASCIIEncoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                ascii.GetBytes(source, 0, 0, new byte[1], 0);
            });
        }

        // NegTest2: charIndex is less than zero.
        [Fact]
        public void NegTest2()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
            count = TestLibrary.Generator.GetInt32(-55) % source.Length + 1;
            bytes = new byte[count];
            byteIndex = 0;

            DoNegAOORTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        // NegTest3: charCount is less than zero.
        [Fact]
        public void NegTest3()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = TestLibrary.Generator.GetInt32(-55) % source.Length;
            count = -1 * TestLibrary.Generator.GetInt32(-55) % source.Length - 1;
            bytes = new byte[source.Length];
            byteIndex = 0;

            DoNegAOORTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        // NegTest4: byteIndex is less than zero.
        [Fact]
        public void NegTest4()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = TestLibrary.Generator.GetInt32(-55) % source.Length;
            count = TestLibrary.Generator.GetInt32(-55) % (source.Length - charIndex) + 1;
            bytes = new byte[count];
            byteIndex = -1 * TestLibrary.Generator.GetInt32(-55) % source.Length - 1;

            DoNegAOORTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        // NegTest5: charIndex is greater than or equal the length of string.
        [Fact]
        public void NegTest5()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = source.Length + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - source.Length);
            count = 0;
            bytes = new byte[0];
            byteIndex = 0;

            DoNegAOORTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        // NegTest6: count does not denote a valid range in source string.
        [Fact]
        public void NegTest6()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = TestLibrary.Generator.GetInt32(-55) % source.Length;
            count = source.Length - charIndex + 1 +
                TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - source.Length + charIndex);
            bytes = new byte[1];
            byteIndex = 0;

            DoNegAOORTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        // NegTest7: count does not denote a valid range in source string.
        [Fact]
        public void NegTest7()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = TestLibrary.Generator.GetInt32(-55) % source.Length;
            count = TestLibrary.Generator.GetInt32(-55) % (source.Length - charIndex) + 1;
            int minLength = ascii.GetByteCount(source.Substring(charIndex, count));
            bytes = new byte[minLength];
            byteIndex = bytes.Length + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - bytes.Length);

            DoNegAOORTest(ascii, source, charIndex, count, bytes, byteIndex);
        }

        // NegTest8: bytes does not have enough capacity from byteIndex to the end of the array to accommodate the resulting bytes.
        [Fact]
        public void NegTest8()
        {
            ASCIIEncoding ascii;
            string source;
            int charIndex, count;
            byte[] bytes;
            int byteIndex;

            ascii = new ASCIIEncoding();
            source = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charIndex = TestLibrary.Generator.GetInt32(-55) % source.Length;
            count = TestLibrary.Generator.GetInt32(-55) % (source.Length - charIndex) + 1;
            int minLength = ascii.GetByteCount(source.Substring(charIndex, count));
            bytes = new byte[TestLibrary.Generator.GetInt32(-55) % minLength];
            byteIndex = 0;

            Assert.Throws<ArgumentException>(() =>
           {
               ascii.GetBytes(source, charIndex, count, bytes, byteIndex);
           });
        }

        private void DoNegAOORTest(ASCIIEncoding ascii,
                                   string source, int charIndex, int count,
                                   byte[] bytes, int byteIndex)
        {
            ascii = new ASCIIEncoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ascii.GetBytes(source, charIndex, count, bytes, byteIndex);
            });
        }
    }
}
