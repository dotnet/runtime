// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Tests;
using System.Text;
using Xunit;

namespace System.Numerics.Tests
{
    public class BFloat16Tests
    {
        private static ushort BFloat16ToUInt16Bits(BFloat16 value) => Unsafe.BitCast<BFloat16, ushort>(value);

        private static BFloat16 UInt16BitsToBFloat16(ushort value) => Unsafe.BitCast<ushort, BFloat16>(value);

        [Fact]
        public static void Epsilon()
        {
            Assert.Equal(0x0001u, BFloat16ToUInt16Bits(BFloat16.Epsilon));
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal(0x7F80u, BFloat16ToUInt16Bits(BFloat16.PositiveInfinity));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal(0xFF80u, BFloat16ToUInt16Bits(BFloat16.NegativeInfinity));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal(0xFFC0u, BFloat16ToUInt16Bits(BFloat16.NaN));
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

        public static IEnumerable<object[]> IsFinite_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, false };                  // Negative Infinity
            yield return new object[] { BFloat16.MinValue, true };                           // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8400), true };   // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x83FF), true };   // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), true };   // Max Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8000), true };   // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), true };   // Positive Zero
            yield return new object[] { BFloat16.Epsilon, true };                            // Min Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x03FF), true };   // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0400), true };   // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, true };                           // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, false };                  // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsFinite_TestData))]
        public static void IsFinite(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsFinite(value));
        }

        public static IEnumerable<object[]> IsInfinity_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, true };                   // Negative Infinity
            yield return new object[] { BFloat16.MinValue, false };                          // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, false };                          // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, true };                   // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsInfinity_TestData))]
        public static void IsInfinity(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsInfinity(value));
        }

        public static IEnumerable<object[]> IsNaN_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, false };                  // Negative Infinity
            yield return new object[] { BFloat16.MinValue, false };                          // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, true };                                // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, false };                          // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, false };                  // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsNaN_TestData))]
        public static void IsNaN(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsNaN(value));
        }

        public static IEnumerable<object[]> IsNegative_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, true };                   // Negative Infinity
            yield return new object[] { BFloat16.MinValue, true };                           // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), true };   // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), true };   // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), true };   // Max Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8000), true };   // Negative Zero
            yield return new object[] { BFloat16.NaN, true };                                // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, false };                          // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, false };                  // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsNegative_TestData))]
        public static void IsNegative(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsNegative(value));
        }

        public static IEnumerable<object[]> IsNegativeInfinity_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, true };                   // Negative Infinity
            yield return new object[] { BFloat16.MinValue, false };                          // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, false };                          // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, false };                  // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsNegativeInfinity_TestData))]
        public static void IsNegativeInfinity(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsNegativeInfinity(value));
        }

        public static IEnumerable<object[]> IsNormal_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, false };                  // Negative Infinity
            yield return new object[] { BFloat16.MinValue, true };                           // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), true };   // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), true };   // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, true };                           // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, false };                  // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsNormal_TestData))]
        public static void IsNormal(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsNormal(value));
        }

        public static IEnumerable<object[]> IsPositiveInfinity_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, false };                  // Negative Infinity
            yield return new object[] { BFloat16.MinValue, false };                          // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, false };                          // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, true };                   // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsPositiveInfinity_TestData))]
        public static void IsPositiveInfinity(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsPositiveInfinity(value));
        }

        public static IEnumerable<object[]> IsSubnormal_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, false };                  // Negative Infinity
            yield return new object[] { BFloat16.MinValue, false };                          // Min Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { UInt16BitsToBFloat16(0x807F), true };   // Min Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8001), true };   // Max Negative Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, true };                            // Min Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x007F), true };   // Max Positive Subnormal
            yield return new object[] { UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
            yield return new object[] { BFloat16.MaxValue, false };                          // Max Positive Normal
            yield return new object[] { BFloat16.PositiveInfinity, false };                  // Positive Infinity
        }

        [Theory]
        [MemberData(nameof(IsSubnormal_TestData))]
        public static void IsSubnormal(BFloat16 value, bool expected)
        {
            Assert.Equal(expected, BFloat16.IsSubnormal(value));
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
            yield return new object[] { BFloat16.MaxValue, BFloat16.PositiveInfinity, -1 };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, -1 };
            yield return new object[] { BFloat16.MaxValue, BFloat16.NaN, 1 };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, 0 };
            yield return new object[] { BFloat16.NaN, UInt16BitsToBFloat16(0x0000), -1 };
            yield return new object[] { BFloat16.MaxValue, null, 1 };
            yield return new object[] { BFloat16.MinValue, BFloat16.NegativeInfinity, 1 };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.MinValue, -1 };
            yield return new object[] { UInt16BitsToBFloat16(0x8000), BFloat16.NegativeInfinity, 1 }; // Negative zero
            yield return new object[] { BFloat16.NegativeInfinity, UInt16BitsToBFloat16(0x8000), -1 }; // Negative zero
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NegativeInfinity, 0 };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, 0 };
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

                if (BFloat16.IsNaN(value) || BFloat16.IsNaN(other))
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
                (BFloat16.PositiveInfinity, float.PositiveInfinity), // PosInfinity
                (BFloat16.NegativeInfinity, float.NegativeInfinity), // NegInfinity
                (UInt16BitsToBFloat16(0b0_11111111_1000000), BitConverter.UInt32BitsToSingle(0x7FC00000)), // Positive Quiet NaN
                (BFloat16.NaN, float.NaN), // Negative Quiet NaN
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

        public static IEnumerable<object[]> ExplicitConversion_ToDouble_TestData()
        {
            (BFloat16 Original, double Expected)[] data =
            {
                (UInt16BitsToBFloat16(0b0_01111111_0000000), 1d), // 1
                (UInt16BitsToBFloat16(0b1_01111111_0000000), -1d), // -1
                (BFloat16.MaxValue, BitConverter.UInt64BitsToDouble(0x47EFE000_00000000)), // 3.3895314E+38
                (BFloat16.MinValue, BitConverter.UInt64BitsToDouble(0xC7EFE000_00000000)), // -3.3895314E+38
                (UInt16BitsToBFloat16(0b0_01111011_1001101), 0.10009765625d), // 0.1ish
                (UInt16BitsToBFloat16(0b1_01111011_1001101), -0.10009765625d), // -0.1ish
                (UInt16BitsToBFloat16(0b0_10000100_0101000), 42d), // 42
                (UInt16BitsToBFloat16(0b1_10000100_0101000), -42d), // -42
                (BFloat16.PositiveInfinity, double.PositiveInfinity), // PosInfinity
                (BFloat16.NegativeInfinity, double.NegativeInfinity), // NegInfinity
                (UInt16BitsToBFloat16(0b0_11111111_1000000), BitConverter.UInt64BitsToDouble(0x7FF80000_00000000)), // Positive Quiet NaN
                (BFloat16.NaN, double.NaN), // Negative Quiet NaN
                (UInt16BitsToBFloat16(0b0_11111111_1010101), BitConverter.UInt64BitsToDouble(0x7FFAA000_00000000)), // Positive Signalling NaN - Should preserve payload
                (UInt16BitsToBFloat16(0b1_11111111_1010101), BitConverter.UInt64BitsToDouble(0xFFFAA000_00000000)), // Negative Signalling NaN - Should preserve payload
                (BFloat16.Epsilon, BitConverter.UInt64BitsToDouble(0x37A00000_00000000)), // PosEpsilon = 9.1835E-41
                (UInt16BitsToBFloat16(0), 0d), // 0
                (UInt16BitsToBFloat16(0b1_00000000_0000000), -0d), // -0
                (UInt16BitsToBFloat16(0b0_10000000_1001001), 3.140625d), // 3.140625
                (UInt16BitsToBFloat16(0b1_10000000_1001001), -3.140625d), // -3.140625
                (UInt16BitsToBFloat16(0b0_10000000_0101110), 2.71875d), // 2.71875
                (UInt16BitsToBFloat16(0b1_10000000_0101110), -2.71875d), // -2.71875
                (UInt16BitsToBFloat16(0b0_01111111_1000000), 1.5d), // 1.5
                (UInt16BitsToBFloat16(0b1_01111111_1000000), -1.5d), // -1.5
                (UInt16BitsToBFloat16(0b0_01111111_1000001), 1.5078125d), // 1.5078125
                (UInt16BitsToBFloat16(0b1_01111111_1000001), -1.5078125d), // -1.5078125
                (UInt16BitsToBFloat16(0b0_00000001_0000000), BitConverter.UInt64BitsToDouble(0x3810000000000000)), // smallest normal
                (UInt16BitsToBFloat16(0b0_00000000_1111111), BitConverter.UInt64BitsToDouble(0x380FC00000000000)), // largest subnormal
                (UInt16BitsToBFloat16(0b0_00000000_1000000), BitConverter.UInt64BitsToDouble(0x3800000000000000)), // middle subnormal
                (UInt16BitsToBFloat16(0b0_00000000_0111111), BitConverter.UInt64BitsToDouble(0x37FF800000000000)), // just below middle subnormal
                (UInt16BitsToBFloat16(0b0_00000000_0000001), BitConverter.UInt64BitsToDouble(0x37A0000000000000)), // smallest subnormal
                (UInt16BitsToBFloat16(0b1_00000000_0000001), BitConverter.UInt64BitsToDouble(0xB7A0000000000000)), // highest negative subnormal
                (UInt16BitsToBFloat16(0b1_00000000_0111111), BitConverter.UInt64BitsToDouble(0xB7FF800000000000)), // just above negative middle subnormal
                (UInt16BitsToBFloat16(0b1_00000000_1000000), BitConverter.UInt64BitsToDouble(0xB800000000000000)), // negative middle subnormal
                (UInt16BitsToBFloat16(0b1_00000000_1111111), BitConverter.UInt64BitsToDouble(0xB80FC00000000000)), // lowest negative subnormal
                (UInt16BitsToBFloat16(0b1_00000001_0000000), BitConverter.UInt64BitsToDouble(0xB810000000000000)) // highest negative normal
            };

            foreach ((BFloat16 original, double expected) in data)
            {
                yield return new object[] { original, expected };
            }
        }

        [MemberData(nameof(ExplicitConversion_ToDouble_TestData))]
        [Theory]
        public static void ExplicitConversion_ToDouble(BFloat16 value, double expected) // Check the underlying bits for verifying NaNs
        {
            double d = (double)value;
            AssertExtensions.Equal(expected, d);
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
                (float.PositiveInfinity, BFloat16.PositiveInfinity), // Overflow
                (float.NegativeInfinity, BFloat16.NegativeInfinity), // Overflow
                (float.NaN, BFloat16.NaN), // Quiet Negative NaN
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
                                  UInt16BitsToBFloat16(0b0_00000_000000000)), // (BFloat16 minimum subnormal / 2) should underflow to zero
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

        public static IEnumerable<object[]> ExplicitConversion_FromDouble_TestData()
        {
            (double, BFloat16)[] data =
            {
                (Math.PI, UInt16BitsToBFloat16(0b0_10000000_1001001)), // 3.140625
                (Math.E, UInt16BitsToBFloat16(0b0_10000000_0101110)), // 2.71875
                (-Math.PI, UInt16BitsToBFloat16(0b1_10000000_1001001)), // -3.140625
                (-Math.E, UInt16BitsToBFloat16(0b1_10000000_0101110)), // -2.71875
                (double.MaxValue, BFloat16.PositiveInfinity), // Overflow
                (double.MinValue, BFloat16.NegativeInfinity), // Overflow
                (double.PositiveInfinity, BFloat16.PositiveInfinity), // Overflow
                (double.NegativeInfinity, BFloat16.NegativeInfinity), // Overflow
                (double.NaN, BFloat16.NaN), // Quiet Negative NaN
                (BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), UInt16BitsToBFloat16(0b0_11111111_1000000)), // Quiet Positive NaN
                (BitConverter.UInt64BitsToDouble(0xFFFAAAAA_AAAAAAAA), UInt16BitsToBFloat16(0b1_11111111_1010101)), // Signalling Negative NaN
                (BitConverter.UInt64BitsToDouble(0x7FFAAAAA_AAAAAAAA), UInt16BitsToBFloat16(0b0_11111111_1010101)), // Signalling Positive NaN
                (double.Epsilon, UInt16BitsToBFloat16(0)), // Underflow
                (-double.Epsilon, UInt16BitsToBFloat16(0b1_00000000_0000000)), // Underflow                (1f, UInt16BitsToBFloat16(0b0_01111111_0000000)), // 1
                (-1d, UInt16BitsToBFloat16(0b1_01111111_0000000)), // -1
                (0d, UInt16BitsToBFloat16(0)), // 0
                (-0d, UInt16BitsToBFloat16(0b1_00000000_0000000)), // -0
                (42d, UInt16BitsToBFloat16(0b0_10000100_0101000)), // 42
                (-42d, UInt16BitsToBFloat16(0b1_10000100_0101000)), // -42
                (0.1d, UInt16BitsToBFloat16(0b0_01111011_1001101)), // 0.10009765625
                (-0.1d, UInt16BitsToBFloat16(0b1_01111011_1001101)), // -0.10009765625
                (1.5d, UInt16BitsToBFloat16(0b0_01111111_1000000)), // 1.5
                (-1.5d, UInt16BitsToBFloat16(0b1_01111111_1000000)), // -1.5
                (1.5078125d, UInt16BitsToBFloat16(0b0_01111111_1000001)), // 1.5078125
                (-1.5078125d, UInt16BitsToBFloat16(0b1_01111111_1000001)), // -1.5078125
                (BitConverter.UInt64BitsToDouble(0x3810000000000000), UInt16BitsToBFloat16(0b0_00000001_0000000)), // smallest normal
                (BitConverter.UInt64BitsToDouble(0x380FC00000000000), UInt16BitsToBFloat16(0b0_00000000_1111111)), // largest subnormal
                (BitConverter.UInt64BitsToDouble(0x3800000000000000), UInt16BitsToBFloat16(0b0_00000000_1000000)), // middle subnormal
                (BitConverter.UInt64BitsToDouble(0x37FF800000000000), UInt16BitsToBFloat16(0b0_00000000_0111111)), // just below middle subnormal
                (BitConverter.UInt64BitsToDouble(0x37A0000000000000), UInt16BitsToBFloat16(0b0_00000000_0000001)), // smallest subnormal
                (BitConverter.UInt64BitsToDouble(0xB7A0000000000000), UInt16BitsToBFloat16(0b1_00000000_0000001)), // highest negative subnormal
                (BitConverter.UInt64BitsToDouble(0xB7FF800000000000), UInt16BitsToBFloat16(0b1_00000000_0111111)), // just above negative middle subnormal
                (BitConverter.UInt64BitsToDouble(0xB800000000000000), UInt16BitsToBFloat16(0b1_00000000_1000000)), // negative middle subnormal
                (BitConverter.UInt64BitsToDouble(0xB80FC00000000000), UInt16BitsToBFloat16(0b1_00000000_1111111)), // lowest negative subnormal
                (BitConverter.UInt64BitsToDouble(0xB810000000000000), UInt16BitsToBFloat16(0b1_00000001_0000000)), // highest negative normal
                (BitConverter.UInt64BitsToDouble(0x4090700000000001),
                    UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052+ULP rounds up
                (BitConverter.UInt64BitsToDouble(0x4090700000000000),
                    UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052 rounds to even
                (BitConverter.UInt64BitsToDouble(0x40906FFFFFFFFFFF),
                    UInt16BitsToBFloat16(0b0_10001001_0000011)), // 1052-ULP rounds down
                (BitConverter.UInt64BitsToDouble(0x4090500000000000),
                    UInt16BitsToBFloat16(0b0_10001001_0000010)), // 1044 rounds to even
                (BitConverter.UInt64BitsToDouble(0xC0906FFFFFFFFFFF),
                    UInt16BitsToBFloat16(0b1_10001001_0000011)), // -1052+ULP rounds towards zero
                (BitConverter.UInt64BitsToDouble(0xC090700000000000),
                    UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052 rounds to even
                (BitConverter.UInt64BitsToDouble(0xC090700000000001),
                    UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052-ULP rounds away from zero
                (BitConverter.UInt64BitsToDouble(0xC090500000000000),
                    UInt16BitsToBFloat16(0b1_10001001_0000010)), // -1044 rounds to even
                (BitConverter.UInt64BitsToDouble(0x3800E00000000001),
                    UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal + ULP rounds up
                (BitConverter.UInt64BitsToDouble(0x3800E00000000000),
                    UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal rounds to even
                (BitConverter.UInt64BitsToDouble(0x3800DFFFFFFFFFFF),
                    UInt16BitsToBFloat16(0b0_00000000_1000011)), // subnormal - ULP rounds down
                (BitConverter.UInt64BitsToDouble(0xB800DFFFFFFFFFFF),
                    UInt16BitsToBFloat16(0b1_00000000_1000011)), // neg subnormal + ULP rounds higher
                (BitConverter.UInt64BitsToDouble(0xB800E00000000000),
                    UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal rounds to even
                (BitConverter.UInt64BitsToDouble(0xB800E00000000001),
                    UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal - ULP rounds lower
                (BitConverter.UInt64BitsToDouble(0x3788000000000000), UInt16BitsToBFloat16(0b0_00000_000000000)), // (BFloat16 minimum subnormal / 2) should underflow to zero
            };

            foreach ((double original, BFloat16 expected) in data)
            {
                yield return new object[] { original, expected };
            }
        }

        [MemberData(nameof(ExplicitConversion_FromDouble_TestData))]
        [Theory]
        public static void ExplicitConversion_FromDouble(double d, BFloat16 expected) // Check the underlying bits for verifying NaNs
        {
            BFloat16 b16 = (BFloat16)d;
            AssertExtensions.Equal(BFloat16ToUInt16Bits(expected), BFloat16ToUInt16Bits(b16));
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;

            var dollarSignCommaSeparatorFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = ","
            };

            var decimalSeparatorFormat = new NumberFormatInfo()
            {
                NumberDecimalSeparator = "."
            };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;

            yield return new object[] { "-123", defaultStyle, null, -123.0f };
            yield return new object[] { "0", defaultStyle, null, 0.0f };
            yield return new object[] { "123", defaultStyle, null, 123.0f };
            yield return new object[] { "  123  ", defaultStyle, null, 123.0f };
            yield return new object[] { (567.89f).ToString(), defaultStyle, null, 567.89f };
            yield return new object[] { (-567.89f).ToString(), defaultStyle, null, -567.89f };
            yield return new object[] { "1E23", defaultStyle, null, 1E23f };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, 0.234f };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 234.0f };
            yield return new object[] { new string('0', 13) + "338953138925153547590470800371487866880" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 3.3895314e38f };
            yield return new object[] { new string('0', 14) + "338953138925153547590470800371487866880" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 3.3895314e38f };

            // 2^11 + 1. Not exactly representable
            yield return new object[] { "2049.0", defaultStyle, invariantFormat, 2048.0f };
            yield return new object[] { "2049.000000000000001", defaultStyle, invariantFormat, 2050.0f };
            yield return new object[] { "2049.0000000000000001", defaultStyle, invariantFormat, 2050.0f };
            yield return new object[] { "2049.00000000000000001", defaultStyle, invariantFormat, 2050.0f };
            yield return new object[] { "5.000000000000000004", defaultStyle, invariantFormat, 5.0f };
            yield return new object[] { "5.0000000000000000004", defaultStyle, invariantFormat, 5.0f };
            yield return new object[] { "5.004", defaultStyle, invariantFormat, 5.004f };
            yield return new object[] { "5.004000000000000000", defaultStyle, invariantFormat, 5.004f };
            yield return new object[] { "5.0040000000000000000", defaultStyle, invariantFormat, 5.004f };
            yield return new object[] { "5.040", defaultStyle, invariantFormat, 5.04f };

            yield return new object[] { "5004.000000000000000", defaultStyle, invariantFormat, 5004.0f };
            yield return new object[] { "50040.0", defaultStyle, invariantFormat, 50040.0f };
            yield return new object[] { "5004", defaultStyle, invariantFormat, 5004.0f };
            yield return new object[] { "050040", defaultStyle, invariantFormat, 50040.0f };
            yield return new object[] { "0.000000000000000000", defaultStyle, invariantFormat, 0.0f };
            yield return new object[] { "0.005", defaultStyle, invariantFormat, 0.005f };
            yield return new object[] { "0.0400", defaultStyle, invariantFormat, 0.04f };
            yield return new object[] { "1200e0", defaultStyle, invariantFormat, 1200.0f };
            yield return new object[] { "120100e-4", defaultStyle, invariantFormat, 12.01f };
            yield return new object[] { "12010.00e-4", defaultStyle, invariantFormat, 1.201f };
            yield return new object[] { "12000e-4", defaultStyle, invariantFormat, 1.2f };
            yield return new object[] { "1200", defaultStyle, invariantFormat, 1200.0f };

            yield return new object[] { (123.1f).ToString(), NumberStyles.AllowDecimalPoint, null, 123.1f };
            yield return new object[] { (1000.0f).ToString("N0"), NumberStyles.AllowThousands, null, 1000.0f };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, 123.0f };
            yield return new object[] { (123.567f).ToString(), NumberStyles.Any, emptyFormat, 123.567f };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, 123.0f };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0f };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0f };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, 123.123f };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, -123.0f };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, float.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, float.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, float.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, float expectedFloat)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            BFloat16 result;
            BFloat16 expected = (BFloat16)expectedFloat;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(BFloat16.TryParse(value, out result));
                    Assert.True(expected.Equals(result));

                    Assert.Equal(expected, BFloat16.Parse(value));
                }

                Assert.True(expected.Equals(BFloat16.Parse(value, provider: provider)));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(BFloat16.TryParse(value, style, provider, out result));
            Assert.True(expected.Equals(result) || (BFloat16.IsNaN(expected) && BFloat16.IsNaN(result)));

            Assert.True(expected.Equals(BFloat16.Parse(value, style, provider)) || (BFloat16.IsNaN(expected) && BFloat16.IsNaN(result)));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(BFloat16.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.True(expected.Equals(result));

                Assert.True(expected.Equals(BFloat16.Parse(value, style)));
                Assert.True(expected.Equals(BFloat16.Parse(value, style, NumberFormatInfo.CurrentInfo)));
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var dollarSignDecimalSeparatorFormat = new NumberFormatInfo();
            dollarSignDecimalSeparatorFormat.CurrencySymbol = "$";
            dollarSignDecimalSeparatorFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { (100.0f).ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456f).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456f).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456f).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Negative hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            BFloat16 result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(BFloat16.TryParse(value, out result));
                    Assert.Equal(default(BFloat16), result);

                    Assert.Throws(exceptionType, () => BFloat16.Parse(value));
                }

                Assert.Throws(exceptionType, () => BFloat16.Parse(value, provider: provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(BFloat16.TryParse(value, style, provider, out result));
            Assert.Equal(default(BFloat16), result);

            Assert.Throws(exceptionType, () => BFloat16.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(BFloat16.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(BFloat16), result);

                Assert.Throws(exceptionType, () => BFloat16.Parse(value, style));
                Assert.Throws(exceptionType, () => BFloat16.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            const NumberStyles DefaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            yield return new object[] { "-123", 1, 3, DefaultStyle, null, (float)123 };
            yield return new object[] { "-123", 0, 3, DefaultStyle, null, (float)-12 };
            yield return new object[] { "1E23", 0, 3, DefaultStyle, null, (float)1E2 };
            yield return new object[] { "123", 0, 2, NumberStyles.Float, new NumberFormatInfo(), (float)12 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$", CurrencyGroupSeparator = "," }, (float)10 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, (float)123 };
            yield return new object[] { "-Infinity", 1, 8, NumberStyles.Any, NumberFormatInfo.InvariantInfo, float.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, float expectedFloat)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            BFloat16 result;
            BFloat16 expected = (BFloat16)expectedFloat;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(BFloat16.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, BFloat16.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, BFloat16.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.True(expected.Equals(BFloat16.Parse(value.AsSpan(offset, count), style, provider)) || (BFloat16.IsNaN(expected) && BFloat16.IsNaN(BFloat16.Parse(value.AsSpan(offset, count), style, provider))));

            Assert.True(BFloat16.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.True(expected.Equals(result) || (BFloat16.IsNaN(expected) && BFloat16.IsNaN(result)));
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => float.Parse(value.AsSpan(), style, provider));

                Assert.False(float.TryParse(value.AsSpan(), style, provider, out float result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, float expectedFloat)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;

            BFloat16 result;
            BFloat16 expected = (BFloat16)expectedFloat;

            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(BFloat16.TryParse(valueUtf8, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, BFloat16.Parse(valueUtf8));
                }

                Assert.Equal(expected, BFloat16.Parse(valueUtf8, provider: provider));
            }

            Assert.True(expected.Equals(BFloat16.Parse(valueUtf8, style, provider)) || (BFloat16.IsNaN(expected) && BFloat16.IsNaN(BFloat16.Parse(value.AsSpan(offset, count), style, provider))));

            Assert.True(BFloat16.TryParse(valueUtf8, style, provider, out result));
            Assert.True(expected.Equals(result) || (BFloat16.IsNaN(expected) && BFloat16.IsNaN(result)));
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);
                Exception e = Assert.Throws(exceptionType, () => BFloat16.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(float.TryParse(valueUtf8, style, provider, out float result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => BFloat16.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { -4580.0f, "G", null, "-4580" };
            yield return new object[] { 0.0f, "G", null, "0" };
            yield return new object[] { 4580.0f, "G", null, "4580" };

            yield return new object[] { float.NaN, "G", null, "NaN" };

            yield return new object[] { 2464.0f, "N", null, "2,464.00" };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customNegativePattern = new NumberFormatInfo() { NumberNegativePattern = 0 };
            yield return new object[] { -6300.0f, "G", customNegativePattern, "-6300" };

            var customNegativeSignDecimalGroupSeparator = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*"
            };
            yield return new object[] { -2464.0f, "N", customNegativeSignDecimalGroupSeparator, "#2*464~00" };
            yield return new object[] { 2464.0f, "N", customNegativeSignDecimalGroupSeparator, "2*464~00" };

            var customNegativeSignGroupSeparatorNegativePattern = new NumberFormatInfo()
            {
                NegativeSign = "xx", // Set to trash to make sure it doesn't show up
                NumberGroupSeparator = "*",
                NumberNegativePattern = 0
            };
            yield return new object[] { -2464.0f, "N", customNegativeSignGroupSeparatorNegativePattern, "(2*464.00)" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { float.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { float.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { float.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }

        public static IEnumerable<object[]> ToString_TestData_NotNetFramework()
        {
            foreach (var testData in ToString_TestData())
            {
                yield return testData;
            }

            yield return new object[] { BFloat16.MinValue, "G", null, "-3.39E+38" };
            yield return new object[] { BFloat16.MaxValue, "G", null, "3.39E+38" };

            yield return new object[] { BFloat16.Epsilon, "G", null, "1E-40" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { BFloat16.Epsilon, "G", invariantFormat, "1E-40" };

            yield return new object[] { 32.5f, "C100", invariantFormat, "\u00A432.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5f, "P100", invariantFormat, "3,250.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { 32.5f, "E100", invariantFormat, "3.2500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { 32.5f, "F100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5f, "N100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
        }

        [Fact]
        public static void Test_ToString_NotNetFramework()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData_NotNetFramework())
                {
                    ToStringTest(testdata[0] is float floatData ? (BFloat16)floatData : (BFloat16)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void ToStringTest(BFloat16 f, string format, IFormatProvider provider, string expected)
        {
            bool isDefaultProvider = provider == null;
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(expected, f.ToString());
                    Assert.Equal(expected, f.ToString((IFormatProvider)null));
                }
                Assert.Equal(expected, f.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant())); // If format is upper case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant())); // If format is lower case, then exponents are printed in lower case
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), null));
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), null));
            }
            Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), provider));
            Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            BFloat16 f = (BFloat16)123.0f;
            Assert.Throws<FormatException>(() => f.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => f.ToString("Y", null)); // Invalid format
            long intMaxPlus1 = (long)int.MaxValue + 1;
            string intMaxPlus1String = intMaxPlus1.ToString();
            Assert.Throws<FormatException>(() => f.ToString("E" + intMaxPlus1String));
        }

        [Fact]
        public static void TryFormat()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData())
                {
                    float localI = (float)testdata[0];
                    string localFormat = (string)testdata[1];
                    IFormatProvider localProvider = (IFormatProvider)testdata[2];
                    string localExpected = (string)testdata[3];

                    try
                    {
                        NumberFormatTestHelper.TryFormatNumberTest(localI, localFormat, localProvider, localExpected, formatCasingMatchesOutput: false);
                    }
                    catch (Exception exc)
                    {
                        throw new Exception($"Failed on `{localI}`, `{localFormat}`, `{localProvider}`, `{localExpected}`. {exc}");
                    }
                }
            }
        }
    }
}
