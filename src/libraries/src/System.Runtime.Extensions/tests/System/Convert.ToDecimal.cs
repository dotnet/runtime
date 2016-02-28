// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToDecimalTests : ConvertTestBase<Decimal>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Decimal[] expectedValues = { 1.0m, Decimal.Zero };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Decimal[] expectedValues = { Byte.MaxValue, Byte.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { Decimal.MaxValue, Decimal.MinValue, 0 };
            Decimal[] expectedValues = { Decimal.MaxValue, Decimal.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 1000.0, 100.0, 0.0, 0.001, -1000.0, -100.0, };
            Decimal[] expectedValues = { 1000.0m, 100.0m, 0.0m, 0.001m, -1000.0m, -100.0m };
            Verify(Convert.ToDecimal, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, Double.MinValue };
            VerifyThrows<OverflowException, Double>(Convert.ToDecimal, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Decimal[] expectedValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Decimal[] expectedValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { Int64.MaxValue, Int64.MinValue, 0 };
            Decimal[] expectedValues = { Int64.MaxValue, Int64.MinValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Decimal[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToDecimal, Convert.ToDecimal, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToDecimal, Convert.ToDecimal, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { SByte.MinValue, SByte.MaxValue, 0 };
            Decimal[] expectedValues = { SByte.MinValue, SByte.MaxValue, 0 };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 1000.0f, 100.0f, 0.0f, -1.0f, -100.0f };
            Decimal[] expectedValues = { 1000.0m, 100.0m, 0.0m, -1.0m, -100.0m };
            Verify(Convert.ToDecimal, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, Single.MinValue };
            VerifyThrows<OverflowException, Single>(Convert.ToDecimal, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { Int32.MaxValue.ToString(), Int64.MaxValue.ToString(), Decimal.MaxValue.ToString(), Decimal.MinValue.ToString(), "0", null };
            Decimal[] expectedValues = { Int32.MaxValue, Int64.MaxValue, Decimal.MaxValue, Decimal.MinValue, 0, 0 };
            VerifyFromString(Convert.ToDecimal, Convert.ToDecimal, testValues, expectedValues);

            String[] overflowValues = { "1" + Decimal.MaxValue.ToString(), Decimal.MinValue.ToString() + "1" };
            VerifyFromStringThrows<OverflowException>(Convert.ToDecimal, Convert.ToDecimal, overflowValues);

            String[] formatExceptionValues = { "100E12" };
            VerifyFromStringThrows<FormatException>(Convert.ToDecimal, Convert.ToDecimal, formatExceptionValues);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue };
            Decimal[] expectedValues = { UInt16.MaxValue, UInt16.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue };
            Decimal[] expectedValues = { UInt32.MaxValue, UInt32.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue };
            Decimal[] expectedValues = { UInt64.MaxValue, UInt64.MinValue };
            Verify(Convert.ToDecimal, testValues, expectedValues);
        }
    }
}
