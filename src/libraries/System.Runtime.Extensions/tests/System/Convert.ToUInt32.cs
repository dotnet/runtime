// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToUInt32Tests : ConvertTestBase<UInt32>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            UInt32[] expectedValues = { 1, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            UInt32[] expectedValues = { 255, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { Char.MinValue, Char.MaxValue, 'b' };
            UInt32[] expectedValues = { Char.MinValue, Char.MaxValue, 98 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 1000m, 0m };
            UInt32[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MaxValue, Decimal.MinValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 1000.0, 0.0, -0.5, 4294967295.49999, 472.2, 472.6, 472.5, 471.5 };
            UInt32[] expectedValues = { 1000, 0, 0, 4294967295, 472, 473, 472, 472 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, -0.500000000001, -100.0, 4294967296, 4294967295.5 };
            VerifyThrows<OverflowException, Double>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { 1000, 0, Int16.MaxValue };
            UInt32[] expectedValues = { 1000, 0, (UInt32)Int16.MaxValue };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            Int16[] overflowValues = { Int16.MinValue };
            VerifyThrows<OverflowException, Int16>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { 1000, 0, Int32.MaxValue };
            UInt32[] expectedValues = { 1000, 0, Int32.MaxValue };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MinValue };
            VerifyThrows<OverflowException, Int32>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 1000, 0 };
            UInt32[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MaxValue, Int64.MinValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            UInt32[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToUInt32, Convert.ToUInt32, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToUInt32, Convert.ToUInt32, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, 0 };
            UInt32[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            SByte[] overflowValues = { SByte.MinValue };
            VerifyThrows<OverflowException, SByte>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 1000.0f, 0.0f };
            UInt32[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, -100.0f };
            VerifyThrows<OverflowException, Single>(Convert.ToUInt32, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), Int32.MaxValue.ToString(), "2147483648", "2147483649", null };
            UInt32[] expectedValues = { 1000, 0, UInt16.MaxValue, UInt32.MaxValue, Int32.MaxValue, (UInt32)Int32.MaxValue + 1, (UInt32)Int32.MaxValue + 2, 0 };
            VerifyFromString(Convert.ToUInt32, Convert.ToUInt32, testValues, expectedValues);

            String[] overflowValues = { "-1", Decimal.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToUInt32, Convert.ToUInt32, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToUInt32, Convert.ToUInt32, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "ffffffff", "4294967295", "37777777777", "11111111111111111111111111111111", "0", "0", "0", "0", "2147483647", "2147483648", "2147483649" };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2, 10, 10, 10 };
            UInt32[] expectedValues = { 0, 0, 0, 0, UInt32.MaxValue, UInt32.MaxValue, UInt32.MaxValue, UInt32.MaxValue, UInt32.MinValue, UInt32.MinValue, UInt32.MinValue, UInt32.MinValue, (UInt32)Int32.MaxValue, (UInt32)Int32.MaxValue + 1, (UInt32)Int32.MaxValue + 2 };
            VerifyFromStringWithBase(Convert.ToUInt32, testValues, testBases, expectedValues);

            String[] overflowValues = { "18446744073709551616", "18446744073709551617", "18446744073709551618", "18446744073709551619", "18446744073709551620", "-4294967297", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777" };
            Int32[] overflowBases = { 10, 10, 10, 10, 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToUInt32, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToUInt32, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToUInt32, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 100, 0 };
            UInt32[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue };
            UInt32[] expectedValues = { UInt32.MaxValue, UInt32.MinValue };
            Verify(Convert.ToUInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 100, 0 };
            UInt32[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt32, testValues, expectedValues);

            UInt64[] values = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToUInt32, values);
        }
    }
}
