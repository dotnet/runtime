// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class ISO_8859_1
    {
        private static readonly EncodingTestHelper s_encodingUtil_Latin1 = new EncodingTestHelper("iso-8859-1");

        [Fact]
        public static void BoundaryValues()
        {
            s_encodingUtil_Latin1.GetByteCountTest(String.Empty, 0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest(String.Empty, 0, 1, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_Latin1.GetByteCountTest((String)null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", 1, -1, 0));
            s_encodingUtil_Latin1.GetByteCountTest("abc", 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", 3, 1, 0));
            s_encodingUtil_Latin1.GetByteCountTest("abc", 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetByteCountTest("abc", 2, 2, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_Latin1.GetCharCountTest((Byte[])null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0));
            s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 2, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_Latin1.GetBytesTest((String)null, 0, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", -1, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", -1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 4, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 1, 3, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_Latin1.GetCharsTest((Byte[])null, 0, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 4, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, 3, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, -2, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, -1, 1, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 1, 0, String.Empty, 0));
            s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, 1, 1, "\u0000", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, 1, 2, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 1, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 3, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 3, 0, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 3, -1, 1, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 3, 1, 0, (Byte[])null, 0));
            s_encodingUtil_Latin1.GetBytesTest("abc", 0, 0, 1, 1, new Byte[] { 0x00 }, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 0, 1, 2, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 0, -1, -1, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetBytesTest("abc", 0, 1, -1, -1, (Byte[])null, 0));
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void ValidCodes()
        {
            s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x01, 0x09, 0x10, 0x3F, 0x5C, 0x9F, 0xCB, 0xE7, 0xFF }, 0, 9, -1, 0, "\u0001\u0009\u0010\u003F\u005C\u009F\u00CB\u00E7\u00FF", 9);
            s_encodingUtil_Latin1.GetCharsTest(new Byte[] { 0x60, 0x7E, 0xE3 }, 0, 3, -1, 0, "\u0060\u007E\u00E3", 3);
            s_encodingUtil_Latin1.GetBytesTest("\u0001\u0060\u007E\u00E3\u0108\u2018\uFF59", 0, 7, -1, 0, new Byte[] { 0x01, 0x60, 0x7E, 0xE3, 0x43, 0x27, 0x79 }, 7);
            s_encodingUtil_Latin1.GetBytesTest("\uFF59\uFF60\u0262\u5FC3", 0, 4, -1, 0, new Byte[] { 0x79, 0x3F, 0x3F, 0x3F }, 4);
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void MaxCharCount()
        {
            s_encodingUtil_Latin1.GetMaxCharCountTest(0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetMaxCharCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetMaxCharCountTest(-2147483648, 0));
            s_encodingUtil_Latin1.GetMaxCharCountTest(2147483647, 2147483647);
            s_encodingUtil_Latin1.GetMaxCharCountTest(10, 10);
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void MaxByteCount()
        {
            s_encodingUtil_Latin1.GetMaxByteCountTest(0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetMaxByteCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_Latin1.GetMaxByteCountTest(-2147483648, 0));
            s_encodingUtil_Latin1.GetMaxByteCountTest(2147483646, 2147483647);
            s_encodingUtil_Latin1.GetMaxByteCountTest(10, 11);
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void Preamble()
        {
            s_encodingUtil_Latin1.GetPreambleTest(new Byte[] { });
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void DefaultFallback()
        {
            s_encodingUtil_Latin1.GetBytesTest("\uD800\uDFFF", 0, 2, -1, 0, new Byte[] { 0x3F, 0x3F }, 2);
            s_encodingUtil_Latin1.GetBytesTest("\u0100\u201E\uFF5E\u16DA", 0, 4, -1, 0, new Byte[] { 0x41, 0x22, 0x7E, 0x3F }, 4);
        }
    }
}