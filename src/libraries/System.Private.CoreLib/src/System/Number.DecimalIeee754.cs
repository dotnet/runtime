// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System
{
    internal static partial class Number
    {
        /// <summary>
        /// Encodes the given IEEE 754 decimal components into their binary IEEE 754
        /// decimal interchange format (BID), handles rounding/infinitive cases, producing the final <typeparamref name="TValue"/> bit pattern.
        /// </summary>
        /// <param name="signed">
        /// The sign of the value. <c>true</c> indicates a negative number; otherwise, <c>false</c>.
        /// </param>
        /// <param name="significand">
        /// The fully decoded significand (coefficient):
        /// - This is the complete integer coefficient with no packed BID encoding.
        /// - It includes all significant digits (non-trailing).
        /// - It has not been scaled by the exponent.
        /// </param>
        /// <param name="exponent">
        /// The <b>unbiased</b> exponent (actual exponent as defined by IEEE 754).
        /// This value has already been adjusted by subtracting the format's exponent bias,
        /// and will be re-biased internally when constructing the BID bit pattern.
        /// </param>
        /// <returns>
        /// The 32-bit or 64-bit or 128-bit IEEE 754 decimal BID encoding (depending on <typeparamref name="TValue"/>),
        /// containing the sign bit, combination field, biased exponent, and coefficient continuation bits.
        /// </returns>
        internal static TValue ConstructorToDecimalIeee754Bits<TDecimal, TValue>(bool signed, TValue significand, int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TValue.IsZero(significand))
            {
                return signed ? TDecimal.NegativeZero : TDecimal.Zero;
            }

            if (significand > TDecimal.MaxSignificand || exponent > TDecimal.MaxExponent || exponent < TDecimal.MinExponent)
            {
                return ConstructorToDecimalIeee754BitsRounding(signed, significand, exponent);
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, significand, exponent);

            // This method adjusts the significand and exponent to ensure they fall within valid bounds.
            // It handles underflow and overflow of the exponent by trimming or padding digits accordingly,
            // and applies rounding when the number of digits exceeds the allowed precision.
            static TValue ConstructorToDecimalIeee754BitsRounding(bool signed, TValue significand, int exponent)
            {
                int numberDigits = TDecimal.CountDigits(significand);

                if (exponent < TDecimal.MinExponent)
                {
                    int numberDigitsRemove = (TDecimal.MinExponent - exponent);

                    if (numberDigitsRemove >= numberDigits)
                    {
                        return TDecimal.Zero;
                    }

                    significand = RemoveDigitsAndRound(significand, numberDigitsRemove);
                    exponent += numberDigitsRemove;

                    if (significand > TDecimal.MaxSignificand)
                    {
                        return ConstructorToDecimalIeee754BitsRounding(signed, TDecimal.MaxSignificand + TValue.One, exponent);
                    }
                }
                else if (exponent > TDecimal.MaxExponent)
                {
                    int numberZeroDigits = exponent - TDecimal.MaxExponent;

                    if (numberDigits + numberZeroDigits <= TDecimal.Precision)
                    {
                        exponent -= numberZeroDigits;
                        significand *= TDecimal.Power10(numberZeroDigits);
                    }
                    else
                    {
                        return signed ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                    }
                }
                else if (numberDigits > TDecimal.Precision)
                {
                    int numberDigitsRemove = numberDigits - TDecimal.Precision;

                    if (exponent + numberDigitsRemove >= TDecimal.MaxExponent)
                    {
                        return signed ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                    }

                    significand = RemoveDigitsAndRound(significand, numberDigitsRemove);
                    exponent += numberDigitsRemove;

                    if (significand > TDecimal.MaxSignificand)
                    {
                        return ConstructorToDecimalIeee754BitsRounding(signed, TDecimal.MaxSignificand + TValue.One, exponent);
                    }
                }

                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed, significand, exponent);
            }

            static TValue RemoveDigitsAndRound(TValue significand, int numberDigitsRemove)
            {
                (significand, TValue remainder) = TDecimal.DivRemPow10(significand, numberDigitsRemove);

                if (remainder == TValue.Zero)
                {
                    return significand;
                }

                TValue half = TValue.CreateTruncating(5) * TDecimal.Power10(numberDigitsRemove - 1);

                if (remainder > half || (remainder == half && TValue.IsOddInteger(significand)))
                {
                    significand += TValue.One;
                }

                return significand;
            }
        }

        internal struct DecodedDecimalIeee754<TSignificand>
            where TSignificand : IBinaryInteger<TSignificand>
        {
            public bool Signed { get; }
            public int UnbiasedExponent { get; }

            /// <summary>
            /// The decoded significand (coefficient) in integer form:
            /// - Fully decoded from the BID encoding (no combination-field or DPD/BID packing).
            /// - Represents the normalized coefficient; includes the implicit leading digit if applicable.
            /// - Not scaled by the (unbiased) exponent.
            /// </summary>
            public TSignificand Significand { get; }

            public DecodedDecimalIeee754(bool signed, int unbiasedExponent, TSignificand significand)
            {
                Signed = signed;
                UnbiasedExponent = unbiasedExponent;
                Significand = significand;
            }
        }

        internal static DecodedDecimalIeee754<TValue> UnpackDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            bool signed = (decimalBits & TDecimal.SignMask) != TValue.Zero;
            TValue significand;
            int biasedExponent;

            if ((decimalBits & TDecimal.G0G1Mask) == TDecimal.G0G1Mask)
            {
                biasedExponent = TDecimal.ConvertToExponent((decimalBits & TDecimal.G2ToGwPlus3ExponentMask) >> (TDecimal.NumberBitsSignificand + 1));
                significand = (decimalBits & TDecimal.GwPlus4SignificandMask) | TDecimal.MostSignificantBitOfSignificandMask;
            }
            else
            {
                biasedExponent = TDecimal.ConvertToExponent((decimalBits & TDecimal.G0ToGwPlus1ExponentMask) >> (TDecimal.NumberBitsSignificand + 3));
                significand = decimalBits & TDecimal.GwPlus2ToGwPlus4SignificandMask;
            }

            return new DecodedDecimalIeee754<TValue>(signed, biasedExponent - TDecimal.ExponentBias, significand);
        }

        internal static int CompareDecimalIeee754<TDecimal, TValue>(TValue currentDecimalBits, TValue otherDecimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (currentDecimalBits == otherDecimalBits)
            {
                return 0;
            }

            bool isCurrentNaN = TDecimal.IsNaN(currentDecimalBits);
            bool isOtherNaN = TDecimal.IsNaN(otherDecimalBits);

            if (isCurrentNaN || isOtherNaN)
            {
                if (isCurrentNaN && isOtherNaN)
                {
                    return 0;
                }
                else
                {
                    return isCurrentNaN ? -1 : 1;
                }
            }

            bool isCurrentNegative = (currentDecimalBits & TDecimal.SignMask) != TValue.Zero;
            bool isOtherNegative = (otherDecimalBits & TDecimal.SignMask) != TValue.Zero;
            DecodedDecimalIeee754<TValue> current;
            DecodedDecimalIeee754<TValue> other;

            if (isCurrentNegative)
            {
                if (!isOtherNegative)
                {
                    return currentDecimalBits == TDecimal.NegativeZero && otherDecimalBits == TDecimal.Zero ? 0 : -1;
                }
                current = UnpackDecimalIeee754<TDecimal, TValue>(otherDecimalBits);
                other = UnpackDecimalIeee754<TDecimal, TValue>(currentDecimalBits);
            }
            else if (isOtherNegative)
            {
                return currentDecimalBits == TDecimal.Zero && otherDecimalBits == TDecimal.NegativeZero ? 0 : 1;
            }
            else
            {
                current = UnpackDecimalIeee754<TDecimal, TValue>(currentDecimalBits);
                other = UnpackDecimalIeee754<TDecimal, TValue>(otherDecimalBits);
            }

            return InternalUnsignedCompare(current, other);

            // This method is needed to correctly compare decimals that represent the same numeric value
            // but have different exponent/significand pairs. For example, 10e2 and 1e3 have different exponents,
            // but represent the same number (1000). This function normalizes exponents and compares them accordingly,
            // without considering sign.
            static int InternalUnsignedCompare(DecodedDecimalIeee754<TValue> current, DecodedDecimalIeee754<TValue> other)
            {
                if (current.UnbiasedExponent == other.UnbiasedExponent && current.Significand == other.Significand)
                {
                    return 0;
                }

                if (current.UnbiasedExponent < other.UnbiasedExponent)
                {
                    return -InternalUnsignedCompare(other, current);
                }

                if (current.Significand >= other.Significand)
                {
                    return 1;
                }

                int diffExponent = current.UnbiasedExponent - other.UnbiasedExponent;
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

        private static TValue NumberToDecimalIeee754Bits<TDecimal, TValue>(ref NumberBuffer number)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            Debug.Assert(number.Digits[0] != '0');
            Debug.Assert(number.DigitsCount != 0);

            if (number.DigitsCount > TDecimal.Precision)
            {
                return DecimalIeee754Rounding<TDecimal, TValue>(ref number, TDecimal.Precision);
            }

            int positiveExponent = (Math.Max(0, number.Scale));
            int integerDigitsPresent = Math.Min(positiveExponent, number.DigitsCount);
            int fractionalDigitsPresent = number.DigitsCount - integerDigitsPresent;
            int exponent = number.Scale - integerDigitsPresent - fractionalDigitsPresent;

            if (exponent < TDecimal.MinExponent)
            {
                int numberDigitsRemove = (TDecimal.MinExponent - exponent);
                if (numberDigitsRemove < number.DigitsCount)
                {
                    int numberDigitsRemain = number.DigitsCount - numberDigitsRemove;
                    return DecimalIeee754Rounding<TDecimal, TValue>(ref number, numberDigitsRemain);
                }
                else
                {
                    return number.IsNegative ? TDecimal.NegativeZero : TDecimal.Zero;
                }
            }

            TValue significand = TDecimal.NumberToSignificand(ref number, number.DigitsCount);

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);
        }

        /// <summary>
        /// Encodes the given IEEE 754 decimal components into their finite number binary IEEE 754
        /// decimal interchange format (BID), producing the final <typeparamref name="TValue"/> bit pattern.
        /// </summary>
        /// <param name="signed">
        /// The sign of the value. <c>true</c> indicates a negative number; otherwise, <c>false</c>.
        /// </param>
        /// <param name="significand">
        /// The fully decoded significand (coefficient):
        /// - This is the complete integer coefficient with no packed BID encoding.
        /// - It includes all significant digits (non-trailing).
        /// - It has not been scaled by the exponent.
        /// </param>
        /// <param name="exponent">
        /// The <b>unbiased</b> exponent (actual exponent as defined by IEEE 754).
        /// This value has already been adjusted by subtracting the format's exponent bias,
        /// and will be re-biased internally when constructing the BID bit pattern.
        /// </param>
        /// <returns>
        /// The 32-bit or 64-bit or 128-bit IEEE 754 decimal BID encoding (depending on <typeparamref name="TValue"/>),
        /// containing the sign bit, combination field, biased exponent, and coefficient continuation bits.
        /// </returns>
        private static TValue DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(bool signed, TValue significand, int exponent)
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

        /// <summary>
        /// Performs IEEE 754-compliant rounding on a decimal-like number before converting it
        /// to an IEEE 754 decimal32/64/128 encoded value.
        ///
        /// ---------------------------------------------------------------
        ///  ROUNDING DECISION (implements round-to-nearest, ties-to-even)
        ///
        ///  Unit In The Last Place (ULP) formula: ULP = 10^(unbiased exponent - number digits precision + 1)
        ///  The difference between the unrounded number and the rounded
        ///  representable value is effectively compared against ±ULP/2.
        ///
        ///  If discarded part > 0.5 ULP → round up
        ///  If discarded part &lt; 0.5 ULP → round down
        ///  If exactly 0.5 ULP → ties-to-even
        /// </summary>
        private static TValue DecimalIeee754Rounding<TDecimal, TValue>(ref NumberBuffer number, int digits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            Debug.Assert(digits < number.DigitsCount);

            TValue significand = TDecimal.NumberToSignificand(ref number, digits);

            int positiveExponent = (Math.Max(0, number.Scale));
            int integerDigitsPresent = Math.Min(positiveExponent, number.DigitsCount);
            int fractionalDigitsPresent = number.DigitsCount - integerDigitsPresent;
            int exponent = number.Scale - integerDigitsPresent - fractionalDigitsPresent;

            exponent += number.DigitsCount - digits;

            bool increaseOne = false;
            int midPointValue = number.Digits[digits];

            if (midPointValue > '5')
            {
                increaseOne = true;
            }
            else if (midPointValue == '5')
            {
                int index = digits + 1;
                int c = number.Digits[index];
                bool tiedToEvenRounding = true;

                while (index < number.DigitsCount && c != 0)
                {
                    if (c != '0')
                    {
                        increaseOne = true;
                        tiedToEvenRounding = false;
                        break;
                    }
                    ++index;
                    c = number.Digits[index];
                }

                if (tiedToEvenRounding && !int.IsEvenInteger(number.Digits[digits - 1] - '0'))
                {
                    increaseOne = true;
                }
            }

            if (increaseOne)
            {
                if (significand == TDecimal.MaxSignificand)
                {
                    significand = TDecimal.Power10(TDecimal.Precision - 1);
                    exponent += 1;
                }
                else
                {
                    significand += TValue.One;
                }
            }

            if (exponent > TDecimal.MaxExponent)
            {
                return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);
        }
    }
}
