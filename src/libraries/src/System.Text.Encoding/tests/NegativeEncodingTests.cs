// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public static class NegativeEncodingTests
    {
        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { new UnicodeEncoding() };
            yield return new object[] { new UTF7Encoding() };
            yield return new object[] { new UTF8Encoding() };
            yield return new object[] { new ASCIIEncoding() };
        }
        
        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetByteCount_Invalid(Encoding encoding)
        {
            // Chars is null
            Assert.Throws<ArgumentNullException>(encoding is ASCIIEncoding ? "chars" : "s", () => encoding.GetByteCount((string)null));
            Assert.Throws<ArgumentNullException>("chars", () => encoding.GetByteCount((char[])null));
            Assert.Throws<ArgumentNullException>("chars", () => encoding.GetByteCount(null, 0, 0));

            // Index or count < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetByteCount(new char[3], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetByteCount(new char[3], 0, -1));

            // Index + count > chars.Length
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetByteCount(new char[3], 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetByteCount(new char[3], 1, 3));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetByteCount(new char[3], 2, 2));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetByteCount(new char[3], 3, 1));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetByteCount(new char[3], 4, 0));
        }
        
        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetBytes_Invalid(Encoding encoding)
        {
            string expectedStringParamName = encoding is ASCIIEncoding ? "chars" : "s";

            // Source is null
            Assert.Throws<ArgumentNullException>("s", () => encoding.GetBytes((string)null));
            Assert.Throws<ArgumentNullException>("chars", () => encoding.GetBytes((char[])null));
            Assert.Throws<ArgumentNullException>("chars", () => encoding.GetBytes(null, 0, 0));
            Assert.Throws<ArgumentNullException>(expectedStringParamName, () => encoding.GetBytes((string)null, 0, 0, new byte[1], 0));
            Assert.Throws<ArgumentNullException>("chars", () => encoding.GetBytes((char[])null, 0, 0, new byte[1], 0));

            // Char index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetBytes(new char[1], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("charIndex", () => encoding.GetBytes("a", -1, 0, new byte[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("charIndex", () => encoding.GetBytes(new char[1], -1, 0, new byte[1], 0));

            // Char count < 0
            Assert.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetBytes(new char[1], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => encoding.GetBytes("a", 0, -1, new byte[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => encoding.GetBytes(new char[1], 0, -1, new byte[1], 0));

            // Char index + count > source.Length
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetBytes(new char[1], 2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(expectedStringParamName, () => encoding.GetBytes("a", 2, 0, new byte[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetBytes(new char[1], 2, 0, new byte[1], 0));

            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetBytes(new char[1], 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(expectedStringParamName, () => encoding.GetBytes("a", 1, 1, new byte[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetBytes(new char[1], 1, 1, new byte[1], 0));

            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetBytes(new char[1], 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(expectedStringParamName, () => encoding.GetBytes("a", 0, 2, new byte[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => encoding.GetBytes(new char[1], 0, 2, new byte[1], 0));

            // Byte index < 0
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => encoding.GetBytes("a", 0, 1, new byte[1], -1));
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => encoding.GetBytes(new char[1], 0, 1, new byte[1], -1));

            // Byte index > bytes.Length
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => encoding.GetBytes("a", 0, 1, new byte[1], 2));
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => encoding.GetBytes(new char[1], 0, 1, new byte[1], 2));

            // Bytes does not have enough capacity to accomodate result
            Assert.Throws<ArgumentException>("bytes", () => encoding.GetBytes("abc", 0, 3, new byte[1], 0));
            Assert.Throws<ArgumentException>("bytes", () => encoding.GetBytes(new char[3], 0, 3, new byte[1], 0));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetCharCount_Invalid(Encoding encoding)
        {
            // Bytes is null
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetCharCount(null));
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetCharCount(null, 0, 0));

            // Index or count < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetCharCount(new byte[4], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetCharCount(new byte[4], 0, -1));

            // Index + count > bytes.Length
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetCharCount(new byte[4], 5, 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetCharCount(new byte[4], 4, 1));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetCharCount(new byte[4], 3, 2));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetCharCount(new byte[4], 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetCharCount(new byte[4], 1, 4));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetCharCount(new byte[4], 0, 5));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetChars_Invalid(Encoding encoding)
        {
            // Bytes is null
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetChars(null));
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetChars(null, 0, 0));
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetChars(null, 0, 0, new char[0], 0));

            // Chars is null
            Assert.Throws<ArgumentNullException>("chars", () => encoding.GetChars(new byte[4], 0, 4, null, 0));

            // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => encoding.GetChars(new byte[4], -1, 4));
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => encoding.GetChars(new byte[4], -1, 4, new char[1], 0));

            // Count < 0
            Assert.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetChars(new byte[4], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>("byteCount", () => encoding.GetChars(new byte[4], 0, -1, new char[1], 0));

            // Count > bytes.Length
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 0, 5));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 0, 5, new char[1], 0));

            // Index + count > bytes.Length
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 5, 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 5, 0, new char[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 4, 1));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 4, 1, new char[1], 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 3, 2));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetChars(new byte[4], 3, 2, new char[1], 0));

            // CharIndex < 0 or >= chars.Length
            Assert.Throws<ArgumentOutOfRangeException>("charIndex", () => encoding.GetChars(new byte[4], 0, 4, new char[1], -1));
            Assert.Throws<ArgumentOutOfRangeException>("charIndex", () => encoding.GetChars(new byte[4], 0, 4, new char[1], 2));

            // Chars does not have enough capacity to accomodate result
            Assert.Throws<ArgumentException>("chars", () => encoding.GetChars(new byte[4], 0, 4, new char[1], 1));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetMaxByteCount_Invalid(Encoding encoding)
        {
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => encoding.GetMaxByteCount(-1));
            if (!(encoding is ASCIIEncoding))
            {
                Assert.Throws<ArgumentOutOfRangeException>("charCount", () => encoding.GetMaxByteCount(int.MaxValue / 2));
            }
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => encoding.GetMaxByteCount(int.MaxValue));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetMaxCharCount_Invalid(Encoding encoding)
        {
            Assert.Throws<ArgumentOutOfRangeException>("byteCount", () => encoding.GetMaxCharCount(-1));
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public static void GetString_Invalid(Encoding encoding)
        {
            // Bytes is null
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetString(null));
            Assert.Throws<ArgumentNullException>("bytes", () => encoding.GetString(null, 0, 0));

            // Index or count < 0
            Assert.Throws<ArgumentOutOfRangeException>(encoding is ASCIIEncoding ? "byteIndex" : "index", () => encoding.GetString(new byte[1], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(encoding is ASCIIEncoding ? "byteCount" : "count", () => encoding.GetString(new byte[1], 0, -1));

            // Index + count > bytes.Length
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetString(new byte[1], 2, 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetString(new byte[1], 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => encoding.GetString(new byte[1], 0, 2));
        }
    }
}
