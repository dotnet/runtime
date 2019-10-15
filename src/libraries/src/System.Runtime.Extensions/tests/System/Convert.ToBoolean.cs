// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToBooleanTests : ConvertTestBase<bool>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            Verify(Convert.ToBoolean, testValues, testValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MinValue, byte.MaxValue, };
            bool[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { decimal.MaxValue, decimal.MinValue, decimal.One, decimal.Zero, 0m, 0.0m, 1.5m, -1.5m, 500.00m };
            bool[] expectedValues = { true, true, true, false, false, false, true, true, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0d, 0.0, 1.5, -1.5, 1.5e300, -1.7e-500, -1.7e300, -1.7e-320 };
            bool[] expectedValues = { true, true, true, true, true, true, false, false, true, true, true, false, true, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { short.MinValue, short.MaxValue, 0 };
            bool[] expectedValues = { true, true, false };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { int.MinValue, int.MaxValue, 0 };
            bool[] expectedValues = { true, true, false };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { long.MinValue, long.MaxValue, 0 };
            bool[] expectedValues = { true, true, false, };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { null, "True", "true ", " true", " true ", " false ", " false", "false ", "False" };
            bool[] expectedValues = { false, true, true, true, true, false, false, false, false };
            VerifyFromString(Convert.ToBoolean, Convert.ToBoolean, testValues, expectedValues);

            string[] invalidValues = { "Hello" };
            VerifyFromStringThrows<FormatException>(Convert.ToBoolean, Convert.ToBoolean, invalidValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            bool[] expectedValues = { false };
            VerifyFromObject(Convert.ToBoolean, Convert.ToBoolean, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToBoolean, Convert.ToBoolean, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 0, sbyte.MaxValue, sbyte.MinValue };
            bool[] expectedValues = { false, true, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f, 0.0f, 1.5f, -1.5f, 1.5e30f, -1.7e-100f, -1.7e30f, -1.7e-40f, };
            bool[] expectedValues = { true, true, true, true, true, true, false, false, true, true, true, false, true, true, };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { ushort.MinValue, ushort.MaxValue };
            bool[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { uint.MinValue, uint.MaxValue };
            bool[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { ulong.MinValue, ulong.MaxValue };
            bool[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }
    }
}
