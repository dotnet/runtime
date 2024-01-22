// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

#pragma warning disable xUnit1013 // https://github.com/xunit/xunit/issues/1973

namespace System.Tests
{
    public abstract class BitConverterBase
    {
        [Theory]
        [InlineData(true, new byte[] { 0x01 })]
        [InlineData(false, new byte[] { 0x00 })]
        public abstract void ConvertFromBool(bool boolean, byte[] expected);

        [Theory]
        [InlineData((short)0, new byte[] { 0x00, 0x00 })]
        [InlineData((short)-15, new byte[] { 0xF1, 0xFF })]
        [InlineData((short)15, new byte[] { 0x0F, 0x00 })]
        [InlineData((short)10000, new byte[] { 0x10, 0x27 })]
        [InlineData((short)-10000, new byte[] { 0xF0, 0xD8 })]
        [InlineData(short.MinValue, new byte[] { 0x00, 0x80 })]
        [InlineData(short.MaxValue, new byte[] { 0xFF, 0x7F })]
        public abstract void ConvertFromShort(short num, byte[] expected);

        [Theory]
        [InlineData('A', new byte[] { 0x41, 0x00 })]
        [InlineData('*', new byte[] { 0x2A, 0x00 })]
        [InlineData('3', new byte[] { 0x33, 0x00 })]
        [InlineData('[', new byte[] { 0x5B, 0x00 })]
        [InlineData('a', new byte[] { 0x61, 0x00 })]
        [InlineData('{', new byte[] { 0x7B, 0x00 })]
        [InlineData('\0', new byte[] { 0x00, 0x00 })]
        [InlineData(' ', new byte[] { 0x20, 0x00 })]
        [InlineData('\u263a', new byte[] { 0x3A, 0x26 })] // Smiley Face
        public abstract void ConvertFromChar(char character, byte[] expected);

        [Theory]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(15, new byte[] { 0x0F, 0x00, 0x00, 0x00 })]
        [InlineData(-15, new byte[] { 0xF1, 0xFF, 0xFF, 0xFF })]
        [InlineData(1048576, new byte[] { 0x00, 0x00, 0x10, 0x00 })]
        [InlineData(-1048576, new byte[] { 0x00, 0x00, 0xF0, 0xFF })]
        [InlineData(1000000000, new byte[] { 0x00, 0xCA, 0x9A, 0x3B })]
        [InlineData(-1000000000, new byte[] { 0x00, 0x36, 0x65, 0xC4 })]
        [InlineData(int.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
        [InlineData(int.MinValue, new byte[] { 0x00, 0x00, 0x00, 0x80 })]
        public abstract void ConvertFromInt(int num, byte[] expected);

        [Theory]
        [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(16777215, new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(-16777215, new byte[] { 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(1000000000, new byte[] { 0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(-1000000000, new byte[] { 0x00, 0x36, 0x65, 0xC4, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(4294967296, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 })]
        [InlineData(-4294967296, new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(187649984473770, new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0x00, 0x00 })]
        [InlineData(-187649984473770, new byte[] { 0x56, 0x55, 0x55, 0x55, 0x55, 0x55, 0xFF, 0xFF })]
        [InlineData(1000000000000000000, new byte[] { 0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6, 0xE0, 0x0D })]
        [InlineData(-1000000000000000000, new byte[] { 0x00, 0x00, 0x9C, 0x58, 0x4C, 0x49, 0x1F, 0xF2 })]
        [InlineData(long.MinValue, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 })]
        [InlineData(long.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F })]
        public abstract void ConvertFromLong(long num, byte[] expected);

        public static IEnumerable<object[]> ConvertFromInt128TestData()
        {
            yield return new object[] { (Int128)(0), new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (Int128)(+16777215), new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (Int128)(-16777215), new byte[] { 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
            yield return new object[] { (Int128)(+1000000000), new byte[] { 0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (Int128)(-1000000000), new byte[] { 0x00, 0x36, 0x65, 0xC4, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
            yield return new object[] { (Int128)(+4294967296), new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (Int128)(-4294967296), new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
            yield return new object[] { (Int128)(+187649984473770), new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (Int128)(-187649984473770), new byte[] { 0x56, 0x55, 0x55, 0x55, 0x55, 0x55, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
            yield return new object[] { (Int128)(+1000000000000000000), new byte[] { 0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6, 0xE0, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (Int128)(-1000000000000000000), new byte[] { 0x00, 0x00, 0x9C, 0x58, 0x4C, 0x49, 0x1F, 0xF2, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
            yield return new object[] { Int128.MinValue, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 } };
            yield return new object[] { Int128.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F } };
        }

        [Theory]
        [MemberData(nameof(ConvertFromInt128TestData))]
        public abstract void ConvertFromInt128(Int128 num, byte[] expected);

        [Theory]
        [InlineData((ushort)0, new byte[] { 0x00, 0x00 })]
        [InlineData((ushort)15, new byte[] { 0x0F, 0x00 })]
        [InlineData((ushort)1023, new byte[] { 0xFF, 0x03 })]
        [InlineData((ushort)10000, new byte[] { 0x10, 0x27 })]
        [InlineData((ushort)short.MaxValue, new byte[] { 0xFF, 0x7F })]
        [InlineData(ushort.MaxValue, new byte[] { 0xFF, 0xFF })]
        public abstract void ConvertFromUShort(ushort num, byte[] expected);

        [Theory]
        [InlineData((uint)0, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
        [InlineData((uint)15, new byte[] { 0x0F, 0x00, 0x00, 0x00 })]
        [InlineData((uint)1023, new byte[] { 0xFF, 0x03, 0x00, 0x00 })]
        [InlineData((uint)1048576, new byte[] { 0x00, 0x00, 0x10, 0x00 })]
        [InlineData((uint)1000000000, new byte[] { 0x00, 0xCA, 0x9A, 0x3B })]
        [InlineData(int.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
        [InlineData(uint.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
        public abstract void ConvertFromUInt(uint num, byte[] expected);

        [Theory]
        [InlineData((ulong)0xFFFFFF, new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData((ulong)1000000000, new byte[] { 0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData((ulong)0x100000000, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 })]
        [InlineData((ulong)0xAAAAAAAAAAAA, new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0x00, 0x00 })]
        [InlineData((ulong)1000000000000000000, new byte[] { 0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6, 0xE0, 0x0D })]
        [InlineData(10000000000000000000, new byte[] { 0x00, 0x00, 0xE8, 0x89, 0x04, 0x23, 0xC7, 0x8A })]
        [InlineData((ulong)0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(long.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F })]
        [InlineData(ulong.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
        public abstract void ConvertFromULong(ulong num, byte[] expected);

        public static IEnumerable<object[]> ConvertFromUInt128TestData()
        {
            yield return new object[] { (UInt128)(0xFFFFFFu), new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(1000000000u), new byte[] { 0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(0x100000000u), new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(0xAAAAAAAAAAAAu), new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(1000000000000000000u), new byte[] { 0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6, 0xE0, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(10000000000000000000), new byte[] { 0x00, 0x00, 0xE8, 0x89, 0x04, 0x23, 0xC7, 0x8A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(0u), new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } };
            yield return new object[] { (UInt128)(Int128.MaxValue), new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F } };
            yield return new object[] { UInt128.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
        }

        [Theory]
        [MemberData(nameof(ConvertFromUInt128TestData))]
        public abstract void ConvertFromUInt128(UInt128 num, byte[] expected);

        public static IEnumerable<object[]> ConvertFromHalfTestData()
        {
            yield return new object[] { (Half)0.0, new byte[] { 0x00, 0x00 } };
            yield return new object[] { (Half)123.44, new byte[] { 0xB7, 0x57 } };
            yield return new object[] { Half.MinValue, new byte[] { 0xFF, 0xFB } };
            yield return new object[] { Half.MaxValue, new byte[] { 0xFF, 0x7B } };
            yield return new object[] { Half.Epsilon, new byte[] { 0x01, 0x00 } };
            yield return new object[] { Half.NaN, new byte[] { 0x00, 0xFE } };
            yield return new object[] { Half.PositiveInfinity, new byte[] { 0x00, 0x7C } };
            yield return new object[] { Half.NegativeInfinity, new byte[] { 0x00, 0xFC } };
        }

        [Theory]
        [MemberData(nameof(ConvertFromHalfTestData))]
        public abstract void ConvertFromHalf(Half num, byte[] expected);

        [Theory]
        [InlineData(0.0F, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(1.0F, new byte[] { 0x00, 0x00, 0x80, 0x3F })]
        [InlineData(15.0F, new byte[] { 0x00, 0x00, 0x70, 0x41 })]
        [InlineData(65535.0F, new byte[] { 0x00, 0xFF, 0x7F, 0x47 })]
        [InlineData(0.00390625F, new byte[] { 0x00, 0x00, 0x80, 0x3B })]
        [InlineData(0.00000000023283064365386962890625F, new byte[] { 0x00, 0x00, 0x80, 0x2F })]
        [InlineData(1.2345E-35F, new byte[] { 0x49, 0x46, 0x83, 0x05 })]
        [InlineData(1.2345671F, new byte[] { 0x4B, 0x06, 0x9E, 0x3F })]
        [InlineData(1.2345673F, new byte[] { 0x4D, 0x06, 0x9E, 0x3F })]
        [InlineData(1.2345677F, new byte[] { 0x50, 0x06, 0x9E, 0x3F })]
        [InlineData(1.23456789E+35F, new byte[] { 0x1E, 0x37, 0xBE, 0x79 })]
        [InlineData(float.MinValue, new byte[] { 0xFF, 0xFF, 0x7F, 0xFF })]
        [InlineData(float.MaxValue, new byte[] { 0xFF, 0xFF, 0x7F, 0x7F })]
        [InlineData(float.Epsilon, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
        [InlineData(float.NaN, new byte[] { 0x00, 0x00, 0xC0, 0xFF })]
        [InlineData(float.NegativeInfinity, new byte[] { 0x00, 0x00, 0x80, 0xFF })]
        [InlineData(float.PositiveInfinity, new byte[] { 0x00, 0x00, 0x80, 0x7F })]
        public abstract void ConvertFromFloat(float num, byte[] expected);

        [Theory]
        [InlineData(0.0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(1.0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F })]
        [InlineData(255.0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x6F, 0x40 })]
        [InlineData(4294967295.0, new byte[] { 0x00, 0x00, 0xE0, 0xFF, 0xFF, 0xFF, 0xEF, 0x41 })]
        [InlineData(0.00390625, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x3F })]
        [InlineData(0.00000000023283064365386962890625, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3D })]
        [InlineData(1.23456789012345E-300, new byte[] { 0xDF, 0x88, 0x1E, 0x1C, 0xFE, 0x74, 0xAA, 0x01 })]
        [InlineData(1.2345678901234565, new byte[] { 0xFA, 0x59, 0x8C, 0x42, 0xCA, 0xC0, 0xF3, 0x3F })]
        [InlineData(1.2345678901234567, new byte[] { 0xFB, 0x59, 0x8C, 0x42, 0xCA, 0xC0, 0xF3, 0x3F })]
        [InlineData(1.2345678901234569, new byte[] { 0xFC, 0x59, 0x8C, 0x42, 0xCA, 0xC0, 0xF3, 0x3F })]
        [InlineData(1.23456789012345678E+300, new byte[] { 0x52, 0xD3, 0xBB, 0xBC, 0xE8, 0x7E, 0x3D, 0x7E })]
        [InlineData(double.MinValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0xFF })]
        [InlineData(double.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F })]
        [InlineData(double.Epsilon, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(double.NaN, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0xFF })]
        [InlineData(double.NegativeInfinity, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0xFF })]
        [InlineData(double.PositiveInfinity, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x7F })]
        public abstract void ConvertFromDouble(double num, byte[] expected);

        private static byte[] s_toCharByteArray = { 32, 0, 0, 42, 0, 65, 0, 125, 0, 197, 0, 168, 3, 41, 4, 172, 32, 0x3A, 0x26 };

        public static IEnumerable<object[]> ToCharTestData()
        {
            yield return new object[] { 0, ' ', s_toCharByteArray };
            yield return new object[] { 1, '\0', s_toCharByteArray };
            yield return new object[] { 3, '*', s_toCharByteArray };
            yield return new object[] { 5, 'A', s_toCharByteArray };
            yield return new object[] { 7, '}', s_toCharByteArray };
            yield return new object[] { 9, '\u00C5', s_toCharByteArray }; // Latin capital letter A with ring above (\u00C5)
            yield return new object[] { 11, '\u03A8', s_toCharByteArray }; // Greek capital letter Psi (\u03A8)
            yield return new object[] { 13, '\u0429', s_toCharByteArray }; // Cyrillic capital letter Shcha (\u0429)
            yield return new object[] { 15, '\u20AC', s_toCharByteArray }; // Euro sign (\u20AC)
            yield return new object[] { 17, '\u263A', s_toCharByteArray }; // Smiley Face (\u263A)
        }

        [Theory]
        [MemberData(nameof(ToCharTestData))]
        public abstract void ToChar(int index, char expected, byte[] byteArray);

        private static byte[] s_toInt16ByteArray = { 15, 0, 0, 128, 16, 39, 240, 216, 241, 255, 127 };

        public static IEnumerable<object[]> ToInt16TestData()
        {
            yield return new object[] { 1, (short)0, s_toInt16ByteArray };
            yield return new object[] { 0, (short)15, s_toInt16ByteArray };
            yield return new object[] { 8, (short)-15, s_toInt16ByteArray };
            yield return new object[] { 4, (short)10000, s_toInt16ByteArray };
            yield return new object[] { 6, (short)-10000, s_toInt16ByteArray };
            yield return new object[] { 9, (short)32767, s_toInt16ByteArray };
            yield return new object[] { 2, (short)-32768, s_toInt16ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToInt16TestData))]
        public abstract void ToInt16(int index, short expected, byte[] byteArray);

        [Theory]
        [InlineData(0x00EC, new byte[] { 0xEC, 0x00, 0x00, 0x00 })]
        [InlineData(0x3FFFFFFF, new byte[] { 0xFF, 0xFF, 0xFF, 0x3F })]
        [InlineData(0x1000, new byte[] { 0x00, 0x10, 0x00, 0x00 })]
        public abstract void ToInt32(int expected, byte[] byteArray);

        private static byte[] s_toInt64ByteArray = {
            0x00, 0x36, 0x65, 0xC4, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF,

            0xFF, 0xFF, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x56, 0x55, 0x55,
            0x55, 0x55, 0x55, 0xFF, 0xFF, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0xAA, 0x00, 0x00, 0x64, 0xA7, 0xB3,
            0xB6, 0xE0, 0x0D, 0x00, 0x00, 0x9C, 0x58, 0x4C,
            0x49, 0x1F, 0xF2
        };

        public static IEnumerable<object[]> ToInt64TestData()
        {
            yield return new object[] { 8, (long)0, s_toInt64ByteArray };
            yield return new object[] { 5, (long)16777215, s_toInt64ByteArray };
            yield return new object[] { 34, (long)-16777215, s_toInt64ByteArray };
            yield return new object[] { 17, (long)1000000000, s_toInt64ByteArray };
            yield return new object[] { 0, (long)-1000000000, s_toInt64ByteArray };
            yield return new object[] { 21, 4294967296, s_toInt64ByteArray };
            yield return new object[] { 26, -4294967296, s_toInt64ByteArray };
            yield return new object[] { 53, 187649984473770, s_toInt64ByteArray };
            yield return new object[] { 45, -187649984473770, s_toInt64ByteArray };
            yield return new object[] { 59, 1000000000000000000, s_toInt64ByteArray };
            yield return new object[] { 67, -1000000000000000000, s_toInt64ByteArray };
            yield return new object[] { 37, 9223372036854775807, s_toInt64ByteArray };
            yield return new object[] { 9, -9223372036854775808, s_toInt64ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToInt64TestData))]
        public abstract void ToInt64(int index, long expected, byte[] byteArray);

        private static byte[] s_toInt128ByteArray = {
            0x00, 0x36, 0x65, 0xC4, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x56, 0x55, 0x55, 0x55,
            0x55, 0x55, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6,
            0xE0, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x9C, 0x58, 0x4C, 0x49, 0x1F, 0xF2,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        public static IEnumerable<object[]> ToInt128TestData()
        {
            yield return new object[] { 16, (Int128)(0), s_toInt128ByteArray };
            yield return new object[] { 13, (Int128)(+16777215), s_toInt128ByteArray };
            yield return new object[] { 73, (Int128)(-16777215), s_toInt128ByteArray };
            yield return new object[] { 33, (Int128)(+1000000000), s_toInt128ByteArray };
            yield return new object[] { 0, (Int128)(-1000000000), s_toInt128ByteArray };
            yield return new object[] { 45, (Int128)(+4294967296), s_toInt128ByteArray };
            yield return new object[] { 57, (Int128)(-4294967296), s_toInt128ByteArray };
            yield return new object[] { 108,(Int128)(+187649984473770), s_toInt128ByteArray };
            yield return new object[] { 92, (Int128)(-187649984473770), s_toInt128ByteArray };
            yield return new object[] { 122, (Int128)(+1000000000000000000), s_toInt128ByteArray };
            yield return new object[] { 136, (Int128)(-1000000000000000000), s_toInt128ByteArray };
            yield return new object[] { 76, Int128.MaxValue, s_toInt128ByteArray };
            yield return new object[] { 17, Int128.MinValue, s_toInt128ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToInt128TestData))]
        public abstract void ToInt128(int index, Int128 expected, byte[] byteArray);

        private static byte[] s_toUInt16ByteArray = { 15, 0, 0, 255, 3, 16, 39, 255, 255, 127 };

        public static IEnumerable<object[]> ToUInt16TestData()
        {
            yield return new object[] { 1, (ushort)0, s_toUInt16ByteArray };
            yield return new object[] { 0, (ushort)15, s_toUInt16ByteArray };
            yield return new object[] { 3, (ushort)1023, s_toUInt16ByteArray };
            yield return new object[] { 5, (ushort)10000, s_toUInt16ByteArray };
            yield return new object[] { 8, (ushort)32767, s_toUInt16ByteArray };
            yield return new object[] { 7, (ushort)65535, s_toUInt16ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToUInt16TestData))]
        public abstract void ToUInt16(int index, ushort expected, byte[] byteArray);

        private static byte[] s_toUInt32ByteArray = { 15, 0, 0, 0, 0, 16, 0, 255, 3, 0, 0, 202, 154, 59, 255, 255, 255, 255, 127 };

        public static IEnumerable<object[]> ToUInt32TestData()
        {
            yield return new object[] { 1, (uint)0, s_toUInt32ByteArray };
            yield return new object[] { 0, (uint)15, s_toUInt32ByteArray };
            yield return new object[] { 7, (uint)1023, s_toUInt32ByteArray };
            yield return new object[] { 3, (uint)1048576, s_toUInt32ByteArray };
            yield return new object[] { 10, (uint)1000000000, s_toUInt32ByteArray };
            yield return new object[] { 15, (uint)2147483647, s_toUInt32ByteArray };
            yield return new object[] { 14, 4294967295, s_toUInt32ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToUInt32TestData))]
        public abstract void ToUInt32(int index, uint expected, byte[] byteArray);

        private static byte[] s_toUInt64ByteArray = {
            0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x64,
            0xa7, 0xb3, 0xb6, 0xe0, 0x0d, 0x00, 0xca, 0x9a,
            0x3b, 0x00, 0x00, 0x00, 0x00, 0xaa, 0xaa, 0xaa,
            0xaa, 0xaa, 0xaa, 0x00, 0x00, 0xe8, 0x89, 0x04,
            0x23, 0xc7, 0x8a, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0x7f
        };

        public static IEnumerable<object[]> ToUInt64TestData()
        {
            yield return new object[] { 3, (ulong)0, s_toUInt64ByteArray };
            yield return new object[] { 0, (ulong)16777215, s_toUInt64ByteArray };
            yield return new object[] { 21, (ulong)1000000000, s_toUInt64ByteArray };
            yield return new object[] { 7, (ulong)4294967296, s_toUInt64ByteArray };
            yield return new object[] { 29, (ulong)187649984473770, s_toUInt64ByteArray };
            yield return new object[] { 13, (ulong)1000000000000000000, s_toUInt64ByteArray };
            yield return new object[] { 35, 10000000000000000000, s_toUInt64ByteArray };
            yield return new object[] { 44, (ulong)9223372036854775807, s_toUInt64ByteArray };
            yield return new object[] { 43, 18446744073709551615, s_toUInt64ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToUInt64TestData))]
        public abstract void ToUInt64(int index, ulong expected, byte[] byteArray);

        private static byte[] s_toUInt128ByteArray = {
            0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64,
            0xA7, 0xB3, 0xB6, 0xE0, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCA, 0x9A, 0x3B,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAA, 0xAA, 0xAA, 0xAA,
            0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE8, 0x89, 0x04, 0x23,
            0xC7, 0x8A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F
        };

        public static IEnumerable<object[]> ToUInt128TestData()
        {
            yield return new object[] { 3, (UInt128)(0), s_toUInt128ByteArray };
            yield return new object[] { 0, (UInt128)(16777215), s_toUInt128ByteArray };
            yield return new object[] { 44, (UInt128)(1000000000), s_toUInt128ByteArray };
            yield return new object[] { 15, (UInt128)(4294967296), s_toUInt128ByteArray };
            yield return new object[] { 60, (UInt128)(187649984473770), s_toUInt128ByteArray };
            yield return new object[] { 29, (UInt128)(1000000000000000000), s_toUInt128ByteArray };
            yield return new object[] { 74, (UInt128)(10000000000000000000), s_toUInt128ByteArray };
            yield return new object[] { 91, (UInt128)(Int128.MaxValue), s_toUInt128ByteArray };
            yield return new object[] { 90, UInt128.MaxValue, s_toUInt128ByteArray };
        }

        [Theory]
        [MemberData(nameof(ToUInt128TestData))]
        public abstract void ToUInt128(int index, UInt128 expected, byte[] byteArray);

        private static byte[] s_toHalfByteArray =
            { 0x00, 0x00, 0xB7, 0x57, 0xFF, 0xFB, 0xFF, 0x7B, 0x01, 0x00, 0x00, 0xFE, 0x00, 0x7C, 0x00, 0xFC };

        public static IEnumerable<object[]> ToHalfTestData()
        {
            yield return new object[] { 0, (Half)0.0, s_toHalfByteArray };
            yield return new object[] { 2, (Half)123.44, s_toHalfByteArray };
            yield return new object[] { 4, Half.MinValue, s_toHalfByteArray };
            yield return new object[] { 6, Half.MaxValue, s_toHalfByteArray };
            yield return new object[] { 8, Half.Epsilon, s_toHalfByteArray };
            yield return new object[] { 10, Half.NaN, s_toHalfByteArray };
            yield return new object[] { 12, Half.PositiveInfinity, s_toHalfByteArray };
            yield return new object[] { 14, Half.NegativeInfinity, s_toHalfByteArray };
        }

        [Theory]
        [MemberData(nameof(ToHalfTestData))]
        public abstract void ToHalf(int index, Half expected, byte[] byteArray);

        private static byte[] s_toSingleByteArray =
            { 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x70, 0x41, 0x00, 0xFF, 0x7F, 0x47, 0x00, 0x00, 0x80, 0x3B, 0x00, 0x00,
            0x80, 0x2F, 0x49, 0x46, 0x83, 0x05, 0x4B, 0x06, 0x9E, 0x3F, 0x4D, 0x06, 0x9E, 0x3F, 0x50, 0x06, 0x9E, 0x3F, 0x1E, 0x37,
            0xBE, 0x79, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0x7F, 0x7F, 0x01, 0x00, 0x00, 0x00, 0xC0, 0xFF, 0x00, 0x00, 0x80, 0xFF, 0x00,
            0x00, 0x80, 0x7F };

        public static IEnumerable<object[]> ToSingleTestData()
        {
            yield return new object[] { 0, 0.0000000E+000, s_toSingleByteArray };
            yield return new object[] { 2, 1.0000000E+000, s_toSingleByteArray };
            yield return new object[] { 6, 1.5000000E+001, s_toSingleByteArray };
            yield return new object[] { 10, 6.5535000E+004, s_toSingleByteArray };
            yield return new object[] { 14, 3.9062500E-003, s_toSingleByteArray };
            yield return new object[] { 18, 2.3283064E-010, s_toSingleByteArray };
            yield return new object[] { 22, 1.2345000E-035, s_toSingleByteArray };
            yield return new object[] { 26, 1.2345671E+000, s_toSingleByteArray };
            yield return new object[] { 30, 1.2345673E+000, s_toSingleByteArray };
            yield return new object[] { 34, 1.2345676E+000, s_toSingleByteArray };
            yield return new object[] { 38, 1.2345679E+035, s_toSingleByteArray };
            yield return new object[] { 42, -3.4028235E+038, s_toSingleByteArray };
            yield return new object[] { 45, 3.4028235E+038, s_toSingleByteArray };
            yield return new object[] { 49, 1.4012985E-045, s_toSingleByteArray };
            yield return new object[] { 51, float.NaN, s_toSingleByteArray };
            yield return new object[] { 55, float.NegativeInfinity, s_toSingleByteArray };
            yield return new object[] { 59, float.PositiveInfinity, s_toSingleByteArray };
        }

        [Theory]
        [MemberData(nameof(ToSingleTestData))]
        public abstract void ToSingle(int index, float expected, byte[] byteArray);

        private static byte[] s_toDoubleByteArray =
            { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x6F, 0x40, 0x00, 0x00,
            0xE0, 0xFF, 0xFF, 0xFF, 0xEF, 0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x70, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xF0, 0x3D, 0xDF, 0x88, 0x1E, 0x1C, 0xFE, 0x74, 0xAA, 0x01, 0xFA, 0x59, 0x8C, 0x42, 0xCA, 0xC0, 0xF3, 0x3F, 0xFB, 0x59,
            0x8C, 0x42, 0xCA, 0xC0, 0xF3, 0x3F, 0xFC, 0x59, 0x8C, 0x42, 0xCA, 0xC0, 0xF3, 0x3F, 0x52, 0xD3, 0xBB, 0xBC, 0xE8, 0x7E,
            0x3D, 0x7E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xF0, 0x7F };

        public static IEnumerable<object[]> ToDoubleTestData()
        {
            yield return new object[] { 0, 0.0000000000000000E+000, s_toDoubleByteArray };
            yield return new object[] { 2, 1.0000000000000000E+000, s_toDoubleByteArray };
            yield return new object[] { 10, 2.5500000000000000E+002, s_toDoubleByteArray };
            yield return new object[] { 18, 4.2949672950000000E+009, s_toDoubleByteArray };
            yield return new object[] { 26, 3.9062500000000000E-003, s_toDoubleByteArray };
            yield return new object[] { 34, 2.3283064365386963E-010, s_toDoubleByteArray };
            yield return new object[] { 42, 1.2345678901234500E-300, s_toDoubleByteArray };
            yield return new object[] { 50, 1.2345678901234565E+000, s_toDoubleByteArray };
            yield return new object[] { 58, 1.2345678901234567E+000, s_toDoubleByteArray };
            yield return new object[] { 66, 1.2345678901234569E+000, s_toDoubleByteArray };
            yield return new object[] { 74, 1.2345678901234569E+300, s_toDoubleByteArray };
            yield return new object[] { 82, -1.7976931348623157E+308, s_toDoubleByteArray };
            yield return new object[] { 89, 1.7976931348623157E+308, s_toDoubleByteArray };
            yield return new object[] { 97, 4.9406564584124654E-324, s_toDoubleByteArray };
            yield return new object[] { 99, double.NaN, s_toDoubleByteArray };
            yield return new object[] { 107, double.NegativeInfinity, s_toDoubleByteArray };
            yield return new object[] { 115, double.PositiveInfinity, s_toDoubleByteArray };
        }

        [Theory]
        [MemberData(nameof(ToDoubleTestData))]
        public abstract void ToDouble(int index, double expected, byte[] byteArray);

        private static byte[] s_toBooleanByteArray = { 0, 1, 2, 4, 8, 16, 32, 64, 128, 255 };

        public static IEnumerable<object[]> ToBooleanTestData()
        {
            yield return new object[] { 0, false, s_toBooleanByteArray };
            yield return new object[] { 1, true, s_toBooleanByteArray };
            yield return new object[] { 2, true, s_toBooleanByteArray };
            yield return new object[] { 3, true, s_toBooleanByteArray };
            yield return new object[] { 4, true, s_toBooleanByteArray };
            yield return new object[] { 5, true, s_toBooleanByteArray };
            yield return new object[] { 6, true, s_toBooleanByteArray };
            yield return new object[] { 7, true, s_toBooleanByteArray };
            yield return new object[] { 8, true, s_toBooleanByteArray };
            yield return new object[] { 9, true, s_toBooleanByteArray };
        }

        [Theory]
        [MemberData(nameof(ToBooleanTestData))]
        public abstract void ToBoolean(int index, bool expected, byte[] byteArray);
    }
}
