// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToSByteTests : ConvertTestBase<SByte>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            SByte[] expectedValues = { 1, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { 100, 0 };
            SByte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Byte[] overflowValues = { Byte.MaxValue };
            VerifyThrows<OverflowException, Byte>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { 'A', Char.MinValue, };
            SByte[] expectedValues = { 65, (SByte)Char.MinValue };
            Verify(Convert.ToSByte, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { 100m, -100m, 0m };
            SByte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MaxValue, Decimal.MinValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { 100.0, -100.0, 0 };
            SByte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Double[] overflowValues = { Double.MaxValue, Double.MinValue };
            VerifyThrows<OverflowException, Double>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { 100, -100, 0 };
            SByte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MaxValue, Int64.MinValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { 100, -100, 0 };
            SByte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MaxValue, Int32.MinValue };
            VerifyThrows<OverflowException, Int32>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 100, -100, 0 };
            SByte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MaxValue, Int64.MinValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            SByte[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToSByte, Convert.ToSByte, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToSByte, Convert.ToSByte, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { SByte.MaxValue, SByte.MinValue };
            SByte[] expectedValues = { SByte.MaxValue, SByte.MinValue };
            Verify(Convert.ToSByte, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { 100.0f, -100.0f, 0.0f, };
            SByte[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToSByte, testValues, expectedValues);

            Single[] overflowValues = { Single.MaxValue, Single.MinValue };
            VerifyThrows<OverflowException, Single>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "100", "-100", "0", SByte.MinValue.ToString(), SByte.MaxValue.ToString() };
            SByte[] expectedValues = { 100, -100, 0, SByte.MinValue, SByte.MaxValue };
            VerifyFromString(Convert.ToSByte, Convert.ToSByte, testValues, expectedValues);

            String[] overflowValues = { Int16.MinValue.ToString(), Int16.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToSByte, Convert.ToSByte, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToSByte, Convert.ToSByte, formatExceptionValues);

            // Note: Only the Convert.ToSByte(String, IFormatProvider) overload throws an ArgumentNullException.
            // This is inconsistent with the other numeric conversions, but fixing this behavior is not worth making
            // a breaking change which will affect the desktop CLR.
            Assert.Throws<ArgumentNullException>(() => Convert.ToSByte((String)null, TestFormatProvider.s_instance));
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "7f", "127", "177", "1111111", "80", "-128", "200", "10000000" };
            Int32[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            SByte[] expectedValues = { 0, 0, 0, 0, SByte.MaxValue, SByte.MaxValue, SByte.MaxValue, SByte.MaxValue, SByte.MinValue, SByte.MinValue, SByte.MinValue, SByte.MinValue };
            VerifyFromStringWithBase(Convert.ToSByte, testValues, testBases, expectedValues);

            String[] overflowValues = { "128", "-129", "111111111", "1FF", "777" };
            Int32[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToSByte, overflowValues, overflowBases);

            String[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            Int32[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToSByte, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            Int32[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToSByte, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 100, 0 };
            SByte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            UInt16[] overflowValues = { UInt16.MaxValue };
            VerifyThrows<OverflowException, UInt16>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { 100, 0 };
            SByte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            UInt32[] overflowValues = { UInt32.MaxValue };
            VerifyThrows<OverflowException, UInt32>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 100, 0 };
            SByte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToSByte, overflowValues);
        }
    }
}
