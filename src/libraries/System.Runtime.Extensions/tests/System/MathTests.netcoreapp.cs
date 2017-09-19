// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Math.Clamp is not in CoreRT yet.")]
    public static partial class MathTests
    {
        public static IEnumerable<object[]> Clamp_UnsignedInt_TestData()
        {
            yield return new object[] { 1, 1, 3, 1 };
            yield return new object[] { 2, 1, 3, 2 };
            yield return new object[] { 3, 1, 3, 3 };
            yield return new object[] { 1, 1, 1, 1 };

            yield return new object[] { 0, 1, 3, 1 };
            yield return new object[] { 4, 1, 3, 3 };
        }

        public static IEnumerable<object[]> Clamp_SignedInt_TestData()
        {
            yield return new object[] { -1, -1, 1, -1 };
            yield return new object[] { 0, -1, 1, 0 };
            yield return new object[] { 1, -1, 1, 1 };
            yield return new object[] { 1, -1, 1, 1 };

            yield return new object[] { -2, -1, 1, -1 };
            yield return new object[] { 2, -1, 1, 1 };
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_SByte(sbyte value, sbyte min, sbyte max, sbyte expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_Byte(byte value, byte min, byte max, byte expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Short(short value, short min, short max, short expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_UShort(ushort value, ushort min, ushort max, ushort expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Int(int value, int min, int max, int expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_UInt(uint value, uint min, uint max, uint expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Long(long value, long min, long max, long expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_ULong(ulong value, ulong min, ulong max, ulong expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(1, double.NegativeInfinity, double.PositiveInfinity, 1)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(1, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(1, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, double.NaN, 1, double.NaN)]
        [InlineData(double.NaN, 1, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1, 1, double.NaN)]
        [InlineData(1, double.NaN, double.NaN, 1)]
        [InlineData(1, double.NaN, 1, 1)]
        [InlineData(1, 1, double.NaN, 1)]
        public static void Clamp_Double(double value, double min, double max, double expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(1, float.NegativeInfinity, float.PositiveInfinity, 1)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(1, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(1, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.NaN, float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.NaN, 1, float.NaN)]
        [InlineData(float.NaN, 1, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1, 1, float.NaN)]
        [InlineData(1, float.NaN, float.NaN, 1)]
        [InlineData(1, float.NaN, 1, 1)]
        [InlineData(1, 1, float.NaN, 1)]
        public static void Clamp_Float(float value, float min, float max, float expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Decimal(decimal value, decimal min, decimal max, decimal expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Fact]
        public static void Clamp_MinGreaterThanMax_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((sbyte)1, (sbyte)2, (sbyte)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((byte)1, (byte)2, (byte)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((short)1, (short)2, (short)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((ushort)1, (ushort)2, (ushort)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((int)1, (int)2, (int)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((uint)1, (uint)2, (uint)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((long)1, (long)2, (long)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((ulong)1, (ulong)2, (ulong)1));

            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((float)1, (float)2, (float)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((double)1, (double)2, (double)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((decimal)1, (decimal)2, (decimal)1));
        }
    }
}
