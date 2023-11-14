// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Text.Encodings.Tests
{
    public class DecoderMiscTests
    {
        [Fact]
        public static unsafe void ConvertTest()
        {
            string s1 = "This is simple ascii string";
            string s2 = "\uD800\uDC00\u0200";           // non ascii letters
            string s3 = s1 + s2;

            Encoding utf8 = Encoding.UTF8;
            Decoder decoder = utf8.GetDecoder();
            char [] chars = new char[200];

            byte [] bytes1 = utf8.GetBytes(s1);
            byte [] bytes2 = utf8.GetBytes(s2);
            byte [] bytes3 = utf8.GetBytes(s3);

            int bytesUsed;
            int charsUsed;
            bool completed;

            fixed (byte *pBytes1 = bytes1)
            fixed (byte *pBytes2 = bytes2)
            fixed (byte *pBytes3 = bytes3)
            fixed (char *pChars = chars)
            {
                decoder.Convert(pBytes1, bytes1.Length, pChars, chars.Length, true, out bytesUsed, out charsUsed, out completed);
                string s = new string(chars, 0, charsUsed);
                Assert.Equal(bytes1.Length, bytesUsed);
                Assert.Equal(s1.Length, charsUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes1");
                Assert.Equal(s1, s);

                decoder.Convert(pBytes2, bytes2.Length, pChars, chars.Length, true, out bytesUsed, out charsUsed, out completed);
                s = new string(chars, 0, charsUsed);
                Assert.Equal(bytes2.Length, bytesUsed);
                Assert.Equal(s2.Length, charsUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes2");
                Assert.Equal(s2, s);

                decoder.Convert(pBytes3, bytes3.Length, pChars, chars.Length, true, out bytesUsed, out charsUsed, out completed);
                s = new string(chars, 0, charsUsed);
                Assert.Equal(bytes3.Length, bytesUsed);
                Assert.Equal(s3.Length, charsUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes3");
                Assert.Equal(s3, s);

                decoder.Convert(pBytes3 + bytes1.Length, bytes3.Length - bytes1.Length, pChars, chars.Length, true, out bytesUsed, out charsUsed, out completed);
                s = new string(chars, 0, charsUsed);
                Assert.Equal(bytes2.Length, bytesUsed);
                Assert.Equal(s2.Length, charsUsed);
                Assert.True(completed, "Expected to have the full operation compeleted with bytes3+bytes1.Length");
                Assert.Equal(s2, s);

                decoder.Convert(pBytes3 + bytes1.Length, bytes3.Length - bytes1.Length, pChars, 2, true, out bytesUsed, out charsUsed, out completed);
                s = new string(chars, 0, charsUsed);
                Assert.True(bytes2.Length > bytesUsed, "Expected to use less bytes when there is not enough char buffer");
                Assert.Equal(2, charsUsed);
                Assert.False(completed, "Expected to have the operation not fully completed");
                Assert.Equal(s2.Substring(0, 2), s);

                int encodedBytes = bytesUsed;
                decoder.Convert(pBytes3 + bytes1.Length + bytesUsed, bytes3.Length - bytes1.Length - encodedBytes, pChars, 1, true, out bytesUsed, out charsUsed, out completed);
                Assert.Equal(bytes3.Length - bytes1.Length - encodedBytes, bytesUsed);
                Assert.Equal(1, charsUsed);
                Assert.True(completed, "expected to flush the remaining character");
                Assert.Equal(s2[s2.Length - 1], pChars[0]);
            }
        }

        [Fact]
        public static unsafe void ConvertNegativeTest()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            int bytesUsed;
            int charsUsed;
            bool completed;

            byte [] bytes = Encoding.UTF8.GetBytes("\u0D800\uDC00");

            fixed (byte *bytesPtr = bytes)
            fixed (char *charsPtr = new char[1])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;

                AssertExtensions.Throws<ArgumentNullException>("chars", () => decoder.Convert(pBytes, bytes.Length, null, 1, true, out bytesUsed, out charsUsed, out completed));
                AssertExtensions.Throws<ArgumentNullException>("bytes", () => decoder.Convert(null, bytes.Length, pChars, 1, true, out bytesUsed, out charsUsed, out completed));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("byteCount", () => decoder.Convert(pBytes, -1, pChars, 1, true, out bytesUsed, out charsUsed, out completed));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => decoder.Convert(pBytes, bytes.Length, pChars, -1, true, out bytesUsed, out charsUsed, out completed));

                AssertExtensions.Throws<ArgumentException>("chars", () => decoder.Convert(pBytes, bytes.Length, pChars, 0, true, out bytesUsed, out charsUsed, out completed));
            }

            decoder = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback()).GetDecoder();

            fixed (byte *bytesPtr = new byte [] { 0xFF, 0xFF })
            fixed (char *charsPtr = new char[2])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;

                Assert.Throws<DecoderFallbackException>(() => decoder.Convert(pBytes, 2, pChars, 2, true, out bytesUsed, out charsUsed, out completed));
            }
        }

        [Fact]
        public static unsafe void GetCharsTest()
        {
            string s1 = "This is simple ascii string";
            string s2 = "\uD800\uDC00\u0200";           // non ascii letters
            string s3 = s1 + s2;

            Encoding utf8 = Encoding.UTF8;
            Decoder decoder = utf8.GetDecoder();
            char [] chars = new char[200];

            byte [] bytes1 = utf8.GetBytes(s1);
            byte [] bytes2 = utf8.GetBytes(s2);
            byte [] bytes3 = utf8.GetBytes(s3);

            fixed (byte *pBytes1 = bytes1)
            fixed (byte *pBytes2 = bytes2)
            fixed (byte *pBytes3 = bytes3)
            fixed (char *pChars = chars)
            {
                int charsUsed = decoder.GetChars(pBytes1, bytes1.Length, pChars, chars.Length, true);
                string s = new string(chars, 0, charsUsed);
                Assert.Equal(s1.Length, charsUsed);
                Assert.Equal(s1.Length, decoder.GetCharCount(pBytes1, bytes1.Length, true));
                Assert.Equal(s1, s);

                charsUsed = decoder.GetChars(pBytes2, bytes2.Length, pChars, chars.Length, true);
                s = new string(chars, 0, charsUsed);
                Assert.Equal(s2.Length, charsUsed);
                Assert.Equal(s2.Length, decoder.GetCharCount(pBytes2, bytes2.Length, true));
                Assert.Equal(s2, s);

                charsUsed = decoder.GetChars(pBytes3, bytes3.Length, pChars, chars.Length, true);
                s = new string(chars, 0, charsUsed);
                Assert.Equal(s3.Length, charsUsed);
                Assert.Equal(s3.Length, decoder.GetCharCount(pBytes3, bytes3.Length, true));
                Assert.Equal(s3, s);

                charsUsed = decoder.GetChars(pBytes3 + bytes1.Length, bytes3.Length - bytes1.Length, pChars, chars.Length, true);
                s = new string(chars, 0, charsUsed);
                Assert.Equal(s2.Length, charsUsed);
                Assert.Equal(s2.Length, decoder.GetCharCount(pBytes3 + bytes1.Length, bytes3.Length - bytes1.Length, true));
                Assert.Equal(s2, s);
            }
        }

        [Fact]
        public static unsafe void GetCharsNegativeTest()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte [] bytes = Encoding.UTF8.GetBytes("\u0D800\uDC00");

            fixed (byte *bytesPtr = bytes)
            fixed (char *charsPtr = new char[1])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;

                AssertExtensions.Throws<ArgumentNullException>("chars", () => decoder.GetChars(pBytes, bytes.Length, null, 1, true));
                AssertExtensions.Throws<ArgumentNullException>("bytes", () => decoder.GetChars(null, bytes.Length, pChars, 1, true));
                AssertExtensions.Throws<ArgumentNullException>("bytes", () => decoder.GetCharCount(null, bytes.Length, true));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("byteCount", () => decoder.GetChars(pBytes, -1, pChars, 1, true));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("charCount", () => decoder.GetChars(pBytes, bytes.Length, pChars, -1, true));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => decoder.GetCharCount(pBytes, -1, true));

                AssertExtensions.Throws<ArgumentException>("chars", () => decoder.GetChars(pBytes, bytes.Length, pChars, 1, true));
            }

            decoder = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback()).GetDecoder();

            fixed (byte *bytesPtr = new byte [] { 0xFF, 0xFF })
            fixed (char *charsPtr = new char[2])
            {
                byte *pBytes = bytesPtr;
                char *pChars = charsPtr;

                Assert.Throws<DecoderFallbackException>(() => decoder.GetChars(pBytes, 2, pChars, 2, true));
                Assert.Throws<DecoderFallbackException>(() => decoder.GetCharCount(pBytes, 2, true));
            }
        }

        [Fact]
        public static void DecoderExceptionFallbackBufferTest()
        {
            Decoder decoder = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback()).GetDecoder();

            byte [] bytes = new byte [] { 0xFF, 0xFF };
            char [] chars = new char [bytes.Length];

            Assert.Throws<DecoderFallbackException>(() => decoder.GetChars(bytes, 0, 2, chars, 2));

            DecoderFallbackBuffer fallbackBuffer = decoder.FallbackBuffer;
            Assert.True(fallbackBuffer is DecoderExceptionFallbackBuffer, "Expected to get DecoderExceptionFallbackBuffer type");
            Assert.Throws<DecoderFallbackException>(() => fallbackBuffer.Fallback(bytes, 0));
            Assert.Throws<DecoderFallbackException>(() => fallbackBuffer.Fallback(new byte [] { 0x40, 0x60 }, 0));

            Assert.Equal(0, fallbackBuffer.Remaining);
            Assert.Equal('\u0000', fallbackBuffer.GetNextChar());

            Assert.False(fallbackBuffer.MovePrevious(), "MovePrevious expected to always return false");

            fallbackBuffer.Reset();

            Assert.Equal(0, fallbackBuffer.Remaining);
            Assert.Equal('\u0000', fallbackBuffer.GetNextChar());
        }

        [Theory]
        [InlineData(new byte[] { 0xF1 }, new byte[] { 0xF1 }, -1)]
        [InlineData(new byte[] { 0xF1 }, new byte[] { 0x80 }, -1)]
        [InlineData(new byte[] { 0xF1, 0x80 }, new byte[] { }, -2)]
        [InlineData(new byte[] { 0xF1, 0x80 }, new byte[] { 0x60 }, -2)]
        [InlineData(new byte[] { 0xF1, 0x80 }, new byte[] { 0x80 }, -2)]
        [InlineData(new byte[] { 0xF1, 0x80, 0x80 }, new byte[] { 0x60 }, -3)]
        [InlineData(new byte[] { 0xF1, 0x80, 0x80 }, new byte[] { 0x80, 0x81 }, 1)]
        public static void DecoderFallbackExceptionIndexTests(byte[] firstPayload, byte[] secondPayload, int expectedIndex)
        {
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            // First test GetChars / GetChars

            Decoder decoder = encoding.GetDecoder();
            Assert.Equal(0, decoder.GetChars(firstPayload, 0, firstPayload.Length, new char[0], 0, flush: false));

            DecoderFallbackException ex = Assert.Throws<DecoderFallbackException>(
                () => decoder.GetChars(secondPayload, 0, secondPayload.Length, new char[8], 0, flush: true));
            Assert.Equal(expectedIndex, ex.Index);

            // Then test GetChars / GetCharCount

            decoder = encoding.GetDecoder();
            Assert.Equal(0, decoder.GetChars(firstPayload, 0, firstPayload.Length, new char[0], 0, flush: false));

            ex = Assert.Throws<DecoderFallbackException>(
                () => decoder.GetCharCount(secondPayload, 0, secondPayload.Length, flush: true));
            Assert.Equal(expectedIndex, ex.Index);
        }

        [Fact]
        public static void DecoderReplacementFallbackBufferTest()
        {
            Decoder decoder = Encoding.GetEncoding("us-ascii", new EncoderReplacementFallback(), new DecoderReplacementFallback()).GetDecoder();

            byte [] bytes = new byte [] { 0xFF, 0xFF };
            char [] chars = new char [bytes.Length];

            DecoderFallbackBuffer fallbackBuffer = decoder.FallbackBuffer;
            Assert.True(fallbackBuffer is DecoderReplacementFallbackBuffer, "Expected to get DecoderReplacementFallbackBuffer type");
            Assert.True(fallbackBuffer.Fallback(bytes, 0), "Expected we fallback on the given buffer");
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
        public void DecoderConvertSplitAcrossInvalidMultiByteSequenceTests()
        {
            // Input = [ C2 58 59 5A ]
            // Expected output = [ FFFD 0058 0059 005A ]

            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] destBuffer = new char[100];

            int bytesConsumed,  charsWritten;
            bool completed;

            decoder.Convert(new byte[] { 0xC2 }, destBuffer, flush: false, out bytesConsumed, out charsWritten, out completed);
            Assert.Equal(1, bytesConsumed);
            Assert.Equal(0, charsWritten); // waiting for more bytes in multi-byte sequence
            Assert.True(completed);

            decoder.Convert(new byte[] { (byte)'X' }, destBuffer, flush: false, out bytesConsumed, out charsWritten, out completed);
            Assert.Equal(1, bytesConsumed);
            Assert.Equal(2, charsWritten); // U+FFFD + 'X'
            Assert.True(completed); // no internal state
            Assert.Equal(new char[] { '\uFFFD', 'X' }, destBuffer[0..2]);

            decoder.Convert(new byte[] { (byte)'Y' }, destBuffer, flush: false, out bytesConsumed, out charsWritten, out completed);
            Assert.Equal(1, bytesConsumed);
            Assert.Equal(1, charsWritten);
            Assert.True(completed); // no internal state
            Assert.Equal('Y', destBuffer[0]);

            decoder.Convert(new byte[] { (byte)'Z' }, destBuffer, flush: true, out bytesConsumed, out charsWritten, out completed);
            Assert.Equal(1, bytesConsumed);
            Assert.Equal(1, charsWritten);
            Assert.True(completed); // no internal state
            Assert.Equal('Z', destBuffer[0]);
        }

        [Fact]
        public void DecoderGetBytesSplitAcrossInvalidMultiByteSequenceTests()
        {
            // Input = [ C2 58 59 5A ]
            // Expected output = [ FFFD 0058 0059 005A ]

            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] destBuffer = new char[100];

            int charCount = decoder.GetCharCount(new byte[] { 0xC2 }, flush: false);
            Assert.Equal(0, charCount); // waiting for more bytes in multi-byte sequence

            charCount = decoder.GetChars(new byte[] { 0xC2 }, destBuffer, flush: false);
            Assert.Equal(0, charCount);

            charCount = decoder.GetCharCount(new byte[] { (byte)'X' }, flush: false);
            Assert.Equal(2, charCount); // U+FFFD + 'X'

            charCount = decoder.GetChars(new byte[] { (byte)'X' }, destBuffer, flush: false);
            Assert.Equal(2, charCount);
            Assert.Equal(new char[] { '\uFFFD', 'X' }, destBuffer[0..2]);

            charCount = decoder.GetCharCount(new byte[] { (byte)'Y' }, flush: false);
            Assert.Equal(1, charCount); // 'Y'

            charCount = decoder.GetChars(new byte[] { (byte)'Y' }, destBuffer, flush: false);
            Assert.Equal(1, charCount);
            Assert.Equal('Y', destBuffer[0]);

            charCount = decoder.GetCharCount(new byte[] { (byte)'Z' }, flush: true);
            Assert.Equal(1, charCount); // 'Z'

            charCount = decoder.GetChars(new byte[] { (byte)'Z' }, destBuffer, flush: true);
            Assert.Equal(1, charCount);
            Assert.Equal('Z', destBuffer[0]);
        }
    }
}
