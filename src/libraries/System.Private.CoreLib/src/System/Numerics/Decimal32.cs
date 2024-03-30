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
          IDecimalIeee754UnpackInfo<Decimal32, int, uint>
    {
        internal readonly uint _value;

        private const int MaxDecimalExponent = 90;
        private const int MinDecimalExponent = -101;
        private const int NumberDigitsPrecision = 7;
        private const int Bias = 101;
        private const int NumberBitsExponent = 8;
        private const uint PositiveInfinityValue = 0x78000000;
        private const uint NegativeInfinityValue = 0xf8000000;
        private const uint ZeroValue = 0x00000000;
        private const int MaxSignificand = 9_999_999;
        private static readonly Decimal32 PositiveInfinity = new Decimal32(PositiveInfinityValue);
        private static readonly Decimal32 NegativeInfinity = new Decimal32(NegativeInfinityValue);

        public Decimal32(int significand, int exponent)
        {
            _value = Number.CalDecimalIeee754<Decimal32, int, uint>(significand, exponent);
        }

        internal Decimal32(uint value)
        {
            _value = value;
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

            return CoreCompareTo(i);
        }

        private int CoreCompareTo(Decimal32 obj)
        {
            if (obj._value == _value)
            {
                return 0;
            }

            Number.DecimalIeee754<int> current = Number.UnpackDecimalIeee754<Decimal32, int, uint>(_value);
            Number.DecimalIeee754<int> other = Number.UnpackDecimalIeee754<Decimal32, int, uint>(obj._value);

            if (current.Signed && !other.Signed) return -1;

            if (!current.Signed && other.Signed) return 1;

            if (current.Exponent > other.Exponent)
            {
                return current.Signed ? -InternalUnsignedCompare(current, other) : InternalUnsignedCompare(current, other);
            }

            if (current.Exponent < other.Exponent)
            {
                return current.Signed ? InternalUnsignedCompare(other, current) : -InternalUnsignedCompare(current, other);
            }

            if (current.Significand == other.Significand) return 0;

            if (current.Significand > other.Significand)
            {
                return current.Signed ? -1 : 1;
            }
            else
            {
                return current.Signed ? 1 : -1;
            }

            static int InternalUnsignedCompare(Number.DecimalIeee754<int> biggerExp, Number.DecimalIeee754<int> smallerExp)
            {
                if (biggerExp.Significand >= smallerExp.Significand) return 1;

                int diffExponent = biggerExp.Exponent - smallerExp.Exponent;
                if (diffExponent < NumberDigitsPrecision)
                {
                    int factor = Int32Powers10[diffExponent];
                    int quotient = smallerExp.Significand / biggerExp.Significand;
                    int remainder = smallerExp.Significand % biggerExp.Significand;

                    if (quotient < factor) return 1;
                    if (quotient > factor) return -1;
                    if (remainder > 0) return -1;
                    return 0;
                }

                return 1;
            }
        }

        public bool Equals(Decimal32 other)
        {
            return CoreCompareTo(other) == 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal32 && Equals((Decimal32)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.CountDigits(int number) => FormattingHelpers.CountDigits((uint)number);
        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.Power10(int exponent) => Int32Powers10[exponent];

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MaxSignificand => MaxSignificand;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MaxDecimalExponent => MaxDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MinDecimalExponent => MinDecimalExponent;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberDigitsPrecision => NumberDigitsPrecision;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.Bias => Bias;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.MostSignificantBitNumberOfSignificand => 23;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberBitsEncoding => 32;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberBitsCombinationField => 11;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.PositiveInfinityBits => PositiveInfinityValue;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NegativeInfinityBits => NegativeInfinityValue;

        static uint IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.Zero => ZeroValue;

        static uint IDecimalIeee754UnpackInfo<Decimal32, int, uint>.SignMask => 0x8000_0000;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.NumberBitsEncoding => 32;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.NumberBitsExponent => NumberBitsExponent;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.Bias => Bias;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.TwoPowerMostSignificantBitNumberOfSignificand => 8388608;

        static int IDecimalIeee754ConstructorInfo<Decimal32, int, uint>.NumberBitsExponent => NumberBitsExponent;

        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.ConvertToExponent(uint value) => (int)value;
        static int IDecimalIeee754UnpackInfo<Decimal32, int, uint>.ConvertToSignificand(uint value) => (int)value;
    }
}
