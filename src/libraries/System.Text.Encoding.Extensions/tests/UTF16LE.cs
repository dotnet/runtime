// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class UTF16LE
    {
        private static readonly EncodingTestHelper s_encodingUtil_UTF16LE = new EncodingTestHelper("UTF-16LE");

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues()
        {
            s_encodingUtil_UTF16LE.GetByteCountTest(String.Empty, 0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest(String.Empty, 0, 1, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetByteCountTest((String)null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", 1, -1, 0));
            s_encodingUtil_UTF16LE.GetByteCountTest("abc", 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", 3, 1, 0));
            s_encodingUtil_UTF16LE.GetByteCountTest("abc", 2, 1, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest("abc", 2, 2, 0));
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetCharCountTest((Byte[])null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 1, -1, 0));
            s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 6, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 6, 1, 0));
            s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 4, 2, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 7, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 4, 3, 0));
        }

        [Fact]
        public static void GetBytesCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetByteCountTest((String)null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetByteCountTest(String.Empty, -1, 0));
            s_encodingUtil_UTF16LE.GetByteCountTest(String.Empty, 0, 0);
            s_encodingUtil_UTF16LE.GetByteCountTest("a", 0, 0);
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetCharCountTest((Byte[])null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { }, -1, 0));
            s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { }, 0, 0);
            s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00 }, 0, 0);
        }

        [Fact]
        public static void GetBytesCount_InvalidConversionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetBytesTest((String)null, 0, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", -1, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", -1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 4, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 1, 3, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void GetCharCount_InvalidConversionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetCharsTest((Byte[])null, 0, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, -1, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, -1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 7, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 2, 5, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetChars_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, -2, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, -1, 1, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, 1, 0, String.Empty, 0));
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 0, 1, 1, "\u0000", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 0, 1, 2, String.Empty, 0));
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, 5, 1, "\u0000\u0061\u0062\u0063\u0000", 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 0, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 2, -1, -1, String.Empty, 0));
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, 0, 0, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, 0, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00 }, 0, 0, 1, 0, "\u0000", 0);
        }

        [Fact]
        public static void GetBytes_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 3, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 3, 0, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 3, -1, 1, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 3, 1, 0, (Byte[])null, 0));
            s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 0, 1, 1, new Byte[] { 0x00 }, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 0, 1, 2, (Byte[])null, 0));
            s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 3, 8, 1, new Byte[] { 0x00, 0x61, 0x00, 0x62, 0x00, 0x63, 0x00, 0x00 }, 6);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 0, -1, -1, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest("abc", 0, 1, -1, -1, (Byte[])null, 0));
            s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, 0, 0, -1, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, 0, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF16LE.GetBytesTest("a", 0, 0, 2, 0, new Byte[] { 0x00, 0x00 }, 0);
        }

        [Fact]
        public static void GetChars_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetCharsTest((Byte[])null, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, 0, -2, 0, (String)null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, 0, 0, -1, String.Empty, 0));
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { }, 0, -1, -100, String.Empty, 0);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00 }, 0, 1, 1, "\u0000", 0);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00 }, 2, -1, -100, "a", 1);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00 }, 2, 2, 2, "\u0061\u0000", 1);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00 }, 2, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetBytes_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetBytesTest((String)null, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, 0, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, 0, 0, -1, new Byte[] { }, 0));
            s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF16LE.GetBytesTest(String.Empty, 0, -1, -100, new Byte[] { }, 0);
            s_encodingUtil_UTF16LE.GetBytesTest("a", 0, 2, 2, new Byte[] { 0x00, 0x00 }, 0);
            s_encodingUtil_UTF16LE.GetBytesTest("a", 1, -1, -100, new Byte[] { 0x61, 0x00 }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("a", 1, 4, 4, new Byte[] { 0x61, 0x00, 0x00, 0x00 }, 2);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF16LE.GetBytesTest("a", 1, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void BasicValidInputs()
        {
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00 }, 0, 2, -1, 0, "a", 1);
        }

        [Fact]
        public static void InvalidSequences()
        {
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x00 }, 0, 3, -1, 0, "\u0061\uFFFD", 2);
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61 }, 0, 1, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0x61, 0x00, 0x00 }, 0, 3, 2, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0x61 }, 0, 1, 1, 0, String.Empty, 0));
        }

        // They don't represent abstract characters, but still need to be transmitted
        [Fact]
        public static void Specials()
        {
            // U+FFFF, U+FFFE, U+FFF
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0xFF, 0xFF, 0xFE, 0xFF }, 0, 4, -1, 0, "\uFFFF\uFFFE", 2);
            s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xFF, 0xFE, 0xFF }, 0, 4, -1, 0, "\uFFFF\uFFFE", 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uFFFF\uFFFE", 0, 2, -1, 0, new Byte[] { 0xFF, 0xFF, 0xFE, 0xFF }, 4);
            s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uFFFF\uFFFE", 0, 2, -1, 0, new Byte[] { 0xFF, 0xFF, 0xFE, 0xFF }, 4);

            // U+FDD0 - U+FDE
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0xD0, 0xFD, 0xEF, 0xFD }, 0, 4, -1, 0, "\uFDD0\uFDEF", 2);
            s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0xD0, 0xFD, 0xEF, 0xFD }, 0, 4, -1, 0, "\uFDD0\uFDEF", 2);
        }

        [Fact]
        public static void Surrogates()
        {
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 0, 4, -1, 0, "\uD800\uDC00", 2);
            s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 0, 4, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uD800\uDC00", 0, 2, -1, 0, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 4);
            s_encodingUtil_UTF16LE.GetByteCountTest("\uD800\uDC00", 0, 2, 4);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 0, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0xFF, 0xDB, 0x00, 0xDC }, 0, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0xFF, 0xDB, 0x00, 0xDC }, 0, 3, -1, 0, "\uFFFD\uFFFD", 2);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 2, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0x00, 0xD8, 0xFF, 0xDF }, 2, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF16LE.GetCharsTest(false, false, new Byte[] { 0xFF, 0xDF }, 0, 2, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 0, 2, 1, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0x00, 0xDC }, 0, 2, 1, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 0, 3, 2, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0x00, 0xDC }, 0, 3, 2, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0x00, 0xD8, 0x00, 0xDC }, 2, 2, 1, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0x00, 0xD8, 0xFF, 0xDF }, 2, 2, 1, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0xFF, 0xDF }, 1, 3, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF16LE.GetBytesTest("\uD800\uDC00", 0, 1, -1, 0, new Byte[] { 0xFD, 0xFF }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uDBFF\uDFFF", 0, 1, -1, 0, new Byte[] { 0xFD, 0xFF }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uD800\uDC00", 1, 1, -1, 0, new Byte[] { 0xFD, 0xFF }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uDBFF\uDFFF", 1, 1, -1, 0, new Byte[] { 0xFD, 0xFF }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uD800\uDC00\uFFFD\uFEB7", 0, 4, -1, 0, new Byte[] { 0x00, 0xD8, 0x00, 0xDC, 0xFD, 0xFF, 0xB7, 0xFE }, 8);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uD800\uDC00", 0, 1, 2, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uDBFF\uDFFF", 0, 1, 2, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uD800\uDC00", 1, 1, 2, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uDBFF\uDFFF", 1, 1, 2, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void MaxCharCount()
        {
            s_encodingUtil_UTF16LE.GetMaxCharCountTest(0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxCharCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxCharCountTest(-2147483648, 0));
            s_encodingUtil_UTF16LE.GetMaxCharCountTest(2147483646, 1073741824);
            s_encodingUtil_UTF16LE.GetMaxCharCountTest(2147483647, 1073741825);
            s_encodingUtil_UTF16LE.GetMaxCharCountTest(10, 6);
        }

        [Fact]
        public static void MaxByteCount()
        {
            s_encodingUtil_UTF16LE.GetMaxByteCountTest(0, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxByteCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxByteCountTest(-2147483648, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxByteCountTest(2147483647, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxByteCountTest(1342177279, 0));
            s_encodingUtil_UTF16LE.GetMaxByteCountTest(1073741822, 2147483646);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF16LE.GetMaxByteCountTest(1073741824, 0));
            s_encodingUtil_UTF16LE.GetMaxByteCountTest(10, 22);
        }

        [Fact]
        public static void Preamble()
        {
            s_encodingUtil_UTF16LE.GetPreambleTest(new Byte[] { 0xFF, 0xFE });
            s_encodingUtil_UTF16LE.GetPreambleTest(true, false, new Byte[] { 0xFF, 0xFE });
            s_encodingUtil_UTF16LE.GetPreambleTest(false, true, new Byte[] { });
        }
    }
}