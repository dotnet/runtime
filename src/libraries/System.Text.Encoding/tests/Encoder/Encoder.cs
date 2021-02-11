// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Theory]
        [InlineData(new char[] { '\ud800' }, new char[] { }, -1)]
        [InlineData(new char[] { '\ud800' }, new char[] { 'x' }, -1)]
        [InlineData(new char[] { '\ud800' }, new char[] { '\ud800' }, -1)]
        [InlineData(new char[] { '\ud800' }, new char[] { '\udfff', '\udfff' }, 1)]
        public static void EncoderFallbackExceptionIndexTests(char[] firstPayload, char[] secondPayload, int expectedIndex)
        {
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            // First test GetBytes / GetBytes

            Encoder encoder = encoding.GetEncoder();
            Assert.Equal(0, encoder.GetBytes(firstPayload, 0, firstPayload.Length, new byte[0], 0, flush: false));

            EncoderFallbackException ex = Assert.Throws<EncoderFallbackException>(
                () => encoder.GetBytes(secondPayload, 0, secondPayload.Length, new byte[8], 0, flush: true));
            Assert.Equal(expectedIndex, ex.Index);

            // Then test GetBytes / GetByteCount

            encoder = encoding.GetEncoder();
            Assert.Equal(0, encoder.GetBytes(firstPayload, 0, firstPayload.Length, new byte[0], 0, flush: false));

            ex = Assert.Throws<EncoderFallbackException>(
                () => encoder.GetByteCount(secondPayload, 0, secondPayload.Length, flush: true));
            Assert.Equal(expectedIndex, ex.Index);
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

        [Fact]
        public void EncoderConvertSplitAcrossInvalidSurrogateTests()
        {
            // Input = [ D800 0058 0059 005A ]
            // Expected output = [ EF BF BD 58 59 5A ]

            Encoder encoder = Encoding.UTF8.GetEncoder();
            byte[] destBuffer = new byte[100];
            
            int charsConsumed, bytesWritten;
            bool completed;

            encoder.Convert(new char[] { '\uD800' }, destBuffer, flush: false, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(0, bytesWritten); // waiting for second half of surrogate pair
            Assert.True(completed);

            encoder.Convert(new char[] { 'X' }, destBuffer, flush: false, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(4, bytesWritten); // U+FFFD bytes + ASCII 'X'
            Assert.True(completed); // no internal state
            Assert.Equal(new byte[] { 0xEF, 0xBF, 0xBD, (byte)'X' }, destBuffer[0..4]);

            encoder.Convert(new char[] { 'Y' }, destBuffer, flush: false, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(1, bytesWritten); // ASCII 'Y'
            Assert.True(completed); // no internal state
            Assert.Equal((byte)'Y', destBuffer[0]);

            encoder.Convert(new char[] { 'Z' }, destBuffer, flush: true, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(1, bytesWritten); // ASCII 'Z'
            Assert.True(completed); // no internal state
            Assert.Equal((byte)'Z', destBuffer[0]);
        }

        [Fact]
        public void EncoderConvertSplitAcrossUnencodableSurrogatePairTests()
        {
            // Input = [ D83C DF32 0058 0059 005A ]
            // Expected output = "[1F332]XYZ" (as ASCII bytes)

            Encoder encoder = Encoding.GetEncoding(
                "ascii",
                new CustomEncoderReplacementFallback(),
                DecoderFallback.ExceptionFallback).GetEncoder();
            byte[] destBuffer = new byte[100];

            int charsConsumed, bytesWritten;
            bool completed;

            encoder.Convert(new char[] { '\uD83C' }, destBuffer, flush: false, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(0, bytesWritten); // waiting for second half of surrogate pair
            Assert.True(completed);

            encoder.Convert(new char[] { '\uDF32', 'X' }, destBuffer, flush: false, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(2, charsConsumed);
            Assert.Equal(8, bytesWritten); // ASCII "[1F332]X"
            Assert.True(completed); // no internal state
            Assert.Equal(Encoding.ASCII.GetBytes("[1F332]X"), destBuffer[0..8]);

            encoder.Convert(new char[] { 'Y' }, destBuffer, flush: false, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(1, bytesWritten); // ASCII 'Y'
            Assert.True(completed); // no internal state
            Assert.Equal((byte)'Y', destBuffer[0]);

            encoder.Convert(new char[] { 'Z' }, destBuffer, flush: true, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(1, charsConsumed);
            Assert.Equal(1, bytesWritten); // ASCII 'Z'
            Assert.True(completed); // no internal state
            Assert.Equal((byte)'Z', destBuffer[0]);
        }

        [Fact]
        public void EncoderGetBytesSplitAcrossInvalidSurrogateTests()
        {
            // Input = [ D800 0058 0059 005A ]
            // Expected output = [ EF BF BD 58 59 5A ]

            Encoder encoder = Encoding.UTF8.GetEncoder();
            byte[] destBuffer = new byte[100];

            int byteCount = encoder.GetByteCount(new char[] { '\uD800' }, flush: false);
            Assert.Equal(0, byteCount); // waiting for second half of surrogate pair

            byteCount = encoder.GetBytes(new char[] { '\uD800' }, destBuffer, flush: false);
            Assert.Equal(0, byteCount);

            byteCount = encoder.GetByteCount(new char[] { 'X' }, flush: false);
            Assert.Equal(4, byteCount); // U+FFFD bytes + ASCII 'X'

            byteCount = encoder.GetBytes(new char[] { 'X' }, destBuffer, flush: false);
            Assert.Equal(4, byteCount);
            Assert.Equal(new byte[] { 0xEF, 0xBF, 0xBD, (byte)'X' }, destBuffer[0..4]);

            byteCount = encoder.GetBytes(new char[] { 'Y' }, destBuffer, flush: false);
            Assert.Equal(1, byteCount); // ASCII 'Y'

            byteCount = encoder.GetBytes(new char[] { 'Y' }, destBuffer, flush: false);
            Assert.Equal(1, byteCount);
            Assert.Equal((byte)'Y', destBuffer[0]);

            byteCount = encoder.GetBytes(new char[] { 'Z' }, destBuffer, flush: true);
            Assert.Equal(1, byteCount); // ASCII 'Z'

            byteCount = encoder.GetBytes(new char[] { 'Z' }, destBuffer, flush: true);
            Assert.Equal(1, byteCount);
            Assert.Equal((byte)'Z', destBuffer[0]);
        }

        [Fact]
        public void EncoderGetBytesSplitAcrossUnencodableSurrogatePairTests()
        {
            // Input = [ D83C DF32 0058 0059 005A ]
            // Expected output = "[1F332]XYZ" (as ASCII bytes)

            Encoder encoder = Encoding.GetEncoding(
                "ascii",
                new CustomEncoderReplacementFallback(),
                DecoderFallback.ExceptionFallback).GetEncoder();
            byte[] destBuffer = new byte[100];

            int byteCount = encoder.GetByteCount(new char[] { '\uD83C' }, flush: false);
            Assert.Equal(0, byteCount); // waiting for second half of surrogate pair

            byteCount = encoder.GetBytes(new char[] { '\uD83C' }, destBuffer, flush: false);
            Assert.Equal(0, byteCount);

            byteCount = encoder.GetByteCount(new char[] { '\uDF32', 'X' }, flush: false);
            Assert.Equal(8, byteCount); // ASCII "[1F332]X"

            byteCount = encoder.GetBytes(new char[] { '\uDF32', 'X' }, destBuffer, flush: false);
            Assert.Equal(8, byteCount);
            Assert.Equal(Encoding.ASCII.GetBytes("[1F332]X"), destBuffer[0..8]);

            byteCount = encoder.GetBytes(new char[] { 'Y' }, destBuffer, flush: false);
            Assert.Equal(1, byteCount); // ASCII 'Y'

            byteCount = encoder.GetBytes(new char[] { 'Y' }, destBuffer, flush: false);
            Assert.Equal(1, byteCount);
            Assert.Equal((byte)'Y', destBuffer[0]);

            byteCount = encoder.GetBytes(new char[] { 'Z' }, destBuffer, flush: true);
            Assert.Equal(1, byteCount); // ASCII 'Z'

            byteCount = encoder.GetBytes(new char[] { 'Z' }, destBuffer, flush: true);
            Assert.Equal(1, byteCount);
            Assert.Equal((byte)'Z', destBuffer[0]);
        }
    }
}
