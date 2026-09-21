// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Tests
{
    public sealed class TotalOrderIeee754ComparerTests
    {
        public static IEnumerable<object[]> SingleTestData
        {
            get
            {
                yield return new object[] { 0.0f, 0.0f, 0 };
                yield return new object[] { -0.0f, -0.0f, 0 };
                yield return new object[] { 0.0f, -0.0f, 1 };
                yield return new object[] { -0.0f, 0.0f, -1 };
                yield return new object[] { 0.0f, 1.0f, -1 };
                yield return new object[] { float.PositiveInfinity, 1.0f, 1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0xFFC00000), float.NegativeInfinity, -1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0xFFC00000), -1.0f, -1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0x7FC00000), 1.0f, 1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0x7FC00000), float.PositiveInfinity, 1 };
                yield return new object[] { float.NaN, float.NaN, 0 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0xFFC00000), BitConverter.UInt32BitsToSingle(0x7FC00000), -1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0x7FC00000), BitConverter.UInt32BitsToSingle(0x7FC00001), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(SingleTestData))]
        public void TotalOrderTestSingle(float x, float y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<float>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        [Theory]
        [MemberData(nameof(SingleTestData))]
        public void TotalOrderTestSingleWrapperMatchesFastPath(float x, float y, int result)
        {
            // SingleWrapper forces TotalOrderIeee754Comparer<T> through the fully generic CompareGeneric
            // path -- which float itself never reaches, since Compare fast-paths built-in types -- so
            // this confirms the generic path reproduces the exact totalOrder result for real binary32
            // values, not just the synthetic byte arrays used by the stub-based tests above.
            var comparer = new TotalOrderIeee754Comparer<SingleWrapper>();
            Assert.Equal(result, Math.Sign(comparer.Compare(new SingleWrapper(x), new SingleWrapper(y))));
        }

        public static IEnumerable<object[]> DoubleTestData
        {
            get
            {
                yield return new object[] { 0.0, 0.0, 0 };
                yield return new object[] { -0.0, -0.0, 0 };
                yield return new object[] { 0.0, -0.0, 1 };
                yield return new object[] { -0.0, 0.0, -1 };
                yield return new object[] { 0.0, 1.0, -1 };
                yield return new object[] { double.PositiveInfinity, 1.0, 1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), double.NegativeInfinity, -1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), -1.0, -1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), 1.0, 1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), double.PositiveInfinity, 1 };
                yield return new object[] { double.NaN, double.NaN, 0 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), -1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), BitConverter.UInt64BitsToDouble(0x7FF80000_00000001), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(DoubleTestData))]
        public void TotalOrderTestDouble(double x, double y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<double>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        [Theory]
        [MemberData(nameof(DoubleTestData))]
        public void TotalOrderTestDoubleWrapperMatchesFastPath(double x, double y, int result)
        {
            // See TotalOrderTestSingleWrapperMatchesFastPath; DoubleWrapper does the same for binary64.
            var comparer = new TotalOrderIeee754Comparer<DoubleWrapper>();
            Assert.Equal(result, Math.Sign(comparer.Compare(new DoubleWrapper(x), new DoubleWrapper(y))));
        }

        public static IEnumerable<object[]> HalfTestData
        {
            get
            {
                yield return new object[] { (Half)0.0, (Half)0.0, 0 };
                yield return new object[] { (Half)(-0.0), (Half)(-0.0), 0 };
                yield return new object[] { (Half)0.0, (Half)(-0.0), 1 };
                yield return new object[] { (Half)(-0.0), (Half)0.0, -1 };
                yield return new object[] { (Half)0.0, (Half)1.0, -1 };
                yield return new object[] { Half.PositiveInfinity, (Half)1.0, 1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0xFE00), Half.NegativeInfinity, -1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0xFE00), (Half)(-1.0), -1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0x7E00), (Half)1.0, 1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0x7E00), Half.PositiveInfinity, 1 };
                yield return new object[] { Half.NaN, Half.NaN, 0 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0xFE00), BitConverter.UInt16BitsToHalf(0x7E00), -1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0x7E00), BitConverter.UInt16BitsToHalf(0x7E01), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(HalfTestData))]
        public void TotalOrderTestHalf(Half x, Half y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<Half>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        public static IEnumerable<object[]> BFloat16TestData
        {
            get
            {
                yield return new object[] { (BFloat16)0.0f, (BFloat16)0.0f, 0 };
                yield return new object[] { (BFloat16)(-0.0f), (BFloat16)(-0.0f), 0 };
                yield return new object[] { (BFloat16)0.0f, (BFloat16)(-0.0f), 1 };
                yield return new object[] { (BFloat16)(-0.0f), (BFloat16)0.0f, -1 };
                yield return new object[] { (BFloat16)0.0f, (BFloat16)1.0f, -1 };
                yield return new object[] { BFloat16.PositiveInfinity, (BFloat16)1.0f, 1 };
                yield return new object[] { BFloat16.NaN, BFloat16.NegativeInfinity, -1 };
                yield return new object[] { BFloat16.NaN, (BFloat16)(-1.0f), -1 };
                yield return new object[] { -BFloat16.NaN, (BFloat16)1.0f, 1 };
                yield return new object[] { -BFloat16.NaN, BFloat16.PositiveInfinity, 1 };
                yield return new object[] { BFloat16.NaN, BFloat16.NaN, 0 };
                yield return new object[] { BFloat16.NaN, -BFloat16.NaN, -1 };
                yield return new object[] { Unsafe.BitCast<ushort, BFloat16>(0x7FC0), Unsafe.BitCast<ushort, BFloat16>(0x7FC1), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(BFloat16TestData))]
        public void TotalOrderTestBFloat16(BFloat16 x, BFloat16 y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<BFloat16>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        public static IEnumerable<object[]> NFloatTestData
        {
            get
            {
                yield return new object[] { (NFloat)(0.0f), (NFloat)(0.0f), 0 };
                yield return new object[] { (NFloat)(-0.0f), (NFloat)(-0.0f), 0 };
                yield return new object[] { (NFloat)(0.0f), (NFloat)(-0.0f), 1 };
                yield return new object[] { (NFloat)(-0.0f), (NFloat)(0.0f), -1 };
                yield return new object[] { (NFloat)(0.0f), (NFloat)(1.0f), -1 };
                yield return new object[] { NFloat.PositiveInfinity, (NFloat)(1.0f), 1 };
                yield return new object[] { NFloat.NaN, NFloat.NaN, 0 };
                if (Environment.Is64BitProcess)
                {
                    yield return new object[] { (NFloat)BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), (NFloat)(1.0d), 1 };
                    yield return new object[] { (NFloat)BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), NFloat.PositiveInfinity, 1 };
                    yield return new object[] { (NFloat)BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), NFloat.NegativeInfinity, -1 };
                    yield return new object[] { (NFloat)BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), (NFloat)(-1.0d), -1 };
                    yield return new object[] { (NFloat)BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), (NFloat)(BitConverter.UInt64BitsToDouble(0x7FF80000_00000000)), -1 };
                    yield return new object[] { (NFloat)BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), (NFloat)(BitConverter.UInt64BitsToDouble(0x7FF80000_00000001)), -1 };
                }
                else
                {
                    yield return new object[] { (NFloat)BitConverter.UInt32BitsToSingle(0x7FC00000), (NFloat)(1.0f), 1 };
                    yield return new object[] { (NFloat)BitConverter.UInt32BitsToSingle(0x7FC00000), NFloat.PositiveInfinity, 1 };
                    yield return new object[] { (NFloat)BitConverter.UInt32BitsToSingle(0xFFC00000), NFloat.NegativeInfinity, -1 };
                    yield return new object[] { (NFloat)BitConverter.UInt32BitsToSingle(0xFFC00000), (NFloat)(-1.0f), -1 };
                    yield return new object[] { (NFloat)BitConverter.UInt32BitsToSingle(0xFFC00000), (NFloat)(BitConverter.UInt32BitsToSingle(0x7FC00000)), -1 };
                    yield return new object[] { (NFloat)BitConverter.UInt32BitsToSingle(0x7FC00000), (NFloat)(BitConverter.UInt32BitsToSingle(0x7FC00001)), -1 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(NFloatTestData))]
        public void TotalOrderTestNFloat(NFloat x, NFloat y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<NFloat>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        public static IEnumerable<object[]> Decimal32TestData
        {
            get
            {
                yield return new object[] { Decimal32.Parse("0", CultureInfo.InvariantCulture), Decimal32.Parse("0", CultureInfo.InvariantCulture), 0 };
                yield return new object[] { Decimal32.NegativeZero, Decimal32.NegativeZero, 0 };
                yield return new object[] { Decimal32.Parse("0", CultureInfo.InvariantCulture), Decimal32.NegativeZero, 1 };
                yield return new object[] { Decimal32.NegativeZero, Decimal32.Parse("0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal32.Parse("0", CultureInfo.InvariantCulture), Decimal32.Parse("1", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal32.PositiveInfinity, Decimal32.Parse("1", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal32.NaN, Decimal32.NegativeInfinity, -1 };
                yield return new object[] { Decimal32.NaN, Decimal32.Parse("-1", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { -Decimal32.NaN, Decimal32.Parse("1", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { -Decimal32.NaN, Decimal32.PositiveInfinity, 1 };
                yield return new object[] { Decimal32.NaN, Decimal32.NaN, 0 };
                yield return new object[] { Decimal32.NaN, -Decimal32.NaN, -1 };
                // Same-signed NaNs are ordered by payload (implementation defined, not part of IEEE 754 totalOrder)
                yield return new object[] { Unsafe.BitCast<uint, Decimal32>(0x7C00_0000), Unsafe.BitCast<uint, Decimal32>(0x7C00_0001), -1 };
                yield return new object[] { Unsafe.BitCast<uint, Decimal32>(0xFC00_0000), Unsafe.BitCast<uint, Decimal32>(0xFC00_0001), 1 };
                // Signaling NaNs order before quiet NaNs of the same sign (+sNaN < +qNaN)
                yield return new object[] { Unsafe.BitCast<uint, Decimal32>(0x7E00_0000), Unsafe.BitCast<uint, Decimal32>(0x7C00_0000), -1 };
                yield return new object[] { Unsafe.BitCast<uint, Decimal32>(0xFE00_0000), Unsafe.BitCast<uint, Decimal32>(0xFC00_0000), 1 };
                // Cohort members: equal value, differing quantum exponent (totalOrder orders by exponent)
                yield return new object[] { Decimal32.Parse("1.00", CultureInfo.InvariantCulture), Decimal32.Parse("1.0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal32.Parse("1.0", CultureInfo.InvariantCulture), Decimal32.Parse("1.00", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal32.Parse("1e1", CultureInfo.InvariantCulture), Decimal32.Parse("10e0", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal32.Parse("-1.00", CultureInfo.InvariantCulture), Decimal32.Parse("-1.0", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal32.Parse("-1.0", CultureInfo.InvariantCulture), Decimal32.Parse("-1.00", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal32.Parse("0.00", CultureInfo.InvariantCulture), Decimal32.Parse("0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal32.Parse("-0.00", CultureInfo.InvariantCulture), Decimal32.NegativeZero, 1 };
            }
        }

        [Theory]
        [MemberData(nameof(Decimal32TestData))]
        public void TotalOrderTestDecimal32(Decimal32 x, Decimal32 y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<Decimal32>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        public static IEnumerable<object[]> Decimal64TestData
        {
            get
            {
                yield return new object[] { Decimal64.Parse("0", CultureInfo.InvariantCulture), Decimal64.Parse("0", CultureInfo.InvariantCulture), 0 };
                yield return new object[] { Decimal64.NegativeZero, Decimal64.NegativeZero, 0 };
                yield return new object[] { Decimal64.Parse("0", CultureInfo.InvariantCulture), Decimal64.NegativeZero, 1 };
                yield return new object[] { Decimal64.NegativeZero, Decimal64.Parse("0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal64.Parse("0", CultureInfo.InvariantCulture), Decimal64.Parse("1", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal64.PositiveInfinity, Decimal64.Parse("1", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal64.NaN, Decimal64.NegativeInfinity, -1 };
                yield return new object[] { Decimal64.NaN, Decimal64.Parse("-1", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { -Decimal64.NaN, Decimal64.Parse("1", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { -Decimal64.NaN, Decimal64.PositiveInfinity, 1 };
                yield return new object[] { Decimal64.NaN, Decimal64.NaN, 0 };
                yield return new object[] { Decimal64.NaN, -Decimal64.NaN, -1 };
                // Same-signed NaNs are ordered by payload (implementation defined, not part of IEEE 754 totalOrder)
                yield return new object[] { Unsafe.BitCast<ulong, Decimal64>(0x7C00_0000_0000_0000), Unsafe.BitCast<ulong, Decimal64>(0x7C00_0000_0000_0001), -1 };
                yield return new object[] { Unsafe.BitCast<ulong, Decimal64>(0xFC00_0000_0000_0000), Unsafe.BitCast<ulong, Decimal64>(0xFC00_0000_0000_0001), 1 };
                // Signaling NaNs order before quiet NaNs of the same sign (+sNaN < +qNaN)
                yield return new object[] { Unsafe.BitCast<ulong, Decimal64>(0x7E00_0000_0000_0000), Unsafe.BitCast<ulong, Decimal64>(0x7C00_0000_0000_0000), -1 };
                yield return new object[] { Unsafe.BitCast<ulong, Decimal64>(0xFE00_0000_0000_0000), Unsafe.BitCast<ulong, Decimal64>(0xFC00_0000_0000_0000), 1 };
                // Cohort members: equal value, differing quantum exponent (totalOrder orders by exponent)
                yield return new object[] { Decimal64.Parse("1.00", CultureInfo.InvariantCulture), Decimal64.Parse("1.0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal64.Parse("1.0", CultureInfo.InvariantCulture), Decimal64.Parse("1.00", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal64.Parse("1e1", CultureInfo.InvariantCulture), Decimal64.Parse("10e0", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal64.Parse("-1.00", CultureInfo.InvariantCulture), Decimal64.Parse("-1.0", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal64.Parse("-1.0", CultureInfo.InvariantCulture), Decimal64.Parse("-1.00", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal64.Parse("0.00", CultureInfo.InvariantCulture), Decimal64.Parse("0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal64.Parse("-0.00", CultureInfo.InvariantCulture), Decimal64.NegativeZero, 1 };
            }
        }

        [Theory]
        [MemberData(nameof(Decimal64TestData))]
        public void TotalOrderTestDecimal64(Decimal64 x, Decimal64 y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<Decimal64>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        public static IEnumerable<object[]> Decimal128TestData
        {
            get
            {
                yield return new object[] { Decimal128.Parse("0", CultureInfo.InvariantCulture), Decimal128.Parse("0", CultureInfo.InvariantCulture), 0 };
                yield return new object[] { Decimal128.NegativeZero, Decimal128.NegativeZero, 0 };
                yield return new object[] { Decimal128.Parse("0", CultureInfo.InvariantCulture), Decimal128.NegativeZero, 1 };
                yield return new object[] { Decimal128.NegativeZero, Decimal128.Parse("0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal128.Parse("0", CultureInfo.InvariantCulture), Decimal128.Parse("1", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal128.PositiveInfinity, Decimal128.Parse("1", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal128.NaN, Decimal128.NegativeInfinity, -1 };
                yield return new object[] { Decimal128.NaN, Decimal128.Parse("-1", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { -Decimal128.NaN, Decimal128.Parse("1", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { -Decimal128.NaN, Decimal128.PositiveInfinity, 1 };
                yield return new object[] { Decimal128.NaN, Decimal128.NaN, 0 };
                yield return new object[] { Decimal128.NaN, -Decimal128.NaN, -1 };
                // Same-signed NaNs are ordered by payload (implementation defined, not part of IEEE 754 totalOrder).
                // The payload here lives entirely in the low bits, which the coefficient decode would discard.
                yield return new object[] { Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x7C00_0000_0000_0000, 0x0000_0000_0000_0000)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x7C00_0000_0000_0000, 0x0000_0000_0000_0001)), -1 };
                yield return new object[] { Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0xFC00_0000_0000_0000, 0x0000_0000_0000_0000)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0xFC00_0000_0000_0000, 0x0000_0000_0000_0001)), 1 };
                // Signaling NaNs order before quiet NaNs of the same sign (+sNaN < +qNaN)
                yield return new object[] { Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x7E00_0000_0000_0000, 0x0000_0000_0000_0000)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x7C00_0000_0000_0000, 0x0000_0000_0000_0000)), -1 };
                yield return new object[] { Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0xFE00_0000_0000_0000, 0x0000_0000_0000_0000)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0xFC00_0000_0000_0000, 0x0000_0000_0000_0000)), 1 };
                // Cohort members: equal value, differing quantum exponent (totalOrder orders by exponent)
                yield return new object[] { Decimal128.Parse("1.00", CultureInfo.InvariantCulture), Decimal128.Parse("1.0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal128.Parse("1.0", CultureInfo.InvariantCulture), Decimal128.Parse("1.00", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal128.Parse("1e1", CultureInfo.InvariantCulture), Decimal128.Parse("10e0", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal128.Parse("-1.00", CultureInfo.InvariantCulture), Decimal128.Parse("-1.0", CultureInfo.InvariantCulture), 1 };
                yield return new object[] { Decimal128.Parse("-1.0", CultureInfo.InvariantCulture), Decimal128.Parse("-1.00", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal128.Parse("0.00", CultureInfo.InvariantCulture), Decimal128.Parse("0", CultureInfo.InvariantCulture), -1 };
                yield return new object[] { Decimal128.Parse("-0.00", CultureInfo.InvariantCulture), Decimal128.NegativeZero, 1 };
            }
        }

        [Theory]
        [MemberData(nameof(Decimal128TestData))]
        public void TotalOrderTestDecimal128(Decimal128 x, Decimal128 y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<Decimal128>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void TotalOrderTestInvalidSignificand(int significandByteCount)
        {
            var comparer = new TotalOrderIeee754Comparer<StubFloatingPointIeee754>();
            StubFloatingPointIeee754 xy = new StubFloatingPointIeee754(1, significandByteCount);
            Assert.Throws<OverflowException>(() => comparer.Compare(xy, xy));
        }

        [Theory]
        [InlineData(2, 5)]
        [InlineData(5, 2)]
        [InlineData(3, 3)]
        public void TotalOrderTestSignificandDifferingByteCountSameBitLength(int xSignificandByteCount, int ySignificandByteCount)
        {
            // The stub never writes real significand data, so both operands are conceptually all-zero
            // (equal) magnitudes regardless of their differing byte counts, as long as their bit lengths
            // (which the comparer trusts as the true magnitude order) match.
            var comparer = new TotalOrderIeee754Comparer<StubFloatingPointIeee754>();
            StubFloatingPointIeee754 x = new StubFloatingPointIeee754(1, xSignificandByteCount);
            StubFloatingPointIeee754 y = new StubFloatingPointIeee754(1, ySignificandByteCount);
            Assert.Equal(0, comparer.Compare(x, y));
        }

        public static IEnumerable<object[]> CohortExponentTestData
        {
            get
            {
                // Same-length baseline
                yield return new object[] { false, new byte[] { 0x05 }, false, new byte[] { 0x03 }, 1 };
                yield return new object[] { false, new byte[] { 0x03 }, false, new byte[] { 0x05 }, -1 };

                // Shorter positive vs. longer positive (+5 vs. +260)
                yield return new object[] { false, new byte[] { 0x05 }, false, new byte[] { 0x01, 0x04 }, -1 };
                yield return new object[] { false, new byte[] { 0x01, 0x04 }, false, new byte[] { 0x05 }, 1 };

                // Shorter negative vs. longer negative (-5 vs. -260)
                yield return new object[] { false, new byte[] { 0xFB }, false, new byte[] { 0xFE, 0xFC }, 1 };
                yield return new object[] { false, new byte[] { 0xFE, 0xFC }, false, new byte[] { 0xFB }, -1 };

                // Positive vs. negative with differing lengths (+260 vs. -5)
                yield return new object[] { false, new byte[] { 0x01, 0x04 }, false, new byte[] { 0xFB }, 1 };
                yield return new object[] { false, new byte[] { 0xFB }, false, new byte[] { 0x01, 0x04 }, -1 };

                // A negative value's cohort ordering is reversed by the outer negation, so the exponent
                // comparison above is inverted end-to-end (+5 vs. +260, both operands negative values)
                yield return new object[] { true, new byte[] { 0x05 }, true, new byte[] { 0x01, 0x04 }, 1 };
                yield return new object[] { true, new byte[] { 0x01, 0x04 }, true, new byte[] { 0x05 }, -1 };

                // Same length and same shortest bit length, differing magnitude (+5 vs. +6): the
                // bit-length fast path cannot decide, so the byte-wise fallback must still disambiguate
                yield return new object[] { false, new byte[] { 0x05 }, false, new byte[] { 0x06 }, -1 };
                yield return new object[] { false, new byte[] { 0x06 }, false, new byte[] { 0x05 }, 1 };

                // Same length and same shortest bit length, differing magnitude (-5 vs. -6)
                yield return new object[] { false, new byte[] { 0xFB }, false, new byte[] { 0xFA }, 1 };
                yield return new object[] { false, new byte[] { 0xFA }, false, new byte[] { 0xFB }, -1 };

                // Differing length but equal value and equal shortest bit length (+5 vs. sign-extended
                // +5): the bit-length fast path also cannot decide here, exercising the differing-length
                // fallback in CompareMagnitudeBigEndian directly
                yield return new object[] { false, new byte[] { 0x05 }, false, new byte[] { 0x00, 0x05 }, 0 };
                yield return new object[] { false, new byte[] { 0x00, 0x05 }, false, new byte[] { 0x05 }, 0 };

                // Differing length but equal value and equal shortest bit length (-5 vs. sign-extended -5)
                yield return new object[] { false, new byte[] { 0xFB }, false, new byte[] { 0xFF, 0xFB }, 0 };
                yield return new object[] { false, new byte[] { 0xFF, 0xFB }, false, new byte[] { 0xFB }, 0 };

                // Both exponents are exactly zero: a shortest bit length of 0 uniquely identifies a zero
                // exponent, so this is settled without reading either operand's raw bytes
                yield return new object[] { false, new byte[] { 0x00 }, false, new byte[] { 0x00 }, 0 };

                // Zero vs. a non-zero positive/negative exponent: settled by reading only the non-zero
                // operand's sign, without ever reading the zero operand's raw bytes
                yield return new object[] { false, new byte[] { 0x00 }, false, new byte[] { 0x05 }, -1 };
                yield return new object[] { false, new byte[] { 0x05 }, false, new byte[] { 0x00 }, 1 };
                yield return new object[] { false, new byte[] { 0x00 }, false, new byte[] { 0xFB }, 1 };
                yield return new object[] { false, new byte[] { 0xFB }, false, new byte[] { 0x00 }, -1 };
            }
        }

        [Theory]
        [MemberData(nameof(CohortExponentTestData))]
        public void TotalOrderTestCohortExponentDifferingLength(bool xIsNegative, byte[] xExponent, bool yIsNegative, byte[] yExponent, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<StubFloatingPointIeee754Cohort>();
            StubFloatingPointIeee754Cohort x = new StubFloatingPointIeee754Cohort(xIsNegative, xExponent);
            StubFloatingPointIeee754Cohort y = new StubFloatingPointIeee754Cohort(yIsNegative, yExponent);
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        private readonly struct StubFloatingPointIeee754 : IFloatingPointIeee754<StubFloatingPointIeee754>
        {
            private readonly int _significandBitLength;
            private readonly int _significandByteCount;

            public StubFloatingPointIeee754(int significandBitLength, int significandByteCount)
            {
                _significandBitLength = significandBitLength;
                _significandByteCount = significandByteCount;
            }

            public static StubFloatingPointIeee754 Epsilon => default;
            public static StubFloatingPointIeee754 NaN => default;
            public static StubFloatingPointIeee754 NegativeInfinity => default;
            public static StubFloatingPointIeee754 NegativeZero => default;
            public static StubFloatingPointIeee754 PositiveInfinity => default;
            public static StubFloatingPointIeee754 NegativeOne => default;
            public static StubFloatingPointIeee754 E => default;
            public static StubFloatingPointIeee754 Pi => default;
            public static StubFloatingPointIeee754 Tau => default;
            public static StubFloatingPointIeee754 One => default;
            public static int Radix => default;
            public static StubFloatingPointIeee754 Zero => default;
            public static StubFloatingPointIeee754 AdditiveIdentity => default;
            public static StubFloatingPointIeee754 MultiplicativeIdentity => default;
            public static StubFloatingPointIeee754 Abs(StubFloatingPointIeee754 value) => default;
            public static StubFloatingPointIeee754 Acos(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Acosh(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 AcosPi(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Asin(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Asinh(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 AsinPi(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Atan(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Atan2(StubFloatingPointIeee754 y, StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Atan2Pi(StubFloatingPointIeee754 y, StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Atanh(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 AtanPi(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 BitDecrement(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 BitIncrement(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Cbrt(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Cos(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Cosh(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 CosPi(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Exp(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Exp10(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Exp2(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 FusedMultiplyAdd(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right, StubFloatingPointIeee754 addend) => default;
            public static StubFloatingPointIeee754 Hypot(StubFloatingPointIeee754 x, StubFloatingPointIeee754 y) => default;
            public static StubFloatingPointIeee754 Ieee754Remainder(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => default;
            public static int ILogB(StubFloatingPointIeee754 x) => default;
            public static bool IsCanonical(StubFloatingPointIeee754 value) => true;
            public static bool IsComplexNumber(StubFloatingPointIeee754 value) => false;
            public static bool IsEvenInteger(StubFloatingPointIeee754 value) => false;
            public static bool IsFinite(StubFloatingPointIeee754 value) => false;
            public static bool IsImaginaryNumber(StubFloatingPointIeee754 value) => false;
            public static bool IsInfinity(StubFloatingPointIeee754 value) => false;
            public static bool IsInteger(StubFloatingPointIeee754 value) => false;
            public static bool IsNaN(StubFloatingPointIeee754 value) => true;
            public static bool IsNegative(StubFloatingPointIeee754 value) => false;
            public static bool IsNegativeInfinity(StubFloatingPointIeee754 value) => false;
            public static bool IsNormal(StubFloatingPointIeee754 value) => false;
            public static bool IsOddInteger(StubFloatingPointIeee754 value) => false;
            public static bool IsPositive(StubFloatingPointIeee754 value) => false;
            public static bool IsPositiveInfinity(StubFloatingPointIeee754 value) => false;
            public static bool IsRealNumber(StubFloatingPointIeee754 value) => false;
            public static bool IsSubnormal(StubFloatingPointIeee754 value) => false;
            public static bool IsZero(StubFloatingPointIeee754 value) => false;
            public static StubFloatingPointIeee754 Log(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Log(StubFloatingPointIeee754 x, StubFloatingPointIeee754 newBase) => default;
            public static StubFloatingPointIeee754 Log10(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Log2(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 MaxMagnitude(StubFloatingPointIeee754 x, StubFloatingPointIeee754 y) => default;
            public static StubFloatingPointIeee754 MaxMagnitudeNumber(StubFloatingPointIeee754 x, StubFloatingPointIeee754 y) => default;
            public static StubFloatingPointIeee754 MinMagnitude(StubFloatingPointIeee754 x, StubFloatingPointIeee754 y) => default;
            public static StubFloatingPointIeee754 MinMagnitudeNumber(StubFloatingPointIeee754 x, StubFloatingPointIeee754 y) => default;
            public static StubFloatingPointIeee754 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754 Parse(string s, NumberStyles style, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754 Parse(string s, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754 Pow(StubFloatingPointIeee754 x, StubFloatingPointIeee754 y) => default;
            public static StubFloatingPointIeee754 RootN(StubFloatingPointIeee754 x, int n) => default;
            public static StubFloatingPointIeee754 Round(StubFloatingPointIeee754 x, int digits, MidpointRounding mode) => default;
            public static StubFloatingPointIeee754 ScaleB(StubFloatingPointIeee754 x, int n) => default;
            public static StubFloatingPointIeee754 Sin(StubFloatingPointIeee754 x) => default;
            public static (StubFloatingPointIeee754 Sin, StubFloatingPointIeee754 Cos) SinCos(StubFloatingPointIeee754 x) => default;
            public static (StubFloatingPointIeee754 SinPi, StubFloatingPointIeee754 CosPi) SinCosPi(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Sinh(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 SinPi(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Sqrt(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Tan(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 Tanh(StubFloatingPointIeee754 x) => default;
            public static StubFloatingPointIeee754 TanPi(StubFloatingPointIeee754 x) => default;

            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            public static bool TryParse(string? s, IFormatProvider? provider, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754>.TryConvertFromChecked<TOther>(TOther value, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754>.TryConvertFromSaturating<TOther>(TOther value, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754>.TryConvertFromTruncating<TOther>(TOther value, out StubFloatingPointIeee754 result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754>.TryConvertToChecked<TOther>(StubFloatingPointIeee754 value, out TOther result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754>.TryConvertToSaturating<TOther>(StubFloatingPointIeee754 value, out TOther result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754>.TryConvertToTruncating<TOther>(StubFloatingPointIeee754 value, out TOther result)
            {
                result = default;
                return false;
            }

            public int CompareTo(object? obj) => 1;

            public int CompareTo(StubFloatingPointIeee754 other) => 1;

            public bool Equals(StubFloatingPointIeee754 other) => false;

            public int GetExponentByteCount() => 0;

            public int GetExponentShortestBitLength() => 0;

            public int GetSignificandBitLength() => _significandBitLength;

            public int GetSignificandByteCount() => _significandByteCount;

            public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            {
                charsWritten = 0;
                return false;
            }

            public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length >= _significandByteCount)
                {
                    // CoreLib builds with SkipLocalsInit, so the caller's stackalloc'd buffer is not
                    // guaranteed to start zeroed; clear it explicitly so tests comparing two equal,
                    // all-zero significands with differing byte counts are deterministic.
                    destination.Slice(0, _significandByteCount).Clear();
                    bytesWritten = _significandByteCount;
                    return true;
                }

                bytesWritten = 0;
                return false;
            }

            public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length >= _significandByteCount)
                {
                    destination.Slice(0, _significandByteCount).Clear();
                    bytesWritten = _significandByteCount;
                    return true;
                }

                bytesWritten = 0;
                return false;
            }

            public override bool Equals(object o) => false;
            public override int GetHashCode() => 0;

            public static StubFloatingPointIeee754 operator +(StubFloatingPointIeee754 value) => default;
            public static StubFloatingPointIeee754 operator +(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => default;
            public static StubFloatingPointIeee754 operator -(StubFloatingPointIeee754 value) => default;
            public static StubFloatingPointIeee754 operator -(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => default;
            public static StubFloatingPointIeee754 operator ++(StubFloatingPointIeee754 value) => default;
            public static StubFloatingPointIeee754 operator --(StubFloatingPointIeee754 value) => default;
            public static StubFloatingPointIeee754 operator *(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => default;
            public static StubFloatingPointIeee754 operator /(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => default;
            public static StubFloatingPointIeee754 operator %(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => default;
            public static bool operator ==(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => false;
            public static bool operator !=(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => false;
            public static bool operator <(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => false;
            public static bool operator >(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => false;
            public static bool operator <=(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => false;
            public static bool operator >=(StubFloatingPointIeee754 left, StubFloatingPointIeee754 right) => false;
        }

        // Models a custom IFloatingPointIeee754<T> whose quantum exponent has a minimal, value-dependent
        // two's complement byte count that can differ between two operands representing the same equal
        // (cohort) value. Always compares as equal (via `==`) with a variable value sign, so `Compare`
        // always reaches the exponent-cohort branch and calls `CompareExponent` with the given raw bytes.
        private readonly struct StubFloatingPointIeee754Cohort : IFloatingPointIeee754<StubFloatingPointIeee754Cohort>
        {
            private readonly bool _isNegative;
            private readonly byte[] _exponent;

            public StubFloatingPointIeee754Cohort(bool isNegative, byte[] exponent)
            {
                _isNegative = isNegative;
                _exponent = exponent;
            }

            public static StubFloatingPointIeee754Cohort Epsilon => default;
            public static StubFloatingPointIeee754Cohort NaN => default;
            public static StubFloatingPointIeee754Cohort NegativeInfinity => default;
            public static StubFloatingPointIeee754Cohort NegativeZero => default;
            public static StubFloatingPointIeee754Cohort PositiveInfinity => default;
            public static StubFloatingPointIeee754Cohort NegativeOne => default;
            public static StubFloatingPointIeee754Cohort E => default;
            public static StubFloatingPointIeee754Cohort Pi => default;
            public static StubFloatingPointIeee754Cohort Tau => default;
            public static StubFloatingPointIeee754Cohort One => default;
            public static int Radix => default;
            public static StubFloatingPointIeee754Cohort Zero => default;
            public static StubFloatingPointIeee754Cohort AdditiveIdentity => default;
            public static StubFloatingPointIeee754Cohort MultiplicativeIdentity => default;
            public static StubFloatingPointIeee754Cohort Abs(StubFloatingPointIeee754Cohort value) => default;
            public static StubFloatingPointIeee754Cohort Acos(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Acosh(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort AcosPi(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Asin(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Asinh(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort AsinPi(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Atan(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Atan2(StubFloatingPointIeee754Cohort y, StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Atan2Pi(StubFloatingPointIeee754Cohort y, StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Atanh(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort AtanPi(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort BitDecrement(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort BitIncrement(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Cbrt(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Cos(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Cosh(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort CosPi(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Exp(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Exp10(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Exp2(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort FusedMultiplyAdd(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right, StubFloatingPointIeee754Cohort addend) => default;
            public static StubFloatingPointIeee754Cohort Hypot(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort y) => default;
            public static StubFloatingPointIeee754Cohort Ieee754Remainder(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => default;
            public static int ILogB(StubFloatingPointIeee754Cohort x) => default;
            public static bool IsCanonical(StubFloatingPointIeee754Cohort value) => true;
            public static bool IsComplexNumber(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsEvenInteger(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsFinite(StubFloatingPointIeee754Cohort value) => true;
            public static bool IsImaginaryNumber(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsInfinity(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsInteger(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsNaN(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsNegative(StubFloatingPointIeee754Cohort value) => value._isNegative;
            public static bool IsNegativeInfinity(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsNormal(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsOddInteger(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsPositive(StubFloatingPointIeee754Cohort value) => !value._isNegative;
            public static bool IsPositiveInfinity(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsRealNumber(StubFloatingPointIeee754Cohort value) => true;
            public static bool IsSubnormal(StubFloatingPointIeee754Cohort value) => false;
            public static bool IsZero(StubFloatingPointIeee754Cohort value) => false;
            public static StubFloatingPointIeee754Cohort Log(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Log(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort newBase) => default;
            public static StubFloatingPointIeee754Cohort Log10(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Log2(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort MaxMagnitude(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort y) => default;
            public static StubFloatingPointIeee754Cohort MaxMagnitudeNumber(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort y) => default;
            public static StubFloatingPointIeee754Cohort MinMagnitude(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort y) => default;
            public static StubFloatingPointIeee754Cohort MinMagnitudeNumber(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort y) => default;
            public static StubFloatingPointIeee754Cohort Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754Cohort Parse(string s, NumberStyles style, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754Cohort Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754Cohort Parse(string s, IFormatProvider? provider) => default;
            public static StubFloatingPointIeee754Cohort Pow(StubFloatingPointIeee754Cohort x, StubFloatingPointIeee754Cohort y) => default;
            public static StubFloatingPointIeee754Cohort RootN(StubFloatingPointIeee754Cohort x, int n) => default;
            public static StubFloatingPointIeee754Cohort Round(StubFloatingPointIeee754Cohort x, int digits, MidpointRounding mode) => default;
            public static StubFloatingPointIeee754Cohort ScaleB(StubFloatingPointIeee754Cohort x, int n) => default;
            public static StubFloatingPointIeee754Cohort Sin(StubFloatingPointIeee754Cohort x) => default;
            public static (StubFloatingPointIeee754Cohort Sin, StubFloatingPointIeee754Cohort Cos) SinCos(StubFloatingPointIeee754Cohort x) => default;
            public static (StubFloatingPointIeee754Cohort SinPi, StubFloatingPointIeee754Cohort CosPi) SinCosPi(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Sinh(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort SinPi(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Sqrt(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Tan(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort Tanh(StubFloatingPointIeee754Cohort x) => default;
            public static StubFloatingPointIeee754Cohort TanPi(StubFloatingPointIeee754Cohort x) => default;

            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            public static bool TryParse(string? s, IFormatProvider? provider, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754Cohort>.TryConvertFromChecked<TOther>(TOther value, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754Cohort>.TryConvertFromSaturating<TOther>(TOther value, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754Cohort>.TryConvertFromTruncating<TOther>(TOther value, out StubFloatingPointIeee754Cohort result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754Cohort>.TryConvertToChecked<TOther>(StubFloatingPointIeee754Cohort value, out TOther result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754Cohort>.TryConvertToSaturating<TOther>(StubFloatingPointIeee754Cohort value, out TOther result)
            {
                result = default;
                return false;
            }

            static bool INumberBase<StubFloatingPointIeee754Cohort>.TryConvertToTruncating<TOther>(StubFloatingPointIeee754Cohort value, out TOther result)
            {
                result = default;
                return false;
            }

            public int CompareTo(object? obj) => 0;

            public int CompareTo(StubFloatingPointIeee754Cohort other) => 0;

            public bool Equals(StubFloatingPointIeee754Cohort other) => true;

            public int GetExponentByteCount() => _exponent.Length;

            public int GetExponentShortestBitLength()
            {
                // Compute the true shortest two's complement bit length of the raw exponent bytes via
                // BigInteger, so tests exercise the comparer's real bit-length fast path instead of a
                // hardcoded value.
                BigInteger value = new BigInteger(_exponent, isUnsigned: false, isBigEndian: true);
                return ((IBinaryInteger<BigInteger>)value).GetShortestBitLength();
            }

            public int GetSignificandBitLength() => 0;

            public int GetSignificandByteCount() => 0;

            public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            {
                charsWritten = 0;
                return false;
            }

            public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length >= _exponent.Length)
                {
                    _exponent.CopyTo(destination);
                    bytesWritten = _exponent.Length;
                    return true;
                }

                bytesWritten = 0;
                return false;
            }

            public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = 0;
                return true;
            }

            public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            public override bool Equals(object o) => false;
            public override int GetHashCode() => 0;

            public static StubFloatingPointIeee754Cohort operator +(StubFloatingPointIeee754Cohort value) => default;
            public static StubFloatingPointIeee754Cohort operator +(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => default;
            public static StubFloatingPointIeee754Cohort operator -(StubFloatingPointIeee754Cohort value) => default;
            public static StubFloatingPointIeee754Cohort operator -(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => default;
            public static StubFloatingPointIeee754Cohort operator ++(StubFloatingPointIeee754Cohort value) => default;
            public static StubFloatingPointIeee754Cohort operator --(StubFloatingPointIeee754Cohort value) => default;
            public static StubFloatingPointIeee754Cohort operator *(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => default;
            public static StubFloatingPointIeee754Cohort operator /(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => default;
            public static StubFloatingPointIeee754Cohort operator %(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => default;
            public static bool operator ==(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => true;
            public static bool operator !=(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => false;
            public static bool operator <(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => false;
            public static bool operator >(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => false;
            public static bool operator <=(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => true;
            public static bool operator >=(StubFloatingPointIeee754Cohort left, StubFloatingPointIeee754Cohort right) => true;
        }

        // Wraps a real System.Single/System.Double so tests can drive TotalOrderIeee754Comparer<T>'s
        // fully generic CompareGeneric path -- which float/double themselves never reach, since Compare
        // fast-paths them via CompareIntegerSemantic -- while asserting its result against the exact
        // totalOrder semantics of a real IEEE 754 binary32/64 value. Members that CompareGeneric,
        // CompareExponent, and CompareSignificand do not use are intentionally unsupported.
        private readonly struct SingleWrapper : IFloatingPointIeee754<SingleWrapper>
        {
            private readonly float _value;

            public SingleWrapper(float value) => _value = value;

            public static SingleWrapper Epsilon => new(float.Epsilon);
            public static SingleWrapper NaN => new(float.NaN);
            public static SingleWrapper NegativeInfinity => new(float.NegativeInfinity);
            public static SingleWrapper NegativeZero => new(float.NegativeZero);
            public static SingleWrapper PositiveInfinity => new(float.PositiveInfinity);
            public static SingleWrapper NegativeOne => new(-1.0f);
            public static SingleWrapper E => new(float.E);
            public static SingleWrapper Pi => new(float.Pi);
            public static SingleWrapper Tau => new(float.Tau);
            public static SingleWrapper One => new(1.0f);
            public static int Radix => 2;
            public static SingleWrapper Zero => new(0.0f);
            public static SingleWrapper AdditiveIdentity => new(0.0f);
            public static SingleWrapper MultiplicativeIdentity => new(1.0f);
            public static SingleWrapper Abs(SingleWrapper value) => throw new NotImplementedException();
            public static SingleWrapper Acos(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Acosh(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper AcosPi(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Asin(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Asinh(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper AsinPi(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Atan(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Atan2(SingleWrapper y, SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Atan2Pi(SingleWrapper y, SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Atanh(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper AtanPi(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper BitDecrement(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper BitIncrement(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Cbrt(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Cos(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Cosh(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper CosPi(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Exp(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Exp10(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Exp2(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper FusedMultiplyAdd(SingleWrapper left, SingleWrapper right, SingleWrapper addend) => throw new NotImplementedException();
            public static SingleWrapper Hypot(SingleWrapper x, SingleWrapper y) => throw new NotImplementedException();
            public static SingleWrapper Ieee754Remainder(SingleWrapper left, SingleWrapper right) => throw new NotImplementedException();
            public static int ILogB(SingleWrapper x) => throw new NotImplementedException();
            public static bool IsCanonical(SingleWrapper value) => true;
            public static bool IsComplexNumber(SingleWrapper value) => false;
            public static bool IsEvenInteger(SingleWrapper value) => float.IsEvenInteger(value._value);
            public static bool IsFinite(SingleWrapper value) => float.IsFinite(value._value);
            public static bool IsImaginaryNumber(SingleWrapper value) => false;
            public static bool IsInfinity(SingleWrapper value) => float.IsInfinity(value._value);
            public static bool IsInteger(SingleWrapper value) => float.IsInteger(value._value);
            public static bool IsNaN(SingleWrapper value) => float.IsNaN(value._value);
            public static bool IsNegative(SingleWrapper value) => float.IsNegative(value._value);
            public static bool IsNegativeInfinity(SingleWrapper value) => float.IsNegativeInfinity(value._value);
            public static bool IsNormal(SingleWrapper value) => float.IsNormal(value._value);
            public static bool IsOddInteger(SingleWrapper value) => float.IsOddInteger(value._value);
            public static bool IsPositive(SingleWrapper value) => float.IsPositive(value._value);
            public static bool IsPositiveInfinity(SingleWrapper value) => float.IsPositiveInfinity(value._value);
            public static bool IsRealNumber(SingleWrapper value) => float.IsRealNumber(value._value);
            public static bool IsSubnormal(SingleWrapper value) => float.IsSubnormal(value._value);
            public static bool IsZero(SingleWrapper value) => value._value == 0.0f;
            public static SingleWrapper Log(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Log(SingleWrapper x, SingleWrapper newBase) => throw new NotImplementedException();
            public static SingleWrapper Log10(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Log2(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper MaxMagnitude(SingleWrapper x, SingleWrapper y) => throw new NotImplementedException();
            public static SingleWrapper MaxMagnitudeNumber(SingleWrapper x, SingleWrapper y) => throw new NotImplementedException();
            public static SingleWrapper MinMagnitude(SingleWrapper x, SingleWrapper y) => throw new NotImplementedException();
            public static SingleWrapper MinMagnitudeNumber(SingleWrapper x, SingleWrapper y) => throw new NotImplementedException();
            public static SingleWrapper Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static SingleWrapper Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static SingleWrapper Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
            public static SingleWrapper Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
            public static SingleWrapper Pow(SingleWrapper x, SingleWrapper y) => throw new NotImplementedException();
            public static SingleWrapper RootN(SingleWrapper x, int n) => throw new NotImplementedException();
            public static SingleWrapper Round(SingleWrapper x, int digits, MidpointRounding mode) => throw new NotImplementedException();
            public static SingleWrapper ScaleB(SingleWrapper x, int n) => throw new NotImplementedException();
            public static SingleWrapper Sin(SingleWrapper x) => throw new NotImplementedException();
            public static (SingleWrapper Sin, SingleWrapper Cos) SinCos(SingleWrapper x) => throw new NotImplementedException();
            public static (SingleWrapper SinPi, SingleWrapper CosPi) SinCosPi(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Sinh(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper SinPi(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Sqrt(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Tan(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper Tanh(SingleWrapper x) => throw new NotImplementedException();
            public static SingleWrapper TanPi(SingleWrapper x) => throw new NotImplementedException();

            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out SingleWrapper result) => throw new NotImplementedException();
            public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out SingleWrapper result) => throw new NotImplementedException();
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out SingleWrapper result) => throw new NotImplementedException();
            public static bool TryParse(string? s, IFormatProvider? provider, out SingleWrapper result) => throw new NotImplementedException();

            static bool INumberBase<SingleWrapper>.TryConvertFromChecked<TOther>(TOther value, out SingleWrapper result) => throw new NotImplementedException();
            static bool INumberBase<SingleWrapper>.TryConvertFromSaturating<TOther>(TOther value, out SingleWrapper result) => throw new NotImplementedException();
            static bool INumberBase<SingleWrapper>.TryConvertFromTruncating<TOther>(TOther value, out SingleWrapper result) => throw new NotImplementedException();
            static bool INumberBase<SingleWrapper>.TryConvertToChecked<TOther>(SingleWrapper value, out TOther result) => throw new NotImplementedException();
            static bool INumberBase<SingleWrapper>.TryConvertToSaturating<TOther>(SingleWrapper value, out TOther result) => throw new NotImplementedException();
            static bool INumberBase<SingleWrapper>.TryConvertToTruncating<TOther>(SingleWrapper value, out TOther result) => throw new NotImplementedException();

            public int CompareTo(object? obj) => obj is SingleWrapper other ? CompareTo(other) : throw new ArgumentException();
            public int CompareTo(SingleWrapper other) => _value.CompareTo(other._value);
            public bool Equals(SingleWrapper other) => _value.Equals(other._value);

            public int GetExponentByteCount() => ((IFloatingPoint<float>)_value).GetExponentByteCount();
            public int GetExponentShortestBitLength() => ((IFloatingPoint<float>)_value).GetExponentShortestBitLength();
            public int GetSignificandBitLength() => ((IFloatingPoint<float>)_value).GetSignificandBitLength();
            public int GetSignificandByteCount() => ((IFloatingPoint<float>)_value).GetSignificandByteCount();

            public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
                _value.TryFormat(destination, out charsWritten, format, provider);

            public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<float>)_value).TryWriteExponentBigEndian(destination, out bytesWritten);

            public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<float>)_value).TryWriteExponentLittleEndian(destination, out bytesWritten);

            public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<float>)_value).TryWriteSignificandBigEndian(destination, out bytesWritten);

            public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<float>)_value).TryWriteSignificandLittleEndian(destination, out bytesWritten);

            public override bool Equals(object o) => o is SingleWrapper other && Equals(other);
            public override int GetHashCode() => _value.GetHashCode();

            public static SingleWrapper operator +(SingleWrapper value) => new(+value._value);
            public static SingleWrapper operator +(SingleWrapper left, SingleWrapper right) => new(left._value + right._value);
            public static SingleWrapper operator -(SingleWrapper value) => new(-value._value);
            public static SingleWrapper operator -(SingleWrapper left, SingleWrapper right) => new(left._value - right._value);
            public static SingleWrapper operator ++(SingleWrapper value) => new(value._value + 1.0f);
            public static SingleWrapper operator --(SingleWrapper value) => new(value._value - 1.0f);
            public static SingleWrapper operator *(SingleWrapper left, SingleWrapper right) => new(left._value * right._value);
            public static SingleWrapper operator /(SingleWrapper left, SingleWrapper right) => new(left._value / right._value);
            public static SingleWrapper operator %(SingleWrapper left, SingleWrapper right) => new(left._value % right._value);
            public static bool operator ==(SingleWrapper left, SingleWrapper right) => left._value == right._value;
            public static bool operator !=(SingleWrapper left, SingleWrapper right) => left._value != right._value;
            public static bool operator <(SingleWrapper left, SingleWrapper right) => left._value < right._value;
            public static bool operator >(SingleWrapper left, SingleWrapper right) => left._value > right._value;
            public static bool operator <=(SingleWrapper left, SingleWrapper right) => left._value <= right._value;
            public static bool operator >=(SingleWrapper left, SingleWrapper right) => left._value >= right._value;
        }

        // See SingleWrapper; wraps a real System.Double instead.
        private readonly struct DoubleWrapper : IFloatingPointIeee754<DoubleWrapper>
        {
            private readonly double _value;

            public DoubleWrapper(double value) => _value = value;

            public static DoubleWrapper Epsilon => new(double.Epsilon);
            public static DoubleWrapper NaN => new(double.NaN);
            public static DoubleWrapper NegativeInfinity => new(double.NegativeInfinity);
            public static DoubleWrapper NegativeZero => new(double.NegativeZero);
            public static DoubleWrapper PositiveInfinity => new(double.PositiveInfinity);
            public static DoubleWrapper NegativeOne => new(-1.0);
            public static DoubleWrapper E => new(double.E);
            public static DoubleWrapper Pi => new(double.Pi);
            public static DoubleWrapper Tau => new(double.Tau);
            public static DoubleWrapper One => new(1.0);
            public static int Radix => 2;
            public static DoubleWrapper Zero => new(0.0);
            public static DoubleWrapper AdditiveIdentity => new(0.0);
            public static DoubleWrapper MultiplicativeIdentity => new(1.0);
            public static DoubleWrapper Abs(DoubleWrapper value) => throw new NotImplementedException();
            public static DoubleWrapper Acos(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Acosh(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper AcosPi(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Asin(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Asinh(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper AsinPi(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Atan(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Atan2(DoubleWrapper y, DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Atan2Pi(DoubleWrapper y, DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Atanh(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper AtanPi(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper BitDecrement(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper BitIncrement(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Cbrt(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Cos(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Cosh(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper CosPi(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Exp(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Exp10(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Exp2(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper FusedMultiplyAdd(DoubleWrapper left, DoubleWrapper right, DoubleWrapper addend) => throw new NotImplementedException();
            public static DoubleWrapper Hypot(DoubleWrapper x, DoubleWrapper y) => throw new NotImplementedException();
            public static DoubleWrapper Ieee754Remainder(DoubleWrapper left, DoubleWrapper right) => throw new NotImplementedException();
            public static int ILogB(DoubleWrapper x) => throw new NotImplementedException();
            public static bool IsCanonical(DoubleWrapper value) => true;
            public static bool IsComplexNumber(DoubleWrapper value) => false;
            public static bool IsEvenInteger(DoubleWrapper value) => double.IsEvenInteger(value._value);
            public static bool IsFinite(DoubleWrapper value) => double.IsFinite(value._value);
            public static bool IsImaginaryNumber(DoubleWrapper value) => false;
            public static bool IsInfinity(DoubleWrapper value) => double.IsInfinity(value._value);
            public static bool IsInteger(DoubleWrapper value) => double.IsInteger(value._value);
            public static bool IsNaN(DoubleWrapper value) => double.IsNaN(value._value);
            public static bool IsNegative(DoubleWrapper value) => double.IsNegative(value._value);
            public static bool IsNegativeInfinity(DoubleWrapper value) => double.IsNegativeInfinity(value._value);
            public static bool IsNormal(DoubleWrapper value) => double.IsNormal(value._value);
            public static bool IsOddInteger(DoubleWrapper value) => double.IsOddInteger(value._value);
            public static bool IsPositive(DoubleWrapper value) => double.IsPositive(value._value);
            public static bool IsPositiveInfinity(DoubleWrapper value) => double.IsPositiveInfinity(value._value);
            public static bool IsRealNumber(DoubleWrapper value) => double.IsRealNumber(value._value);
            public static bool IsSubnormal(DoubleWrapper value) => double.IsSubnormal(value._value);
            public static bool IsZero(DoubleWrapper value) => value._value == 0.0;
            public static DoubleWrapper Log(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Log(DoubleWrapper x, DoubleWrapper newBase) => throw new NotImplementedException();
            public static DoubleWrapper Log10(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Log2(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper MaxMagnitude(DoubleWrapper x, DoubleWrapper y) => throw new NotImplementedException();
            public static DoubleWrapper MaxMagnitudeNumber(DoubleWrapper x, DoubleWrapper y) => throw new NotImplementedException();
            public static DoubleWrapper MinMagnitude(DoubleWrapper x, DoubleWrapper y) => throw new NotImplementedException();
            public static DoubleWrapper MinMagnitudeNumber(DoubleWrapper x, DoubleWrapper y) => throw new NotImplementedException();
            public static DoubleWrapper Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static DoubleWrapper Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static DoubleWrapper Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
            public static DoubleWrapper Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
            public static DoubleWrapper Pow(DoubleWrapper x, DoubleWrapper y) => throw new NotImplementedException();
            public static DoubleWrapper RootN(DoubleWrapper x, int n) => throw new NotImplementedException();
            public static DoubleWrapper Round(DoubleWrapper x, int digits, MidpointRounding mode) => throw new NotImplementedException();
            public static DoubleWrapper ScaleB(DoubleWrapper x, int n) => throw new NotImplementedException();
            public static DoubleWrapper Sin(DoubleWrapper x) => throw new NotImplementedException();
            public static (DoubleWrapper Sin, DoubleWrapper Cos) SinCos(DoubleWrapper x) => throw new NotImplementedException();
            public static (DoubleWrapper SinPi, DoubleWrapper CosPi) SinCosPi(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Sinh(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper SinPi(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Sqrt(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Tan(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper Tanh(DoubleWrapper x) => throw new NotImplementedException();
            public static DoubleWrapper TanPi(DoubleWrapper x) => throw new NotImplementedException();

            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out DoubleWrapper result) => throw new NotImplementedException();
            public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out DoubleWrapper result) => throw new NotImplementedException();
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DoubleWrapper result) => throw new NotImplementedException();
            public static bool TryParse(string? s, IFormatProvider? provider, out DoubleWrapper result) => throw new NotImplementedException();

            static bool INumberBase<DoubleWrapper>.TryConvertFromChecked<TOther>(TOther value, out DoubleWrapper result) => throw new NotImplementedException();
            static bool INumberBase<DoubleWrapper>.TryConvertFromSaturating<TOther>(TOther value, out DoubleWrapper result) => throw new NotImplementedException();
            static bool INumberBase<DoubleWrapper>.TryConvertFromTruncating<TOther>(TOther value, out DoubleWrapper result) => throw new NotImplementedException();
            static bool INumberBase<DoubleWrapper>.TryConvertToChecked<TOther>(DoubleWrapper value, out TOther result) => throw new NotImplementedException();
            static bool INumberBase<DoubleWrapper>.TryConvertToSaturating<TOther>(DoubleWrapper value, out TOther result) => throw new NotImplementedException();
            static bool INumberBase<DoubleWrapper>.TryConvertToTruncating<TOther>(DoubleWrapper value, out TOther result) => throw new NotImplementedException();

            public int CompareTo(object? obj) => obj is DoubleWrapper other ? CompareTo(other) : throw new ArgumentException();
            public int CompareTo(DoubleWrapper other) => _value.CompareTo(other._value);
            public bool Equals(DoubleWrapper other) => _value.Equals(other._value);

            public int GetExponentByteCount() => ((IFloatingPoint<double>)_value).GetExponentByteCount();
            public int GetExponentShortestBitLength() => ((IFloatingPoint<double>)_value).GetExponentShortestBitLength();
            public int GetSignificandBitLength() => ((IFloatingPoint<double>)_value).GetSignificandBitLength();
            public int GetSignificandByteCount() => ((IFloatingPoint<double>)_value).GetSignificandByteCount();

            public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);

            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
                _value.TryFormat(destination, out charsWritten, format, provider);

            public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<double>)_value).TryWriteExponentBigEndian(destination, out bytesWritten);

            public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<double>)_value).TryWriteExponentLittleEndian(destination, out bytesWritten);

            public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<double>)_value).TryWriteSignificandBigEndian(destination, out bytesWritten);

            public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) =>
                ((IFloatingPoint<double>)_value).TryWriteSignificandLittleEndian(destination, out bytesWritten);

            public override bool Equals(object o) => o is DoubleWrapper other && Equals(other);
            public override int GetHashCode() => _value.GetHashCode();

            public static DoubleWrapper operator +(DoubleWrapper value) => new(+value._value);
            public static DoubleWrapper operator +(DoubleWrapper left, DoubleWrapper right) => new(left._value + right._value);
            public static DoubleWrapper operator -(DoubleWrapper value) => new(-value._value);
            public static DoubleWrapper operator -(DoubleWrapper left, DoubleWrapper right) => new(left._value - right._value);
            public static DoubleWrapper operator ++(DoubleWrapper value) => new(value._value + 1.0);
            public static DoubleWrapper operator --(DoubleWrapper value) => new(value._value - 1.0);
            public static DoubleWrapper operator *(DoubleWrapper left, DoubleWrapper right) => new(left._value * right._value);
            public static DoubleWrapper operator /(DoubleWrapper left, DoubleWrapper right) => new(left._value / right._value);
            public static DoubleWrapper operator %(DoubleWrapper left, DoubleWrapper right) => new(left._value % right._value);
            public static bool operator ==(DoubleWrapper left, DoubleWrapper right) => left._value == right._value;
            public static bool operator !=(DoubleWrapper left, DoubleWrapper right) => left._value != right._value;
            public static bool operator <(DoubleWrapper left, DoubleWrapper right) => left._value < right._value;
            public static bool operator >(DoubleWrapper left, DoubleWrapper right) => left._value > right._value;
            public static bool operator <=(DoubleWrapper left, DoubleWrapper right) => left._value <= right._value;
            public static bool operator >=(DoubleWrapper left, DoubleWrapper right) => left._value >= right._value;
        }
    }
}
