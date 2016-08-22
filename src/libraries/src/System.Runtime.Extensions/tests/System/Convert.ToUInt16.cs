// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToUInt16Tests : ConvertTestBase<UInt16>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            UInt16[] expectedValues = { 1, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            UInt16[] expectedValues = { 255, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
            UInt16[] expectedValues = { Char.MaxValue, Char.MinValue, 98 };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 1000m, 0m };
            UInt16[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MaxValue, Decimal.MinValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 1000.0, 0.0 };
            UInt16[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, -100.0 };
            VerifyThrows<OverflowException, Double>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { 1000, 0, Int16.MaxValue };
            UInt16[] expectedValues = { 1000, 0, (UInt16)Int16.MaxValue };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            Int16[] overflowValues = { Int16.MinValue };
            VerifyThrows<OverflowException, Int16>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { 1000, 0 };
            UInt16[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MinValue, Int32.MaxValue };
            VerifyThrows<OverflowException, Int32>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 1000, 0 };
            UInt16[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MinValue, Int64.MaxValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            UInt16[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToUInt16, Convert.ToUInt16, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToUInt16, Convert.ToUInt16, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 100, 0 };
            UInt16[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            SByte[] values = { SByte.MinValue };
            VerifyThrows<OverflowException, SByte>(Convert.ToUInt16, values);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 1000.0f, 0.0f };
            UInt16[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            Single[] values = { Single.MaxValue, -100.0f };
            VerifyThrows<OverflowException, Single>(Convert.ToUInt16, values);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), null };
            UInt16[] expectedValues = { 1000, 0, UInt16.MaxValue, 0 };
            VerifyFromString(Convert.ToUInt16, Convert.ToUInt16, testValues, expectedValues);

            String[] overflowValues = { "-1", Decimal.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToUInt16, Convert.ToUInt16, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToUInt16, Convert.ToUInt16, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "ffff", "65535", "177777", "1111111111111111", "0", "0", "0", "0" };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            UInt16[] expectedValues = { 0, 0, 0, 0, UInt16.MaxValue, UInt16.MaxValue, UInt16.MaxValue, UInt16.MaxValue, UInt16.MinValue, UInt16.MinValue, UInt16.MinValue, UInt16.MinValue };
            VerifyFromStringWithBase(Convert.ToUInt16, testValues, testBases, expectedValues);

            String[] overflowValues = { "65536", "-1", "11111111111111111", "1FFFF", "777777" };
            Int32[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToUInt16, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToUInt16, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToUInt16, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue };
            UInt16[] expectedValues = { UInt16.MaxValue, UInt16.MinValue };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { 100, 0 };
            UInt16[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            UInt32[] overflowValues = { UInt32.MaxValue };
            VerifyThrows<OverflowException, UInt32>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 100, 0 };
            UInt16[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToUInt16, overflowValues);
        }
    }
}
