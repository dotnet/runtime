// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Tests;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace System.Numerics.Tests
{
    public class BFloat16Tests
    {
        private static BFloat16 CrossPlatformMachineEpsilon => (BFloat16)3.90625e-03f;

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
            AssertEqual(expected, b16);
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
            AssertEqual(expected, b16);
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

        public static IEnumerable<object[]> ToStringRoundtrip_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.MinValue };
            yield return new object[] { -MathF.PI };
            yield return new object[] { -MathF.E };
            yield return new object[] { -0.845512408f };
            yield return new object[] { -0.0f };
            yield return new object[] { BFloat16.NaN };
            yield return new object[] { 0.0f };
            yield return new object[] { 0.845512408f };
            yield return new object[] { BFloat16.Epsilon };
            yield return new object[] { MathF.E };
            yield return new object[] { MathF.PI };
            yield return new object[] { BFloat16.MaxValue };
            yield return new object[] { BFloat16.PositiveInfinity };

            yield return new object[] { (UInt16BitsToBFloat16(0b0_00000001_0000000)) }; // smallest normal
            yield return new object[] { (UInt16BitsToBFloat16(0b0_00000000_1111111)) }; // largest subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b0_00000000_1000000)) }; // middle subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b0_00000000_0111111)) }; // just below middle subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b0_00000000_0000000)) }; // smallest subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b1_00000000_0000000)) }; // highest negative subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b1_00000000_0111111)) }; // just above negative middle subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b1_00000000_1000000)) }; // negative middle subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b1_00000000_1111111)) }; // lowest negative subnormal
            yield return new object[] { (UInt16BitsToBFloat16(0b1_00000001_0000000)) }; // highest negative normal
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip(object o_value)
        {
            float value = o_value is float floatValue ? floatValue : (float)(BFloat16)o_value;
            BFloat16 result = BFloat16.Parse(value.ToString());
            AssertEqual((BFloat16)value, result);
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip_R(object o_value)
        {
            float value = o_value is float floatValue ? floatValue : (float)(BFloat16)o_value;
            BFloat16 result = BFloat16.Parse(value.ToString("R"));
            AssertEqual((BFloat16)value, result);
        }

        public static IEnumerable<object[]> RoundTripFloat_CornerCases()
        {
            // Magnitude smaller than 2^-133 maps to 0
            yield return new object[] { (BFloat16)(4.6e-41f), 0 };
            yield return new object[] { (BFloat16)(-4.6e-41f), 0 };
            // Magnitude smaller than 2^(map to subnormals
            yield return new object[] { (BFloat16)(0.567e-39f), 0.567e-39f };
            yield return new object[] { (BFloat16)(-0.567e-39f), -0.567e-39f };
            // Normal numbers
            yield return new object[] { (BFloat16)(55.77f), 55.75f };
            yield return new object[] { (BFloat16)(-55.77f), -55.75f };
            // Magnitude smaller than 2^(map to infinity
            yield return new object[] { (BFloat16)(float.BitDecrement(float.PositiveInfinity)), float.PositiveInfinity };
            yield return new object[] { (BFloat16)(float.BitIncrement(float.NegativeInfinity)), float.NegativeInfinity };
            // Infinity and NaN map to infinity and Nan
            yield return new object[] { BFloat16.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { BFloat16.NaN, float.NaN };
        }

        [Theory]
        [MemberData(nameof(RoundTripFloat_CornerCases))]
        public static void ToSingle(BFloat16 BFloat16, float verify)
        {
            float f = (float)BFloat16;
            Assert.Equal(f, verify, precision: 1);
        }

        [Fact]
        public static void EqualityMethodAndOperator()
        {
            Assert.True(BFloat16.NaN.Equals(BFloat16.NaN));
            Assert.False(BFloat16.NaN == BFloat16.NaN);
            Assert.Equal(BFloat16.NaN, BFloat16.NaN);
        }


        public static IEnumerable<object[]> MaxMagnitudeNumber_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NegativeInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, BFloat16.MaxValue };
            yield return new object[] { BFloat16.MaxValue, BFloat16.MinValue, BFloat16.MaxValue };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, (BFloat16)BFloat16.One, (BFloat16)BFloat16.One };
            yield return new object[] { (BFloat16)BFloat16.One, BFloat16.NaN, (BFloat16)BFloat16.One };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)0.0f, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)0.0f, (BFloat16)(-0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)2.0f, (BFloat16)(-3.0f), (BFloat16)(-3.0f) };
            yield return new object[] { (BFloat16)(-3.0f), (BFloat16)2.0f, (BFloat16)(-3.0f) };
            yield return new object[] { (BFloat16)3.0f, (BFloat16)(-2.0f), (BFloat16)3.0f };
            yield return new object[] { (BFloat16)(-2.0f), (BFloat16)3.0f, (BFloat16)3.0f };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitudeNumber_TestData))]
        public static void MaxMagnitudeNumberTest(BFloat16 x, BFloat16 y, BFloat16 expectedResult)
        {
            AssertEqual(expectedResult, BFloat16.MaxMagnitudeNumber(x, y), (BFloat16)0.0f);
        }

        public static IEnumerable<object[]> MaxNumber_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NegativeInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, BFloat16.MaxValue };
            yield return new object[] { BFloat16.MaxValue, BFloat16.MinValue, BFloat16.MaxValue };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, (BFloat16)BFloat16.One, (BFloat16)BFloat16.One };
            yield return new object[] { (BFloat16)BFloat16.One, BFloat16.NaN, (BFloat16)BFloat16.One };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)0.0f, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)0.0f, (BFloat16)(-0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)2.0f, (BFloat16)(-3.0f), (BFloat16)2.0f };
            yield return new object[] { (BFloat16)(-3.0f), (BFloat16)2.0f, (BFloat16)2.0f };
            yield return new object[] { (BFloat16)3.0f, (BFloat16)(-2.0f), (BFloat16)3.0f };
            yield return new object[] { (BFloat16)(-2.0f), (BFloat16)3.0f, (BFloat16)3.0f };
        }

        [Theory]
        [MemberData(nameof(MaxNumber_TestData))]
        public static void MaxNumberTest(BFloat16 x, BFloat16 y, BFloat16 expectedResult)
        {
            AssertEqual(expectedResult, BFloat16.MaxNumber(x, y), (BFloat16)0.0f);
        }

        public static IEnumerable<object[]> MinMagnitudeNumber_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.PositiveInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, BFloat16.MinValue };
            yield return new object[] { BFloat16.MaxValue, BFloat16.MinValue, BFloat16.MinValue };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, (BFloat16)BFloat16.One, (BFloat16)BFloat16.One };
            yield return new object[] { (BFloat16)BFloat16.One, BFloat16.NaN, (BFloat16)BFloat16.One };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)0.0f, (BFloat16)(-0.0f) };
            yield return new object[] { (BFloat16)0.0f, (BFloat16)(-0.0f), (BFloat16)(-0.0f) };
            yield return new object[] { (BFloat16)2.0f, (BFloat16)(-3.0f), (BFloat16)2.0f };
            yield return new object[] { (BFloat16)(-3.0f), (BFloat16)2.0f, (BFloat16)2.0f };
            yield return new object[] { (BFloat16)3.0f, (BFloat16)(-2.0f), (BFloat16)(-2.0f) };
            yield return new object[] { (BFloat16)(-2.0f), (BFloat16)3.0f, (BFloat16)(-2.0f) };
        }

        [Theory]
        [MemberData(nameof(MinMagnitudeNumber_TestData))]
        public static void MinMagnitudeNumberTest(BFloat16 x, BFloat16 y, BFloat16 expectedResult)
        {
            AssertEqual(expectedResult, BFloat16.MinMagnitudeNumber(x, y), (BFloat16)0.0f);
        }

        public static IEnumerable<object[]> MinNumber_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.PositiveInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, BFloat16.MinValue };
            yield return new object[] { BFloat16.MaxValue, BFloat16.MinValue, BFloat16.MinValue };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, (BFloat16)BFloat16.One, (BFloat16)BFloat16.One };
            yield return new object[] { (BFloat16)BFloat16.One, BFloat16.NaN, (BFloat16)BFloat16.One };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)0.0f, (BFloat16)(-0.0f) };
            yield return new object[] { (BFloat16)0.0f, (BFloat16)(-0.0f), (BFloat16)(-0.0f) };
            yield return new object[] { (BFloat16)2.0f, (BFloat16)(-3.0f), (BFloat16)(-3.0f) };
            yield return new object[] { (BFloat16)(-3.0f), (BFloat16)2.0f, (BFloat16)(-3.0f) };
            yield return new object[] { (BFloat16)3.0f, (BFloat16)(-2.0f), (BFloat16)(-2.0f) };
            yield return new object[] { (BFloat16)(-2.0f), (BFloat16)3.0f, (BFloat16)(-2.0f) };
        }

        [Theory]
        [MemberData(nameof(MinNumber_TestData))]
        public static void MinNumberTest(BFloat16 x, BFloat16 y, BFloat16 expectedResult)
        {
            AssertEqual(expectedResult, BFloat16.MinNumber(x, y), (BFloat16)0.0f);
        }

        public static IEnumerable<object[]> ExpM1_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)(-BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-3.14159265f), (BFloat16)(-0.956786082f), CrossPlatformMachineEpsilon };             // value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), (BFloat16)(-0.934011964f), CrossPlatformMachineEpsilon };             // value: -(e)
            yield return new object[] { (BFloat16)(-2.30258509f), (BFloat16)(-0.9f), CrossPlatformMachineEpsilon };             // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57079633f), (BFloat16)(-0.792120424f), CrossPlatformMachineEpsilon };             // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.44269504f), (BFloat16)(-0.763709912f), CrossPlatformMachineEpsilon };             // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.41421356f), (BFloat16)(-0.756883266f), CrossPlatformMachineEpsilon };             // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.12837917f), (BFloat16)(-0.676442736f), CrossPlatformMachineEpsilon };             // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(-0.632120559f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.785398163f), (BFloat16)(-0.544061872f), CrossPlatformMachineEpsilon };             // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707106781f), (BFloat16)(-0.506931309f), CrossPlatformMachineEpsilon };             // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.693147181f), (BFloat16)(-0.5f), CrossPlatformMachineEpsilon };             // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.636619772f), (BFloat16)(-0.470922192f), CrossPlatformMachineEpsilon };             // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.434294482f), (BFloat16)(-0.352278515f), CrossPlatformMachineEpsilon };             // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.318309886f), (BFloat16)(-0.272622651f), CrossPlatformMachineEpsilon };             // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.318309886f), (BFloat16)(0.374802227f), CrossPlatformMachineEpsilon };             // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.434294482f), (BFloat16)(0.543873444f), CrossPlatformMachineEpsilon };             // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.636619772f), (BFloat16)(0.890081165f), CrossPlatformMachineEpsilon };             // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.693147181f), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707106781f), (BFloat16)(1.02811498f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.785398163f), (BFloat16)(1.19328005f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(1.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.12837917f), (BFloat16)(2.09064302f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.41421356f), (BFloat16)(3.11325038f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.44269504f), (BFloat16)(3.23208611f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57079633f), (BFloat16)(3.81047738f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.30258509f), (BFloat16)(9.0f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.71828183f), (BFloat16)(14.1542622f), CrossPlatformMachineEpsilon * (BFloat16)100 }; // value:  (e)
            yield return new object[] { (BFloat16)(3.14159265f), (BFloat16)(22.1406926f), CrossPlatformMachineEpsilon * (BFloat16)100 }; // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, 0.0 };
        }

        [Theory]
        [MemberData(nameof(ExpM1_TestData))]
        public static void ExpM1Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.ExpM1(value), allowedVariance);
        }

        public static IEnumerable<object[]> Exp2_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-3.14159265f), (BFloat16)(0.113314732f), CrossPlatformMachineEpsilon };        // value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), (BFloat16)(0.151955223f), CrossPlatformMachineEpsilon };        // value: -(e)
            yield return new object[] { (BFloat16)(-2.30258509f), (BFloat16)(0.202699566f), CrossPlatformMachineEpsilon };        // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57079633f), (BFloat16)(0.336622537f), CrossPlatformMachineEpsilon };        // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.44269504f), (BFloat16)(0.367879441f), CrossPlatformMachineEpsilon };        // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.41421356f), (BFloat16)(0.375214227f), CrossPlatformMachineEpsilon };        // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.12837917f), (BFloat16)(0.457429347f), CrossPlatformMachineEpsilon };        // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(0.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.785398163f), (BFloat16)(0.580191810f), CrossPlatformMachineEpsilon };        // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707106781f), (BFloat16)(0.612547327f), CrossPlatformMachineEpsilon };        // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.693147181f), (BFloat16)(0.618503138f), CrossPlatformMachineEpsilon };        // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.636619772f), (BFloat16)(0.643218242f), CrossPlatformMachineEpsilon };        // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.434294482f), (BFloat16)(0.740055574f), CrossPlatformMachineEpsilon };        // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.318309886f), (BFloat16)(0.802008879f), CrossPlatformMachineEpsilon };        // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(BFloat16.One), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(BFloat16.One), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.318309886f), (BFloat16)(1.24686899f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.434294482f), (BFloat16)(1.35124987f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.636619772f), (BFloat16)(1.55468228f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.693147181f), (BFloat16)(1.61680667f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707106781f), (BFloat16)(1.63252692f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.785398163f), (BFloat16)(1.72356793f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(2.0f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.12837917f), (BFloat16)(2.18612996f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.41421356f), (BFloat16)(2.66514414f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.44269504f), (BFloat16)(2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57079633f), (BFloat16)(2.97068642f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.30258509f), (BFloat16)(4.93340967f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.71828183f), (BFloat16)(6.58088599f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (e)
            yield return new object[] { (BFloat16)(3.14159265f), (BFloat16)(8.82497783f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, 0.0f };
        }

        [Theory]
        [MemberData(nameof(Exp2_TestData))]
        public static void Exp2Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Exp2(value), allowedVariance);
        }

        public static IEnumerable<object[]> Exp2M1_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)(-BFloat16.One), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-3.14159265f), (BFloat16)(-0.886685268f), CrossPlatformMachineEpsilon };            // value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), (BFloat16)(-0.848044777f), CrossPlatformMachineEpsilon };            // value: -(e)
            yield return new object[] { (BFloat16)(-2.30258509f), (BFloat16)(-0.797300434f), CrossPlatformMachineEpsilon };            // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57079633f), (BFloat16)(-0.663377463f), CrossPlatformMachineEpsilon };            // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.44269504f), (BFloat16)(-0.632120559f), CrossPlatformMachineEpsilon };            // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.41421356f), (BFloat16)(-0.624785773f), CrossPlatformMachineEpsilon };            // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.12837917f), (BFloat16)(-0.542570653f), CrossPlatformMachineEpsilon };            // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(-0.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.785398163f), (BFloat16)(-0.419808190f), CrossPlatformMachineEpsilon };            // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707106781f), (BFloat16)(-0.387452673f), CrossPlatformMachineEpsilon };            // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.693147181f), (BFloat16)(-0.381496862f), CrossPlatformMachineEpsilon };            // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.636619772f), (BFloat16)(-0.356781758f), CrossPlatformMachineEpsilon };            // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.434294482f), (BFloat16)(-0.259944426f), CrossPlatformMachineEpsilon };            // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.318309886f), (BFloat16)(-0.197991121f), CrossPlatformMachineEpsilon };            // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.318309886f), (BFloat16)(0.246868989f), CrossPlatformMachineEpsilon };            // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.434294482f), (BFloat16)(0.351249873f), CrossPlatformMachineEpsilon };            // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.636619772f), (BFloat16)(0.554682275f), CrossPlatformMachineEpsilon };            // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.693147181f), (BFloat16)(0.616806672f), CrossPlatformMachineEpsilon };            // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707106781f), (BFloat16)(0.632526919f), CrossPlatformMachineEpsilon };            // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.785398163f), (BFloat16)(0.723567934f), CrossPlatformMachineEpsilon };            // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.12837917f), (BFloat16)(1.18612996f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.41421356f), (BFloat16)(1.66514414f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.44269504f), (BFloat16)(1.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57079633f), (BFloat16)(1.97068642f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.30258509f), (BFloat16)(3.93340967f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.71828183f), (BFloat16)(5.58088599f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (e)
            yield return new object[] { (BFloat16)(3.14159265f), (BFloat16)(7.82497783f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.0f };
        }

        [Theory]
        [MemberData(nameof(Exp2M1_TestData))]
        public static void Exp2M1Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Exp2M1(value), allowedVariance);
        }

        public static IEnumerable<object[]> Exp10_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)0.0f, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-3.14159265f), (BFloat16)0.000721784159f, CrossPlatformMachineEpsilon / (BFloat16)1000 };  // value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), (BFloat16)0.00191301410f, CrossPlatformMachineEpsilon / (BFloat16)100 };   // value: -(e)
            yield return new object[] { (BFloat16)(-2.30258509f), (BFloat16)0.00498212830f, CrossPlatformMachineEpsilon / (BFloat16)100 };   // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57079633f), (BFloat16)0.0268660410f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.44269504f), (BFloat16)0.0360831928f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.41421356f), (BFloat16)0.0385288847f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.12837917f), (BFloat16)0.0744082059f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)0.1f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.785398163f), (BFloat16)0.163908636f, CrossPlatformMachineEpsilon };         // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707106781f), (BFloat16)0.196287760f, CrossPlatformMachineEpsilon };         // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.693147181f), (BFloat16)0.202699566f, CrossPlatformMachineEpsilon };         // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.636619772f), (BFloat16)0.230876765f, CrossPlatformMachineEpsilon };         // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.434294482f), (BFloat16)0.367879441f, CrossPlatformMachineEpsilon };         // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.318309886f), (BFloat16)0.480496373f, CrossPlatformMachineEpsilon };         // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)BFloat16.One, (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)BFloat16.One, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.318309886f), (BFloat16)2.08118116f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.434294482f), (BFloat16)2.71828183f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.636619772f), (BFloat16)4.33131503f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.693147181f), (BFloat16)4.93340967f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707106781f), (BFloat16)5.09456117f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.785398163f), (BFloat16)6.10095980f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };
            yield return new object[] { (BFloat16)(1.12837917f), (BFloat16)13.4393779f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.41421356f), (BFloat16)25.9545535f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.44269504f), (BFloat16)27.7137338f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57079633f), (BFloat16)37.2217105f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.30258509f), (BFloat16)200.717432f, CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.71828183f), (BFloat16)522.735300f, CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (e)
            yield return new object[] { (BFloat16)(3.14159265f), (BFloat16)1385.45573f, CrossPlatformMachineEpsilon * (BFloat16)10000 }; // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.0f };
        }

        [Theory]
        [MemberData(nameof(Exp10_TestData))]
        public static void Exp10Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Exp10(value), allowedVariance);
        }

        public static IEnumerable<object[]> Exp10M1_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)(-BFloat16.One), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-3.14159265f), (BFloat16)(-0.999278216f), CrossPlatformMachineEpsilon };               // value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), (BFloat16)(-0.998086986f), CrossPlatformMachineEpsilon };               // value: -(e)
            yield return new object[] { (BFloat16)(-2.30258509f), (BFloat16)(-0.995017872f), CrossPlatformMachineEpsilon };               // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57079633f), (BFloat16)(-0.973133959f), CrossPlatformMachineEpsilon };               // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.44269504f), (BFloat16)(-0.963916807f), CrossPlatformMachineEpsilon };               // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.41421356f), (BFloat16)(-0.961471115f), CrossPlatformMachineEpsilon };               // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.12837917f), (BFloat16)(-0.925591794f), CrossPlatformMachineEpsilon };               // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(-0.9f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.785398163f), (BFloat16)(-0.836091364f), CrossPlatformMachineEpsilon };               // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707106781f), (BFloat16)(-0.803712240f), CrossPlatformMachineEpsilon };               // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.693147181f), (BFloat16)(-0.797300434f), CrossPlatformMachineEpsilon };               // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.636619772f), (BFloat16)(-0.769123235f), CrossPlatformMachineEpsilon };               // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.434294482f), (BFloat16)(-0.632120559f), CrossPlatformMachineEpsilon };               // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.318309886f), (BFloat16)(-0.519503627f), CrossPlatformMachineEpsilon };               // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.318309886f), (BFloat16)(1.08118116f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.434294482f), (BFloat16)(1.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.636619772f), (BFloat16)(3.33131503f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.693147181f), (BFloat16)(3.93340967f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707106781f), (BFloat16)(4.09456117f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.785398163f), (BFloat16)(5.10095980f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(9.0f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.12837917f), (BFloat16)(12.4393779f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.41421356f), (BFloat16)(24.9545535f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.44269504f), (BFloat16)(26.7137338f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57079633f), (BFloat16)(36.2217105f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.30258509f), (BFloat16)(199.717432f), CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.71828183f), (BFloat16)(521.735300f), CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (e)
            yield return new object[] { (BFloat16)(3.14159265f), (BFloat16)(1384.45573f), CrossPlatformMachineEpsilon * (BFloat16)10000 }; // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.0f };
        }

        [Theory]
        [MemberData(nameof(Exp10M1_TestData))]
        public static void Exp10M1Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Exp10M1(value), allowedVariance);
        }

        public static IEnumerable<object[]> LogP1_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-3.14159265f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(e)
            yield return new object[] { (BFloat16)(-1.41421356f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(sqrt(2))
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-BFloat16.One), BFloat16.NegativeInfinity, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-0.956786082f), (BFloat16)(-3.14159265f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi)
            yield return new object[] { (BFloat16)(-0.934011964f), (BFloat16)(-2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(e)
            yield return new object[] { (BFloat16)(-0.9f), (BFloat16)(-2.30258509f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(ln(10))
            yield return new object[] { (BFloat16)(-0.792120424f), (BFloat16)(-1.57079633f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi / 2)
            yield return new object[] { (BFloat16)(-0.763709912f), (BFloat16)(-1.44269504f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(log2(e))
            yield return new object[] { (BFloat16)(-0.756883266f), (BFloat16)(-1.41421356f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(sqrt(2))
            yield return new object[] { (BFloat16)(-0.676442736f), (BFloat16)(-1.12837917f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-0.632120559f), (BFloat16)(-BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-0.544061872f), (BFloat16)(-0.785398163f), CrossPlatformMachineEpsilon };             // expected: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.506931309f), (BFloat16)(-0.707106781f), CrossPlatformMachineEpsilon };             // expected: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.5f), (BFloat16)(-0.693147181f), CrossPlatformMachineEpsilon };             // expected: -(ln(2))
            yield return new object[] { (BFloat16)(-0.470922192f), (BFloat16)(-0.636619772f), CrossPlatformMachineEpsilon };             // expected: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), 0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), 0.0f };
            yield return new object[] { (BFloat16)(0.374802227f), (BFloat16)(0.318309886f), CrossPlatformMachineEpsilon };             // expected:  (1 / pi)
            yield return new object[] { (BFloat16)(0.543873444f), (BFloat16)(0.434294482f), CrossPlatformMachineEpsilon };             // expected:  (log10(e))
            yield return new object[] { (BFloat16)(0.890081165f), (BFloat16)(0.636619772f), CrossPlatformMachineEpsilon };             // expected:  (2 / pi)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(0.693147181f), CrossPlatformMachineEpsilon };             // expected:  (ln(2))
            yield return new object[] { (BFloat16)(1.02811498f), (BFloat16)(0.707106781f), CrossPlatformMachineEpsilon };             // expected:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(1.19328005f), (BFloat16)(0.785398163f), CrossPlatformMachineEpsilon };             // expected:  (pi / 4)
            yield return new object[] { (BFloat16)(1.71828183f), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(2.09064302f), (BFloat16)(1.12837917f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(3.11325038f), (BFloat16)(1.41421356f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (sqrt(2))
            yield return new object[] { (BFloat16)(3.23208611f), (BFloat16)(1.44269504f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (log2(e))
            yield return new object[] { (BFloat16)(3.81047738f), (BFloat16)(1.57079633f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi / 2)
            yield return new object[] { (BFloat16)(9.0f), (BFloat16)(2.30258509f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (ln(10))
            yield return new object[] { (BFloat16)(14.1542622f), (BFloat16)(2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (e)
            yield return new object[] { (BFloat16)(22.1406926f), (BFloat16)(3.14159265f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.0f };
        }

        [Theory]
        [MemberData(nameof(LogP1_TestData))]
        public static void LogP1Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.LogP1(value), allowedVariance);
        }

        public static IEnumerable<object[]> Log2P1_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-BFloat16.One), BFloat16.NegativeInfinity, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-0.886685268f), (BFloat16)(-3.14159265f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi)
            yield return new object[] { (BFloat16)(-0.848044777f), (BFloat16)(-2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(e)
            yield return new object[] { (BFloat16)(-0.797300434f), (BFloat16)(-2.30258509f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(ln(10))
            yield return new object[] { (BFloat16)(-0.663377463f), (BFloat16)(-1.57079633f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi / 2)
            yield return new object[] { (BFloat16)(-0.632120559f), (BFloat16)(-1.44269504f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(log2(e))
            yield return new object[] { (BFloat16)(-0.624785773f), (BFloat16)(-1.41421356f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(sqrt(2))
            yield return new object[] { (BFloat16)(-0.542570653f), (BFloat16)(-1.12837917f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-0.5f), (BFloat16)(-BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-0.419808190f), (BFloat16)(-0.785398163f), CrossPlatformMachineEpsilon };             // expected: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.387452673f), (BFloat16)(-0.707106781f), CrossPlatformMachineEpsilon };             // expected: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.381496862f), (BFloat16)(-0.693147181f), CrossPlatformMachineEpsilon };             // expected: -(ln(2))
            yield return new object[] { (BFloat16)(-0.356781758f), (BFloat16)(-0.636619772f), CrossPlatformMachineEpsilon };             // expected: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.259944426f), (BFloat16)(-0.434294482f), CrossPlatformMachineEpsilon };             // expected: -(log10(e))
            yield return new object[] { (BFloat16)(-0.197991121f), (BFloat16)(-0.318309886f), CrossPlatformMachineEpsilon };             // expected: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.246868989f), (BFloat16)(0.318309886f), CrossPlatformMachineEpsilon };             // expected:  (1 / pi)
            yield return new object[] { (BFloat16)(0.351249873f), (BFloat16)(0.434294482f), CrossPlatformMachineEpsilon };             // expected:  (log10(e))
            yield return new object[] { (BFloat16)(0.554682275f), (BFloat16)(0.636619772f), CrossPlatformMachineEpsilon };             // expected:  (2 / pi)
            yield return new object[] { (BFloat16)(0.616806672f), (BFloat16)(0.693147181f), CrossPlatformMachineEpsilon };             // expected:  (ln(2))
            yield return new object[] { (BFloat16)(0.632526919f), (BFloat16)(0.707106781f), CrossPlatformMachineEpsilon };             // expected:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.723567934f), (BFloat16)(0.785398163f), CrossPlatformMachineEpsilon };             // expected:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.18612996f), (BFloat16)(1.12837917f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.66514414f), (BFloat16)(1.41421356f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.71828183f), (BFloat16)(1.44269504f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (log2(e))
            yield return new object[] { (BFloat16)(1.97068642f), (BFloat16)(1.57079633f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi / 2)
            yield return new object[] { (BFloat16)(3.93340967f), (BFloat16)(2.30258509f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (ln(10))
            yield return new object[] { (BFloat16)(5.58088599f), (BFloat16)(2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (e)
            yield return new object[] { (BFloat16)(7.82497783f), (BFloat16)(3.14159265f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.0f };
        }

        [Theory]
        [MemberData(nameof(Log2P1_TestData))]
        public static void Log2P1Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Log2P1(value), allowedVariance);
        }

        public static IEnumerable<object[]> Log10P1_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-3.14159265f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(pi)
            yield return new object[] { (BFloat16)(-2.71828183f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(e)
            yield return new object[] { (BFloat16)(-1.41421356f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(sqrt(2))
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-BFloat16.One), BFloat16.NegativeInfinity, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-0.998086986f), (BFloat16)(-2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(e)
            yield return new object[] { (BFloat16)(-0.995017872f), (BFloat16)(-2.30258509f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(ln(10))
            yield return new object[] { (BFloat16)(-0.973133959f), (BFloat16)(-1.57079633f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi / 2)
            yield return new object[] { (BFloat16)(-0.963916807f), (BFloat16)(-1.44269504f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(log2(e))
            yield return new object[] { (BFloat16)(-0.961471115f), (BFloat16)(-1.41421356f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(sqrt(2))
            yield return new object[] { (BFloat16)(-0.925591794f), (BFloat16)(-1.12837917f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-0.9f), (BFloat16)(-1.0f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-0.836091364f), (BFloat16)(-0.785398163f), CrossPlatformMachineEpsilon };             // expected: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.803712240f), (BFloat16)(-0.707106781f), CrossPlatformMachineEpsilon };             // expected: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.797300434f), (BFloat16)(-0.693147181f), CrossPlatformMachineEpsilon };             // expected: -(ln(2))
            yield return new object[] { (BFloat16)(-0.769123235f), (BFloat16)(-0.636619772f), CrossPlatformMachineEpsilon };             // expected: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.632120559f), (BFloat16)(-0.434294482f), CrossPlatformMachineEpsilon };             // expected: -(log10(e))
            yield return new object[] { (BFloat16)(-0.519503627f), (BFloat16)(-0.318309886f), CrossPlatformMachineEpsilon };             // expected: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(1.08118116f), (BFloat16)(0.318309886f), CrossPlatformMachineEpsilon };             // expected:  (1 / pi)
            yield return new object[] { (BFloat16)(1.71828183f), (BFloat16)(0.434294482f), CrossPlatformMachineEpsilon };             // expected:  (log10(e))        value: (e)
            yield return new object[] { (BFloat16)(3.33131503f), (BFloat16)(0.636619772f), CrossPlatformMachineEpsilon };             // expected:  (2 / pi)
            yield return new object[] { (BFloat16)(3.93340967f), (BFloat16)(0.693147181f), CrossPlatformMachineEpsilon };             // expected:  (ln(2))
            yield return new object[] { (BFloat16)(4.09456117f), (BFloat16)(0.707106781f), CrossPlatformMachineEpsilon };             // expected:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(5.10095980f), (BFloat16)(0.785398163f), CrossPlatformMachineEpsilon };             // expected:  (pi / 4)
            yield return new object[] { (BFloat16)(9.0f), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(12.4393779f), (BFloat16)(1.12837917f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(24.9545535f), (BFloat16)(1.41421356f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (sqrt(2))
            yield return new object[] { (BFloat16)(26.7137338f), (BFloat16)(1.44269504f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (log2(e))
            yield return new object[] { (BFloat16)(36.2217105f), (BFloat16)(1.57079633f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi / 2)
            yield return new object[] { (BFloat16)(199.717432f), (BFloat16)(2.30258509f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (ln(10))
            yield return new object[] { (BFloat16)(521.735300f), (BFloat16)(2.71828183f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (e)
            yield return new object[] { (BFloat16)(1384.45573f), (BFloat16)(3.14159265f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.0f };
        }

        [Theory]
        [MemberData(nameof(Log10P1_TestData))]
        public static void Log10P1Test(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Log10P1(value), allowedVariance);
        }

        public static IEnumerable<object[]> Hypot_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, BFloat16.Zero, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, BFloat16.One, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, BFloat16.E, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, (BFloat16)10.0f, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.One, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)1.57079633f, (BFloat16)1.57079633f, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)2.0f, (BFloat16)2.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.E, BFloat16.E, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)3.0f, (BFloat16)3.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)10.0f, (BFloat16)10.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.One, BFloat16.One, (BFloat16)1.41421356f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, (BFloat16)0.318309886f, (BFloat16)2.73685536f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (1 / pi)
            yield return new object[] { BFloat16.E, (BFloat16)0.434294482f, (BFloat16)2.75275640f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (log10(e))
            yield return new object[] { BFloat16.E, (BFloat16)0.636619772f, (BFloat16)2.79183467f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (2 / pi)
            yield return new object[] { BFloat16.E, (BFloat16)0.693147181f, (BFloat16)2.80526454f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (ln(2))
            yield return new object[] { BFloat16.E, (BFloat16)0.707106781f, (BFloat16)2.80874636f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (1 / sqrt(2))
            yield return new object[] { BFloat16.E, (BFloat16)0.785398163f, (BFloat16)2.82947104f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (pi / 4)
            yield return new object[] { BFloat16.E, BFloat16.One, (BFloat16)2.89638673f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)
            yield return new object[] { BFloat16.E, (BFloat16)1.12837917f, (BFloat16)2.94317781f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (2 / sqrt(pi))
            yield return new object[] { BFloat16.E, (BFloat16)1.41421356f, (BFloat16)3.06415667f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (sqrt(2))
            yield return new object[] { BFloat16.E, (BFloat16)1.44269504f, (BFloat16)3.07740558f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (log2(e))
            yield return new object[] { BFloat16.E, (BFloat16)1.57079633f, (BFloat16)3.13949951f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (pi / 2)
            yield return new object[] { BFloat16.E, (BFloat16)2.30258509f, (BFloat16)3.56243656f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (ln(10))
            yield return new object[] { BFloat16.E, BFloat16.E, (BFloat16)3.84423103f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (e)
            yield return new object[] { BFloat16.E, (BFloat16)3.14159265f, (BFloat16)4.15435440f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (pi)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.318309886f, (BFloat16)10.0050648f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (1 / pi)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.434294482f, (BFloat16)10.0094261f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (log10(e))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.636619772f, (BFloat16)10.0202437f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (2 / pi)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.693147181f, (BFloat16)10.0239939f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (ln(2))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.707106781f, (BFloat16)10.0249688f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (1 / sqrt(2))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.785398163f, (BFloat16)10.0307951f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (pi / 4)
            yield return new object[] { (BFloat16)10.0f, BFloat16.One, (BFloat16)10.0498756f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.12837917f, (BFloat16)10.0634606f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (2 / sqrt(pi))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.41421356f, (BFloat16)10.0995049f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (sqrt(2))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.44269504f, (BFloat16)10.1035325f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (log2(e))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.57079633f, (BFloat16)10.1226183f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (pi / 2)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)2.30258509f, (BFloat16)10.2616713f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (ln(10))
            yield return new object[] { (BFloat16)10.0f, BFloat16.E, (BFloat16)10.3628691f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (e)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)3.14159265f, (BFloat16)10.4818703f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.Zero, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.One, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.E, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 10.0f, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(Hypot_TestData))]
        public static void Hypot(BFloat16 x, BFloat16 y, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.Hypot(-x, -y), allowedVariance);
            AssertEqual(expectedResult, BFloat16.Hypot(-x, +y), allowedVariance);
            AssertEqual(expectedResult, BFloat16.Hypot(+x, -y), allowedVariance);
            AssertEqual(expectedResult, BFloat16.Hypot(+x, +y), allowedVariance);

            AssertEqual(expectedResult, BFloat16.Hypot(-y, -x), allowedVariance);
            AssertEqual(expectedResult, BFloat16.Hypot(-y, +x), allowedVariance);
            AssertEqual(expectedResult, BFloat16.Hypot(+y, -x), allowedVariance);
            AssertEqual(expectedResult, BFloat16.Hypot(+y, +x), allowedVariance);
        }

        public static IEnumerable<object[]> RootN_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, -5, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, -4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, -3, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, -2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, -1, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, 1, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, 2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, 3, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, 4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NegativeInfinity, 5, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { -BFloat16.E, -5, -(BFloat16)0.81873075f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { -BFloat16.E, -4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.E, -3, -(BFloat16)0.71653131f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { -BFloat16.E, -2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.E, -1, -(BFloat16)0.36787944f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { -BFloat16.E, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.E, 1, -BFloat16.E, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { -BFloat16.E, 2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.E, 3, -(BFloat16)1.39561243f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { -BFloat16.E, 4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.E, 5, -(BFloat16)1.22140276f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { -BFloat16.One, -5, -BFloat16.One, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, -4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, -3, -BFloat16.One, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, -2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, -1, -BFloat16.One, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, 1, -BFloat16.One, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, 2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, 3, -BFloat16.One, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, 4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.One, 5, -BFloat16.One, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, -5, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, -4, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, -3, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, -2, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, -1, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, 1, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, 2, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, 3, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, 4, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { -BFloat16.Zero, 5, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, -5, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, -4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, -3, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, -2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, -1, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, 1, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, 2, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, 3, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, 4, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.NaN, 5, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, -5, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, -4, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, -3, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, -2, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, -1, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, 1, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, 2, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, 3, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, 4, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, 5, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.One, -5, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, -4, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, -3, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, -2, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, -1, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.One, 1, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, 2, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, 3, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, 4, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.One, 5, BFloat16.One, BFloat16.Zero };
            yield return new object[] { BFloat16.E, -5, (BFloat16)0.81873075f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -4, (BFloat16)0.77880078f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -3, (BFloat16)0.71653131f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -2, (BFloat16)0.60653066f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -1, (BFloat16)0.36787944f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.E, 1, BFloat16.E, BFloat16.Zero };
            yield return new object[] { BFloat16.E, 2, (BFloat16)1.64872127f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 3, (BFloat16)1.39561243f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 4, (BFloat16)1.28402542f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 5, (BFloat16)1.22140276f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.PositiveInfinity, -5, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, -4, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, -3, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, -2, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, -1, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 1, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 2, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 3, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 4, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, 5, BFloat16.PositiveInfinity, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(RootN_TestData))]
        public static void RootN(BFloat16 x, int n, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.RootN(x, n), allowedVariance);
        }

        public static IEnumerable<object[]> AcosPi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.One, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.540302306f, (BFloat16)0.318309886f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.204957194f, (BFloat16)0.434294482f, CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.Zero, (BFloat16)0.5f, BFloat16.Zero };
            yield return new object[] { -(BFloat16)0.416146837f, (BFloat16)0.636619772f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.570233249f, (BFloat16)0.693147181f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.605699867f, (BFloat16)0.707106781f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.781211892f, (BFloat16)0.785398163f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.0f, BFloat16.One, BFloat16.Zero };
            yield return new object[] { -(BFloat16)0.919764995f, (BFloat16)0.871620833f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.266255342f, (BFloat16)0.585786438f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.179057946f, (BFloat16)0.557304959f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.220584041f, (BFloat16)0.429203673f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.581195664f, (BFloat16)0.302585093f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.633255651f, (BFloat16)0.718281828f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.902685362f, (BFloat16)0.858407346f, CrossPlatformMachineEpsilon };
        }

        [Theory]
        [MemberData(nameof(AcosPi_TestData))]
        public static void AcosPiTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(expectedResult, BFloat16.AcosPi(value), allowedVariance);
        }

        public static IEnumerable<object[]> AsinPi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.841470985f, (BFloat16)0.318309886f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.978770938f, (BFloat16)0.434294482f, CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.One, (BFloat16)0.5f, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.909297427f, (BFloat16)0.363380228f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.821482831f, (BFloat16)0.306852819f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.795693202f, (BFloat16)0.292893219f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.624265953f, (BFloat16)0.214601837f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.392469559f, -(BFloat16)0.128379167f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.963902533f, -(BFloat16)0.414213562f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.983838529f, -(BFloat16)0.442695041f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.975367972f, -(BFloat16)0.429203673f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.813763848f, (BFloat16)0.302585093f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.773942685f, (BFloat16)0.281718172f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.430301217f, -(BFloat16)0.141592654f, CrossPlatformMachineEpsilon };
        }

        [Theory]
        [MemberData(nameof(AsinPi_TestData))]
        public static void AsinPiTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.AsinPi(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.AsinPi(+value), allowedVariance);
        }

        public static IEnumerable<object[]> Atan2Pi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, -BFloat16.One, BFloat16.One, BFloat16.Zero };                   // y: sinpi(0)              x:  cospi(1)
            yield return new object[] { BFloat16.Zero, -BFloat16.Zero, BFloat16.One, BFloat16.Zero };                   // y: sinpi(0)              x: -cospi(0.5)
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };                   // y: sinpi(0)              x:  cospi(0.5)
            yield return new object[] { BFloat16.Zero, BFloat16.One, BFloat16.Zero, BFloat16.Zero };                   // y: sinpi(0)              x:  cospi(0)
            yield return new object[] { (BFloat16)0.841470985f, (BFloat16)0.540302306f, (BFloat16)0.318309886f, CrossPlatformMachineEpsilon }; // y: sinpi(1 / pi)         x:  cospi(1 / pi)
            yield return new object[] { (BFloat16)0.978770938f, (BFloat16)0.204957194f, (BFloat16)0.434294482f, CrossPlatformMachineEpsilon }; // y: sinpi(log10(e))       x:  cospi(log10(e))
            yield return new object[] { BFloat16.One, -BFloat16.Zero, (BFloat16)0.5f, BFloat16.Zero };                   // y: sinpi(0.5)            x: -cospi(0.5)
            yield return new object[] { BFloat16.One, BFloat16.Zero, (BFloat16)0.5f, BFloat16.Zero };                   // y: sinpi(0.5)            x:  cospi(0.5)
            yield return new object[] { (BFloat16)0.909297427f, -(BFloat16)0.416146837f, (BFloat16)0.636619772f, CrossPlatformMachineEpsilon }; // y: sinpi(2 / pi)         x:  cospi(2 / pi)
            yield return new object[] { (BFloat16)0.821482831f, -(BFloat16)0.570233249f, (BFloat16)0.693147181f, CrossPlatformMachineEpsilon }; // y: sinpi(ln(2))          x:  cospi(ln(2))
            yield return new object[] { (BFloat16)0.795693202f, -(BFloat16)0.605699867f, (BFloat16)0.707106781f, CrossPlatformMachineEpsilon }; // y: sinpi(1 / sqrt(2))    x:  cospi(1 / sqrt(2))
            yield return new object[] { (BFloat16)0.624265953f, -(BFloat16)0.781211892f, (BFloat16)0.785398163f, CrossPlatformMachineEpsilon }; // y: sinpi(pi / 4)         x:  cospi(pi / 4)
            yield return new object[] { -(BFloat16)0.392469559f, -(BFloat16)0.919764995f, -(BFloat16)0.871620833f, CrossPlatformMachineEpsilon }; // y: sinpi(2 / sqrt(pi))   x:  cospi(2 / sqrt(pi))
            yield return new object[] { -(BFloat16)0.963902533f, -(BFloat16)0.266255342f, -(BFloat16)0.585786438f, CrossPlatformMachineEpsilon }; // y: sinpi(sqrt(2))        x:  cospi(sqrt(2))
            yield return new object[] { -(BFloat16)0.983838529f, -(BFloat16)0.179057946f, -(BFloat16)0.557304959f, CrossPlatformMachineEpsilon }; // y: sinpi(log2(e))        x:  cospi(log2(e))
            yield return new object[] { -(BFloat16)0.975367972f, (BFloat16)0.220584041f, -(BFloat16)0.429203673f, CrossPlatformMachineEpsilon }; // y: sinpi(pi / 2)         x:  cospi(pi / 2)
            yield return new object[] { (BFloat16)0.813763848f, (BFloat16)0.581195664f, (BFloat16)0.302585093f, CrossPlatformMachineEpsilon }; // y: sinpi(ln(10))         x:  cospi(ln(10))
            yield return new object[] { (BFloat16)0.773942685f, -(BFloat16)0.633255651f, (BFloat16)0.718281828f, CrossPlatformMachineEpsilon }; // y: sinpi(e)              x:  cospi(e)
            yield return new object[] { -(BFloat16)0.430301217f, -(BFloat16)0.902685362f, -(BFloat16)0.858407346f, CrossPlatformMachineEpsilon }; // y: sinpi(pi)             x:  cospi(pi)
            yield return new object[] { BFloat16.One, BFloat16.NegativeInfinity, BFloat16.One, BFloat16.Zero };                   // y: sinpi(0.5)
            yield return new object[] { BFloat16.One, BFloat16.PositiveInfinity, BFloat16.Zero, BFloat16.Zero };                   // y: sinpi(0.5)
            yield return new object[] { BFloat16.PositiveInfinity, -BFloat16.One, (BFloat16)0.5f, BFloat16.Zero };                   //                          x:  cospi(1)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.One, (BFloat16)0.5f, BFloat16.Zero };                   //                          x:  cospi(0)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NegativeInfinity, (BFloat16)0.75f, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)0.25f, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(Atan2Pi_TestData))]
        public static void Atan2PiTest(BFloat16 y, BFloat16 x, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.Atan2Pi(-y, +x), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.Atan2Pi(+y, +x), allowedVariance);
        }

        public static IEnumerable<object[]> AtanPi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.55740773f, (BFloat16)0.318309886f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)4.77548954f, (BFloat16)0.434294482f, CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.PositiveInfinity, (BFloat16)0.5f, BFloat16.Zero };
            yield return new object[] { -(BFloat16)2.18503986f, -(BFloat16)0.363380228f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.44060844f, -(BFloat16)0.306852819f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.31367571f, -(BFloat16)0.292893219f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.79909940f, -(BFloat16)0.214601837f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.42670634f, (BFloat16)0.128379167f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)3.62021857f, (BFloat16)0.414213562f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)5.49452594f, (BFloat16)0.442695041f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)4.42175222f, -(BFloat16)0.429203673f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)1.40015471f, (BFloat16)0.302585093f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.22216467f, -(BFloat16)0.281718172f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.476690146f, (BFloat16)0.141592654f, CrossPlatformMachineEpsilon };
        }

        [Theory]
        [MemberData(nameof(AtanPi_TestData))]
        public static void AtanPiTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.AtanPi(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.AtanPi(+value), allowedVariance);
        }

        public static IEnumerable<object[]> CosPi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.One, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.318309886f, (BFloat16)0.540302306f, CrossPlatformMachineEpsilon };       // value:  (1 / pi)
            yield return new object[] { (BFloat16)0.434294482f, (BFloat16)0.204957194f, CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)0.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.636619772f, -(BFloat16)0.416146837f, CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)0.693147181f, -(BFloat16)0.570233249f, CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)0.707106781f, -(BFloat16)0.605699867f, CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)0.785398163f, -(BFloat16)0.781211892f, CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { BFloat16.One, -(BFloat16)1.0f, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.12837917f, -(BFloat16)0.919764995f, CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)1.41421356f, -(BFloat16)0.266255342f, CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)1.44269504f, -(BFloat16)0.179057946f, CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)1.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.57079633f, (BFloat16)0.220584041f, CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)2.0f, (BFloat16)1.0, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.30258509f, (BFloat16)0.581195664f, CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)2.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.71828183f, -(BFloat16)0.633255651f, CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)3.0f, -(BFloat16)1.0, BFloat16.Zero };
            yield return new object[] { (BFloat16)3.14159265f, -(BFloat16)0.902685362f, CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)3.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(CosPi_TestData))]
        public static void CosPiTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(+expectedResult, BFloat16.CosPi(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.CosPi(+value), allowedVariance);
        }

        public static IEnumerable<object[]> SinPi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.318309886f, (BFloat16)0.841470985f, CrossPlatformMachineEpsilon };       // value:  (1 / pi)
            yield return new object[] { (BFloat16)0.434294482f, (BFloat16)0.978770938f, CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)0.5f, BFloat16.One, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.636619772f, (BFloat16)0.909297427f, CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)0.693147181f, (BFloat16)0.821482831f, CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)0.707106781f, (BFloat16)0.795693202f, CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)0.785398163f, (BFloat16)0.624265953f, CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { BFloat16.One, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.12837917f, -(BFloat16)0.392469559f, CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)1.41421356f, -(BFloat16)0.963902533f, CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)1.44269504f, -(BFloat16)0.983838529f, CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)1.5f, -(BFloat16)1.0f, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.57079633f, -(BFloat16)0.975367972f, CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)2.0f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.30258509f, (BFloat16)0.813763848f, CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)2.5f, BFloat16.One, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.71828183f, (BFloat16)0.773942685f, CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)3.0f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)3.14159265f, -(BFloat16)0.430301217f, CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)3.5f, -(BFloat16)1.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(SinPi_TestData))]
        public static void SinPiTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.SinPi(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.SinPi(+value), allowedVariance);
        }

        public static IEnumerable<object[]> TanPi_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.318309886f, (BFloat16)1.55740772f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (1 / pi)
            yield return new object[] { (BFloat16)0.434294482f, (BFloat16)4.77548954f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (log10(e))
            yield return new object[] { (BFloat16)0.5f, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.636619772f, -(BFloat16)2.18503986f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (2 / pi)
            yield return new object[] { (BFloat16)0.693147181f, -(BFloat16)1.44060844f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(2))
            yield return new object[] { (BFloat16)0.707106781f, -(BFloat16)1.31367571f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)0.785398163f, -(BFloat16)0.799099398f, CrossPlatformMachineEpsilon };             // value:  (pi / 4)
            yield return new object[] { BFloat16.One, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.12837917f, (BFloat16)0.426706344f, CrossPlatformMachineEpsilon };             // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)1.41421356f, (BFloat16)3.62021857f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (sqrt(2))
            yield return new object[] { (BFloat16)1.44269504f, (BFloat16)5.49452594f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (log2(e))
            yield return new object[] { (BFloat16)1.5f, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.57079633f, -(BFloat16)4.42175222f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (pi / 2)
            yield return new object[] { (BFloat16)2.0f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.30258509f, (BFloat16)1.40015471f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)2.5f, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.71828183f, -(BFloat16)1.22216467f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (e)
            yield return new object[] { (BFloat16)3.0f, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)3.14159265f, (BFloat16)0.476690146f, CrossPlatformMachineEpsilon };             // value:  (pi)
            yield return new object[] { (BFloat16)3.5f, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(TanPi_TestData))]
        public static void TanPiTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.TanPi(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.TanPi(+value), allowedVariance);
        }

        public static IEnumerable<object[]> BitDecrement_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NegativeInfinity };
            yield return new object[] { UInt16BitsToBFloat16(0xC049), UInt16BitsToBFloat16(0xC04A) };    // value: -(pi)
            yield return new object[] { UInt16BitsToBFloat16(0xC02E), UInt16BitsToBFloat16(0xC02F) };    // value: -(e)
            yield return new object[] { UInt16BitsToBFloat16(0xC013), UInt16BitsToBFloat16(0xC014) };    // value: -(ln(10))
            yield return new object[] { UInt16BitsToBFloat16(0xBFC9), UInt16BitsToBFloat16(0xBFCA) };    // value: -(pi / 2)
            yield return new object[] { UInt16BitsToBFloat16(0xBFB9), UInt16BitsToBFloat16(0xBFBA) };    // value: -(log2(e))
            yield return new object[] { UInt16BitsToBFloat16(0xBFB5), UInt16BitsToBFloat16(0xBFB6) };    // value: -(sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0xBF90), UInt16BitsToBFloat16(0xBF91) };    // value: -(2 / sqrt(pi))
            yield return new object[] { UInt16BitsToBFloat16(0xBF80), UInt16BitsToBFloat16(0xBF81) };
            yield return new object[] { UInt16BitsToBFloat16(0xBF49), UInt16BitsToBFloat16(0xBF4A) };    // value: -(pi / 4)
            yield return new object[] { UInt16BitsToBFloat16(0xBF35), UInt16BitsToBFloat16(0xBF36) };    // value: -(1 / sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0xBF31), UInt16BitsToBFloat16(0xBF32) };    // value: -(ln(2))
            yield return new object[] { UInt16BitsToBFloat16(0xBF23), UInt16BitsToBFloat16(0xBF24) };    // value: -(2 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0xBEDE), UInt16BitsToBFloat16(0xBEDF) };    // value: -(log10(e))
            yield return new object[] { UInt16BitsToBFloat16(0xBEA3), UInt16BitsToBFloat16(0xBEA4) };    // value: -(1 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0x8000), -BFloat16.Epsilon };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { UInt16BitsToBFloat16(0x0000), -BFloat16.Epsilon };
            yield return new object[] { UInt16BitsToBFloat16(0x3EA3), UInt16BitsToBFloat16(0x3EA2) };    // value:  (1 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0x3EDE), UInt16BitsToBFloat16(0x3EDD) };    // value:  (log10(e))
            yield return new object[] { UInt16BitsToBFloat16(0x3F23), UInt16BitsToBFloat16(0x3F22) };    // value:  (2 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0x3F31), UInt16BitsToBFloat16(0x3F30) };    // value:  (ln(2))
            yield return new object[] { UInt16BitsToBFloat16(0x3F35), UInt16BitsToBFloat16(0x3F34) };    // value:  (1 / sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0x3F49), UInt16BitsToBFloat16(0x3F48) };    // value:  (pi / 4)
            yield return new object[] { UInt16BitsToBFloat16(0x3F80), UInt16BitsToBFloat16(0x3F7F) };
            yield return new object[] { UInt16BitsToBFloat16(0x3F90), UInt16BitsToBFloat16(0x3F8F) };    // value:  (2 / sqrt(pi))
            yield return new object[] { UInt16BitsToBFloat16(0x3FB5), UInt16BitsToBFloat16(0x3FB4) };    // value:  (sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0x3FB9), UInt16BitsToBFloat16(0x3FB8) };    // value:  (log2(e))
            yield return new object[] { UInt16BitsToBFloat16(0x3FC9), UInt16BitsToBFloat16(0x3FC8) };    // value:  (pi / 2)
            yield return new object[] { UInt16BitsToBFloat16(0x4013), UInt16BitsToBFloat16(0x4012) };    // value:  (ln(10))
            yield return new object[] { UInt16BitsToBFloat16(0x402E), UInt16BitsToBFloat16(0x402D) };    // value:  (e)
            yield return new object[] { UInt16BitsToBFloat16(0x4049), UInt16BitsToBFloat16(0x4048) };    // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.MaxValue };
        }

        [Theory]
        [MemberData(nameof(BitDecrement_TestData))]
        public static void BitDecrement(BFloat16 value, BFloat16 expectedResult)
        {
            AssertEqual(expectedResult, BFloat16.BitDecrement(value), BFloat16.Zero);
        }

        public static IEnumerable<object[]> BitIncrement_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.MinValue };
            yield return new object[] { UInt16BitsToBFloat16(0xC049), UInt16BitsToBFloat16(0xC048) };    // value: -(pi)
            yield return new object[] { UInt16BitsToBFloat16(0xC02E), UInt16BitsToBFloat16(0xC02D) };    // value: -(e)
            yield return new object[] { UInt16BitsToBFloat16(0xC013), UInt16BitsToBFloat16(0xC012) };    // value: -(ln(10))
            yield return new object[] { UInt16BitsToBFloat16(0xBFC9), UInt16BitsToBFloat16(0xBFC8) };    // value: -(pi / 2)
            yield return new object[] { UInt16BitsToBFloat16(0xBFB9), UInt16BitsToBFloat16(0xBFB8) };    // value: -(log2(e))
            yield return new object[] { UInt16BitsToBFloat16(0xBFB5), UInt16BitsToBFloat16(0xBFB4) };    // value: -(sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0xBF90), UInt16BitsToBFloat16(0xBF8F) };    // value: -(2 / sqrt(pi))
            yield return new object[] { UInt16BitsToBFloat16(0xBF80), UInt16BitsToBFloat16(0xBF7F) };
            yield return new object[] { UInt16BitsToBFloat16(0xBF49), UInt16BitsToBFloat16(0xBF48) };    // value: -(pi / 4)
            yield return new object[] { UInt16BitsToBFloat16(0xBF35), UInt16BitsToBFloat16(0xBF34) };    // value: -(1 / sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0xBF31), UInt16BitsToBFloat16(0xBF30) };    // value: -(ln(2))
            yield return new object[] { UInt16BitsToBFloat16(0xBF23), UInt16BitsToBFloat16(0xBF22) };    // value: -(2 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0xBEDE), UInt16BitsToBFloat16(0xBEDD) };    // value: -(log10(e))
            yield return new object[] { UInt16BitsToBFloat16(0xBEA3), UInt16BitsToBFloat16(0xBEA2) };    // value: -(1 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0x8000), BFloat16.Epsilon };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { UInt16BitsToBFloat16(0x0000), BFloat16.Epsilon };
            yield return new object[] { UInt16BitsToBFloat16(0x3EA3), UInt16BitsToBFloat16(0x3EA4) };    // value:  (1 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0x3EDE), UInt16BitsToBFloat16(0x3EDF) };    // value:  (log10(e))
            yield return new object[] { UInt16BitsToBFloat16(0x3F23), UInt16BitsToBFloat16(0x3F24) };    // value:  (2 / pi)
            yield return new object[] { UInt16BitsToBFloat16(0x3F31), UInt16BitsToBFloat16(0x3F32) };    // value:  (ln(2))
            yield return new object[] { UInt16BitsToBFloat16(0x3F35), UInt16BitsToBFloat16(0x3F36) };    // value:  (1 / sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0x3F49), UInt16BitsToBFloat16(0x3F4A) };    // value:  (pi / 4)
            yield return new object[] { UInt16BitsToBFloat16(0x3F80), UInt16BitsToBFloat16(0x3F81) };
            yield return new object[] { UInt16BitsToBFloat16(0x3F90), UInt16BitsToBFloat16(0x3F91) };    // value:  (2 / sqrt(pi))
            yield return new object[] { UInt16BitsToBFloat16(0x3FB5), UInt16BitsToBFloat16(0x3FB6) };    // value:  (sqrt(2))
            yield return new object[] { UInt16BitsToBFloat16(0x3FB9), UInt16BitsToBFloat16(0x3FBA) };    // value:  (log2(e))
            yield return new object[] { UInt16BitsToBFloat16(0x3FC9), UInt16BitsToBFloat16(0x3FCA) };    // value:  (pi / 2)
            yield return new object[] { UInt16BitsToBFloat16(0x4013), UInt16BitsToBFloat16(0x4014) };    // value:  (ln(10))
            yield return new object[] { UInt16BitsToBFloat16(0x402E), UInt16BitsToBFloat16(0x402F) };    // value:  (e)
            yield return new object[] { UInt16BitsToBFloat16(0x4049), UInt16BitsToBFloat16(0x404A) };    // value:  (pi)
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(BitIncrement_TestData))]
        public static void BitIncrement(BFloat16 value, BFloat16 expectedResult)
        {
            AssertEqual(expectedResult, BFloat16.BitIncrement(value), BFloat16.Zero);
        }

        public static IEnumerable<object[]> Lerp_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NegativeInfinity, (BFloat16)(0.5f), BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.NaN, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.PositiveInfinity, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)(0.0f), (BFloat16)(0.5f), BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NegativeInfinity, (BFloat16)(1.0f), (BFloat16)(0.5f), BFloat16.NegativeInfinity };
            yield return new object[] { BFloat16.NaN, BFloat16.NegativeInfinity, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, BFloat16.PositiveInfinity, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, (BFloat16)(0.0f), (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.NaN, (BFloat16)(1.0f), (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NegativeInfinity, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.NaN, (BFloat16)(0.5f), BFloat16.NaN };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, (BFloat16)(0.5f), BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.PositiveInfinity, (BFloat16)(0.0f), (BFloat16)(0.5f), BFloat16.PositiveInfinity };
            yield return new object[] { BFloat16.PositiveInfinity, (BFloat16)(1.0f), (BFloat16)(0.5f), BFloat16.PositiveInfinity };
            yield return new object[] { (BFloat16)(1.0f), (BFloat16)(3.0f), (BFloat16)(0.0f), (BFloat16)(1.0f) };
            yield return new object[] { (BFloat16)(1.0f), (BFloat16)(3.0f), (BFloat16)(0.5f), (BFloat16)(2.0f) };
            yield return new object[] { (BFloat16)(1.0f), (BFloat16)(3.0f), (BFloat16)(1.0f), (BFloat16)(3.0f) };
            yield return new object[] { (BFloat16)(1.0f), (BFloat16)(3.0f), (BFloat16)(2.0f), (BFloat16)(5.0f) };
            yield return new object[] { (BFloat16)(2.0f), (BFloat16)(4.0f), (BFloat16)(0.0f), (BFloat16)(2.0f) };
            yield return new object[] { (BFloat16)(2.0f), (BFloat16)(4.0f), (BFloat16)(0.5f), (BFloat16)(3.0f) };
            yield return new object[] { (BFloat16)(2.0f), (BFloat16)(4.0f), (BFloat16)(1.0f), (BFloat16)(4.0f) };
            yield return new object[] { (BFloat16)(2.0f), (BFloat16)(4.0f), (BFloat16)(2.0f), (BFloat16)(6.0f) };
            yield return new object[] { (BFloat16)(3.0f), (BFloat16)(1.0f), (BFloat16)(0.0f), (BFloat16)(3.0f) };
            yield return new object[] { (BFloat16)(3.0f), (BFloat16)(1.0f), (BFloat16)(0.5f), (BFloat16)(2.0f) };
            yield return new object[] { (BFloat16)(3.0f), (BFloat16)(1.0f), (BFloat16)(1.0f), (BFloat16)(1.0f) };
            yield return new object[] { (BFloat16)(3.0f), (BFloat16)(1.0f), (BFloat16)(2.0f), -(BFloat16)(1.0f) };
            yield return new object[] { (BFloat16)(4.0f), (BFloat16)(2.0f), (BFloat16)(0.0f), (BFloat16)(4.0f) };
            yield return new object[] { (BFloat16)(4.0f), (BFloat16)(2.0f), (BFloat16)(0.5f), (BFloat16)(3.0f) };
            yield return new object[] { (BFloat16)(4.0f), (BFloat16)(2.0f), (BFloat16)(1.0f), (BFloat16)(2.0f) };
            yield return new object[] { (BFloat16)(4.0f), (BFloat16)(2.0f), (BFloat16)(2.0f), (BFloat16)(0.0f) };
        }

        [Theory]
        [MemberData(nameof(Lerp_TestData))]
        public static void LerpTest(BFloat16 value1, BFloat16 value2, BFloat16 amount, BFloat16 expectedResult)
        {
            AssertEqual(+expectedResult, BFloat16.Lerp(+value1, +value2, amount), BFloat16.Zero);
            AssertEqual((expectedResult == BFloat16.Zero) ? expectedResult : -expectedResult, BFloat16.Lerp(-value1, -value2, amount), BFloat16.Zero);
        }

        public static IEnumerable<object[]> DegreesToRadians_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)(0.3184f), (BFloat16)(0.005554f), CrossPlatformMachineEpsilon };       // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.4343f), (BFloat16)(0.00758f), CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.5f), (BFloat16)(0.00872f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)(0.01111f), CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6934f), (BFloat16)(0.0121f), CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)(0.01234f), CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.785f), (BFloat16)(0.0137f), CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { (BFloat16)(1.0f), (BFloat16)(0.01744f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(1.128f), (BFloat16)(0.01968f), CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)(0.02467f), CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.442f), (BFloat16)(0.02518f), CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.5f), (BFloat16)(0.02617f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)(0.0274f), CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.0f), (BFloat16)(0.03488f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(2.303f), (BFloat16)(0.04016f), CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.5f), (BFloat16)(0.0436f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)(0.04742f), CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)(3.0f), (BFloat16)(0.05234f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(3.14f), (BFloat16)(0.0548f), CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)(3.5f), (BFloat16)(0.06107f), CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(DegreesToRadians_TestData))]
        public static void DegreesToRadiansTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.DegreesToRadians(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.DegreesToRadians(+value), allowedVariance);
        }

        public static IEnumerable<object[]> RadiansToDegrees_TestData()
        {
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)(0.005554f), (BFloat16)(0.3184f), CrossPlatformMachineEpsilon };       // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.00758f), (BFloat16)(0.4343f), CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.00872f), (BFloat16)(0.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.01111f), (BFloat16)(0.6367f), CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.0121f), (BFloat16)(0.6934f), CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.01234f), (BFloat16)(0.707f), CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.0137f), (BFloat16)(0.785f), CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { (BFloat16)(0.01744f), (BFloat16)(1.0f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.01968f), (BFloat16)(1.128f), CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(0.02467f), (BFloat16)(1.414f), CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(0.02518f), (BFloat16)(1.442f), CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)(0.02617f), (BFloat16)(1.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.0274f), (BFloat16)(1.57f), CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)(0.03488f), (BFloat16)(2.0f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.04016f), (BFloat16)(2.303f), CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)(0.0436f), (BFloat16)(2.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.04742f), (BFloat16)(2.719f), CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)(0.05234f), (BFloat16)(3.0f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.0548f), (BFloat16)(3.14f), CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)(0.06107f), (BFloat16)(3.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.PositiveInfinity, BFloat16.PositiveInfinity, BFloat16.Zero };
        }

        [Theory]
        [MemberData(nameof(RadiansToDegrees_TestData))]
        public static void RadiansToDegreesTest(BFloat16 value, BFloat16 expectedResult, BFloat16 allowedVariance)
        {
            AssertEqual(-expectedResult, BFloat16.RadiansToDegrees(-value), allowedVariance);
            AssertEqual(+expectedResult, BFloat16.RadiansToDegrees(+value), allowedVariance);
        }

        #region AssertExtentions
        static bool IsNegativeZero(BFloat16 value)
        {
            return BFloat16ToUInt16Bits(value) == 0x8000;
        }

        static bool IsPositiveZero(BFloat16 value)
        {
            return BFloat16ToUInt16Bits(value) == 0;
        }

        static string ToStringPadded(BFloat16 value)
        {
            if (BFloat16.IsNaN(value))
            {
                return "NaN".PadLeft(5);
            }
            else if (BFloat16.IsPositiveInfinity(value))
            {
                return "+\u221E".PadLeft(5);
            }
            else if (BFloat16.IsNegativeInfinity(value))
            {
                return "-\u221E".PadLeft(5);
            }
            else if (IsNegativeZero(value))
            {
                return "-0.0".PadLeft(5);
            }
            else if (IsPositiveZero(value))
            {
                return "+0.0".PadLeft(5);
            }
            else
            {
                return $"{value,5:G5}";
            }
        }

        /// <summary>Verifies that two <see cref="BFloat16"/> values's binary representations are identical.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <exception cref="EqualException">Thrown when the representations are not identical</exception>
        private static void AssertEqual(BFloat16 expected, BFloat16 actual)
        {
            if (BFloat16ToUInt16Bits(expected) == BFloat16ToUInt16Bits(actual))
            {
                return;
            }

            if (PlatformDetection.IsRiscV64Process && BFloat16.IsNaN(expected) && BFloat16.IsNaN(actual))
            {
                // RISC-V does not preserve payload
                return;
            }

            throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
        }

        /// <summary>Verifies that two <see cref="BFloat16"/> values are equal, within the <paramref name="variance"/>.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        private static void AssertEqual(BFloat16 expected, BFloat16 actual, BFloat16 variance)
        {
            if (BFloat16.IsNaN(expected))
            {
                if (BFloat16.IsNaN(actual))
                {
                    return;
                }

                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (BFloat16.IsNaN(actual))
            {
                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (BFloat16.IsNegativeInfinity(expected))
            {
                if (BFloat16.IsNegativeInfinity(actual))
                {
                    return;
                }

                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (BFloat16.IsNegativeInfinity(actual))
            {
                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (BFloat16.IsPositiveInfinity(expected))
            {
                if (BFloat16.IsPositiveInfinity(actual))
                {
                    return;
                }

                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (BFloat16.IsPositiveInfinity(actual))
            {
                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (IsNegativeZero(expected))
            {
                if (IsNegativeZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsNegativeZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            if (IsPositiveZero(expected))
            {
                if (IsPositiveZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsPositiveZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            BFloat16 delta = (BFloat16)Math.Abs((float)actual - (float)expected);

            if (delta > variance)
            {
                throw EqualException.ForMismatchedValues(ToStringPadded(expected), ToStringPadded(actual));
            }
        }
        #endregion
    }
}
