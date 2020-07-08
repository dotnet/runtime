// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ConvertToByteTests : ConvertTestBase<byte>
    {
        [Fact]
        public void FromBoolean()
        {
            bool[] testValues = { true, false };
            byte[] expectedValues = { 1, 0 };
            Verify(Convert.ToByte, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            char[] testValues = { 'A', char.MinValue };
            byte[] expectedValues = { (byte)'A', (byte)char.MinValue };
            Verify(Convert.ToByte, testValues, expectedValues);

            char[] overflowValues = { char.MaxValue };
            VerifyThrows<OverflowException, char>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromDecimal()
        {
            decimal[] testValues = { byte.MaxValue, byte.MinValue, 254.01m, 254.9m };
            byte[] expectedValues = { byte.MaxValue, byte.MinValue, 254, 255 };
            Verify(Convert.ToByte, testValues, expectedValues);

            decimal[] overflowValues = { decimal.MinValue, decimal.MaxValue };
            VerifyThrows<OverflowException, decimal>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromDouble()
        {
            double[] testValues = { byte.MinValue, byte.MaxValue, 100.0, 254.9, 255.2 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 100, 255, 255 };
            Verify(Convert.ToByte, testValues, expectedValues);

            double[] overflowValues = { double.MinValue, double.MaxValue };
            VerifyThrows<OverflowException, double>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { byte.MinValue, byte.MaxValue, 10, 2 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 10, 2 };
            Verify(Convert.ToByte, testValues, expectedValues);

            short[] overflowValues = { short.MinValue, short.MaxValue };
            VerifyThrows<OverflowException, short>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { byte.MinValue, byte.MaxValue, 10 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 10 };
            Verify(Convert.ToByte, testValues, expectedValues);

            int[] overflowValues = { int.MinValue, int.MaxValue };
            VerifyThrows<OverflowException, int>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { byte.MinValue, byte.MaxValue, 10 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 10 };
            Verify(Convert.ToByte, testValues, expectedValues);

            long[] overflowValues = { long.MinValue, long.MaxValue };
            VerifyThrows<OverflowException, long>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            byte[] expectedValues = { 0 };
            VerifyFromObject(Convert.ToByte, Convert.ToByte, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToByte, Convert.ToByte, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { 0, 10, sbyte.MaxValue };
            byte[] expectedValues = { 0, 10, (byte)sbyte.MaxValue };
            Verify(Convert.ToByte, testValues, expectedValues);

            sbyte[] overflowValues = { sbyte.MinValue };
            VerifyThrows<OverflowException, sbyte>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            float[] testValues = { byte.MaxValue, byte.MinValue, 254.01f, 254.9f };
            byte[] expectedValues = { byte.MaxValue, byte.MinValue, 254, 255 };
            Verify(Convert.ToByte, testValues, expectedValues);

            float[] overflowValues = { float.MinValue, float.MaxValue };
            VerifyThrows<OverflowException, float>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { Byte.MaxValue.ToString(), Byte.MinValue.ToString(), "0", "100", null };
            byte[] expectedValues = { byte.MaxValue, byte.MinValue, 0, 100, 0 };
            VerifyFromString(Convert.ToByte, Convert.ToByte, testValues, expectedValues);

            string[] overflowValues = { Int32.MinValue.ToString(), Int32.MaxValue.ToString() };
            VerifyFromStringThrows<OverflowException>(Convert.ToByte, Convert.ToByte, overflowValues);

            string[] formatExceptionValues = { "abba" };
            VerifyFromStringThrows<FormatException>(Convert.ToByte, Convert.ToByte, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithBase()
        {
            string[] testValues = { null, null, null, null, "10", "100", "1011", "ff", "0xff", "77", "11", "11111111" };
            int[] testBases = { 10, 2, 8, 16, 10, 10, 2, 16, 16, 8, 2, 2 };
            byte[] expectedValues = { 0, 0, 0, 0, 10, 100, 11, 255, 255, 63, 3, 255 };
            VerifyFromStringWithBase(Convert.ToByte, testValues, testBases, expectedValues);

            string[] overflowValues = { "256", "111111111", "ffffe", "7777777", "-1" };
            int[] overflowBases = { 10, 2, 16, 8, 10 };
            VerifyFromStringWithBaseThrows<OverflowException>(Convert.ToByte, overflowValues, overflowBases);

            string[] formatExceptionValues = { "fffg", "0xxfff", "8", "112", "!56" };
            int[] formatExceptionBases = { 16, 16, 8, 2, 10 };
            VerifyFromStringWithBaseThrows<FormatException>(Convert.ToByte, formatExceptionValues, formatExceptionBases);

            string[] argumentExceptionValues = { null };
            int[] argumentExceptionBases = { 11 };
            VerifyFromStringWithBaseThrows<ArgumentException>(Convert.ToByte, argumentExceptionValues, argumentExceptionBases);
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { byte.MinValue, byte.MaxValue, 10, 100 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 10, 100 };
            Verify(Convert.ToByte, testValues, expectedValues);

            ushort[] overflowValues = { ushort.MaxValue };
            VerifyThrows<OverflowException, ushort>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { byte.MinValue, byte.MaxValue, 10, 100 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 10, 100 };
            Verify(Convert.ToByte, testValues, expectedValues);

            uint[] overflowValues = { uint.MaxValue };
            VerifyThrows<OverflowException, uint>(Convert.ToByte, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { byte.MinValue, byte.MaxValue, 10, 100 };
            byte[] expectedValues = { byte.MinValue, byte.MaxValue, 10, 100 };
            Verify(Convert.ToByte, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue };
            VerifyThrows<OverflowException, ulong>(Convert.ToByte, overflowValues);
        }
    }
}
