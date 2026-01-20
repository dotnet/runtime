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

        [Fact]
        public static void Epsilon()
        {
            Assert.Equal(0x0001u, BitConverter.BFloat16ToUInt16Bits(BFloat16.Epsilon));
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal(0x7F80u, BitConverter.BFloat16ToUInt16Bits(BFloat16.PositiveInfinity));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal(0xFF80u, BitConverter.BFloat16ToUInt16Bits(BFloat16.NegativeInfinity));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal(0xFFC0u, BitConverter.BFloat16ToUInt16Bits(BFloat16.NaN));
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(0xFF7Fu, BitConverter.BFloat16ToUInt16Bits(BFloat16.MinValue));
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7F7Fu, BitConverter.BFloat16ToUInt16Bits(BFloat16.MaxValue));
        }

        [Fact]
        public static void Ctor_Empty()
        {
            var value = new BFloat16();
            Assert.Equal(0x0000, BitConverter.BFloat16ToUInt16Bits(value));
        }

        public static IEnumerable<object[]> IsFinite_TestData()
        {
            yield return new object[] { BFloat16.NegativeInfinity, false };                  // Negative Infinity
            yield return new object[] { BFloat16.MinValue, true };                           // Min Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8400), true };   // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x83FF), true };   // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), true };   // Max Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), true };   // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), true };   // Positive Zero
            yield return new object[] { BFloat16.Epsilon, true };                            // Min Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x03FF), true };   // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0400), true };   // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, true };                                // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), true };   // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), true };   // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), true };   // Max Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), true };   // Negative Zero
            yield return new object[] { BFloat16.NaN, true };                                // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), true };   // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), true };   // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), false };  // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), false };  // Max Negative Subnormal (Negative Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, false };                           // Min Positive Subnormal (Positive Epsilon)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), false };  // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8080), false };  // Max Negative Normal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x807F), true };   // Min Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8001), true };   // Max Negative Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), false };  // Negative Zero
            yield return new object[] { BFloat16.NaN, false };                               // NaN
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), false };  // Positive Zero
            yield return new object[] { BFloat16.Epsilon, true };                            // Min Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x007F), true };   // Max Positive Subnormal
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0080), false };  // Min Positive Normal
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
            yield return new object[] { BFloat16.Epsilon, BitConverter.UInt16BitsToBFloat16(0x8001), 1 };
            yield return new object[] { BFloat16.MaxValue, BitConverter.UInt16BitsToBFloat16(0x0000), 1 };
            yield return new object[] { BFloat16.MaxValue, BFloat16.Epsilon, 1 };
            yield return new object[] { BFloat16.MaxValue, BFloat16.PositiveInfinity, -1 };
            yield return new object[] { BFloat16.MinValue, BFloat16.MaxValue, -1 };
            yield return new object[] { BFloat16.MaxValue, BFloat16.NaN, 1 };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, 0 };
            yield return new object[] { BFloat16.NaN, BitConverter.UInt16BitsToBFloat16(0x0000), -1 };
            yield return new object[] { BFloat16.MaxValue, null, 1 };
            yield return new object[] { BFloat16.MinValue, BFloat16.NegativeInfinity, 1 };
            yield return new object[] { BFloat16.NegativeInfinity, BFloat16.MinValue, -1 };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), BFloat16.NegativeInfinity, 1 }; // Negative zero
            yield return new object[] { BFloat16.NegativeInfinity, BitConverter.UInt16BitsToBFloat16(0x8000), -1 }; // Negative zero
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
            yield return new object[] { BFloat16.MaxValue, BitConverter.UInt16BitsToBFloat16(0x0000), false };
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
                (BitConverter.UInt16BitsToBFloat16(0b0_01111111_0000000), 1f), // 1
                (BitConverter.UInt16BitsToBFloat16(0b1_01111111_0000000), -1f), // -1
                (BFloat16.MaxValue, BitConverter.UInt32BitsToSingle(0x7F7F0000)), // 3.3895314E+38
                (BFloat16.MinValue, BitConverter.UInt32BitsToSingle(0xFF7F0000)), // -3.3895314E+38
                (BitConverter.UInt16BitsToBFloat16(0b0_01111011_1001101), 0.10009765625f), // 0.1ish
                (BitConverter.UInt16BitsToBFloat16(0b1_01111011_1001101), -0.10009765625f), // -0.1ish
                (BitConverter.UInt16BitsToBFloat16(0b0_10000100_0101000), 42f), // 42
                (BitConverter.UInt16BitsToBFloat16(0b1_10000100_0101000), -42f), // -42
                (BFloat16.PositiveInfinity, float.PositiveInfinity), // PosInfinity
                (BFloat16.NegativeInfinity, float.NegativeInfinity), // NegInfinity
                (BitConverter.UInt16BitsToBFloat16(0b0_11111111_1000000), BitConverter.UInt32BitsToSingle(0x7FC00000)), // Positive Quiet NaN
                (BFloat16.NaN, float.NaN), // Negative Quiet NaN
                (BitConverter.UInt16BitsToBFloat16(0b0_11111111_1010101), BitConverter.UInt32BitsToSingle(0x7FD50000)), // Positive Signalling NaN - Should preserve payload
                (BitConverter.UInt16BitsToBFloat16(0b1_11111111_1010101), BitConverter.UInt32BitsToSingle(0xFFD50000)), // Negative Signalling NaN - Should preserve payload
                (BFloat16.Epsilon, BitConverter.UInt32BitsToSingle(0x00010000)), // PosEpsilon = 9.1835E-41
                (BitConverter.UInt16BitsToBFloat16(0), 0), // 0
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000), -0f), // -0
                (BitConverter.UInt16BitsToBFloat16(0b0_10000000_1001001), 3.140625f), // 3.140625
                (BitConverter.UInt16BitsToBFloat16(0b1_10000000_1001001), -3.140625f), // -3.140625
                (BitConverter.UInt16BitsToBFloat16(0b0_10000000_0101110), 2.71875f), // 2.71875
                (BitConverter.UInt16BitsToBFloat16(0b1_10000000_0101110), -2.71875f), // -2.71875
                (BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000000), 1.5f), // 1.5
                (BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000000), -1.5f), // -1.5
                (BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000001), 1.5078125f), // 1.5078125
                (BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000001), -1.5078125f), // -1.5078125
                (BitConverter.UInt16BitsToBFloat16(0b0_00000001_0000000), BitConverter.UInt32BitsToSingle(0x00800000)), // smallest normal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_1111111), BitConverter.UInt32BitsToSingle(0x007F0000)), // largest subnormal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000000), BitConverter.UInt32BitsToSingle(0x00400000)), // middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_0111111), BitConverter.UInt32BitsToSingle(0x003F0000)), // just below middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000001), BitConverter.UInt32BitsToSingle(0x00010000)), // smallest subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000001), BitConverter.UInt32BitsToSingle(0x80010000)), // highest negative subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0111111), BitConverter.UInt32BitsToSingle(0x803F0000)), // just above negative middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000000), BitConverter.UInt32BitsToSingle(0x80400000)), // negative middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_1111111), BitConverter.UInt32BitsToSingle(0x807F0000)), // lowest negative subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000001_0000000), BitConverter.UInt32BitsToSingle(0x80800000)) // highest negative normal
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
                (BitConverter.UInt16BitsToBFloat16(0b0_01111111_0000000), 1d), // 1
                (BitConverter.UInt16BitsToBFloat16(0b1_01111111_0000000), -1d), // -1
                (BFloat16.MaxValue, BitConverter.UInt64BitsToDouble(0x47EFE000_00000000)), // 3.3895314E+38
                (BFloat16.MinValue, BitConverter.UInt64BitsToDouble(0xC7EFE000_00000000)), // -3.3895314E+38
                (BitConverter.UInt16BitsToBFloat16(0b0_01111011_1001101), 0.10009765625d), // 0.1ish
                (BitConverter.UInt16BitsToBFloat16(0b1_01111011_1001101), -0.10009765625d), // -0.1ish
                (BitConverter.UInt16BitsToBFloat16(0b0_10000100_0101000), 42d), // 42
                (BitConverter.UInt16BitsToBFloat16(0b1_10000100_0101000), -42d), // -42
                (BFloat16.PositiveInfinity, double.PositiveInfinity), // PosInfinity
                (BFloat16.NegativeInfinity, double.NegativeInfinity), // NegInfinity
                (BitConverter.UInt16BitsToBFloat16(0b0_11111111_1000000), BitConverter.UInt64BitsToDouble(0x7FF80000_00000000)), // Positive Quiet NaN
                (BFloat16.NaN, double.NaN), // Negative Quiet NaN
                (BitConverter.UInt16BitsToBFloat16(0b0_11111111_1010101), BitConverter.UInt64BitsToDouble(0x7FFAA000_00000000)), // Positive Signalling NaN - Should preserve payload
                (BitConverter.UInt16BitsToBFloat16(0b1_11111111_1010101), BitConverter.UInt64BitsToDouble(0xFFFAA000_00000000)), // Negative Signalling NaN - Should preserve payload
                (BFloat16.Epsilon, BitConverter.UInt64BitsToDouble(0x37A00000_00000000)), // PosEpsilon = 9.1835E-41
                (BitConverter.UInt16BitsToBFloat16(0), 0d), // 0
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000), -0d), // -0
                (BitConverter.UInt16BitsToBFloat16(0b0_10000000_1001001), 3.140625d), // 3.140625
                (BitConverter.UInt16BitsToBFloat16(0b1_10000000_1001001), -3.140625d), // -3.140625
                (BitConverter.UInt16BitsToBFloat16(0b0_10000000_0101110), 2.71875d), // 2.71875
                (BitConverter.UInt16BitsToBFloat16(0b1_10000000_0101110), -2.71875d), // -2.71875
                (BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000000), 1.5d), // 1.5
                (BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000000), -1.5d), // -1.5
                (BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000001), 1.5078125d), // 1.5078125
                (BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000001), -1.5078125d), // -1.5078125
                (BitConverter.UInt16BitsToBFloat16(0b0_00000001_0000000), BitConverter.UInt64BitsToDouble(0x3810000000000000)), // smallest normal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_1111111), BitConverter.UInt64BitsToDouble(0x380FC00000000000)), // largest subnormal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000000), BitConverter.UInt64BitsToDouble(0x3800000000000000)), // middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_0111111), BitConverter.UInt64BitsToDouble(0x37FF800000000000)), // just below middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000001), BitConverter.UInt64BitsToDouble(0x37A0000000000000)), // smallest subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000001), BitConverter.UInt64BitsToDouble(0xB7A0000000000000)), // highest negative subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0111111), BitConverter.UInt64BitsToDouble(0xB7FF800000000000)), // just above negative middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000000), BitConverter.UInt64BitsToDouble(0xB800000000000000)), // negative middle subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000000_1111111), BitConverter.UInt64BitsToDouble(0xB80FC00000000000)), // lowest negative subnormal
                (BitConverter.UInt16BitsToBFloat16(0b1_00000001_0000000), BitConverter.UInt64BitsToDouble(0xB810000000000000)) // highest negative normal
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
                (MathF.PI, BitConverter.UInt16BitsToBFloat16(0b0_10000000_1001001)), // 3.140625
                (MathF.E, BitConverter.UInt16BitsToBFloat16(0b0_10000000_0101110)), // 2.71875
                (-MathF.PI, BitConverter.UInt16BitsToBFloat16(0b1_10000000_1001001)), // -3.140625
                (-MathF.E, BitConverter.UInt16BitsToBFloat16(0b1_10000000_0101110)), // -2.71875
                (float.MaxValue, BitConverter.UInt16BitsToBFloat16(0b0_11111111_0000000)), // Overflow
                (float.MinValue, BitConverter.UInt16BitsToBFloat16(0b1_11111111_0000000)), // Overflow
                (float.PositiveInfinity, BFloat16.PositiveInfinity), // Overflow
                (float.NegativeInfinity, BFloat16.NegativeInfinity), // Overflow
                (float.NaN, BFloat16.NaN), // Quiet Negative NaN
                (BitConverter.UInt32BitsToSingle(0x7FC00000), BitConverter.UInt16BitsToBFloat16(0b0_11111111_1000000)), // Quiet Positive NaN
                (BitConverter.UInt32BitsToSingle(0xFFD55555), BitConverter.UInt16BitsToBFloat16(0b1_11111111_1010101)), // Signalling Negative NaN
                (BitConverter.UInt32BitsToSingle(0x7FD55555), BitConverter.UInt16BitsToBFloat16(0b0_11111111_1010101)), // Signalling Positive NaN
                (float.Epsilon, BitConverter.UInt16BitsToBFloat16(0)), // Underflow
                (-float.Epsilon, BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000)), // Underflow
                (1f, BitConverter.UInt16BitsToBFloat16(0b0_01111111_0000000)), // 1
                (-1f, BitConverter.UInt16BitsToBFloat16(0b1_01111111_0000000)), // -1
                (0f, BitConverter.UInt16BitsToBFloat16(0)), // 0
                (-0f, BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000)), // -0
                (42f, BitConverter.UInt16BitsToBFloat16(0b0_10000100_0101000)), // 42
                (-42f, BitConverter.UInt16BitsToBFloat16(0b1_10000100_0101000)), // -42
                (0.1f, BitConverter.UInt16BitsToBFloat16(0b0_01111011_1001101)), // 0.10009765625
                (-0.1f, BitConverter.UInt16BitsToBFloat16(0b1_01111011_1001101)), // -0.10009765625
                (1.5f, BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000000)), // 1.5
                (-1.5f, BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000000)), // -1.5
                (1.5078125f, BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000001)), // 1.5078125
                (-1.5078125f, BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000001)), // -1.5078125
                (BitConverter.UInt32BitsToSingle(0x00800000), BitConverter.UInt16BitsToBFloat16(0b0_00000001_0000000)), // smallest normal
                (BitConverter.UInt32BitsToSingle(0x007F0000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_1111111)), // largest subnormal
                (BitConverter.UInt32BitsToSingle(0x00400000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000000)), // middle subnormal
                (BitConverter.UInt32BitsToSingle(0x003F0000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_0111111)), // just below middle subnormal
                (BitConverter.UInt32BitsToSingle(0x00010000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000001)), // smallest subnormal
                (BitConverter.UInt32BitsToSingle(0x80010000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000001)), // highest negative subnormal
                (BitConverter.UInt32BitsToSingle(0x803F0000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_0111111)), // just above negative middle subnormal
                (BitConverter.UInt32BitsToSingle(0x80400000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000000)), // negative middle subnormal
                (BitConverter.UInt32BitsToSingle(0x807F0000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_1111111)), // lowest negative subnormal
                (BitConverter.UInt32BitsToSingle(0x80800000), BitConverter.UInt16BitsToBFloat16(0b1_00000001_0000000)), // highest negative normal
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000111000000000000001),
                                  BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052+ULP rounds up
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000111000000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052 rounds to even
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000110111111111111111),
                                  BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000011)), // 1052-ULP rounds down
                (BitConverter.UInt32BitsToSingle(0b0_10001001_00000101000000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000010)), // 1044 rounds to even
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000110111111111111111),
                                  BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000011)), // -1052+ULP rounds towards zero
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000111000000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052 rounds to even
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000111000000000000001),
                                  BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052-ULP rounds away from zero
                (BitConverter.UInt32BitsToSingle(0b1_10001001_00000101000000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000010)), // -1044 rounds to even
                (BitConverter.UInt32BitsToSingle(0b0_00000000_10000111000000000000001),
                                  BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal + ULP rounds up
                (BitConverter.UInt32BitsToSingle(0b0_00000000_10000111000000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal rounds to even
                (BitConverter.UInt32BitsToSingle(0b0_00000000_10000110111111111111111),
                                  BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000011)), // subnormal - ULP rounds down
                (BitConverter.UInt32BitsToSingle(0b1_00000000_10000110111111111111111),
                                  BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000011)), // neg subnormal + ULP rounds higher
                (BitConverter.UInt32BitsToSingle(0b1_00000000_10000111000000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal rounds to even
                (BitConverter.UInt32BitsToSingle(0b1_00000000_10000111000000000000001),
                                  BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal - ULP rounds lower,
                (BitConverter.UInt32BitsToSingle(0b0_00000000_00000000110000000000000),
                                  BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000000)), // (BFloat16 minimum subnormal / 2) should underflow to zero
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
                (Math.PI, BitConverter.UInt16BitsToBFloat16(0b0_10000000_1001001)), // 3.140625
                (Math.E, BitConverter.UInt16BitsToBFloat16(0b0_10000000_0101110)), // 2.71875
                (-Math.PI, BitConverter.UInt16BitsToBFloat16(0b1_10000000_1001001)), // -3.140625
                (-Math.E, BitConverter.UInt16BitsToBFloat16(0b1_10000000_0101110)), // -2.71875
                (double.MaxValue, BFloat16.PositiveInfinity), // Overflow
                (double.MinValue, BFloat16.NegativeInfinity), // Overflow
                (double.PositiveInfinity, BFloat16.PositiveInfinity), // Overflow
                (double.NegativeInfinity, BFloat16.NegativeInfinity), // Overflow
                (double.NaN, BFloat16.NaN), // Quiet Negative NaN
                (BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), BitConverter.UInt16BitsToBFloat16(0b0_11111111_1000000)), // Quiet Positive NaN
                (BitConverter.UInt64BitsToDouble(0xFFFAAAAA_AAAAAAAA), BitConverter.UInt16BitsToBFloat16(0b1_11111111_1010101)), // Signalling Negative NaN
                (BitConverter.UInt64BitsToDouble(0x7FFAAAAA_AAAAAAAA), BitConverter.UInt16BitsToBFloat16(0b0_11111111_1010101)), // Signalling Positive NaN
                (double.Epsilon, BitConverter.UInt16BitsToBFloat16(0)), // Underflow
                (-double.Epsilon, BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000)), // Underflow
                (1f, BitConverter.UInt16BitsToBFloat16(0b0_01111111_0000000)), // 1
                (-1d, BitConverter.UInt16BitsToBFloat16(0b1_01111111_0000000)), // -1
                (0d, BitConverter.UInt16BitsToBFloat16(0)), // 0
                (-0d, BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000)), // -0
                (42d, BitConverter.UInt16BitsToBFloat16(0b0_10000100_0101000)), // 42
                (-42d, BitConverter.UInt16BitsToBFloat16(0b1_10000100_0101000)), // -42
                (0.1d, BitConverter.UInt16BitsToBFloat16(0b0_01111011_1001101)), // 0.10009765625
                (-0.1d, BitConverter.UInt16BitsToBFloat16(0b1_01111011_1001101)), // -0.10009765625
                (1.5d, BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000000)), // 1.5
                (-1.5d, BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000000)), // -1.5
                (1.5078125d, BitConverter.UInt16BitsToBFloat16(0b0_01111111_1000001)), // 1.5078125
                (-1.5078125d, BitConverter.UInt16BitsToBFloat16(0b1_01111111_1000001)), // -1.5078125
                (BitConverter.UInt64BitsToDouble(0x3810000000000000), BitConverter.UInt16BitsToBFloat16(0b0_00000001_0000000)), // smallest normal
                (BitConverter.UInt64BitsToDouble(0x380FC00000000000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_1111111)), // largest subnormal
                (BitConverter.UInt64BitsToDouble(0x3800000000000000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000000)), // middle subnormal
                (BitConverter.UInt64BitsToDouble(0x37FF800000000000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_0111111)), // just below middle subnormal
                (BitConverter.UInt64BitsToDouble(0x37A0000000000000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000001)), // smallest subnormal
                (BitConverter.UInt64BitsToDouble(0xB7A0000000000000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000001)), // highest negative subnormal
                (BitConverter.UInt64BitsToDouble(0xB7FF800000000000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_0111111)), // just above negative middle subnormal
                (BitConverter.UInt64BitsToDouble(0xB800000000000000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000000)), // negative middle subnormal
                (BitConverter.UInt64BitsToDouble(0xB80FC00000000000), BitConverter.UInt16BitsToBFloat16(0b1_00000000_1111111)), // lowest negative subnormal
                (BitConverter.UInt64BitsToDouble(0xB810000000000000), BitConverter.UInt16BitsToBFloat16(0b1_00000001_0000000)), // highest negative normal
                (BitConverter.UInt64BitsToDouble(0x4090700000000001),
                    BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052+ULP rounds up
                (BitConverter.UInt64BitsToDouble(0x4090700000000000),
                    BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000100)), // 1052 rounds to even
                (BitConverter.UInt64BitsToDouble(0x40906FFFFFFFFFFF),
                    BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000011)), // 1052-ULP rounds down
                (BitConverter.UInt64BitsToDouble(0x4090500000000000),
                    BitConverter.UInt16BitsToBFloat16(0b0_10001001_0000010)), // 1044 rounds to even
                (BitConverter.UInt64BitsToDouble(0xC0906FFFFFFFFFFF),
                    BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000011)), // -1052+ULP rounds towards zero
                (BitConverter.UInt64BitsToDouble(0xC090700000000000),
                    BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052 rounds to even
                (BitConverter.UInt64BitsToDouble(0xC090700000000001),
                    BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000100)), // -1052-ULP rounds away from zero
                (BitConverter.UInt64BitsToDouble(0xC090500000000000),
                    BitConverter.UInt16BitsToBFloat16(0b1_10001001_0000010)), // -1044 rounds to even
                (BitConverter.UInt64BitsToDouble(0x3800E00000000001),
                    BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal + ULP rounds up
                (BitConverter.UInt64BitsToDouble(0x3800E00000000000),
                    BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000100)), // subnormal rounds to even
                (BitConverter.UInt64BitsToDouble(0x3800DFFFFFFFFFFF),
                    BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000011)), // subnormal - ULP rounds down
                (BitConverter.UInt64BitsToDouble(0xB800DFFFFFFFFFFF),
                    BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000011)), // neg subnormal + ULP rounds higher
                (BitConverter.UInt64BitsToDouble(0xB800E00000000000),
                    BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal rounds to even
                (BitConverter.UInt64BitsToDouble(0xB800E00000000001),
                    BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000100)), // neg subnormal - ULP rounds lower
                (BitConverter.UInt64BitsToDouble(0x3788000000000000), BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000000)), // (BFloat16 minimum subnormal / 2) should underflow to zero
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

        public static IEnumerable<object[]> ExplicitConversion_FromInt32_TestData()
        {
            (int, BFloat16)[] data =
            {
                (-0x103_0001, BitConverter.UInt16BitsToBFloat16(0xCB82)), // -16973824 - 1 rounds lower
                (-0x103_0000, BitConverter.UInt16BitsToBFloat16(0xCB82)), // -16973824 rounds to even
                (-0x102_FFFF, BitConverter.UInt16BitsToBFloat16(0xCB81)), // -16973824 + 1 rounds higher
                (-0x101_0001, BitConverter.UInt16BitsToBFloat16(0xCB81)), // -16842752 - 1 rounds lower
                (-0x101_0000, BitConverter.UInt16BitsToBFloat16(0xCB80)), // -16842752 rounds to even
                (-0x100_FFFF, BitConverter.UInt16BitsToBFloat16(0xCB80)), // -16842752 + 1 rounds higher
                (0, BFloat16.Zero),
                (0x100_FFFF, BitConverter.UInt16BitsToBFloat16(0x4B80)), // 16842752 - 1 rounds lower
                (0x101_0000, BitConverter.UInt16BitsToBFloat16(0x4B80)), // 16842752 rounds to even
                (0x101_0001, BitConverter.UInt16BitsToBFloat16(0x4B81)), // 16842752 + 1 rounds higher
                (0x102_FFFF, BitConverter.UInt16BitsToBFloat16(0x4B81)), // 16973824 - 1 rounds lower
                (0x103_0000, BitConverter.UInt16BitsToBFloat16(0x4B82)), // 16973824 rounds to even
                (0x103_0001, BitConverter.UInt16BitsToBFloat16(0x4B82)), // 16973824 + 1 rounds higher
                (int.MinValue, BitConverter.UInt16BitsToBFloat16(0xCF00)),
            };

            foreach ((int original, BFloat16 expected) in data)
            {
                yield return new object[] { original, expected };
            }
        }

        [MemberData(nameof(ExplicitConversion_FromInt32_TestData))]
        [Theory]
        public static void ExplicitConversion_FromInt32(int i, BFloat16 expected)
        {
            BFloat16 b16 = (BFloat16)i;
            AssertEqual(expected, b16);
        }

        public static IEnumerable<object[]> ExplicitConversion_FromUInt32_TestData()
        {
            (uint, BFloat16)[] data =
            {
                (0, BFloat16.Zero),
                (0x100_FFFF, BitConverter.UInt16BitsToBFloat16(0x4B80)), // 16842752 - 1 rounds lower
                (0x101_0000, BitConverter.UInt16BitsToBFloat16(0x4B80)), // 16842752 rounds to even
                (0x101_0001, BitConverter.UInt16BitsToBFloat16(0x4B81)), // 16842752 + 1 rounds higher
                (0x102_FFFF, BitConverter.UInt16BitsToBFloat16(0x4B81)), // 16973824 - 1 rounds lower
                (0x103_0000, BitConverter.UInt16BitsToBFloat16(0x4B82)), // 16973824 rounds to even
                (0x103_0001, BitConverter.UInt16BitsToBFloat16(0x4B82)), // 16973824 + 1 rounds higher
                (0x8000_0000, BitConverter.UInt16BitsToBFloat16(0x4F00)),
                (0xFF7F_FFFF, BitConverter.UInt16BitsToBFloat16(0x4F7F)), // 4286578688 - 1 rounds lower
                (0xFF80_0000, BitConverter.UInt16BitsToBFloat16(0x4F80)), // 4286578688 rounds to even
                (0xFF80_0001, BitConverter.UInt16BitsToBFloat16(0x4F80)), // 4286578688 + 1 rounds higher
                (0xFFFF_FFFF, BitConverter.UInt16BitsToBFloat16(0x4F80)),
            };

            foreach ((uint original, BFloat16 expected) in data)
            {
                yield return new object[] { original, expected };
            }
        }

        [MemberData(nameof(ExplicitConversion_FromUInt32_TestData))]
        [Theory]
        public static void ExplicitConversion_FromUInt32(uint i, BFloat16 expected)
        {
            BFloat16 b16 = (BFloat16)i;
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

            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b0_00000001_0000000)) }; // smallest normal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b0_00000000_1111111)) }; // largest subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b0_00000000_1000000)) }; // middle subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b0_00000000_0111111)) }; // just below middle subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b0_00000000_0000000)) }; // smallest subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0000000)) }; // highest negative subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b1_00000000_0111111)) }; // just above negative middle subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b1_00000000_1000000)) }; // negative middle subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b1_00000000_1111111)) }; // lowest negative subnormal
            yield return new object[] { (BitConverter.UInt16BitsToBFloat16(0b1_00000001_0000000)) }; // highest negative normal
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
            yield return new object[] { (BFloat16)(-3.141f), (BFloat16)(-0.957f), CrossPlatformMachineEpsilon };             // value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), (BFloat16)(-0.9336f), CrossPlatformMachineEpsilon };             // value: -(e)
            yield return new object[] { (BFloat16)(-2.297f), (BFloat16)(-0.8984f), CrossPlatformMachineEpsilon };             // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57f), (BFloat16)(-0.793f), CrossPlatformMachineEpsilon };             // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.445f), (BFloat16)(-0.7656f), CrossPlatformMachineEpsilon };             // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.414f), (BFloat16)(-0.7578f), CrossPlatformMachineEpsilon };             // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.125f), (BFloat16)(-0.6758f), CrossPlatformMachineEpsilon };             // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(-0.6328f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.7852f), (BFloat16)(-0.543f), CrossPlatformMachineEpsilon };             // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707f), (BFloat16)(-0.5078f), CrossPlatformMachineEpsilon };             // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.6914f), (BFloat16)(-0.5f), CrossPlatformMachineEpsilon };             // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.6367f), (BFloat16)(-0.4707f), CrossPlatformMachineEpsilon };             // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.4336f), (BFloat16)(-0.3516f), CrossPlatformMachineEpsilon };             // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.3184f), (BFloat16)(-0.2734f), CrossPlatformMachineEpsilon };             // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(0.0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.3184f), (BFloat16)(0.375f), CrossPlatformMachineEpsilon };             // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.4336f), (BFloat16)(0.543f), CrossPlatformMachineEpsilon };             // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)(0.8906f), CrossPlatformMachineEpsilon };             // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6914f), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)(1.031f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7852f), (BFloat16)(1.195f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(1.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.125f), (BFloat16)(2.078f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)(3.109f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.445f), (BFloat16)(3.25f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)(3.812f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.297f), (BFloat16)(8.938f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)(14.19f), CrossPlatformMachineEpsilon * (BFloat16)100 }; // value:  (e)
            yield return new object[] { (BFloat16)(3.141f), (BFloat16)(22.12f), CrossPlatformMachineEpsilon * (BFloat16)100 }; // value:  (pi)
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
            yield return new object[] { (BFloat16)(-3.141f), (BFloat16)(0.1133f), CrossPlatformMachineEpsilon };        // value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), (BFloat16)(0.1523f), CrossPlatformMachineEpsilon };        // value: -(e)
            yield return new object[] { (BFloat16)(-2.297f), (BFloat16)(0.2031f), CrossPlatformMachineEpsilon };        // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57f), (BFloat16)(0.3359f), CrossPlatformMachineEpsilon };        // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.445f), (BFloat16)(0.3672f), CrossPlatformMachineEpsilon };        // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.414f), (BFloat16)(0.375f), CrossPlatformMachineEpsilon };        // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.125f), (BFloat16)(0.459f), CrossPlatformMachineEpsilon };        // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(0.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.7852f), (BFloat16)(0.582f), CrossPlatformMachineEpsilon };        // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707f), (BFloat16)(0.6133f), CrossPlatformMachineEpsilon };        // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.6914f), (BFloat16)(0.6211f), CrossPlatformMachineEpsilon };        // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.6367f), (BFloat16)(0.6445f), CrossPlatformMachineEpsilon };        // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.4336f), (BFloat16)(0.7422f), CrossPlatformMachineEpsilon };        // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.3184f), (BFloat16)(0.8008f), CrossPlatformMachineEpsilon };        // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(BFloat16.One), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)(BFloat16.One), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.3184f), (BFloat16)(1.25f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.4336f), (BFloat16)(1.352f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)(1.555f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6914f), (BFloat16)(1.617f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)(1.633f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7852f), (BFloat16)(1.727f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(2.0f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.125f), (BFloat16)(2.188f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)(2.672f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.445f), (BFloat16)(2.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)(2.969f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.297f), (BFloat16)(4.906f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)(6.594f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (e)
            yield return new object[] { (BFloat16)(3.141f), (BFloat16)(8.812f), CrossPlatformMachineEpsilon * (BFloat16)10 };   // value:  (pi)
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
            yield return new object[] { (BFloat16)(-3.141f), (BFloat16)(-0.8867f), CrossPlatformMachineEpsilon };            // value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), (BFloat16)(-0.8477f), CrossPlatformMachineEpsilon };            // value: -(e)
            yield return new object[] { (BFloat16)(-2.297f), (BFloat16)(-0.7969f), CrossPlatformMachineEpsilon };            // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57f), (BFloat16)(-0.6641f), CrossPlatformMachineEpsilon };            // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.445f), (BFloat16)(-0.6328f), CrossPlatformMachineEpsilon };            // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.414f), (BFloat16)(-0.625f), CrossPlatformMachineEpsilon };            // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.125f), (BFloat16)(-0.543f), CrossPlatformMachineEpsilon };            // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(-0.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.7852f), (BFloat16)(-0.4199f), CrossPlatformMachineEpsilon };            // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707f), (BFloat16)(-0.3867f), CrossPlatformMachineEpsilon };            // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.6914f), (BFloat16)(-0.3809f), CrossPlatformMachineEpsilon };            // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.6367f), (BFloat16)(-0.3574f), CrossPlatformMachineEpsilon };            // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.4336f), (BFloat16)(-0.2598f), CrossPlatformMachineEpsilon };            // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.3184f), (BFloat16)(-0.1982f), CrossPlatformMachineEpsilon };            // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.3184f), (BFloat16)(0.2471f), CrossPlatformMachineEpsilon };            // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.4336f), (BFloat16)(0.3516f), CrossPlatformMachineEpsilon };            // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)(0.5547f), CrossPlatformMachineEpsilon };            // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6914f), (BFloat16)(0.6133f), CrossPlatformMachineEpsilon };            // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)(0.6328f), CrossPlatformMachineEpsilon };            // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7852f), (BFloat16)(0.7227f), CrossPlatformMachineEpsilon };            // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.125f), (BFloat16)(1.18f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)(1.664f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.445f), (BFloat16)(1.727f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)(1.969f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.297f), (BFloat16)(3.906f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)(5.594f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (e)
            yield return new object[] { (BFloat16)(3.141f), (BFloat16)(7.812f), CrossPlatformMachineEpsilon * (BFloat16)10 }; // value:  (pi)
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
            yield return new object[] { (BFloat16)(-3.141f), (BFloat16)0.0007248f, CrossPlatformMachineEpsilon / (BFloat16)1000 };  // value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), (BFloat16)0.001907f, CrossPlatformMachineEpsilon / (BFloat16)100 };   // value: -(e)
            yield return new object[] { (BFloat16)(-2.297f), (BFloat16)0.005035f, CrossPlatformMachineEpsilon / (BFloat16)100 };   // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57f), (BFloat16)0.02686f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.445f), (BFloat16)0.03589f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.414f), (BFloat16)0.03857f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.125f), (BFloat16)0.0752f, CrossPlatformMachineEpsilon / (BFloat16)10 };    // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)0.1f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.7852f), (BFloat16)0.1641f, CrossPlatformMachineEpsilon };         // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707f), (BFloat16)0.1963f, CrossPlatformMachineEpsilon };         // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.6914f), (BFloat16)0.2031f, CrossPlatformMachineEpsilon };         // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.6367f), (BFloat16)0.2305f, CrossPlatformMachineEpsilon };         // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.4336f), (BFloat16)0.3691f, CrossPlatformMachineEpsilon };         // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.3184f), (BFloat16)0.4805f, CrossPlatformMachineEpsilon };         // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0.0f), (BFloat16)BFloat16.One, (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.0f), (BFloat16)BFloat16.One, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.3184f), (BFloat16)2.078f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.4336f), (BFloat16)2.719f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)4.344f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6914f), (BFloat16)4.906f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)5.094f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7852f), (BFloat16)6.094f, CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };
            yield return new object[] { (BFloat16)(1.125f), (BFloat16)13.31f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)26f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.445f), (BFloat16)27.88f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)37.25f, CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.297f), (BFloat16)198f, CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)524f, CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (e)
            yield return new object[] { (BFloat16)(3.141f), (BFloat16)1384f, CrossPlatformMachineEpsilon * (BFloat16)10000 }; // value:  (pi)
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
            yield return new object[] { (BFloat16)(-3.141f), (BFloat16)(-1f), CrossPlatformMachineEpsilon };               // value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), (BFloat16)(-1f), CrossPlatformMachineEpsilon };               // value: -(e)
            yield return new object[] { (BFloat16)(-2.297f), (BFloat16)(-0.9961f), CrossPlatformMachineEpsilon };               // value: -(ln(10))
            yield return new object[] { (BFloat16)(-1.57f), (BFloat16)(-0.9727f), CrossPlatformMachineEpsilon };               // value: -(pi / 2)
            yield return new object[] { (BFloat16)(-1.445f), (BFloat16)(-0.9648f), CrossPlatformMachineEpsilon };               // value: -(log2(e))
            yield return new object[] { (BFloat16)(-1.414f), (BFloat16)(-0.9609f), CrossPlatformMachineEpsilon };               // value: -(sqrt(2))
            yield return new object[] { (BFloat16)(-1.125f), (BFloat16)(-0.9258f), CrossPlatformMachineEpsilon };               // value: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-BFloat16.One), (BFloat16)(-0.9f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(-0.7852f), (BFloat16)(-0.8359f), CrossPlatformMachineEpsilon };               // value: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.707f), (BFloat16)(-0.8047f), CrossPlatformMachineEpsilon };               // value: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.6914f), (BFloat16)(-0.7969f), CrossPlatformMachineEpsilon };               // value: -(ln(2))
            yield return new object[] { (BFloat16)(-0.6367f), (BFloat16)(-0.7695f), CrossPlatformMachineEpsilon };               // value: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.4336f), (BFloat16)(-0.6328f), CrossPlatformMachineEpsilon };               // value: -(log10(e))
            yield return new object[] { (BFloat16)(-0.3184f), (BFloat16)(-0.5195f), CrossPlatformMachineEpsilon };               // value: -(1 / pi)
            yield return new object[] { (BFloat16)(-0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.3184f), (BFloat16)(1.078f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / pi)
            yield return new object[] { (BFloat16)(0.4336f), (BFloat16)(1.711f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)(3.328f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6914f), (BFloat16)(3.906f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)(4.094f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7852f), (BFloat16)(5.094f), CrossPlatformMachineEpsilon * (BFloat16)10 };    // value:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(9.0f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.125f), (BFloat16)(12.31f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)(25f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.445f), (BFloat16)(26.88f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)(36.25f), CrossPlatformMachineEpsilon * (BFloat16)100 };   // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.297f), (BFloat16)(197f), CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)(524f), CrossPlatformMachineEpsilon * (BFloat16)1000 };  // value:  (e)
            yield return new object[] { (BFloat16)(3.141f), (BFloat16)(1384f), CrossPlatformMachineEpsilon * (BFloat16)10000 }; // value:  (pi)
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
            yield return new object[] { (BFloat16)(-3.141f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(e)
            yield return new object[] { (BFloat16)(-1.414f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(sqrt(2))
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-BFloat16.One), BFloat16.NegativeInfinity, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-0.957f), (BFloat16)(-3.141f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi)
            yield return new object[] { (BFloat16)(-0.9336f), (BFloat16)(-2.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(e)
            yield return new object[] { (BFloat16)(-0.8984f), (BFloat16)(-2.281f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(ln(10))
            yield return new object[] { (BFloat16)(-0.793f), (BFloat16)(-1.578f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi / 2)
            yield return new object[] { (BFloat16)(-0.7656f), (BFloat16)(-1.453f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(log2(e))
            yield return new object[] { (BFloat16)(-0.7578f), (BFloat16)(-1.422f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(sqrt(2))
            yield return new object[] { (BFloat16)(-0.6758f), (BFloat16)(-1.125f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-0.6321f), (BFloat16)(-BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-0.543f), (BFloat16)(-0.7812f), CrossPlatformMachineEpsilon };             // expected: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.5078f), (BFloat16)(-0.707f), CrossPlatformMachineEpsilon };             // expected: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.5f), (BFloat16)(-0.6914f), CrossPlatformMachineEpsilon };             // expected: -(ln(2))
            yield return new object[] { (BFloat16)(-0.4707f), (BFloat16)(-0.6367f), CrossPlatformMachineEpsilon };             // expected: -(2 / pi)
            yield return new object[] { (BFloat16)(-0f), (BFloat16)(0f), 0.0f };
            yield return new object[] { (BFloat16)(0f), (BFloat16)(0f), 0.0f };
            yield return new object[] { (BFloat16)(0.375f), (BFloat16)(0.3184f), CrossPlatformMachineEpsilon };             // expected:  (1 / pi)
            yield return new object[] { (BFloat16)(0.543f), (BFloat16)(0.4336f), CrossPlatformMachineEpsilon };             // expected:  (log10(e))
            yield return new object[] { (BFloat16)(0.8906f), (BFloat16)(0.6367f), CrossPlatformMachineEpsilon };             // expected:  (2 / pi)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(0.6931f), CrossPlatformMachineEpsilon };             // expected:  (ln(2))
            yield return new object[] { (BFloat16)(1.031f), (BFloat16)(0.707f), CrossPlatformMachineEpsilon };             // expected:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(1.195f), (BFloat16)(0.7852f), CrossPlatformMachineEpsilon };             // expected:  (pi / 4)
            yield return new object[] { (BFloat16)(1.719f), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(2.094f), (BFloat16)(1.133f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(3.109f), (BFloat16)(1.414f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (sqrt(2))
            yield return new object[] { (BFloat16)(3.234f), (BFloat16)(1.445f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (log2(e))
            yield return new object[] { (BFloat16)(3.812f), (BFloat16)(1.57f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi / 2)
            yield return new object[] { (BFloat16)(9f), (BFloat16)(2.297f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (ln(10))
            yield return new object[] { (BFloat16)(14.12f), (BFloat16)(2.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (e)
            yield return new object[] { (BFloat16)(22.12f), (BFloat16)(3.141f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi)
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
            yield return new object[] { (BFloat16)(-0.8867f), (BFloat16)(-3.141f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi)
            yield return new object[] { (BFloat16)(-0.8477f), (BFloat16)(-2.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(e)
            yield return new object[] { (BFloat16)(-0.7969f), (BFloat16)(-2.297f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(ln(10))
            yield return new object[] { (BFloat16)(-0.6641f), (BFloat16)(-1.57f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi / 2)
            yield return new object[] { (BFloat16)(-0.6328f), (BFloat16)(-1.445f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(log2(e))
            yield return new object[] { (BFloat16)(-0.625f), (BFloat16)(-1.414f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(sqrt(2))
            yield return new object[] { (BFloat16)(-0.543f), (BFloat16)(-1.133f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-0.5f), (BFloat16)(-BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-0.4199f), (BFloat16)(-0.7852f), CrossPlatformMachineEpsilon };             // expected: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.3867f), (BFloat16)(-0.707f), CrossPlatformMachineEpsilon };             // expected: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.3809f), (BFloat16)(-0.6914f), CrossPlatformMachineEpsilon };             // expected: -(ln(2))
            yield return new object[] { (BFloat16)(-0.3574f), (BFloat16)(-0.6367f), CrossPlatformMachineEpsilon };             // expected: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.2598f), (BFloat16)(-0.4336f), CrossPlatformMachineEpsilon };             // expected: -(log10(e))
            yield return new object[] { (BFloat16)(-0.1982f), (BFloat16)(-0.3184f), CrossPlatformMachineEpsilon };             // expected: -(1 / pi)
            yield return new object[] { (BFloat16)(-0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0.2471f), (BFloat16)(0.3184f), CrossPlatformMachineEpsilon };             // expected:  (1 / pi)
            yield return new object[] { (BFloat16)(0.3516f), (BFloat16)(0.4355f), CrossPlatformMachineEpsilon };             // expected:  (log10(e))
            yield return new object[] { (BFloat16)(0.5547f), (BFloat16)(0.6367f), CrossPlatformMachineEpsilon };             // expected:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6172f), (BFloat16)(0.6953f), CrossPlatformMachineEpsilon };             // expected:  (ln(2))
            yield return new object[] { (BFloat16)(0.6328f), (BFloat16)(0.707f), CrossPlatformMachineEpsilon };             // expected:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7227f), (BFloat16)(0.7852f), CrossPlatformMachineEpsilon };             // expected:  (pi / 4)
            yield return new object[] { (BFloat16)(BFloat16.One), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(1.188f), (BFloat16)(1.133f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.664f), (BFloat16)(1.414f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.719f), (BFloat16)(1.445f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (log2(e))
            yield return new object[] { (BFloat16)(1.969f), (BFloat16)(1.57f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi / 2)
            yield return new object[] { (BFloat16)(3.938f), (BFloat16)(2.297f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (ln(10))
            yield return new object[] { (BFloat16)(5.594f), (BFloat16)(2.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (e)
            yield return new object[] { (BFloat16)(7.812f), (BFloat16)(3.141f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi)
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
            yield return new object[] { (BFloat16)(-3.141f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(pi)
            yield return new object[] { (BFloat16)(-2.719f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(e)
            yield return new object[] { (BFloat16)(-1.414f), BFloat16.NaN, (BFloat16)0.0f };                              //                              value: -(sqrt(2))
            yield return new object[] { BFloat16.NaN, BFloat16.NaN, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-BFloat16.One), BFloat16.NegativeInfinity, (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(-0.9961f), (BFloat16)(-2.406f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(ln(10))
            yield return new object[] { (BFloat16)(-0.9727f), (BFloat16)(-1.562f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(pi / 2)
            yield return new object[] { (BFloat16)(-0.9648f), (BFloat16)(-1.453f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(log2(e))
            yield return new object[] { (BFloat16)(-0.9609f), (BFloat16)(-1.406f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(sqrt(2))
            yield return new object[] { (BFloat16)(-0.9258f), (BFloat16)(-1.133f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected: -(2 / sqrt(pi))
            yield return new object[] { (BFloat16)(-0.8984f), (BFloat16)(-0.9922f), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(-0.8359f), (BFloat16)(-0.7852f), CrossPlatformMachineEpsilon };             // expected: -(pi / 4)
            yield return new object[] { (BFloat16)(-0.8047f), (BFloat16)(-0.7109f), CrossPlatformMachineEpsilon };             // expected: -(1 / sqrt(2))
            yield return new object[] { (BFloat16)(-0.7969f), (BFloat16)(-0.6914f), CrossPlatformMachineEpsilon };             // expected: -(ln(2))
            yield return new object[] { (BFloat16)(-0.7695f), (BFloat16)(-0.6367f), CrossPlatformMachineEpsilon };             // expected: -(2 / pi)
            yield return new object[] { (BFloat16)(-0.6328f), (BFloat16)(-0.4355f), CrossPlatformMachineEpsilon };             // expected: -(log10(e))
            yield return new object[] { (BFloat16)(-0.5195f), (BFloat16)(-0.3184f), CrossPlatformMachineEpsilon };             // expected: -(1 / pi)
            yield return new object[] { (BFloat16)(-0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(0f), (BFloat16)(0f), (BFloat16)0.0f };
            yield return new object[] { (BFloat16)(1.078f), (BFloat16)(0.3184f), CrossPlatformMachineEpsilon };             // expected:  (1 / pi)
            yield return new object[] { (BFloat16)(1.719f), (BFloat16)(0.4336f), CrossPlatformMachineEpsilon };             // expected:  (log10(e))        value: (e)
            yield return new object[] { (BFloat16)(3.328f), (BFloat16)(0.6367f), CrossPlatformMachineEpsilon };             // expected:  (2 / pi)
            yield return new object[] { (BFloat16)(3.938f), (BFloat16)(0.6953f), CrossPlatformMachineEpsilon };             // expected:  (ln(2))
            yield return new object[] { (BFloat16)(4.094f), (BFloat16)(0.707f), CrossPlatformMachineEpsilon };             // expected:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(5.094f), (BFloat16)(0.7852f), CrossPlatformMachineEpsilon };             // expected:  (pi / 4)
            yield return new object[] { (BFloat16)(9.0f), (BFloat16)(BFloat16.One), CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { (BFloat16)(12.44f), (BFloat16)(1.125f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(25f), (BFloat16)(1.414f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (sqrt(2))
            yield return new object[] { (BFloat16)(26.75f), (BFloat16)(1.445f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (log2(e))
            yield return new object[] { (BFloat16)(36.25f), (BFloat16)(1.57f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi / 2)
            yield return new object[] { (BFloat16)(200f), (BFloat16)(2.297f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (ln(10))
            yield return new object[] { (BFloat16)(520f), (BFloat16)(2.719f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (e)
            yield return new object[] { (BFloat16)(1384f), (BFloat16)(3.141f), CrossPlatformMachineEpsilon * (BFloat16)10 };  // expected:  (pi)
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
            yield return new object[] { BFloat16.Zero, (BFloat16)1.57f, (BFloat16)1.57f, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)2.0f, (BFloat16)2.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, BFloat16.E, BFloat16.E, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)3.0f, (BFloat16)3.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.Zero, (BFloat16)10.0f, (BFloat16)10.0f, BFloat16.Zero };
            yield return new object[] { BFloat16.One, BFloat16.One, (BFloat16)1.41421356f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, (BFloat16)0.3184f, (BFloat16)2.734f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (1 / pi)
            yield return new object[] { BFloat16.E, (BFloat16)0.4336f, (BFloat16)2.75f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (log10(e))
            yield return new object[] { BFloat16.E, (BFloat16)0.6367f, (BFloat16)2.797f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (2 / pi)
            yield return new object[] { BFloat16.E, (BFloat16)0.6914f, (BFloat16)2.812f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (ln(2))
            yield return new object[] { BFloat16.E, (BFloat16)0.707f, (BFloat16)2.812f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (1 / sqrt(2))
            yield return new object[] { BFloat16.E, (BFloat16)0.7852f, (BFloat16)2.828f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (pi / 4)
            yield return new object[] { BFloat16.E, BFloat16.One, (BFloat16)2.896f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)
            yield return new object[] { BFloat16.E, (BFloat16)1.125f, (BFloat16)2.938f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (2 / sqrt(pi))
            yield return new object[] { BFloat16.E, (BFloat16)1.414f, (BFloat16)3.062f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (sqrt(2))
            yield return new object[] { BFloat16.E, (BFloat16)1.445f, (BFloat16)3.078f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (log2(e))
            yield return new object[] { BFloat16.E, (BFloat16)1.57f, (BFloat16)3.141f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (pi / 2)
            yield return new object[] { BFloat16.E, (BFloat16)2.297f, (BFloat16)3.562f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (ln(10))
            yield return new object[] { BFloat16.E, BFloat16.E, (BFloat16)3.844f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (e)
            yield return new object[] { BFloat16.E, (BFloat16)3.141f, (BFloat16)4.156f, CrossPlatformMachineEpsilon * (BFloat16)10 };   // x: (e)   y: (pi)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.3184f, (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (1 / pi)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.4336f, (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (log10(e))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.6367f, (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (2 / pi)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.6914f, (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (ln(2))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.707f, (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (1 / sqrt(2))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)0.7852f, (BFloat16)10.0f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (pi / 4)
            yield return new object[] { (BFloat16)10.0f, BFloat16.One, (BFloat16)10.05f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.125f, (BFloat16)10.06f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (2 / sqrt(pi))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.414f, (BFloat16)10.12f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (sqrt(2))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.445f, (BFloat16)10.12f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (log2(e))
            yield return new object[] { (BFloat16)10.0f, (BFloat16)1.57f, (BFloat16)10.12f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (pi / 2)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)2.297f, (BFloat16)10.25f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (ln(10))
            yield return new object[] { (BFloat16)10.0f, BFloat16.E, (BFloat16)10.36f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (e)
            yield return new object[] { (BFloat16)10.0f, (BFloat16)3.141f, (BFloat16)10.5f, CrossPlatformMachineEpsilon * (BFloat16)100 };  //          y: (pi)
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
            yield return new object[] { BFloat16.E, -5, (BFloat16)0.8187f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -4, (BFloat16)0.7788f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -3, (BFloat16)0.7165f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -2, (BFloat16)0.6065f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, -1, (BFloat16)0.3679f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 0, BFloat16.NaN, BFloat16.Zero };
            yield return new object[] { BFloat16.E, 1, BFloat16.E, BFloat16.Zero };
            yield return new object[] { BFloat16.E, 2, (BFloat16)1.6487f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 3, (BFloat16)1.3956f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 4, (BFloat16)1.2840f, CrossPlatformMachineEpsilon * (BFloat16)10 };
            yield return new object[] { BFloat16.E, 5, (BFloat16)1.2214f, CrossPlatformMachineEpsilon * (BFloat16)10 };
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
            yield return new object[] { (BFloat16)0.5391f, (BFloat16)0.3184f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.2051f, (BFloat16)0.4336f, CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.Zero, (BFloat16)0.5f, BFloat16.Zero };
            yield return new object[] { -(BFloat16)0.416f, (BFloat16)0.6367f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.5703f, (BFloat16)0.6914f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.6055f, (BFloat16)0.707f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.7812f, (BFloat16)0.7852f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.0f, BFloat16.One, BFloat16.Zero };
            yield return new object[] { -(BFloat16)0.918f, (BFloat16)0.8711f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.2656f, (BFloat16)0.5859f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.1787f, (BFloat16)0.5586f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.2207f, (BFloat16)0.4297f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.582f, (BFloat16)0.3027f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.6328f, (BFloat16)0.7188f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.9023f, (BFloat16)0.8594f, CrossPlatformMachineEpsilon };
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
            yield return new object[] { (BFloat16)0.8398f, (BFloat16)0.3164f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.9805f, (BFloat16)0.4375f, CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.One, (BFloat16)0.5f, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.9102f, (BFloat16)0.3633f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.8203f, (BFloat16)0.3066f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.7969f, (BFloat16)0.293f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.625f, (BFloat16)0.2148f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.3926f, -(BFloat16)0.1279f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.9648f, -(BFloat16)0.416f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.9844f, -(BFloat16)0.4434f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.9766f, -(BFloat16)0.4316f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.8125f, (BFloat16)0.3027f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.7734f, (BFloat16)0.2812f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.4297f, -(BFloat16)0.1416f, CrossPlatformMachineEpsilon };
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
            yield return new object[] { (BFloat16)0.8398f, (BFloat16)0.5391f, (BFloat16)0.3184f, CrossPlatformMachineEpsilon }; // y: sinpi(1 / pi)         x:  cospi(1 / pi)
            yield return new object[] { (BFloat16)0.9805f, (BFloat16)0.2051f, (BFloat16)0.4336f, CrossPlatformMachineEpsilon }; // y: sinpi(log10(e))       x:  cospi(log10(e))
            yield return new object[] { BFloat16.One, -BFloat16.Zero, (BFloat16)0.5f, BFloat16.Zero };                   // y: sinpi(0.5)            x: -cospi(0.5)
            yield return new object[] { BFloat16.One, BFloat16.Zero, (BFloat16)0.5f, BFloat16.Zero };                   // y: sinpi(0.5)            x:  cospi(0.5)
            yield return new object[] { (BFloat16)0.9102f, -(BFloat16)0.416f, (BFloat16)0.6367f, CrossPlatformMachineEpsilon }; // y: sinpi(2 / pi)         x:  cospi(2 / pi)
            yield return new object[] { (BFloat16)0.8203f, -(BFloat16)0.5703f, (BFloat16)0.6953f, CrossPlatformMachineEpsilon }; // y: sinpi(ln(2))          x:  cospi(ln(2))
            yield return new object[] { (BFloat16)0.7969f, -(BFloat16)0.6055f, (BFloat16)0.707f, CrossPlatformMachineEpsilon }; // y: sinpi(1 / sqrt(2))    x:  cospi(1 / sqrt(2))
            yield return new object[] { (BFloat16)0.625f, -(BFloat16)0.7812f, (BFloat16)0.7852f, CrossPlatformMachineEpsilon }; // y: sinpi(pi / 4)         x:  cospi(pi / 4)
            yield return new object[] { -(BFloat16)0.3926f, -(BFloat16)0.918f, -(BFloat16)0.8711f, CrossPlatformMachineEpsilon }; // y: sinpi(2 / sqrt(pi))   x:  cospi(2 / sqrt(pi))
            yield return new object[] { -(BFloat16)0.9648f, -(BFloat16)0.2656f, -(BFloat16)0.5859f, CrossPlatformMachineEpsilon }; // y: sinpi(sqrt(2))        x:  cospi(sqrt(2))
            yield return new object[] { -(BFloat16)0.9844f, -(BFloat16)0.1787f, -(BFloat16)0.5586f, CrossPlatformMachineEpsilon }; // y: sinpi(log2(e))        x:  cospi(log2(e))
            yield return new object[] { -(BFloat16)0.9766f, (BFloat16)0.2207f, -(BFloat16)0.4297f, CrossPlatformMachineEpsilon }; // y: sinpi(pi / 2)         x:  cospi(pi / 2)
            yield return new object[] { (BFloat16)0.8125f, (BFloat16)0.582f, (BFloat16)0.3027f, CrossPlatformMachineEpsilon }; // y: sinpi(ln(10))         x:  cospi(ln(10))
            yield return new object[] { (BFloat16)0.7734f, -(BFloat16)0.6328f, (BFloat16)0.7188f, CrossPlatformMachineEpsilon }; // y: sinpi(e)              x:  cospi(e)
            yield return new object[] { -(BFloat16)0.4297f, -(BFloat16)0.9023f, -(BFloat16)0.8594f, CrossPlatformMachineEpsilon }; // y: sinpi(pi)             x:  cospi(pi)
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
            yield return new object[] { (BFloat16)1.555f, (BFloat16)0.3184f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)4.781f, (BFloat16)0.4336f, CrossPlatformMachineEpsilon };
            yield return new object[] { BFloat16.PositiveInfinity, (BFloat16)0.5f, BFloat16.Zero };
            yield return new object[] { -(BFloat16)2.188f, -(BFloat16)0.3633f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.438f, -(BFloat16)0.3066f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.312f, -(BFloat16)0.293f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)0.8008f, -(BFloat16)0.2148f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.4258f, (BFloat16)0.1279f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)3.625f, (BFloat16)0.4141f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)5.5f, (BFloat16)0.4434f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)4.406f, -(BFloat16)0.4297f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)1.398f, (BFloat16)0.3027f, CrossPlatformMachineEpsilon };
            yield return new object[] { -(BFloat16)1.219f, -(BFloat16)0.2812f, CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)0.4766f, (BFloat16)0.1416f, CrossPlatformMachineEpsilon };
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
            yield return new object[] { (BFloat16)0.3184f, (BFloat16)0.5391f, CrossPlatformMachineEpsilon };       // value:  (1 / pi)
            yield return new object[] { (BFloat16)0.4336f, (BFloat16)0.207f, CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)0.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.6367f, -(BFloat16)0.416f, CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)0.6914f, -(BFloat16)0.5664f, CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)0.707f, -(BFloat16)0.6055f, CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)0.7852f, -(BFloat16)0.7812f, CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { BFloat16.One, -(BFloat16)1.0f, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.125f, -(BFloat16)0.9258f, CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)1.414f, -(BFloat16)0.2676f, CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)1.445f, -(BFloat16)0.1709f, CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)1.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.57f, (BFloat16)0.2188f, CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)2.0f, (BFloat16)1.0, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.297f, (BFloat16)0.5938f, CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)2.5f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.719f, -(BFloat16)0.6328f, CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)3.0f, -(BFloat16)1.0, BFloat16.Zero };
            yield return new object[] { (BFloat16)3.141f, -(BFloat16)0.9023f, CrossPlatformMachineEpsilon };       // value:  (pi)
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
            yield return new object[] { (BFloat16)0.3184f, (BFloat16)0.8398f, CrossPlatformMachineEpsilon };       // value:  (1 / pi)
            yield return new object[] { (BFloat16)0.4336f, (BFloat16)0.9766f, CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)0.5f, BFloat16.One, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.6367f, (BFloat16)0.9102f, CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)0.6914f, (BFloat16)0.8242f, CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)0.707f, (BFloat16)0.7969f, CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)0.7852f, (BFloat16)0.625f, CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { BFloat16.One, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.125f, -(BFloat16)0.3828f, CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)1.414f, -(BFloat16)0.9648f, CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)1.445f, -(BFloat16)0.9844f, CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)1.5f, -(BFloat16)1f, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.57f, -(BFloat16)0.9766f, CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)2.0f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.297f, (BFloat16)0.8047f, CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)2.5f, BFloat16.One, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.719f, (BFloat16)0.7734f, CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)3.0f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)3.141f, -(BFloat16)0.4277f, CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)3.5f, -(BFloat16)1f, BFloat16.Zero };
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
            yield return new object[] { (BFloat16)0.3184f, (BFloat16)1.555f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (1 / pi)
            yield return new object[] { (BFloat16)0.4336f, (BFloat16)4.719f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (log10(e))
            yield return new object[] { (BFloat16)0.5f, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { (BFloat16)0.6367f, -(BFloat16)2.188f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (2 / pi)
            yield return new object[] { (BFloat16)0.6914f, -(BFloat16)1.461f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(2))
            yield return new object[] { (BFloat16)0.707f, -(BFloat16)1.312f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)0.7852f, -(BFloat16)0.8008f, CrossPlatformMachineEpsilon };             // value:  (pi / 4)
            yield return new object[] { BFloat16.One, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.125f, (BFloat16)0.4141f, CrossPlatformMachineEpsilon };             // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)1.414f, (BFloat16)3.609f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (sqrt(2))
            yield return new object[] { (BFloat16)1.445f, (BFloat16)5.75f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (log2(e))
            yield return new object[] { (BFloat16)1.5f, BFloat16.NegativeInfinity, BFloat16.Zero };
            yield return new object[] { (BFloat16)1.57f, -(BFloat16)4.469f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (pi / 2)
            yield return new object[] { (BFloat16)2.0f, BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.297f, (BFloat16)1.352f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (ln(10))
            yield return new object[] { (BFloat16)2.5f, BFloat16.PositiveInfinity, BFloat16.Zero };
            yield return new object[] { (BFloat16)2.719f, -(BFloat16)1.219f, CrossPlatformMachineEpsilon * (BFloat16)10 };  // value:  (e)
            yield return new object[] { (BFloat16)3.0f, -BFloat16.Zero, BFloat16.Zero };
            yield return new object[] { (BFloat16)3.141f, (BFloat16)0.4727f, CrossPlatformMachineEpsilon };             // value:  (pi)
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xC049), BitConverter.UInt16BitsToBFloat16(0xC04A) };    // value: -(pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xC02E), BitConverter.UInt16BitsToBFloat16(0xC02F) };    // value: -(e)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xC013), BitConverter.UInt16BitsToBFloat16(0xC014) };    // value: -(ln(10))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBFC9), BitConverter.UInt16BitsToBFloat16(0xBFCA) };    // value: -(pi / 2)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBFB9), BitConverter.UInt16BitsToBFloat16(0xBFBA) };    // value: -(log2(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBFB5), BitConverter.UInt16BitsToBFloat16(0xBFB6) };    // value: -(sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF90), BitConverter.UInt16BitsToBFloat16(0xBF91) };    // value: -(2 / sqrt(pi))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF80), BitConverter.UInt16BitsToBFloat16(0xBF81) };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF49), BitConverter.UInt16BitsToBFloat16(0xBF4A) };    // value: -(pi / 4)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF35), BitConverter.UInt16BitsToBFloat16(0xBF36) };    // value: -(1 / sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF31), BitConverter.UInt16BitsToBFloat16(0xBF32) };    // value: -(ln(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF23), BitConverter.UInt16BitsToBFloat16(0xBF24) };    // value: -(2 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBEDE), BitConverter.UInt16BitsToBFloat16(0xBEDF) };    // value: -(log10(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBEA3), BitConverter.UInt16BitsToBFloat16(0xBEA4) };    // value: -(1 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), -BFloat16.Epsilon };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), -BFloat16.Epsilon };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3EA3), BitConverter.UInt16BitsToBFloat16(0x3EA2) };    // value:  (1 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3EDE), BitConverter.UInt16BitsToBFloat16(0x3EDD) };    // value:  (log10(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F23), BitConverter.UInt16BitsToBFloat16(0x3F22) };    // value:  (2 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F31), BitConverter.UInt16BitsToBFloat16(0x3F30) };    // value:  (ln(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F35), BitConverter.UInt16BitsToBFloat16(0x3F34) };    // value:  (1 / sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F49), BitConverter.UInt16BitsToBFloat16(0x3F48) };    // value:  (pi / 4)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F80), BitConverter.UInt16BitsToBFloat16(0x3F7F) };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F90), BitConverter.UInt16BitsToBFloat16(0x3F8F) };    // value:  (2 / sqrt(pi))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3FB5), BitConverter.UInt16BitsToBFloat16(0x3FB4) };    // value:  (sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3FB9), BitConverter.UInt16BitsToBFloat16(0x3FB8) };    // value:  (log2(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3FC9), BitConverter.UInt16BitsToBFloat16(0x3FC8) };    // value:  (pi / 2)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x4013), BitConverter.UInt16BitsToBFloat16(0x4012) };    // value:  (ln(10))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x402E), BitConverter.UInt16BitsToBFloat16(0x402D) };    // value:  (e)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x4049), BitConverter.UInt16BitsToBFloat16(0x4048) };    // value:  (pi)
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
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xC049), BitConverter.UInt16BitsToBFloat16(0xC048) };    // value: -(pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xC02E), BitConverter.UInt16BitsToBFloat16(0xC02D) };    // value: -(e)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xC013), BitConverter.UInt16BitsToBFloat16(0xC012) };    // value: -(ln(10))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBFC9), BitConverter.UInt16BitsToBFloat16(0xBFC8) };    // value: -(pi / 2)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBFB9), BitConverter.UInt16BitsToBFloat16(0xBFB8) };    // value: -(log2(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBFB5), BitConverter.UInt16BitsToBFloat16(0xBFB4) };    // value: -(sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF90), BitConverter.UInt16BitsToBFloat16(0xBF8F) };    // value: -(2 / sqrt(pi))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF80), BitConverter.UInt16BitsToBFloat16(0xBF7F) };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF49), BitConverter.UInt16BitsToBFloat16(0xBF48) };    // value: -(pi / 4)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF35), BitConverter.UInt16BitsToBFloat16(0xBF34) };    // value: -(1 / sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF31), BitConverter.UInt16BitsToBFloat16(0xBF30) };    // value: -(ln(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBF23), BitConverter.UInt16BitsToBFloat16(0xBF22) };    // value: -(2 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBEDE), BitConverter.UInt16BitsToBFloat16(0xBEDD) };    // value: -(log10(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0xBEA3), BitConverter.UInt16BitsToBFloat16(0xBEA2) };    // value: -(1 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x8000), BFloat16.Epsilon };
            yield return new object[] { BFloat16.NaN, BFloat16.NaN };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x0000), BFloat16.Epsilon };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3EA3), BitConverter.UInt16BitsToBFloat16(0x3EA4) };    // value:  (1 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3EDE), BitConverter.UInt16BitsToBFloat16(0x3EDF) };    // value:  (log10(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F23), BitConverter.UInt16BitsToBFloat16(0x3F24) };    // value:  (2 / pi)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F31), BitConverter.UInt16BitsToBFloat16(0x3F32) };    // value:  (ln(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F35), BitConverter.UInt16BitsToBFloat16(0x3F36) };    // value:  (1 / sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F49), BitConverter.UInt16BitsToBFloat16(0x3F4A) };    // value:  (pi / 4)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F80), BitConverter.UInt16BitsToBFloat16(0x3F81) };
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3F90), BitConverter.UInt16BitsToBFloat16(0x3F91) };    // value:  (2 / sqrt(pi))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3FB5), BitConverter.UInt16BitsToBFloat16(0x3FB6) };    // value:  (sqrt(2))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3FB9), BitConverter.UInt16BitsToBFloat16(0x3FBA) };    // value:  (log2(e))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x3FC9), BitConverter.UInt16BitsToBFloat16(0x3FCA) };    // value:  (pi / 2)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x4013), BitConverter.UInt16BitsToBFloat16(0x4014) };    // value:  (ln(10))
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x402E), BitConverter.UInt16BitsToBFloat16(0x402F) };    // value:  (e)
            yield return new object[] { BitConverter.UInt16BitsToBFloat16(0x4049), BitConverter.UInt16BitsToBFloat16(0x404A) };    // value:  (pi)
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
            yield return new object[] { (BFloat16)(0.4336f), (BFloat16)(0.007568f), CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.5f), (BFloat16)(0.008728f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.6367f), (BFloat16)(0.01111f), CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.6953f), (BFloat16)(0.01215f), CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.707f), (BFloat16)(0.01233f), CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.7852f), (BFloat16)(0.01373f), CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { (BFloat16)(1.0f), (BFloat16)(0.01746f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(1.125f), (BFloat16)(0.01965f), CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(1.414f), (BFloat16)(0.02466f), CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(1.445f), (BFloat16)(0.02527f), CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)(1.5f), (BFloat16)(0.02612f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(1.57f), (BFloat16)(0.02747f), CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)(2.0f), (BFloat16)(0.03491f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(2.297f), (BFloat16)(0.04004f), CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)(2.5f), (BFloat16)(0.0437f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(2.719f), (BFloat16)(0.04736f), CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)(3.0f), (BFloat16)(0.05225f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(3.141f), (BFloat16)(0.05493f), CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)(3.5f), (BFloat16)(0.06104f), CrossPlatformMachineEpsilon };
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
            yield return new object[] { (BFloat16)(0.007568f), (BFloat16)(0.4336f), CrossPlatformMachineEpsilon };       // value:  (log10(e))
            yield return new object[] { (BFloat16)(0.008728f), (BFloat16)(0.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.01111f), (BFloat16)(0.6367f), CrossPlatformMachineEpsilon };       // value:  (2 / pi)
            yield return new object[] { (BFloat16)(0.01208f), (BFloat16)(0.6914f), CrossPlatformMachineEpsilon };       // value:  (ln(2))
            yield return new object[] { (BFloat16)(0.01233f), (BFloat16)(0.707f), CrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
            yield return new object[] { (BFloat16)(0.01367f), (BFloat16)(0.7852f), CrossPlatformMachineEpsilon };       // value:  (pi / 4)
            yield return new object[] { (BFloat16)(0.01746f), (BFloat16)(1f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.01965f), (BFloat16)(1.125f), CrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
            yield return new object[] { (BFloat16)(0.02466f), (BFloat16)(1.414f), CrossPlatformMachineEpsilon };       // value:  (sqrt(2))
            yield return new object[] { (BFloat16)(0.02515f), (BFloat16)(1.438f), CrossPlatformMachineEpsilon };       // value:  (log2(e))
            yield return new object[] { (BFloat16)(0.02612f), (BFloat16)(1.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.02734f), (BFloat16)(1.57f), CrossPlatformMachineEpsilon };       // value:  (pi / 2)
            yield return new object[] { (BFloat16)(0.03491f), (BFloat16)(2f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.04004f), (BFloat16)(2.297f), CrossPlatformMachineEpsilon };       // value:  (ln(10))
            yield return new object[] { (BFloat16)(0.0437f), (BFloat16)(2.5f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.04736f), (BFloat16)(2.719f), CrossPlatformMachineEpsilon };       // value:  (e)
            yield return new object[] { (BFloat16)(0.05225f), (BFloat16)(3f), CrossPlatformMachineEpsilon };
            yield return new object[] { (BFloat16)(0.05469f), (BFloat16)(3.141f), CrossPlatformMachineEpsilon };       // value:  (pi)
            yield return new object[] { (BFloat16)(0.06104f), (BFloat16)(3.5f), CrossPlatformMachineEpsilon };
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
            return BitConverter.BFloat16ToUInt16Bits(value) == 0x8000;
        }

        static bool IsPositiveZero(BFloat16 value)
        {
            return BitConverter.BFloat16ToUInt16Bits(value) == 0;
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
            if (BitConverter.BFloat16ToUInt16Bits(expected) == BitConverter.BFloat16ToUInt16Bits(actual))
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
