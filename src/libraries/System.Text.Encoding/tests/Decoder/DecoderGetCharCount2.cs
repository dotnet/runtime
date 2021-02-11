// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    // GetCharCount(System.Byte[],System.Int32,System.Int32,System.Boolean)
    public class DecoderGetCharCount2
    {
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 127;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
        #endregion

        #region Positive Test Cases
        // PosTest1: Call GetCharCount with ASCII decoder and ASCII byte array
        [Fact]
        public void PosTest1()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            int expected = bytes.Length;
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            VerificationHelper(decoder, bytes, 0, bytes.Length, true, expected, "001.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length, false, expected, "001.2");
        }

        // PosTest2: Call GetCharCount with Unicode decoder and ASCII byte array
        [Fact]
        public void PosTest2()
        {
            Decoder decoder = Encoding.Unicode.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            VerificationHelper(decoder, bytes, 0, bytes.Length, true, bytes.Length / 2 + 1, "002.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length, false, bytes.Length / 2, "002.2");
        }

        // PosTest3: Call GetCharCount with Unicode decoder and Unicode byte array
        [Fact]
        public void PosTest3()
        {
            Decoder decoder = Encoding.Unicode.GetDecoder();
            int expected = 6;
            // Unicode string: \u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5
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

            VerificationHelper(decoder, bytes, 0, bytes.Length, true, expected, "003.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length, false, expected, "003.2");
        }

        // PosTest4: Call GetCharCount with Unicode decoder and Arbitrary byte array
        [Fact]
        public void PosTest4()
        {
            Decoder decoder = Encoding.Unicode.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            _generator.GetBytes(-55, bytes);

            decoder.GetCharCount(bytes, 0, bytes.Length, true);
            decoder.GetCharCount(bytes, 0, bytes.Length, false);
        }

        // PosTest5: Call GetCharCount with ASCII decoder and Arbitrary byte array
        [Fact]
        public void PosTest5()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            _generator.GetBytes(-55, bytes);

            decoder.GetCharCount(bytes, 0, bytes.Length, true);
            decoder.GetCharCount(bytes, 0, bytes.Length, false);
        }

        // PosTest6: Call GetCharCount with ASCII decoder and convert partial of ASCII array
        [Fact]
        public void PosTest6()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            int expected = bytes.Length / 2;
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            VerificationHelper(decoder, bytes, 0, expected, true, expected, "006.1");
            VerificationHelper(decoder, bytes, 0, expected, false, expected, "006.2");
            VerificationHelper(decoder, bytes, expected, expected, true, expected, "006.3");
            VerificationHelper(decoder, bytes, 1, expected, false, expected, "006.4");
        }

        // PosTest7: Call GetCharCount with Unicode decoder and convert partial of Unicode byte array
        [Fact]
        public void PosTest7()
        {
            Decoder decoder = Encoding.Unicode.GetDecoder();
            // Unicode string: \u8FD9\u4E2A\u4E00\u4E2A\u6D4B\u8BD5
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
            int expected = 3;

            VerificationHelper(decoder, bytes, 0, bytes.Length / 2, true, expected, "007.1");
            VerificationHelper(decoder, bytes, 0, bytes.Length / 2, false, expected, "007.2");
            VerificationHelper(decoder, bytes, bytes.Length / 2, 0, true, 0, "007.3");
            // Set index to 1, so some characters may be not converted
            VerificationHelper(decoder, bytes, 1, bytes.Length / 2, false, expected, "007.4");
        }

        // PosTest8: Call GetCharCount with ASCII decoder and count = 0
        [Fact]
        public void PosTest8()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            int expected = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            VerificationHelper(decoder, bytes, 0, expected, true, expected, "008.1");
            VerificationHelper(decoder, bytes, 0, expected, false, expected, "008.2");
            VerificationHelper(decoder, bytes, 1, expected, true, expected, "008.3");
            VerificationHelper(decoder, bytes, bytes.Length, expected, false, expected, "008.4");
        }
        #endregion

        private void VerificationHelper(Decoder decoder, byte[] bytes, int index, int count, bool flush, int expected, string errorno)
        {
            int ret = decoder.GetCharCount(bytes, index, count, flush);
            Assert.Equal(expected, ret);
        }
    }
}
