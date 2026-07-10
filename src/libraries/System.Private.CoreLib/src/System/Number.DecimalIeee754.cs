// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System
{
    internal static partial class Number
    {
        internal static int GetDecimalIeee754HashCode<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(decimalBits) || TDecimal.IsInfinity(decimalBits))
            {
                return (decimalBits & TDecimal.NaNMask).GetHashCode();
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);
            if (decoded.Significand == TValue.Zero)
            {
                return TDecimal.Zero.GetHashCode();
            }

            int digits = TDecimal.CountDigits(decoded.Significand);
            if (digits < TDecimal.Precision)
            {
                int numberZeroDigits = TDecimal.Precision - digits;
                TValue significand = decoded.Significand * TDecimal.Power10(numberZeroDigits);
                int exponent = decoded.UnbiasedExponent - numberZeroDigits;
                return HashCode.Combine(decoded.Signed, significand, exponent);
            }
            return HashCode.Combine(decoded.Signed, decoded.Significand, decoded.UnbiasedExponent);
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

        /// <summary>
        /// Compares two IEEE 754 decimal values represented by their raw bit patterns.
        /// </summary>
        /// <remarks>
        /// The implementation first handles special cases so the result stays consistent
        /// with the .NET equality and ordering contract required by <c>Equals</c>,
        /// <c>GetHashCode</c>, and <c>CompareTo</c>:
        /// - identical bit patterns compare equal;
        /// - NaN compares equal to NaN;
        /// - infinities are handled explicitly;
        /// - positive and negative zero compare equal.
        /// After special-case handling, finite non-zero values are unpacked and compared
        /// by sign first, then by unsigned magnitude.
        /// Treating NaN as equal to NaN here is intentional so values that are considered
        /// equal for comparison also behave consistently in hashing, collections, and
        /// sorting scenarios.
        /// </remarks>
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

            if (TDecimal.IsInfinity(currentDecimalBits) || TDecimal.IsInfinity(otherDecimalBits))
            {
                return InternalInfinityCompare(currentDecimalBits, otherDecimalBits);
            }

            DecodedDecimalIeee754<TValue> current = UnpackDecimalIeee754<TDecimal, TValue>(currentDecimalBits);
            DecodedDecimalIeee754<TValue> other = UnpackDecimalIeee754<TDecimal, TValue>(otherDecimalBits);

            if (current.Significand == TValue.Zero && other.Significand == TValue.Zero)
            {
                return 0;
            }

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

            int result = InternalUnsignedCompare(current, other);
            return current.Signed ? -result : result;

            // This method is needed to correctly compare decimals that represent the same numeric value
            // but have different exponent/significand pairs. For example, 10e2 and 1e3 have different exponents,
            // but represent the same number (1000). This function normalizes exponents and compares them accordingly,
            // without considering sign.
            static int InternalUnsignedCompare(DecodedDecimalIeee754<TValue> current, DecodedDecimalIeee754<TValue> other)
            {
                if (current.Significand == TValue.Zero)
                {
                    return other.Significand == TValue.Zero ? 0 : -1;
                }
                else if (other.Significand == TValue.Zero)
                {
                    return 1;
                }

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

            static int InternalInfinityCompare(TValue current, TValue other)
            {
                if (TDecimal.IsPositiveInfinity(current))
                {
                    return TDecimal.IsPositiveInfinity(other) ? 0 : 1;
                }
                else if (TDecimal.IsNegativeInfinity(current))
                {
                    return TDecimal.IsNegativeInfinity(other) ? 0 : -1;
                }

                return TDecimal.IsPositiveInfinity(other) ? -1 : 1;
            }
        }

        /// <summary>
        /// Determines whether two IEEE 754 decimal values represented by their raw bit patterns
        /// are numerically equal using the semantics required by the equality operator.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CompareDecimalIeee754{TDecimal, TValue}"/>, which treats NaN as equal
        /// to NaN so that equality is consistent with hashing and ordering, this method follows the
        /// IEEE 754 equality-comparison rules used by <c>operator ==</c>: NaN is unordered and is
        /// therefore never equal to any value, including itself. All other values (including the two
        /// zeros and different members of the same cohort) compare by numeric value.
        /// </remarks>
        internal static bool EqualsDecimalIeee754<TDecimal, TValue>(TValue leftDecimalBits, TValue rightDecimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(leftDecimalBits) || TDecimal.IsNaN(rightDecimalBits))
            {
                return false;
            }

            return CompareDecimalIeee754<TDecimal, TValue>(leftDecimalBits, rightDecimalBits) == 0;
        }

        /// <summary>
        /// Determines the ordering of two IEEE 754 decimal values represented by their raw bit
        /// patterns using the semantics required by the relational operators
        /// (<c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>).
        /// </summary>
        /// <remarks>
        /// A NaN operand is unordered: every relational comparison that involves one is
        /// <see langword="false"/>. When neither operand is NaN the values are ordered by numeric
        /// value using <see cref="CompareDecimalIeee754{TDecimal, TValue}"/>, so the two zeros
        /// compare equal and different members of the same cohort compare equal.
        /// </remarks>
        internal static bool LessThanDecimalIeee754<TDecimal, TValue>(TValue leftDecimalBits, TValue rightDecimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return !TDecimal.IsNaN(leftDecimalBits) && !TDecimal.IsNaN(rightDecimalBits)
                && CompareDecimalIeee754<TDecimal, TValue>(leftDecimalBits, rightDecimalBits) < 0;
        }

        /// <inheritdoc cref="LessThanDecimalIeee754{TDecimal, TValue}(TValue, TValue)"/>
        internal static bool GreaterThanDecimalIeee754<TDecimal, TValue>(TValue leftDecimalBits, TValue rightDecimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return !TDecimal.IsNaN(leftDecimalBits) && !TDecimal.IsNaN(rightDecimalBits)
                && CompareDecimalIeee754<TDecimal, TValue>(leftDecimalBits, rightDecimalBits) > 0;
        }

        /// <inheritdoc cref="LessThanDecimalIeee754{TDecimal, TValue}(TValue, TValue)"/>
        internal static bool LessThanOrEqualDecimalIeee754<TDecimal, TValue>(TValue leftDecimalBits, TValue rightDecimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return !TDecimal.IsNaN(leftDecimalBits) && !TDecimal.IsNaN(rightDecimalBits)
                && CompareDecimalIeee754<TDecimal, TValue>(leftDecimalBits, rightDecimalBits) <= 0;
        }

        /// <inheritdoc cref="LessThanDecimalIeee754{TDecimal, TValue}(TValue, TValue)"/>
        internal static bool GreaterThanOrEqualDecimalIeee754<TDecimal, TValue>(TValue leftDecimalBits, TValue rightDecimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return !TDecimal.IsNaN(leftDecimalBits) && !TDecimal.IsNaN(rightDecimalBits)
                && CompareDecimalIeee754<TDecimal, TValue>(leftDecimalBits, rightDecimalBits) >= 0;
        }

        private static TValue NumberToDecimalIeee754Bits<TDecimal, TValue>(ref NumberBuffer number)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (number.DigitsCount == 0)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, TValue.Zero, number.Scale);
            }

            Debug.Assert(number.Digits[0] != '0');
            Debug.Assert(number.DigitsCount != 0);

            int positiveExponent = Math.Max(0, number.Scale);
            int integerDigitsPresent = Math.Min(positiveExponent, number.DigitsCount);
            int fractionalDigitsPresent = number.DigitsCount - integerDigitsPresent;
            int exponent = number.Scale - integerDigitsPresent - fractionalDigitsPresent;

            if (exponent > TDecimal.MaxExponent)
            {
                return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            if (exponent > TDecimal.MaxAdjustedExponent)
            {
                return ClampExponentOverflow(ref number, exponent);
            }

            if (exponent < TDecimal.MinAdjustedExponent)
            {
                int numberDigitsRemove = TDecimal.MinAdjustedExponent - exponent;

                if (numberDigitsRemove > number.DigitsCount)
                {
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, TValue.Zero, TDecimal.MinAdjustedExponent);
                }
                else if (numberDigitsRemove < number.DigitsCount)
                {
                    int numberDigitsRemain = number.DigitsCount - numberDigitsRemove;
                    Debug.Assert(numberDigitsRemain <= TDecimal.Precision);
                    return DecimalIeee754Rounding<TDecimal, TValue>(ref number, numberDigitsRemain);
                }
                else
                {
                    return RoundToZeroOrEpsilon<TDecimal, TValue>(ref number);
                }
            }

            if (number.DigitsCount > TDecimal.Precision)
            {
                int numberDigitsRemove = number.DigitsCount - TDecimal.Precision;
                if (exponent + numberDigitsRemove > TDecimal.MaxAdjustedExponent)
                {
                    return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }
                return DecimalIeee754Rounding<TDecimal, TValue>(ref number, TDecimal.Precision);
            }

            TValue significand = TDecimal.NumberToSignificand(ref number, number.DigitsCount);

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);

            static TValue ClampExponentOverflow(ref NumberBuffer number, int exponent)
            {
                Debug.Assert(exponent > TDecimal.MaxAdjustedExponent);

                int numberDigits = number.DigitsCount;

                int numberZeroDigits = exponent - TDecimal.MaxAdjustedExponent;

                if (numberDigits + numberZeroDigits > TDecimal.Precision)
                {
                    return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }

                for (int i = numberDigits; i < numberDigits + numberZeroDigits; i++)
                {
                    number.Digits[i] = (byte)'0';
                }

                number.Digits[numberDigits + numberZeroDigits] = (byte)('\0');
                number.DigitsCount += numberZeroDigits;
                number.Scale -= numberZeroDigits;

                number.CheckConsistency();

                TValue significand = TDecimal.NumberToSignificand(ref number, number.DigitsCount);

                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, TDecimal.MaxAdjustedExponent);
            }
        }

        /// <summary>
        /// Encodes the given IEEE 754 decimal components into their Binary Integer Decimal (BID),
        /// producing the final <typeparamref name="TValue"/> bit pattern.
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
            Debug.Assert(significand <= TDecimal.MaxSignificand);

            if (TValue.IsZero(significand))
            {
                if (exponent < TDecimal.MinAdjustedExponent)
                {
                    exponent = TDecimal.MinAdjustedExponent;
                }
                else if (exponent > TDecimal.MaxExponent)
                {
                    exponent = TDecimal.MaxExponent;
                }
            }

            uint biasedExponent = (uint)(exponent + TDecimal.ExponentBias);

            TValue value = TValue.Zero;
            bool msbSignificand = (significand & TDecimal.MostSignificantBitOfSignificandMask) != TValue.Zero;

            if (signed)
            {
                value = TDecimal.SignMask;
            }

            if (msbSignificand)
            {
                value |= TDecimal.G0G1Mask;
                value |= TDecimal.EncodeExponentToG2ThroughGwPlus3(biasedExponent);
                significand ^= TDecimal.MostSignificantBitOfSignificandMask;
                value |= significand;
            }
            else
            {
                value |= TDecimal.EncodeExponentToG0ThroughGwPlus1(biasedExponent);
                value |= significand;
            }

            return value;
        }

        private static TValue RoundToZeroOrEpsilon<TDecimal, TValue>(ref NumberBuffer coefficient)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            int midPointValue = coefficient.Digits[0];

            if (midPointValue < '5')
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(coefficient.IsNegative, TValue.Zero, TDecimal.MinAdjustedExponent);
            }
            else if (midPointValue > '5')
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(coefficient.IsNegative, TValue.One, TDecimal.MinAdjustedExponent);
            }
            else
            {
                return coefficient.DigitsCount > 1 && (coefficient.Digits.Slice(1).ContainsAnyExcept((byte)'0') || coefficient.HasNonZeroTail)
                    ? DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(coefficient.IsNegative, TValue.One, TDecimal.MinAdjustedExponent)
                    : DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(coefficient.IsNegative, TValue.Zero, TDecimal.MinAdjustedExponent);
            }
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
        private static TValue DecimalIeee754Rounding<TDecimal, TValue>(ref NumberBuffer number, int digitsRemain)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            Debug.Assert(digitsRemain < number.DigitsCount);
            Debug.Assert(digitsRemain <= TDecimal.Precision);
            Debug.Assert(digitsRemain >= 0);

            int positiveExponent = (Math.Max(0, number.Scale));
            int integerDigitsPresent = Math.Min(positiveExponent, number.DigitsCount);
            int fractionalDigitsPresent = number.DigitsCount - integerDigitsPresent;
            int exponent = number.Scale - integerDigitsPresent - fractionalDigitsPresent;
            exponent += number.DigitsCount - digitsRemain;

            Debug.Assert(exponent >= TDecimal.MinAdjustedExponent && exponent <= TDecimal.MaxAdjustedExponent);

            TValue significand = TDecimal.NumberToSignificand(ref number, digitsRemain);

            Debug.Assert(significand <= TDecimal.MaxSignificand);

            bool roundDown = true;
            int midPointValue = number.Digits[digitsRemain];

            if (midPointValue > '5')
            {
                roundDown = false;
            }
            else if (midPointValue == '5')
            {
                int index = digitsRemain + 1;

                if (number.HasNonZeroTail
                    || int.IsOddInteger(number.Digits[digitsRemain - 1] - '0')
                    || number.Digits.Slice(index, number.DigitsCount - index).ContainsAnyExcept((byte)'0'))
                {
                    roundDown = false;
                }
            }

            if (roundDown)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);
            }

            if (significand == TDecimal.MaxSignificand)
            {
                exponent += 1;

                if (exponent > TDecimal.MaxAdjustedExponent)
                {
                    return number.IsNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }

                significand = TDecimal.Power10(TDecimal.Precision - 1);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);
            }

            significand += TValue.One;

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(number.IsNegative, significand, exponent);
        }

        /// <summary>
        /// Adds two IEEE 754 decimal values represented by their raw bit patterns and returns the
        /// bit pattern of the correctly rounded (round-to-nearest, ties-to-even) sum.
        /// </summary>
        /// <remarks>
        /// The two operands are decoded, aligned to their common (smaller) exponent, and summed
        /// exactly in base 10. Digits that fall below the retained precision are folded into a
        /// sticky flag so the shared rounding path produces the same result as computing the exact
        /// sum. This mirrors the mathematical behavior of the Intel reference implementation while
        /// remaining independent of the underlying integer width.
        /// </remarks>
        internal static TValue AddDecimalIeee754<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_add`, `bid64_add`, and `bid128_add` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(left) || TDecimal.IsNaN(right))
            {
                return TDecimal.NaN;
            }

            if (TDecimal.IsInfinity(left))
            {
                // Inf + Inf with opposing signs is invalid (NaN); every other combination
                // that includes at least one infinity returns that infinity (canonicalized).
                if (TDecimal.IsInfinity(right) && (TDecimal.IsNegative(left) != TDecimal.IsNegative(right)))
                {
                    return TDecimal.NaN;
                }
                return TDecimal.IsNegative(left) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            if (TDecimal.IsInfinity(right))
            {
                return TDecimal.IsNegative(right) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(left);
            DecodedDecimalIeee754<TValue> b = UnpackDecimalIeee754<TDecimal, TValue>(right);

            bool aZero = TValue.IsZero(a.Significand);
            bool bZero = TValue.IsZero(b.Significand);

            if (aZero && bZero)
            {
                // The sum of two zeros keeps the shared sign, otherwise it is +0 under
                // round-to-nearest. The preferred exponent for a zero result is the smaller one.
                bool zeroSign = a.Signed == b.Signed && a.Signed;
                int zeroExponent = Math.Min(a.UnbiasedExponent, b.UnbiasedExponent);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(zeroSign, TValue.Zero, zeroExponent);
            }

            if (aZero || bZero)
            {
                // Adding zero yields the other operand's value, but the preferred exponent is the
                // smaller of the two exponents. When the zero's exponent is larger there is nothing
                // to do; otherwise the coefficient is padded with trailing zeros (bounded by the
                // available precision) to lower the exponent toward the zero's exponent.
                DecodedDecimalIeee754<TValue> nonZero = aZero ? b : a;
                TValue nonZeroBits = aZero ? right : left;
                int zeroExponent = aZero ? a.UnbiasedExponent : b.UnbiasedExponent;

                if (zeroExponent >= nonZero.UnbiasedExponent)
                {
                    return nonZeroBits;
                }

                int nonZeroDigits = TDecimal.CountDigits(nonZero.Significand);
                int pad = Math.Min(nonZero.UnbiasedExponent - zeroExponent, TDecimal.Precision - nonZeroDigits);
                TValue paddedSignificand = nonZero.Significand * TDecimal.Power10(pad);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(nonZero.Signed, paddedSignificand, nonZero.UnbiasedExponent - pad);
            }

            // Both operands are finite and non-zero. Order them so `hi` has the larger (or equal)
            // exponent, then align `lo` to `hi` by scaling `hi` up. The exponent difference is
            // capped: anything beyond `guard` extra digits cannot influence the retained precision
            // except through rounding, so those low-order digits of `lo` become a sticky flag.
            DecodedDecimalIeee754<TValue> hi;
            DecodedDecimalIeee754<TValue> lo;

            if (a.UnbiasedExponent >= b.UnbiasedExponent)
            {
                hi = a;
                lo = b;
            }
            else
            {
                hi = b;
                lo = a;
            }

            int exponentDifference = hi.UnbiasedExponent - lo.UnbiasedExponent;
            int guard = TDecimal.Precision + 2;
            int effectiveDifference = Math.Min(exponentDifference, guard);
            int droppedDigits = exponentDifference - effectiveDifference;
            int commonExponent = hi.UnbiasedExponent - effectiveDifference;

            int capacity = (2 * TDecimal.Precision) + 4;
            Span<byte> hiDigits = stackalloc byte[capacity];
            Span<byte> loDigits = stackalloc byte[capacity];

            int hiLength = WriteDigits(hi.Significand, hiDigits, TDecimal.CountDigits(hi.Significand));
            for (int i = 0; i < effectiveDifference; i++)
            {
                hiDigits[hiLength++] = (byte)'0';
            }

            int loRawLength = TDecimal.CountDigits(lo.Significand);
            bool sticky = false;
            int loLength;

            if (droppedDigits >= loRawLength)
            {
                // Every digit of `lo` falls below the retained range; it only contributes stickiness.
                loLength = 0;
                sticky = true;
            }
            else if (droppedDigits > 0)
            {
                Span<byte> loRaw = stackalloc byte[capacity];
                WriteDigits(lo.Significand, loRaw, loRawLength);
                loLength = loRawLength - droppedDigits;
                loRaw.Slice(0, loLength).CopyTo(loDigits);

                for (int i = loLength; i < loRawLength; i++)
                {
                    if (loRaw[i] != (byte)'0')
                    {
                        sticky = true;
                        break;
                    }
                }
            }
            else
            {
                loLength = WriteDigits(lo.Significand, loDigits, loRawLength);
            }

            bool sameSign = hi.Signed == lo.Signed;
            bool resultSign;
            int magnitudeLength;
            Span<byte> magnitude = stackalloc byte[capacity + 1];

            if (sameSign)
            {
                magnitudeLength = BigAdd(hiDigits.Slice(0, hiLength), loDigits.Slice(0, loLength), magnitude);
                resultSign = hi.Signed;
            }
            else
            {
                int comparison = BigCompare(hiDigits.Slice(0, hiLength), loDigits.Slice(0, loLength));

                if (comparison > 0)
                {
                    magnitudeLength = BigSub(hiDigits.Slice(0, hiLength), loDigits.Slice(0, loLength), magnitude);
                    resultSign = hi.Signed;

                    if (sticky)
                    {
                        // The true `lo` magnitude is slightly larger than its retained digits, so the
                        // exact difference is one unit smaller with a non-zero fractional remainder.
                        BigDecrement(magnitude.Slice(0, magnitudeLength));
                    }
                }
                else if (comparison < 0)
                {
                    // `droppedDigits` is always zero here, so there is no sticky remainder to account for.
                    magnitudeLength = BigSub(loDigits.Slice(0, loLength), hiDigits.Slice(0, hiLength), magnitude);
                    resultSign = lo.Signed;
                }
                else
                {
                    // Exact cancellation produces +0 under round-to-nearest.
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(false, TValue.Zero, commonExponent);
                }
            }

            int start = 0;
            while (start < magnitudeLength && magnitude[start] == (byte)'0')
            {
                start++;
            }
            int digitsCount = magnitudeLength - start;

            if (digitsCount == 0)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(false, TValue.Zero, commonExponent);
            }

            Span<byte> numberDigits = stackalloc byte[capacity + 2];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, numberDigits);
            magnitude.Slice(start, digitsCount).CopyTo(number.Digits);
            number.Digits[digitsCount] = 0;
            number.DigitsCount = digitsCount;
            number.Scale = digitsCount + commonExponent;
            number.IsNegative = resultSign;
            number.HasNonZeroTail = sticky;
            number.CheckConsistency();

            return NumberToDecimalIeee754Bits<TDecimal, TValue>(ref number);

            static int WriteDigits(TValue value, Span<byte> destination, int length)
            {
                TValue ten = TValue.CreateTruncating(10);

                for (int i = length - 1; i >= 0; i--)
                {
                    (value, TValue remainder) = TValue.DivRem(value, ten);
                    destination[i] = (byte)('0' + int.CreateTruncating(remainder));
                }

                return length;
            }
        }

        private static ReadOnlySpan<byte> TrimLeadingZeros(ReadOnlySpan<byte> digits)
        {
            int start = 0;
            while (start < digits.Length && digits[start] == (byte)'0')
            {
                start++;
            }
            return digits.Slice(start);
        }

        private static int BigCompare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            left = TrimLeadingZeros(left);
            right = TrimLeadingZeros(right);

            if (left.Length != right.Length)
            {
                return left.Length < right.Length ? -1 : 1;
            }

            return left.SequenceCompareTo(right);
        }

        private static int BigAdd(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, Span<byte> result)
        {
            int leftLength = left.Length;
            int rightLength = right.Length;
            int length = Math.Max(leftLength, rightLength) + 1;
            int carry = 0;

            for (int position = 0; position < length; position++)
            {
                int sum = carry;

                if (position < leftLength)
                {
                    sum += left[leftLength - 1 - position] - '0';
                }
                if (position < rightLength)
                {
                    sum += right[rightLength - 1 - position] - '0';
                }

                if (sum >= 10)
                {
                    sum -= 10;
                    carry = 1;
                }
                else
                {
                    carry = 0;
                }

                result[length - 1 - position] = (byte)('0' + sum);
            }

            return length;
        }

        // Subtracts `right` from `left`, which must be greater than or equal to `right` in magnitude.
        private static int BigSub(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, Span<byte> result)
        {
            left = TrimLeadingZeros(left);
            right = TrimLeadingZeros(right);

            int leftLength = left.Length;
            int rightLength = right.Length;
            int borrow = 0;

            for (int position = 0; position < leftLength; position++)
            {
                int difference = (left[leftLength - 1 - position] - '0') - borrow;

                if (position < rightLength)
                {
                    difference -= right[rightLength - 1 - position] - '0';
                }

                if (difference < 0)
                {
                    difference += 10;
                    borrow = 1;
                }
                else
                {
                    borrow = 0;
                }

                result[leftLength - 1 - position] = (byte)('0' + difference);
            }

            return leftLength;
        }

        // Subtracts one from a non-zero magnitude in place. The caller guarantees no underflow.
        private static void BigDecrement(Span<byte> magnitude)
        {
            for (int position = magnitude.Length - 1; position >= 0; position--)
            {
                if (magnitude[position] > (byte)'0')
                {
                    magnitude[position]--;
                    return;
                }

                magnitude[position] = (byte)'9';
            }
        }
    }
}
