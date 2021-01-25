// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ConvertToInt64Tests : ConvertTestBase<long>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            long[] expectedValues = { 1, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            long[] expectedValues = { 255, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { char.MaxValue, char.MinValue, 'b' };
            long[] expectedValues = { char.MaxValue, char.MinValue, 98 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 100m, -100m, 0m };
            long[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MaxValue, decimal.MinValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToInt64, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 100.0, -100.0, 0 };
            long[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, double.MinValue };
            VerifyThrows<OverflowException, double>(Convert.ToInt64, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { 100, -100, 0, };
            long[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { int.MaxValue, int.MinValue, 0 };
            long[] expectedValues = { int.MaxValue, int.MinValue, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { long.MaxValue, long.MinValue, 0 };
            long[] expectedValues = { long.MaxValue, long.MinValue, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            long[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToInt64, Convert.ToInt64, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToInt64, Convert.ToInt64, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, -100, 0 };
            long[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 100.0f, -100.0f, 0.0f, };
            long[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt64, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, float.MinValue };
            VerifyThrows<OverflowException, float>(Convert.ToInt64, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "100", "-100", "0", Int64.MinValue.ToString(), Int64.MaxValue.ToString(), null };
            long[] expectedValues = { 100, -100, 0, long.MinValue, long.MaxValue, 0 };
            VerifyFromString(Convert.ToInt64, Convert.ToInt64, testValues, expectedValues);

            string[] overflowValues = { "1" + Int64.MaxValue.ToString(), Int64.MinValue.ToString() + "1" };
            VerifyFromStringThrows<OverflowException>(Convert.ToInt64, Convert.ToInt64, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToInt64, Convert.ToInt64, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "7FFFFFFFFFFFFFFF", "9223372036854775807", "777777777777777777777", "111111111111111111111111111111111111111111111111111111111111111", "8000000000000000", "-9223372036854775808", "1000000000000000000000", "1000000000000000000000000000000000000000000000000000000000000000" };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            long[] expectedValues = { 0, 0, 0, 0, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MinValue, long.MinValue, long.MinValue, long.MinValue };
            VerifyFromStringWithBase(Convert.ToInt64, testValues, testBases, expectedValues);

            string[] overflowValues = { "9223372036854775808", "-9223372036854775809", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777" };
            int[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToInt64, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToInt64, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToInt64, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 100, 0 };
            long[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { 100, 0 };
            long[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 100, 0 };
            long[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToInt64, overflowValues);
        }
    }
}
