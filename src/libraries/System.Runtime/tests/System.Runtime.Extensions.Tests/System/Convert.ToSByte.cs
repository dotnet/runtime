// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ConvertToSByteTests : ConvertTestBase<sbyte>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            sbyte[] expectedValues = { 1, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { 100, 0 };
            sbyte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            byte[] overflowValues = { byte.MaxValue };
            VerifyThrows<OverflowException, byte>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { 'A', char.MinValue, };
            sbyte[] expectedValues = { 65, (sbyte)char.MinValue };
            Verify(Convert.ToSByte, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 100m, -100m, 0m };
            sbyte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MaxValue, decimal.MinValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 100.0, -100.0, 0 };
            sbyte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, double.MinValue };
            VerifyThrows<OverflowException, double>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { 100, -100, 0 };
            sbyte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            long[] overflowValues = { long.MaxValue, long.MinValue };
            VerifyThrows<OverflowException, long>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { 100, -100, 0 };
            sbyte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            int[] overflowValues = { int.MaxValue, int.MinValue };
            VerifyThrows<OverflowException, int>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 100, -100, 0 };
            sbyte[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            long[] overflowValues = { long.MaxValue, long.MinValue };
            VerifyThrows<OverflowException, long>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            sbyte[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToSByte, Convert.ToSByte, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToSByte, Convert.ToSByte, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { sbyte.MaxValue, sbyte.MinValue };
            sbyte[] expectedValues = { sbyte.MaxValue, sbyte.MinValue };
            Verify(Convert.ToSByte, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 100.0f, -100.0f, 0.0f, };
            sbyte[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToSByte, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, float.MinValue };
            VerifyThrows<OverflowException, float>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "100", "-100", "0", SByte.MinValue.ToString(), SByte.MaxValue.ToString() };
            sbyte[] expectedValues = { 100, -100, 0, sbyte.MinValue, sbyte.MaxValue };
            VerifyFromString(Convert.ToSByte, Convert.ToSByte, testValues, expectedValues);

            string[] overflowValues = { Int16.MinValue.ToString(), Int16.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToSByte, Convert.ToSByte, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToSByte, Convert.ToSByte, formatExceptionValues);

            // Note: Only the Convert.ToSByte(String, IFormatProvider) overload throws an ArgumentNullException.
            // This is inconsistent with the other numeric conversions, but fixing this behavior is not worth making
            // a breaking change.
            Assert.Throws<ArgumentNullException>(() => Convert.ToSByte((string)null, TestFormatProvider.s_instance));
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "7f", "127", "177", "1111111", "80", "-128", "200", "10000000" };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            sbyte[] expectedValues = { 0, 0, 0, 0, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MinValue, sbyte.MinValue, sbyte.MinValue, sbyte.MinValue };
            VerifyFromStringWithBase(Convert.ToSByte, testValues, testBases, expectedValues);

            string[] overflowValues = { "128", "-129", "111111111", "1FF", "777" };
            int[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToSByte, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToSByte, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToSByte, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 100, 0 };
            sbyte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            ushort[] overflowValues = { ushort.MaxValue };
            VerifyThrows<OverflowException, ushort>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { 100, 0 };
            sbyte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            uint[] overflowValues = { uint.MaxValue };
            VerifyThrows<OverflowException, uint>(Convert.ToSByte, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 100, 0 };
            sbyte[] expectedValues = { 100, 0 };
            Verify(Convert.ToSByte, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToSByte, overflowValues);
        }
    }
}
