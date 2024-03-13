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
            yield return new object[] { BFloat16.MaxValue, BFloat16.MaxValue, 0 };
            yield return new object[] { BFloat16.MaxValue, BFloat16.MinValue, 1 };
            yield return new object[] { BFloat16.Epsilon, UInt16BitsToBFloat16(0x8001), 1 };
            yield return new object[] { BFloat16.MaxValue, UInt16BitsToBFloat16(0x0000), 1 };
            yield return new object[] { BFloat16.MaxValue, BFloat16.Epsilon, 1 };
            // yield return new object[] { BFloat16.MaxValue, BFloat16.PositiveInfinity, -1 };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, -1 };
            // yield return new object[] { BFloat16.MaxValue, BFloat16.NaN, 1 };
            // yield return new object[] { BFloat16.NaN, BFloat16.NaN, 0 };
            // yield return new object[] { BFloat16.NaN, UInt16BitsToBFloat16(0x0000), -1 };
            yield return new object[] { BFloat16.MaxValue, null, 1 };
            // yield return new object[] { BFloat16.MinValue, BFloat16.NegativeInfinity, 1 };
            // yield return new object[] { BFloat16.NegativeInfinity, BFloat16.MinValue, -1 };
            // yield return new object[] { UInt16BitsToBFloat16(0x8000), BFloat16.NegativeInfinity, 1 }; // Negative zero
            // yield return new object[] { BFloat16.NegativeInfinity, UInt16BitsToBFloat16(0x8000), -1 }; // Negative zero
            // yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NegativeInfinity, 0 };
            // yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, 0 };
            yield return new object[] { (BFloat16)(-180f), (BFloat16)(-180f), 0 };
            yield return new object[] { (BFloat16)(180f), (BFloat16)(180f), 0 };
            yield return new object[] { (BFloat16)(-180f), (BFloat16)(180f), -1 };
            yield return new object[] { (BFloat16)(180f), (BFloat16)(-180f), 1 };
            yield return new object[] { (BFloat16)(-65535), (object)null, 1 };
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

        public static IEnumerable<object[]> ExplicitConversion_ToSingle_TestData()
        {
            (BFloat16 Original, float Expected)[] data = // Fraction is truncated for lower 16 bits
            {
                (UInt16BitsToBFloat16(0b0_01111111_0000000), 1f), // 1
                (UInt16BitsToBFloat16(0b1_01111111_0000000), -1f), // -1
                (BFloat16.MaxValue, BitConverter.UInt32BitsToSingle(0x7F7F0000)), // 3.3895314E+38
                (BFloat16.MinValue, BitConverter.UInt32BitsToSingle(0xFF7F0000)), // -3.3895314E+38
                (UInt16BitsToBFloat16(0b0_01111011_1001101), 0.10009765625f), // 0.1ish
                (UInt16BitsToBFloat16(0b1_01111011_1001101), -0.10009765625f), // -0.1ish
                (UInt16BitsToBFloat16(0b0_10000100_0101000), 42f), // 42
                (UInt16BitsToBFloat16(0b1_10000100_0101000), -42f), // -42
                // (BFloat16.PositiveInfinity, float.PositiveInfinity), // PosInfinity
                // (BFloat16.NegativeInfinity, float.NegativeInfinity), // NegInfinity
                (UInt16BitsToBFloat16(0b0_11111111_1000000), BitConverter.UInt32BitsToSingle(0x7FC00000)), // Positive Quiet NaN
                // (BFloat16.NaN, float.NaN), // Negative Quiet NaN
                (UInt16BitsToBFloat16(0b0_11111111_1010101), BitConverter.UInt32BitsToSingle(0x7FD50000)), // Positive Signalling NaN - Should preserve payload
                (UInt16BitsToBFloat16(0b1_11111111_1010101), BitConverter.UInt32BitsToSingle(0xFFD50000)), // Negative Signalling NaN - Should preserve payload
                (BFloat16.Epsilon, BitConverter.UInt32BitsToSingle(0x00010000)), // PosEpsilon = 9.1835E-41
                (UInt16BitsToBFloat16(0), 0), // 0
                (UInt16BitsToBFloat16(0b1_00000000_0000000), -0f), // -0
                (UInt16BitsToBFloat16(0b0_10000000_1001001), 3.140625f), // 3.140625
                (UInt16BitsToBFloat16(0b1_10000000_1001001), -3.140625f), // -3.140625
                (UInt16BitsToBFloat16(0b0_10000000_0101110), 2.71875f), // 2.71875
                (UInt16BitsToBFloat16(0b1_10000000_0101110), -2.71875f), // -2.71875
                (UInt16BitsToBFloat16(0b0_01111111_1000000), 1.5f), // 1.5
                (UInt16BitsToBFloat16(0b1_01111111_1000000), -1.5f), // -1.5
                (UInt16BitsToBFloat16(0b0_01111111_1000001), 1.5078125f), // 1.5078125
                (UInt16BitsToBFloat16(0b1_01111111_1000001), -1.5078125f), // -1.5078125
                (UInt16BitsToBFloat16(0b0_00000001_0000000), BitConverter.UInt32BitsToSingle(0x00800000)), // smallest normal
                (UInt16BitsToBFloat16(0b0_00000000_1111111), BitConverter.UInt32BitsToSingle(0x007F0000)), // largest subnormal
                (UInt16BitsToBFloat16(0b0_00000000_1000000), BitConverter.UInt32BitsToSingle(0x00400000)), // middle subnormal
                (UInt16BitsToBFloat16(0b0_00000000_0111111), BitConverter.UInt32BitsToSingle(0x003F0000)), // just below middle subnormal
                (UInt16BitsToBFloat16(0b0_00000000_0000001), BitConverter.UInt32BitsToSingle(0x00010000)), // smallest subnormal
                (UInt16BitsToBFloat16(0b1_00000000_0000001), BitConverter.UInt32BitsToSingle(0x80010000)), // highest negative subnormal
                (UInt16BitsToBFloat16(0b1_00000000_0111111), BitConverter.UInt32BitsToSingle(0x803F0000)), // just above negative middle subnormal
                (UInt16BitsToBFloat16(0b1_00000000_1000000), BitConverter.UInt32BitsToSingle(0x80400000)), // negative middle subnormal
                (UInt16BitsToBFloat16(0b1_00000000_1111111), BitConverter.UInt32BitsToSingle(0x807F0000)), // lowest negative subnormal
                (UInt16BitsToBFloat16(0b1_00000001_0000000), BitConverter.UInt32BitsToSingle(0x80800000)) // highest negative normal
            };

            foreach ((BFloat16 original, float expected) in data)
            {
                yield return new object[] { original, expected };
            }
        }

        [MemberData(nameof(ExplicitConversion_ToSingle_TestData))]
        [Theory]
        public static void ExplicitConversion_ToSingle(BFloat16 value, float expected) // Check the underlying bits for verifying NaNs
        {
            float f = (float)value;
            AssertExtensions.Equal(expected, f);
        }
        public static IEnumerable<object[]> ExplicitConversion_FromSingle_TestData()
        {
            (float, BFloat16)[] data =
            {
                (MathF.PI, UInt16BitsToBFloat16(0b0_10000000_1001001)), // 3.140625
                (MathF.E, UInt16BitsToBFloat16(0b0_10000000_0101110)), // 2.71875
                (-MathF.PI, UInt16BitsToBFloat16(0b1_10000000_1001001)), // -3.140625
                (-MathF.E, UInt16BitsToBFloat16(0b1_10000000_0101110)), // -2.71875
                (float.MaxValue, UInt16BitsToBFloat16(0b0_11111111_0000000)), // Overflow
                (float.MinValue, UInt16BitsToBFloat16(0b1_11111111_0000000)), // Overflow
                //(float.PositiveInfinity, BFloat16.PositiveInfinity), // Overflow
                //(float.NegativeInfinity, BFloat16.NegativeInfinity), // Overflow
                //(float.NaN, BFloat16.NaN), // Quiet Negative NaN
                (BitConverter.UInt32BitsToSingle(0x7FC00000), UInt16BitsToBFloat16(0b0_11111111_1000000)), // Quiet Positive NaN
                (BitConverter.UInt32BitsToSingle(0xFFD55555), UInt16BitsToBFloat16(0b1_11111111_1010101)), // Signalling Negative NaN
                (BitConverter.UInt32BitsToSingle(0x7FD55555), UInt16BitsToBFloat16(0b0_11111111_1010101)), // Signalling Positive NaN
                (float.Epsilon, UInt16BitsToBFloat16(0)), // Underflow
                (-float.Epsilon, UInt16BitsToBFloat16(0b1_00000000_0000000)), // Underflow
                (1f, UInt16BitsToBFloat16(0b0_01111111_0000000)), // 1
                (-1f, UInt16BitsToBFloat16(0b1_01111111_0000000)), // -1
                (0f, UInt16BitsToBFloat16(0)), // 0
                (-0f, UInt16BitsToBFloat16(0b1_00000000_0000000)), // -0
                (42f, UInt16BitsToBFloat16(0b0_10000100_0101000)), // 42
                (-42f, UInt16BitsToBFloat16(0b1_10000100_0101000)), // -42
                (0.1f, UInt16BitsToBFloat16(0b0_01111011_1001101)), // 0.10009765625
                (-0.1f, UInt16BitsToBFloat16(0b1_01111011_1001101)), // -0.10009765625
                (1.5f, UInt16BitsToBFloat16(0b0_01111111_1000000)), // 1.5
                (-1.5f, UInt16BitsToBFloat16(0b1_01111111_1000000)), // -1.5
                (1.5078125f, UInt16BitsToBFloat16(0b0_01111111_1000001)), // 1.5078125
                (-1.5078125f, UInt16BitsToBFloat16(0b1_01111111_1000001)), // -1.5078125
                (BitConverter.UInt32BitsToSingle(0x00800000), UInt16BitsToBFloat16(0b0_00000001_0000000)), // smallest normal
                (BitConverter.UInt32BitsToSingle(0x007F0000), UInt16BitsToBFloat16(0b0_00000000_1111111)), // largest subnormal
                (BitConverter.UInt32BitsToSingle(0x00400000), UInt16BitsToBFloat16(0b0_00000000_1000000)), // middle subnormal
                (BitConverter.UInt32BitsToSingle(0x003F0000), UInt16BitsToBFloat16(0b0_00000000_0111111)), // just below middle subnormal
                (BitConverter.UInt32BitsToSingle(0x00010000), UInt16BitsToBFloat16(0b0_00000000_0000001)), // smallest subnormal
                (BitConverter.UInt32BitsToSingle(0x80010000), UInt16BitsToBFloat16(0b1_00000000_0000001)), // highest negative subnormal
                (BitConverter.UInt32BitsToSingle(0x803F0000), UInt16BitsToBFloat16(0b1_00000000_0111111)), // just above negative middle subnormal
                (BitConverter.UInt32BitsToSingle(0x80400000), UInt16BitsToBFloat16(0b1_00000000_1000000)), // negative middle subnormal
                (BitConverter.UInt32BitsToSingle(0x807F0000), UInt16BitsToBFloat16(0b1_00000000_1111111)), // lowest negative subnormal
                (BitConverter.UInt32BitsToSingle(0x80800000), UInt16BitsToBFloat16(0b1_00000001_0000000)), // highest negative normal
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000111000000000000001),
                                  UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052+ULP rounds up
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000111000000000000000),
                                  UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052 rounds to even
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000110111111111111111),
                                  UInt16BitsToBFloat16(0b0_10001001_0000011)), // 1052-ULP rounds down
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000101000000000000000),
                                  UInt16BitsToBFloat16(0b0_10001001_0000010)), // 1044 rounds to even
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000110111111111111111),
                                  UInt16BitsToBFloat16(0b1_10001001_0000011)), // -1052+ULP rounds towards zero
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000111000000000000000),
                                  UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052 rounds to even
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000111000000000000001),
                                  UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052-ULP rounds away from zero
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000101000000000000000),
                                  UInt16BitsToBFloat16(0b1_10001001_0000010)), // -1044 rounds to even
                (BitConverter.UInt32BitsToSingle(0b0_00000000_10000111000000000000001),
                                  UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal + ULP rounds up
                (BitConverter.UInt32BitsToSingle(0b0_00000000_10000111000000000000000),
                                  UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal rounds to even
                (BitConverter.UInt32BitsToSingle(0b0_00000000_10000110111111111111111),
                                  UInt16BitsToBFloat16(0b0_00000000_1000011)), // subnormal - ULP rounds down
                (BitConverter.UInt32BitsToSingle(0b1_00000000_10000110111111111111111),
                                  UInt16BitsToBFloat16(0b1_00000000_1000011)), // neg subnormal + ULP rounds higher
                (BitConverter.UInt32BitsToSingle(0b1_00000000_10000111000000000000000),
                                  UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal rounds to even
                (BitConverter.UInt32BitsToSingle(0b1_00000000_10000111000000000000001),
                                  UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal - ULP rounds lower,
                (BitConverter.UInt32BitsToSingle(0b0_00000000_00000000110000000000000),
                                  UInt16BitsToBFloat16(0b0_00000_000000000)), // (BFloat16-precision minimum subnormal / 2) should underflow to zero
            };

            foreach ((float original, BFloat16 expected) in data)
            {
                yield return new object[] { original, expected };
            }
        }

        [MemberData(nameof(ExplicitConversion_FromSingle_TestData))]
        [Theory]
        public static void ExplicitConversion_FromSingle(float f, BFloat16 expected) // Check the underlying bits for verifying NaNs
        {
            BFloat16 b16 = (BFloat16)f;
            AssertExtensions.Equal(BFloat16ToUInt16Bits(expected), BFloat16ToUInt16Bits(b16));
        }
    }
}
