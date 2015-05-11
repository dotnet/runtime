// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public static class UTF7
    {
        private static readonly EncodingTestHelper s_encodingUtil_UTF7 = new EncodingTestHelper("UTF-7");

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues()
        {
            s_encodingUtil_UTF7.GetByteCountTest(String.Empty, 0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest(String.Empty, 0, 1, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetByteCountTest((String)null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", 1, -1, 0));
            s_encodingUtil_UTF7.GetByteCountTest("abc", 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", 3, 1, 0));
            s_encodingUtil_UTF7.GetByteCountTest("abc", 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest("abc", 2, 2, 0));
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetCharCountTest((Byte[])null, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0));
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 3, 1, 0));
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 1, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 4, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 2, 0));
        }

        [Fact]
        public static void GetByteCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetByteCountTest((String)null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetByteCountTest(String.Empty, -1, 0));
            s_encodingUtil_UTF7.GetByteCountTest(String.Empty, 0, 0);
            s_encodingUtil_UTF7.GetByteCountTest("a", 0, 0);
        }

        [Fact]
        public static void GetCharCount_InvalidArgumentAndBoundaryValues_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetCharCountTest((Byte[])null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { }, -1, 0));
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { }, 0, 0);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x61 }, 0, 0);
        }

        [Fact]
        public static void GetBytes_InvalidConversionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetBytesTest((String)null, 0, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", -1, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", -1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 1, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 4, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 1, 3, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void GetChars_InvalidConversionInput()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetCharsTest((Byte[])null, 0, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, -1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 4, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 1, 3, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetChars_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, -2, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, -1, 1, String.Empty, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 1, 0, String.Empty, 0));
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, 1, 1, "\u0000", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, 1, 2, String.Empty, 0));
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 3, 5, 1, "\u0000\u0061\u0062\u0063\u0000", 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 0, -1, -1, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61, 0x62, 0x63 }, 0, 1, -1, -1, String.Empty, 0));
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, 0, 0, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, 0, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61 }, 0, 0, 1, 0, "\u0000", 0);
        }

        [Fact]
        public static void GetBytes_BufferBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 3, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 3, 0, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 3, -1, 1, (Byte[])null, 0));
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 3, 1, 0, (Byte[])null, 0));
            s_encodingUtil_UTF7.GetBytesTest("abc", 0, 0, 1, 1, new Byte[] { 0x00 }, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 0, 1, 2, (Byte[])null, 0));
            s_encodingUtil_UTF7.GetBytesTest("abc", 0, 3, 5, 1, new Byte[] { 0x00, 0x61, 0x62, 0x63, 0x00 }, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 0, -1, -1, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest("abc", 0, 1, -1, -1, (Byte[])null, 0));
            s_encodingUtil_UTF7.GetBytesTest(String.Empty, 0, 0, -1, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF7.GetBytesTest(String.Empty, 0, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF7.GetBytesTest("a", 0, 0, 1, 0, new Byte[] { 0x00 }, 0);
        }

        [Fact]
        public static void GetChars_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetCharsTest((Byte[])null, 0, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, -1, 0, 0, String.Empty, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, 0, -2, 0, (String)null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, 0, 0, -1, String.Empty, 0));
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, 0, 0, 0, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { }, 0, -1, -100, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61 }, 0, 1, 1, "\u0000", 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61 }, 1, -1, -100, "a", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61 }, 1, 2, 2, "\u0061\u0000", 1);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x61 }, 1, 0, 0, String.Empty, 0));
        }

        [Fact]
        public static void GetBytes_BufferBoundary_Pointer()
        {
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetBytesTest((String)null, 0, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest(String.Empty, -1, 0, 0, new Byte[] { }, 0));
            Assert.Throws<ArgumentNullException>(() => s_encodingUtil_UTF7.GetBytesTest(String.Empty, 0, -2, 0, (Byte[])null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetBytesTest(String.Empty, 0, 0, -1, new Byte[] { }, 0));
            s_encodingUtil_UTF7.GetBytesTest(String.Empty, 0, 0, 0, new Byte[] { }, 0);
            s_encodingUtil_UTF7.GetBytesTest(String.Empty, 0, -1, -100, new Byte[] { }, 0);
            s_encodingUtil_UTF7.GetBytesTest("a", 0, 1, 1, new Byte[] { 0x00 }, 0);
            s_encodingUtil_UTF7.GetBytesTest("a", 1, -1, -100, new Byte[] { 0x61 }, 1);
            s_encodingUtil_UTF7.GetBytesTest("a", 1, 2, 2, new Byte[] { 0x61, 0x00 }, 1);
            Assert.Throws<ArgumentException>(() => s_encodingUtil_UTF7.GetBytesTest("a", 1, 0, 0, new Byte[] { }, 0));
        }

        [Fact]
        public static void CorrectClassesOfInput()
        {
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x41, 0x09, 0x0D, 0x0A, 0x20, 0x2F, 0x7A }, 0, 7, -1, 0, "A\t\r\n /z", 7);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x41, 0x09, 0x0D, 0x0A, 0x20, 0x2F, 0x7A }, 0, 7, 7);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x45, 0x45, 0x41, 0x43, 0x51 }, 0, 7, -1, 0, "A\t", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x41, 0x45, 0x45, 0x41, 0x43, 0x51 }, 0, 7, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51 }, 0, 7, -1, 0, "!}", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51 }, 0, 7, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D }, 0, 8, -1, 0, "!}", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D }, 0, 8, 2);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x21, 0x7D }, 0, 2, -1, 0, "!}", 2);
            s_encodingUtil_UTF7.GetCharCountTest(true, false, new Byte[] { 0x21, 0x7D }, 0, 2, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D }, 1, 2, -1, 0, "AC", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D }, 1, 2, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 8, -1, 0, "\u0E59\u05D1", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 7, 2);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 8, -1, 0, "\u0E59\u05D1", 2);
            s_encodingUtil_UTF7.GetCharCountTest(true, false, new Byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 7, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x41, 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 18, -1, 0, "\u0041\u0021\u007D\u0009\u0E59\u05D1", 6);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x41, 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 17, 6);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x41, 0x21, 0x7D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 12, -1, 0, "\u0041\u0021\u007D\u0009\u0E59\u05D1", 6);
            s_encodingUtil_UTF7.GetCharCountTest(true, false, new Byte[] { 0x41, 0x21, 0x7D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 11, 6);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x32, 0x41, 0x41, 0x2D }, 0, 5, -1, 0, "\uD800", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x33, 0x2F, 0x38, 0x2D }, 0, 5, -1, 0, "\uDFFF", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x32, 0x41, 0x44, 0x66, 0x2F, 0x77, 0x2D }, 0, 8, -1, 0, "\uD800\uDFFF", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x2D }, 0, 2, -1, 0, "+", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x2D, 0x41 }, 0, 3, -1, 0, "+A", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x2B, 0x41, 0x41, 0x2D }, 0, 5, -1, 0, "\uF800", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2D }, 0, 1, -1, 0, "-", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x2D, 0x2D }, 0, 3, -1, 0, "+-", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x09 }, 0, 2, -1, 0, "\t", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x09, 0x2D }, 0, 3, -1, 0, "\t-", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x1E, 0x2D }, 0, 3, -1, 0, "-", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x7F, 0x1E, 0x2D }, 0, 4, -1, 0, "-", 3);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, -1, 0, "!-", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x80, 0x81 }, 0, 2, -1, 0, "\u0080\u0081", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x80, 0x81 }, 0, 2, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x1E }, 0, 1, -1, 0, "", 1);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x1E }, 0, 1, 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x21 }, 0, 1, -1, 0, "!", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, -1, 0, "!-", 2);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, -1, 0, "!-", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x21, 0x41, 0x41, 0x2D }, 0, 5, -1, 0, "!AA-", 4);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x2B, 0x21, 0x41, 0x41, 0x2D }, 0, 5, -1, 0, "!AA-", 4);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 5, -1, 0, "\u0080\u0081\u0082-", 4);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 5, 4);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 4, -1, 0, "\u0080\u0081\u0082", 3);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 4, 3);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 5, -1, 0, "!", 1);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x21 }, 0, 1, -1, 0, "!", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B }, 0, 1, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 5, -1, 0, "!", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 2, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 3, -1, 0, String.Empty, 0);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x48, 0x2D }, 0, 5, -1, 0, "!", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x48, 0x35, 0x41, 0x41, 0x2D }, 0, 8, -1, 0, "\u0021\uF900", 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x41, 0x43, 0x48, 0x35, 0x41, 0x41, 0x2D }, 0, 4, -1, 0, "!", 1);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 5, -1, 0, "\u0080\u0081\u0082-", 4);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 5, 4);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x09, 0x2D }, 0, 3, -1, 0, "\t-", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x09, 0x2D }, 0, 3, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, -1, 0, "!-", 2);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, 2);
            s_encodingUtil_UTF7.GetCharsTest(true, false, new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, -1, 0, "!-", 2);
            s_encodingUtil_UTF7.GetCharCountTest(true, false, new Byte[] { 0x2B, 0x21, 0x2D }, 0, 3, 2);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x21, 0x80, 0x21, 0x2D }, 0, 5, -1, 0, "!\u0080!-", 4);
            s_encodingUtil_UTF7.GetCharsTest(new Byte[] { 0x2B, 0x80, 0x21, 0x80, 0x21, 0x1E, 0x2D }, 0, 7, -1, 0, "\u0080!\u0080!-", 6);
            s_encodingUtil_UTF7.GetCharCountTest(new Byte[] { 0x2B, 0x21, 0x80, 0x21, 0x2D }, 0, 5, 4);
            s_encodingUtil_UTF7.GetBytesTest("A\t\r\n /z", 0, 7, -1, 0, new Byte[] { 0x41, 0x09, 0x0D, 0x0A, 0x20, 0x2F, 0x7A }, 7);
            s_encodingUtil_UTF7.GetByteCountTest("A\t\r\n /z", 0, 7, 7);
            s_encodingUtil_UTF7.GetBytesTest("!}", 0, 2, -1, 0, new Byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D }, 8);
            s_encodingUtil_UTF7.GetByteCountTest("!}", 0, 2, 8);
            s_encodingUtil_UTF7.GetBytesTest(true, false, "!}", 0, 2, -1, 0, new Byte[] { 0x21, 0x7D }, 2);
            s_encodingUtil_UTF7.GetByteCountTest(true, false, "!}", 0, 2, 2);
            s_encodingUtil_UTF7.GetBytesTest("", 0, 1, -1, 0, new Byte[] { 0x2B, 0x41, 0x41, 0x77, 0x2D }, 5);
            s_encodingUtil_UTF7.GetBytesTest("\u212B", 0, 1, -1, 0, new Byte[] { 0x2B, 0x49, 0x53, 0x73, 0x2D }, 5);
            s_encodingUtil_UTF7.GetBytesTest("!}", 1, 1, -1, 0, new Byte[] { 0x2B, 0x41, 0x48, 0x30, 0x2D }, 5);
            s_encodingUtil_UTF7.GetByteCountTest("!}", 1, 1, 5);
            s_encodingUtil_UTF7.GetBytesTest("\u0E59\u05D1", 0, 2, -1, 0, new Byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 8);
            s_encodingUtil_UTF7.GetByteCountTest("\u0E59\u05D1", 0, 2, 8);
            s_encodingUtil_UTF7.GetBytesTest(true, false, "\u0E59\u05D1", 0, 2, -1, 0, new Byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 8);
            s_encodingUtil_UTF7.GetByteCountTest(true, false, "\u0E59\u05D1", 0, 2, 8);
            s_encodingUtil_UTF7.GetBytesTest("\u0041\u0021\u007D\u0009\u0E59\u05D1", 0, 6, -1, 0, new Byte[] { 0x41, 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 18);
            s_encodingUtil_UTF7.GetByteCountTest("\u0041\u0021\u007D\u0009\u0E59\u05D1", 0, 6, 18);
            s_encodingUtil_UTF7.GetBytesTest(true, false, "\u0041\u0021\u007D\u0009\u0E59\u05D1", 0, 6, -1, 0, new Byte[] { 0x41, 0x21, 0x7D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 12);
            s_encodingUtil_UTF7.GetByteCountTest(true, false, "\u0041\u0021\u007D\u0009\u0E59\u05D1", 0, 6, 12);
            s_encodingUtil_UTF7.GetBytesTest("\uD800", 0, 1, -1, 0, new Byte[] { 0x2B, 0x32, 0x41, 0x41, 0x2D }, 5);
            s_encodingUtil_UTF7.GetBytesTest("\uDFFF", 0, 1, -1, 0, new Byte[] { 0x2B, 0x33, 0x2F, 0x38, 0x2D }, 5);
            s_encodingUtil_UTF7.GetBytesTest("\uD800\uDFFF", 0, 2, -1, 0, new Byte[] { 0x2B, 0x32, 0x41, 0x44, 0x66, 0x2F, 0x77, 0x2D }, 8);
            s_encodingUtil_UTF7.GetBytesTest("+-", 0, 2, -1, 0, new Byte[] { 0x2B, 0x2D, 0x2D }, 3);
            s_encodingUtil_UTF7.GetByteCountTest("+-", 0, 2, 3);
            s_encodingUtil_UTF7.GetBytesTest("-", 0, 1, -1, 0, new Byte[] { 0x2D }, 1);
            s_encodingUtil_UTF7.GetBytesTest("+-", 0, 2, -1, 0, new Byte[] { 0x2B, 0x2D, 0x2D }, 3);
        }

        [Fact]
        public static void MaxCharCount()
        {
            s_encodingUtil_UTF7.GetMaxCharCountTest(0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxCharCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxCharCountTest(-2147483648, 0));
            s_encodingUtil_UTF7.GetMaxCharCountTest(2147483647, 2147483647);
            s_encodingUtil_UTF7.GetMaxCharCountTest(10, 10);
        }

        [Fact]
        public static void MaxByteCount()
        {
            s_encodingUtil_UTF7.GetMaxByteCountTest(0, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxByteCountTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxByteCountTest(-2147483648, 0));
            s_encodingUtil_UTF7.GetMaxByteCountTest(268435455, 805306367);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxByteCountTest(2147483647, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxByteCountTest(1717986918, 0));
            s_encodingUtil_UTF7.GetMaxByteCountTest(10, 32);
            s_encodingUtil_UTF7.GetMaxByteCountTest(715827881, 2147483645);
            Assert.Throws<ArgumentOutOfRangeException>(() => s_encodingUtil_UTF7.GetMaxByteCountTest(805306367, 0));
        }

        [Fact]
        public static void Preamble()
        {
            s_encodingUtil_UTF7.GetPreambleTest(new Byte[] { });
        }
    }
}