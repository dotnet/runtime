// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    /// <summary>
    /// An IEEE 754 compliant float16 type.
    /// </summary>
    [Serializable] // TODO do we need this? Half doesn't have it, Single does.
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Decimal32
        : IComparable<Decimal32>,
          IComparable,
          ISpanFormattable,
          ISpanParsable<Decimal32>,
          IEquatable<Decimal32>,
          IDecimalFloatingPointIeee754<Decimal32>,
          IMinMaxValue<Decimal32>
    {
        // Constants for manipulating the private bit-representation
        // TODO I think adding masks that help us "triage" the type of encoding will be useful.

        internal const uint SignMask = 0x8000_0000;
        internal const int SignShift = 31;

        internal const uint CombinationMask = 0x7FF0_0000;
        internal const int CombinationShift = 20;

        // TODO figure out if these three are useful, I don't think they are
        internal const uint NaNMask = 0x7C00_0000;
        internal const uint InfinityMask = 0x7800_0000;
        internal const uint TrailingSignificandMask = 0x000F_FFFF;

        // TODO I think these might be useless in Decimal
/*        internal const ushort MinCombination = 0x0000;
        internal const ushort MaxCombination = 0x07FF;*/

        internal const sbyte EMax = 96;
        internal const sbyte EMin = -95;

        internal const byte Precision = 7;
        internal const byte ExponentBias = 101;

        internal const sbyte MaxQExponent = EMax - Precision + 1;
        internal const sbyte MinQExponent = EMin - Precision + 1;

        // TODO I think these might be useless in Decimal
        /*        internal const uint MinTrailingSignificand = 0x0000_0000;
                internal const uint MaxTrailingSignificand = 0x000F_FFFF; // TODO double check this, might be artificially bounded below this*/

        // Constants representing the private bit-representation for various default values.
        // See either IEEE-754 2019 section 3.5 or https://en.wikipedia.org/wiki/Decimal32_floating-point_format for a breakdown of the encoding.

        // PositiveZero Bits
        // Hex:                   0x0000_0000
        // Binary:                0000_0000_0000_0000_0000_0000_0000_0000
        // Split into sections:   0 | 000_0000_0 | 000_0000_0000_0000_0000_0000
        //                        0 | 0000_0000  | 000_0000_0000_0000_0000_0000
        // Section labels:        a | b          | c
        //
        // a. Sign bit.
        // b. Biased Exponent, which is q + 101. 0000_0000 == 0 == -101 + 101, so this is encoding a q of -101.
        // c. Significand, set to 0.
        //
        // Encoded value:         0 x 10^-101

        private const uint PositiveZeroBits = 0x0000_0000;
        private const uint NegativeZeroBits = SignMask | PositiveZeroBits;


        // Epsilon Bits
        // Hex:                   0x0000_0001
        // Binary:                0000_0000_0000_0000_0000_0000_0000_0001
        // Split into sections:   0 | 000_0000_0 | 000_0000_0000_0000_0000_0001
        //                        0 | 0000_0000  | 000_0000_0000_0000_0000_0001
        // Section labels:        a | b          | c
        //
        // a. Sign bit.
        // b. Biased Exponent, which is q + 101. 0000_0000 == 0 == -101 + 101, so this is encoding a q of -101.
        // c. Significand, set to 1.
        //
        // Encoded value:         1 x 10^-101

        private const uint EpsilonBits = 0x0000_0001;

        // PositiveInfinityBits
        // Hex:                   0x7800_0000
        // Binary:                0111_1000_0000_0000_0000_0000_0000_0000
        // Split into sections:   0 | 111_1000_0000 | 0000_0000_0000_0000_0000
        // Section labels:        a | b             | c
        //
        // a. Sign bit.
        // b. Combination field G0 through G10. G0-G4 == 11110 encodes infinity.
        // c. Trailing significand.
        // Note: Canonical infinity has everything after G5 set to 0.
        private const uint PositiveInfinityBits = 0x7800_0000;
        private const uint NegativeInfinityBits = SignMask | PositiveInfinityBits;


        // QNanBits
        // Hex:                   0x7C00_0000
        // Binary:                0111_1100_0000_0000_0000_0000_0000_0000
        // Split into sections:   0 | 111_1100_0000 | 0000_0000_0000_0000_0000
        // Section labels:        a | b             | c
        //
        // a. Sign bit (ignored for NaN).
        // b. Combination field G0 through G10. G0-G4 == 11111 encodes NaN.
        // c. Trailing significand. Can be used to encode a payload, to distinguish different NaNs.
        // Note: Canonical NaN has G6-G10 as 0 and the encoding of the payload also canonical.
        private const uint QNanBits = 0x7C00_0000; // TODO I used a "positive" NaN here, should it be negative?
        private const uint SNanBits = 0x7E00_0000;


        // MaxValueBits
        // Hex:                   0x77F8_967F
        // Binary:                0111_0111_1111_1000_1001_0110_0111_1111
        // Split into sections:   0 | 11 | 1_0111_111 | 1_1000_1001_0110_0111_1111
        //                        0 | 11 | 1011_1111  | [100]1_1000_1001_0110_0111_1111
        // Section labels:        a | b  | c          | d
        //
        // a. Sign bit.
        // b. G0 and G1 of the combination field, "11" indicates this version of encoding.
        // c. Biased Exponent, which is q + 101. 1011_1111 == 191 == 90 + 101, so this is encoding an q of 90.
        // d. Significand. Section b. indicates an implied prefix of [100]. [100]1_1000_1001_0110_0111_1111 == 9,999,999.
        //
        // Encoded value:         9,999,999 x 10^90
        private const uint MaxValueBits = 0x77F8_967F;
        private const uint MinValueBits = SignMask | MaxValueBits;

        // PositiveOneBits Bits
        // Hex:                   0x3280_0001
        // Binary:                0011_0010_1000_0000_0000_0000_0000_0001
        // Split into sections:   0 | 011_0010_1 | 000_0000_0000_0000_0000_0001
        //                        0 | 0110_0101  | 000_0000_0000_0000_0000_0001
        // Section labels:        a | b          | c
        //
        // a. Sign bit.
        // b. Biased Exponent, which is q + 101. 0110_0101 == 101 == 0 + 101, so this is encoding an q of 0.
        // c. Significand, set to 1.
        //
        // Encoded value:         1 x 10^0
        private const uint PositiveOneBits = 0x3280_0001;
        private const uint NegativeOneBits = SignMask | PositiveOneBits;
        /*
                private const uint EBits = 0; // TODO
                private const uint PiBits = 0; // TODO
                private const uint TauBits = 0; // TODO*/

        // Well-defined and commonly used values

        public static Decimal32 MaxValue => new Decimal32(MaxValueBits);                    //  9.999999E90

        public static Decimal32 MinValue => new Decimal32(MinValueBits);                    // -9.999999E90

        public static Decimal32 Epsilon => new Decimal32(EpsilonBits);                      //  1E-101

        public static Decimal32 NaN => new Decimal32(QNanBits);                             //  0.0 / 0.0

        public static Decimal32 NegativeInfinity => new Decimal32(NegativeInfinityBits);    // -1.0 / 0.0

        public static Decimal32 NegativeZero => new Decimal32(NegativeZeroBits);            // -0E-101

        public static Decimal32 PositiveInfinity => new Decimal32(PositiveInfinityBits);    //  1.0 / 0.0

        public static Decimal32 NegativeOne => new Decimal32(NegativeOneBits);              // -1E0

        public static Decimal32 E => throw new NotImplementedException();

        public static Decimal32 Pi => throw new NotImplementedException();

        public static Decimal32 Tau => throw new NotImplementedException();

        public static Decimal32 One => new Decimal32(PositiveOneBits);                      //  1E0

        public static int Radix => 10;

        public static Decimal32 Zero => new Decimal32(PositiveZeroBits);                    // -0E-101

        public static Decimal32 AdditiveIdentity => new Decimal32(PositiveZeroBits); // TODO do we want to make sure this is a zero such that the quantum of any other value is preserved on addition?

        public static Decimal32 MultiplicativeIdentity => new Decimal32(PositiveZeroBits); // TODO do we want to make sure this is a zero such that the quantum of any other value is preserved on addition?

        internal readonly uint _value; // TODO: Single places this at the top, Half places it here. Also, Single has this as private, Half has it as internal. What do we want?

        // Private Constructors

        internal Decimal32(uint value)
        {
            _value = value;
        }

        private Decimal32(bool sign, uint combination, uint trailing_sig) => _value = (uint)(((sign ? 1 : 0) << SignShift) + (combination << CombinationShift) + trailing_sig); // TODO do we need this?
        private Decimal32(bool sign, uint q, uint sig)
        {
            // Edge conditions: MinQExponent <= q <= MaxQExponent, 0 <= sig <= 9999999
            _value = 0; // TODO
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static Decimal32 Abs(Decimal32 value)
        {
            return new Decimal32(value._value & ~SignMask);
        }

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
        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
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
        public static Decimal32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        public static Decimal32 Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
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
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => throw new NotImplementedException();
        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromChecked<TOther>(TOther value, out Decimal32 result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromSaturating<TOther>(TOther value, out Decimal32 result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromTruncating<TOther>(TOther value, out Decimal32 result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }
        private static bool TryConvertFrom<TOther>(TOther value, out Decimal32 result)
            where TOther : INumberBase<TOther>
        {
            // TODO
        }
        static bool INumberBase<Decimal32>.TryConvertToChecked<TOther>(Decimal32 value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertToSaturating<TOther>(Decimal32 value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<Decimal32>.TryConvertToTruncating<TOther>(Decimal32 value, out TOther result) => throw new NotImplementedException();
        public int CompareTo(Decimal32 other) => throw new NotImplementedException();
        public int CompareTo(object? obj) => throw new NotImplementedException();
        public bool Equals(Decimal32 other) => throw new NotImplementedException();
        public int GetExponentByteCount() => throw new NotImplementedException();
        public int GetExponentShortestBitLength() => throw new NotImplementedException();
        public int GetSignificandBitLength() => throw new NotImplementedException();
        public int GetSignificandByteCount() => throw new NotImplementedException();
        public TypeCode GetTypeCode() => throw new NotImplementedException();
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

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal32 && Equals((Decimal32)obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
    }
}
