// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace System
{
    internal interface IDecimalIeee754ConstructorInfo<TSelf, TSignificand>
        where TSelf : unmanaged, IDecimalIeee754ConstructorInfo<TSelf, TSignificand>
        where TSignificand : IBinaryInteger<TSignificand>
    {
        static abstract TSignificand MaxSignificand { get; }
        static abstract int MaxDecimalExponent { get; }
        static abstract int MinDecimalExponent { get; }
        static abstract int NumberDigitsPrecision { get; }
        static abstract int CountDigits(TSignificand number);
        static abstract TSignificand Power10(int exponent);
    }

    internal static partial class Number
    {
        internal static bool ParseDecimalIeee754<TDecimal, TSignificand>(ref TSignificand significand, ref int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ConstructorInfo<TDecimal, TSignificand>
            where TSignificand : IBinaryInteger<TSignificand>
        {
            TSignificand unsignedSignificand = significand > TSignificand.Zero ? significand : -significand;

            if (unsignedSignificand > TDecimal.MaxSignificand && exponent > TDecimal.MaxDecimalExponent)
            {
                throw new OverflowException(SR.Overflow_Decimal);
            }

            TSignificand ten = TSignificand.CreateTruncating(10);
            if (exponent < TDecimal.MinDecimalExponent)
            {
                while (unsignedSignificand % ten == TSignificand.Zero)
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
            significand = unsignedSignificand;
            return true;
        }
    }
}
