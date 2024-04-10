// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Numerics
{
    public readonly struct Decimal64
        : IComparable,
          IEquatable<Decimal64>,
          IDecimalIeee754ParseAndFormatInfo<Decimal64>,
          IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>,
          IDecimalIeee754UnpackInfo<Decimal64, long, ulong>,
          IDecimalIeee754TryParseInfo<Decimal64, long>
    {
        internal readonly ulong _value;

        private const int MaxDecimalExponent = 369;
        private const int MinDecimalExponent = -398;
        private const int NumberDigitsPrecision = 16;
        private const int Bias = 398;
        private const int NumberBitsExponent = 10;
        private const ulong PositiveInfinityValue = 0x7800_0000_0000_0000;
        private const ulong NegativeInfinityValue = 0xF800_0000_0000_0000;
        private const ulong ZeroValue = 0x0000_0000_0000_0000;
        private const long MaxSignificand = 9_999_999_999_999_999;
        private const ulong G0G1Mask = 0x6000_0000_0000_0000;
        private const ulong SignMask = 0x8000_0000_0000_0000;
        private const ulong MostSignificantBitOfSignificandMask = 0x0020_0000_0000_0000;

        public Decimal64(long significand, int exponent)
        {
            _value = Number.CalDecimalIeee754<Decimal64, long, ulong>(significand, exponent);
        }

        internal Decimal64(ulong value)
        {
            _value = value;
        }

        public static Decimal64 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        public static Decimal64 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static Decimal64 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal64 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal64 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal64(s, style, NumberFormatInfo.GetInstance(provider));
        }
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

        public static Decimal64 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal64 result) => TryParse(s, NumberStyles.Number, provider: null, out result);
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal64 result) => TryParse(s, NumberStyles.Number, provider: null, out result);
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => TryParse(s, NumberStyles.Number, provider, out result);
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => TryParse(s, NumberStyles.Number, provider, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimalIeee754<Decimal64, long, char>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }
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

        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is not Decimal64 i)
            {
                throw new ArgumentException(SR.Arg_MustBeDecimal32);
            }

            return Number.CompareDecimalIeee754<Decimal64, long, ulong>(_value, i._value);
        }

        public bool Equals(Decimal64 other)
        {
            return Number.CompareDecimalIeee754<Decimal64, long, ulong>(_value, other._value) == 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal64 && Equals((Decimal64)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return Number.FormatDecimal64(this, null, NumberFormatInfo.CurrentInfo);
        }
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimal64(this, format, NumberFormatInfo.CurrentInfo);
        }
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimal64(this, null, NumberFormatInfo.GetInstance(provider));
        }
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimal64(this, format, NumberFormatInfo.GetInstance(provider));
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64>.MaxScale => 385;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.CountDigits(long number) => FormattingHelpers.CountDigits((ulong)number);

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Power10(int exponent) => Int64Powers10[exponent];

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MaxSignificand => MaxSignificand;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MaxDecimalExponent => MaxDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MinDecimalExponent => MinDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Bias => Bias;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsEncoding => 64;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsCombinationField => 13;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsExponent => NumberBitsExponent;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.PositiveInfinityBits => PositiveInfinityValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NegativeInfinityBits => NegativeInfinityValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Zero => ZeroValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.G0G1Mask => G0G1Mask;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.SignMask => SignMask;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.ConvertToExponent(ulong value) => (int)value;

        static long IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.ConvertToSignificand(ulong value) => (long)value;

        static long IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.Power10(int exponent) => Int64Powers10[exponent];

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.SignMask => SignMask;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.Bias => Bias;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.NumberDigitsPrecision => NumberDigitsPrecision;

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.G0G1Mask => G0G1Mask;

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.G0ToGwPlus1ExponentMask => 0x7FE0_0000_0000_0000;

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.G2ToGwPlus3ExponentMask => 0x1FF8_0000_0000_0000;

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.GwPlus2ToGwPlus4SignificandMask => 0x001F_FFFF_FFFF_FFFF;

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.GwPlus4SignificandMask => 0x0007_FFFF_FFFF_FFFF;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.NumberBitsSignificand => 50;

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static int IDecimalIeee754TryParseInfo<Decimal64, long>.DecimalNumberBufferLength => Number.Decimal64NumberBufferLength;

        static bool IDecimalIeee754TryParseInfo<Decimal64, long>.TryNumberToDecimalIeee754(ref Number.NumberBuffer number, out long significand, out int exponent)
             => Number.TryNumberToDecimalIeee754<Decimal64, long>(ref number, out significand, out exponent);

        static Decimal64 IDecimalIeee754TryParseInfo<Decimal64, long>.Construct(long significand, int exponent) => new Decimal64(significand, exponent);
    }
}
