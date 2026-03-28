// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class LeadingZeroCountTests
    {
        // LeadingZeroCount is defined to count leading zeros in the most significant 32-bit word of
        // the value's magnitude (same as when BigInteger's internal _bits array used uint[] limbs).
        // Negative values have infinite sign-extension of 1-bits, so their LZC is always 0.

        // Values stored in _sign (magnitude <= int.MaxValue):
        //   LZC is computed on the 32-bit representation of _sign.

        // Values stored in _bits (magnitude > int.MaxValue):
        //   LZC is the leading zero count of the most significant 32-bit word of the value.
        //   On 64-bit systems a limb holds 64 bits; we extract the most significant 32-bit half.

        [Theory]
        [InlineData(0, 32)]              // Zero: 32 leading zeros
        [InlineData(1, 31)]              // 0x00000001
        [InlineData(2, 30)]              // 0x00000002
        [InlineData(0x7FFFFFFF, 1)]      // int.MaxValue: 0x7FFFFFFF, 1 leading zero
        [InlineData(-1, 0)]              // Negative: always 0
        [InlineData(-2, 0)]              // Negative
        [InlineData(int.MinValue + 1, 0)] // Most negative value stored in _sign (-int.MaxValue)
        public static void SmallValues(int value, int expected)
        {
            Assert.Equal((BigInteger)expected, BigInteger.LeadingZeroCount(new BigInteger(value)));
        }

        [Theory]
        // Boundary at 2^31 (int.MaxValue + 1 = 0x80000000): first value that goes into _bits.
        // Leading "0" prefix keeps the high bit clear, making BigInteger interpret as positive.
        [InlineData("080000000", 0)]              // 2^31: MSW = 0x80000000
        [InlineData("0FFFFFFFF", 0)]              // uint.MaxValue: MSW = 0xFFFFFFFF
        [InlineData("0100000000", 31)]            // 2^32: MSW = 0x00000001
        [InlineData("0100000001", 31)]            // 2^32 + 1: MSW = 0x00000001
        [InlineData("07FFFFFFF00000000", 1)]      // MSW of most-significant 32-bit word = 0x7FFFFFFF
        [InlineData("07FFFFFFFFFFFFFFF", 1)]      // long.MaxValue: MSW = 0x7FFFFFFF
        [InlineData("08000000000000000", 0)]      // 2^63 (= long.MaxValue + 1): MSW = 0x80000000
        [InlineData("0FFFFFFFFFFFFFFFF", 0)]      // ulong.MaxValue: MSW = 0xFFFFFFFF
        [InlineData("010000000000000000", 31)]    // 2^64: MSW = 0x00000001
        [InlineData("080000000000000000000000000000000", 0)]  // 2^127: MSW = 0x80000000
        [InlineData("0100000000000000000000000000000000", 31)] // 2^128: MSW = 0x00000001
        public static void LargePositiveValues(string hexValue, int expected)
        {
            BigInteger value = BigInteger.Parse(hexValue, Globalization.NumberStyles.HexNumber);
            Assert.Equal((BigInteger)expected, BigInteger.LeadingZeroCount(value));
        }

        [Theory]
        // Negative values always have LZC = 0 (infinite sign-extension of 1-bits).
        // Construct large negative values as negations of known positive magnitudes.
        [InlineData("080000000")]        // -(2^31): magnitude stored in _bits
        [InlineData("0FFFFFFFF")]        // -(uint.MaxValue): magnitude stored in _bits
        [InlineData("0100000000")]       // -(2^32): magnitude stored in _bits
        [InlineData("07FFFFFFFFFFFFFFF")] // -(long.MaxValue): magnitude stored in _bits
        [InlineData("08000000000000000")] // -(2^63): magnitude stored in _bits
        public static void LargeNegativeValues(string hexMagnitude)
        {
            // Parse the magnitude as a positive hex value (leading zero keeps high bit clear),
            // then negate it so the result is negative and stored in _bits.
            BigInteger magnitude = BigInteger.Parse(hexMagnitude, Globalization.NumberStyles.HexNumber);
            BigInteger value = -magnitude;
            Assert.True(value < 0);
            Assert.Equal((BigInteger)0, BigInteger.LeadingZeroCount(value));
        }

        [Fact]
        public static void IntMinValue()
        {
            // int.MinValue (-2^31) is stored in _bits (excluded from the _sign-only range),
            // and it's negative so LZC = 0.
            Assert.Equal((BigInteger)0, BigInteger.LeadingZeroCount(new BigInteger(int.MinValue)));
        }

        [Fact]
        public static void LzcIsAlwaysNonNegative()
        {
            // Regardless of how big the value is, LeadingZeroCount is always >= 0.
            BigInteger hugePositive = BigInteger.Pow(2, 1000);
            BigInteger result = BigInteger.LeadingZeroCount(hugePositive);
            Assert.True(result >= 0);
        }

        [Fact]
        public static void PlatformIndependence()
        {
            // Results must be the same on 32-bit and 64-bit platforms.
            // For Zero: always 32, not nint.Size * 8.
            Assert.Equal((BigInteger)32, BigInteger.LeadingZeroCount(BigInteger.Zero));

            // For One: always 31, not nint.Size * 8 - 1.
            Assert.Equal((BigInteger)31, BigInteger.LeadingZeroCount(BigInteger.One));

            // For values crossing the 32-bit/64-bit limb boundary, results must be consistent.
            // 2^32: MSW in 32-bit view = 0x00000001, so LZC = 31.
            Assert.Equal((BigInteger)31, BigInteger.LeadingZeroCount(BigInteger.Pow(2, 32)));

            // 2^63: MSW in 32-bit view = 0x80000000, so LZC = 0.
            Assert.Equal((BigInteger)0, BigInteger.LeadingZeroCount(BigInteger.Pow(2, 63)));

            // 2^64: MSW in 32-bit view = 0x00000001, so LZC = 31.
            Assert.Equal((BigInteger)31, BigInteger.LeadingZeroCount(BigInteger.Pow(2, 64)));
        }
    }
}
