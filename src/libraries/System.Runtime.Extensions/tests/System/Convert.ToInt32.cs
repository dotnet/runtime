// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ConvertToInt32Tests : ConvertTestBase<int>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            int[] expectedValues = { 1, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            int[] expectedValues = { 255, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { char.MinValue, char.MaxValue, 'b' };
            int[] expectedValues = { char.MinValue, char.MaxValue, 98 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 100m, -100m, 0m };
            int[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MaxValue, decimal.MinValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 100.0, -100.0, 0 };
            int[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, double.MinValue };
            VerifyThrows<OverflowException, double>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { 100, -100, 0, };
            int[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { int.MaxValue, int.MinValue, 0 };
            int[] expectedValues = { int.MaxValue, int.MinValue, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 100, -100, 0 };
            int[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            long[] overflowValues = { long.MaxValue, long.MinValue };
            VerifyThrows<OverflowException, long>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            int[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToInt32, Convert.ToInt32, testValues, expectedValues);

            object[] invalidValues = { new object() };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToInt32, Convert.ToInt32, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, -100, 0 };
            int[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 100.0f, -100.0f, 0.0f, };
            int[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt32, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, float.MinValue };
            VerifyThrows<OverflowException, float>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "100", "-100", "0", Int32.MinValue.ToString(), Int32.MaxValue.ToString(), null };
            int[] expectedValues = { 100, -100, 0, int.MinValue, int.MaxValue, 0 };
            VerifyFromString(Convert.ToInt32, Convert.ToInt32, testValues, expectedValues);

            string[] overflowValues = { Int64.MinValue.ToString(), Int64.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToInt32, Convert.ToInt32, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToInt32, Convert.ToInt32, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "7FFFFFFF", "2147483647", "17777777777", "1111111111111111111111111111111", "80000000", "-2147483648", "20000000000", "10000000000000000000000000000000", };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2, };
            int[] expectedValues = { 0, 0, 0, 0, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue, };
            VerifyFromStringWithBase(Convert.ToInt32, testValues, testBases, expectedValues);

            string[] overflowValues = { "2147483648", "-2147483649", "111111111111111111111111111111111", "1FFFFffff", "777777777777" };
            int[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToInt32, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToInt32, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToInt32, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 100, 0 };
            int[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { 100, 0 };
            int[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            uint[] overflowValues = { uint.MaxValue };
            VerifyThrows<OverflowException, uint>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 100, 0 };
            int[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToInt32, overflowValues);
        }
    }
}
