// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToDoubleTests : ConvertTestBase<Double>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Double[] expectedValues = { 1.0, 0.0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Double[] expectedValues = { Byte.MaxValue, Byte.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { Decimal.MaxValue, Decimal.MinValue, 0.0m };
            Double[] expectedValues = { (Double)Decimal.MaxValue, (Double)Decimal.MinValue, 0.0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { Double.MaxValue, Double.MinValue, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon };
            Double[] expectedValues = { Double.MaxValue, Double.MinValue, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Double[] expectedValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Double[] expectedValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { Int64.MaxValue, Int64.MinValue, 0 };
            Double[] expectedValues = { (Double)Int64.MaxValue, (Double)Int64.MinValue, 0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Double[] expectedValues = { 0.0 };
            VerifyFromObject(Convert.ToDouble, Convert.ToDouble, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToDouble, Convert.ToDouble, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { SByte.MaxValue, SByte.MinValue };
            Double[] expectedValues = { SByte.MaxValue, SByte.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { Single.MaxValue, Single.MinValue, 0.0f };
            Double[] expectedValues = { Single.MaxValue, Single.MinValue, 0.0 };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { Double.MinValue.ToString("R"), Double.MaxValue.ToString("R"), (0.0).ToString(), (10.0).ToString(), (-10.0).ToString(), null };
            Double[] expectedValues = { Double.MinValue, Double.MaxValue, 0.0, 10.0, -10.0, 0.0 };
            VerifyFromString(Convert.ToDouble, Convert.ToDouble, testValues, expectedValues);

            String[] overflowValues = { Double.MaxValue.ToString(), Double.MinValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToDouble, Convert.ToDouble, overflowValues);

            String[] formatExceptionValues = { "123xyz" };
            VerifyFromStringThrows<FormatException>(Convert.ToDouble, Convert.ToDouble, formatExceptionValues);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue };
            Double[] expectedValues = { UInt16.MaxValue, UInt16.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue };
            Double[] expectedValues = { UInt32.MaxValue, UInt32.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue };
            Double[] expectedValues = { (Double)UInt64.MaxValue, (Double)UInt64.MinValue };
            Verify(Convert.ToDouble, testValues, expectedValues);
        }
    }
}
