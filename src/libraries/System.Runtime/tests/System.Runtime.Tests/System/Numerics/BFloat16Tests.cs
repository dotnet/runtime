// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Numerics.Tests
{
    public class BFloat16Tests
    {
        private static ushort BFloat16ToUInt16Bits(BFloat16 value) => Unsafe.BitCast<BFloat16, ushort>(value);

        private static BFloat16 UInt16BitsToBFloat16(ushort value) => Unsafe.BitCast<ushort, BFloat16>(value);

        private static bool IsNaN(BFloat16 value) => float.IsNaN(BitConverter.Int32BitsToSingle(BFloat16ToUInt16Bits(value) << 16));

        [Fact]
        public static void Epsilon()
        {
            Assert.Equal(0x0001u, BFloat16ToUInt16Bits(BFloat16.Epsilon));
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(0xFF7Fu, BFloat16ToUInt16Bits(BFloat16.MinValue));
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7F7Fu, BFloat16ToUInt16Bits(BFloat16.MaxValue));
        }

        [Fact]
        public static void Ctor_Empty()
        {
            var value = new BFloat16();
            Assert.Equal(0x0000, BFloat16ToUInt16Bits(value));
        }

        public static IEnumerable<object[]> CompareTo_ThrowsArgumentException_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { 234.0 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_ThrowsArgumentException_TestData))]
        public static void CompareTo_ThrowsArgumentException(object obj)
        {
            Assert.Throws<ArgumentException>(() => BFloat16.MaxValue.CompareTo(obj));
        }

        public static IEnumerable<object[]> CompareTo_TestData()
        {
            yield return new object[] { Half.MaxValue, Half.MaxValue, 0 };
            yield return new object[] { Half.MaxValue, Half.MinValue, 1 };
            yield return new object[] { Half.Epsilon, BitConverter.UInt16BitsToHalf(0x8001), 1 };
            yield return new object[] { Half.MaxValue, BitConverter.UInt16BitsToHalf(0x0000), 1 };
            yield return new object[] { Half.MaxValue, Half.Epsilon, 1 };
            yield return new object[] { Half.MaxValue, Half.PositiveInfinity, -1 };
            yield return new object[] { Half.MinValue, Half.MaxValue, -1 };
            yield return new object[] { Half.MaxValue, Half.NaN, 1 };
            yield return new object[] { Half.NaN, Half.NaN, 0 };
            yield return new object[] { Half.NaN, BitConverter.UInt16BitsToHalf(0x0000), -1 };
            yield return new object[] { Half.MaxValue, null, 1 };
            yield return new object[] { Half.MinValue, Half.NegativeInfinity, 1 };
            yield return new object[] { Half.NegativeInfinity, Half.MinValue, -1 };
            yield return new object[] { BitConverter.UInt16BitsToHalf(0x8000), Half.NegativeInfinity, 1 }; // Negative zero
            yield return new object[] { Half.NegativeInfinity, BitConverter.UInt16BitsToHalf(0x8000), -1 }; // Negative zero
            yield return new object[] { Half.NegativeInfinity, Half.NegativeInfinity, 0 };
            yield return new object[] { Half.PositiveInfinity, Half.PositiveInfinity, 0 };
            yield return new object[] { (Half)(-180f), (Half)(-180f), 0 };
            yield return new object[] { (Half)(180f), (Half)(180f), 0 };
            yield return new object[] { (Half)(-180f), (Half)(180f), -1 };
            yield return new object[] { (Half)(180f), (Half)(-180f), 1 };
            yield return new object[] { (Half)(-65535), (object)null, 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo(BFloat16 value, object obj, int expected)
        {
            if (obj is BFloat16 other)
            {
                Assert.Equal(expected, Math.Sign(value.CompareTo(other)));

                if (IsNaN(value) || IsNaN(other))
                {
                    Assert.False(value >= other);
                    Assert.False(value > other);
                    Assert.False(value <= other);
                    Assert.False(value < other);
                }
                else
                {
                    if (expected >= 0)
                    {
                        Assert.True(value >= other);
                        Assert.False(value < other);
                    }
                    if (expected > 0)
                    {
                        Assert.True(value > other);
                        Assert.False(value <= other);
                    }
                    if (expected <= 0)
                    {
                        Assert.True(value <= other);
                        Assert.False(value > other);
                    }
                    if (expected < 0)
                    {
                        Assert.True(value < other);
                        Assert.False(value >= other);
                    }
                }
            }

            Assert.Equal(expected, Math.Sign(value.CompareTo(obj)));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { BFloat16.MaxValue, BFloat16.MaxValue, true };
            yield return new object[] { BFloat16.MaxValue, BFloat16.MinValue, false };
            yield return new object[] { BFloat16.MaxValue, UInt16BitsToBFloat16(0x0000), false };
            yield return new object[] { BFloat16.MaxValue, 789.0f, false };
            yield return new object[] { BFloat16.MaxValue, "789", false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(BFloat16 value, object obj, bool expected)
        {
            Assert.Equal(expected, value.Equals(obj));
        }
    }
}
