// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToCharTests : ConvertTestBase<Char>
    {
        [Fact]
        public void FromByte()
        {
            Byte[] testValues = { Byte.MaxValue, Byte.MinValue };
            Char[] expectedValues = { (Char)Byte.MaxValue, (Char)Byte.MinValue };
            Verify(Convert.ToChar, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            Object[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
            Char[] expectedValues = { Char.MaxValue, Char.MinValue, 'b' };
            Verify<Object>(Convert.ToChar, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            Object[] invalidValues = { 0m, Decimal.MinValue, Decimal.MaxValue };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToChar, Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromDouble()
        {
            Object[] invalidValues = { 0.0, Double.MinValue, Double.MaxValue };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToChar, Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromInt16()
        {
            Int16[] testValues = { Int16.MaxValue, 0 };
            Char[] expectedValues = { (Char)Int16.MaxValue, '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            Int16[] overflowValues = { Int16.MinValue, -1000 };
            VerifyThrows<OverflowException, Int16>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            Int32[] testValues = { Char.MaxValue, Char.MinValue };
            Char[] expectedValues = { Char.MaxValue, Char.MinValue };
            Verify(Convert.ToChar, testValues, expectedValues);

            Int32[] overflowValues = { Int32.MinValue, Int32.MaxValue, (Int32)UInt16.MaxValue + 1, -1000 };
            VerifyThrows<OverflowException, Int32>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            Int64[] testValues = { 0, 98, UInt16.MaxValue };
            Char[] expectedValues = { '\0', 'b', Char.MaxValue };
            Verify(Convert.ToChar, testValues, expectedValues);

            Int64[] overflowValues = { Int64.MinValue, Int64.MaxValue, -1 };
            VerifyThrows<OverflowException, Int64>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            Object[] testValues = { null };
            Char[] expectedValues = { '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            Object[] invalidValues = { new Object(), DateTime.Now };
            VerifyThrows<InvalidCastException, Object>(Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            SByte[] testValues = { SByte.MaxValue, 0 };
            Char[] expectedValues = { (Char)SByte.MaxValue, '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            SByte[] overflowValues = { SByte.MinValue, -100, -1 };
            VerifyThrows<OverflowException, SByte>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            Object[] invalidValues = { 0f, Single.MinValue, Single.MaxValue };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToChar, Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromString()
        {
            String[] testValues = { "a", "T", "z", "a" };
            Char[] expectedValues = { 'a', 'T', 'z', 'a' };
            VerifyFromString(Convert.ToChar, Convert.ToChar, testValues, expectedValues);

            String[] formatExceptionValues = { String.Empty, "ab" };
            VerifyFromStringThrows<FormatException>(Convert.ToChar, Convert.ToChar, formatExceptionValues);
            VerifyFromStringThrows<ArgumentNullException>(Convert.ToChar, Convert.ToChar, new String[] { null });
        }

        [Fact]
        public void FromUInt16()
        {
            UInt16[] testValues = { 0, 98, UInt16.MaxValue };
            Char[] expectedValues = { '\0', 'b', Char.MaxValue };
            Verify(Convert.ToChar, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            UInt32[] testValues = { UInt16.MaxValue, 0 };
            Char[] expectedValues = { (Char)UInt16.MaxValue, '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            UInt32[] overflowValues = { UInt32.MaxValue };
            VerifyThrows<OverflowException, UInt32>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            UInt64[] testValues = { 0, 98, UInt16.MaxValue };
            Char[] expectedValues = { '\0', 'b', Char.MaxValue };
            Verify(Convert.ToChar, testValues, expectedValues);

            UInt64[] overflowValues = { UInt64.MaxValue, (UInt64)UInt16.MaxValue + 1 };
            VerifyThrows<OverflowException, UInt64>(Convert.ToChar, overflowValues);
        }
    }
}
