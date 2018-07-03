// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToUInt32Tests : ConvertTestBase<uint>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            uint[] expectedValues = { 1, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            uint[] expectedValues = { 255, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { char.MinValue, char.MaxValue, 'b' };
            uint[] expectedValues = { char.MinValue, char.MaxValue, 98 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 1000m, 0m };
            uint[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MaxValue, decimal.MinValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 1000.0, 0.0, -0.5, 4294967295.49999, 472.2, 472.6, 472.5, 471.5 };
            uint[] expectedValues = { 1000, 0, 0, 4294967295, 472, 473, 472, 472 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, -0.500000000001, -100.0, 4294967296, 4294967295.5 };
            VerifyThrows<OverflowException, double>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { 1000, 0, short.MaxValue };
            uint[] expectedValues = { 1000, 0, (uint)short.MaxValue };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            short[] overflowValues = { short.MinValue };
            VerifyThrows<OverflowException, short>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { 1000, 0, int.MaxValue };
            uint[] expectedValues = { 1000, 0, int.MaxValue };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            int[] overflowValues = { int.MinValue };
            VerifyThrows<OverflowException, int>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 1000, 0 };
            uint[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            long[] overflowValues = { long.MaxValue, long.MinValue };
            VerifyThrows<OverflowException, long>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            uint[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToUInt32, Convert.ToUInt32, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToUInt32, Convert.ToUInt32, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, 0 };
            uint[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            sbyte[] overflowValues = { sbyte.MinValue };
            VerifyThrows<OverflowException, sbyte>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 1000.0f, 0.0f };
            uint[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, -100.0f };
            VerifyThrows<OverflowException, float>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), Int32.MaxValue.ToString(), "2147483648", "2147483649", null };
            uint[] expectedValues = { 1000, 0, ushort.MaxValue, uint.MaxValue, int.MaxValue, (uint)int.MaxValue + 1, (uint)int.MaxValue + 2, 0 };
            VerifyFromString(Convert.ToUInt32, Convert.ToUInt32, testValues, expectedValues);

            string[] overflowValues = { "-1", Decimal.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToUInt32, Convert.ToUInt32, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToUInt32, Convert.ToUInt32, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "ffffffff", "4294967295", "37777777777", "11111111111111111111111111111111", "0", "0", "0", "0", "2147483647", "2147483648", "2147483649" };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2, 10, 10, 10 };
            uint[] expectedValues = { 0, 0, 0, 0, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, (uint)int.MaxValue, (uint)int.MaxValue + 1, (uint)int.MaxValue + 2 };
            VerifyFromStringWithBase(Convert.ToUInt32, testValues, testBases, expectedValues);

            string[] overflowValues = { "18446744073709551616", "18446744073709551617", "18446744073709551618", "18446744073709551619", "18446744073709551620", "-4294967297", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777" };
            int[] overflowBases = { 10, 10, 10, 10, 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToUInt32, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToUInt32, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToUInt32, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 100, 0 };
            uint[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { uint.MaxValue, uint.MinValue };
            uint[] expectedValues = { uint.MaxValue, uint.MinValue };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 100, 0 };
            uint[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            ulong[] values = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToUInt32, values);
        }
    }
}
