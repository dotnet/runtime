// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public unsafe partial class Char8Tests
    {
        [Theory]
        [InlineData(10, 20, -1)]
        [InlineData(20, 10, 1)]
        [InlineData(30, 30, 0)]
        public static void CompareTo(Char8 a, Char8 b, int expectedSign)
        {
            Assert.Equal(expectedSign, Math.Sign(a.CompareTo(b)));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0xFF)]
        [InlineData(0x80)]
        [InlineData(0x00)]
        [InlineData(0x1234)]
        [InlineData(0x12345678)]
        [InlineData(0x1234567812345678)]
        public static void CastOperators(long value)
        {
            // Only the low byte is preserved when casting through Char8.

            Assert.Equal((byte)value, (byte)(Char8)(byte)value);
            Assert.Equal((sbyte)value, (sbyte)(Char8)(sbyte)value);
            Assert.Equal((char)(value & 0xFF), (char)(Char8)(char)value);
            Assert.Equal((short)(value & 0xFF), (short)(Char8)(short)value);
            Assert.Equal((ushort)(value & 0xFF), (ushort)(Char8)(ushort)value);
            Assert.Equal((int)(value & 0xFF), (int)(Char8)(int)value);
            Assert.Equal((uint)(value & 0xFF), (uint)(Char8)(uint)value);
            Assert.Equal((long)(value & 0xFF), (long)(Char8)(long)value);
            Assert.Equal((ulong)(value & 0xFF), (ulong)(Char8)(ulong)value);
        }

        [Fact]
        public static void EqualsObject()
        {
            Assert.False(((Char8)42).Equals((object)null));
            Assert.False(((Char8)42).Equals((object)(int)42));
            Assert.False(((Char8)42).Equals((object)(Char8)43));
            Assert.True(((Char8)42).Equals((object)(Char8)42));
        }

        [Fact]
        public static void EqualsChar8()
        {
            Assert.True(((Char8)42).Equals(42)); // implicit cast to Char8
            Assert.False(((Char8)42).Equals(43)); // implicit cast to Char8
        }

        [Fact]
        public static void GetHashCode_ReturnsValue()
        {
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                Assert.Equal(i, ((Char8)i).GetHashCode());
            }
        }

        [Theory]
        [InlineData(10, 20, false)]
        [InlineData(20, 10, false)]
        [InlineData(30, 30, true)]
        public static void OperatorEquals(Char8 a, Char8 b, bool expected)
        {
            Assert.Equal(expected, (Char8)a == (Char8)b);
            Assert.NotEqual(expected, (Char8)a != (Char8)b);
        }

        [Theory]
        [InlineData(10, 20, true)]
        [InlineData(20, 10, false)]
        [InlineData(29, 30, true)]
        [InlineData(30, 30, false)]
        [InlineData(31, 30, false)]
        public static void OperatorLessThan(Char8 a, Char8 b, bool expected)
        {
            Assert.Equal(expected, (Char8)a < (Char8)b);
            Assert.NotEqual(expected, (Char8)a >= (Char8)b);
        }

        [Theory]
        [InlineData(10, 20, false)]
        [InlineData(20, 10, true)]
        [InlineData(29, 30, false)]
        [InlineData(30, 30, false)]
        [InlineData(31, 30, true)]
        public static void OperatorGreaterThan(Char8 a, Char8 b, bool expected)
        {
            Assert.Equal(expected, (Char8)a > (Char8)b);
            Assert.NotEqual(expected, (Char8)a <= (Char8)b);
        }

        [Fact]
        public static void ToString_ReturnsHexValue()
        {
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                Assert.Equal(i.ToString("X2", CultureInfo.InvariantCulture), ((Char8)i).ToString());
            }
        }
    }
}
