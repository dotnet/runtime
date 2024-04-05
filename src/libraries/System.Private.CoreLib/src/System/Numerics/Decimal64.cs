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
          IDecimalIeee754UnpackInfo<Decimal64, long, ulong>
    {
        internal readonly ulong _value;

        private const int MaxDecimalExponent = 369;
        private const int MinDecimalExponent = -398;
        private const int NumberDigitsPrecision = 16;
        private const int Bias = 398;
        private const int NumberBitsExponent = 10;
        private const ulong PositiveInfinityValue = 0x7800000000000000;
        private const ulong NegativeInfinityValue = 0xf800000000000000;
        private const ulong ZeroValue = 0x0000000000000000;
        private const long MaxSignificand = 9_999_999_999_999_999;

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
            return Number.TryParseDecimal64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = new Decimal64(0, 0);
                return false;
            }
            return Number.TryParseDecimal64(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64>.MaxScale => 385;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.CountDigits(long number) => FormattingHelpers.CountDigits((ulong)number);

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Power10(int exponent) => Int64Powers10[exponent];

        static long IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MaxSignificand => MaxSignificand;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MaxDecimalExponent => MaxDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MinDecimalExponent => MinDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Bias => Bias;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.MostSignificantBitNumberOfSignificand => 53;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsEncoding => 64;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsCombinationField => 13;

        static int IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NumberBitsExponent => NumberBitsExponent;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.PositiveInfinityBits => PositiveInfinityValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.NegativeInfinityBits => NegativeInfinityValue;

        static ulong IDecimalIeee754ConstructorInfo<Decimal64, long, ulong>.Zero => ZeroValue;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.ConvertToExponent(ulong value) => (int)value;

        static long IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.ConvertToSignificand(ulong value) => (long)value;
        static long IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.Power10(int exponent) => Int64Powers10[exponent];

        static ulong IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.SignMask => 0x8000_0000_0000_0000;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.NumberBitsEncoding => 64;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.NumberBitsExponent => NumberBitsExponent;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.Bias => Bias;

        static long IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.TwoPowerMostSignificantBitNumberOfSignificand => 9_007_199_254_740_992;

        static int IDecimalIeee754UnpackInfo<Decimal64, long, ulong>.NumberDigitsPrecision => NumberDigitsPrecision;
    }
}
