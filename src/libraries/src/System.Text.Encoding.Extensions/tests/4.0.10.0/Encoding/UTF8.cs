// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class UTF8
    {
        private static readonly EncodingTestHelper s_encodingUtil_UTF8 = new EncodingTestHelper("UTF-8");

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues()
        {
            s_encodingUtil_UTF8.GetByteCountTest(String.Empty, 0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest(String.Empty, 0, 1, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetByteCountTest((String)null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", 1, -1, 0));
            s_encodingUtil_UTF8.GetByteCountTest("abc", 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", 3, 1, 0));
            s_encodingUtil_UTF8.GetByteCountTest("abc", 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest("abc", 2, 2, 0));
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetCharCountTest((Byte[])null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 1, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 2, 0));
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetByteCountTest((String)null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetByteCountTest(String.Empty, -1, 0));
            s_encodingUtil_UTF8.GetByteCountTest(String.Empty, 0, 0);
            s_encodingUtil_UTF8.GetByteCountTest("a", 0, 0);
        }

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetCharCountTest((Byte[])null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { }, -1, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { }, 0, 0);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x61 }, 0, 0);
        }

        [Fact]
        public static void GetBytes_InvalidConverstionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetBytesTest((String)null, 0, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", -1, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", -1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 4, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 1, 3, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void GetChars_InvalidConverstionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetCharsTest((Byte[])null, 0, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 4, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, 3, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetChars_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 3, -2, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 3, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 3, -1, 1, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 3, 1, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 0, 1, 1, "\u0000", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 0, 1, 2, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 5, 1, "\u0000\u0061\u0062\u0063\u0000", 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 0, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 1, -1, -1, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, 0, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61 }, 0, 0, 1, 0, "\u0000", 0);
        }

        [Fact]
        public static void GetBytes_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 3, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 3, 0, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 3, -1, 1, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 3, 1, 0, (Byte[])null, 0));
            s_encodingUtil_UTF8.GetBytesTest("abc", 0, 0, 1, 1, new Byte[] { 0x00 }, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 0, 1, 2, (Byte[])null, 0));
            s_encodingUtil_UTF8.GetBytesTest("abc", 0, 3, 5, 1, new Byte[] { 0x00, 0x61, 0x62, 0x63, 0x00 }, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 0, -1, -1, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest("abc", 0, 1, -1, -1, (Byte[])null, 0));
            s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, 0, -1, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF8.GetBytesTest("a", 0, 0, 1, 0, new Byte[] { 0x00 }, 0);
        }

        [Fact]
        public static void GetChars_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetCharsTest((Byte[])null, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, -2, 0, (String)null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, 0, -1, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { }, 0, -1, -100, String.Empty, 0);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61 }, 0, 1, 1, "\u0000", 0);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61 }, 1, -1, -100, "a", 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61 }, 1, 2, 2, "\u0061\u0000", 1);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61 }, 1, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetBytes_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetBytesTest((String)null, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest(String.Empty, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, 0, -1, new Byte[] { }, 0));
            s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF8.GetBytesTest(String.Empty, 0, -1, -100, new Byte[] { }, 0);
            s_encodingUtil_UTF8.GetBytesTest("a", 0, 1, 1, new Byte[] { 0x00 }, 0);
            s_encodingUtil_UTF8.GetBytesTest("a", 1, -1, -100, new Byte[] { 0x61 }, 1);
            s_encodingUtil_UTF8.GetBytesTest("a", 1, 2, 2, new Byte[] { 0x61, 0x00 }, 1);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF8.GetBytesTest("a", 1, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void GetChars_ValidInput()
        {
            // 1 byte
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x41 }, 0, 1, -1, 0, "A", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x41 }, 0, 1, -1, 0, "A", 1);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x41 }, 0, 1, 1);
            s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0x41 }, 0, 1, 1);

            // Control code
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x1F, 0x10, 0x00, 0x09 }, 0, 4, -1, 0, "\u001F\u0010\u0000\u0009", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x1F, 0x10, 0x00, 0x09 }, 0, 4, -1, 0, "\u001F\u0010\u0000\u0009", 4);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x1F, 0x00, 0x10, 0x09 }, 0, 4, -1, 0, "\u001F\u0000\u0010\u0009", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x1F, 0x00, 0x10, 0x09 }, 0, 4, -1, 0, "\u001F\u0000\u0010\u0009", 4);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x00, 0x1F, 0x10, 0x09 }, 0, 4, -1, 0, "\u0000\u001F\u0010\u0009", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x00, 0x1F, 0x10, 0x09 }, 0, 4, -1, 0, "\u0000\u001F\u0010\u0009", 4);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0x1F, 0x10, 0x00, 0x09 }, 0, 4, 4);
            s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0x1F, 0x10, 0x00, 0x09 }, 0, 4, 4);

            // 2 bytes
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xC3, 0xA1 }, 0, 2, -1, 0, "\u00E1", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xC3, 0xA1 }, 0, 2, -1, 0, "\u00E1", 1);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xC3, 0xA1 }, 0, 2, 1);
            s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xC3, 0xA1 }, 0, 2, 1);

            // 3 bytes
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xE8, 0x80, 0x80 }, 0, 3, -1, 0, "\u8000", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xE8, 0x80, 0x80 }, 0, 3, -1, 0, "\u8000", 1);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xE8, 0x80, 0x80 }, 0, 3, 1);
            s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xE8, 0x80, 0x80 }, 0, 3, 1);

            // 4 bytes
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0, 4, -1, 0, "\uD800\uDC00", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0, 4, -1, 0, "\uD800\uDC00", 2);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0, 4, 2);
            s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0, 4, 2);
        }

        [Fact]
        public static void InvalidByteSequence()
        {
            // Simple invalid byte
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xAA }, 0, 1, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xAA }, 0, 1, 1, 0, String.Empty, 0));

            // Simple unfinished sequence
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xC0 }, 0, 1, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xC0 }, 0, 1, 1, 0, String.Empty, 0));

            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 3, -1, 0, "\uFFEE", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 3, -1, 0, "\uFFEE", 1);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 3, 1);
            s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 3, 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xFF, 0xEE }, 0, 3, -1, 0, "\uFFFD\uFFFD\uFFFD", 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xFF, 0xEE }, 0, 3, 3, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xFF, 0xAE }, 0, 3, -1, 0, "\uFFFD\uFFFD\uFFFD", 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xFF, 0xAE }, 0, 3, 3, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xFF, 0xAE }, 0, 3, 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xFF, 0xAE }, 0, 3, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xC0, 0xBF }, 0, 2, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xC0, 0xBF }, 0, 2, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xC0, 0xBF }, 0, 2, 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xC0, 0xBF }, 0, 2, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xE0, 0x9C, 0x90 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xE0, 0x9C, 0x90 }, 0, 3, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xE0, 0x9C, 0x90 }, 0, 3, 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xE0, 0x9C, 0x90 }, 0, 3, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF0, 0x8F, 0xA4, 0x80 }, 0, 4, -1, 0, "\uFFFD\uFFFD\uFFFD", 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF0, 0x8F, 0xA4, 0x80 }, 0, 4, 3, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xF0, 0x8F, 0xA4, 0x80 }, 0, 4, 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xF0, 0x8F, 0xA4, 0x80 }, 0, 4, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, 2, 0, "\uFFFD\u0000", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, 0));
            s_encodingUtil_UTF8.GetCharCountTest(false, false, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 2, 2, 0, "\uFFFD\u0000", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 2, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 2, 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 2, 0));
            s_encodingUtil_UTF8.GetCharCountTest(false, false, new Byte[] { 0xEF, 0xBF, 0xAE }, 0, 2, 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0x61 }, 0, 3, -1, 0, "\uFFFD\u0061", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0x61 }, 0, 3, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xBF, 0x61 }, 0, 3, 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xBF, 0x61 }, 0, 3, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xEF, 0xBF, 0xAE }, 0, 5, -1, 0, "\uFFFD\uFFEE", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xEF, 0xBF, 0xAE }, 0, 5, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xBF, 0xEF, 0xBF, 0xAE }, 0, 5, 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xBF, 0xEF, 0xBF, 0xAE }, 0, 5, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF0, 0xC4, 0x80 }, 0, 3, -1, 0, "\uFFFD\u0100", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF0, 0xC4, 0x80 }, 0, 3, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xF0, 0xC4, 0x80 }, 0, 3, 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xF0, 0xC4, 0x80 }, 0, 3, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xC0, 0xBF }, 0, 4, -1, 0, "\uFFFD\uFFFD\uFFFD", 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xC0, 0xBF }, 0, 4, 3, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xEF, 0xBF, 0xC0, 0xBF }, 0, 4, 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xEF, 0xBF, 0xC0, 0xBF }, 0, 4, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0x41 }, 0, 2, -1, 0, "\uFFFD\u0041", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0x41 }, 0, 2, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0x41 }, 0, 3, -1, 0, "\uFFFD\u0041", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0x41 }, 0, 3, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xAA, 0x41 }, 0, 2, -1, 0, "\uFFFD\u0041", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xAA, 0x41 }, 0, 2, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetByteCountTest("\u00C5", 0, 1, 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xC3, 0x85 }, 0, 2, -1, 0, "\u00C5", 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xE2, 0x84, 0xAB }, 0, 3, -1, 0, "\u212B", 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x61, 0xCC, 0x8A }, 0, 3, -1, 0, "\u0061\u030A", 2);
        }

        /* High: D800 - DBFF; Low: DC00-DFFF
         Invalid/unpaired surrogates are not considered representing unique
         Unicode scalar values, therefore they should be always filtered out
         (they are removed from the roundtripping requirement by design)
        
         D800 + DC00: 110110-0000-000000 110111-0000000000: U+000-00001-00000000-00000000: 11110000 10010000 10000000 10000000
         D800 + DFFF: 110110-0000-000000 110111-1111111111: U+000-00001-00000011-11111111: 11110000 10010000 10001111 10111111
         DBFF + DC00: 110110-1111-111111 110111-0000000000: U+000-10000-11111100-00000000: 11110100 10001111 10110000 10000000
         DBFF + DFFF: 110110-1111-111111 110111-1111111111: U+000-10000-11111111-11111111: 11110100 10001111 10111111 10111111
         D800: 11101101 10100000 10000000
         DBFF: 11101101 10101111 10111111
         DC00: 11101101 10110000 10000000
         DFFF: 11101101 10111111 10111111 */
        [Fact]
        public static void Surrogates1()
        {
            // Border cases
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0, 4, -1, 0, "\uD800\uDC00", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF0, 0x90, 0x8F, 0xBF }, 0, 4, -1, 0, "\uD800\uDFFF", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF4, 0x8F, 0xB0, 0x80 }, 0, 4, -1, 0, "\uDBFF\uDC00", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, 0, 4, -1, 0, "\uDBFF\uDFFF", 2);

            // Something in between
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xF3, 0xB0, 0x80, 0x80 }, 0, 4, 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF3, 0xB0, 0x80, 0x80 }, 0, 4, -1, 0, "\uDB80\uDC00", 2);
            s_encodingUtil_UTF8.GetBytesTest("\uDB80\uDC00", 0, 2, -1, 0, new Byte[] { 0xF3, 0xB0, 0x80, 0x80 }, 4);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 0, 4, -1, 0, "\uD800\uDC00", 2);

            // Unfinished surrogates
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xED, 0xA0, 0x80 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xED, 0xAF, 0xBF }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xED, 0xB0, 0x80 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xED, 0xBF, 0xBF }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xA0, 0x80 }, 0, 3, 3, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xAF, 0xBF }, 0, 3, 3, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xB0, 0x80 }, 0, 3, 3, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xBF, 0xBF }, 0, 3, 3, 0, String.Empty, 0));

            // Something in-between
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xED, 0xB0, 0x80 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xED, 0xA0, 0x80 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xB0, 0x80 }, 0, 3, 3, 0, String.Empty, 1));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xA0, 0x80 }, 0, 3, 3, 0, String.Empty, 1));
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0xB0, 0x80 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0xA0, 0x80 }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);

            // Two high, two low, etc
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xA0, 0x80, 0xED, 0xAF, 0xBF }, 0, 6, 6, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xB0, 0x80, 0xED, 0xB0, 0x80 }, 0, 6, 6, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xA0, 0x80, 0xED, 0xA0, 0x80 }, 0, 6, 6, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0xA0, 0x80, 0xED, 0xAF, 0xBF }, 0, 6, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0xB0, 0x80, 0xED, 0xB0, 0x80 }, 0, 6, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0xA0, 0x80, 0xED, 0xA0, 0x80 }, 0, 6, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD", 4);

            // Too high scalar value in surrogates
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0xA0, 0x80, 0xEE, 0x80, 0x80 }, 0, 6, 6, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0xA0, 0x80, 0xEE, 0x80, 0x80 }, 0, 6, -1, 0, "\uFFFD\uFFFD\uE000", 3);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, -1, 0, "\uFFFD\uFFFD\uFFFD", 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, 4, 0, String.Empty, 0));
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, -1, 0, "\uFFFD\uFFFD\uFFFD", 3);
            s_encodingUtil_UTF8.GetCharCountTest(new Byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, 3);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF8.GetCharCountTest(false, true, new Byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, 0));
            s_encodingUtil_UTF8.GetCharCountTest(false, false, new Byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, 3);
        }

        // They don't represent abstract characters, but still need to be transmitted
        [Fact]
        public static void Specials()
        {
            // U+FFFF
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xBF }, 0, 3, -1, 0, "\uFFFF", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xBF }, 0, 3, 2, 0, "\uFFFF\u0000", 1);

            // U_FFFE
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBF, 0xBE }, 0, 3, -1, 0, "\uFFFE", 1);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBF, 0xBE }, 0, 3, 2, 0, "\uFFFE\u0000", 1);

            // U+FDD0 - U+FDEF
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xB7, 0x90, 0xEF, 0xB7, 0xAF }, 0, 6, -1, 0, "\uFDD0\uFDEF", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xB7, 0x90, 0xEF, 0xB7, 0xAF }, 0, 6, 2, 0, "\uFDD0\uFDEF", 2);

            // BOM (U+FEFF) is never generated or absorbed, regardless of constructor parameters
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 0, 4, -1, 0, "\uFEFF\u0041", 2);

            // Existing BOM untouched, no extra BOM generated
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 0, 4, -1, 0, "\uFEFF\u0041", 2);

            // No extra BOM generated
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x41, 0x00, 0x00, 0x00 }, 0, 1, 4, 0, "\u0041\u0000\u0000\u0000", 1);
            s_encodingUtil_UTF8.GetCharsTest(true, true, new Byte[] { 0x41, 0x00, 0x00, 0x00 }, 0, 1, 4, 0, "\u0041\u0000\u0000\u0000", 1);

            // 1 byte
            s_encodingUtil_UTF8.GetBytesTest("A", 0, 1, -1, 0, new Byte[] { 0x41 }, 1);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "A", 0, 1, -1, 0, new Byte[] { 0x41 }, 1);
            s_encodingUtil_UTF8.GetByteCountTest("A", 0, 1, 1);
            s_encodingUtil_UTF8.GetByteCountTest(false, true, "A", 0, 1, 1);

            // Control codes
            s_encodingUtil_UTF8.GetBytesTest("\u001F\u0010\u0000\u0009", 0, 4, -1, 0, new Byte[] { 0x1F, 0x10, 0x00, 0x09 }, 4);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\u001F\u0010\u0000\u0009", 0, 4, -1, 0, new Byte[] { 0x1F, 0x10, 0x00, 0x09 }, 4);
            s_encodingUtil_UTF8.GetByteCountTest("\u001F\u0010\u0000\u0009", 0, 4, 4);
            s_encodingUtil_UTF8.GetByteCountTest(false, true, "\u001F\u0010\u0000\u0009", 0, 4, 4);

            // Slow loop with fast ASCII processing
            s_encodingUtil_UTF8.GetBytesTest("eeeee", 0, 5, -1, 0, new Byte[] { 0x65, 0x65, 0x65, 0x65, 0x65 }, 5);
            s_encodingUtil_UTF8.GetBytesTest("e\u00E1eee", 0, 5, -1, 0, new Byte[] { 0x65, 0xC3, 0xA1, 0x65, 0x65, 0x65 }, 6);
            s_encodingUtil_UTF8.GetBytesTest("\u0065\u8000\u0065\u0065\u0065", 0, 5, -1, 0, new Byte[] { 0x65, 0xE8, 0x80, 0x80, 0x65, 0x65, 0x65 }, 7);
            s_encodingUtil_UTF8.GetBytesTest("\u0065\uD800\uDC00\u0065\u0065\u0065", 0, 6, -1, 0, new Byte[] { 0x65, 0xF0, 0x90, 0x80, 0x80, 0x65, 0x65, 0x65 }, 8);
            s_encodingUtil_UTF8.GetBytesTest("eeeeeeeeeeeeeee", 0, 15, -1, 0, new Byte[] { 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65 }, 15);
            s_encodingUtil_UTF8.GetBytesTest("eeeeee\u00E1eeeeeeee", 0, 15, -1, 0, new Byte[] { 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0xC3, 0xA1, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65 }, 16);
            s_encodingUtil_UTF8.GetBytesTest("\u0065\u0065\u0065\u0065\u0065\u0065\u8000\u0065\u0065\u0065\u0065\u0065\u0065\u0065\u0065", 0, 15, -1, 0, new Byte[] { 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0xE8, 0x80, 0x80, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65 }, 17);
            s_encodingUtil_UTF8.GetBytesTest("\u0065\u0065\u0065\u0065\u0065\u0065\uD800\uDC00\u0065\u0065\u0065\u0065\u0065\u0065\u0065\u0065", 0, 16, -1, 0, new Byte[] { 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0xF0, 0x90, 0x80, 0x80, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65 }, 18);

            // 2 bytes
            s_encodingUtil_UTF8.GetBytesTest("\u00E1", 0, 1, -1, 0, new Byte[] { 0xC3, 0xA1 }, 2);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\u00E1", 0, 1, -1, 0, new Byte[] { 0xC3, 0xA1 }, 2);
            s_encodingUtil_UTF8.GetByteCountTest("\u00E1", 0, 1, 2);
            s_encodingUtil_UTF8.GetByteCountTest(false, true, "\u00E1", 0, 1, 2);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\u00E1\u00E1\u00E1\u00E1", 0, 5, -1, 0, new Byte[] { 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 10);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1e\u00E1\u00E1\u00E1", 0, 5, -1, 0, new Byte[] { 0xC3, 0xA1, 0x65, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 9);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\u8000\u00E1\u00E1\u00E1", 0, 5, -1, 0, new Byte[] { 0xC3, 0xA1, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 11);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\uD800\uDC00\u00E1\u00E1\u00E1", 0, 6, -1, 0, new Byte[] { 0xC3, 0xA1, 0xF0, 0x90, 0x80, 0x80, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 12);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1", 0, 15, -1, 0, new Byte[] { 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 30);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\u00E1e\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1", 0, 15, -1, 0, new Byte[] { 0xC3, 0xA1, 0xC3, 0xA1, 0x65, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 29);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\u00E1\u8000\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1", 0, 15, -1, 0, new Byte[] { 0xC3, 0xA1, 0xC3, 0xA1, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 31);
            s_encodingUtil_UTF8.GetBytesTest("\u00E1\u00E1\uD800\uDC00\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1\u00E1", 0, 16, -1, 0, new Byte[] { 0xC3, 0xA1, 0xC3, 0xA1, 0xF0, 0x90, 0x80, 0x80, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1, 0xC3, 0xA1 }, 32);

            // 3 bytes
            s_encodingUtil_UTF8.GetBytesTest("\u8000", 0, 1, -1, 0, new Byte[] { 0xE8, 0x80, 0x80 }, 3);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\u8000", 0, 1, -1, 0, new Byte[] { 0xE8, 0x80, 0x80 }, 3);
            s_encodingUtil_UTF8.GetByteCountTest("\u8000", 0, 1, 3);
            s_encodingUtil_UTF8.GetByteCountTest(false, true, "\u8000", 0, 1, 3);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u8000\u8000\u8000", 0, 4, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 12);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u0065\u8000\u8000", 0, 4, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0x65, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 10);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u00E1\u8000\u8000", 0, 4, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 11);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\uD800\uDC00\u8000\u8000", 0, 5, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 13);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000", 0, 15, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 45);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u8000\u8000\u0065\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000", 0, 15, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0x65, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 43);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u8000\u8000\u00E1\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000", 0, 15, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 44);
            s_encodingUtil_UTF8.GetBytesTest("\u8000\u8000\u8000\uD800\uDC00\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000\u8000", 0, 16, -1, 0, new Byte[] { 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xE8, 0x80, 0x80 }, 46);

            // 4 bytes
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00", 0, 2, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 4);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\uD800\uDC00", 0, 2, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 4);
            s_encodingUtil_UTF8.GetByteCountTest("\uD800\uDC00", 0, 2, 4);
            s_encodingUtil_UTF8.GetByteCountTest(false, true, "\uD800\uDC00", 0, 2, 4);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\uD800\uDC00\uD800\uDC00", 0, 6, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 }, 12);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\u0065\uD800\uDC00", 0, 5, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0x65, 0xF0, 0x90, 0x80, 0x80 }, 9);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\u00E1\uD800\uDC00", 0, 5, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xC3, 0xA1, 0xF0, 0x90, 0x80, 0x80 }, 10);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\u8000\uD800\uDC00", 0, 5, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 }, 11);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00", 0, 16, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 }, 32);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\u0065\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00", 0, 15, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0x65, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 }, 29);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\u00E1\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00", 0, 15, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xC3, 0xA1, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 }, 30);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00\u8000\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00\uD800\uDC00", 0, 15, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 }, 31);

            // Mixed
            s_encodingUtil_UTF8.GetBytesTest("\u0065\u0065\u00E1\u0065\u0065\u8000\u00E1\u0065\uD800\uDC00\u8000\u00E1\u0065\u0065\u0065", 0, 15, -1, 0, new Byte[] { 0x65, 0x65, 0xC3, 0xA1, 0x65, 0x65, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0x65, 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0x65, 0x65, 0x65 }, 24);

            // Invalid char sequences are replaced with UTF-8 encoding of U+FFFD
            s_encodingUtil_UTF8.GetBytesTest("\uFFFD", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD }, 3);
        }

        /* High: D800 - DBFF; Low: DC00-DFFF
         Invalid/unpaired surrogates are not considered representing unique
         Unicode scalar values, therefore they should be always filtered out
         (they are removed from the roundtripping requirement by design)
        
         D800 + DC00: 110110-0000-000000 110111-0000000000: U+000-00001-00000000-00000000: 11110000 10010000 10000000 10000000
         D800 + DFFF: 110110-0000-000000 110111-1111111111: U+000-00001-00000011-11111111: 11110000 10010000 10001111 10111111
         DBFF + DC00: 110110-1111-111111 110111-0000000000: U+000-10000-11111100-00000000: 11110100 10001111 10110000 10000000
         DBFF + DFFF: 110110-1111-111111 110111-1111111111: U+000-10000-11111111-11111111: 11110100 10001111 10111111 10111111
         D800: 11101101 10100000 10000000
         DBFF: 11101101 10101111 10111111
         DC00: 11101101 10110000 10000000
         DFFF: 11101101 10111111 10111111 */
        [Fact]
        public static void Surrogates2()
        {
            // Border cases 
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDC00", 0, 2, -1, 0, new Byte[] { 0xF0, 0x90, 0x80, 0x80 }, 4);
            s_encodingUtil_UTF8.GetBytesTest("\uD800\uDFFF", 0, 2, -1, 0, new Byte[] { 0xF0, 0x90, 0x8F, 0xBF }, 4);
            s_encodingUtil_UTF8.GetBytesTest("\uDBFF\uDC00", 0, 2, -1, 0, new Byte[] { 0xF4, 0x8F, 0xB0, 0x80 }, 4);
            s_encodingUtil_UTF8.GetBytesTest("\uDBFF\uDFFF", 0, 2, -1, 0, new Byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, 4);

            // Unfinished surrogates
            s_encodingUtil_UTF8.GetBytesTest("\uD800", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD }, 3);
            s_encodingUtil_UTF8.GetBytesTest("\uDBFF", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD }, 3);
            s_encodingUtil_UTF8.GetBytesTest("\uDC00", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD }, 3);
            s_encodingUtil_UTF8.GetBytesTest("\uDFFF", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD }, 3);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uD800", 0, 1, 4, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uDBFF", 0, 1, 4, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uDC00", 0, 1, 4, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uDFFF", 0, 1, 4, 0, new Byte[] { }, 0));

            // Too high
            s_encodingUtil_UTF8.GetBytesTest("\u0041\uFFFD\uFFFD", 0, 3, -1, 0, new Byte[] { 0x41, 0xEF, 0xBF, 0xBD, 0xEF, 0xBF, 0xBD }, 7);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uDC00\uDC00", 0, 2, 6, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF8.GetBytesTest(false, false, "\uDC00\uDC00", 0, 2, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD, 0xEF, 0xBF, 0xBD }, 6);

            // Too low
            s_encodingUtil_UTF8.GetBytesTest("\u0041\uFFFD\uFFFD", 0, 3, -1, 0, new Byte[] { 0x41, 0xEF, 0xBF, 0xBD, 0xEF, 0xBF, 0xBD }, 7);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uD800\uD800", 0, 2, 6, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF8.GetBytesTest(false, false, "\uD800\uD800", 0, 2, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD, 0xEF, 0xBF, 0xBD }, 6);

            // Invalid second in surrogate pair
            s_encodingUtil_UTF8.GetBytesTest("\u0041\uD800\uE000", 0, 3, -1, 0, new Byte[] { 0x41, 0xEF, 0xBF, 0xBD, 0xEE, 0x80, 0x80 }, 7);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uD800\uE000", 0, 2, 6, 0, new Byte[] { }, 0));

            // Hacked surrogates
            s_encodingUtil_UTF8.GetBytesTest("\uD800\u0041\uDC00", 0, 3, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD, 0x41, 0xEF, 0xBF, 0xBD }, 7);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uD800\u0041\uDC00", 0, 3, 7, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF8.GetBytesTest(false, false, "\uD800\u0041\uDC00", 0, 3, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD, 0x41, 0xEF, 0xBF, 0xBD }, 7);
            s_encodingUtil_UTF8.GetByteCountTest("\uD800\u0041\uDC00", 0, 3, 7);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetByteCountTest(false, true, "\uD800\u0041\uDC00", 0, 3, 0));
            s_encodingUtil_UTF8.GetBytesTest("\uD800\u0041\u0042\u07FF\u0043\uDC00", 0, 6, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBD, 0x41, 0x42, 0xDF, 0xBF, 0x43, 0xEF, 0xBF, 0xBD }, 11);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetBytesTest(false, true, "\uD800\u0041\u0042\u07FF\u0043\uDC00", 0, 6, 11, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF8.GetByteCountTest("\uD800\u0041\u0042\u07FF\u0043\uDC00", 0, 6, 11);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF8.GetByteCountTest(false, true, "\uD800\u0041\u0042\u07FF\u0043\uDC00", 0, 6, 0));
        }

        // They don't represent abstract characters, but still need to be transmitted
        [Fact]
        public static void Specials2()
        {
            // U+FFFF
            s_encodingUtil_UTF8.GetBytesTest("\uFFFF", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBF }, 3);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\uFFFF", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBF }, 3);

            // U+FFFE
            s_encodingUtil_UTF8.GetBytesTest("\uFFFE", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBE }, 3);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\uFFFE", 0, 1, -1, 0, new Byte[] { 0xEF, 0xBF, 0xBE }, 3);

            // U+FDD0 - U+FDEF
            s_encodingUtil_UTF8.GetBytesTest("\uFDD0\uFDEF", 0, 2, -1, 0, new Byte[] { 0xEF, 0xB7, 0x90, 0xEF, 0xB7, 0xAF }, 6);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\uFDD0\uFDEF", 0, 2, -1, 0, new Byte[] { 0xEF, 0xB7, 0x90, 0xEF, 0xB7, 0xAF }, 6);

            // BOM
            s_encodingUtil_UTF8.GetBytesTest("\uFEFF\u0041", 0, 2, -1, 0, new Byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 4);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "\uFEFF\u0041", 0, 2, -1, 0, new Byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 4);
            s_encodingUtil_UTF8.GetBytesTest(false, true, "A", 0, 1, 4, 0, new Byte[] { 0x41, 0x00, 0x00, 0x00 }, 1);
            s_encodingUtil_UTF8.GetBytesTest(true, true, "A", 0, 1, 4, 0, new Byte[] { 0x41, 0x00, 0x00, 0x00 }, 1);
        }

        /* ---------- Taxonomy of Well-formed Byte Sequences ---------        
         1. U+0000   - U+007F  : 00-7F
         2. U+0080   - U+07FF  : C2-DF 80-BF
         3. U+0800   - U+0FFF  : E0    A0-BF 80-BF
         4. U+1000   - U+CFFF  : E1-EC 80-BF 80-BF
         5. U+D000   - U+D7FF  : ED    80-9F 80-BF
         6. U+E000   - U+FFFF  : EE-EF 80-BF 80-BF
         7. U+10000  - U+3FFFF : F0    90-BF 80-BF 80-BF
         8. U+40000  - U+FFFFF : F1-F3 80-BF 80-BF 80-BF
         9. U+100000 - U+10FFFF: F4    80-8F 80-BF 80-BF */
        [Fact]
        public static void WellFormedByteSequence()
        {
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x00, 0x7F }, 0, 2, -1, 0, "\u0000\u007F", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F }, 0, 14, -1, 0, "\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F", 14);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0x80, 0x90, 0xA0, 0xB0, 0xC1 }, 0, 5, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 5);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x80, 0x90, 0xA0, 0xB0, 0xC1 }, 0, 15, -1, 0, "\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 15);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0x80, 0x90, 0xA0, 0xB0, 0xC1, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F }, 0, 15, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F", 15);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xC2, 0x80, 0xDF, 0xBF }, 0, 4, -1, 0, "\u0080\u07FF", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xC2, 0x80, 0xDF, 0xBF, 0xC2, 0x80, 0xDF, 0xBF, 0xC2, 0x80, 0xDF, 0xBF, 0xC2, 0x80, 0xDF, 0xBF }, 0, 16, -1, 0, "\u0080\u07FF\u0080\u07FF\u0080\u07FF\u0080\u07FF", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xC2, 0x7F, 0xC2, 0xC0, 0xDF, 0x7F, 0xDF, 0xC0 }, 0, 8, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xC2, 0xDF }, 0, 2, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0x80, 0x80, 0xC1, 0x80, 0xC1, 0xBF }, 0, 6, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 6);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xC2, 0x7F, 0xC2, 0xC0, 0x7F, 0x7F, 0x7F, 0x7F, 0xC3, 0xA1, 0xDF, 0x7F, 0xDF, 0xC0 }, 0, 14, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\u007F\u007F\u007F\u007F\u00E1\uFFFD\u007F\uFFFD\uFFFD", 13);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF }, 0, 6, -1, 0, "\u0800\u0FFF", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF, 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF, 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF }, 0, 18, -1, 0, "\u0800\u0FFF\u0800\u0FFF\u0800\u0FFF", 6);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xE0, 0xA0, 0x7F, 0xE0, 0xA0, 0xC0, 0xE0, 0xBF, 0x7F, 0xE0, 0xBF, 0xC0 }, 0, 12, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xE0, 0x9F, 0x80, 0xE0, 0xC0, 0x80, 0xE0, 0x9F, 0xBF, 0xE0, 0xC0, 0xBF }, 0, 12, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 10);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xE0, 0xA0, 0x7F, 0xE0, 0xA0, 0xC0, 0x7F, 0xE0, 0xBF, 0x7F, 0xC3, 0xA1, 0xE0, 0xBF, 0xC0 }, 0, 15, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\u007F\uFFFD\u007F\u00E1\uFFFD\uFFFD", 10);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF }, 0, 6, -1, 0, "\u1000\uCFFF", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF, 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF, 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF }, 0, 18, -1, 0, "\u1000\uCFFF\u1000\uCFFF\u1000\uCFFF", 6);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xE1, 0x80, 0x7F, 0xE1, 0x80, 0xC0, 0xE1, 0xBF, 0x7F, 0xE1, 0xBF, 0xC0, 0xEC, 0x80, 0x7F, 0xEC, 0x80, 0xC0, 0xEC, 0xBF, 0x7F, 0xEC, 0xBF, 0xC0 }, 0, 24, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 16);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xE1, 0x7F, 0x80, 0xE1, 0xC0, 0x80, 0xE1, 0x7F, 0xBF, 0xE1, 0xC0, 0xBF, 0xEC, 0x7F, 0x80, 0xEC, 0xC0, 0x80, 0xEC, 0x7F, 0xBF, 0xEC, 0xC0, 0xBF }, 0, 24, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD", 24);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF }, 0, 6, -1, 0, "\uD000\uD7FF", 2);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF, 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF, 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF }, 0, 18, -1, 0, "\uD000\uD7FF\uD000\uD7FF\uD000\uD7FF", 6);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0x80, 0x7F, 0xED, 0x80, 0xC0, 0xED, 0x9F, 0x7F, 0xED, 0x9F, 0xC0 }, 0, 12, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0x7F, 0x80, 0xED, 0xA0, 0x80, 0xED, 0x7F, 0xBF, 0xED, 0xA0, 0xBF }, 0, 12, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD", 10);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xED, 0x7F, 0x80, 0xED, 0xA0, 0x80, 0xE8, 0x80, 0x80, 0xED, 0x7F, 0xBF, 0xED, 0xA0, 0xBF }, 0, 15, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u8000\uFFFD\u007F\uFFFD\uFFFD\uFFFD", 11);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xEE, 0x80, 0x80, 0xEF, 0xBF, 0xBF, 0xEE, 0x80, 0x80, 0xEF, 0xBF, 0xBF, 0xEE, 0x80, 0x80, 0xEF, 0xBF, 0xBF }, 0, 18, -1, 0, "\uE000\uFFFF\uE000\uFFFF\uE000\uFFFF", 6);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xEE, 0x80, 0x7F, 0xEE, 0x80, 0xC0, 0xEE, 0xBF, 0x7F, 0xEE, 0xBF, 0xC0, 0xEF, 0x80, 0x7F, 0xEF, 0x80, 0xC0, 0xEF, 0xBF, 0x7F, 0xEF, 0xBF, 0xC0 }, 0, 24, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 16);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xEE, 0x7F, 0x80, 0xEE, 0xC0, 0x80, 0xEE, 0x7F, 0xBF, 0xEE, 0xC0, 0xBF, 0xEF, 0x7F, 0x80, 0xEF, 0xC0, 0x80, 0xEF, 0x7F, 0xBF, 0xEF, 0xC0, 0xBF }, 0, 24, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD", 24);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0xBF, 0xBF, 0xBF }, 0, 8, -1, 0, "\uD800\uDC00\uD8BF\uDFFF", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0xBF, 0xBF, 0xBF, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0xBF, 0xBF, 0xBF }, 0, 16, -1, 0, "\uD800\uDC00\uD8BF\uDFFF\uD800\uDC00\uD8BF\uDFFF", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF0, 0x90, 0x80, 0x7F, 0xF0, 0x90, 0x80, 0xC0, 0xF0, 0xBF, 0xBF, 0x7F, 0xF0, 0xBF, 0xBF, 0xC0 }, 0, 16, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF0, 0x90, 0x7F, 0x80, 0xF0, 0x90, 0xC0, 0x80, 0xF0, 0x90, 0x7F, 0xBF, 0xF0, 0x90, 0xC0, 0xBF }, 0, 16, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD", 12);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF0, 0x8F, 0x80, 0x80, 0xF0, 0xC0, 0x80, 0x80, 0xF0, 0x8F, 0xBF, 0xBF, 0xF0, 0xC0, 0xBF, 0xBF }, 0, 16, -1, 0, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 14);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF1, 0x80, 0x80, 0x80, 0xF3, 0xBF, 0xBF, 0xBF }, 0, 8, -1, 0, "\uD8C0\uDC00\uDBBF\uDFFF", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF1, 0x80, 0x80, 0x80, 0xF3, 0xBF, 0xBF, 0xBF, 0xF1, 0x80, 0x80, 0x80, 0xF3, 0xBF, 0xBF, 0xBF }, 0, 16, -1, 0, "\uD8C0\uDC00\uDBBF\uDFFF\uD8C0\uDC00\uDBBF\uDFFF", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF1, 0x80, 0x80, 0x7F, 0xF1, 0x80, 0x80, 0xC0, 0xF1, 0xBF, 0xBF, 0x7F, 0xF1, 0xBF, 0xBF, 0xC0, 0xF3, 0x80, 0x80, 0x7F, 0xF3, 0x80, 0x80, 0xC0, 0xF3, 0xBF, 0xBF, 0x7F, 0xF3, 0xBF, 0xBF, 0xC0 }, 0, 32, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 16);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF1, 0x80, 0x7F, 0x80, 0xF1, 0x80, 0xC0, 0x80, 0xF1, 0x80, 0x7F, 0xBF, 0xF1, 0x80, 0xC0, 0xBF, 0xF3, 0x80, 0x7F, 0x80, 0xF3, 0x80, 0xC0, 0x80, 0xF3, 0x80, 0x7F, 0xBF, 0xF3, 0x80, 0xC0, 0xBF }, 0, 32, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD", 24);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF1, 0x7F, 0x80, 0x80, 0xF1, 0xC0, 0x80, 0x80, 0xF1, 0x7F, 0xBF, 0xBF, 0xF1, 0xC0, 0xBF, 0xBF, 0xF3, 0x7F, 0x80, 0x80, 0xF3, 0xC0, 0x80, 0x80, 0xF3, 0x7F, 0xBF, 0xBF, 0xF3, 0xC0, 0xBF, 0xBF }, 0, 32, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 32);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF4, 0x80, 0x80, 0x80, 0xF4, 0x8F, 0xBF, 0xBF }, 0, 8, -1, 0, "\uDBC0\uDC00\uDBFF\uDFFF", 4);
            s_encodingUtil_UTF8.GetCharsTest(false, true, new Byte[] { 0xF4, 0x80, 0x80, 0x80, 0xF4, 0x8F, 0xBF, 0xBF, 0xF4, 0x80, 0x80, 0x80, 0xF4, 0x8F, 0xBF, 0xBF }, 0, 16, -1, 0, "\uDBC0\uDC00\uDBFF\uDFFF\uDBC0\uDC00\uDBFF\uDFFF", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF4, 0x80, 0x80, 0x7F, 0xF4, 0x80, 0x80, 0xC0, 0xF4, 0x8F, 0xBF, 0x7F, 0xF4, 0x8F, 0xBF, 0xC0 }, 0, 16, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD", 8);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF4, 0x80, 0x7F, 0x80, 0xF4, 0x80, 0xC0, 0x80, 0xF4, 0x80, 0x7F, 0xBF, 0xF4, 0x80, 0xC0, 0xBF }, 0, 16, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD", 12);
            s_encodingUtil_UTF8.GetCharsTest(false, false, new Byte[] { 0xF4, 0x7F, 0x80, 0x80, 0xF4, 0x90, 0x80, 0x80, 0xF4, 0x7F, 0xBF, 0xBF, 0xF4, 0x90, 0xBF, 0xBF }, 0, 16, -1, 0, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD", 14);
        }

        [Fact]
        public static void MaxCharCount()
        {
            s_encodingUtil_UTF8.GetMaxCharCountTest(0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxCharCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxCharCountTest(-2147483648, 0));
            s_encodingUtil_UTF8.GetMaxCharCountTest(2147483646, 2147483647);
            s_encodingUtil_UTF8.GetMaxCharCountTest(10, 11);
        }

        [Fact]
        public static void MaxByteCount()
        {
            s_encodingUtil_UTF8.GetMaxByteCountTest(0, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxByteCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxByteCountTest(-2147483648, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxByteCountTest(2147483647, 0));
            s_encodingUtil_UTF8.GetMaxByteCountTest(10, 33);
            s_encodingUtil_UTF8.GetMaxByteCountTest(715827881, 2147483646);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxByteCountTest(715827883, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF8.GetMaxByteCountTest(1431655765, 0));
        }

        [Fact]
        public static void Preamble()
        {
            s_encodingUtil_UTF8.GetPreambleTest(false, false, new Byte[] { });
            s_encodingUtil_UTF8.GetPreambleTest(true, false, new Byte[] { 0xEF, 0xBB, 0xBF });
            s_encodingUtil_UTF8.GetPreambleTest(new Byte[] { 0xEF, 0xBB, 0xBF });
        }

        [Fact]
        public static void Miscellaneous()
        {
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xC2, 0xA4, 0xC3, 0x90, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xC2, 0xA3, 0xC2, 0xA4 }, 0, 16, -1, 0, "\u00A4\u00D0aR|{AnGe\u00A3\u00A4", 12);
            s_encodingUtil_UTF8.GetBytesTest("\u00A4\u00D0aR|{AnGe\u00A3\u00A4", 0, 12, -1, 0, new Byte[] { 0xC2, 0xA4, 0xC3, 0x90, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xC2, 0xA3, 0xC2, 0xA4 }, 16);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xA4, 0xD0, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xA3, 0xA4 }, 0, 12, -1, 0, "\uFFFD\uFFFD\u0061\u0052\u007C\u007B\u0041\u006E\u0047\u0065\uFFFD\uFFFD", 12);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xA3 }, 0, 1, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xA3, 0xA4 }, 0, 2, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x65, 0xA3, 0xA4 }, 0, 3, -1, 0, "\u0065\uFFFD\uFFFD", 3);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0x47, 0x65, 0xA3, 0xA4 }, 0, 4, -1, 0, "\u0047\u0065\uFFFD\uFFFD", 4);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xA4, 0xD0, 0x61, 0xA3, 0xA4 }, 0, 5, -1, 0, "\uFFFD\uFFFD\u0061\uFFFD\uFFFD", 5);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xA4, 0xD0, 0x61, 0xA3 }, 0, 4, -1, 0, "\uFFFD\uFFFD\u0061\uFFFD", 4);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xD0, 0x61, 0xA3 }, 0, 3, -1, 0, "\uFFFD\u0061\uFFFD", 3);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xA4, 0x61, 0xA3 }, 0, 3, -1, 0, "\uFFFD\u0061\uFFFD", 3);
            s_encodingUtil_UTF8.GetCharsTest(new Byte[] { 0xD0, 0x61, 0x52, 0xA3 }, 0, 4, -1, 0, "\uFFFD\u0061\u0052\uFFFD", 4);
        }
    }
}