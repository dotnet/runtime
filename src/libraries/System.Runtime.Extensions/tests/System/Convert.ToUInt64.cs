// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToUInt64Tests : ConvertTestBase<UInt64>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            UInt64[] expectedValues = { 1, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            UInt64[] expectedValues = { Byte.MaxValue, Byte.MinValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
            UInt64[] expectedValues = { Char.MaxValue, Char.MinValue, 98 };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 1000m, 0m };
            UInt64[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MinValue, Decimal.MaxValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 1000.0, 0.0 };
            UInt64[] expectedValues = { (UInt64)1000, (UInt64)0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, -100.0 };
            VerifyThrows<OverflowException, Double>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { 1000, 0, Int16.MaxValue };
            UInt64[] expectedValues = { 1000, 0, (UInt64)Int16.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            Int16[] overflowValues = { Int16.MinValue };
            VerifyThrows<OverflowException, Int16>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { 1000, 0, Int32.MaxValue };
            UInt64[] expectedValues = { 1000, 0, Int32.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MinValue };
            VerifyThrows<OverflowException, Int32>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 1000, 0, Int64.MaxValue };
            UInt64[] expectedValues = { 1000, 0, Int64.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MinValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            UInt64[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToUInt64, Convert.ToUInt64, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToUInt64, Convert.ToUInt64, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, 0 };
            UInt64[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            SByte[] overflowValues = { SByte.MinValue };
            VerifyThrows<OverflowException, SByte>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 1000.0f, 0.0f };
            UInt64[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, -100.0f };
            VerifyThrows<OverflowException, Single>(Convert.ToUInt64, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), UInt64.MaxValue.ToString(), "9223372036854775807", "9223372036854775808", "9223372036854775809", null };
            UInt64[] expectedValues = { 1000, 0, UInt16.MaxValue, UInt32.MaxValue, UInt64.MaxValue, Int64.MaxValue, (UInt64)Int64.MaxValue + 1, (UInt64)Int64.MaxValue + 2, 0 };
            VerifyFromString(Convert.ToUInt64, Convert.ToUInt64, testValues, expectedValues);

            String[] overflowValues = { "-1", Decimal.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToUInt64, Convert.ToUInt64, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToUInt64, Convert.ToUInt64, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "ffffffffffffffff", "18446744073709551615", "1777777777777777777777", "1111111111111111111111111111111111111111111111111111111111111111", "0", "0", "0", "0", "9223372036854775807", "9223372036854775808" /*VSWhidbey #526568*/, "9223372036854775809", "9223372036854775810", "9223372036854775811" };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2, 10, 10, 10, 10, 10 };
            UInt64[] expectedValues = { 0, 0, 0, 0, UInt64.MaxValue, UInt64.MaxValue, UInt64.MaxValue, UInt64.MaxValue, UInt64.MinValue, UInt64.MinValue, UInt64.MinValue, UInt64.MinValue, (UInt64)Int64.MaxValue, (UInt64)Int64.MaxValue + 1 /*VSWhidbey #526568*/, (UInt64)Int64.MaxValue + 2, (UInt64)Int64.MaxValue + 3, (UInt64)Int64.MaxValue + 4 };
            VerifyFromStringWithBase(Convert.ToUInt64, testValues, testBases, expectedValues);

            String[] overflowValues = { "18446744073709551616", "18446744073709551617", "18446744073709551618", "18446744073709551619", "18446744073709551620", "-4294967297", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777" };
            Int32[] overflowBases = { 10, 10, 10, 10, 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToUInt64, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToUInt64, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToUInt64, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 100, 0 };
            UInt64[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt32.MinValue, UInt32.MaxValue };
            UInt64[] expectedValues = { UInt32.MinValue, UInt32.MaxValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue };
            UInt64[] expectedValues = { UInt64.MaxValue, UInt64.MinValue };
            Verify(Convert.ToUInt64, testValues, expectedValues);
        }
    }
}
