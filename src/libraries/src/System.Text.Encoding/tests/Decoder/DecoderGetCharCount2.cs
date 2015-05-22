// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetCharCount(System.Byte[],System.Int32,System.Int32,System.Boolean)
    public class DecoderGetCharCount2
    {
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 127;
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
            // Unicode string: 这个一个测试
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
            TestLibrary.Generator.GetBytes(-55, bytes);

            decoder.GetCharCount(bytes, 0, bytes.Length, true);
            decoder.GetCharCount(bytes, 0, bytes.Length, false);
        }

        // PosTest5: Call GetCharCount with ASCII decoder and Arbitrary byte array
        [Fact]
        public void PosTest5()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            TestLibrary.Generator.GetBytes(-55, bytes);

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
            // Unicode string: 这个一个测试
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
            // Set index to 1, so some characters may be not coverted
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

        #region Negative Test Cases
        // NegTest1: ArgumentNullException should be thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            VerificationHelper<ArgumentNullException>(decoder, null, 0, 0, true, typeof(ArgumentNullException), "101.1");
            VerificationHelper<ArgumentNullException>(decoder, null, 0, 0, false, typeof(ArgumentNullException), "101.2");
        }

        // NegTest2: ArgumentOutOfRangeException should be thrown when count is less than zero
        [Fact]
        public void NegTest2()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, -1, 0, true, typeof(ArgumentOutOfRangeException), "102.1");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, -1, 0, false, typeof(ArgumentOutOfRangeException), "102.2");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 0, -1, true, typeof(ArgumentOutOfRangeException), "102.3");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 0, -1, false, typeof(ArgumentOutOfRangeException), "102.4");
        }

        // NegTest3: ArgumentOutOfRangeException should be thrown when index and count do not denote a valid range in bytes.
        [Fact]
        public void NegTest3()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];

            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 1, bytes.Length, true, typeof(ArgumentOutOfRangeException), "103.1");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, 1, bytes.Length, false, typeof(ArgumentOutOfRangeException), "103.2");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, bytes.Length, 1, false, typeof(ArgumentOutOfRangeException), "103.3");
            VerificationHelper<ArgumentOutOfRangeException>(decoder, bytes, bytes.Length, 1, true, typeof(ArgumentOutOfRangeException), "103.4");
        }
        #endregion

        private void VerificationHelper(Decoder decoder, byte[] bytes, int index, int count, bool flush, int expected, string errorno)
        {
            int ret = decoder.GetCharCount(bytes, index, count, flush);
            Assert.Equal(expected, ret);
        }

        private void VerificationHelper<T>(Decoder decoder, byte[] bytes, int index, int count, bool flush, Type expected, string errorno) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                int ret = decoder.GetCharCount(bytes, index, count, flush);
            });
        }
    }
}
