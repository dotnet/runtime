// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToInt64Tests : ConvertTestBase<Int64>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Int64[] expectedValues = { 1, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Int64[] expectedValues = { 255, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
            Int64[] expectedValues = { Char.MaxValue, Char.MinValue, 98 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 100m, -100m, 0m };
            Int64[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MaxValue, Decimal.MinValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToInt64, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 100.0, -100.0, 0 };
            Int64[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, Double.MinValue };
            VerifyThrows<OverflowException, Double>(Convert.ToInt64, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { 100, -100, 0, };
            Int64[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Int64[] expectedValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { Int64.MaxValue, Int64.MinValue, 0 };
            Int64[] expectedValues = { Int64.MaxValue, Int64.MinValue, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Int64[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToInt64, Convert.ToInt64, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToInt64, Convert.ToInt64, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, -100, 0 };
            Int64[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 100.0f, -100.0f, 0.0f, };
            Int64[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt64, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, Single.MinValue };
            VerifyThrows<OverflowException, Single>(Convert.ToInt64, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "100", "-100", "0", Int64.MinValue.ToString(), Int64.MaxValue.ToString(), null };
            Int64[] expectedValues = { 100, -100, 0, Int64.MinValue, Int64.MaxValue, 0 };
            VerifyFromString(Convert.ToInt64, Convert.ToInt64, testValues, expectedValues);

            String[] overflowValues = { "1" + Int64.MaxValue.ToString(), Int64.MinValue.ToString() + "1" };
            VerifyFromStringThrows<OverflowException>(Convert.ToInt64, Convert.ToInt64, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToInt64, Convert.ToInt64, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "7FFFFFFFFFFFFFFF", "9223372036854775807", "777777777777777777777", "111111111111111111111111111111111111111111111111111111111111111", "8000000000000000", "-9223372036854775808", "1000000000000000000000", "1000000000000000000000000000000000000000000000000000000000000000" };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            Int64[] expectedValues = { 0, 0, 0, 0, Int64.MaxValue, Int64.MaxValue, Int64.MaxValue, Int64.MaxValue, Int64.MinValue, Int64.MinValue, Int64.MinValue, Int64.MinValue };
            VerifyFromStringWithBase(Convert.ToInt64, testValues, testBases, expectedValues);

            String[] overflowValues = { "9223372036854775808", "-9223372036854775809", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777" };
            Int32[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToInt64, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToInt64, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToInt64, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 100, 0 };
            Int64[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { 100, 0 };
            Int64[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 100, 0 };
            Int64[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt64, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToInt64, overflowValues);
        }
    }
}
