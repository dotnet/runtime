// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class ASCII
    {
        private static readonly EncodingTestHelper s_encodingUtil_ASCII = new EncodingTestHelper("ASCII");

        [Fact]
        public static void GetByteCount_BoundaryValues()
        {
            s_encodingUtil_ASCII.GetByteCountTest(String.Empty, 0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest(String.Empty, 0, 1, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_ASCII.GetByteCountTest((String)null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", 1, -1, 0));
            s_encodingUtil_ASCII.GetByteCountTest("abc", 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", 3, 1, 0));
            s_encodingUtil_ASCII.GetByteCountTest("abc", 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetByteCountTest("abc", 2, 2, 0));
        }

        [Fact]
        public static void GetCharCount_BoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_ASCII.GetCharCountTest((Byte[])null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0));
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 1, 0));
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 2, 0));
        }

        [Fact]
        public static void GetBytes_BoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_ASCII.GetBytesTest((String)null, 0, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", -1, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", -1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 4, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 1, 3, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void GetChars_BoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_ASCII.GetCharsTest((Byte[])null, 0, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 4, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, 3, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetChars_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, -2, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, -1, 1, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 1, 0, String.Empty, 0));
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, 1, 1, "\u0000", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, 1, 2, String.Empty, 0));
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 5, 1, "\u0000\u0061\u0062\u0063\u0000", 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 1, -1, -1, String.Empty, 0));
        }

        [Fact]
        public static void GetBytes_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 3, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 3, 0, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 3, -1, 1, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 3, 1, 0, (Byte[])null, 0));
            s_encodingUtil_ASCII.GetBytesTest("abc", 0, 0, 1, 1, new Byte[] { 0x00 }, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 0, 1, 2, (Byte[])null, 0));
            s_encodingUtil_ASCII.GetBytesTest("abc", 0, 3, 5, 1, new Byte[] { 0x00, 0x61, 0x62, 0x63, 0x00 }, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 0, -1, -1, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetBytesTest("abc", 0, 1, -1, -1, (Byte[])null, 0));
        }

        [Fact]
        public static void CorrectByteCodes()
        {
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x41 }, 0, 1, -1, 0, "A", 1);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x41 }, 0, 1, 1);
        }

        [Fact]
        public static void InvalidSequences()
        {
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x7F }, 0, 1, -1, 0, "", 1);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x7F }, 0, 1, 1);
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0x80 }, 0, 1, -1, 0, "?", 1);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x80 }, 0, 1, 1);
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0xFF }, 0, 1, -1, 0, "?", 1);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0xFF }, 0, 1, 1);
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0xC1 }, 0, 1, -1, 0, "?", 1);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0xC1 }, 0, 1, 1);
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0xC1, 0x41, 0xF0, 0x42 }, 0, 4, -1, 0, "?A?B", 4);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0xC1, 0x41, 0xF0, 0x42 }, 0, 4, 4);
            s_encodingUtil_ASCII.GetBytesTest("A", 0, 1, -1, 0, new Byte[] { 0x41 }, 1);
            s_encodingUtil_ASCII.GetByteCountTest("A", 0, 1, 1);
            s_encodingUtil_ASCII.GetBytesTest("\u0080", 0, 1, -1, 0, new Byte[] { 0x3F }, 1);
            s_encodingUtil_ASCII.GetByteCountTest("\u0080", 0, 1, 1);
            s_encodingUtil_ASCII.GetBytesTest("\uD800\uDC00\u0061\u0CFF", 0, 4, -1, 0, new Byte[] { 0x3F, 0x3F, 0x61, 0x3F }, 4);
            s_encodingUtil_ASCII.GetByteCountTest("\uD800\uDC00\u0061\u0CFF", 0, 4, 4);
            s_encodingUtil_ASCII.GetBytesTest("\u00FF", 0, 1, -1, 0, new Byte[] { 0x3F }, 1);
        }

        [Fact]
        public static void MaxCharCount()
        {
            s_encodingUtil_ASCII.GetMaxCharCountTest(0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetMaxCharCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetMaxCharCountTest(-2147483648, 0));
            s_encodingUtil_ASCII.GetMaxCharCountTest(2147483647, 2147483647);
            s_encodingUtil_ASCII.GetMaxCharCountTest(10, 10);
        }

        [Fact]
        public static void MaxByteCount()
        {
            s_encodingUtil_ASCII.GetMaxByteCountTest(0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetMaxByteCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_ASCII.GetMaxByteCountTest(-2147483648, 0));
            s_encodingUtil_ASCII.GetMaxByteCountTest(2147483646, 2147483647);
            s_encodingUtil_ASCII.GetMaxByteCountTest(10, 11);
        }

        [Fact]
        public static void Preamble()
        {
            s_encodingUtil_ASCII.GetPreambleTest(new Byte[] { });
        }

        [Fact]
        public static void DefaultFallback()
        {
            s_encodingUtil_ASCII.GetBytesTest("\u0080\u00FF\u0B71\uFFFF\uD800\uDFFF", 0, 6, -1, 0, new Byte[] { 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F }, 6);
        }
    }
}