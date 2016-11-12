// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using Xunit;

namespace System.Tests
{
    public static partial class BitConverterTests
    {
        [Fact]
        public static unsafe void IsLittleEndian()
        {
            short s = 1;
            Assert.Equal(BitConverter.IsLittleEndian, *((byte*)&s) == 1);
        }

        [Fact]
        public static void ValueArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToBoolean(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToChar(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToDouble(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToInt16(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToInt32(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToInt64(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToSingle(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt16(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt32(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt64(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToString(null));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToString(null, 0));
            Assert.Throws<ArgumentNullException>("value", () => BitConverter.ToString(null, 0, 0));
        }

        [Fact]
        public static void StartIndexBeyondLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[1], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[1], 1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[1], 2));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToChar(new byte[2], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToChar(new byte[2], 2));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToChar(new byte[2], 3));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToDouble(new byte[8], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToDouble(new byte[8], 8));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToDouble(new byte[8], 9));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt16(new byte[2], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt16(new byte[2], 2));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt16(new byte[2], 3));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt32(new byte[4], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt32(new byte[4], 4));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt32(new byte[4], 5));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt64(new byte[8], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt64(new byte[8], 8));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt64(new byte[8], 9));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToSingle(new byte[4], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToSingle(new byte[4], 4));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToSingle(new byte[4], 5));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt16(new byte[2], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt16(new byte[2], 2));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt16(new byte[2], 3));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt32(new byte[4], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt32(new byte[4], 4));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt32(new byte[4], 5));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt64(new byte[8], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt64(new byte[8], 8));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt64(new byte[8], 9));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], -1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 1));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 2));

            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 2, 0));

            Assert.Throws<ArgumentOutOfRangeException>("length", () => BitConverter.ToString(new byte[1], 0, -1));
        }

        [Fact]
        public static void StartIndexPlusNeededLengthTooLong()
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[0], 0));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToChar(new byte[2], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToDouble(new byte[8], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToInt16(new byte[2], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToInt32(new byte[4], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToInt64(new byte[8], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToSingle(new byte[4], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToUInt16(new byte[2], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToUInt32(new byte[4], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToUInt64(new byte[8], 1));
            Assert.Throws<ArgumentException>("value", () => BitConverter.ToString(new byte[2], 1, 2));
        }

        [Fact]
        public static void DoubleToInt64Bits()
        {
            Double input = 123456.3234;
            Int64 result = BitConverter.DoubleToInt64Bits(input);
            Assert.Equal(4683220267154373240L, result);
            Double roundtripped = BitConverter.Int64BitsToDouble(result);
            Assert.Equal(input, roundtripped);
        }

        [Fact]
        public static void RoundtripBoolean()
        {
            Byte[] bytes = BitConverter.GetBytes(true);
            Assert.Equal(1, bytes.Length);
            Assert.Equal(1, bytes[0]);
            Assert.True(BitConverter.ToBoolean(bytes, 0));

            bytes = BitConverter.GetBytes(false);
            Assert.Equal(1, bytes.Length);
            Assert.Equal(0, bytes[0]);
            Assert.False(BitConverter.ToBoolean(bytes, 0));
        }

        [Fact]
        public static void RoundtripChar()
        {
            Char input = 'A';
            Byte[] expected = { 0x41, 0 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToChar, input, expected);
        }

        [Fact]
        public static void RoundtripDouble()
        {
            Double input = 123456.3234;
            Byte[] expected = { 0x78, 0x7a, 0xa5, 0x2c, 0x05, 0x24, 0xfe, 0x40 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToDouble, input, expected);
        }

        [Fact]
        public static void RoundtripSingle()
        {
            Single input = 8392.34f;
            Byte[] expected = { 0x5c, 0x21, 0x03, 0x46 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToSingle, input, expected);
        }

        [Fact]
        public static void RoundtripInt16()
        {
            Int16 input = 0x1234;
            Byte[] expected = { 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt16, input, expected);
        }

        [Fact]
        public static void RoundtripInt32()
        {
            Int32 input = 0x12345678;
            Byte[] expected = { 0x78, 0x56, 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt32, input, expected);
        }

        [Fact]
        public static void RoundtripInt64()
        {
            Int64 input = 0x0123456789abcdef;
            Byte[] expected = { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt64, input, expected);
        }

        [Fact]
        public static void RoundtripUInt16()
        {
            UInt16 input = 0x1234;
            Byte[] expected = { 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt16, input, expected);
        }

        [Fact]
        public static void RoundtripUInt32()
        {
            UInt32 input = 0x12345678;
            Byte[] expected = { 0x78, 0x56, 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt32, input, expected);
        }

        [Fact]
        public static void RoundtripUInt64()
        {
            UInt64 input = 0x0123456789abcdef;
            Byte[] expected = { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt64, input, expected);
        }

        [Fact]
        public static void RoundtripString()
        {
            Byte[] bytes = { 0x12, 0x34, 0x56, 0x78, 0x9a };

            Assert.Equal("12-34-56-78-9A", BitConverter.ToString(bytes));
            Assert.Equal("56-78-9A", BitConverter.ToString(bytes, 2));
            Assert.Equal("56", BitConverter.ToString(bytes, 2, 1));

            Assert.Same(string.Empty, BitConverter.ToString(new byte[0]));
            Assert.Same(string.Empty, BitConverter.ToString(new byte[3], 1, 0));
        }

        [Fact]
        public static void ToString_ByteArray_Long()
        {
            byte[] bytes = Enumerable.Range(0, 256 * 4).Select(i => (byte)i).ToArray();

            string expected = string.Join("-", bytes.Select(b => b.ToString("X2")));

            Assert.Equal(expected, BitConverter.ToString(bytes));
            Assert.Equal(expected.Substring(3, expected.Length - 6), BitConverter.ToString(bytes, 1, bytes.Length - 2));
        }

        [Fact]
        public static void ToString_ByteArrayTooLong_Throws()
        {
            byte[] arr;
            try
            {
                arr = new byte[int.MaxValue / 3 + 1];
            }
            catch (OutOfMemoryException)
            {
                // Exit out of the test if we don't have an enough contiguous memory
                // available to create a big enough array.
                return;
            }

            Assert.Throws<ArgumentOutOfRangeException>("length", () => BitConverter.ToString(arr));
        }

        private static void VerifyRoundtrip<TInput>(Func<TInput, Byte[]> getBytes, Func<Byte[], int, TInput> convertBack, TInput input, Byte[] expectedBytes)
        {
            Byte[] bytes = getBytes(input);
            Assert.Equal(expectedBytes.Length, bytes.Length);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(expectedBytes);
            }

            Assert.Equal(expectedBytes, bytes);
            Assert.Equal(input, convertBack(bytes, 0));

            // Also try unaligned startIndex
            byte[] longerBytes = new byte[bytes.Length + 1];
            longerBytes[0] = 0;
            Array.Copy(bytes, 0, longerBytes, 1, bytes.Length);
            Assert.Equal(input, convertBack(longerBytes, 1));
        }
    }
}
