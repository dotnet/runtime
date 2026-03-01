// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Numerics
{
    public readonly struct Decimal128
        : IComparable,
          IComparable<Decimal128>,
          IEquatable<Decimal128>,
          ISpanParsable<Decimal128>,
          IMinMaxValue<Decimal128>,
          IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>
    {
#if BIGENDIAN
        internal readonly ulong _upper;
        internal readonly ulong _lower;
#else
        internal readonly ulong _lower;
        internal readonly ulong _upper;
#endif

        private const int MaxExponent = 6111;
        private const int MinExponent = -6176;
        private const int Precision = 34;
        private const int ExponentBias = 6176;
        private const int NumberBitsExponent = 14;
        private static UInt128 PositiveInfinityValue => new UInt128(upper: 0x7800_0000_0000_0000, lower: 0);
        private static UInt128 NegativeInfinityValue => new UInt128(upper: 0xf800_0000_0000_0000, lower: 0);
        private static UInt128 ZeroValue => new UInt128(0, 0);
        private static UInt128 NegativeZeroValue => new UInt128(0x8000_0000_0000_0000, 0);
        private static UInt128 QuietNaNValue => new UInt128(0x7C00_0000_0000_0000, 0);
        private static UInt128 MaxInternalValue = new UInt128(upper: 0x5FFF_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF);
        private static UInt128 MinInternalValue = new UInt128(upper: 0xDFFF_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF);

        private const ulong NaNMaskUpper = 0x7C00_0000_0000_0000;

        public static Decimal128 PositiveInfinity => new Decimal128(PositiveInfinityValue);
        public static Decimal128 NegativeInfinity => new Decimal128(NegativeInfinityValue);
        public static Decimal128 NaN => new Decimal128(QuietNaNValue);
        public static Decimal128 NegativeZero => new Decimal128(NegativeZeroValue);
        public static Decimal128 Zero => new Decimal128(ZeroValue);
        public static Decimal128 MinValue => new Decimal128(MinInternalValue);
        public static Decimal128 MaxValue => new Decimal128(MaxInternalValue);

        internal Decimal128(UInt128 value)
        {
            _upper = value.Upper;
            _lower = value.Lower;
        }

        public Decimal128(Int128 significand, int exponent)
        {
            UInt128 value = Number.ConstructorToDecimalIeee754Bits<Decimal128, UInt128>(significand < 0, (UInt128)(significand < 0 ? -significand : significand), exponent);
            _upper = value.Upper;
            _lower = value.Lower;
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
            return Number.ParseDecimalIeee754<char, Decimal128, UInt128>(s, style, NumberFormatInfo.GetInstance(provider));
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
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value is not Decimal128 other)
            {
                return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeDecimal128);
            }
            return CompareTo(other);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal128 other)
        {
            var current = new UInt128(_upper, _lower);
            var another = new UInt128(other._upper, other._lower);
            return Number.CompareDecimalIeee754<Decimal128, UInt128>(current, another);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal128 other)
        {
            var current = new UInt128(_upper, _lower);
            var another = new UInt128(other._upper, other._lower);
            return Number.CompareDecimalIeee754<Decimal128, UInt128>(current, another) == 0;
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
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(this, format, NumberFormatInfo.GetInstance(provider));
        }

        private static UInt128[] UInt128Powers10 =>
            [
                new UInt128(0, 1),
                new UInt128(0, 10),
                new UInt128(0, 100),
                new UInt128(0, 1000),
                new UInt128(0, 10000),
                new UInt128(0, 100000),
                new UInt128(0, 1000000),
                new UInt128(0, 10000000),
                new UInt128(0, 100000000),
                new UInt128(0, 1000000000),
                new UInt128(0, 10000000000),
                new UInt128(0, 100000000000),
                new UInt128(0, 1000000000000),
                new UInt128(0, 10000000000000),
                new UInt128(0, 100000000000000),
                new UInt128(0, 1000000000000000),
                new UInt128(0, 10000000000000000),
                new UInt128(0, 100000000000000000),
                new UInt128(0, 1000000000000000000),
                new UInt128(0, 10000000000000000000),
                new UInt128(5, 7766279631452241920),
                new UInt128(54, 3875820019684212736),
                new UInt128(542, 1864712049423024128),
                new UInt128(5421, 200376420520689664),
                new UInt128(54210, 2003764205206896640),
                new UInt128(542101, 1590897978359414784),
                new UInt128(5421010, 15908979783594147840),
                new UInt128(54210108, 11515845246265065472),
                new UInt128(542101086, 4477988020393345024),
                new UInt128(5421010862, 7886392056514347008),
                new UInt128(54210108624, 5076944270305263616),
                new UInt128(542101086242, 13875954555633532928),
                new UInt128(5421010862427, 9632337040368467968),
                new UInt128(54210108624275, 4089650035136921600),
                new UInt128(542101086242752, 4003012203950112768),
            ];

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxScale => 6145;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MinScale => -6175;

        static string IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.ToDecStr(UInt128 significand)
        {
            return Number.UInt128ToDecStr(significand);
        }

        Number.DecodedDecimalIeee754<UInt128> IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Unpack()
        {
            return Number.UnpackDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower));
        }

        static unsafe UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NumberToSignificand(ref Number.NumberBuffer number, int digits)
        {
            if (digits <= 19)
            {
                return Number.DigitsToUInt64(number.DigitsPtr, digits);
            }
            else
            {
                Number.AccumulateDecimalDigitsIntoBigInteger(ref number, 0, (uint)digits, out Number.BigInteger result);
                return result.ToUInt128();
            }
        }

        static Decimal128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Construct(UInt128 value) => new Decimal128(value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.ConvertToExponent(UInt128 value) => (int)value;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Power10(int exponent) => UInt128Powers10[exponent];

        static (UInt128 Quotient, UInt128 Remainder) IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.DivRemPow10(UInt128 value, int exponent)
        {
            UInt128 power = UInt128Powers10[exponent];
            return UInt128.DivRem(value, power);
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.CountDigits(UInt128 significand) => FormattingHelpers.CountDigits(significand);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.BufferLength => Number.Decimal128NumberBufferLength;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MinExponent => MinExponent;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.PositiveInfinity => PositiveInfinityValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NegativeInfinity => NegativeInfinityValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Zero => ZeroValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NegativeZero => NegativeZeroValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NaN => QuietNaNValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MostSignificantBitOfSignificandMask => new UInt128(0x0002_0000_0000_0000, 0);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NumberBitsEncoding => 128;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NumberBitsExponent => NumberBitsExponent;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.SignMask => new UInt128(0x8000_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.G0G1Mask => new UInt128(0x6000_0000_0000_0000, 0);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NumberBitsSignificand => 110;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.G0ToGwPlus1ExponentMask => new UInt128(0x7FFE_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.G2ToGwPlus3ExponentMask => new UInt128(0x1FFF_8000_0000_0000, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.GwPlus2ToGwPlus4SignificandMask => new UInt128(0x0001_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.GwPlus4SignificandMask => new UInt128(0x0000_7FFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxSignificand => new UInt128(upper: 0x0001_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF); // 9_999_999_999_999_999_999_999_999_999_999_999;

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsNaN(UInt128 decimalBits)
        {
            return (decimalBits.Upper & NaNMaskUpper) == NaNMaskUpper;
        }
    }
}
