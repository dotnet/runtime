// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToDoubleTests : ConvertTestBase<double>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            double[] expectedValues = { 1.0, 0.0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            double[] expectedValues = { byte.MaxValue, byte.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { decimal.MaxValue, decimal.MinValue, 0.0m };
            double[] expectedValues = { (double)decimal.MaxValue, (double)decimal.MinValue, 0.0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { double.MaxValue, double.MinValue, double.NegativeInfinity, double.PositiveInfinity, double.Epsilon };
            double[] expectedValues = { double.MaxValue, double.MinValue, double.NegativeInfinity, double.PositiveInfinity, double.Epsilon };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { short.MaxValue, short.MinValue, 0 };
            double[] expectedValues = { short.MaxValue, short.MinValue, 0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { int.MaxValue, int.MinValue, 0 };
            double[] expectedValues = { int.MaxValue, int.MinValue, 0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { long.MaxValue, long.MinValue, 0 };
            double[] expectedValues = { (double)long.MaxValue, (double)long.MinValue, 0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            double[] expectedValues = { 0.0 };
            VerifyFromObject(Convert.ToDouble, Convert.ToDouble, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToDouble, Convert.ToDouble, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { sbyte.MaxValue, sbyte.MinValue };
            double[] expectedValues = { sbyte.MaxValue, sbyte.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { float.MaxValue, float.MinValue, 0.0f };
            double[] expectedValues = { float.MaxValue, float.MinValue, 0.0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { Double.MinValue.ToString("R"), Double.MaxValue.ToString("R"), (0.0).ToString(), (10.0).ToString(), (-10.0).ToString(), null };
            double[] expectedValues = { double.MinValue, double.MaxValue, 0.0, 10.0, -10.0, 0.0 };
            VerifyFromString(Convert.ToDouble, Convert.ToDouble, testValues, expectedValues);

            string[] formatExceptionValues = { "123xyz" };
            VerifyFromStringThrows<FormatException>(Convert.ToDouble, Convert.ToDouble, formatExceptionValues);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void FromString_NetFramework()
        {
            string[] overflowValues = { Double.MaxValue.ToString(), Double.MinValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToDouble, Convert.ToDouble, overflowValues);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromString_NotNetFramework()
        {
            string[] overflowValues = { Double.MaxValue.ToString(), Double.MinValue.ToString() };
            VerifyFromString(Convert.ToDouble, Convert.ToDouble, overflowValues, new double[] { 1.7976931348623157E+308, -1.7976931348623157E+308 });
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { ushort.MaxValue, ushort.MinValue };
            double[] expectedValues = { ushort.MaxValue, ushort.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { uint.MaxValue, uint.MinValue };
            double[] expectedValues = { uint.MaxValue, uint.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { ulong.MaxValue, ulong.MinValue };
            double[] expectedValues = { (double)ulong.MaxValue, (double)ulong.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }
    }
}
