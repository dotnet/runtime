// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToCharTests : ConvertTestBase<char>
    {
        [Fact]
        public void FromByte()
        {
            byte[] testValues = { byte.MaxValue, byte.MinValue };
            char[] expectedValues = { (char)byte.MaxValue, (char)byte.MinValue };
            Verify(Convert.ToChar, testValues, expectedValues);
        }

        [Fact]
        public void FromChar()
        {
            object[] testValues = { char.MaxValue, char.MinValue, 'b' };
            char[] expectedValues = { char.MaxValue, char.MinValue, 'b' };
            Verify<object>(Convert.ToChar, testValues, expectedValues);
        }

        [Fact]
        public void FromDecimal()
        {
            object[] invalidValues = { 0m, decimal.MinValue, decimal.MaxValue };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToChar, Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromDouble()
        {
            object[] invalidValues = { 0.0, double.MinValue, double.MaxValue };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToChar, Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromInt16()
        {
            short[] testValues = { short.MaxValue, 0 };
            char[] expectedValues = { (char)short.MaxValue, '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            short[] overflowValues = { short.MinValue, -1000 };
            VerifyThrows<OverflowException, short>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromInt32()
        {
            int[] testValues = { char.MaxValue, char.MinValue };
            char[] expectedValues = { char.MaxValue, char.MinValue };
            Verify(Convert.ToChar, testValues, expectedValues);

            int[] overflowValues = { int.MinValue, int.MaxValue, (int)ushort.MaxValue + 1, -1000 };
            VerifyThrows<OverflowException, int>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromInt64()
        {
            long[] testValues = { 0, 98, ushort.MaxValue };
            char[] expectedValues = { '\0', 'b', char.MaxValue };
            Verify(Convert.ToChar, testValues, expectedValues);

            long[] overflowValues = { long.MinValue, long.MaxValue, -1 };
            VerifyThrows<OverflowException, long>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromObject()
        {
            object[] testValues = { null };
            char[] expectedValues = { '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            object[] invalidValues = { new object(), DateTime.Now };
            VerifyThrows<InvalidCastException, object>(Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromSByte()
        {
            sbyte[] testValues = { sbyte.MaxValue, 0 };
            char[] expectedValues = { (char)sbyte.MaxValue, '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            sbyte[] overflowValues = { sbyte.MinValue, -100, -1 };
            VerifyThrows<OverflowException, sbyte>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromSingle()
        {
            object[] invalidValues = { 0f, float.MinValue, float.MaxValue };
            VerifyFromObjectThrows<InvalidCastException>(Convert.ToChar, Convert.ToChar, invalidValues);
        }

        [Fact]
        public void FromString()
        {
            string[] testValues = { "a", "T", "z", "a" };
            char[] expectedValues = { 'a', 'T', 'z', 'a' };
            VerifyFromString(Convert.ToChar, Convert.ToChar, testValues, expectedValues);

            string[] formatExceptionValues = { string.Empty, "ab" };
            VerifyFromStringThrows<FormatException>(Convert.ToChar, Convert.ToChar, formatExceptionValues);
            VerifyFromStringThrows<ArgumentNullException>(Convert.ToChar, Convert.ToChar, new string[] { null });
        }

        [Fact]
        public void FromUInt16()
        {
            ushort[] testValues = { 0, 98, ushort.MaxValue };
            char[] expectedValues = { '\0', 'b', char.MaxValue };
            Verify(Convert.ToChar, testValues, expectedValues);
        }

        [Fact]
        public void FromUInt32()
        {
            uint[] testValues = { ushort.MaxValue, 0 };
            char[] expectedValues = { (char)ushort.MaxValue, '\0' };
            Verify(Convert.ToChar, testValues, expectedValues);

            uint[] overflowValues = { uint.MaxValue };
            VerifyThrows<OverflowException, uint>(Convert.ToChar, overflowValues);
        }

        [Fact]
        public void FromUInt64()
        {
            ulong[] testValues = { 0, 98, ushort.MaxValue };
            char[] expectedValues = { '\0', 'b', char.MaxValue };
            Verify(Convert.ToChar, testValues, expectedValues);

            ulong[] overflowValues = { ulong.MaxValue, (ulong)ushort.MaxValue + 1 };
            VerifyThrows<OverflowException, ulong>(Convert.ToChar, overflowValues);
        }
    }
}
