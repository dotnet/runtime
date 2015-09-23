// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Globalization.Extensions.Tests
{
    public class IdnMappingThrows
    {
        [Fact]
        public static void GetAsciiThrows()
        {
            IdnMapping idnMapping = new IdnMapping();

            Assert.Throws<ArgumentNullException>("unicode", () => idnMapping.GetAscii(null, -5));
            Assert.Throws<ArgumentNullException>("unicode", () => idnMapping.GetAscii(null, -5, -10));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => idnMapping.GetAscii("abc", -5, -10));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => idnMapping.GetAscii("abc", 10, -10));
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => idnMapping.GetAscii("abc", 4, 99));
            Assert.Throws<ArgumentOutOfRangeException>("unicode", () => idnMapping.GetAscii("abc", 2, 2));
            Assert.Throws<ArgumentException>("unicode", () => idnMapping.GetAscii("abc", 3, 0));
        }

        [Fact]
        public static void GetUnicodeThrows()
        {
            IdnMapping idnMapping = new IdnMapping();

            Assert.Throws<ArgumentNullException>("ascii", () => idnMapping.GetUnicode(null, -5));
            Assert.Throws<ArgumentNullException>("ascii", () => idnMapping.GetUnicode(null, -5, -10));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => idnMapping.GetUnicode("abc", -5, -10));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => idnMapping.GetUnicode("abc", 10, -10));
            Assert.Throws<ArgumentOutOfRangeException>("byteIndex", () => idnMapping.GetUnicode("abc", 4, 99));
            Assert.Throws<ArgumentOutOfRangeException>("ascii", () => idnMapping.GetUnicode("abc", 2, 2));
            Assert.Throws<ArgumentException>("ascii", () => idnMapping.GetUnicode("abc", 3, 0));
        }
    }
}

