// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            s_encodingUtil_ASCII.GetByteCountTest("abc", 2, 1, 1);
        }

        [Fact]
        public static void GetCharCount_BoundaryValues()
        {
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0x61, 0x62, 0x63 }, 2, 1, 1);
        }

        [Fact]
        public static void InvalidSequences()
        {
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0xC1 }, 0, 1, 1);
            s_encodingUtil_ASCII.GetCharsTest(new Byte[] { 0xC1, 0x41, 0xF0, 0x42 }, 0, 4, -1, 0, "?A?B", 4);
            s_encodingUtil_ASCII.GetCharCountTest(new Byte[] { 0xC1, 0x41, 0xF0, 0x42 }, 0, 4, 4);
            s_encodingUtil_ASCII.GetBytesTest("\uD800\uDC00\u0061\u0CFF", 0, 4, -1, 0, new Byte[] { 0x3F, 0x3F, 0x61, 0x3F }, 4);
            s_encodingUtil_ASCII.GetByteCountTest("\uD800\uDC00\u0061\u0CFF", 0, 4, 4);
            s_encodingUtil_ASCII.GetBytesTest("\u00FF", 0, 1, -1, 0, new Byte[] { 0x3F }, 1);
        }
        
        [Fact]
        public static void DefaultFallback()
        {
            s_encodingUtil_ASCII.GetBytesTest("\u0080\u00FF\u0B71\uFFFF\uD800\uDFFF", 0, 6, -1, 0, new Byte[] { 0x3F, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F }, 6);
        }
    }
}
