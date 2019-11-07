// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToUInt16Tests : ConvertTestBase<ushort>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            ushort[] expectedValues = { 1, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            ushort[] expectedValues = { 255, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { char.MaxValue, char.MinValue, 'b' };
            ushort[] expectedValues = { char.MaxValue, char.MinValue, 98 };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 1000m, 0m };
            ushort[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MaxValue, decimal.MinValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 1000.0, 0.0 };
            ushort[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, -100.0 };
            VerifyThrows<OverflowException, double>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { 1000, 0, short.MaxValue };
            ushort[] expectedValues = { 1000, 0, (ushort)short.MaxValue };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            short[] overflowValues = { short.MinValue };
            VerifyThrows<OverflowException, short>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { 1000, 0 };
            ushort[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            int[] overflowValues = { int.MinValue, int.MaxValue };
            VerifyThrows<OverflowException, int>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 1000, 0 };
            ushort[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            long[] overflowValues = { long.MinValue, long.MaxValue };
            VerifyThrows<OverflowException, long>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            ushort[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToUInt16, Convert.ToUInt16, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToUInt16, Convert.ToUInt16, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, 0 };
            ushort[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            sbyte[] values = { sbyte.MinValue };
            VerifyThrows<OverflowException, sbyte>(Convert.ToUInt16, values);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 1000.0f, 0.0f };
            ushort[] expectedValues = { 1000, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            float[] values = { float.MaxValue, -100.0f };
            VerifyThrows<OverflowException, float>(Convert.ToUInt16, values);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), null };
            ushort[] expectedValues = { 1000, 0, ushort.MaxValue, 0 };
            VerifyFromString(Convert.ToUInt16, Convert.ToUInt16, testValues, expectedValues);

            string[] overflowValues = { "-1", Decimal.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToUInt16, Convert.ToUInt16, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToUInt16, Convert.ToUInt16, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "ffff", "65535", "177777", "1111111111111111", "0", "0", "0", "0" };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            ushort[] expectedValues = { 0, 0, 0, 0, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MinValue, ushort.MinValue, ushort.MinValue, ushort.MinValue };
            VerifyFromStringWithBase(Convert.ToUInt16, testValues, testBases, expectedValues);

            string[] overflowValues = { "65536", "-1", "11111111111111111", "1FFFF", "777777" };
            int[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToUInt16, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToUInt16, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToUInt16, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { ushort.MaxValue, ushort.MinValue };
            ushort[] expectedValues = { ushort.MaxValue, ushort.MinValue };
            Verify(Convert.ToUInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { 100, 0 };
            ushort[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            uint[] overflowValues = { uint.MaxValue };
            VerifyThrows<OverflowException, uint>(Convert.ToUInt16, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 100, 0 };
            ushort[] expectedValues = { 100, 0 };
            Verify(Convert.ToUInt16, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToUInt16, overflowValues);
        }
    }
}
