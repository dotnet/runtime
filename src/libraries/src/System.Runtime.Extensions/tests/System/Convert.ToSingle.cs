// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToSingleTests : ConvertTestBase<Single>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { false, true };
            Single[] expectedValues = { 0.0f, 1.0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Single[] expectedValues = { Byte.MaxValue, Byte.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 1000m, 0m, -1000m, Decimal.MaxValue, Decimal.MinValue };
            Single[] expectedValues = { 1000f, 0.0f, -1000f, (Single)Decimal.MaxValue, (Single)Decimal.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 1000.0, 100.0, 0.0, -100.0, -1000.0, Double.MaxValue, Double.MinValue };
            Single[] expectedValues = { 1000.0f, 100.0f, 0.0f, -100.0f, -1000.0f, Single.PositiveInfinity, Single.NegativeInfinity };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Single[] expectedValues = { Int16.MaxValue, Int16.MinValue, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Single[] expectedValues = { Int32.MaxValue, Int32.MinValue, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { Int64.MaxValue, Int64.MinValue, 0 };
            Single[] expectedValues = { Int64.MaxValue, Int64.MinValue, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Single[] expectedValues = { 0f };
            VerifyFromObject(Convert.ToSingle, Convert.ToSingle, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToSingle, Convert.ToSingle, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, -100, 0 };
            Single[] expectedValues = { 100f, -100f, 0f };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { Single.MaxValue, Single.MinValue, new Single(), Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon };
            Single[] expectedValues = { Single.MaxValue, Single.MinValue, new Single(), Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { Single.MaxValue.ToString("R"), (0f).ToString(), Single.MinValue.ToString("R"), null };
            Single[] expectedValues = { Single.MaxValue, 0f, Single.MinValue, 0f };
            VerifyFromString(Convert.ToSingle, Convert.ToSingle, testValues, expectedValues);

            String[] overflowValues = { Double.MinValue.ToString(), Double.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToSingle, Convert.ToSingle, overflowValues);

            String[] formatExceptionValues = { "1f2d" };
            VerifyFromStringThrows<FormatException>(Convert.ToSingle, Convert.ToSingle, formatExceptionValues);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue, };
            Single[] expectedValues = { UInt16.MaxValue, UInt16.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue };
            Single[] expectedValues = { UInt32.MaxValue, UInt32.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue };
            Single[] expectedValues = { UInt64.MaxValue, UInt64.MinValue };
            Verify(Convert.ToSingle, testValues, expectedValues);
        }
    }
}
