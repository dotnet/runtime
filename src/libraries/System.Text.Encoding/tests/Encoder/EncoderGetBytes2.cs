// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32,System.Boolean)
    public class EncoderGetBytes2
    {
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 256;
        private const char HIGH_SURROGATE_START = '\ud800';
        private const char HIGH_SURROGATE_END = '\udbff';
        #endregion

        #region Positive Test Cases
        // PosTest1: Call GetBytes to convert an arbitrary character array by using ASCII encoder
        [Fact]
        public void PosTest1()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY * 4];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = TestLibrary.Generator.GetChar(-55);
            }
            Encoder encoder = Encoding.UTF8.GetEncoder();

            int ret1 = encoder.GetBytes(chars, 0, chars.Length, bytes, 0, true);
            int ret2 = encoder.GetBytes(chars, 0, chars.Length, bytes, 0, false);

            // If the last character is a surrogate and the encoder is flushing its state, it will return a fallback character. 
            // When the encoder isn't flushing its state then it holds on to the remnant bytes between calls so that if the next
            // bytes passed in form a valid character you'd get that char as a result.
            // The difference in length of a low surrogate flushed vs non-flushed is 1 
            if (IsHighSurrogate(chars[chars.Length - 1]))
            {
                ret2 += 3;
                encoder.GetBytes(chars, 0, chars.Length, bytes, 0, true);
            }
            Assert.Equal(ret2, ret1);

            ret1 = encoder.GetBytes(chars, 0, 0, bytes, 0, true);
            ret2 = encoder.GetBytes(chars, 0, 0, bytes, 0, false);
            Assert.Equal(ret2, ret1);
            Assert.Equal(0, ret1);
        }

        // PosTest2: Call GetBytes to convert an arbitrary character array by using unicode encoder
        [Fact]
        public void PosTest2()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[chars.Length * 4];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = TestLibrary.Generator.GetChar(-55);
            }
            Encoder encoder = Encoding.Unicode.GetEncoder();

            int ret1 = encoder.GetBytes(chars, 0, chars.Length, bytes, 0, true);
            int ret2 = encoder.GetBytes(chars, 0, chars.Length, bytes, 0, false);

            // If the last character is a surrogate and the encoder is flushing its state, it will return a fallback character. 
            // When the encoder isn't flushing its state then it holds on to the remnant bytes between calls so that if the next
            // bytes passed in form a valid character you'd get that char as a result
            if (IsHighSurrogate(chars[chars.Length - 1]))
            {
                ret2 += 2;
                // If there is a leftover surrogate from the last call then not flushing the buffer will have an effect on the next call.
                // Flush the buffer before using the same encoder a second time. We don't care about the output of this call
                encoder.GetBytes(chars, 0, chars.Length, bytes, 0, true);
            }
            Assert.Equal(ret2, ret1);
            ret1 = encoder.GetBytes(chars, 0, 0, bytes, 0, true);
            ret2 = encoder.GetBytes(chars, 0, 0, bytes, 0, false);
            Assert.Equal(ret2, ret1);
            Assert.Equal(0, ret1);
        }

        // PosTest3: Call GetBytes to convert an ASCII character array by using ASCII encoder
        [Fact]
        public void PosTest3()
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()_+-=\\|/?<>  ,.`~".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, true, chars.Length, "003.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, false, chars.Length, "003.2");

            // partial convertion
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 0, true, chars.Length - 1, "003.3");
            VerificationHelper(encoder, chars, 0, 1, bytes, 1, false, 1, "003.4");
        }

        // PosTest4: Call GetBytes to convert an ASCII character array by using Unicode encoder
        [Fact]
        public void PosTest4()
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()_+-=\\|/?<>  ,.`~".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, true, chars.Length * 2, "004.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, false, chars.Length * 2, "004.2");

            // partial convertion
            VerificationHelper(encoder, chars, 0, chars.Length - 1, bytes, 0, true, (chars.Length - 1) * 2, "004.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, false, 2, "004.4");
        }

        // PosTest6: Call GetBytes to convert an unicode character array by using unicode encoder
        [Fact]
        public void PosTest5()
        {
            char[] chars = "\u8FD9\u4E2A\u4E00\u4E2AABC\u6D4B\u8BD5".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, true, chars.Length * 2, "006.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, false, chars.Length * 2, "006.2");

            // partial convertion
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 1, true, (chars.Length - 1) * 2, "006.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, false, 2, "006.4");
        }
        #endregion

        #region Nagetive Test Cases
        // NegTest1: ArgumentNullException should be thrown when chars is a null reference or bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper<ArgumentNullException>(encoder, null, 0, 0, new byte[1], 0, true, typeof(ArgumentNullException), "101.1");
            VerificationHelper<ArgumentNullException>(encoder, new char[1], 0, 0, null, 0, true, typeof(ArgumentNullException), "101.2");
            VerificationHelper<ArgumentNullException>(encoder, null, 0, 0, new byte[1], 0, false, typeof(ArgumentNullException), "101.3");
            VerificationHelper<ArgumentNullException>(encoder, new char[1], 0, 0, null, 0, false, typeof(ArgumentNullException), "101.4");
        }

        // NegTest2: ArgumentOutOfRangeException should be thrown when charIndex or charCount or byteIndex is less than zero.
        [Fact]
        public void NegTest2()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper<ArgumentOutOfRangeException>(encoder, new char[1], 0, -1, new byte[1], 0, true, typeof(ArgumentOutOfRangeException), "102.1");
            VerificationHelper<ArgumentOutOfRangeException>(encoder, new char[1], 0, 0, new byte[1], -1, true, typeof(ArgumentOutOfRangeException), "102.2");
            VerificationHelper<ArgumentOutOfRangeException>(encoder, new char[1], -1, 0, new byte[1], -1, true, typeof(ArgumentOutOfRangeException), "102.3");
        }

        // NegTest3: ArgumentException should be thrown when byteCount is less than the resulting number of bytes
        [Fact]
        public void NegTest3()
        {
            Encoder encoder = Encoding.Unicode.GetEncoder();
            char[] chars = new char[c_SIZE_OF_ARRAY];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = TestLibrary.Generator.GetChar(-55);
            }
            byte[] bytes1 = new byte[1];

            VerificationHelper<ArgumentException>(encoder, chars, 0, chars.Length, bytes1, 1, true, typeof(ArgumentException), "103.1");
            VerificationHelper<ArgumentException>(encoder, chars, 0, chars.Length, bytes1, 1, false, typeof(ArgumentException), "103.2");
        }

        // NegTest4: ArgumentOutOfRangeException should be thrown when charIndex and charCount do not denote a valid range in chars
        [Fact]
        public void NegTest4()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper<ArgumentOutOfRangeException>(encoder, new char[1], 1, 1, new byte[1], 0, true, typeof(ArgumentOutOfRangeException), "104.1");
            VerificationHelper<ArgumentOutOfRangeException>(encoder, new char[1], 0, 2, new byte[1], 0, true, typeof(ArgumentOutOfRangeException), "104.2");
        }

        // NegTest5: ArgumentOutOfRangeException should be thrown when byteIndex is not a valid index in bytes
        [Fact]
        public void NegTest5()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper<ArgumentOutOfRangeException>(encoder, new char[1], 0, 1, new byte[1], 2, true, typeof(ArgumentOutOfRangeException), "105.1");
        }
        #endregion

        private void VerificationHelper(Encoder encoder, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex,
            bool flush, int expectedRetVal, string errorno)
        {
            int actualRetVal = encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, flush);
            Assert.Equal(expectedRetVal, actualRetVal);
        }

        private void VerificationHelper<T>(Encoder encoder, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex,
            bool flush, Type expected, string errorno) where T : Exception
        {
            string str = new string(chars);

            Assert.Throws<T>(() =>
            {
                int actualRetVal = encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, flush);
            });
        }

        private bool IsHighSurrogate(char c)
        {
            return ((c >= HIGH_SURROGATE_START) && (c <= HIGH_SURROGATE_END));
        }
    }
}
