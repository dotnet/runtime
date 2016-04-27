// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            s_encodingUtil_UTF16LE.GetByteCountTest("abc", 2, 1, 2);
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues()
        {
            s_encodingUtil_UTF16LE.GetCharCountTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 4, 2, 1);
        }
        
        [Fact]
        public static void GetChars_BufferBoundary()
        {
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, 5, 1, "\u0000\u0061\u0062\u0063\u0000", 3);
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

            // U+FDD0 - U+FDEF
            s_encodingUtil_UTF16LE.GetCharsTest(new Byte[] { 0xD0, 0xFD, 0xEF, 0xFD }, 0, 4, -1, 0, "\uFDD0\uFDEF", 2);
            s_encodingUtil_UTF16LE.GetCharsTest(false, true, new Byte[] { 0xD0, 0xFD, 0xEF, 0xFD }, 0, 4, -1, 0, "\uFDD0\uFDEF", 2);
        }

        [Fact]
        public static void Surrogates()
        {
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
            s_encodingUtil_UTF16LE.GetBytesTest("\uDBFF\uDFFF", 0, 1, -1, 0, new Byte[] { 0xFD, 0xFF }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uDBFF\uDFFF", 1, 1, -1, 0, new Byte[] { 0xFD, 0xFF }, 2);
            s_encodingUtil_UTF16LE.GetBytesTest("\uD800\uDC00\uFFFD\uFEB7", 0, 4, -1, 0, new Byte[] { 0x00, 0xD8, 0x00, 0xDC, 0xFD, 0xFF, 0xB7, 0xFE }, 8);
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uDBFF\uDFFF", 0, 1, 2, 0, new Byte[] { }, 0));
            Assert.Throws<EncoderFallbackException>(() => s_encodingUtil_UTF16LE.GetBytesTest(false, true, "\uDBFF\uDFFF", 1, 1, 2, 0, new Byte[] { }, 0));
        }
    }
}
