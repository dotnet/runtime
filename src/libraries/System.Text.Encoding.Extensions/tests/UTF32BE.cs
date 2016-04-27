// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            s_encodingUtil_UTF32BE.GetByteCountTest("abc", 2, 1, 4);
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
            s_encodingUtil_UTF32BE.GetBytesTest("\uD800\uDFFF", 0, 2, -1, 0, new Byte[] { 0x00, 0x01, 0x03, 0xFF }, 4);
            s_encodingUtil_UTF32BE.GetBytesTest("\uDBFF\uDC00", 0, 2, -1, 0, new Byte[] { 0x00, 0x10, 0xFC, 0x00 }, 4);
            s_encodingUtil_UTF32BE.GetBytesTest("\uDBFF\uDFFF", 0, 2, -1, 0, new Byte[] { 0x00, 0x10, 0xFF, 0xFF }, 4);
            s_encodingUtil_UTF32BE.GetCharsTest(new Byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0xFF, 0xFF }, 0, 8, -1, 0, "\uD800\uDC00\uDBFF\uDFFF", 4);

            // Invalid 1 surrogate character
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
        }
    }
}
