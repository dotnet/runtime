// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            s_encodingUtil_UTF7.GetByteCountTest("abc", 2, 1, 1);
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
    }
}
