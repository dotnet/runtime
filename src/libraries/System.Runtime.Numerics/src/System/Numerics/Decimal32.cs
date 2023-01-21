// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

        private const NumberStyles DefaultParseStyle = NumberStyles.Float | NumberStyles.AllowThousands; // TODO is this correct?

        //
        // Constants for manipulating the private bit-representation
        //

        internal const uint SignMask = 0x8000_0000;
        internal const int SignShift = 31;

        internal const uint CombinationMask = 0x7FF0_0000;
        internal const int CombinationShift = 20;
        internal const int CombinationWidth = 11;
        internal const ushort ShiftedCombinationMask = (ushort)(CombinationMask >> CombinationShift);

        internal const uint TrailingSignificandMask = 0x000F_FFFF;
        internal const int TrailingSignificandWidth = 20;

        internal const sbyte EMax = 96;
        internal const sbyte EMin = -95;

        internal const byte Precision = 7;
        internal const byte ExponentBias = 101;

        internal const sbyte MaxQExponent = EMax - Precision + 1;
        internal const sbyte MinQExponent = EMin - Precision + 1;

        internal const int MaxSignificand = 9999999;
        internal const int MinSignificand = -9999999;

        // The 5 bits that classify the value as NaN, Infinite, or Finite
        // If the Classification bits are set to 11111, the value is NaN
        // If the Classification bits are set to 11110, the value is Infinite
        // Otherwise, the value is Finite
        internal const uint ClassificationMask = 0x7C00_0000;
        internal const uint NaNMask = 0x7C00_0000;
        internal const uint InfinityMask = 0x7800_0000;

        // If the Classification bits are set to 11XXX, we encode the significand one way. Otherwise, we encode it a different way
        internal const uint FiniteNumberClassificationMask = 0x6000_0000;

        // Finite significands are encoded in two different ways. Encoding type can be selected based on whether or not this bit is set in the decoded significand.
        internal const uint SignificandEncodingTypeMask = 0x0080_0000;

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

        internal readonly uint _value; // TODO: Single places this at the top, Half places it here. Also, Single has this as private, Half has it as internal. What do we want?

        //
        // Internal Constructors and Decoders
        //

        internal Decimal32(uint value)
        {
            _value = value;
        }

        // Constructs a Decimal32 representing a value in the form (-1)^s * 10^q * c, where
        // * s is 0 or 1
        // * q is any integer MinQExponent <= q <= MaxQExponent
        // * c is the significand represented by a digit string of the form
        //   `d0 d1 d2 ... dp-1`, where p is Precision. c is an integer with 0 <= c < 10^p.
        internal Decimal32(bool sign, sbyte q, uint c)
        {
            Debug.Assert(q >= MinQExponent && q <= MaxQExponent);
            Debug.Assert(q + ExponentBias >= 0);
            Debug.Assert(c <= MaxSignificand);

            uint trailing_sig = c & TrailingSignificandMask;

            // Two types of combination encodings for finite numbers
            uint combination = 0;

            if ((c & SignificandEncodingTypeMask) == 0)
            {
                // We are encoding a significand that has the most significand 4 bits set to 0xyz
                combination |= (uint)(q + ExponentBias) << 3;
                combination |= 0b0111 & (c >> TrailingSignificandWidth); // combination = (biased_exponent, xyz)
            }
            else
            {
                // We are encoding a significand that has the most significand 4 bits set to 100x
                combination |= 0b11 << (CombinationWidth - 2);
                combination |= (uint)(q + ExponentBias) << 1;
                combination |= 0b0001 & (c >> CombinationShift); // combination = (11, biased_exponent, x)
            }

            _value = (uint)(((sign ? 1 : 0) << SignShift) + (combination << CombinationShift) + trailing_sig);
        }

        internal byte BiasedExponent
        {
            get
            {
                uint bits = _value;
                return ExtractBiasedExponentFromBits(bits);
            }
        }

        internal sbyte Exponent
        {
            get
            {
                return (sbyte)(BiasedExponent - ExponentBias);
            }
        }

        internal uint Significand
        {
            get
            {
                return ExtractSignificandFromBits(_value);
            }
        }

        internal uint TrailingSignificand
        {
            get
            {
                return _value & TrailingSignificandMask;
            }
        }

        // returns garbage for infinity and NaN, TODO maybe fix this
        internal static byte ExtractBiasedExponentFromBits(uint bits)
        {
            ushort combination = (ushort)((bits >> CombinationShift) & ShiftedCombinationMask);

            // Two types of encodings for finite numbers
            if ((bits & FiniteNumberClassificationMask) == FiniteNumberClassificationMask)
            {
                // G0 and G1 are 11, exponent is stored in G2:G(CombinationWidth - 1)
                return (byte)(combination >> 1);
            }
            else
            {
                // G0 and G1 are not 11, exponent is stored in G0:G(CombinationWidth - 3)
                return (byte)(combination >> 3);
            }
        }

        // returns garbage for infinity and NaN, TODO maybe fix this
        internal static uint ExtractSignificandFromBits(uint bits)
        {
            ushort combination = (ushort)((bits >> CombinationShift) & ShiftedCombinationMask);

            // Two types of encodings for finite numbers
            uint significand;
            if ((bits & FiniteNumberClassificationMask) == FiniteNumberClassificationMask)
            {
                // G0 and G1 are 11, 4 MSBs of significand are 100x, where x is G(CombinationWidth)
                significand = (uint)(0b1000 | (combination & 0b1));
            }
            else
            {
                // G0 and G1 are not 11, 4 MSBs of significand are 0xyz, where G(CombinationWidth - 2):G(CombinationWidth)
                significand = (uint)(combination & 0b111);
            }
            significand <<= TrailingSignificandWidth;
            significand += bits & TrailingSignificandMask;
            return significand;
        }

        // IEEE 754 specifies NaNs to be propagated
        internal static Decimal32 Negate(Decimal32 value)
        {
            return IsNaN(value) ? value : new Decimal32((ushort)(value._value ^ SignMask));
        }

        private static uint StripSign(Decimal32 value)
        {
            return value._value & ~SignMask;
        }

        //
        // Parsing (INumberBase, IParsable, ISpanParsable)
        //

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="Decimal32.PositiveInfinity"/> or <see cref="Decimal32.NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException("s");
            return IeeeDecimalNumber.ParseDecimal32(s, DefaultParseStyle, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="Decimal32.PositiveInfinity"/> or <see cref="Decimal32.NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(string s, NumberStyles style)
        {
            IeeeDecimalNumber.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException("s");
            return IeeeDecimalNumber.ParseDecimal32(s, style, NumberFormatInfo.CurrentInfo);
        }

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            IeeeDecimalNumber.ValidateParseStyleFloatingPoint(DefaultParseStyle); // TODO I copied this from NumberFormatInfo to IeeeDecimalNumber, is that ok?
            return IeeeDecimalNumber.ParseDecimal32(s, DefaultParseStyle, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
        public static Decimal32 Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException("s");
            return IeeeDecimalNumber.ParseDecimal32(s, DefaultParseStyle, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)" />
        public static Decimal32 Parse(ReadOnlySpan<char> s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            IeeeDecimalNumber.ValidateParseStyleFloatingPoint(style);
            return IeeeDecimalNumber.ParseDecimal32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(string, NumberStyles, IFormatProvider?)" />
        public static Decimal32 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            IeeeDecimalNumber.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException("s");
            return IeeeDecimalNumber.ParseDecimal32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="Decimal32.PositiveInfinity"/> or <see cref="Decimal32.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal32 result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }
            return TryParse(s, DefaultParseStyle, provider: null, out result);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="Decimal32.PositiveInfinity"/> or <see cref="Decimal32.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal32 result)
        {
            return TryParse(s, DefaultParseStyle, provider: null, out result);
        }

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, DefaultParseStyle, provider, out result);

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, DefaultParseStyle, provider, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            IeeeDecimalNumber.ValidateParseStyleFloatingPoint(style);
            return IeeeDecimalNumber.TryParseDecimal32(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(string?, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            IeeeDecimalNumber.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = Zero;
                return false;
            }

            return IeeeDecimalNumber.TryParseDecimal32(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        //
        // Misc. Methods (IComparable, IEquatable)
        //

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="obj"/>, zero if this is equal to <paramref name="obj"/>, or a value greater than zero if this is greater than <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not of type <see cref="Decimal32"/>.</exception>
        public int CompareTo(object? obj) => throw new NotImplementedException();

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="other"/>, zero if this is equal to <paramref name="other"/>, or a value greater than zero if this is greater than <paramref name="other"/>.</returns>
        public int CompareTo(Decimal32 other) => throw new NotImplementedException();

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Decimal32 other) && Equals(other);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="other"/> value.
        /// </summary>
        public bool Equals(Decimal32 other)
        {
            return this == other
                || (IsNaN(this) && IsNaN(other));
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            // TODO we know that NaNs and Zeros should hash the same. Should values of the same cohort have the same hash?
            throw new NotImplementedException();
        }

        //
        // Formatting (IFormattable, ISpanFormattable)
        //

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) // TODO the interface calls this second param "formatProvider". Which do we want?
        {
            // Temporary Formatting for debugging
            if (IsNaN(this))
            {
                return "NaN";
            }
            else if (IsPositiveInfinity(this))
            {
                return "Infinity";
            }
            else if (IsNegativeInfinity(this))
            {
                return "-Infinity";
            }

            return (IsPositive(this) ? "" : "-") + Significand.ToString() + "E" + Exponent.ToString();
        }

        /// <summary>
        /// Tries to format the value of the current Decimal32 instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns></returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            throw new NotImplementedException();
        }


        //
        // Explicit Convert To Decimal32 TODO
        //

        //
        // Explicit Convert From Decimal32 TODO
        //

        //
        // Implicit Convert To Decimal32 TODO
        //

        //
        // Implicit Convert From Decimal32 TODO
        //

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static Decimal32 operator +(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Decimal32 IAdditiveIdentity<Decimal32, Decimal32>.AdditiveIdentity => new Decimal32(PositiveZeroBits); // TODO make sure this is a zero such that the quantum of any other value is preserved on addition


        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // IDecimalFloatingPointIeee754
        //

        /// <inheritdoc cref="IDecimalFloatingPointIeee754{TSelf}.Quantize(TSelf, TSelf)" />
        public static Decimal32 Quantize(Decimal32 x, Decimal32 y)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IDecimalFloatingPointIeee754{TSelf}.Quantum(TSelf)" />
        public static Decimal32 Quantum(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IDecimalFloatingPointIeee754{TSelf}.SameQuantum(TSelf, TSelf)" />
        public static bool SameQuantum(Decimal32 x, Decimal32 y)
        {
            return x.Exponent == y.Exponent
                || (IsInfinity(x) && IsInfinity(y))
                || (IsNaN(x) && IsNaN(y));
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Decimal32 operator --(Decimal32 value) => throw new NotImplementedException();

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static Decimal32 operator /(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // IEqualityOperators
        //

        // Fast access for 10^n where n is 0:(Precision - 1)
        private static readonly uint[] s_powers10 = new uint[] {
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000
        };

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(Decimal32 left, Decimal32 right) // TODO we can probably do this faster
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is not equal to anything, including itself.
                return false;
            }

            if (IsZero(left) && IsZero(right))
            {
                // IEEE defines that positive and negative zero are equivalent.
                return true;
            }

            bool sameSign = IsPositive(left) == IsPositive(right);

            if (IsInfinity(left) || IsInfinity(right))
            {
                if (IsInfinity(left) && IsInfinity(right) && sameSign)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // IEEE defines that two values of the same cohort are numerically equivalent

            uint leftSignificand = left.Significand;
            uint rightSignificand = right.Significand;
            sbyte leftQ = left.Exponent;
            sbyte rightQ = right.Exponent;
            int diffQ = leftQ - rightQ;

            bool sameNumericalValue = false;
            if (int.Abs(diffQ) < Precision) // If diffQ is >= Precision, the non-zero finite values have exponents too far apart for them to possibly be equal
            {
                try
                {
                    if (diffQ < 0)
                    {
                        // leftQ is smaller than rightQ, scale leftSignificand
                        leftSignificand = checked(leftSignificand * s_powers10[int.Abs(diffQ)]);
                    }
                    else
                    {
                        // rightQ is smaller than (or equal to) leftQ, scale rightSignificand
                        rightSignificand = checked(rightSignificand * s_powers10[diffQ]);
                    }
                }
                catch
                {
                    // multiplication overflowed, return false
                    return false;
                }

                if (leftSignificand == rightSignificand)
                {
                    sameNumericalValue = true;
                }
            }

            return sameNumericalValue && sameSign;
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static Decimal32 Exp(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static Decimal32 ExpM1(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static Decimal32 Exp2(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static Decimal32 Exp2M1(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static Decimal32 Exp10(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static Decimal32 Exp10M1(Decimal32 x) => throw new NotImplementedException();

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static Decimal32 Ceiling(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static Decimal32 Floor(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static Decimal32 Round(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static Decimal32 Round(Decimal32 x, int digits) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static Decimal32 Round(Decimal32 x, MidpointRounding mode) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static Decimal32 Round(Decimal32 x, int digits, MidpointRounding mode) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static Decimal32 Truncate(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<Decimal32>.GetExponentByteCount() => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<Decimal32>.GetExponentShortestBitLength() => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<Decimal32>.GetSignificandBitLength() => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<Decimal32>.GetSignificandByteCount() => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) => throw new NotImplementedException();

        //
        // IFloatingPointConstants
        //

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.E" />
        public static Decimal32 E => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        public static Decimal32 Pi => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        public static Decimal32 Tau => throw new NotImplementedException();

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Epsilon" />
        public static Decimal32 Epsilon => new Decimal32(EpsilonBits);                      //  1E-101

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NaN" />
        public static Decimal32 NaN => new Decimal32(QNanBits);                             //  0.0 / 0.0

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeInfinity" />
        public static Decimal32 NegativeInfinity => new Decimal32(NegativeInfinityBits);    // -1.0 / 0.0

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static Decimal32 NegativeZero => new Decimal32(NegativeZeroBits);            // -0E-101

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.PositiveInfinity" />
        public static Decimal32 PositiveInfinity => new Decimal32(PositiveInfinityBits);    //  1.0 / 0.0

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static Decimal32 Atan2(Decimal32 y, Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static Decimal32 Atan2Pi(Decimal32 y, Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static Decimal32 BitDecrement(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static Decimal32 BitIncrement(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static Decimal32 FusedMultiplyAdd(Decimal32 left, Decimal32 right, Decimal32 addend) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static Decimal32 Ieee754Remainder(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static Decimal32 ReciprocalEstimate(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static Decimal32 ReciprocalSqrtEstimate(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static Decimal32 ScaleB(Decimal32 x, int n) => throw new NotImplementedException();

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static Decimal32 Compound(Half x, Decimal32 n) => throw new NotImplementedException();

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static Decimal32 Acosh(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static Decimal32 Asinh(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static Decimal32 Atanh(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static Decimal32 Cosh(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static Decimal32 Sinh(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static Decimal32 Tanh(Decimal32 x) => throw new NotImplementedException();

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static Decimal32 operator ++(Decimal32 value) => throw new NotImplementedException();

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static Decimal32 Log(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static Decimal32 Log(Decimal32 x, Decimal32 newBase) => throw new NotImplementedException();

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static Decimal32 Log10(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static Decimal32 LogP1(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2(TSelf)" />
        public static Decimal32 Log2(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static Decimal32 Log2P1(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static Decimal32 Log10P1(Decimal32 x) => throw new NotImplementedException();

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static Decimal32 MaxValue => new Decimal32(MaxValueBits);                    //  9.999999E90

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static Decimal32 MinValue => new Decimal32(MinValueBits);                    // -9.999999E90

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static Decimal32 operator %(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        public static Decimal32 MultiplicativeIdentity => new Decimal32(PositiveZeroBits); // TODO make sure this is a zero such that the quantum of any other value is preserved on multiplication

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static Decimal32 operator *(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static Decimal32 Clamp(Decimal32 value, Decimal32 min, Decimal32 max) => throw new NotImplementedException();

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static Decimal32 CopySign(Decimal32 value, Decimal32 sign) => throw new NotImplementedException();

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static Decimal32 Max(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static Decimal32 MaxNumber(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static Decimal32 Min(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        public static Decimal32 MinNumber(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(Decimal32 value) => throw new NotImplementedException();


        //
        // INumberBase (well defined/commonly used values)
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        public static Decimal32 One => new Decimal32(PositiveOneBits);                      //  1E0

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Decimal32>.Radix => 10; // TODO this should be exposed implicitly as it is required by IEEE

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        public static Decimal32 Zero => new Decimal32(PositiveZeroBits);                    // -0E-101

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static Decimal32 Abs(Decimal32 value)
        {
            return new Decimal32(value._value & ~SignMask);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal32 CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal32 result;

            if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal32 CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal32 result;

            if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal32 CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal32 result;

            if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Decimal32>.IsCanonical(Decimal32 value) => throw new NotImplementedException(); // TODO this should be exposed implicitly as it is required by IEEE

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<Decimal32>.IsComplexNumber(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        public static bool IsFinite(Decimal32 value)
        {
            return StripSign(value) < PositiveInfinityBits;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<Decimal32>.IsImaginaryNumber(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        public static bool IsInfinity(Decimal32 value)
        {
            return (value._value & ClassificationMask) == InfinityMask;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        public static bool IsNaN(Decimal32 value)
        {
            return (value._value & ClassificationMask) == NaNMask;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(Decimal32 value)
        {
            return (int)(value._value) < 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        public static bool IsNegativeInfinity(Decimal32 value)
        {
            return IsInfinity(value) && IsNegative(value);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        public static bool IsNormal(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(Decimal32 value)
        {
            return (int)(value._value) >= 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        public static bool IsPositiveInfinity(Decimal32 value)
        {
            return IsInfinity(value) && IsPositive(value);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        public static bool IsSubnormal(Decimal32 value) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Decimal32>.IsZero(Decimal32 value) // TODO this should be exposed implicitly as it is required by IEEE (see private function below)
        {
            return value.Significand == 0;
        }

        private static bool IsZero(Decimal32 value)
        {
            return value.Significand == 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static Decimal32 MaxMagnitude(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static Decimal32 MaxMagnitudeNumber(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Decimal32 MinMagnitude(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static Decimal32 MinMagnitudeNumber(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromChecked<TOther>(TOther value, out Decimal32 result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060 // Remove unused parameter
        private static bool TryConvertFromChecked<TOther>(TOther value, out Decimal32 result)
#pragma warning restore IDE0060 // Remove unused parameter
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(char))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(double))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Half))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(short))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(int))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(long))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(float))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(uint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                throw new NotImplementedException();
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromSaturating<TOther>(TOther value, out Decimal32 result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060 // Remove unused parameter
        private static bool TryConvertFromSaturating<TOther>(TOther value, out Decimal32 result)
#pragma warning restore IDE0060 // Remove unused parameter
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(char))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(double))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Half))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(short))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(int))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(long))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(float))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(uint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                throw new NotImplementedException();
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromTruncating<TOther>(TOther value, out Decimal32 result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060 // Remove unused parameter TODO remove this
        private static bool TryConvertFromTruncating<TOther>(TOther value, out Decimal32 result)
#pragma warning restore IDE0060 // Remove unused parameter
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(char))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(double))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Half))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(short))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(int))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(long))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(float))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(uint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                throw new NotImplementedException();
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertToChecked<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(char))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(double))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Half))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(short))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(int))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(long))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(float))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(uint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                throw new NotImplementedException();
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertToSaturating<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(char))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(double))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Half))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(short))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(int))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(long))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(float))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(uint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                throw new NotImplementedException();
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertToTruncating<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(char))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(double))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Half))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(short))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(int))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(long))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(float))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(uint))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                throw new NotImplementedException();
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                throw new NotImplementedException();
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static Decimal32 Pow(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static Decimal32 Cbrt(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static Decimal32 Hypot(Decimal32 x, Decimal32 y) => throw new NotImplementedException();

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static Decimal32 RootN(Decimal32 x, int n) => throw new NotImplementedException();

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static Decimal32 Sqrt(Decimal32 x) => throw new NotImplementedException();

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        public static Decimal32 NegativeOne => new Decimal32(NegativeOneBits);              // -1E0

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static Decimal32 operator -(Decimal32 left, Decimal32 right) => throw new NotImplementedException();

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static Decimal32 Acos(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static Decimal32 AcosPi(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static Decimal32 Asin(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static Decimal32 AsinPi(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static Decimal32 Atan(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static Decimal32 AtanPi(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static Decimal32 Cos(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static Decimal32 CosPi(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static Decimal32 Sin(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (Decimal32 Sin, Decimal32 Cos) SinCos(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCosPi(TSelf)" />
        public static (Decimal32 SinPi, Decimal32 CosPi) SinCosPi(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static Decimal32 SinPi(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static Decimal32 Tan(Decimal32 x) => throw new NotImplementedException();

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static Decimal32 TanPi(Decimal32 x) => throw new NotImplementedException();

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static Decimal32 operator -(Decimal32 value) => Negate(value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static Decimal32 operator +(Decimal32 value) => value;
    }
}
