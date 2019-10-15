// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToInt16Tests : ConvertTestBase<short>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            short[] expectedValues = { 1, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            short[] expectedValues = { 255, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { 'A', char.MinValue, };
            short[] expectedValues = { 65, (short)char.MinValue };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { 100m, -100m, 0m };
            short[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MaxValue, decimal.MinValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { 100.0, -100.0, 0 };
            short[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            double[] overflowValues = { double.MaxValue, double.MinValue };
            VerifyThrows<OverflowException, double>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { short.MaxValue, short.MinValue, 0 };
            short[] expectedValues = { short.MaxValue, short.MinValue, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { 100, -100, 0 };
            short[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            int[] overflowValues = { int.MaxValue, int.MinValue };
            VerifyThrows<OverflowException, int>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 100, -100, 0 };
            short[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            long[] overflowValues = { long.MaxValue, long.MinValue };
            VerifyThrows<OverflowException, long>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            short[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToInt16, Convert.ToInt16, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToInt16, Convert.ToInt16, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 100, -100, 0 };
            short[] expectedValues = { 100, -100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { 100.0f, -100.0f, 0.0f, };
            short[] expectedValues = { 100, -100, 0, };
            Verify(Convert.ToInt16, testValues, expectedValues);

            float[] overflowValues = { float.MaxValue, float.MinValue };
            VerifyThrows<OverflowException, float>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "100", "-100", "0", Int16.MinValue.ToString(), Int16.MaxValue.ToString(), null };
            short[] expectedValues = { 100, -100, 0, short.MinValue, short.MaxValue, 0 };
            VerifyFromString(Convert.ToInt16, Convert.ToInt16, testValues, expectedValues);

            string[] overflowValues = { Int32.MinValue.ToString(), Int32.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToInt16, Convert.ToInt16, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToInt16, Convert.ToInt16, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "7fff", "32767", "77777", "111111111111111", "8000", "-32768", "100000", "1000000000000000" };
            int[] testBases = { 10, 2, 8, 16, 16, 10, 8, 2, 16, 10, 8, 2 };
            short[] expectedValues = { 0, 0, 0, 0, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MinValue, short.MinValue, short.MinValue, short.MinValue };
            VerifyFromStringWithBase(Convert.ToInt16, testValues, testBases, expectedValues);

            string[] overflowValues = { "32768", "-32769", "11111111111111111", "1FFFF", "777777" };
            int[] overflowBases = { 10, 10, 2, 16, 8 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToInt16, overflowValues, overflowBases);

            string[] formatExceptionValues = { "12", "ffffffffffffffffffff" };
            int[] formatExceptionBases = { 2, 8 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToInt16, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { "10", "11", "abba", "-ab" };
            int[] argumentExceptionBases = { -1, 3, 0, 16 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToInt16, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 100, 0 };
            short[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            ushort[] overflowValues = { ushort.MaxValue };
            VerifyThrows<OverflowException, ushort>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { 100, 0 };
            short[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            uint[] overflowValues = { uint.MaxValue };
            VerifyThrows<OverflowException, uint>(Convert.ToInt16, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 100, 0 };
            short[] expectedValues = { 100, 0 };
            Verify(Convert.ToInt16, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToInt16, overflowValues);
        }
    }
}
