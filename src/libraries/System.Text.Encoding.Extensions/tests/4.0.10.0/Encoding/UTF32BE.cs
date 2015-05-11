// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class UTF32BE
    {
        private static readonly EncodingTestHelper s_encodingUtil_UTF32BE = new EncodingTestHelper("UTF-32BE");

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues()
        {
            s_encodingUtil_UTF32BE.GetByteCountTest(String.Empty, 0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest(String.Empty, 0, 1, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetByteCountTest((String)null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", 1, -1, 0));
            s_encodingUtil_UTF32BE.GetByteCountTest("abc", 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", 3, 1, 0));
            s_encodingUtil_UTF32BE.GetByteCountTest("abc", 2, 1, 4);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest("abc", 2, 2, 0));
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetCharCountTest((Byte[])null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 1, -1, 0));
            s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 8, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 8, 1, 0));
            s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 4, 4, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 9, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 4, 5, 0));
        }

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetByteCountTest((String)null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetByteCountTest(String.Empty, -1, 0));
            s_encodingUtil_UTF32BE.GetByteCountTest(String.Empty, 0, 0);
            s_encodingUtil_UTF32BE.GetByteCountTest("a", 0, 0);
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetCharCountTest((Byte[])null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { }, -1, 0));
            s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { }, 0, 0);
            s_encodingUtil_UTF32BE.GetCharCountTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 0, 0);
        }

        [Fact]
        public static void GetBytes_InvalidConversionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetBytesTest((String)null, 0, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", -1, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", -1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 4, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 1, 3, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void GetChars_InvalidConversionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetCharsTest((Byte[])null, 0, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, -1, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, -1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 9, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 4, 5, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 5, -2, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetChars_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 8, -2, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 8, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 8, -1, 1, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 8, 1, 0, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 0, 1, 1, "\u0000", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 0, 1, 2, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 8, 4, 1, "\u0000\u0061\u0062\u0000", 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 0, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62 }, 0, 4, -1, -1, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, 0, 0, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, 0, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 0, 0, 1, 0, "\u0000", 0);
        }

        [Fact]
        public static void GetBytes_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 3, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 3, 0, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 3, -1, 1, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 3, 1, 0, (Byte[])null, 0));
            s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 0, 1, 1, new Byte[] { 0x00 }, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 0, 1, 2, (Byte[])null, 0));
            s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 3, 14, 1, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62, 0x00, 0x00, 0x00, 0x63, 0x00 }, 12);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 0, -1, -1, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 1, -1, -1, (Byte[])null, 0));
            s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, 0, 0, -1, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, 0, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF32BE.GetBytesTest("a", 0, 0, 4, 0, new Byte[] { 0x00, 0x00, 0x00, 0x00 }, 0);
        }

        [Fact]
        public static void GetChars_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetCharsTest((Byte[])null, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, 0, -2, 0, (String)null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, 0, 0, -1, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { }, 0, -1, -100, String.Empty, 0);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 0, 1, 1, "\u0000", 0);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 4, -1, -100, "a", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 4, 2, 2, "\u0061\u0000", 1);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 4, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetBytes_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetBytesTest((String)null, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, 0, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, 0, 0, -1, new Byte[] { }, 0));
            s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF32BE.GetBytesTest(String.Empty, 0, -1, -100, new Byte[] { }, 0);
            s_encodingUtil_UTF32BE.GetBytesTest("a", 0, 4, 4, new Byte[] { 0x00, 0x00, 0x00, 0x00 }, 0);
            s_encodingUtil_UTF32BE.GetBytesTest("a", 1, -1, -100, new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 4);
            s_encodingUtil_UTF32BE.GetBytesTest("a", 1, 6, 6, new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00 }, 4);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF32BE.GetBytesTest("a", 1, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void ValidCodePoints()
        {
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x61 }, 0, 4, -1, 0, "a", 1);
            s_encodingUtil_UTF32BE.GetBytesTest("abc", 0, 3, -1, 0, new Byte[] { 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x62, 0x00, 0x00, 0x00, 0x63 }, 12);
        }

        [Fact]
        public static void GetChars_InvalidSequence_OddNumberOfByte()
        {
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x61, 0x00 }, 0, 3, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x61 }, 0, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x61 }, 0, 1, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(false, false, new Byte[] { 0x00, 0x61, 0x00 }, 0, 3, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(false, false, new Byte[] { 0x00, 0x61 }, 0, 2, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(false, false, new Byte[] { 0x61 }, 0, 1, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x61, 0x00 }, 0, 3, 1, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x61 }, 0, 2, 1, 0, String.Empty, 0));
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x61 }, 0, 1, 1, 0, String.Empty, 0));
        }

        // They don't represent abstract characters, but still need to be transmitted
        [Fact]
        public static void Specials()
        {
            // U+FFFF, U+FFFE, U+FFFD
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFE, 0x00, 0x00, 0xFF, 0xFD }, 0, 12, -1, 0, "\uFFFF\uFFFE\uFFFD", 3);
            s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFE, 0x00, 0x00, 0xFF, 0xFD }, 0, 12, -1, 0, "\uFFFF\uFFFE\uFFFD", 3);
            s_encodingUtil_UTF32BE.GetBytesTest("\uFFFF\uFFFE\uFFFD", 0, 3, -1, 0, new Byte[] { 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFE, 0x00, 0x00, 0xFF, 0xFD }, 12);
            s_encodingUtil_UTF32BE.GetBytesTest(false, true, "\uFFFF\uFFFE\uFFFD", 0, 3, -1, 0, new Byte[] { 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFE, 0x00, 0x00, 0xFF, 0xFD }, 12);

            /// U+FDD0 - U+FDEF
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xFD, 0xD0, 0x00, 0x00, 0xFD, 0xEF }, 0, 8, -1, 0, "\uFDD0\uFDEF", 2);
            s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xFD, 0xD0, 0x00, 0x00, 0xFD, 0xEF }, 0, 8, -1, 0, "\uFDD0\uFDEF", 2);
        }

        /// <summary>
        /// High: D800 - DBFF; Low: DC00-DFFF
        /// Invalid/unpaired surrogates are not considered representing unique
        /// Unicode scalar values, therefore they should be always filtered out
        /// (they are removed from the roundtripping requirement by design)
        ///
        /// D800 + DC00: 110110-0000-000000 110111-0000000000: U+000-00001-00000000-00000000
        /// D800 + DFFF: 110110-0000-000000 110111-1111111111: U+000-00001-00000011-11111111
        /// DBFF + DC00: 110110-1111-111111 110111-0000000000: U+000-10000-11111100-00000000
        /// DBFF + DFFF: 110110-1111-111111 110111-1111111111: U+000-10000-11111111-11111111
        /// </summary>
        [Fact]
        public static void Surrogates()
        {
            s_encodingUtil_UTF32BE.GetBytesTest("\uD800\uDC00", 0, 2, -1, 0, new Byte[] { 0x00, 0x01, 0x00, 0x00 }, 4);
            s_encodingUtil_UTF32BE.GetBytesTest("\uD800\uDFFF", 0, 2, -1, 0, new Byte[] { 0x00, 0x01, 0x03, 0xFF }, 4);
            s_encodingUtil_UTF32BE.GetBytesTest("\uDBFF\uDC00", 0, 2, -1, 0, new Byte[] { 0x00, 0x10, 0xFC, 0x00 }, 4);
            s_encodingUtil_UTF32BE.GetBytesTest("\uDBFF\uDFFF", 0, 2, -1, 0, new Byte[] { 0x00, 0x10, 0xFF, 0xFF }, 4);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0xFF, 0xFF }, 0, 8, -1, 0, "\uD800\uDC00\uDBFF\uDFFF", 4);

            // Invalid 1 surrogate character
            s_encodingUtil_UTF32BE.GetBytesTest("\uD800\uDC00", 0, 1, -1, 0, new Byte[] { 0x00, 0x00, 0xFF, 0xFD }, 4);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF32BE.GetBytesTest(false, true, "\uD800\uDC00", 0, 1, 4, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF32BE.GetBytesTest("\uD800\uDC00", 1, 1, -1, 0, new Byte[] { 0x00, 0x00, 0xFF, 0xFD }, 4);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF32BE.GetBytesTest(false, true, "\uD800\uDC00", 1, 1, 4, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xDF, 0xFF }, 0, 4, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xDF, 0xFF }, 0, 4, 1, 0, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xDF, 0xFF }, 4, 4, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xDF, 0xFF }, 4, 4, 1, 0, String.Empty, 0));

            // Surrogate pair
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xDF, 0xFF }, 0, 8, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xDF, 0xFF }, 0, 8, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xD8, 0x00, 0x00, 0x00, 0xDC, 0x00 }, 0, 8, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xD8, 0x00, 0x00, 0x00, 0xDC, 0x00 }, 0, 8, 2, 0, String.Empty, 0));

            // Invalid first/second
            s_encodingUtil_UTF32BE.GetBytesTest("\uD800\u0041", 0, 2, -1, 0, new Byte[] { 0x00, 0x00, 0xFF, 0xFD, 0x00, 0x00, 0x00, 0x41 }, 8);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF32BE.GetBytesTest(false, true, "\uD800\u0041", 0, 2, 8, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF32BE.GetBytesTest("\u0065\uDC00", 0, 2, -1, 0, new Byte[] { 0x00, 0x00, 0x00, 0x65, 0x00, 0x00, 0xFF, 0xFD }, 8);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF32BE.GetBytesTest(false, true, "\u0065\uDC00", 0, 2, 8, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xFF, 0xFD }, 0, 8, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0xDB, 0xFF, 0x00, 0x00, 0xFF, 0xFD }, 0, 8, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0xDF, 0xFF }, 0, 8, -1, 0, "\u8000\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0xDF, 0xFF }, 0, 8, 2, 0, String.Empty, 0));

            // Too high scalar value
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x11, 0xFF, 0xFF }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x11, 0x00, 0x00 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x01, 0x00, 0x00, 0x00 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x01, 0x10, 0xFF, 0xFF }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0xFF, 0x00, 0x00, 0x00 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32BE.GetCharsTest(false, true, new Byte[] { 0x00, 0x11, 0x00, 0x00 }, 0, 4, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF32BE.GetMaxByteCountTest(0, 4);
            s_encodingUtil_UTF32BE.GetMaxByteCountTest(2, 12);
            s_encodingUtil_UTF32BE.GetMaxByteCountTest(4, 20);
            s_encodingUtil_UTF32BE.GetMaxByteCountTest(536870910, 2147483644);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF32BE.GetMaxByteCountTest(536870911, 0));
        }

        [Fact]
        public static void GetMaxCount()
        {
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(0, 2);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(1, 2);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(2, 3);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(3, 3);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(4, 4);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(5, 4);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(6, 5);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(7, 5);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(8, 6);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(9, 6);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(10, 7);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(11, 7);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(12, 8);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(13, 8);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(14, 9);
            s_encodingUtil_UTF32BE.GetMaxCharCountTest(2147483647, 1073741825);
        }

        [Fact]
        public static void GetPreamble()
        {
            s_encodingUtil_UTF32BE.GetPreambleTest(false, false, new Byte[] { });
            s_encodingUtil_UTF32BE.GetPreambleTest(true, false, new Byte[] { 0x00, 0x00, 0xFE, 0xFF });
            s_encodingUtil_UTF32BE.GetPreambleTest(new Byte[] { 0x00, 0x00, 0xFE, 0xFF });
        }
    }
}