// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    // GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32,System.Boolean)
    public class EncoderGetBytes2
    {
        private const int c_SIZE_OF_ARRAY = 256;
        private const char HIGH_SURROGATE_START = '\ud800';
        private const char HIGH_SURROGATE_END = '\udbff';
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        public static IEnumerable<object[]> Encoders_RandomInput()
        {
            yield return new object[] { Encoding.ASCII.GetEncoder(), 3 };
            yield return new object[] { Encoding.UTF8.GetEncoder(), 3 };
            yield return new object[] { Encoding.Unicode.GetEncoder(), 2 };
        }
      
        public static IEnumerable<object[]> Encoders_ASCIIInput()
        {
            yield return new object[] { Encoding.ASCII.GetEncoder(), 1, new int[] { 1, 0 } };
            yield return new object[] { Encoding.UTF8.GetEncoder(), 1, new int[] { 1, 0 } };
            yield return new object[] { Encoding.Unicode.GetEncoder(), 2, new int[] { 0, 1 } };
        }
        
        public static IEnumerable<object[]> Encoders_UnicodeInput()
        {
            yield return new object[] { Encoding.ASCII.GetEncoder(), new byte[9], "\u8FD9\u4E2A\u4E00\u4E2AABC\u6D4B\u8BD5".ToCharArray(), 8, 1 };
            yield return new object[] { Encoding.UTF8.GetEncoder(), new byte[21], "\u8FD9\u4E2A\u4E00\u4E2AABC\u6D4B\u8BD5".ToCharArray(), 18, 3 };
            yield return new object[] { Encoding.Unicode.GetEncoder(), new byte[27], "\u8FD9\u4E2A\u4E00\u4E2AABC\u6D4B\u8BD5".ToCharArray(), 16, 2 };
        }
        
        public static IEnumerable<object[]> Encoders_MixedInput()
        {
            yield return new object[] { Encoding.ASCII.GetEncoder(), 1, 1, 1 };
            yield return new object[] { Encoding.UTF8.GetEncoder(), 1, 3, 1 };
            yield return new object[] { Encoding.Unicode.GetEncoder(), 2, 2, 2 };
        }
        
        // Call GetBytes to convert an arbitrary character array by using different encoders
        [Theory]
        [MemberData(nameof(Encoders_RandomInput))]
        public void EncoderGetBytesRandomInput(Encoder encoder, int step)
        {
            char[] chars = new char[c_SIZE_OF_ARRAY];
            byte[] bytes = new byte[c_SIZE_OF_ARRAY * 4];
            for (int i = 0; i < chars.Length; ++i)
            {
                chars[i] = _generator.GetChar(-55);
            }

            int ret1 = encoder.GetBytes(chars, 0, chars.Length, bytes, 0, flush: true);
            int ret2 = encoder.GetBytes(chars, 0, chars.Length, bytes, 0, flush: false);

            // If the last character is a surrogate and the encoder is flushing its state, it will return a fallback character. 
            // When the encoder isn't flushing its state then it holds on to the remnant bytes between calls so that if the next
            // bytes passed in form a valid character you'd get that char as a result.
            // The difference in length of a low surrogate flushed vs non-flushed is 1 
            if (IsHighSurrogate(chars[chars.Length - 1]))
            {
                ret2 += step;
                encoder.GetBytes(chars, 0, chars.Length, bytes, 0, flush: true);
            }
            Assert.Equal(ret2, ret1);

            ret1 = encoder.GetBytes(chars, 0, 0, bytes, 0, flush: true);
            ret2 = encoder.GetBytes(chars, 0, 0, bytes, 0, flush: false);
            Assert.Equal(ret2, ret1);
            Assert.Equal(0, ret1);
        }

        // Call GetBytes to convert an ASCII character array by using different encoders
        [Theory]
        [MemberData(nameof(Encoders_ASCIIInput))]
        public void EncoderGetBytesASCIIInput(Encoder encoder, int size, int[] partialStart)
        {
            Assert.Equal(2, partialStart.Length);
            
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()_+-=\\|/?<>  ,.`~".ToCharArray();
            byte[] bytes = new byte[chars.Length * size];
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, flush: true, expectedRetVal: chars.Length * size);
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, flush: false, expectedRetVal: chars.Length * size);

            // partial conversion
            VerificationHelper(encoder, chars, partialStart[0], chars.Length - 1, bytes, 0, flush: true, expectedRetVal: (chars.Length - 1) * size);
            VerificationHelper(encoder, chars, partialStart[1], 1, bytes, 1, flush: false, expectedRetVal: size);
        }

        // Call GetBytes to convert an Unicode character array by using different encoders
        [Theory]
        [MemberData(nameof(Encoders_UnicodeInput))]
        public void EncoderGetBytesUnicodeInput(Encoder encoder, byte[] bytes, char[] chars, int expectedPartial, int expectedComplete)
        {
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, flush: true, expectedRetVal: expectedPartial + expectedComplete);
            VerificationHelper(encoder, chars, 0, chars.Length, bytes, 0, flush: false, expectedRetVal: expectedPartial + expectedComplete);

            // partial conversion
            VerificationHelper(encoder, chars, 1, chars.Length - 1, bytes, 1, flush: true, expectedRetVal: expectedPartial);
            VerificationHelper(encoder, chars, 1, 1, bytes, 1, flush: false, expectedRetVal: expectedComplete);
        }
        
        // Call GetBytes to convert an ASCIIUnicode character array by different encoders
        [Theory]
        [MemberData(nameof(Encoders_MixedInput))]
        public void EncoderGetBytesMixedInput(Encoder encoder, int asciiSize, int unicodeSize0, int unicodeSize1)
        {
            // Bytes does not have enough capacity to accomodate result
            string s = "T\uD83D\uDE01est";
            char[] c = s.ToCharArray();
            
            EncoderGetBytesMixedInput(encoder, c, 2, asciiSize, unicodeSize0, unicodeSize1);
            EncoderGetBytesMixedInput(encoder, c, 3, asciiSize, unicodeSize0, unicodeSize1);
            EncoderGetBytesMixedInput(encoder, c, 4, asciiSize, unicodeSize0, unicodeSize1);
            EncoderGetBytesMixedInput(encoder, c, 5, asciiSize, unicodeSize0, unicodeSize1);
        }

        // Call GetBytes to convert an ASCIIUnicode character array by different encoders
        [Theory]
        [MemberData(nameof(Encoders_MixedInput))]
        public void EncoderGetBytesMixedInputBufferTooSmall(Encoder encoder, int asciiSize, int unicodeSize0, int unicodeSize1)
        {
            // Bytes does not have enough capacity to accomodate result
            string s = "T\uD83D\uDE01est";
            char[] c = s.ToCharArray();           
            
            EncoderGetBytesMixedInputThrows(encoder, c, 2, asciiSize, unicodeSize0, unicodeSize1);
            EncoderGetBytesMixedInputThrows(encoder, c, 3, asciiSize, unicodeSize0, unicodeSize1);
            EncoderGetBytesMixedInputThrows(encoder, c, 4, asciiSize, unicodeSize0, unicodeSize1);
            EncoderGetBytesMixedInputThrows(encoder, c, 5, asciiSize, unicodeSize0, unicodeSize1);
        }

        private void EncoderGetBytesMixedInput(Encoder encoder, char[] chars, int length, int asciiSize, int unicodeSize0, int unicodeSize1)
        {
            int byteLength = asciiSize 
                + Clamp(length - 1, 0, 1) * unicodeSize0 
                + Clamp(length - 2, 0, 1) * unicodeSize1 
                + Math.Max(length - 3, 0) * asciiSize;
            byte[] b = new byte[byteLength];
            Assert.Equal(byteLength, encoder.GetBytes(chars, 0, length, new byte[byteLength], 0, flush: true));
            
            // Fixed buffer so make larger
            b = new byte[20];
            VerificationFixedEncodingHelper(encoder, chars, length, b, byteLength);
        }

        private void EncoderGetBytesMixedInputThrows(Encoder encoder, char[] chars, int length, int asciiSize, int unicodeSize0, int unicodeSize1)
        {
            int byteLength = asciiSize 
                + Clamp(length - 1, 0, 1) * unicodeSize0 
                + Clamp(length - 2, 0, 1) * unicodeSize1 
                + Math.Max(length - 3, 0) * asciiSize;
            byteLength -= 1;
            
            byte[] b = new byte[byteLength];
            
            AssertExtensions.Throws<ArgumentException>("bytes", () => encoder.GetBytes(chars, 0, length, new byte[byteLength], 0, flush: true));
            
            // Fixed buffer so make larger
            b = new byte[20];
            AssertExtensions.Throws<ArgumentException>("bytes", () => VerificationFixedEncodingHelper(encoder, chars, length, b, byteLength));
        }
        
        private static unsafe void VerificationFixedEncodingHelper(Encoder encoder, char[] c, int charCount, byte[] b, int byteCount)
        {
            fixed (char* pChar = c)
            fixed (byte* pByte = b)
            {
                Assert.Equal(byteCount, encoder.GetBytes(pChar, charCount, pByte, byteCount, flush: true));
            }
        }
        
        private void VerificationHelper(Encoder encoder, char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex,
            bool flush, int expectedRetVal)
        {
            int actualRetVal = encoder.GetBytes(chars, charIndex, charCount, bytes, byteIndex, flush);
            Assert.Equal(expectedRetVal, actualRetVal);
        }

        private bool IsHighSurrogate(char c)
        {
            return ((c >= HIGH_SURROGATE_START) && (c <= HIGH_SURROGATE_END));
        }
        
        private static int Clamp(int value, int min, int max)  
        {  
            return (value < min) ? min : (value > max) ? max : value;  
        }
    }
}
