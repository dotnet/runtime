// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToInt16Tests : ConvertTestBase<Int16>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Int16[] expectedValues = { 1, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Int16[] expectedValues = { 255, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { 'A', Char.MinValue, };
            Int16[] expectedValues = { 65, (Int16)Char.MinValue };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 100m, -100m, 0m };
            Int16[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MaxValue, Decimal.MinValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 100.0, -100.0, 0 };
            Int16[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, Double.MinValue };
            VerifyThrows<OverflowException, Double>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Int16[] expectedValues = { Int16.MaxValue, Int16.MinValue, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { 100, -100, 0 };
            Int16[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MaxValue, Int32.MinValue };
            VerifyThrows<OverflowException, Int32>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 100, -100, 0 };
            Int16[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MaxValue, Int64.MinValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Int16[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToInt16, Convert.ToInt16, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToInt16, Convert.ToInt16, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, -100, 0 };
            Int16[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 100.0f, -100.0f, 0.0f, };
            Int16[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt16, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, Single.MinValue };
            VerifyThrows<OverflowException, Single>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "100", "-100", "0", Int16.MinValue.ToString(), Int16.MaxValue.ToString(), null };
            Int16[] expectedValues = { 100, -100, 0, Int16.MinValue, Int16.MaxValue, 0 };
            VerifyFromString(Convert.ToInt16, Convert.ToInt16, testValues, expectedValues);

            String[] overflowValues = { Int32.MinValue.ToString(), Int32.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToInt16, Convert.ToInt16, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToInt16, Convert.ToInt16, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "7fff", "32767", "77777", "111111111111111", "8000", "-32768", "100000", "1000000000000000" };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            Int16[] expectedValues = { 0, 0, 0, 0, Int16.MaxValue, Int16.MaxValue, Int16.MaxValue, Int16.MaxValue, Int16.MinValue, Int16.MinValue, Int16.MinValue, Int16.MinValue };
            VerifyFromStringWithBase(Convert.ToInt16, testValues, testBases, expectedValues);

            String[] overflowValues = { "32768", "-32769", "11111111111111111", "1FFFF", "777777" };
            Int32[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToInt16, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToInt16, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToInt16, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 100, 0 };
            Int16[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            UInt16[] overflowValues = { UInt16.MaxValue };
            VerifyThrows<OverflowException, UInt16>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { 100, 0 };
            Int16[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            UInt32[] overflowValues = { UInt32.MaxValue };
            VerifyThrows<OverflowException, UInt32>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 100, 0 };
            Int16[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToInt16, overflowValues);
        }
    }
}
