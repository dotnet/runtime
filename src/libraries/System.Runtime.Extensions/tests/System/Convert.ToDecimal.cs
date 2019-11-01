// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToDecimalTests : ConvertTestBase<decimal>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            decimal[] expectedValues = { 1.0m, decimal.Zero };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            decimal[] expectedValues = { byte.MaxValue, byte.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { decimal.MaxValue, decimal.MinValue, 0 };
            decimal[] expectedValues = { decimal.MaxValue, decimal.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 1000.0, 100.0, 0.0, 0.001, -1000.0, -100.0, };
            decimal[] expectedValues = { 1000.0m, 100.0m, 0.0m, 0.001m, -1000.0m, -100.0m };
            Verify(Convert.ToDecimal, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, double.MinValue };
            VerifyThrows<OverflowException, double>(Convert.ToDecimal, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { short.MaxValue, short.MinValue, 0 };
            decimal[] expectedValues = { short.MaxValue, short.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { int.MaxValue, int.MinValue, 0 };
            decimal[] expectedValues = { int.MaxValue, int.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { long.MaxValue, long.MinValue, 0 };
            decimal[] expectedValues = { long.MaxValue, long.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            decimal[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToDecimal, Convert.ToDecimal, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToDecimal, Convert.ToDecimal, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { sbyte.MinValue, sbyte.MaxValue, 0 };
            decimal[] expectedValues = { sbyte.MinValue, sbyte.MaxValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 1000.0f, 100.0f, 0.0f, -1.0f, -100.0f };
            decimal[] expectedValues = { 1000.0m, 100.0m, 0.0m, -1.0m, -100.0m };
            Verify(Convert.ToDecimal, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, float.MinValue };
            VerifyThrows<OverflowException, float>(Convert.ToDecimal, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { Int32.MaxValue.ToString(), Int64.MaxValue.ToString(), Decimal.MaxValue.ToString(), Decimal.MinValue.ToString(), "0", null };
            decimal[] expectedValues = { int.MaxValue, long.MaxValue, decimal.MaxValue, decimal.MinValue, 0, 0 };
            VerifyFromString(Convert.ToDecimal, Convert.ToDecimal, testValues, expectedValues);

            string[] overflowValues = { "1" + Decimal.MaxValue.ToString(), Decimal.MinValue.ToString() + "1" };
            VerifyFromStringThrows<OverflowException>(Convert.ToDecimal, Convert.ToDecimal, overflowValues);

            string[] formatExceptionValues = { "100E12" };
            VerifyFromStringThrows<FormatException>(Convert.ToDecimal, Convert.ToDecimal, formatExceptionValues);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { ushort.MaxValue, ushort.MinValue };
            decimal[] expectedValues = { ushort.MaxValue, ushort.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { uint.MaxValue, uint.MinValue };
            decimal[] expectedValues = { uint.MaxValue, uint.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { ulong.MaxValue, ulong.MinValue };
            decimal[] expectedValues = { ulong.MaxValue, ulong.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }
    }
}
