// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToInt32Tests : ConvertTestBase<Int32>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Int32[] expectedValues = { 1, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Int32[] expectedValues = { 255, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { Char.MinValue, Char.MaxValue, 'b' };
            Int32[] expectedValues = { Char.MinValue, Char.MaxValue, 98 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 100m, -100m, 0m };
            Int32[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MaxValue, Decimal.MinValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 100.0, -100.0, 0 };
            Int32[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, Double.MinValue };
            VerifyThrows<OverflowException, Double>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { 100, -100, 0, };
            Int32[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Int32[] expectedValues = { Int32.MaxValue, Int32.MinValue, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 100, -100, 0 };
            Int32[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MaxValue, Int64.MinValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Int32[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToInt32, Convert.ToInt32, testValues, expectedValues);

            Object[] invalidValues = { new Object() };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToInt32, Convert.ToInt32, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, -100, 0 };
            Int32[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 100.0f, -100.0f, 0.0f, };
            Int32[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt32, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, Single.MinValue };
            VerifyThrows<OverflowException, Single>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "100", "-100", "0", Int32.MinValue.ToString(), Int32.MaxValue.ToString(), null };
            Int32[] expectedValues = { 100, -100, 0, Int32.MinValue, Int32.MaxValue, 0 };
            VerifyFromString(Convert.ToInt32, Convert.ToInt32, testValues, expectedValues);

            String[] overflowValues = { Int64.MinValue.ToString(), Int64.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToInt32, Convert.ToInt32, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToInt32, Convert.ToInt32, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "7FFFFFFF", "2147483647", "17777777777", "1111111111111111111111111111111", "80000000", "-2147483648", "20000000000", "10000000000000000000000000000000", };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2, };
            Int32[] expectedValues = { 0, 0, 0, 0, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MinValue, Int32.MinValue, Int32.MinValue, Int32.MinValue, };
            VerifyFromStringWithBase(Convert.ToInt32, testValues, testBases, expectedValues);

            String[] overflowValues = { "2147483648", "-2147483649", "111111111111111111111111111111111", "1FFFFffff", "777777777777" };
            Int32[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToInt32, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToInt32, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToInt32, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 100, 0 };
            Int32[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { 100, 0 };
            Int32[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            UInt32[] overflowValues = { UInt32.MaxValue };
            VerifyThrows<OverflowException, UInt32>(Convert.ToInt32, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 100, 0 };
            Int32[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt32, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToInt32, overflowValues);
        }
    }
}
