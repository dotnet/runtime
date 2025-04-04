// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Numerics
{
    public readonly struct Decimal128
        : IComparable,
          IComparable<Decimal128>,
          IEquatable<Decimal128>,
          ISpanParsable<Decimal128>,
          IDecimalIeee754ParseAndFormatInfo<Decimal128>,
          IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>,
          IDecimalIeee754TryParseInfo<Decimal128, Int128>
    {
#if BIGENDIAN
        internal readonly ulong _upper;
        internal readonly ulong _lower;
#else
        internal readonly ulong _lower;
        internal readonly ulong _upper;
#endif

        private const int MaxDecimalExponent = 6111;
        private const int MinDecimalExponent = -6176;
        private const int NumberDigitsPrecision = 34;
        private const int Bias = 6176;
        private const int NumberBitsExponent = 14;
        private static readonly UInt128 PositiveInfinityValue = new UInt128(upper: 0x7800_0000_0000_0000, lower: 0);
        private static readonly UInt128 NegativeInfinityValue = new UInt128(upper: 0xf800_0000_0000_0000, lower: 0);
        private static readonly UInt128 ZeroValue = new UInt128(0, 0);

        public Decimal128(Int128 significand, int exponent)
        {
            UInt128 value = Number.CalculateDecimalIeee754<Decimal128, Int128, UInt128>(significand, exponent);
            _lower = value.Lower;
            _upper = value.Upper;
        }

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal128 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal128 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <inheritdoc cref="ISpanParsable{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal128 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal128(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal128 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal128 result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal128 result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <inheritdoc cref="ISpanParsable{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out T)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => TryParse(s, NumberStyles.Number, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="string"/> with the given <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => TryParse(s, NumberStyles.Number, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimalIeee754<Decimal128, Int128, char>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = default;
                return false;
            }
            return Number.TryParseDecimalIeee754<Decimal128, Int128, char>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is not Decimal128 i)
            {
                throw new ArgumentException(SR.Arg_MustBeDecimal128);
            }

            var current = new UInt128(_upper, _lower);
            var other = new UInt128(i._upper, i._lower);

            return Number.CompareDecimalIeee754<Decimal128, Int128, UInt128>(current, other);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal128 other)
        {
            var current = new UInt128(_upper, _lower);
            var another = new UInt128(other._upper, other._lower);
            return Number.CompareDecimalIeee754<Decimal128, Int128, UInt128>(current, another);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal128 other)
        {
            var current = new UInt128(_upper, _lower);
            var another = new UInt128(other._upper, other._lower);
            return Number.CompareDecimalIeee754<Decimal128, Int128, UInt128>(current, another) == 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal128 && Equals((Decimal128)obj);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            return new UInt128(_upper, _lower).GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatDecimal128(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimal128(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimal128(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimal128(this, format, NumberFormatInfo.GetInstance(provider));
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128>.Precision => NumberDigitsPrecision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128>.MaxScale => 6145;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.CountDigits(Int128 number) => FormattingHelpers.CountDigits((UInt128)number);

        static Int128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.Power10(int exponent) => Int128Powers10[exponent];

        static Int128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MaxSignificand => new Int128(upper: 0x0001_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF); // 9_999_999_999_999_999_999_999_999_999_999_999;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MaxDecimalExponent => MaxDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MinDecimalExponent => MinDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.Precision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.ExponentBias => Bias;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsEncoding => 128;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsCombinationField => 17;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsExponent => NumberBitsExponent;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.PositiveInfinityBits => PositiveInfinityValue;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NegativeInfinityBits => NegativeInfinityValue;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.Zero => ZeroValue;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.G0G1Mask => new UInt128(0x6000_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MostSignificantBitOfSignificandMask => new UInt128(0x0002_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.SignMask => new UInt128(0x8000_0000_0000_0000, 0);

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.ConvertToExponent(UInt128 value) => (int)value;

        static Int128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.ConvertToSignificand(UInt128 value) => (Int128)value;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.G0ToGwPlus1ExponentMask => new UInt128(0x7FFE_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.G2ToGwPlus3ExponentMask => new UInt128(0x1FFF_8000_0000_0000, 0);

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.GwPlus2ToGwPlus4SignificandMask => new UInt128(0x0001_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.GwPlus4SignificandMask => new UInt128(0x0000_7FFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsSignificand => 110;

        private static Int128[] Int128Powers10 =>
            [
                new Int128(0, 1),
                new Int128(0, 10),
                new Int128(0, 100),
                new Int128(0, 1000),
                new Int128(0, 10000),
                new Int128(0, 100000),
                new Int128(0, 1000000),
                new Int128(0, 10000000),
                new Int128(0, 100000000),
                new Int128(0, 1000000000),
                new Int128(0, 10000000000),
                new Int128(0, 100000000000),
                new Int128(0, 1000000000000),
                new Int128(0, 10000000000000),
                new Int128(0, 100000000000000),
                new Int128(0, 1000000000000000),
                new Int128(0, 10000000000000000),
                new Int128(0, 100000000000000000),
                new Int128(0, 1000000000000000000),
                new Int128(0, 10000000000000000000),
                new Int128(5, 7766279631452241920),
                new Int128(54, 3875820019684212736),
                new Int128(542, 1864712049423024128),
                new Int128(5421, 200376420520689664),
                new Int128(54210, 2003764205206896640),
                new Int128(542101, 1590897978359414784),
                new Int128(5421010, 15908979783594147840),
                new Int128(54210108, 11515845246265065472),
                new Int128(542101086, 4477988020393345024),
                new Int128(5421010862, 7886392056514347008),
                new Int128(54210108624, 5076944270305263616),
                new Int128(542101086242, 13875954555633532928),
                new Int128(5421010862427, 9632337040368467968),
                new Int128(54210108624275, 4089650035136921600),
                new Int128(542101086242752, 4003012203950112768),
            ];

        static bool IDecimalIeee754TryParseInfo<Decimal128, Int128>.TryNumberToDecimalIeee754(ref Number.NumberBuffer number, out Int128 significand, out int exponent)
            => Number.TryNumberToDecimalIeee754<Decimal128, Int128>(ref number, out significand, out exponent);

        static Decimal128 IDecimalIeee754TryParseInfo<Decimal128, Int128>.Construct(Int128 significand, int exponent) => new Decimal128(significand, exponent);

        static int IDecimalIeee754TryParseInfo<Decimal128, Int128>.DecimalNumberBufferLength => Number.Decimal128NumberBufferLength;
    }
}
