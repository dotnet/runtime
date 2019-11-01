// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        [Fact]
        public static void BytesProperty_FromData()
        {
            Utf8String ustr = u8("\U00000012\U00000123\U00001234\U00101234\U00000012\U00000123\U00001234\U00101234");

            Assert.Equal(new byte[]
            {
                0x12,
                0xC4, 0xA3,
                0xE1, 0x88, 0xB4,
                0xF4, 0x81, 0x88, 0xB4,
                0x12,
                0xC4, 0xA3,
                0xE1, 0x88, 0xB4,
                0xF4, 0x81, 0x88, 0xB4,
            }, ustr.Bytes);
        }

        [Fact]
        public static void BytesProperty_FromEmpty()
        {
            Assert.False(Utf8String.Empty.Bytes.GetEnumerator().MoveNext());
        }

        [Fact]
        public static void CharsProperty_FromData()
        {
            Utf8String ustr = u8("\U00000012\U00000123\U00001234\U00101234\U00000012\U00000123\U00001234\U00101234");

            Assert.Equal(new char[]
            {
                '\u0012',
                '\u0123',
                '\u1234',
                '\uDBC4', '\uDE34',
                '\u0012',
                '\u0123',
                '\u1234',
                '\uDBC4', '\uDE34',
            }, ustr.Chars);
        }

        [Fact]
        public static void CharsProperty_FromEmpty()
        {
            Assert.False(Utf8String.Empty.Chars.GetEnumerator().MoveNext());
        }

        [Fact]
        public static void RunesProperty_FromData()
        {
            Utf8String ustr = u8("\U00000012\U00000123\U00001234\U00101234\U00000012\U00000123\U00001234\U00101234");

            Assert.Equal(new Rune[]
            {
                new Rune(0x0012),
                new Rune(0x0123),
                new Rune(0x1234),
                new Rune(0x101234),
                new Rune(0x0012),
                new Rune(0x0123),
                new Rune(0x1234),
                new Rune(0x101234),
            }, ustr.Runes);
        }

        [Fact]
        public static void RunesProperty_FromEmpty()
        {
            Assert.False(Utf8String.Empty.Runes.GetEnumerator().MoveNext());
        }
    }
}
