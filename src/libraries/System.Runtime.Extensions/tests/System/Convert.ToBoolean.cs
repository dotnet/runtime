// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToBooleanTests : ConvertTestBase<Boolean>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Verify(Convert.ToBoolean, testValues, testValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MinValue, Byte.MaxValue, };
            Boolean[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { Decimal.MaxValue, Decimal.MinValue, Decimal.One, Decimal.Zero, 0m, 0.0m, 1.5m, -1.5m, 500.00m };
            Boolean[] expectedValues = { true, true, true, false, false, false, true, true, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { Double.Epsilon, Double.MaxValue, Double.MinValue, Double.NaN, Double.NegativeInfinity, Double.PositiveInfinity, 0d, 0.0, 1.5, -1.5, 1.5e300, -1.7e-500, -1.7e300, -1.7e-320 };
            Boolean[] expectedValues = { true, true, true, true, true, true, false, false, true, true, true, false, true, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Int16.MinValue, Int16.MaxValue, 0 };
            Boolean[] expectedValues = { true, true, false };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Int32.MinValue, Int32.MaxValue, 0 };
            Boolean[] expectedValues = { true, true, false };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { Int64.MinValue, Int64.MaxValue, 0 };
            Boolean[] expectedValues = { true, true, false, };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { null, "True", "true ", " true", " true ", " false ", " false", "false ", "False" };
            Boolean[] expectedValues = { false, true, true, true, true, false, false, false, false };
            VerifyFromString(Convert.ToBoolean, Convert.ToBoolean, testValues, expectedValues);

            String[] invalidValues = { "Hello" };
            VerifyFromStringThrows<FormatException>(Convert.ToBoolean, Convert.ToBoolean, invalidValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Boolean[] expectedValues = { false };
            VerifyFromObject(Convert.ToBoolean, Convert.ToBoolean, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToBoolean, Convert.ToBoolean, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 0, SByte.MaxValue, SByte.MinValue };
            Boolean[] expectedValues = { false, true, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { Single.Epsilon, Single.MaxValue, Single.MinValue, Single.NaN, Single.NegativeInfinity, Single.PositiveInfinity, 0f, 0.0f, 1.5f, -1.5f, 1.5e30f, -1.7e-100f, -1.7e30f, -1.7e-40f, };
            Boolean[] expectedValues = { true, true, true, true, true, true, false, false, true, true, true, false, true, true, };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { UInt16.MinValue, UInt16.MaxValue };
            Boolean[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt32.MinValue, UInt32.MaxValue };
            Boolean[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { UInt64.MinValue, UInt64.MaxValue };
            Boolean[] expectedValues = { false, true };
            Verify(Convert.ToBoolean, testValues, expectedValues);
        }
    }
}
