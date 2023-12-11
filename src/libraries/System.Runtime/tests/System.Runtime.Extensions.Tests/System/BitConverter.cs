// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.Tests
{
    public static class BitConverterTests
    {
        [Fact]
        public static unsafe void IsLittleEndian()
        {
            short s = 1;
            Assert.Equal(BitConverter.IsLittleEndian, *((byte*)&s) == 1);
        }

        [Fact]
        public static unsafe void IsLittleEndianReflection()
        {
            bool value = (bool)typeof(BitConverter).GetField("IsLittleEndian").GetValue(null);
            Assert.Equal(BitConverter.IsLittleEndian, value);
        }

        [Fact]
        public static void ValueArgumentNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToBoolean(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToChar(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToDouble(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToHalf(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToInt16(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToInt32(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToInt64(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToInt128(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToSingle(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt16(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt32(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt64(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToUInt128(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToString(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => BitConverter.ToString(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", null /* param name varies in .NET Framework */, () => BitConverter.ToString(null, 0, 0));
        }

        [Fact]
        public static void StartIndexBeyondLength()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[1], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[1], 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[1], 2));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToChar(new byte[2], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToChar(new byte[2], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToChar(new byte[2], 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToDouble(new byte[8], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToDouble(new byte[8], 8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToDouble(new byte[8], 9));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToHalf(new byte[2], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToHalf(new byte[2], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToHalf(new byte[2], 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt16(new byte[2], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt16(new byte[2], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt16(new byte[2], 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt32(new byte[4], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt32(new byte[4], 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt32(new byte[4], 5));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt64(new byte[8], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt64(new byte[8], 8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt64(new byte[8], 9));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt128(new byte[16], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt128(new byte[16], 16));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToInt128(new byte[16], 17));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToSingle(new byte[4], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToSingle(new byte[4], 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToSingle(new byte[4], 5));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt16(new byte[2], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt16(new byte[2], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt16(new byte[2], 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt32(new byte[4], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt32(new byte[4], 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt32(new byte[4], 5));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt64(new byte[8], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt64(new byte[8], 8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt64(new byte[8], 9));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt128(new byte[16], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt128(new byte[16], 16));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToUInt128(new byte[16], 17));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 2));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToString(new byte[1], 2, 0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => BitConverter.ToString(new byte[1], 0, -1));
        }

        [Fact]
        public static void StartIndexPlusNeededLengthTooLong()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => BitConverter.ToBoolean(new byte[0], 0));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToChar(new byte[2], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToDouble(new byte[8], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToHalf(new byte[2], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToInt16(new byte[2], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToInt32(new byte[4], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToInt64(new byte[8], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToInt128(new byte[16], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToSingle(new byte[4], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToUInt16(new byte[2], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToUInt32(new byte[4], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToUInt64(new byte[8], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToUInt128(new byte[16], 1));
            AssertExtensions.Throws<ArgumentException>("value", null, () => BitConverter.ToString(new byte[2], 1, 2));
        }

        [Fact]
        public static void DoubleToInt64Bits()
        {
            double input = 123456.3234;
            long result = BitConverter.DoubleToInt64Bits(input);
            Assert.Equal(4683220267154373240L, result);
            double roundtripped = BitConverter.Int64BitsToDouble(result);
            Assert.Equal(input, roundtripped);
        }

        [Fact]
        public static void RoundtripBoolean()
        {
            byte[] bytes = BitConverter.GetBytes(true);
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
            char input = 'A';
            byte[] expected = { 0x41, 0 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToChar, input, expected);
        }

        [Fact]
        public static void RoundtripDouble()
        {
            double input = 123456.3234;
            byte[] expected = { 0x78, 0x7a, 0xa5, 0x2c, 0x05, 0x24, 0xfe, 0x40 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToDouble, input, expected);
        }

        [Fact]
        public static void RoundtripSingle()
        {
            float input = 8392.34f;
            byte[] expected = { 0x5c, 0x21, 0x03, 0x46 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToSingle, input, expected);
        }

        [Fact]
        public static void RoundtripHalf()
        {
            Half input = (Half)123.44;
            byte[] expected = { 0xb7, 0x57 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToHalf, input, expected);
        }

        [Fact]
        public static void RoundtripInt16()
        {
            short input = 0x1234;
            byte[] expected = { 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt16, input, expected);
        }

        [Fact]
        public static void RoundtripInt32()
        {
            int input = 0x12345678;
            byte[] expected = { 0x78, 0x56, 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt32, input, expected);
        }

        [Fact]
        public static void RoundtripInt64()
        {
            long input = 0x0123456789abcdef;
            byte[] expected = { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt64, input, expected);
        }

        [Fact]
        public static void RoundtripInt128()
        {
            Int128 input = new Int128(0x0123456789abcdef, 0xfedcba9876543210);
            byte[] expected = { 0x10, 0x32, 0x54, 0x76, 0x98, 0xba, 0xdc, 0xfe, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToInt128, input, expected);
        }

        [Fact]
        public static void RoundtripUInt16()
        {
            ushort input = 0x1234;
            byte[] expected = { 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt16, input, expected);
        }

        [Fact]
        public static void RoundtripUInt32()
        {
            uint input = 0x12345678;
            byte[] expected = { 0x78, 0x56, 0x34, 0x12 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt32, input, expected);
        }

        [Fact]
        public static void RoundtripUInt64()
        {
            ulong input = 0x0123456789abcdef;
            byte[] expected = { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt64, input, expected);
        }

        [Fact]
        public static void RoundtripUInt128()
        {
            UInt128 input = new UInt128(0x0123456789abcdef, 0xfedcba9876543210);
            byte[] expected = { 0x10, 0x32, 0x54, 0x76, 0x98, 0xba, 0xdc, 0xfe, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
            VerifyRoundtrip(BitConverter.GetBytes, BitConverter.ToUInt128, input, expected);
        }

        [Fact]
        public static void RoundtripString()
        {
            byte[] bytes = { 0x12, 0x34, 0x56, 0x78, 0x9a };

            Assert.Equal("12-34-56-78-9A", BitConverter.ToString(bytes));
            Assert.Equal("56-78-9A", BitConverter.ToString(bytes, 2));
            Assert.Equal("56", BitConverter.ToString(bytes, 2, 1));

            Assert.Same(string.Empty, BitConverter.ToString(new byte[0]));
            Assert.Same(string.Empty, BitConverter.ToString(new byte[3], 1, 0));
        }

        [Fact]
        public static void ToString_ByteArray_Long()
        {
            byte[] bytes = Enumerable.Range(0, 256 * 4).Select(i => unchecked((byte)i)).ToArray();

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

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => BitConverter.ToString(arr));
        }

        private static void VerifyRoundtrip<TInput>(Func<TInput, byte[]> getBytes, Func<byte[], int, TInput> convertBack, TInput input, byte[] expectedBytes)
        {
            byte[] bytes = getBytes(input);
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

        [Fact]
        public static void SingleToInt32Bits()
        {
            float input = 12345.63f;
            int result = BitConverter.SingleToInt32Bits(input);
            Assert.Equal(1178658437, result);
            float roundtripped = BitConverter.Int32BitsToSingle(result);
            Assert.Equal(input, roundtripped);
        }

        [Fact]
        public static void HalfToInt16Bits()
        {
            Half input = (Half)12.34;
            short result = BitConverter.HalfToInt16Bits(input);
            Assert.Equal((short)18988, result);
            Half roundtripped = BitConverter.Int16BitsToHalf(result);
            Assert.Equal(input, roundtripped);
        }

        [Fact]
        public static void DoubleToUInt64Bits()
        {
            double input = 123456.3234;
            ulong result = BitConverter.DoubleToUInt64Bits(input);
            Assert.Equal(4683220267154373240UL, result);
            double roundtripped = BitConverter.UInt64BitsToDouble(result);
            Assert.Equal(input, roundtripped);
        }

        [Fact]
        public static void SingleToUInt32Bits()
        {
            float input = 12345.63f;
            uint result = BitConverter.SingleToUInt32Bits(input);
            Assert.Equal(1178658437U, result);
            float roundtripped = BitConverter.UInt32BitsToSingle(result);
            Assert.Equal(input, roundtripped);
        }

        [Fact]
        public static void HalfToUInt16Bits()
        {
            Half input = (Half)12.34;
            ushort result = BitConverter.HalfToUInt16Bits(input);
            Assert.Equal((ushort)18988, result);
            Half roundtripped = BitConverter.UInt16BitsToHalf(result);
            Assert.Equal(input, roundtripped);
        }
    }
}
