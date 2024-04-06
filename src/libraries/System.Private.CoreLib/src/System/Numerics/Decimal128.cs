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
          IEquatable<Decimal128>,
          IDecimalIeee754ParseAndFormatInfo<Decimal128>,
          IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>,
          IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>
    {
        internal readonly UInt128 _value;

        private const int MaxDecimalExponent = 6111;
        private const int MinDecimalExponent = -6176;
        private const int NumberDigitsPrecision = 34;
        private const int Bias = 6176;
        private const int NumberBitsExponent = 14;
        private static readonly UInt128 PositiveInfinityValue = new UInt128(upper: 0x7800000000000000, lower: 0);
        private static readonly UInt128 NegativeInfinityValue = new UInt128(upper: 0xf800000000000000, lower: 0);
        private static readonly UInt128 ZeroValue = new UInt128(0, 0);
        private static readonly Int128 MaxSignificand = new Int128(upper: 542101086242752, lower: 4003012203950112767); // 9_999_999_999_999_999_999_999_999_999_999_999;

        public Decimal128(Int128 significand, int exponent)
        {
            _value = Number.CalDecimalIeee754<Decimal128, Int128, UInt128>(significand, exponent);
        }
        public static Decimal128 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        public static Decimal128 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static Decimal128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal128 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal128(s, style, NumberFormatInfo.GetInstance(provider));
        }
        public static Decimal128 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal128 result) => TryParse(s, NumberStyles.Number, provider: null, out result);
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal128 result) => TryParse(s, NumberStyles.Number, provider: null, out result);
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => TryParse(s, NumberStyles.Number, provider, out result);
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => TryParse(s, NumberStyles.Number, provider, out result);
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimal128(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = new Decimal128(0, 0);
                return false;
            }
            return Number.TryParseDecimal128(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

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

            return Number.CompareDecimalIeee754<Decimal128, Int128, UInt128>(_value, i._value);
        }

        public bool Equals(Decimal128 other)
        {
            return Number.CompareDecimalIeee754<Decimal128, Int128, UInt128>(_value, other._value) == 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal128 && Equals((Decimal128)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
        static int IDecimalIeee754ParseAndFormatInfo<Decimal128>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128>.MaxScale => 6145;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.CountDigits(Int128 number) => FormattingHelpers.CountDigits((UInt128)number);

        static Int128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.Power10(int exponent) => Int128Powers10[exponent];

        static Int128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MaxSignificand => MaxSignificand;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MaxDecimalExponent => MaxDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MinDecimalExponent => MinDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.Bias => Bias;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.MostSignificantBitNumberOfSignificand => 113;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsEncoding => 128;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsCombinationField => 17;

        static int IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NumberBitsExponent => NumberBitsExponent;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.PositiveInfinityBits => PositiveInfinityValue;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.NegativeInfinityBits => NegativeInfinityValue;

        static UInt128 IDecimalIeee754ConstructorInfo<Decimal128, Int128, UInt128>.Zero => ZeroValue;

        static int IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.ConvertToExponent(UInt128 value) => (int)value;

        static Int128 IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.ConvertToSignificand(UInt128 value) => (Int128)value;

        static Int128 IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.Power10(int exponent) => Int128Powers10[exponent];

        static UInt128 IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.SignMask => new UInt128(0x8000_0000_0000_0000, 0);

        static int IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.NumberBitsEncoding => 128;

        static int IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.NumberBitsExponent => NumberBitsExponent;

        static int IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.Bias => Bias;

        static Int128 IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.TwoPowerMostSignificantBitNumberOfSignificand => new Int128(0x0002_0000_0000_0000, 0);

        static int IDecimalIeee754UnpackInfo<Decimal128, Int128, UInt128>.NumberDigitsPrecision => NumberDigitsPrecision;


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
    }
}
