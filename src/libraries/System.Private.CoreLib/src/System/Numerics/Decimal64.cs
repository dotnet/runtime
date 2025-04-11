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
          IDecimalIeee754ParseAndFormatInfo<Decimal64, long, ulong>,
          IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>,
          IDecimalIeee754TryParseInfo<Decimal64, long>
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
        private const long MaxSignificand = 9_999_999_999_999_999;
        private const ulong G0G1Mask = 0x6000_0000_0000_0000;
        private const ulong SignMask = 0x8000_0000_0000_0000;
        private const ulong MostSignificantBitOfSignificandMask = 0x0020_0000_0000_0000;
        private static ReadOnlySpan<long> Int64Powers10 =>
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
            _value = Number.CalculateDecimalIeee754<Decimal64, long, ulong>(significand, exponent);
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
            return Number.ParseDecimal64(s, style, NumberFormatInfo.GetInstance(provider));
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
            return Number.TryParseDecimalIeee754<Decimal64, long, char>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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
            return Number.TryParseDecimalIeee754<Decimal64, long, char>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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
            return Number.CompareDecimalIeee754<Decimal64, long, ulong>(_value, other._value);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal64 other)
        {
            return Number.CompareDecimalIeee754<Decimal64, long, ulong>(_value, other._value) == 0;
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
            return Number.FormatDecimalIeee754<Decimal64, long, ulong>(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal64, long, ulong>(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal64, long, ulong>(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal64, long, ulong>(this, format, NumberFormatInfo.GetInstance(provider));
        }


        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.CountDigits(long number) => FormattingHelpers.CountDigits((ulong)number);

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Power10(int exponent) => Int64Powers10[exponent];

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MaxSignificand => MaxSignificand;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MinExponent => MinExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Precision => Precision;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsEncoding => 64;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsCombinationField => 13;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsExponent => NumberBitsExponent;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.PositiveInfinityBits => PositiveInfinityValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NegativeInfinityBits => NegativeInfinityValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Zero => ZeroValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.G0G1Mask => G0G1Mask;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.SignMask => SignMask;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.ConvertToExponent(ulong value) => (int)value;

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.ConvertToSignificand(ulong value) => (long)value;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.G0ToGwPlus1ExponentMask => 0x7FE0_0000_0000_0000;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.G2ToGwPlus3ExponentMask => 0x1FF8_0000_0000_0000;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.GwPlus2ToGwPlus4SignificandMask => 0x001F_FFFF_FFFF_FFFF;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.GwPlus4SignificandMask => 0x0007_FFFF_FFFF_FFFF;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsSignificand => 50;

        static int IDecimalIeee754TryParseInfo<Decimal64, long>.DecimalNumberBufferLength => Number.Decimal64NumberBufferLength;

        static bool IDecimalIeee754TryParseInfo<Decimal64, long>.TryNumberToDecimalIeee754(ref Number.NumberBuffer number, out long significand, out int exponent)
             => Number.TryNumberToDecimalIeee754<Decimal64, long, ulong>(ref number, out significand, out exponent);

        static Decimal64 IDecimalIeee754TryParseInfo<Decimal64, long>.Construct(long significand, int exponent) => new Decimal64(significand, exponent);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, long, ulong>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, long, ulong>.BufferLength => Number.Decimal64NumberBufferLength;

        static unsafe byte* IDecimalIeee754ParseAndFormatInfo<Decimal64, long, ulong>.ToDecChars(byte* p, long significand)
        {
            return Number.UInt64ToDecChars(p, (ulong)significand, 0);
        }

        Number.DecimalIeee754<long> IDecimalIeee754ParseAndFormatInfo<Decimal64, long, ulong>.Unpack()
        {
            return Number.UnpackDecimalIeee754<Decimal64, long, ulong>(_value);
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, long, ulong>.MaxScale => 385;
    }
}
