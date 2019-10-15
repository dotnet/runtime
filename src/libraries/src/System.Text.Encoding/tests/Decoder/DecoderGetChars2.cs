// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetChars(System.Byte[],System.Int32,System.Int32,System.Char[],System.Int32,System.Boolean)
    public class DecoderGetChars2
    {
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 127;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
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

            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, true, bytes.Length, expectedChars, "001.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, false, bytes.Length, expectedChars, "001.2");
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

            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, true, expectedChars.Length, expectedChars, "002.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, false, expectedChars.Length, expectedChars, "002.2");
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

            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, true, expected.Length, expected, "003.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length, chars, 0, false, expected.Length, expected, "003.2");
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

            VerificationHelper(decoder, bytes, 0, bytes.Length / 2, chars, 0, true, bytes.Length / 2, "004.1");
            VerificationHelper(decoder, bytes, bytes.Length / 2, bytes.Length / 2, chars, chars.Length / 2, true, bytes.Length / 2, "004.2");
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

            VerificationHelper(decoder, bytes, 0, bytes.Length / 2, chars, 0, true, chars.Length / 2, "005.1");
            VerificationHelper(decoder, bytes, bytes.Length / 2, bytes.Length / 2, chars, 1, true, chars.Length / 2, "005.2");
        }

        // PosTest6: Call GetChars with ASCII decoder to convert arbitrary byte array
        [Fact]
        public void PosTest6()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.UTF8.GetDecoder();

            _generator.GetBytes(-55, bytes);

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
        }

        // PosTest7: Call GetChars with Unicode decoder to convert arbitrary byte array
        [Fact]
        public void PosTest7()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.Unicode.GetDecoder();

            _generator.GetBytes(-55, bytes);

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
        }

        // PosTest8: Call GetChars but convert nothing
        [Fact]
        public void PosTest8()
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            char[] chars = new char[bytes.Length];
            Decoder decoder = Encoding.UTF8.GetDecoder();
            _generator.GetBytes(-55, bytes);

            VerificationHelper(decoder, bytes, 0, 0, chars, 0, true, 0, "008.1");

            decoder = Encoding.Unicode.GetDecoder();
            VerificationHelper(decoder, bytes, 0, 0, chars, 0, false, 0, "008.2");
        }
        #endregion

        private void VerificationHelper(Decoder decoder, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush, int expected, string errorno)
        {
            int actual = decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush);
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper(Decoder decoder, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush, int expected, char[] expectedChars, string errorno)
        {
            VerificationHelper(decoder, bytes, byteIndex, byteCount, chars, charIndex, flush, expected, errorno + ".1");

            Assert.Equal(expectedChars.Length, chars.Length);
            Assert.Equal(expectedChars, chars);
        }
    }
}
