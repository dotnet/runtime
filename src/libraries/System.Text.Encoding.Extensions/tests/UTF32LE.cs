// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class UTF32LE
    {
        private static readonly EncodingTestHelper s_encodingUtil_UTF32LE = new EncodingTestHelper("UTF-32LE");

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues()
        {
            s_encodingUtil_UTF32LE.GetByteCountTest("abc", 2, 1, 4);
        }
                
        [Fact]
        public static void GetChars_BufferBoundary()
        {
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x00, 0x00, 0x62, 0x00, 0x00, 0x00 }, 0, 8, 4, 1, "\u0000\u0061\u0062\u0000", 2);
        }
        
        // They don't represent abstract characters, but still need to be transmitted
        [Fact]
        public static void Specials()
        {
            // U+FFFF, U+FFFE, U+FFFD
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 0, 12, -1, 0, "\uFFFF\uFFFE\uFFFD", 3);
            s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 0, 12, -1, 0, "\uFFFF\uFFFE\uFFFD", 3);
            s_encodingUtil_UTF32LE.GetBytesTest("\uFFFF\uFFFE\uFFFD", 0, 3, -1, 0, new Byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 12);
            s_encodingUtil_UTF32LE.GetBytesTest(false, true, "\uFFFF\uFFFE\uFFFD", 0, 3, -1, 0, new Byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 12);

            // U+FDD0 - U+FDEF
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xD0, 0xFD, 0x00, 0x00, 0xEF, 0xFD, 0x00, 0x00 }, 0, 8, -1, 0, "\uFDD0\uFDEF", 2);
            s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0xD0, 0xFD, 0x00, 0x00, 0xEF, 0xFD, 0x00, 0x00 }, 0, 8, -1, 0, "\uFDD0\uFDEF", 2);
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
        /// DBFF + DFFF: 110110-1111-111111 110111-1111111111: U+000-10000-11111111-1111111
        /// </summary>
        [Fact]
        public static void Surrogates()
        {
            s_encodingUtil_UTF32LE.GetBytesTest("\uD800\uDFFF", 0, 2, -1, 0, new Byte[] { 0xFF, 0x03, 0x01, 0x00 }, 4);
            s_encodingUtil_UTF32LE.GetBytesTest("\uDBFF\uDC00", 0, 2, -1, 0, new Byte[] { 0x00, 0xFC, 0x10, 0x00 }, 4);
            s_encodingUtil_UTF32LE.GetBytesTest("\uDBFF\uDFFF", 0, 2, -1, 0, new Byte[] { 0xFF, 0xFF, 0x10, 0x00 }, 4);
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x01, 0x00, 0xFF, 0xFF, 0x10, 0x00 }, 0, 8, -1, 0, "\uD800\uDC00\uDBFF\uDFFF", 4);

            // Invalid - 1 surrogate character
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 4, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 4, 1, 0, String.Empty, 0));
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 4, 4, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 4, 4, 1, 0, String.Empty, 0));

            // Surrogate pair
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 8, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 8, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x00, 0xD8, 0x00, 0x00, 0x00, 0xDC, 0x00, 0x00 }, 0, 8, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0x00, 0xD8, 0x00, 0x00, 0x00, 0xDC, 0x00, 0x00 }, 0, 8, 2, 0, String.Empty, 0));

            // Invalid first/second
            s_encodingUtil_UTF32LE.GetBytesTest("\uD800\u0041", 0, 2, -1, 0, new Byte[] { 0xFD, 0xFF, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 }, 8);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF32LE.GetBytesTest(false, true, "\uD800\u0041", 0, 2, 4, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF32LE.GetBytesTest("\u0065\uDC00", 0, 2, -1, 0, new Byte[] { 0x65, 0x00, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 8);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF32LE.GetBytesTest(false, true, "\u0065\uDC00", 0, 2, 4, 0, new Byte[] { }, 0));
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 0, 8, -1, 0, "\uFFFD\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 0, 8, 2, 0, String.Empty, 0));
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x00, 0x80, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 8, -1, 0, "\u8000\uFFFD", 2);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0x00, 0x80, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 8, 2, 0, String.Empty, 0));

            // Too high scalar values
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xFF, 0x11, 0x00 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x11, 0x00 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xFF, 0x10, 0x01 }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0x00, 0x00, 0x00, 0xFF }, 0, 4, -1, 0, "\uFFFD", 1);
            s_encodingUtil_UTF32LE.GetCharsTest(new Byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4, -1, 0, "\uFFFD", 1);
            Assert.Throws<DecoderFallbackException>(() => s_encodingUtil_UTF32LE.GetCharsTest(false, true, new Byte[] { 0x00, 0x00, 0x11, 0x00 }, 0, 4, 1, 0, String.Empty, 0));
        }
    }
}
