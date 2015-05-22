// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetChars(System.Byte[],System.Int32,System.Int32,System.Char[],System.Int32)
    public class DecoderGetChars3
    {
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 127;
        #endregion

        #region Positive Test Cases
        // PosTest1: Call GetChars with ASCII decoder to convert a ASCII byte array
        [Fact]
        public void PosTest1()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            char[] expectedChars = new char[bytes.Length];
            Decoder decoder = Encoding.UTF8.GetDecoder();

            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }
            for (int i = 0; i < expectedChars.Length; ++i)
            {
                expectedChars[i] = (char)('\0' + i);
            }

            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, bytes.Length, expectedChars, "001.1");
        }

        // PosTest2: Call GetChars with Unicode decoder to convert a ASCII byte array
        [Fact]
        public void PosTest2()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY * 2];
            char[] chars = new char[c_SIZE_OF_ARRAY];
            char[] expectedChars = new char[c_SIZE_OF_ARRAY];
            Decoder decoder = Encoding.Unicode.GetDecoder();

            byte c = 0;
            for (int i = 0; i < bytes.Length; i += 2)
            {
                bytes[i] = c++;
                bytes[i + 1] = 0;
            }
            for (int i = 0; i < expectedChars.Length; ++i)
            {
                expectedChars[i] = (char)('\0' + i);
            }

            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, expectedChars.Length, expectedChars, "002.1");
        }

        // PosTest3: Call GetChars with Unicode decoder to convert a Unicode byte array
        [Fact]
        public void PosTest3()
        {
            byte[] bytes = new byte[] {
                217,
                143,
                42,
                78,
                0,
                78,
                42,
                78,
                75,
                109,
                213,
                139
            };
            char[] expected = "\u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5".ToCharArray();
            char[] chars = new char[expected.Length];
            Decoder decoder = Encoding.Unicode.GetDecoder();

            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, expected.Length, expected, "003.1");
        }

        // PosTest4: Call GetChars with ASCII decoder to convert partial of ASCII byte array
        [Fact]
        public void PosTest4()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.UTF8.GetDecoder();

            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            VerificationHelper(decoder, bytes, 0, bytes.Length / 2, chars, 0, bytes.Length / 2, "004.1");
            VerificationHelper(decoder, bytes, bytes.Length / 2, bytes.Length / 2, chars, chars.Length / 2, bytes.Length / 2, "004.2");
        }

        // PosTest5: Call GetChars with Unicode decoder to convert partial of an Unicode byte array
        [Fact]
        public void PosTest5()
        {
            byte[] bytes = new byte[] {
                217,
                143,
                42,
                78,
                0,
                78,
                42,
                78,
                75,
                109,
                213,
                139
            };
            char[] expected = "\u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5".ToCharArray();
            char[] chars = new char[expected.Length];
            Decoder decoder = Encoding.Unicode.GetDecoder();

            VerificationHelper(decoder, bytes, 0, bytes.Length / 2, chars, 0, chars.Length / 2, "005.1");
            VerificationHelper(decoder, bytes, bytes.Length / 2, bytes.Length / 2, chars, 1, chars.Length / 2, "005.2");
        }

        // PosTest6: Call GetChars with ASCII decoder to convert arbitrary byte array
        [Fact]
        public void PosTest6()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.UTF8.GetDecoder();

            TestLibrary.Generator.GetBytes(-55, bytes);

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
        }

        // PosTest7: Call GetChars with Unicode decoder to convert arbitrary byte array
        [Fact]
        public void PosTest7()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.Unicode.GetDecoder();

            TestLibrary.Generator.GetBytes(-55, bytes);

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
        }

        // PosTest8: Call GetChars but convert nothing
        [Fact]
        public void PosTest8()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.UTF8.GetDecoder();
            TestLibrary.Generator.GetBytes(-55, bytes);

            VerificationHelper(decoder, bytes, 0, 0, chars, 0, 0, "008.1");

            decoder = Encoding.Unicode.GetDecoder();
            VerificationHelper(decoder, bytes, 0, 0, chars, 0, 0, "008.2");
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException should be throw when bytes is a null reference or chars is a null reference
        [Fact]
        public void NegTest1()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            VerificationHelper<ArgumentNullException>(decoder, null, 0, 0, chars, 0, typeof(ArgumentNullException), "101.1");
            VerificationHelper<ArgumentNullException>(decoder, bytes, 0, 0, null, 0, typeof(ArgumentNullException), "101.2");
        }

        // NegTest2: ArgumentOutOfRangeException should be throw when byteIndex or byteCount or charIndex is less than zero.
        [Fact]
        public void NegTest2()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 0, -1, chars, 0, typeof(ArgumentOutOfRangeException), "102.1");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 0, 0, chars, -1, typeof(ArgumentOutOfRangeException), "102.2");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, -1, 0, chars, 0, typeof(ArgumentOutOfRangeException), "102.3");
        }

        // NegTest3: ArgumentException should be throw when charCount is less than the resulting number of characters
        [Fact]
        public void NegTest3()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[1];
            TestLibrary.Generator.GetBytes(-55, bytes);

            VerificationHelper<ArgumentException>(decoder, bytes, 0, bytes.Length, chars, 0, typeof(ArgumentException), "103.1");
        }

        // NegTest4: ArgumentOutOfRangeException should be throw when byteindex and byteCount do not denote a valid range in bytes.
        [Fact]
        public void NegTest4()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 1, bytes.Length, chars, 0, typeof(ArgumentOutOfRangeException), "104.1");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, bytes.Length, 1, chars, 0, typeof(ArgumentOutOfRangeException), "104.2");
        }

        // NegTest5: ArgumentOutOfRangeException should be throw when charIndex is not a valid index in chars.
        [Fact]
        public void NegTest5()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 1, 0, chars, chars.Length + 1, typeof(ArgumentOutOfRangeException), "105.1");
        }
        #endregion

        private void VerificationHelper<T>(Decoder decoder, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, Type expected, string errorno) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            });
        }

        private void VerificationHelper(Decoder decoder, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int expected, string errorno)
        {
            int actual = decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper(Decoder decoder, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int expected, char[] expectedChars, string errorno)
        {
            VerificationHelper(decoder, bytes, byteIndex, byteCount, chars, charIndex, expected, errorno + ".1");
            Assert.Equal(expectedChars.Length, chars.Length);
            Assert.Equal(expectedChars, chars);
        }
    }
}
