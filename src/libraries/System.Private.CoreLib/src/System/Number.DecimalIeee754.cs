// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using static System.Number;

namespace System
{
    internal interface IDecimalIeee754ConstructorInfo<TSelf, TSignificand, TValue>
        where TSelf : unmanaged, IDecimalIeee754ConstructorInfo<TSelf, TSignificand, TValue>
        where TSignificand : IBinaryInteger<TSignificand>
        where TValue : IBinaryInteger<TValue>
    {
        static abstract TSignificand MaxSignificand { get; }
        static abstract int MaxDecimalExponent { get; }
        static abstract int MinDecimalExponent { get; }
        static abstract int NumberDigitsPrecision { get; }
        static abstract int Bias { get; }
        static abstract int CountDigits(TSignificand number);
        static abstract TSignificand Power10(int exponent);
        static abstract int MostSignificantBitNumberOfSignificand { get; }
        static abstract int NumberBitsEncoding { get; }
        static abstract int NumberBitsCombinationField { get; }
        static abstract int NumberBitsExponent { get; }
        static abstract TValue PositiveInfinityBits { get; }
        static abstract TValue NegativeInfinityBits { get; }
        static abstract TValue Zero { get; }
    }

    internal interface IDecimalIeee754UnpackInfo<TSelf, TSignificand, TValue>
        where TSelf : unmanaged, IDecimalIeee754UnpackInfo<TSelf, TSignificand, TValue>
        where TSignificand : IBinaryInteger<TSignificand>
        where TValue : IBinaryInteger<TValue>
    {
        static abstract TValue SignMask { get; }
        static abstract int NumberBitsEncoding { get; }
        static abstract int NumberBitsExponent { get; }
        static abstract int Bias { get; }
        static abstract TSignificand TwoPowerMostSignificantBitNumberOfSignificand { get; }
        static abstract int ConvertToExponent(TValue value);
        static abstract TSignificand ConvertToSignificand(TValue value);
    }

    internal static partial class Number
    {
        internal static TValue CalDecimalIeee754<TDecimal, TSignificand, TValue>(TSignificand significand, int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ConstructorInfo<TDecimal, TSignificand, TValue>
            where TSignificand : IBinaryInteger<TSignificand>
            where TValue : IBinaryInteger<TValue>
        {
            if (significand == TSignificand.Zero)
            {
                return TValue.Zero;
            }

            TSignificand unsignedSignificand = significand > TSignificand.Zero ? significand : -significand;

            if (unsignedSignificand > TDecimal.MaxSignificand && exponent > TDecimal.MaxDecimalExponent)
            {
                return significand > TSignificand.Zero ? TDecimal.PositiveInfinityBits : TDecimal.NegativeInfinityBits;
            }

            TSignificand ten = TSignificand.CreateTruncating(10);
            if (exponent < TDecimal.MinDecimalExponent)
            {
                while (unsignedSignificand >= ten)
                {
                    unsignedSignificand /= ten;
                    ++exponent;
                }
                if (exponent < TDecimal.MinDecimalExponent)
                {
                    throw new OverflowException(SR.Overflow_Decimal);
                }
            }

            if (unsignedSignificand > TDecimal.MaxSignificand)
            {
                int numberDigitsRemoving = TDecimal.CountDigits(unsignedSignificand) - TDecimal.NumberDigitsPrecision;

                if (exponent + numberDigitsRemoving > TDecimal.MaxDecimalExponent)
                {
                    throw new OverflowException(SR.Overflow_Decimal);
                }

                exponent += numberDigitsRemoving;
                TSignificand two = TSignificand.CreateTruncating(2);
                TSignificand divisor = TDecimal.Power10(numberDigitsRemoving);
                TSignificand quotient = unsignedSignificand / divisor;
                TSignificand remainder = unsignedSignificand % divisor;
                TSignificand midPoint = divisor / two;
                bool needRouding = remainder > midPoint || (remainder == midPoint && quotient % two == TSignificand.One);

                if (needRouding && quotient == TDecimal.MaxSignificand && exponent < TDecimal.MaxDecimalExponent)
                {
                    unsignedSignificand = TDecimal.Power10(TDecimal.NumberDigitsPrecision - 1);
                    exponent++;
                }
                else if (needRouding && quotient < TDecimal.MaxSignificand)
                {
                    unsignedSignificand = quotient + TSignificand.One;
                }
                else
                {
                    unsignedSignificand = quotient;
                }
            }
            else if (exponent > TDecimal.MaxDecimalExponent)
            {
                int numberZeroDigits = exponent - TDecimal.MaxDecimalExponent;
                int numberSignificandDigits = TDecimal.CountDigits(unsignedSignificand);

                if (numberSignificandDigits + numberZeroDigits > TDecimal.NumberDigitsPrecision)
                {
                    throw new OverflowException(SR.Overflow_Decimal);
                }
                unsignedSignificand *= TDecimal.Power10(numberZeroDigits);
            }

            exponent += TDecimal.Bias;
            bool msbSignificand = (unsignedSignificand & TSignificand.One << TDecimal.MostSignificantBitNumberOfSignificand) != TSignificand.Zero;

            TValue value = TValue.Zero;
            TValue exponentVal = TValue.CreateTruncating(exponent);
            TValue significandVal = TValue.CreateTruncating(unsignedSignificand);

            if (significand < TSignificand.Zero)
            {
                value = TValue.One << TDecimal.NumberBitsEncoding - 1;
            }

            if (msbSignificand)
            {
                value ^= TValue.One << TDecimal.NumberBitsEncoding - 2;
                value ^= TValue.One << TDecimal.NumberBitsEncoding - 3;
                exponentVal <<= TDecimal.NumberBitsEncoding - 4;
                value ^= exponentVal;
                significandVal <<= TDecimal.NumberBitsEncoding - TDecimal.MostSignificantBitNumberOfSignificand;
                significandVal >>= TDecimal.NumberBitsCombinationField;
                value ^= significandVal;
            }
            else
            {
                exponentVal <<= TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent - 1;
                value ^= exponentVal;
                value ^= significandVal;
            }

            return value;
        }

        internal struct DecimalIeee754<TSignificand>
            where TSignificand : IBinaryInteger<TSignificand>
        {
            public bool Signed { get; }
            public int Exponent { get; }
            public TSignificand Significand { get; }

            public DecimalIeee754(bool signed, int exponent, TSignificand significand)
            {
                Signed = signed;
                Exponent = exponent;
                Significand = significand;
            }
        }

        internal static DecimalIeee754<TSignificand> UnpackDecimalIeee754<TDecimal, TSignificand, TValue>(TValue value)
            where TDecimal : unmanaged, IDecimalIeee754UnpackInfo<TDecimal, TSignificand, TValue>
            where TSignificand : IBinaryInteger<TSignificand>
            where TValue : IBinaryInteger<TValue>
        {
            bool signed = (value & TDecimal.SignMask) != TValue.Zero;
            TValue g0g1Bits = (value << 1) >> TDecimal.NumberBitsEncoding - 2;
            TSignificand significand;
            int exponent;

            if (g0g1Bits == TValue.CreateTruncating(3))
            {
                exponent = TDecimal.ConvertToExponent((value << 3) >> TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent);
                significand = TDecimal.ConvertToSignificand((value << TDecimal.NumberBitsEncoding + 3) >> TDecimal.NumberBitsEncoding + 3);
                significand += TDecimal.TwoPowerMostSignificantBitNumberOfSignificand;
            }
            else
            {
                exponent = TDecimal.ConvertToExponent((value << 1) >> TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent);
                significand = TDecimal.ConvertToSignificand((value << TDecimal.NumberBitsExponent + 1) >> TDecimal.NumberBitsExponent + 1);
            }

            return new DecimalIeee754<TSignificand>(signed, exponent - TDecimal.Bias, significand);
        }
    }
}
