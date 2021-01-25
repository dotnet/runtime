// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ConvertToUInt64Tests : ConvertTestBase<ulong>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            ulong[] expectedValues = { 1, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            ulong[] expectedValues = { byte.MaxValue, byte.MinValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { char.MaxValue, char.MinValue, 'b' };
            ulong[] expectedValues = { char.MaxValue, char.MinValue, 98 };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 1000m, 0m };
            ulong[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MinValue, decimal.MaxValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 1000.0, 0.0 };
            ulong[] expectedValues = { (ulong)1000, (ulong)0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, -100.0 };
            VerifyThrows<OverflowException, double>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { 1000, 0, short.MaxValue };
            ulong[] expectedValues = { 1000, 0, (ulong)short.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            short[] overflowValues = { short.MinValue };
            VerifyThrows<OverflowException, short>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { 1000, 0, int.MaxValue };
            ulong[] expectedValues = { 1000, 0, int.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            int[] overflowValues = { int.MinValue };
            VerifyThrows<OverflowException, int>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 1000, 0, long.MaxValue };
            ulong[] expectedValues = { 1000, 0, long.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            long[] overflowValues = { long.MinValue };
            VerifyThrows<OverflowException, long>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            ulong[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToUInt64, Convert.ToUInt64, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToUInt64, Convert.ToUInt64, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, 0 };
            ulong[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            sbyte[] overflowValues = { sbyte.MinValue };
            VerifyThrows<OverflowException, sbyte>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 1000.0f, 0.0f };
            ulong[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, -100.0f };
            VerifyThrows<OverflowException, float>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), UInt64.MaxValue.ToString(), "9223372036854775807", "9223372036854775808", "9223372036854775809", null };
            ulong[] expectedValues = { 1000, 0, ushort.MaxValue, uint.MaxValue, ulong.MaxValue, long.MaxValue, (ulong)long.MaxValue + 1, (ulong)long.MaxValue + 2, 0 };
            VerifyFromString(Convert.ToUInt64, Convert.ToUInt64, testValues, expectedValues);

            string[] overflowValues = { "-1", Decimal.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToUInt64, Convert.ToUInt64, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToUInt64, Convert.ToUInt64, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "ffffffffffffffff", "18446744073709551615", "1777777777777777777777", "1111111111111111111111111111111111111111111111111111111111111111", "0", "0", "0", "0", "9223372036854775807", "9223372036854775808" /*VSWhidbey #526568*/, "9223372036854775809", "9223372036854775810", "9223372036854775811" };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2, 10, 10, 10, 10, 10 };
            ulong[] expectedValues = { 0, 0, 0, 0, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MinValue, ulong.MinValue, ulong.MinValue, ulong.MinValue, (ulong)long.MaxValue, (ulong)long.MaxValue + 1 /*VSWhidbey #526568*/, (ulong)long.MaxValue + 2, (ulong)long.MaxValue + 3, (ulong)long.MaxValue + 4 };
            VerifyFromStringWithBase(Convert.ToUInt64, testValues, testBases, expectedValues);

            string[] overflowValues = { "18446744073709551616", "18446744073709551617", "18446744073709551618", "18446744073709551619", "18446744073709551620", "-4294967297", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777" };
            int[] overflowBases = { 10, 10, 10, 10, 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToUInt64, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToUInt64, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToUInt64, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 100, 0 };
            ulong[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { uint.MinValue, uint.MaxValue };
            ulong[] expectedValues = { uint.MinValue, uint.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { ulong.MaxValue, ulong.MinValue };
            ulong[] expectedValues = { ulong.MaxValue, ulong.MinValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }
    }
}
