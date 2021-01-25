// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ConvertToSingleTests : ConvertTestBase<float>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { false, true };
            float[] expectedValues = { 0.0f, 1.0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            float[] expectedValues = { byte.MaxValue, byte.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 1000m, 0m, -1000m, decimal.MaxValue, decimal.MinValue };
            float[] expectedValues = { 1000f, 0.0f, -1000f, (float)decimal.MaxValue, (float)decimal.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 1000.0, 100.0, 0.0, -100.0, -1000.0, double.MaxValue, double.MinValue };
            float[] expectedValues = { 1000.0f, 100.0f, 0.0f, -100.0f, -1000.0f, float.PositiveInfinity, float.NegativeInfinity };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { short.MaxValue, short.MinValue, 0 };
            float[] expectedValues = { short.MaxValue, short.MinValue, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { int.MaxValue, int.MinValue, 0 };
            float[] expectedValues = { int.MaxValue, int.MinValue, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { long.MaxValue, long.MinValue, 0 };
            float[] expectedValues = { long.MaxValue, long.MinValue, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            float[] expectedValues = { 0f };
            VerifyFromObject(Convert.ToSingle, Convert.ToSingle, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToSingle, Convert.ToSingle, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, -100, 0 };
            float[] expectedValues = { 100f, -100f, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { float.MaxValue, float.MinValue, new float(), float.NegativeInfinity, float.PositiveInfinity, float.Epsilon };
            float[] expectedValues = { float.MaxValue, float.MinValue, new float(), float.NegativeInfinity, float.PositiveInfinity, float.Epsilon };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { Single.MaxValue.ToString("R"), (0f).ToString(), Single.MinValue.ToString("R"), null };
            float[] expectedValues = { float.MaxValue, 0f, float.MinValue, 0f };
            VerifyFromString(Convert.ToSingle, Convert.ToSingle, testValues, expectedValues);

            string[] formatExceptionValues = { "1f2d" };
            VerifyFromStringThrows<FormatException>(Convert.ToSingle, Convert.ToSingle, formatExceptionValues);
        }

        [Fact]
        public void FromString_NotNetFramework()
        {
            string[] overflowValues = { Double.MinValue.ToString(), Double.MaxValue.ToString() };
            VerifyFromString(Convert.ToSingle, Convert.ToSingle, overflowValues, new float[] { float.NegativeInfinity, float.PositiveInfinity });
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { ushort.MaxValue, ushort.MinValue, };
            float[] expectedValues = { ushort.MaxValue, ushort.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { uint.MaxValue, uint.MinValue };
            float[] expectedValues = { uint.MaxValue, uint.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { ulong.MaxValue, ulong.MinValue };
            float[] expectedValues = { ulong.MaxValue, ulong.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }
    }
}
