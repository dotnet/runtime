// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Decimal32
        : IComparable,
          IComparable<Decimal32>,
          IEquatable<Decimal32>,
          ISpanParsable<Decimal32>,
          IMinMaxValue<Decimal32>,
          IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>
    {
        internal readonly uint _value;

        internal Decimal32(uint value)
        {
            _value = value;
        }

        private const int MaxExponent = 90;
        private const int MinExponent = -101;
        private const int Precision = 7;
        private const int ExponentBias = 101;
        private const int NumberBitsExponent = 8;
        private const uint PositiveInfinityValue = 0x7800_0000;
        private const uint NegativeInfinityValue = 0xF800_0000;
        private const uint ZeroValue = 0x0000_0000;
        private const uint NegativeZeroValue = 0x8000_0000;
        private const uint QuietNaNValue = 0x7C00_0000;
        private const uint G0G1Mask = 0x6000_0000;
        private const uint SignMask = 0x8000_0000;
        private const uint MostSignificantBitOfSignificandMask = 0x0080_0000;
        private const uint NaNMask = 0x7C00_0000;
        private const uint MaxSignificand = 9_999_999;
        private const uint MaxInternalValue = 0x77F8_967F; // 9,999,999 x 10^90
        private const uint MinInternalValue = 0xF7F8_967F; // -9,999,999 x 10^90

        public static Decimal32 PositiveInfinity => new Decimal32(PositiveInfinityValue);
        public static Decimal32 NegativeInfinity => new Decimal32(NegativeInfinityValue);
        public static Decimal32 NaN => new Decimal32(QuietNaNValue);
        public static Decimal32 NegativeZero => new Decimal32(NegativeZeroValue);
        public static Decimal32 Zero => new Decimal32(ZeroValue);
        public static Decimal32 MinValue => new Decimal32(MinInternalValue);
        public static Decimal32 MaxValue => new Decimal32(MaxInternalValue);

        private static ReadOnlySpan<uint> UInt32Powers10 =>
            [
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
            ];

        public Decimal32(int significand, int exponent)
        {
            _value = Number.ConstructorToDecimalIeee754Bits<Decimal32, uint>(significand < 0, (uint)Math.Abs(significand), exponent);
        }

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal32 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal32 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <inheritdoc cref="ISpanParsable{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal32 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal32 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimalIeee754<char, Decimal32, uint>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. </returns>
        public static Decimal32 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal32 result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal32 result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <inheritdoc cref="ISpanParsable{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out T)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, NumberStyles.Number, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> with the given <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, NumberStyles.Number, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinityValue"/> or <see cref="NegativeInfinityValue"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = default;
                return false;
            }
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is not Decimal32 i)
            {
                throw new ArgumentException(SR.Arg_MustBeDecimal32);
            }

            return Number.CompareDecimalIeee754<Decimal32, uint>(_value, i._value);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal32 other)
        {
            return Number.CompareDecimalIeee754<Decimal32, uint>(_value, other._value);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal32 other)
        {
            return Number.CompareDecimalIeee754<Decimal32, uint>(_value, other._value) == 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal32 && Equals((Decimal32)obj);
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
            return Number.FormatDecimalIeee754<Decimal32, uint>(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(this, format, NumberFormatInfo.GetInstance(provider));
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.BufferLength => Number.Decimal32NumberBufferLength;

        static string IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.ToDecStr(uint significand)
        {
            return Number.UInt32ToDecStr(significand);
        }

        Number.DecodedDecimalIeee754<uint> IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Unpack()
        {
            return Number.UnpackDecimalIeee754<Decimal32, uint>(_value);
        }

        static unsafe uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NumberToSignificand(ref Number.NumberBuffer number, int digits)
        {
            return Number.DigitsToUInt32(number.DigitsPtr, digits);
        }

        static Decimal32 IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Construct(uint value) => new Decimal32(value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.ConvertToExponent(uint value) => (int)value;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Power10(int exponent) => UInt32Powers10[exponent];

        static (uint Quotient, uint Remainder) IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.DivRemPow10(uint value, int exponent)
        {
            uint power = UInt32Powers10[exponent];
            return Math.DivRem(value, power);
        }
        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.CountDigits(uint significand) => FormattingHelpers.CountDigits(significand);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxScale => 97;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MinScale => -100;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MinExponent => MinExponent;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.PositiveInfinity => PositiveInfinityValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NegativeInfinity => NegativeInfinityValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Zero => ZeroValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NegativeZero => NegativeZeroValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NaN => QuietNaNValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NumberBitsEncoding => 32;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NumberBitsExponent => NumberBitsExponent;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.SignMask => SignMask;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.G0G1Mask => G0G1Mask;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NumberBitsSignificand => 20;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.G0ToGwPlus1ExponentMask => 0x7F80_0000;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.G2ToGwPlus3ExponentMask => 0x1FE0_0000;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.GwPlus2ToGwPlus4SignificandMask => 0x007F_FFFF;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.GwPlus4SignificandMask => 0x001F_FFFF;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxSignificand => MaxSignificand;

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsNaN(uint decimalBits)
        {
            return (decimalBits & NaNMask) == NaNMask;
        }
    }
}
