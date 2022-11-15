// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Decimal32
        : IComparable<Decimal32>,
          IComparable,
          ISpanFormattable,
          ISpanParsable,
          IEquatable<Decimal32>,
          IConvertible,
          IDecimalFloatingPointIeee754<Decimal32>,
          IMinMaxValue<Decimal32>
    {
        private readonly uint _value;

        public static Decimal32 Epsilon => throw new NotImplementedException();

        public static Decimal32 NaN => throw new NotImplementedException();

        public static Decimal32 NegativeInfinity => throw new NotImplementedException();

        public static Decimal32 NegativeZero => throw new NotImplementedException();

        public static Decimal32 PositiveInfinity => throw new NotImplementedException();

        public static Decimal32 NegativeOne => throw new NotImplementedException();

        public static Decimal32 E => throw new NotImplementedException();

        public static Decimal32 Pi => throw new NotImplementedException();

        public static Decimal32 Tau => throw new NotImplementedException();

        public static Decimal32 One => throw new NotImplementedException();

        public static int Radix => throw new NotImplementedException();

        public static Decimal32 Zero => throw new NotImplementedException();

        public static Decimal32 AdditiveIdentity => throw new NotImplementedException();

        public static Decimal32 MultiplicativeIdentity => throw new NotImplementedException();

        public static Decimal32 Abs(Decimal32 value) => throw new NotImplementedException();
        public static Decimal32 Acos(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Acosh(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 AcosPi(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Asin(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Asinh(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 AsinPi(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Atan(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Atan2(Decimal32 y, Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Atan2Pi(Decimal32 y, Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Atanh(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 AtanPi(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 BitDecrement(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 BitIncrement(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Cbrt(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Cos(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Cosh(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 CosPi(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Exp(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Exp10(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Exp2(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 FusedMultiplyAdd(Decimal32 left, Decimal32 right, Decimal32 addend) => throw new NotImplementedException();
        public static Decimal32 Hypot(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 Ieee754Remainder(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static int ILogB(Decimal32 x) => throw new NotImplementedException();
        public static bool IsCanonical(Decimal32 value) => throw new NotImplementedException();
        public static bool IsComplexNumber(Decimal32 value) => throw new NotImplementedException();
        public static bool IsEvenInteger(Decimal32 value) => throw new NotImplementedException();
        public static bool IsFinite(Decimal32 value) => throw new NotImplementedException();
        public static bool IsImaginaryNumber(Decimal32 value) => throw new NotImplementedException();
        public static bool IsInfinity(Decimal32 value) => throw new NotImplementedException();
        public static bool IsInteger(Decimal32 value) => throw new NotImplementedException();
        public static bool IsNaN(Decimal32 value) => throw new NotImplementedException();
        public static bool IsNegative(Decimal32 value) => throw new NotImplementedException();
        public static bool IsNegativeInfinity(Decimal32 value) => throw new NotImplementedException();
        public static bool IsNormal(Decimal32 value) => throw new NotImplementedException();
        public static bool IsOddInteger(Decimal32 value) => throw new NotImplementedException();
        public static bool IsPositive(Decimal32 value) => throw new NotImplementedException();
        public static bool IsPositiveInfinity(Decimal32 value) => throw new NotImplementedException();
        public static bool IsRealNumber(Decimal32 value) => throw new NotImplementedException();
        public static bool IsSubnormal(Decimal32 value) => throw new NotImplementedException();
        public static bool IsZero(Decimal32 value) => throw new NotImplementedException();
        public static Decimal32 Log(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Log(Decimal32 x, Decimal32 newBase) => throw new NotImplementedException();
        public static Decimal32 Log10(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Log2(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 MaxMagnitude(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 MaxMagnitudeNumber(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 MinMagnitude(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 MinMagnitudeNumber(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Pow(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 Quantize(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 Quantum(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 RootN(Decimal32 x, int n) => throw new NotImplementedException();
        public static Decimal32 Round(Decimal32 x, int digits, MidpointRounding mode) => throw new NotImplementedException();
        public static bool SameQuantum(Decimal32 x, Decimal32 y) => throw new NotImplementedException();
        public static Decimal32 ScaleB(Decimal32 x, int n) => throw new NotImplementedException();
        public static Decimal32 Sin(Decimal32 x) => throw new NotImplementedException();
        public static (Decimal32 Sin, Decimal32 Cos) SinCos(Decimal32 x) => throw new NotImplementedException();
        public static (Decimal32 SinPi, Decimal32 CosPi) SinCosPi(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Sinh(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 SinPi(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Sqrt(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Tan(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 Tanh(Decimal32 x) => throw new NotImplementedException();
        public static Decimal32 TanPi(Decimal32 x) => throw new NotImplementedException();
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertFromChecked<TOther>(TOther value, out Decimal32 result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertFromSaturating<TOther>(TOther value, out Decimal32 result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertFromTruncating<TOther>(TOther value, out Decimal32 result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertToChecked<TOther>(Decimal32 value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertToSaturating<TOther>(Decimal32 value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertToTruncating<TOther>(Decimal32 value, out TOther result) => throw new NotImplementedException();
        public int CompareTo(object? obj) => throw new NotImplementedException();
        public int CompareTo(Decimal32 other) => throw new NotImplementedException();
        public bool Equals(Decimal32 other) => throw new NotImplementedException();
        public int GetExponentByteCount() => throw new NotImplementedException();
        public int GetExponentShortestBitLength() => throw new NotImplementedException();
        public int GetSignificandBitLength() => throw new NotImplementedException();
        public int GetSignificandByteCount() => throw new NotImplementedException();
        public string ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();
        public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();
        public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();
        public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();
        public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();

        public static Decimal32 operator +(Decimal32 value) => throw new NotImplementedException();
        public static Decimal32 operator +(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static Decimal32 operator -(Decimal32 value) => throw new NotImplementedException();
        public static Decimal32 operator -(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static Decimal32 operator ++(Decimal32 value) => throw new NotImplementedException();
        public static Decimal32 operator --(Decimal32 value) => throw new NotImplementedException();
        public static Decimal32 operator *(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static Decimal32 operator /(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static Decimal32 operator %(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static bool operator ==(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static bool operator !=(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static bool operator <(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static bool operator >(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static bool operator <=(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
        public static bool operator >=(Decimal32 left, Decimal32 right) => throw new NotImplementedException();
    }
}
