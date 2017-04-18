// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Text.Encodings.Tests
{
    public class EncoderMiscTests
    {
        [Fact]
        public static unsafe void ConvertTest()
        {
            string s1 = "This is simple ascii string";
            string s2 = "\uD800\uDC00\u0200";           // non ascii letters
            string s3 = s1 + s2;

            Encoding utf8 = Encoding.UTF8;
            Encoder encoder = utf8.GetEncoder();

            byte [] bytes1 = utf8.GetBytes(s1); 
            byte [] bytes2 = utf8.GetBytes(s2); 
            byte [] bytes3 = utf8.GetBytes(s3); 

            byte [] bytes = new byte[50];

	        int bytesUsed;
	        int charsUsed;
	        bool completed;

            fixed (char *pChars1 = s1)
            fixed (char *pChars2 = s2)
            fixed (char *pChars3 = s3)
            fixed (byte *pBytes = bytes)
            {
                encoder.Convert(pChars1, s1.Length, pBytes, bytes.Length, true, out charsUsed, out bytesUsed, out completed);
                Assert.Equal(s1.Length, charsUsed);
                Assert.Equal(bytes1.Length, bytesUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes1");                
                for (int i=0; i<bytes1.Length; i++) { Assert.Equal(bytes1[i], pBytes[i]); }

                encoder.Convert(pChars2, s2.Length, pBytes, bytes.Length, true, out charsUsed, out bytesUsed, out completed);
                Assert.Equal(s2.Length, charsUsed);
                Assert.Equal(bytes2.Length, bytesUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes2");                
                for (int i=0; i<bytes2.Length; i++) { Assert.Equal(bytes2[i], pBytes[i]); }

                encoder.Convert(pChars3, s3.Length, pBytes, bytes.Length, true, out charsUsed, out bytesUsed, out completed);
                Assert.Equal(s3.Length, charsUsed);
                Assert.Equal(bytes3.Length, bytesUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes3");                
                for (int i=0; i<bytes3.Length; i++) { Assert.Equal(bytes3[i], pBytes[i]); }

                encoder.Convert(pChars3 + s1.Length, s3.Length - s1.Length, pBytes, bytes.Length, true, out charsUsed, out bytesUsed, out completed);
                Assert.Equal(s2.Length, charsUsed);
                Assert.Equal(bytes2.Length, bytesUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes3+bytes1.Length");                
                for (int i=0; i<bytes2.Length; i++) { Assert.Equal(bytes2[i], pBytes[i]); }

                encoder.Convert(pChars3 + s1.Length, s3.Length - s1.Length, pBytes, 4, true, out charsUsed, out bytesUsed, out completed);
                Assert.Equal(2, charsUsed);
                Assert.True(bytes2.Length > bytesUsed, "Expected to use less bytes when there is not enough char buffer");
                Assert.False(completed, "Expected to have the operation not fully completed");                
                for (int i=0; i<bytesUsed; i++) { Assert.Equal(bytes2[i], pBytes[i]); }

                encoder.Convert(pChars3 + s3.Length - 1, 1, pBytes, 2, true, out charsUsed, out bytesUsed, out completed);
                Assert.Equal(1, charsUsed);
                Assert.Equal(2, bytesUsed);
                Assert.True(completed, "expected to flush the remaining character");                
                Assert.Equal(bytes2[bytes2.Length - 2], pBytes[0]);
                Assert.Equal(bytes2[bytes2.Length - 1], pBytes[1]);
            }
        }

        [Fact]
        public static unsafe void ConvertNegativeTest()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
	        int bytesUsed;
	        int charsUsed;
	        bool completed;

            string chars = "\u0D800\uDC00";
            byte [] bytes = new byte[4];

            fixed (byte *bytesPtr = bytes)
            fixed (char *charsPtr = chars)
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;

                AssertExtensions.Throws<ArgumentNullException>("chars", () => encoder.Convert(null, chars.Length, pBytes, bytes.Length, true, out charsUsed, out bytesUsed, out completed));
                AssertExtensions.Throws<ArgumentNullException>("bytes", () => encoder.Convert(pChars, chars.Length, null, bytes.Length, true, out charsUsed, out bytesUsed, out completed));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("byteCount", () => encoder.Convert(pChars, chars.Length, pBytes, -1, true, out charsUsed, out bytesUsed, out completed));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => encoder.Convert(pChars, -1, pBytes, bytes.Length, true, out charsUsed, out bytesUsed, out completed));

                AssertExtensions.Throws<ArgumentException>("bytes", () => encoder.Convert(pChars, chars.Length, pBytes, 0, true, out charsUsed, out bytesUsed, out completed));
            }

            encoder = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback()).GetEncoder();

            fixed (char *charsPtr = "\uFFFF")
            fixed (byte *bytesPtr = new byte [2])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;
                
                Assert.Throws<EncoderFallbackException>(() => encoder.Convert(pChars, 1, pBytes, 2, true, out charsUsed, out bytesUsed, out completed));
            }
        }

        [Fact]
        public static unsafe void GetBytesTest()
        {
            string s1 = "This is simple ascii string";
            string s2 = "\uD800\uDC00\u0200";           // non ascii letters
            string s3 = s1 + s2;

            Encoding utf8 = Encoding.UTF8;
            Encoder encoder = utf8.GetEncoder();
            byte [] bytes = new byte[200];

            byte [] bytes1 = utf8.GetBytes(s1);
            byte [] bytes2 = utf8.GetBytes(s2);
            byte [] bytes3 = utf8.GetBytes(s3);

            fixed (char *pChars1 = s1)
            fixed (char *pChars2 = s2)
            fixed (char *pChars3 = s3)
            fixed (byte *pBytes = bytes)
            {
                int bytesUsed = encoder.GetBytes(pChars1, s1.Length, pBytes, bytes.Length, true);
                Assert.Equal(bytes1.Length, bytesUsed);
                Assert.Equal(bytes1.Length, encoder.GetByteCount(pChars1, s1.Length, true));
                for (int i=0; i<bytesUsed; i++) { Assert.Equal(bytes1[i], pBytes[i]); }
                
                bytesUsed = encoder.GetBytes(pChars2, s2.Length, pBytes, bytes.Length, true);
                Assert.Equal(bytes2.Length, bytesUsed);
                Assert.Equal(bytes2.Length, encoder.GetByteCount(pChars2, s2.Length, true));
                for (int i=0; i<bytesUsed; i++) { Assert.Equal(bytes2[i], pBytes[i]); }

                bytesUsed = encoder.GetBytes(pChars3, s3.Length, pBytes, bytes.Length, true);
                Assert.Equal(bytes3.Length, bytesUsed);
                Assert.Equal(bytes3.Length, encoder.GetByteCount(pChars3, s3.Length, true));
                for (int i=0; i<bytesUsed; i++) { Assert.Equal(bytes3[i], pBytes[i]); }

                bytesUsed = encoder.GetBytes(pChars3 + s1.Length, s3.Length - s1.Length, pBytes, bytes.Length, true);
                Assert.Equal(bytes2.Length, bytesUsed);
                Assert.Equal(bytes2.Length, encoder.GetByteCount(pChars3 + s1.Length, s3.Length - s1.Length, true));
                for (int i=0; i<bytesUsed; i++) { Assert.Equal(bytes2[i], pBytes[i]); }
            }
        }

        [Fact]
        public static unsafe void GetBytesNegativeTest()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            string s = "\u0D800\uDC00";

            fixed (char *charsPtr = s)
            fixed (byte *bytesPtr = new byte [4])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;

                AssertExtensions.Throws<ArgumentNullException>("bytes", () => encoder.GetBytes(pChars, s.Length, null, 1, true));
                AssertExtensions.Throws<ArgumentNullException>("chars", () => encoder.GetBytes(null, s.Length, pBytes, 4, true));
                AssertExtensions.Throws<ArgumentNullException>("chars", () => encoder.GetByteCount(null, s.Length, true));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => encoder.GetBytes(pChars, -1, pBytes, 4, true));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("byteCount", () => encoder.GetBytes(pChars, s.Length, pBytes, -1, true));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => encoder.GetByteCount(pChars, -1, true));

                AssertExtensions.Throws<ArgumentException>("bytes", () => encoder.GetBytes(pChars, s.Length, pBytes, 1, true));
            }

            encoder = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback()).GetEncoder();

            fixed (char *charsPtr = "\uFFFF")
            fixed (byte *bytesPtr = new byte [2])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;
                
                Assert.Throws<EncoderFallbackException>(() => encoder.GetBytes(pChars, 1, pBytes, 2, true));
                Assert.Throws<EncoderFallbackException>(() => encoder.GetByteCount(pChars, 1, true));
            }
        }

        [Fact]
        public static void EncoderExceptionFallbackBufferTest()
        {
            Encoder encoder = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback()).GetEncoder();

            char [] chars = new char[] { '\uFFFF' };
            byte [] bytes = new byte[2];

            Assert.Throws<EncoderFallbackException>(() => encoder.GetBytes(chars, 0, 1, bytes, 0, true));

            EncoderFallbackBuffer fallbackBuffer = encoder.FallbackBuffer;
            Assert.True(fallbackBuffer is EncoderExceptionFallbackBuffer, "Expected to get EncoderExceptionFallbackBuffer type");
            Assert.Throws<EncoderFallbackException>(() => fallbackBuffer.Fallback(chars[0], 0));
            Assert.Throws<EncoderFallbackException>(() => fallbackBuffer.Fallback('\u0040', 0));
            Assert.Throws<EncoderFallbackException>(() => fallbackBuffer.Fallback('\uD800', '\uDC00', 0));

            Assert.Equal(0, fallbackBuffer.Remaining);
            Assert.Equal('\u0000', fallbackBuffer.GetNextChar());

            Assert.False(fallbackBuffer.MovePrevious(), "MovePrevious expected to always return false");

            fallbackBuffer.Reset();

            Assert.Equal(0, fallbackBuffer.Remaining);
            Assert.Equal('\u0000', fallbackBuffer.GetNextChar());
            
        }

        [Fact]
        public static void EncoderReplacementFallbackBufferTest()
        {
            Encoder encoder = Encoding.GetEncoding("us-ascii", new EncoderReplacementFallback(), new DecoderReplacementFallback()).GetEncoder();

            char [] chars = new char [] { '\uFFFF' };
            byte [] bytes = new byte [2];

            EncoderFallbackBuffer fallbackBuffer = encoder.FallbackBuffer;
            Assert.True(fallbackBuffer is EncoderReplacementFallbackBuffer, "Expected to get EncoderReplacementFallbackBuffer type");
            Assert.True(fallbackBuffer.Fallback(chars[0], 0), "Expected we fallback on the given buffer");
            Assert.Equal(1, fallbackBuffer.Remaining);
            Assert.False(fallbackBuffer.MovePrevious(), "Expected we cannot move back on the replacement buffer as we are at the Beginning of the buffer");
            Assert.Equal('?', fallbackBuffer.GetNextChar());
            Assert.True(fallbackBuffer.MovePrevious(), "Expected we can move back on the replacement buffer");
            Assert.Equal('?', fallbackBuffer.GetNextChar());

            fallbackBuffer.Reset();
            Assert.Equal(0, fallbackBuffer.Remaining);
            Assert.Equal('\u0000', fallbackBuffer.GetNextChar());
            Assert.False(fallbackBuffer.MovePrevious(), "Expected we cannot move back on the replacement buffer as we are rest the buffer");
        }
    }
}
