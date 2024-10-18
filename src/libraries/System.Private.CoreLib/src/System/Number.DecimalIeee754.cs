// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

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
        static abstract int NumberBitsEncoding { get; }
        static abstract TValue G0G1Mask { get; }
        static abstract TValue MostSignificantBitOfSignificandMask { get; }
        static abstract TValue SignMask { get; }
        static abstract int NumberBitsCombinationField { get; }
        static abstract int NumberBitsExponent { get; }
        static abstract TValue PositiveInfinityBits { get; }
        static abstract TValue NegativeInfinityBits { get; }
        static abstract TValue Zero { get; }
        static abstract string OverflowMessage { get; }
    }

    internal interface IDecimalIeee754UnpackInfo<TSelf, TSignificand, TValue>
        where TSelf : unmanaged, IDecimalIeee754UnpackInfo<TSelf, TSignificand, TValue>
        where TSignificand : IBinaryInteger<TSignificand>
        where TValue : IBinaryInteger<TValue>
    {
        static abstract TValue SignMask { get; }
        static abstract TValue G0G1Mask { get; }
        static abstract TValue G0ToGwPlus1ExponentMask { get; } //G0 to G(w+1)
        static abstract TValue G2ToGwPlus3ExponentMask { get; } //G2 to G(w+3)
        static abstract TValue GwPlus2ToGwPlus4SignificandMask { get; } //G(w+2) to G(w+4)
        static abstract TValue GwPlus4SignificandMask { get; } //G(w+4)
        static abstract int NumberBitsSignificand { get; }
        static abstract int NumberDigitsPrecision { get; }
        static abstract int Bias { get; }
        static abstract TValue MostSignificantBitOfSignificandMask { get; }
        static abstract int ConvertToExponent(TValue value);
        static abstract TSignificand ConvertToSignificand(TValue value);
        static abstract TSignificand Power10(int exponent);
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

            if ((unsignedSignificand > TDecimal.MaxSignificand && exponent >= TDecimal.MaxDecimalExponent)
                || (unsignedSignificand == TDecimal.MaxSignificand && exponent > TDecimal.MaxDecimalExponent))
            {
                return significand > TSignificand.Zero ? TDecimal.PositiveInfinityBits : TDecimal.NegativeInfinityBits;
            }

            TSignificand ten = TSignificand.CreateTruncating(10);
            if (exponent < TDecimal.MinDecimalExponent)
            {
                while (unsignedSignificand > TSignificand.Zero && exponent < TDecimal.MinDecimalExponent)
                {
                    unsignedSignificand /= ten;
                    ++exponent;
                }
                if (unsignedSignificand == TSignificand.Zero)
                {
                    return TDecimal.Zero;
                }
            }

            if (unsignedSignificand > TDecimal.MaxSignificand)
            {
                int numberDigitsRemoving = TDecimal.CountDigits(unsignedSignificand) - TDecimal.NumberDigitsPrecision;

                if (exponent + numberDigitsRemoving > TDecimal.MaxDecimalExponent)
                {
                    throw new OverflowException(TDecimal.OverflowMessage);
                }

                exponent += numberDigitsRemoving;
                TSignificand divisor = TDecimal.Power10(numberDigitsRemoving);
                TSignificand quotient = unsignedSignificand / divisor;
                TSignificand remainder = unsignedSignificand % divisor;
                TSignificand midPoint = divisor >> 1;
                bool needRouding = remainder > midPoint || (remainder == midPoint && (quotient & TSignificand.One) == TSignificand.One);

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
                    throw new OverflowException(TDecimal.OverflowMessage);
                }
                unsignedSignificand *= TDecimal.Power10(numberZeroDigits);
                exponent -= numberZeroDigits;
            }

            exponent += TDecimal.Bias;

            TValue value = TValue.Zero;
            TValue exponentVal = TValue.CreateTruncating(exponent);
            TValue significandVal = TValue.CreateTruncating(unsignedSignificand);
            bool msbSignificand = (significandVal & TDecimal.MostSignificantBitOfSignificandMask) != TValue.Zero;

            if (significand < TSignificand.Zero)
            {
                value = TDecimal.SignMask;
            }

            if (msbSignificand)
            {
                value |= TDecimal.G0G1Mask;
                exponentVal <<= TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent - 3;
                value |= exponentVal;
                significandVal ^= TDecimal.MostSignificantBitOfSignificandMask;
                value |= significandVal;
            }
            else
            {
                exponentVal <<= TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent - 1;
                value |= exponentVal;
                value |= significandVal;
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
            TSignificand significand;
            int exponent;

            if ((value & TDecimal.G0G1Mask) == TDecimal.G0G1Mask)
            {
                exponent = TDecimal.ConvertToExponent((value & TDecimal.G2ToGwPlus3ExponentMask) >> (TDecimal.NumberBitsSignificand + 1));
                significand = TDecimal.ConvertToSignificand((value & TDecimal.GwPlus4SignificandMask) | TDecimal.MostSignificantBitOfSignificandMask);
            }
            else
            {
                exponent = TDecimal.ConvertToExponent((value & TDecimal.G0ToGwPlus1ExponentMask) >> (TDecimal.NumberBitsSignificand + 3));
                significand = TDecimal.ConvertToSignificand(value & TDecimal.GwPlus2ToGwPlus4SignificandMask);
            }

            return new DecimalIeee754<TSignificand>(signed, exponent - TDecimal.Bias, significand);
        }

        internal static int CompareDecimalIeee754<TDecimal, TSignificand, TValue>(TValue currentValue, TValue otherValue)
            where TDecimal : unmanaged, IDecimalIeee754UnpackInfo<TDecimal, TSignificand, TValue>
            where TSignificand : IBinaryInteger<TSignificand>
            where TValue : IBinaryInteger<TValue>
        {
            if (currentValue == otherValue)
            {
                return 0;
            }
            DecimalIeee754<TSignificand> current = UnpackDecimalIeee754<TDecimal, TSignificand, TValue>(currentValue);
            DecimalIeee754<TSignificand> other = UnpackDecimalIeee754<TDecimal, TSignificand, TValue>(otherValue);

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

            static int InternalUnsignedCompare(DecimalIeee754<TSignificand> biggerExp, DecimalIeee754<TSignificand> smallerExp)
            {
                if (biggerExp.Significand >= smallerExp.Significand) return 1;

                int diffExponent = biggerExp.Exponent - smallerExp.Exponent;
                if (diffExponent < TDecimal.NumberDigitsPrecision)
                {
                    TSignificand factor = TDecimal.Power10(diffExponent);
                    TSignificand quotient = smallerExp.Significand / biggerExp.Significand;
                    TSignificand remainder = smallerExp.Significand % biggerExp.Significand;

                    if (quotient < factor) return 1;
                    if (quotient > factor) return -1;
                    if (remainder > TSignificand.Zero) return -1;
                    return 0;
                }

                return 1;
            }
        }
    }
}
