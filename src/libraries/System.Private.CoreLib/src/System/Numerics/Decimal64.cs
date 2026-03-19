// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Numerics
{
    public readonly struct Decimal64
        : IComparable,
          IComparable<Decimal64>,
          IEquatable<Decimal64>,
          ISpanParsable<Decimal64>,
          IMinMaxValue<Decimal64>,
          IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>
    {
        internal readonly ulong _value;

        private const int MaxExponent = 369;
        private const int MinExponent = -398;
        private const int Precision = 16;
        private const int ExponentBias = 398;
        private const int NumberBitsExponent = 10;
        private const ulong PositiveInfinityValue = 0x7800_0000_0000_0000;
        private const ulong NegativeInfinityValue = 0xF800_0000_0000_0000;
        private const ulong ZeroValue = 0x0000_0000_0000_0000;
        private const ulong NegativeZeroValue = 0x8000_0000_0000_0000;
        private const ulong QuietNaNValue = 0x7C00_0000_0000_0000;
        private const ulong G0G1Mask = 0x6000_0000_0000_0000;
        private const ulong SignMask = 0x8000_0000_0000_0000;
        private const ulong MostSignificantBitOfSignificandMask = 0x0020_0000_0000_0000;
        private const ulong NaNMask = 0x7C00_0000_0000_0000;
        private const ulong MaxSignificand = 9_999_999_999_999_999;
        private const ulong MaxInternalValue = 0x77FB_86F2_6FC0_FFFF; // 9_999_999_999_999_999 x 10^369
        private const ulong MinInternalValue = 0xF7FB_86F2_6FC0_FFFF; // -9_999_999_999_999_999 x 10^369

        public static Decimal64 PositiveInfinity => new Decimal64(PositiveInfinityValue);
        public static Decimal64 NegativeInfinity => new Decimal64(NegativeInfinityValue);
        public static Decimal64 NaN => new Decimal64(QuietNaNValue);
        public static Decimal64 NegativeZero => new Decimal64(NegativeZeroValue);
        public static Decimal64 Zero => new Decimal64(ZeroValue);
        public static Decimal64 MinValue => new Decimal64(MinInternalValue);
        public static Decimal64 MaxValue => new Decimal64(MaxInternalValue);


        private static ReadOnlySpan<ulong> UInt64Powers10 =>
            [
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
                10000000,
                100000000,
                1000000000,
                10000000000,
                100000000000,
                1000000000000,
                10000000000000,
                100000000000000,
                1000000000000000,
            ];

        public Decimal64(long significand, int exponent)
        {
            _value = Number.ConstructorToDecimalIeee754Bits<Decimal64, ulong>(significand < 0, (ulong)Math.Abs(significand), exponent);
        }

        internal Decimal64(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal64 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal64 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <inheritdoc cref="ISpanParsable{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal64 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal64 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal64 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimalIeee754<char, Decimal64, ulong>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal64 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal64 result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal64 result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <inheritdoc cref="ISpanParsable{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out T)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => TryParse(s, NumberStyles.Number, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="string"/> with the given <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => TryParse(s, NumberStyles.Number, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimalIeee754<char, Decimal64, ulong>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = default;
                return false;
            }
            return Number.TryParseDecimalIeee754<char, Decimal64, ulong>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value is not Decimal64 other)
            {
                return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeDecimal64);
            }
            return CompareTo(other);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal64 other)
        {
            return Number.CompareDecimalIeee754<Decimal64, ulong>(_value, other._value);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal64 other)
        {
            return Number.CompareDecimalIeee754<Decimal64, ulong>(_value, other._value) == 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal64 && Equals((Decimal64)obj);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(this, format, NumberFormatInfo.GetInstance(provider));
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.BufferLength => Number.Decimal64NumberBufferLength;

        static string IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.ToDecStr(ulong significand)
        {
            return Number.UInt64ToDecStr(significand);
        }

        Number.DecodedDecimalIeee754<ulong> IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Unpack()
        {
            return Number.UnpackDecimalIeee754<Decimal64, ulong>(_value);
        }

        static unsafe ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NumberToSignificand(ref Number.NumberBuffer number, int digits)
        {
            return Number.DigitsToUInt64(number.DigitsPtr, digits);
        }

        static Decimal64 IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Construct(ulong value) => new Decimal64(value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.ConvertToExponent(ulong value) => (int)value;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Power10(int exponent) => UInt64Powers10[exponent];

        static (ulong Quotient, ulong Remainder) IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.DivRemPow10(ulong value, int exponent)
        {
            ulong power = UInt64Powers10[exponent];
            return Math.DivRem(value, power);
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.CountDigits(ulong significand) => FormattingHelpers.CountDigits(significand);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxScale => 385;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MinScale => -397;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MinExponent => MinExponent;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.PositiveInfinity => PositiveInfinityValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NegativeInfinity => NegativeInfinityValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Zero => ZeroValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NegativeZero => NegativeZeroValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NaN => QuietNaNValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NumberBitsEncoding => 64;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NumberBitsExponent => NumberBitsExponent;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.SignMask => SignMask;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.G0G1Mask => G0G1Mask;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NumberBitsSignificand => 50;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.G0ToGwPlus1ExponentMask => 0x7FE0_0000_0000_0000;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.G2ToGwPlus3ExponentMask => 0x1FF8_0000_0000_0000;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.GwPlus2ToGwPlus4SignificandMask => 0x001F_FFFF_FFFF_FFFF;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.GwPlus4SignificandMask => 0x0007_FFFF_FFFF_FFFF;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxSignificand => MaxSignificand;

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsNaN(ulong decimalBits)
        {
            return (decimalBits & NaNMask) == NaNMask;
        }
    }
}
