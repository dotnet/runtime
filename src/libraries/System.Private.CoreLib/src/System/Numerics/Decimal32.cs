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
          IEquatable<Decimal32>,
          IDecimalIeee754ParseAndFormatInfo<Decimal32>,
          IDecimalIeee754ConstructorInfo<Decimal32, int, uint>,
          IDecimalIeee754UnpackInfo<Decimal32, int, uint>,
          IDecimalIeee754TryParseInfo<Decimal32, int>
    {
        internal readonly uint _value;

        private const int MaxDecimalExponent = 90;
        private const int MinDecimalExponent = -101;
        private const int NumberDigitsPrecision = 7;
        private const int Bias = 101;
        private const int NumberBitsExponent = 8;
        private const uint PositiveInfinityValue = 0x7800_0000;
        private const uint NegativeInfinityValue = 0xF800_0000;
        private const uint ZeroValue = 0x00000000;
        private const int MaxSignificand = 9_999_999;
        private const uint G0G1Mask = 0x6000_0000;
        private const uint SignMask = 0x8000_0000;
        private const uint MostSignificantBitOfSignificandMask = 0x0080_0000;

        public Decimal32(int significand, int exponent)
        {
            _value = Number.CalDecimalIeee754<Decimal32, int, uint>(significand, exponent);
        }

        public static Decimal32 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        public static Decimal32 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static Decimal32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal32 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal32 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static Decimal32 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal32 result) => TryParse(s, NumberStyles.Number, provider: null, out result);
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal32 result) => TryParse(s, NumberStyles.Number, provider: null, out result);
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, NumberStyles.Number, provider, out result);
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, NumberStyles.Number, provider, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimalIeee754<Decimal32, int, char>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = default;
                return false;
            }
            return Number.TryParseDecimalIeee754<Decimal32, int, char>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        private static ReadOnlySpan<int> Int32Powers10 =>
            [
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
            ];

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

            return Number.CompareDecimalIeee754<Decimal32, int, uint>(_value, i._value);
        }

        public bool Equals(Decimal32 other)
        {
            return Number.CompareDecimalIeee754<Decimal32, int, uint>(_value, other._value) == 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal32 && Equals((Decimal32)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return Number.FormatDecimal32(this, null, NumberFormatInfo.CurrentInfo);
        }
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimal32(this, format, NumberFormatInfo.CurrentInfo);
        }
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimal32(this, null, NumberFormatInfo.GetInstance(provider));
        }
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimal32(this, format, NumberFormatInfo.GetInstance(provider));
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32>.MaxScale => 97;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.CountDigits(int number) => FormattingHelpers.CountDigits((uint)number);
        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.Power10(int exponent) => Int32Powers10[exponent];

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MaxSignificand => MaxSignificand;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MaxDecimalExponent => MaxDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MinDecimalExponent => MinDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.Bias => Bias;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberBitsEncoding => 32;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberBitsCombinationField => 11;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.PositiveInfinityBits => PositiveInfinityValue;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NegativeInfinityBits => NegativeInfinityValue;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberBitsExponent => NumberBitsExponent;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.Zero => ZeroValue;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.G0G1Mask => G0G1Mask;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.SignMask => SignMask;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.SignMask => SignMask;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.Bias => Bias;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.ConvertToExponent(uint value) => (int)value;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.ConvertToSignificand(uint value) => (int)value;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.Power10(int exponent) => Int32Powers10[exponent];

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.G0G1Mask => G0G1Mask;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.G0ToGwPlus1ExponentMask => 0x7F80_0000;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.G2ToGwPlus3ExponentMask => 0x1FE0_0000;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.GwPlus2ToGwPlus4SignificandMask => 0x007F_FFFF;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.GwPlus4SignificandMask => 0x001F_FFFF;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.NumberBitsSignificand => 20;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static int IDecimalIeee754TryParseInfo<Decimal32, int>.DecimalNumberBufferLength => Number.Decimal32NumberBufferLength;

        static bool IDecimalIeee754TryParseInfo<Decimal32, int>.TryNumberToDecimalIeee754(ref Number.NumberBuffer number, out int significand, out int exponent)
            => Number.TryNumberToDecimalIeee754<Decimal32, int>(ref number, out significand, out exponent);

        static Decimal32 IDecimalIeee754TryParseInfo<Decimal32, int>.Construct(int significand, int exponent) => new Decimal32(significand, exponent);
    }
}
