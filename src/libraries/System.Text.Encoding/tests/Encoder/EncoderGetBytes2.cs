// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32,System.Boolean)
    public class EncoderGetBytes2
    {
        #region Private Fields
        private const int c_SIZE_OF_ARRAY = 256;
        private const char HIGH_SURROGATE_START = '\ud800';
        private const char HIGH_SURROGATE_END = '\udbff';
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
        #endregion

        #region Positive Test Cases
        // PosTest1: Call GetBytes to convert an arbitrary character array by using UTF8 encoder
        [Fact]
        public void PosTest1()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY * 4];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = _generator.GetChar(-55);
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

        // PosTest2: Call GetBytes to convert an arbitrary character array by using Unicode encoder
        [Fact]
        public void PosTest2()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[chars.Length * 4];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = _generator.GetChar(-55);
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

        // PosTest3: Call GetBytes to convert an ASCII character array by using UTF8 encoder
        [Fact]
        public void PosTest3()
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()_+-=\\|/?<>  ,.`~".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.UTF8.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, true, chars.Length, "003.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, false, chars.Length, "003.2");

            // partial conversion
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

            // partial conversion
            VerificationHelper(encoder, chars, 0, chars.Length - 1, bytes, 0, true, (chars.Length - 1) * 2, "004.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, false, 2, "004.4");
        }

        // PosTest6: Call GetBytes to convert an Unicode character array by using Unicode encoder
        [Fact]
        public void PosTest5()
        {
            char[] chars = "\u8FD9\u4E2A\u4E00\u4E2AABC\u6D4B\u8BD5".ToCharArray();
            byte[] bytes = new byte[chars.Length * 2];
            Encoder encoder = Encoding.Unicode.GetEncoder();
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, true, chars.Length * 2, "005.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, false, chars.Length * 2, "005.2");

            // partial conversion
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 1, true, (chars.Length - 1) * 2, "005.3");
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, false, 2, "005.4");
        }
        
        // PosTest6: Call GetBytes to convert an arbitrary character array by using ASCII encoder
        [Fact]
        public void PosTest6()
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY * 4];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = _generator.GetChar(-55);
            }
            Encoder encoder = Encoding.ASCII.GetEncoder();

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
        
        // PosTest7: Call GetBytes to convert an ASCII character array by using ASCII encoder
        [Fact]
        public void PosTest7()
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()_+-=\\|/?<>  ,.`~".ToCharArray();
            byte[] bytes = new byte[chars.Length];
            Encoder encoder = Encoding.ASCII.GetEncoder();

            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, true, chars.Length, "007.1");
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, false, chars.Length, "007.2");

            // partial conversion
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 0, true, chars.Length - 1, "007.3");
            VerificationHelper(encoder, chars, 0, 1, bytes, 1, false, 1, "007.4");
        }
        #endregion

        // NegTest7: Call GetBytes to convert an ASCIIUnicode character array by using UTF8 encoder
        [Fact]
        public void NegTest1()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            // Bytes does not have enough capacity to accomodate result
            string s = "T😁est";
            char[] c = s.ToCharArray();           
            Assert.Throws<ArgumentException>("bytes", () => encoder.GetBytes(c, 0, 2, new byte[3], 0, true));
            Assert.Throws<ArgumentException>("bytes", () => encoder.GetBytes(c, 0, 3, new byte[4], 0, true));
            Assert.Throws<ArgumentException>("bytes", () => encoder.GetBytes(c, 0, 4, new byte[5], 0, true));
            Assert.Throws<ArgumentException>("bytes", () => encoder.GetBytes(c, 0, 5, new byte[6], 0, true));

            byte[] b = new byte[8];
            Assert.Throws<ArgumentException>("bytes", () => FixedEncodingHelper(c, 2, b, 3));
            Assert.Throws<ArgumentException>("bytes", () => FixedEncodingHelper(c, 3, b, 4));
            Assert.Throws<ArgumentException>("bytes", () => FixedEncodingHelper(c, 4, b, 5));
            Assert.Throws<ArgumentException>("bytes", () => FixedEncodingHelper(c, 5, b, 6));
        }
        
        private static unsafe void FixedEncodingHelper(char[] c, int charCount, byte[] b, int byteCount)
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            fixed(char* pChar = c)
            fixed(byte* pByte = b)
            {
                encoder.GetBytes(pChar, charCount, pByte, byteCount, true);
            }
        }
        
        private void VerificationHelper(Encoder encoder, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex,
            bool flush, int expectedRetVal, string errorno)
        {
            int actualRetVal = encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, flush);
            Assert.Equal(expectedRetVal, actualRetVal);
        }

        private bool IsHighSurrogate(char c)
        {
            return ((c >= HIGH_SURROGATE_START) && (c <= HIGH_SURROGATE_END));
        }
    }
}
