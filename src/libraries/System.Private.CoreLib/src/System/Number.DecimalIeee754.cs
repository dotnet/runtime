// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System
{
    internal static partial class Number
    {
        internal static unsafe TValue ConstructorToDecimalIeee754Bits<TDecimal, TValue>(bool signed, TValue significand, int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TValue.IsZero(significand))
            {
                return TDecimal.Zero;
            }

            // This handles cases where the significand or exponent exceed their allowed limits.
            // It attempts to normalize the number by converting the significand into a digit buffer,
            // appending trailing zeros if necessary to bring the exponent within bounds,
            // and rounding if the digit count exceeds the allowed precision.
            // If normalization or rounding still results in an overflow, it returns positive or negative infinity.

            if (significand > TDecimal.MaxSignificand || exponent > TDecimal.MaxExponent)
            {
                byte* pDigits = stackalloc byte[TDecimal.SignificandBufferLength];
                NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, TDecimal.SignificandBufferLength);

                TDecimal.ToNumber(significand, ref number);

                if (exponent > TDecimal.MaxExponent && significand < TDecimal.MaxSignificand)
                {
                    int numberZeroDigits = exponent - TDecimal.MaxExponent;

                    if (number.DigitsCount + numberZeroDigits > TDecimal.Precision)
                    {
                        return TDecimal.PositiveInfinity;
                    }
                    byte* p = number.DigitsPtr + number.DigitsCount;

                    for (int i = 0; i < numberZeroDigits; i++)
                    {
                        *p = (byte)'0';
                        p++;
                    }
                    *p = (byte)'\0';
                    number.DigitsCount += numberZeroDigits;
                    number.Scale += numberZeroDigits;

                    exponent -= numberZeroDigits;
                }
                number.IsNegative = signed;

                significand = TDecimal.NumberToSignificand(ref number);

                if (TDecimal.Precision < number.DigitsCount)
                {
                    DecimalIeee754Rounding<TDecimal, TValue>(ref number, ref significand, ref exponent);

                    if (exponent > TDecimal.MaxExponent)
                    {
                        return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                    }
                }
            }

            return DecimalIeee754BinaryEncoding<TDecimal, TValue>(signed, significand, exponent);
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

        internal static DecimalIeee754<TValue> UnpackDecimalIeee754<TDecimal, TValue>(TValue value)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            bool signed = (value & TDecimal.SignMask) != TValue.Zero;
            TValue significand;
            int exponent;

            if ((value & TDecimal.G0G1Mask) == TDecimal.G0G1Mask)
            {
                exponent = TDecimal.ConvertToExponent((value & TDecimal.G2ToGwPlus3ExponentMask) >> (TDecimal.NumberBitsSignificand + 1));
                significand =(value & TDecimal.GwPlus4SignificandMask) | TDecimal.MostSignificantBitOfSignificandMask;
            }
            else
            {
                exponent = TDecimal.ConvertToExponent((value & TDecimal.G0ToGwPlus1ExponentMask) >> (TDecimal.NumberBitsSignificand + 3));
                significand = value & TDecimal.GwPlus2ToGwPlus4SignificandMask;
            }

            return new DecimalIeee754<TValue>(signed, exponent - TDecimal.ExponentBias, significand);
        }

        internal static int CompareDecimalIeee754<TDecimal, TValue>(TValue currentValue, TValue otherValue)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (currentValue == otherValue)
            {
                return 0;
            }
            DecimalIeee754<TValue> current = UnpackDecimalIeee754<TDecimal, TValue>(currentValue);
            DecimalIeee754<TValue> other = UnpackDecimalIeee754<TDecimal, TValue>(otherValue);

            if (current.Signed)
            {
                if (!other.Signed)
                {
                    return -1;
                }
            }
            else if (other.Signed)
            {
                return 1;
            }

            if (current.Signed)
            {
                (current, other) = (other, current);
            }

            // This method is needed to correctly compare decimals that represent the same numeric value
            // but have different exponent/significand pairs. For example, 10e2 and 1e3 have different exponents,
            // but represent the same number (1000). This function normalizes exponents and compares them accordingly,
            // without considering sign.
            return InternalUnsignedCompare(current, other);

            static int InternalUnsignedCompare(DecimalIeee754<TValue> current, DecimalIeee754<TValue> other)
            {
                if (current.Exponent == other.Exponent && current.Significand == other.Significand)
                {
                    return 0;
                }

                if (current.Exponent < other.Exponent)
                {
                    return -InternalUnsignedCompare(other, current);
                }

                if (current.Significand >= other.Significand)
                {
                    return 1;
                }

                int diffExponent = current.Exponent - other.Exponent;
                if (diffExponent < TDecimal.Precision)
                {
                    TValue factor = TDecimal.Power10(diffExponent);
                    (TValue quotient, TValue remainder) = TValue.DivRem(other.Significand, current.Significand);

                    if (quotient < factor)
                    {
                        return 1;
                    }
                    if (quotient > factor)
                    {
                        return -1;
                    }
                    if (remainder > TValue.Zero)
                    {
                        return -1;
                    }
                    return 0;
                }

                return 1;
            }
        }

        private static unsafe TValue NumberToDecimalIeee754Bits<TDecimal, TValue>(ref NumberBuffer number)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            Debug.Assert(number.DigitsPtr[0] != '0');
            Debug.Assert(number.DigitsCount != 0);
            int positiveExponent = (Math.Max(0, number.Scale));
            int integerDigitsPresent = Math.Min(positiveExponent, number.DigitsCount);
            int fractionalDigitsPresent = number.DigitsCount - integerDigitsPresent;
            int exponent = number.Scale - integerDigitsPresent - fractionalDigitsPresent;

            TValue significand = TDecimal.NumberToSignificand(ref number);

            if (TDecimal.Precision < number.DigitsCount)
            {
                DecimalIeee754Rounding<TDecimal, TValue>(ref number, ref significand, ref exponent);

                if (exponent > TDecimal.MaxExponent)
                {
                    return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }
            }
            return DecimalIeee754BinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);
        }

        private static unsafe TValue DecimalIeee754BinaryEncoding<TDecimal, TValue>(bool signed, TValue significand, int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            exponent += TDecimal.ExponentBias;

            TValue value = TValue.Zero;
            TValue exponentVal = TValue.CreateTruncating(exponent);
            bool msbSignificand = (significand & TDecimal.MostSignificantBitOfSignificandMask) != TValue.Zero;

            if (signed)
            {
                value = TDecimal.SignMask;
            }

            if (msbSignificand)
            {
                value |= TDecimal.G0G1Mask;
                exponentVal <<= TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent - 3;
                value |= exponentVal;
                significand ^= TDecimal.MostSignificantBitOfSignificandMask;
                value |= significand;
            }
            else
            {
                exponentVal <<= TDecimal.NumberBitsEncoding - TDecimal.NumberBitsExponent - 1;
                value |= exponentVal;
                value |= significand;
            }

            return value;
        }

        private static unsafe void DecimalIeee754Rounding<TDecimal, TValue>(ref NumberBuffer number, ref TValue significand, ref int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            Debug.Assert(TDecimal.Precision < number.DigitsCount);

            exponent += number.DigitsCount - TDecimal.Precision;

            int midPointValue = *(number.DigitsPtr + TDecimal.Precision);

            if (midPointValue > '5')
            {
                significand += TValue.One;
            }
            else if (midPointValue == '5')
            {
                MidPointRounding(ref significand, ref number, TDecimal.Precision);
            }

            if (significand > TDecimal.MaxSignificand)
            {
                significand = TValue.One;
                exponent += TDecimal.Precision - 1;
            }

            static void MidPointRounding(ref TValue significand, ref NumberBuffer number, int midPointDigitIndex)
            {
                int index = midPointDigitIndex + 1;
                byte* p = number.DigitsPtr + index;
                int c = *p;
                bool tiedToEvenRounding = true;

                while (index < number.DigitsCount && c != 0)
                {
                    if (c != '0')
                    {
                        significand += TValue.One;
                        tiedToEvenRounding = false;
                        break;
                    }
                    ++index;
                    c = *++p;
                }

                if (tiedToEvenRounding && !int.IsEvenInteger(*(number.DigitsPtr + midPointDigitIndex - 1) - '0'))
                {
                    significand += TValue.One;
                }
            }
        }
    }
}
