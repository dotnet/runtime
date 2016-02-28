// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToByteTests : ConvertTestBase<Byte>
    {
        [Fact]
        public void FromBoolean()
        {
            Boolean[] testValues = { true, false };
            Byte[] expectedValues = { 1, 0 };
            Verify(Convert.ToByte, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Char[] testValues = { 'A', Char.MinValue };
            Byte[] expectedValues = { (Byte)'A', (Byte)Char.MinValue };
            Verify(Convert.ToByte, testValues, expectedValues);

            Char[] overflowValues = { Char.MaxValue };
            VerifyThrows<OverflowException, Char>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Decimal[] testValues = { Byte.MaxValue, Byte.MinValue, 254.01m, 254.9m };
            Byte[] expectedValues = { Byte.MaxValue, Byte.MinValue, 254, 255 };
            Verify(Convert.ToByte, testValues, expectedValues);

            Decimal[] overflowValues = { Decimal.MinValue, Decimal.MaxValue };
            VerifyThrows<OverflowException, Decimal>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            Double[] testValues = { Byte.MinValue, Byte.MaxValue, 100.0, 254.9, 255.2 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 100, 255, 255 };
            Verify(Convert.ToByte, testValues, expectedValues);

            Double[] overflowValues = { Double.MinValue, Double.MaxValue };
            VerifyThrows<OverflowException, Double>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Byte.MinValue, Byte.MaxValue, 10, 2 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 10, 2 };
            Verify(Convert.ToByte, testValues, expectedValues);

            Int16[] overflowValues = { Int16.MinValue, Int16.MaxValue };
            VerifyThrows<OverflowException, Int16>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Byte.MinValue, Byte.MaxValue, 10 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 10 };
            Verify(Convert.ToByte, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MinValue, Int32.MaxValue };
            VerifyThrows<OverflowException, Int32>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { Byte.MinValue, Byte.MaxValue, 10 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 10 };
            Verify(Convert.ToByte, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MinValue, Int64.MaxValue };
            VerifyThrows<OverflowException, Int64>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Byte[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToByte, Convert.ToByte, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToByte, Convert.ToByte, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { 0, 10, SByte.MaxValue };
            Byte[] expectedValues = { 0, 10, (Byte)SByte.MaxValue };
            Verify(Convert.ToByte, testValues, expectedValues);

            SByte[] overflowValues = { SByte.MinValue };
            VerifyThrows<OverflowException, SByte>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            Single[] testValues = { Byte.MaxValue, Byte.MinValue, 254.01f, 254.9f };
            Byte[] expectedValues = { Byte.MaxValue, Byte.MinValue, 254, 255 };
            Verify(Convert.ToByte, testValues, expectedValues);

            Single[] overflowValues = { Single.MinValue, Single.MaxValue };
            VerifyThrows<OverflowException, Single>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { Byte.MaxValue.ToString(), Byte.MinValue.ToString(), "0", "100", null };
            Byte[] expectedValues = { Byte.MaxValue, Byte.MinValue, 0, 100, 0 };
            VerifyFromString(Convert.ToByte, Convert.ToByte, testValues, expectedValues);

            String[] overflowValues = { Int32.MinValue.ToString(), Int32.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToByte, Convert.ToByte, overflowValues);

            String[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToByte, Convert.ToByte, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            String[] testValues = { null, null, null, null, "10", "100", "1011", "ff", "0xff", "77", "11", "11111111" };
            Int32[] testBases = { 10, 2, 8, 16, 10, 10, 2, 16, 16, 8, 2, 2 };
            Byte[] expectedValues = { 0, 0, 0, 0, 10, 100, 11, 255, 255, 63, 3, 255 };
            VerifyFromStringWithBase(Convert.ToByte, testValues, testBases, expectedValues);

            String[] overflowValues = { "256", "111111111", "ffffe", "7777777", "-1" };
            Int32[] overflowBases = { 10, 2, 16, 8, 10 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToByte, overflowValues, overflowBases);

            String[] formatExceptionValues = { "fffg", "0xxfff", "8", "112", "!56" };
            Int32[] formatExceptionBases = { 16, 16, 8, 2, 10 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToByte, formatExceptionValues, formatExceptionBases);

            String[] argumentExceptionValues = { null };
            Int32[] argumentExceptionBases = { 11 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToByte, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { Byte.MinValue, Byte.MaxValue, 10, 100 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 10, 100 };
            Verify(Convert.ToByte, testValues, expectedValues);

            UInt16[] overflowValues = { UInt16.MaxValue };
            VerifyThrows<OverflowException, UInt16>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { Byte.MinValue, Byte.MaxValue, 10, 100 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 10, 100 };
            Verify(Convert.ToByte, testValues, expectedValues);

            UInt32[] overflowValues = { UInt32.MaxValue };
            VerifyThrows<OverflowException, UInt32>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { Byte.MinValue, Byte.MaxValue, 10, 100 };
            Byte[] expectedValues = { Byte.MinValue, Byte.MaxValue, 10, 100 };
            Verify(Convert.ToByte, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue };
            VerifyThrows<OverflowException, UInt64>(Convert.ToByte, overflowValues);
        }
    }
}
