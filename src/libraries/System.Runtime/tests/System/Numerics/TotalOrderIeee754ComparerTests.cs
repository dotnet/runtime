// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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

        [Theory]
        [MemberData(nameof(SingleTestData))]
        public void TotalOrderTestNFloat(float x, float y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<NFloat>();
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
    }
}
