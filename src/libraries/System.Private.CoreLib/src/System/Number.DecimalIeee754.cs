// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
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

            // A finite encoding whose significand exceeds the maximum representable coefficient is
            // non-canonical and is treated as zero. This matches the non-canonical handling in
            // `unpack_BID32`, `unpack_BID64`, and `unpack_BID128` from the Intel Decimal
            // Floating-Point Math Library and keeps all downstream arithmetic and comparison
            // logic robust for every finite bit pattern.
            if (significand > TDecimal.MaxSignificand)
            {
                significand = TValue.Zero;
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

                    if (numberDigitsRemain > TDecimal.Precision)
                    {
                        // The coefficient still exceeds the format precision after shifting to the
                        // minimum quantum, so the value is actually in the normal range rather than
                        // subnormal. Round the full (exact) digit string to the format precision in a
                        // single step; this avoids a double rounding and yields a quantum that is at or
                        // above the minimum adjusted exponent.
                        numberDigitsRemain = TDecimal.Precision;
                    }

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
                else if (exponent > TDecimal.MaxAdjustedExponent)
                {
                    exponent = TDecimal.MaxAdjustedExponent;
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
        /// Produces the quiet NaN that an arithmetic operation propagates from its NaN operands, following the
        /// IEEE 754-2019 §6.2.3 recommendation to preserve the payload of the first NaN operand.
        /// </summary>
        /// <remarks>
        /// The first NaN operand (<paramref name="left" /> before <paramref name="right" />) supplies the sign and
        /// payload of the result. A signaling NaN is quieted and a payload that is too large to be canonical
        /// (greater than or equal to 10^(<c>Precision</c> - 1)) is discarded, matching the behavior of the Intel
        /// reference implementation.
        /// </remarks>
        private static TValue PropagateNaN<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `unpack_BID32`, `unpack_BID64`, and `unpack_BID128_value_BLE` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            TValue nanBits = TDecimal.IsNaN(left) ? left : right;

            // The payload occupies the trailing significand field; a value at or above the canonical
            // bound is non-canonical and reads as zero. Re-encoding through the NaN combination mask
            // clears the signaling bit, yielding a canonical quiet NaN that keeps the operand's sign.
            TValue payload = nanBits & ((TValue.One << TDecimal.NumberBitsSignificand) - TValue.One);

            if (payload >= TDecimal.Power10(TDecimal.Precision - 1))
            {
                payload = TValue.Zero;
            }

            return (nanBits & TDecimal.SignMask) | TDecimal.NaNMask | payload;
        }

        /// <summary>
        /// Returns <paramref name="bits" /> unchanged when it is a number, or the canonical quiet NaN when it is a
        /// NaN. The minimum/maximum family selects one operand to return; routing that operand through this helper
        /// canonicalizes a NaN result as IEEE 754-2019 §5.1 requires without disturbing the numeric selection.
        /// </summary>
        private static TValue CanonicalizeIfNaN<TDecimal, TValue>(TValue bits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return TDecimal.IsNaN(bits) ? PropagateNaN<TDecimal, TValue>(bits, bits) : bits;
        }

        /// <summary>
        /// Adds two IEEE 754 decimal values represented by their raw bit patterns and returns the
        /// bit pattern of the correctly rounded (round-to-nearest, ties-to-even) sum.
        /// </summary>
        /// <remarks>
        /// The two operands are decoded and aligned to their common (smaller) exponent by scaling the larger-exponent
        /// coefficient up at double integer width. Digits of the smaller operand that fall below the retained precision
        /// are folded into a sticky flag. The aligned coefficients are then added or subtracted with word-level integer
        /// arithmetic and fed into the shared rounding path, producing the same result as computing the exact sum. This
        /// mirrors the mathematical behavior of the Intel reference implementation.
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
                return PropagateNaN<TDecimal, TValue>(left, right);
            }

            if (TDecimal.IsInfinity(left))
            {
                // Inf + Inf with opposing signs is invalid (NaN); every other combination
                // that includes at least one infinity returns that infinity (canonicalized).
                if (TDecimal.IsInfinity(right) && (TDecimal.IsNegative(left) != TDecimal.IsNegative(right)))
                {
                    // An invalid operation produces the canonical quiet NaN, which the Intel reference
                    // emits with a positive sign and empty payload (`NaNMask`), unlike the negative
                    // `TDecimal.NaN` constant.
                    return TDecimal.NaNMask;
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

            // Align `hi` to the common exponent by scaling its coefficient up by 10^effectiveDifference. The
            // scaled coefficient can exceed a single limb (up to ~10^(2*Precision+1)), so it is held at double
            // width. The scale factor fits a single limb because effectiveDifference <= Precision + 2.
            WideMultiply(hi.Significand, AlignmentScaleFactor<TDecimal, TValue>(effectiveDifference), out TValue magnitudeHigh, out TValue magnitudeLow);

            // Align `lo` to the common exponent by discarding its `droppedDigits` least-significant digits, which
            // fall below the retained range and only contribute stickiness. The retained portion fits a single limb.
            bool sticky = false;
            TValue loRetained;

            if (droppedDigits >= TDecimal.CountDigits(lo.Significand))
            {
                loRetained = TValue.Zero;
                sticky = true;
            }
            else if (droppedDigits > 0)
            {
                (loRetained, TValue remainder) = TValue.DivRem(lo.Significand, TDecimal.Power10(droppedDigits));
                sticky = !TValue.IsZero(remainder);
            }
            else
            {
                loRetained = lo.Significand;
            }

            bool sameSign = hi.Signed == lo.Signed;
            bool resultSign;

            if (sameSign)
            {
                // Magnitudes add. Fold the retained `lo` coefficient into the double-width accumulator.
                TValue newLow = magnitudeLow + loRetained;
                if (newLow < magnitudeLow)
                {
                    magnitudeHigh += TValue.One;
                }
                magnitudeLow = newLow;
                resultSign = hi.Signed;
            }
            else
            {
                // Magnitudes subtract. When `droppedDigits > 0` the scaled `hi` coefficient is at least 10^guard,
                // which dominates the retained `lo` (< 10^Precision), so `hi` always compares greater in that case.
                int comparison = !TValue.IsZero(magnitudeHigh) ? 1 : magnitudeLow.CompareTo(loRetained);

                if (comparison > 0)
                {
                    if (magnitudeLow < loRetained)
                    {
                        magnitudeHigh -= TValue.One;
                    }
                    magnitudeLow -= loRetained;
                    resultSign = hi.Signed;

                    if (sticky)
                    {
                        // The true `lo` magnitude is slightly larger than its retained digits, so the exact
                        // difference is one unit smaller with a non-zero fractional remainder.
                        if (TValue.IsZero(magnitudeLow))
                        {
                            magnitudeHigh -= TValue.One;
                        }
                        magnitudeLow -= TValue.One;
                    }
                }
                else if (comparison < 0)
                {
                    // `droppedDigits` is always zero here, so there is no sticky remainder to account for and both
                    // magnitudes fit a single limb.
                    magnitudeLow = loRetained - magnitudeLow;
                    magnitudeHigh = TValue.Zero;
                    resultSign = lo.Signed;
                }
                else
                {
                    // Exact cancellation produces +0 under round-to-nearest.
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(false, TValue.Zero, commonExponent);
                }
            }

            if (TValue.IsZero(magnitudeHigh) && TValue.IsZero(magnitudeLow) && !sticky)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(false, TValue.Zero, commonExponent);
            }

            return NumberToDecimalIeee754BitsFromWide<TDecimal, TValue>(resultSign, magnitudeHigh, magnitudeLow, commonExponent, sticky);
        }

        /// <summary>
        /// Subtracts one IEEE 754 decimal value from another, both represented by their raw bit patterns, and
        /// returns the bit pattern of the correctly rounded (round-to-nearest, ties-to-even) difference.
        /// </summary>
        /// <remarks>
        /// Subtraction negates the right operand and defers to <see cref="AddDecimalIeee754" />. The sign of a NaN
        /// operand is left untouched so that a NaN result propagates the same payload and sign as addition would.
        /// </remarks>
        internal static TValue SubtractDecimalIeee754<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_sub`, `bid64_sub`, and `bid128_sub` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (!TDecimal.IsNaN(right))
            {
                right ^= TDecimal.SignMask;
            }

            return AddDecimalIeee754<TDecimal, TValue>(left, right);
        }

        /// <summary>
        /// Multiplies two IEEE 754 decimal values represented by their raw bit patterns and returns the
        /// bit pattern of the correctly rounded (round-to-nearest, ties-to-even) product.
        /// </summary>
        /// <remarks>
        /// The operands are decoded and their coefficients multiplied exactly using a double-width integer product
        /// (the product can require up to twice the format precision). The exact product exponent is the sum of the
        /// operand exponents, which is also the IEEE 754 preferred exponent because trailing zeros of the product are
        /// retained. The exact product is then fed into the shared word-level rounding path. This mirrors the
        /// mathematical behavior of the Intel reference implementation.
        /// </remarks>
        internal static TValue MultiplyDecimalIeee754<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_mul`, `bid64_mul`, and `bid128_mul` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(left) || TDecimal.IsNaN(right))
            {
                return PropagateNaN<TDecimal, TValue>(left, right);
            }

            // The sign of a product is always the exclusive-or of the operand signs, including zeros.
            bool resultSign = TDecimal.IsNegative(left) ^ TDecimal.IsNegative(right);

            bool leftInfinity = TDecimal.IsInfinity(left);
            bool rightInfinity = TDecimal.IsInfinity(right);

            if (leftInfinity || rightInfinity)
            {
                // Infinity multiplied by zero is invalid (NaN); any other product involving an infinity is
                // an infinity carrying the exclusive-or sign.
                bool otherZero = (leftInfinity && !rightInfinity && TValue.IsZero(UnpackDecimalIeee754<TDecimal, TValue>(right).Significand))
                              || (rightInfinity && !leftInfinity && TValue.IsZero(UnpackDecimalIeee754<TDecimal, TValue>(left).Significand));

                if (otherZero)
                {
                    // An invalid operation produces the canonical quiet NaN, which the Intel reference
                    // emits with a positive sign and empty payload (`NaNMask`), unlike the negative
                    // `TDecimal.NaN` constant.
                    return TDecimal.NaNMask;
                }

                return resultSign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(left);
            DecodedDecimalIeee754<TValue> b = UnpackDecimalIeee754<TDecimal, TValue>(right);

            int productExponent = a.UnbiasedExponent + b.UnbiasedExponent;

            if (TValue.IsZero(a.Significand) || TValue.IsZero(b.Significand))
            {
                // Zero times a finite value is zero. The preferred exponent is the sum of the operand
                // exponents (clamped to the representable range by the encoder).
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, TValue.Zero, productExponent);
            }

            // The exact product of two coefficients needs up to twice the format precision, so it is computed at
            // double the integer width. The sum of the operand exponents is also the IEEE 754 preferred exponent
            // (trailing zeros of the product are retained), so it is fed unchanged into the shared word-level
            // rounding/encoding path.
            WideMultiply(a.Significand, b.Significand, out TValue productHigh, out TValue productLow);

            return NumberToDecimalIeee754BitsFromWide<TDecimal, TValue>(resultSign, productHigh, productLow, productExponent, sticky: false);
        }

        /// <summary>
        /// Computes <c>(left × right) + addend</c> for three IEEE 754 decimal values represented by their raw bit
        /// patterns, rounds the exact result once (round-to-nearest, ties-to-even), and returns its bit pattern.
        /// </summary>
        /// <remarks>
        /// The exact product coefficient (up to twice the format precision) is computed at double integer width, so the
        /// product is never rounded before the addend is combined with it. The product and addend are aligned to a
        /// common exponent within a window wide enough to preserve every digit that can influence the result (including
        /// the deepest cancellation); digits below the window are folded into a sticky flag and the aligned coefficients
        /// are fed into the shared word-level rounding path, producing the same result as a single rounding of the exact
        /// value. This mirrors the mathematical behavior of the Intel reference implementation; a faithful port of its
        /// reciprocal-multiply rounding is a possible future performance optimization.
        /// </remarks>
        internal static TValue FusedMultiplyAddDecimalIeee754<TDecimal, TValue>(TValue x, TValue y, TValue z)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_fma`, `bid64_fma`, and `bid128_fma` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // A NaN operand propagates its (quieted) payload. The Intel reference inspects the operands in the order
            // y, then z, then x, so the first NaN in that order supplies the sign and payload of the result.
            if (TDecimal.IsNaN(y))
            {
                return PropagateNaN<TDecimal, TValue>(y, y);
            }

            if (TDecimal.IsNaN(z))
            {
                return PropagateNaN<TDecimal, TValue>(z, z);
            }

            if (TDecimal.IsNaN(x))
            {
                return PropagateNaN<TDecimal, TValue>(x, x);
            }

            // The product sign is the exclusive-or of the factor signs, including zeros and infinities.
            bool productSign = TDecimal.IsNegative(x) ^ TDecimal.IsNegative(y);

            bool xInfinity = TDecimal.IsInfinity(x);
            bool yInfinity = TDecimal.IsInfinity(y);

            if (xInfinity || yInfinity)
            {
                // Infinity multiplied by zero is invalid (NaN); the Intel reference emits the canonical quiet NaN.
                bool otherZero = (xInfinity && !yInfinity && TValue.IsZero(UnpackDecimalIeee754<TDecimal, TValue>(y).Significand))
                              || (yInfinity && !xInfinity && TValue.IsZero(UnpackDecimalIeee754<TDecimal, TValue>(x).Significand));

                if (otherZero)
                {
                    return TDecimal.NaNMask;
                }

                // The product is an infinity. Adding an infinity of the opposite sign is invalid (NaN); every other
                // addend leaves the product's infinity unchanged (canonicalized).
                if (TDecimal.IsInfinity(z) && (TDecimal.IsNegative(z) != productSign))
                {
                    return TDecimal.NaNMask;
                }

                return productSign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            // The product is finite. A finite product plus an infinite addend is that infinity (canonicalized).
            if (TDecimal.IsInfinity(z))
            {
                return TDecimal.IsNegative(z) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> dx = UnpackDecimalIeee754<TDecimal, TValue>(x);
            DecodedDecimalIeee754<TValue> dy = UnpackDecimalIeee754<TDecimal, TValue>(y);
            DecodedDecimalIeee754<TValue> dz = UnpackDecimalIeee754<TDecimal, TValue>(z);

            int productExponent = dx.UnbiasedExponent + dy.UnbiasedExponent;
            bool productZero = TValue.IsZero(dx.Significand) || TValue.IsZero(dy.Significand);

            bool addendSign = dz.Signed;
            int addendExponent = dz.UnbiasedExponent;
            TValue addendSignificand = dz.Significand;
            bool addendZero = TValue.IsZero(addendSignificand);

            if (productZero)
            {
                // A zero product reduces the result to the addend at the preferred (smaller) exponent, matching the
                // zero handling in addition (the product's preferred exponent is the sum of the factor exponents).
                if (addendZero)
                {
                    bool bothNegative = productSign == addendSign && productSign;
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(bothNegative, TValue.Zero, Math.Min(productExponent, addendExponent));
                }

                if (productExponent >= addendExponent)
                {
                    return z;
                }

                int addendDigits = TDecimal.CountDigits(addendSignificand);
                int pad = Math.Min(addendExponent - productExponent, TDecimal.Precision - addendDigits);

                // The product's exponent can be far below the minimum quantum, so bound the padding so the
                // result stays at or above it; a zero product cannot push the exact addend into subnormal range.
                pad = Math.Min(pad, addendExponent - TDecimal.MinAdjustedExponent);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(addendSign, addendSignificand * TDecimal.Power10(pad), addendExponent - pad);
            }

            // The exact product coefficient occupies up to twice the format precision and is held at double width.
            WideMultiply(dx.Significand, dy.Significand, out TValue productHigh, out TValue productLow);
            int productDigits = WideDigitCount<TDecimal, TValue>(productHigh, productLow);

            if (addendZero)
            {
                // Adding a zero lowers the preferred exponent toward the addend's exponent, bounded by the product
                // exponent; the alignment below realizes it by scaling the product's coefficient up (padding zeros).
                addendExponent = Math.Min(addendExponent, productExponent);
            }

            int productMsd = productExponent + productDigits - 1;
            int addendDigitsCount = addendZero ? 0 : TDecimal.CountDigits(addendSignificand);
            int addendMsd = addendZero ? int.MinValue : addendExponent + addendDigitsCount - 1;

            int maxMsd = Math.Max(productMsd, addendMsd);
            int minLsd = Math.Min(productExponent, addendExponent);

            // Retain a window wide enough to hold the full product (up to 2*Precision digits) plus guard digits, which
            // also covers the deepest possible cancellation. When the operands are far enough apart that the window
            // cannot reach the lower one, its digits fall below the retained range and only contribute stickiness.
            int retain = (2 * TDecimal.Precision) + 2;
            int commonExponent = Math.Max(minLsd, maxMsd - retain + 1);

            bool sticky = false;

            TValue aHigh = productHigh;
            TValue aLow = productLow;
            AlignWideToCommonExponent<TDecimal, TValue>(ref aHigh, ref aLow, productExponent, commonExponent, ref sticky);

            TValue bHigh = TValue.Zero;
            TValue bLow = addendSignificand;

            if (!addendZero)
            {
                AlignWideToCommonExponent<TDecimal, TValue>(ref bHigh, ref bLow, addendExponent, commonExponent, ref sticky);
            }

            bool resultSign;
            TValue resultHigh;
            TValue resultLow;

            if (productSign == addendSign)
            {
                // Magnitudes add.
                resultHigh = aHigh + bHigh;
                resultLow = aLow + bLow;

                if (resultLow < aLow)
                {
                    resultHigh += TValue.One;
                }

                resultSign = productSign;
            }
            else
            {
                int comparison = WideCompare(aHigh, aLow, bHigh, bLow);

                if (comparison == 0)
                {
                    // The retained magnitudes cancel exactly. Capping only drops digits when the operands are far
                    // enough apart that one strictly dominates, so an exact cancellation here carries no sticky tail
                    // and yields +0 under round-to-nearest.
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(false, TValue.Zero, commonExponent);
                }

                if (comparison > 0)
                {
                    WideSubtract(aHigh, aLow, bHigh, bLow, out resultHigh, out resultLow);
                    resultSign = productSign;
                }
                else
                {
                    WideSubtract(bHigh, bLow, aHigh, aLow, out resultHigh, out resultLow);
                    resultSign = addendSign;
                }

                if (sticky)
                {
                    // The dropped tail belongs to the smaller (subtrahend) magnitude, so the exact difference is one
                    // unit smaller with a non-zero fractional remainder retained in the sticky flag for rounding.
                    if (TValue.IsZero(resultLow))
                    {
                        resultHigh -= TValue.One;
                    }

                    resultLow -= TValue.One;
                }
            }

            if (TValue.IsZero(resultHigh) && TValue.IsZero(resultLow) && !sticky)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(false, TValue.Zero, commonExponent);
            }

            return NumberToDecimalIeee754BitsFromWide<TDecimal, TValue>(resultSign, resultHigh, resultLow, commonExponent, sticky);
        }

        /// <summary>
        /// Divides two IEEE 754 decimal values represented by their raw bit patterns and returns the
        /// bit pattern of the correctly rounded (round-to-nearest, ties-to-even) quotient.
        /// </summary>
        /// <remarks>
        /// The dividend coefficient is scaled up by a power of ten large enough to expose more than the format
        /// precision in significant quotient digits, then long-divided by the divisor coefficient one decimal digit
        /// at a time. The running remainder and the quotient each stay within a single limb, so the division uses only
        /// word-level integer arithmetic. The final remainder determines whether the result is exact; an inexact
        /// result carries a sticky flag into the shared rounding path, while an exact result has its exponent raised
        /// toward the IEEE 754 preferred exponent (the difference of the operand exponents) by discarding trailing
        /// zeros. This mirrors the mathematical behavior of the Intel reference implementation.
        /// </remarks>
        internal static TValue DivideDecimalIeee754<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_div`, `bid64_div`, and `bid128_div` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(left) || TDecimal.IsNaN(right))
            {
                return PropagateNaN<TDecimal, TValue>(left, right);
            }

            // The sign of a quotient is always the exclusive-or of the operand signs, including zeros.
            bool resultSign = TDecimal.IsNegative(left) ^ TDecimal.IsNegative(right);

            if (TDecimal.IsInfinity(left))
            {
                // Infinity divided by infinity is invalid (NaN); infinity divided by any finite value is
                // an infinity carrying the exclusive-or sign.
                if (TDecimal.IsInfinity(right))
                {
                    // An invalid operation produces the canonical quiet NaN, which the Intel reference
                    // emits with a positive sign and empty payload (`NaNMask`), unlike the negative
                    // `TDecimal.NaN` constant.
                    return TDecimal.NaNMask;
                }

                return resultSign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            if (TDecimal.IsInfinity(right))
            {
                // A finite value divided by infinity is zero with the exclusive-or sign; the preferred
                // exponent is the minimum (the encoder clamps the sub-minimum exponent up to it).
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, TValue.Zero, TDecimal.MinAdjustedExponent);
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(left);
            DecodedDecimalIeee754<TValue> b = UnpackDecimalIeee754<TDecimal, TValue>(right);

            if (TValue.IsZero(b.Significand))
            {
                // Zero divided by zero is invalid (NaN); any other value divided by zero is an infinity
                // carrying the exclusive-or sign (division by zero).
                if (TValue.IsZero(a.Significand))
                {
                    // An invalid operation produces the canonical quiet NaN, which the Intel reference
                    // emits with a positive sign and empty payload (`NaNMask`), unlike the negative
                    // `TDecimal.NaN` constant.
                    return TDecimal.NaNMask;
                }

                return resultSign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            if (TValue.IsZero(a.Significand))
            {
                // Zero divided by a finite non-zero value is zero. The preferred exponent is the difference
                // of the operand exponents (clamped to the representable range by the encoder).
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, TValue.Zero, a.UnbiasedExponent - b.UnbiasedExponent);
            }

            int dividendDigits = TDecimal.CountDigits(a.Significand);
            int divisorDigits = TDecimal.CountDigits(b.Significand);

            // Scale the dividend up so the quotient exposes at least Precision + 1 significant digits.
            // Because both coefficients have at most Precision digits, this shift is always positive.
            int shift = divisorDigits - dividendDigits + TDecimal.Precision + 1;
            Debug.Assert(shift > 0);

            int quotientExponent = a.UnbiasedExponent - b.UnbiasedExponent - shift;

            // Long-divide `a.Significand * 10^shift` by `b.Significand` one decimal digit at a time. The running
            // remainder always stays below the divisor and the quotient always stays below 10^(Precision + 2), so
            // every intermediate value fits a single limb and no wide integer is needed for division. The final
            // remainder determines whether the quotient is exact.
            TValue divisor = b.Significand;
            TValue ten = TValue.CreateTruncating(10);
            (TValue quotient, TValue remainder) = TValue.DivRem(a.Significand, divisor);

            for (int i = 0; i < shift; i++)
            {
                remainder *= ten;
                (TValue digit, remainder) = TValue.DivRem(remainder, divisor);
                quotient = (quotient * ten) + digit;
            }

            bool remainderNonZero = !TValue.IsZero(remainder);

            if (!remainderNonZero)
            {
                // The quotient is exact, so raise its exponent toward the preferred exponent (the difference
                // of the operand exponents) by discarding trailing zeros.
                int idealExponent = a.UnbiasedExponent - b.UnbiasedExponent;

                while (quotientExponent < idealExponent)
                {
                    (TValue stripped, TValue digit) = TValue.DivRem(quotient, ten);

                    if (!TValue.IsZero(digit))
                    {
                        break;
                    }

                    quotient = stripped;
                    quotientExponent++;
                }
            }

            return NumberToDecimalIeee754BitsFromWide<TDecimal, TValue>(resultSign, TValue.Zero, quotient, quotientExponent, remainderNonZero);
        }

        /// <summary>
        /// Computes the truncated remainder (the <c>%</c> operator) of two IEEE 754 decimal values represented by their
        /// raw bit patterns: <c>x - Truncate(x / y) * y</c>, matching the C# floating-point <c>%</c> operator on
        /// <see cref="double"/>/<see cref="float"/>/<see cref="Half"/> (and <em>not</em> the round-to-nearest
        /// IEEE 754 <c>remainder</c> operation).
        /// </summary>
        /// <remarks>
        /// The result carries the sign of the dividend, has magnitude strictly less than <c>|y|</c>, and is always
        /// exact. It is computed at the IEEE 754 preferred exponent <c>min(exp(x), exp(y))</c> by reducing the
        /// dividend coefficient modulo the divisor coefficient. Every intermediate value stays within a single limb:
        /// the running remainder is always below the divisor, so each step scales it up by as many trailing zeros as
        /// keep the product within the integer width (capped at the largest cached power of ten) before taking the
        /// remainder again, stopping once the remainder reaches zero. This mirrors the behavior of the Intel reference
        /// implementation.
        /// </remarks>
        internal static TValue RemainderDecimalIeee754<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>, IMinMaxValue<TValue>
        {
            // This code is based on `bid32_fmod`, `bid64_fmod`, and `bid128_fmod` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(left) || TDecimal.IsNaN(right))
            {
                return PropagateNaN<TDecimal, TValue>(left, right);
            }

            // The remainder always carries the sign of the dividend.
            bool resultSign = TDecimal.IsNegative(left);

            if (TDecimal.IsInfinity(left))
            {
                // Infinity has no finite remainder; the operation is invalid and produces the canonical quiet NaN.
                return TDecimal.NaNMask;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(left);

            if (TDecimal.IsInfinity(right))
            {
                // A finite value has itself as its remainder modulo infinity; re-encode to a canonical form.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, a.Significand, a.UnbiasedExponent);
            }

            DecodedDecimalIeee754<TValue> b = UnpackDecimalIeee754<TDecimal, TValue>(right);

            if (TValue.IsZero(b.Significand))
            {
                // A remainder with a zero divisor is invalid and produces the canonical quiet NaN.
                return TDecimal.NaNMask;
            }

            // The preferred exponent of the remainder is the smaller of the two operand exponents.
            int resultExponent = Math.Min(a.UnbiasedExponent, b.UnbiasedExponent);

            if (TValue.IsZero(a.Significand))
            {
                // Zero modulo any non-zero value is a signed zero at the preferred exponent.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, TValue.Zero, resultExponent);
            }

            TValue remainder;

            if (a.UnbiasedExponent >= b.UnbiasedExponent)
            {
                // Reduce `a.Significand * 10^(ea - eb)` modulo `b.Significand`. The remainder always stays below the
                // divisor, so `remainder * 10^chunk` fits the integer width as long as `10^chunk <= MaxValue / divisor`.
                // Fold that many trailing zeros per step (capped at the largest cached power of ten). Modular
                // arithmetic lets each step absorb several digits at once, so a small divisor reaches the full cached
                // power while a large one folds only a handful. Once the remainder hits zero it stays zero, so stop.
                //
                // TODO: A wider intermediate (as Intel's `bidNN_fmod` uses for Decimal128) would let every step fold
                // the full `Precision` digits regardless of divisor magnitude, shaving the loop count for large gaps.
                remainder = a.Significand % b.Significand;

                int chunk = 1;
                TValue chunkLimit = TValue.MaxValue / b.Significand;

                while ((chunk < TDecimal.Precision - 1) && (TDecimal.Power10(chunk + 1) <= chunkLimit))
                {
                    chunk++;
                }

                for (int gap = a.UnbiasedExponent - b.UnbiasedExponent; (gap > 0) && !TValue.IsZero(remainder); gap -= chunk)
                {
                    int step = Math.Min(chunk, gap);
                    remainder = (remainder * TDecimal.Power10(step)) % b.Significand;
                }
            }
            else
            {
                // The divisor's coefficient is scaled up by `10^(eb - ea)`. When that scaled divisor cannot fit the
                // format precision it necessarily exceeds the dividend coefficient, so the dividend is the remainder.
                int gap = b.UnbiasedExponent - a.UnbiasedExponent;

                if (TDecimal.CountDigits(b.Significand) + gap > TDecimal.Precision)
                {
                    remainder = a.Significand;
                }
                else
                {
                    remainder = a.Significand % (b.Significand * TDecimal.Power10(gap));
                }
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, remainder, resultExponent);
        }

        /// <summary>
        /// Computes the round-to-nearest IEEE 754 <c>remainder</c> of two decimal values represented by their raw bit
        /// patterns: <c>x - y * RoundToNearestEven(x / y)</c>. Unlike the <c>%</c> operator this rounds the quotient to
        /// the nearest integer (ties to even), so the result magnitude is at most <c>|y| / 2</c> and its sign may differ
        /// from the dividend.
        /// </summary>
        /// <remarks>
        /// The truncated remainder and its integer quotient are formed exactly at the preferred exponent
        /// <c>min(exp(x), exp(y))</c> using the same coefficient reduction as the <c>%</c> operator. When twice the
        /// truncated remainder exceeds the divisor coefficient, or equals it while the quotient is odd, the divisor is
        /// subtracted and the sign flipped to land on the nearest multiple. Only the final reduction step's quotient
        /// determines parity: every earlier partial quotient is scaled by a positive power of ten and is therefore even.
        /// </remarks>
        internal static TValue Ieee754RemainderDecimalIeee754<TDecimal, TValue>(TValue left, TValue right)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>, IMinMaxValue<TValue>
        {
            // This code is based on `bid32_rem`, `bid64_rem`, and `bid128_rem` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(left) || TDecimal.IsNaN(right))
            {
                return PropagateNaN<TDecimal, TValue>(left, right);
            }

            bool resultSign = TDecimal.IsNegative(left);

            if (TDecimal.IsInfinity(left))
            {
                // Infinity has no finite remainder; the operation is invalid and produces the canonical quiet NaN.
                return TDecimal.NaNMask;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(left);

            if (TDecimal.IsInfinity(right))
            {
                // A finite value has itself as its remainder modulo infinity; re-encode to a canonical form.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, a.Significand, a.UnbiasedExponent);
            }

            DecodedDecimalIeee754<TValue> b = UnpackDecimalIeee754<TDecimal, TValue>(right);

            if (TValue.IsZero(b.Significand))
            {
                // A remainder with a zero divisor is invalid and produces the canonical quiet NaN.
                return TDecimal.NaNMask;
            }

            // The preferred exponent of the remainder is the smaller of the two operand exponents.
            int resultExponent = Math.Min(a.UnbiasedExponent, b.UnbiasedExponent);

            if (TValue.IsZero(a.Significand))
            {
                // Zero modulo any non-zero value is a signed zero at the preferred exponent.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, TValue.Zero, resultExponent);
            }

            // `remainder` is the truncated remainder and `divisor` the divisor coefficient, both at the preferred
            // exponent; `quotientIsOdd` carries the parity of the full integer quotient for the ties-to-even rule.
            TValue remainder;
            TValue divisor;
            bool quotientIsOdd;

            if (a.UnbiasedExponent >= b.UnbiasedExponent)
            {
                // Reduce `a.Significand * 10^(ea - eb)` modulo `b.Significand`, folding as many trailing zeros per step
                // as keep the product within the integer width (capped at the largest cached power of ten). Each step
                // tracks its quotient; the parity of the last one equals the parity of the full quotient because the
                // earlier partial quotients are each scaled by a later positive power of ten.
                divisor = b.Significand;

                TValue quotient = a.Significand / b.Significand;
                remainder = a.Significand - (quotient * b.Significand);

                int chunk = 1;
                TValue chunkLimit = TValue.MaxValue / b.Significand;

                while ((chunk < TDecimal.Precision - 1) && (TDecimal.Power10(chunk + 1) <= chunkLimit))
                {
                    chunk++;
                }

                for (int gap = a.UnbiasedExponent - b.UnbiasedExponent; (gap > 0) && !TValue.IsZero(remainder); gap -= chunk)
                {
                    int step = Math.Min(chunk, gap);
                    TValue scaled = remainder * TDecimal.Power10(step);

                    quotient = scaled / b.Significand;
                    remainder = scaled - (quotient * b.Significand);
                }

                quotientIsOdd = !TValue.IsZero(quotient & TValue.One);
            }
            else
            {
                // The divisor is scaled up by `10^(eb - ea)`. Once the scaled divisor has more than one digit beyond the
                // format precision it exceeds twice the dividend, so the dividend is already the nearest remainder.
                int gap = b.UnbiasedExponent - a.UnbiasedExponent;

                if (TDecimal.CountDigits(b.Significand) + gap > TDecimal.Precision + 1)
                {
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, a.Significand, resultExponent);
                }

                // The scaled divisor has at most `Precision + 1` digits. Peel one factor of ten so the cached
                // power-of-ten lookup stays within its table, which only spans exponents below `Precision`.
                divisor = (b.Significand * TDecimal.Power10(gap - 1)) * TDecimal.Power10(1);

                TValue quotient = a.Significand / divisor;
                remainder = a.Significand - (quotient * divisor);
                quotientIsOdd = !TValue.IsZero(quotient & TValue.One);
            }

            // Round the quotient to nearest, ties to even: move to the nearer multiple of the divisor when the truncated
            // remainder is past the halfway point, or exactly halfway with an odd quotient. The subtraction flips the sign.
            TValue twiceRemainder = remainder + remainder;

            if ((twiceRemainder > divisor) || ((twiceRemainder == divisor) && quotientIsOdd))
            {
                remainder = divisor - remainder;
                resultSign = !resultSign;
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(resultSign, remainder, resultExponent);
        }

        /// <summary>
        /// Rounds a finite value to <paramref name="digits"/> fractional digits under <paramref name="mode"/>.
        /// The value is <c>coefficient * 10^exponent</c>; when its quantum exponent is already at or above
        /// <c>-digits</c> there is nothing to round and it is returned unchanged. Otherwise the digits below
        /// <c>10^(-digits)</c> are discarded and the retained coefficient is incremented per <paramref name="mode"/>.
        /// Every intermediate fits a single limb: the divisor is at most <c>10^(Precision - 1)</c> and dividing by
        /// at least ten keeps the rounded coefficient below <c>MaxSignificand</c>.
        /// </summary>
        internal static TValue RoundDecimalIeee754<TDecimal, TValue>(TValue bits, int digits, MidpointRounding mode)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (digits < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange_RoundingDigits(nameof(digits));
            }

            if ((uint)mode > (uint)MidpointRounding.ToPositiveInfinity)
            {
                ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
            }

            if (TDecimal.IsNaN(bits))
            {
                // Canonicalize so a signaling or out-of-range-payload NaN operand rounds to the canonical quiet NaN.
                return PropagateNaN<TDecimal, TValue>(bits, bits);
            }

            if (TDecimal.IsInfinity(bits))
            {
                // Canonicalize so a non-canonical infinity operand rounds to the canonical infinity.
                return TDecimal.IsNegative(bits) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(bits);
            int targetExponent = -digits;

            if (a.UnbiasedExponent >= targetExponent)
            {
                // The quantum is already at or coarser than the requested precision; nothing is discarded. Re-encode so
                // a non-canonical operand (coefficient above the format maximum, unpacked to zero) is returned canonical.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, a.Significand, a.UnbiasedExponent);
            }

            int drop = targetExponent - a.UnbiasedExponent;
            int coefficientDigits = TValue.IsZero(a.Significand) ? 0 : TDecimal.CountDigits(a.Significand);

            TValue five = TValue.CreateTruncating(5);
            TValue quotient;

            // Sign of (discarded - half quantum): negative below the midpoint, zero at it, positive above it.
            int discardedComparedToHalf;
            bool discardedNonZero;

            if (drop >= coefficientDigits)
            {
                // Every coefficient digit is discarded, so the retained value is zero before rounding.
                quotient = TValue.Zero;

                if (drop > coefficientDigits)
                {
                    // The whole coefficient is strictly less than half of the discarded quantum.
                    discardedComparedToHalf = -1;
                    discardedNonZero = !TValue.IsZero(a.Significand);
                }
                else
                {
                    TValue half = five * TDecimal.Power10(coefficientDigits - 1);
                    discardedComparedToHalf = a.Significand.CompareTo(half);
                    discardedNonZero = true;
                }
            }
            else
            {
                TValue divisor = TDecimal.Power10(drop);
                TValue discarded;
                (quotient, discarded) = TValue.DivRem(a.Significand, divisor);

                TValue half = five * TDecimal.Power10(drop - 1);
                discardedComparedToHalf = discarded.CompareTo(half);
                discardedNonZero = !TValue.IsZero(discarded);
            }

            bool roundAwayFromZero = mode switch
            {
                MidpointRounding.ToEven => (discardedComparedToHalf > 0) || ((discardedComparedToHalf == 0) && !TValue.IsZero(quotient & TValue.One)),
                MidpointRounding.AwayFromZero => discardedComparedToHalf >= 0,
                MidpointRounding.ToZero => false,
                MidpointRounding.ToNegativeInfinity => discardedNonZero && a.Signed,
                MidpointRounding.ToPositiveInfinity => discardedNonZero && !a.Signed,
                _ => throw new UnreachableException(),
            };

            if (roundAwayFromZero)
            {
                quotient += TValue.One;
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, quotient, targetExponent);
        }

        /// <summary>
        /// Computes the integer base-10 logarithm of a value: the exponent of its most significant digit. The special
        /// cases match <see cref="Math.ILogB(double)"/>, reporting <see cref="int.MinValue"/> for zero and
        /// <see cref="int.MaxValue"/> for both NaN and infinity.
        /// </summary>
        internal static int ILogBDecimalIeee754<TDecimal, TValue>(TValue bits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!TDecimal.IsFinite(bits))
            {
                return int.MaxValue;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(bits);

            if (TValue.IsZero(a.Significand))
            {
                return int.MinValue;
            }

            return a.UnbiasedExponent + TDecimal.CountDigits(a.Significand) - 1;
        }

        /// <summary>
        /// Multiplies a value by <c>10^<paramref name="n"/></c>. A surplus exponent is absorbed into trailing zeros of
        /// the coefficient while it still fits the format precision; anything beyond that overflows to a signed infinity.
        /// A deficit exponent rounds the coefficient (to nearest, ties to even) up to the minimum quantum, underflowing
        /// gradually to a subnormal or a signed zero.
        /// </summary>
        internal static TValue ScaleBDecimalIeee754<TDecimal, TValue>(TValue bits, int n)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_scalbn`, `bid64_scalbn`, and `bid128_scalbn` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(bits))
            {
                return PropagateNaN<TDecimal, TValue>(bits, bits);
            }

            if (TDecimal.IsInfinity(bits))
            {
                // Canonicalize so a non-canonical infinity operand scales to the canonical infinity.
                return TDecimal.IsNegative(bits) ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(bits);
            long exponent = (long)a.UnbiasedExponent + n;

            if (TValue.IsZero(a.Significand))
            {
                // Zero carries no significant digits, so the quantum simply clamps into the representable range.
                int zeroExponent = (int)Math.Clamp(exponent, TDecimal.MinAdjustedExponent, TDecimal.MaxAdjustedExponent);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, TValue.Zero, zeroExponent);
            }

            if (exponent > TDecimal.MaxAdjustedExponent)
            {
                // Absorb the surplus into trailing zeros while the coefficient stays within the format precision.
                long surplus = exponent - TDecimal.MaxAdjustedExponent;

                if (surplus <= TDecimal.Precision - TDecimal.CountDigits(a.Significand))
                {
                    TValue significand = a.Significand * TDecimal.Power10((int)surplus);
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, significand, TDecimal.MaxAdjustedExponent);
                }

                return a.Signed ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            if (exponent < TDecimal.MinAdjustedExponent)
            {
                // Raise the quantum to the minimum by discarding low-order digits, rounding to nearest with ties to even.
                long drop = TDecimal.MinAdjustedExponent - exponent;
                int coefficientDigits = TDecimal.CountDigits(a.Significand);

                if (drop > coefficientDigits)
                {
                    // Even the most significant digit sits below half the minimum quantum, so the result is a signed zero.
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, TValue.Zero, TDecimal.MinAdjustedExponent);
                }

                TValue five = TValue.CreateTruncating(5);
                TValue quotient;
                int discardedComparedToHalf;

                if (drop == coefficientDigits)
                {
                    // The entire coefficient is discarded; compare it against half of the discarded quantum.
                    quotient = TValue.Zero;
                    TValue half = five * TDecimal.Power10(coefficientDigits - 1);
                    discardedComparedToHalf = a.Significand.CompareTo(half);
                }
                else
                {
                    TValue divisor = TDecimal.Power10((int)drop);
                    quotient = a.Significand / divisor;
                    TValue discarded = a.Significand - (quotient * divisor);
                    TValue half = five * TDecimal.Power10((int)drop - 1);
                    discardedComparedToHalf = discarded.CompareTo(half);
                }

                if ((discardedComparedToHalf > 0) || ((discardedComparedToHalf == 0) && !TValue.IsZero(quotient & TValue.One)))
                {
                    quotient += TValue.One;
                }

                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, quotient, TDecimal.MinAdjustedExponent);
            }

            // The shifted quantum is already representable, so the coefficient is preserved exactly.
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, a.Significand, (int)exponent);
        }

        /// <summary>
        /// Returns the least value that compares greater than <paramref name="bits"/> (IEEE 754 <c>nextUp</c>). NaN is
        /// returned unchanged, positive infinity is its own successor, and negative infinity steps to the most negative
        /// finite value.
        /// </summary>
        internal static TValue BitIncrementDecimalIeee754<TDecimal, TValue>(TValue bits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This code is based on `bid32_nextup`, `bid64_nextup`, and `bid128_nextup` from Intel(R) Decimal Floating-Point Math Library
            // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (TDecimal.IsNaN(bits))
            {
                return PropagateNaN<TDecimal, TValue>(bits, bits);
            }

            if (TDecimal.IsInfinity(bits))
            {
                return TDecimal.IsNegative(bits)
                    ? DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: true, TDecimal.MaxSignificand, TDecimal.MaxAdjustedExponent)
                    : TDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> a = UnpackDecimalIeee754<TDecimal, TValue>(bits);

            if (TValue.IsZero(a.Significand))
            {
                // The successor of any zero is the smallest positive subnormal.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: false, TValue.One, TDecimal.MinAdjustedExponent);
            }

            int exponent = a.UnbiasedExponent;

            if (!a.Signed && (a.Significand == TDecimal.MaxSignificand) && (exponent == TDecimal.MaxAdjustedExponent))
            {
                // The successor of the largest finite value is positive infinity.
                return TDecimal.PositiveInfinity;
            }

            if (a.Signed && (a.Significand == TValue.One) && (exponent == TDecimal.MinAdjustedExponent))
            {
                // The successor of the smallest negative subnormal is negative zero.
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(signed: true, TValue.Zero, TDecimal.MinAdjustedExponent);
            }

            // Pad the coefficient toward the minimum quantum (bounded by the precision and the minimum exponent) so a
            // single ulp is the smallest representable step at this magnitude.
            TValue significand = a.Significand;
            int padding = Math.Min(TDecimal.Precision - TDecimal.CountDigits(significand), exponent - TDecimal.MinAdjustedExponent);

            if (padding > 0)
            {
                significand *= TDecimal.Power10(padding);
                exponent -= padding;
            }

            if (!a.Signed)
            {
                // Stepping away from zero adds one ulp, carrying into the next exponent at the precision boundary.
                significand += TValue.One;

                if (significand > TDecimal.MaxSignificand)
                {
                    significand = TDecimal.Power10(TDecimal.Precision - 1);
                    exponent++;
                }
            }
            else
            {
                // Stepping toward zero subtracts one ulp, borrowing from the next exponent at the precision boundary.
                significand -= TValue.One;

                if ((significand == (TDecimal.Power10(TDecimal.Precision - 1) - TValue.One)) && (exponent != TDecimal.MinAdjustedExponent))
                {
                    significand = TDecimal.MaxSignificand;
                    exponent--;
                }
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(a.Signed, significand, exponent);
        }

        /// <summary>
        /// Returns the greatest value that compares less than <paramref name="bits"/> (IEEE 754 <c>nextDown</c>),
        /// computed from the successor identity <c>nextDown(x) = -nextUp(-x)</c>.
        /// </summary>
        internal static TValue BitDecrementDecimalIeee754<TDecimal, TValue>(TValue bits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return BitIncrementDecimalIeee754<TDecimal, TValue>(bits ^ TDecimal.SignMask) ^ TDecimal.SignMask;
        }

        /// <summary>
        /// Computes the scale factor <c>10^<paramref name="exponent"/></c> used to align addition operands. Exponents
        /// within the format's <c>Power10</c> lookup range (<c>0..Precision - 1</c>) come straight from that table,
        /// matching the existing parsing/formatting paths. The slightly larger alignment exponents
        /// (<c>Precision..Precision + 2</c>, which the table does not cover) reduce the exponent by the largest table
        /// entry each iteration and finish with a single lookup for the remainder. The result always fits a single limb
        /// for every supported format.
        /// </summary>
        private static TValue AlignmentScaleFactor<TDecimal, TValue>(int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            int highestTableExponent = TDecimal.Precision - 1;
            TValue largest = TDecimal.Power10(highestTableExponent);
            TValue result = TValue.One;

            while (exponent > highestTableExponent)
            {
                result *= largest;
                exponent -= highestTableExponent;
            }

            return result * TDecimal.Power10(exponent);
        }

        /// <summary>
        /// Computes the full (double-width) product of two significands as a pair of <typeparamref name="TValue"/>
        /// limbs (<paramref name="high"/> holds the more significant half). The product of two coefficients can
        /// require up to twice the format precision, which always fits in two limbs of the underlying integer width.
        /// </summary>
        private static void WideMultiply<TValue>(TValue left, TValue right, out TValue high, out TValue low)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // For the 32-bit and 64-bit formats the full product fits in a single wider C# integer (ulong and
            // UInt128 respectively), so it comes from one native multiply. The 128-bit format has no wider
            // native type and uses the schoolbook half-limb decomposition below.
            if (typeof(TValue) == typeof(uint))
            {
                ulong product = (ulong)uint.CreateTruncating(left) * uint.CreateTruncating(right);
                high = TValue.CreateTruncating(product >> 32);
                low = TValue.CreateTruncating(product);
                return;
            }
            else if (typeof(TValue) == typeof(ulong))
            {
                UInt128 product = (UInt128)ulong.CreateTruncating(left) * ulong.CreateTruncating(right);
                high = TValue.CreateTruncating(product >> 64);
                low = TValue.CreateTruncating(product);
                return;
            }

            int bits = TValue.Zero.GetByteCount() * 8;
            int half = bits / 2;
            TValue lowMask = (TValue.One << half) - TValue.One;

            TValue leftLow = left & lowMask;
            TValue leftHigh = left >> half;
            TValue rightLow = right & lowMask;
            TValue rightHigh = right >> half;

            TValue lowLow = leftLow * rightLow;
            TValue lowHigh = leftLow * rightHigh;
            TValue highLow = leftHigh * rightLow;
            TValue highHigh = leftHigh * rightHigh;

            TValue cross = (lowLow >> half) + (lowHigh & lowMask) + (highLow & lowMask);

            low = (lowLow & lowMask) | ((cross & lowMask) << half);
            high = highHigh + (lowHigh >> half) + (highLow >> half) + (cross >> half);
        }

        /// <summary>
        /// Multiplies the double-width value in (<paramref name="high"/>, <paramref name="low"/>) by a single-limb
        /// <paramref name="factor"/> in place. The caller guarantees the scaled result still fits two limbs.
        /// </summary>
        private static void WideMultiplyByLimb<TValue>(ref TValue high, ref TValue low, TValue factor)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            WideMultiply(low, factor, out TValue lowHigh, out TValue lowLow);
            WideMultiply(high, factor, out TValue highHigh, out TValue highLow);

            TValue newHigh = highLow + lowHigh;

            // The result fits two limbs by construction: the high limb never carries out and `high * factor` never
            // spills past the second limb.
            Debug.Assert(TValue.IsZero(highHigh));
            Debug.Assert(newHigh >= highLow);

            high = newHigh;
            low = lowLow;
        }

        /// <summary>
        /// Scales the double-width value in (<paramref name="high"/>, <paramref name="low"/>) up by
        /// <c>10^<paramref name="power"/></c> in place, multiplying by the largest single-limb power of ten each step.
        /// The caller guarantees the scaled result still fits two limbs.
        /// </summary>
        private static void WideScaleByPow10<TDecimal, TValue>(ref TValue high, ref TValue low, int power)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            int highestTableExponent = TDecimal.Precision - 1;

            while (power > 0)
            {
                int chunk = Math.Min(power, highestTableExponent);
                WideMultiplyByLimb(ref high, ref low, TDecimal.Power10(chunk));
                power -= chunk;
            }
        }

        /// <summary>
        /// Drops the <paramref name="dropCount"/> least-significant decimal digits from the double-width value in
        /// (<paramref name="high"/>, <paramref name="low"/>) in place, folding every dropped digit into
        /// <paramref name="sticky"/>. Used to align an addition operand whose low digits fall below the retained window,
        /// where all dropped digits lie below the rounding position and therefore only contribute stickiness.
        /// </summary>
        private static void WideDropLowDigits<TValue>(ref TValue high, ref TValue low, int dropCount, ref bool sticky)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            for (int i = 0; i < dropCount; i++)
            {
                if (TValue.IsZero(high) && TValue.IsZero(low))
                {
                    break;
                }

                sticky |= WideDivideByTen(ref high, ref low) != 0;
            }
        }

        /// <summary>
        /// Aligns the double-width coefficient in (<paramref name="high"/>, <paramref name="low"/>), whose value is
        /// <c>coefficient·10^<paramref name="exponent"/></c>, to <paramref name="commonExponent"/> in place. A larger
        /// exponent scales the coefficient up (exact); a smaller exponent drops the low digits into
        /// <paramref name="sticky"/>.
        /// </summary>
        private static void AlignWideToCommonExponent<TDecimal, TValue>(ref TValue high, ref TValue low, int exponent, int commonExponent, ref bool sticky)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (exponent > commonExponent)
            {
                WideScaleByPow10<TDecimal, TValue>(ref high, ref low, exponent - commonExponent);
            }
            else if (exponent < commonExponent)
            {
                WideDropLowDigits(ref high, ref low, commonExponent - exponent, ref sticky);
            }
        }

        /// <summary>
        /// Compares two double-width magnitudes, returning a negative value, zero, or a positive value according to
        /// whether the first is less than, equal to, or greater than the second.
        /// </summary>
        private static int WideCompare<TValue>(TValue leftHigh, TValue leftLow, TValue rightHigh, TValue rightLow)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            int highComparison = leftHigh.CompareTo(rightHigh);
            return highComparison != 0 ? highComparison : leftLow.CompareTo(rightLow);
        }

        /// <summary>
        /// Subtracts the double-width magnitude (<paramref name="rightHigh"/>, <paramref name="rightLow"/>) from
        /// (<paramref name="leftHigh"/>, <paramref name="leftLow"/>), which the caller guarantees is the larger.
        /// </summary>
        private static void WideSubtract<TValue>(TValue leftHigh, TValue leftLow, TValue rightHigh, TValue rightLow, out TValue high, out TValue low)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            high = leftHigh - rightHigh;

            if (leftLow < rightLow)
            {
                high -= TValue.One;
            }

            low = leftLow - rightLow;
        }

        /// <summary>
        /// Divides the double-width value in (<paramref name="high"/>, <paramref name="low"/>) by ten in place,
        /// returning the discarded least-significant decimal digit. Used to strip low-order digits during rounding.
        /// </summary>
        /// <remarks>
        /// Only the 128-bit format reaches this helper: the 32-bit and 64-bit formats widen the limb pair to a
        /// single native integer and divide directly (see <see cref="DropDigits{TValue}"/> and
        /// <see cref="WideDigitCount{TDecimal, TValue}"/>). The Intel reference implementation avoids hardware
        /// division here by multiplying with precomputed reciprocals of powers of ten (e.g.
        /// <c>bid_reciprocals10_64</c>) and shifting; this helper instead uses direct integer division for
        /// simplicity, and adopting the reciprocal-multiply tables is a possible future performance optimization.
        /// </remarks>
        private static int WideDivideByTen<TValue>(ref TValue high, ref TValue low)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ten = TValue.CreateTruncating(10);

            if (TValue.IsZero(high))
            {
                (TValue quotient, TValue remainder) = TValue.DivRem(low, ten);
                low = quotient;
                return int.CreateTruncating(remainder);
            }

            int bits = TValue.Zero.GetByteCount() * 8;
            int half = bits / 2;
            TValue lowMask = (TValue.One << half) - TValue.One;
            TValue baseValue = TValue.One << half;

            // Long division of the four half-width limbs (most significant first) by ten. Because the running
            // remainder stays below ten, `remainder * baseValue + limb` never exceeds the integer width.
            TValue rem = TValue.Zero;
            (TValue q3, rem) = TValue.DivRem((rem * baseValue) + (high >> half), ten);
            (TValue q2, rem) = TValue.DivRem((rem * baseValue) + (high & lowMask), ten);
            (TValue q1, rem) = TValue.DivRem((rem * baseValue) + (low >> half), ten);
            (TValue q0, rem) = TValue.DivRem((rem * baseValue) + (low & lowMask), ten);

            high = (q3 << half) | q2;
            low = (q1 << half) | q0;
            return int.CreateTruncating(rem);
        }

        /// <summary>
        /// Counts the number of decimal digits in the double-width value (<paramref name="high"/>, <paramref name="low"/>),
        /// which must be non-zero.
        /// </summary>
        private static int WideDigitCount<TDecimal, TValue>(TValue high, TValue low)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // For the 32-bit and 64-bit formats the (high, low) limb pair fits in a single wider C# integer
            // (ulong and UInt128 respectively), so the digit count comes straight from the existing helpers
            // instead of stripping the high limb a digit at a time. The 128-bit format has no wider native
            // type and falls back to the generic limb loop below.
            if (typeof(TValue) == typeof(uint))
            {
                ulong wide = ((ulong)uint.CreateTruncating(high) << 32) | uint.CreateTruncating(low);
                return FormattingHelpers.CountDigits(wide);
            }
            else if (typeof(TValue) == typeof(ulong))
            {
                UInt128 wide = ((UInt128)ulong.CreateTruncating(high) << 64) | ulong.CreateTruncating(low);
                return FormattingHelpers.CountDigits(wide);
            }

            int count = 0;

            while (!TValue.IsZero(high))
            {
                WideDivideByTen(ref high, ref low);
                count++;
            }

            return count + TDecimal.CountDigits(low);
        }

        /// <summary>
        /// Removes the <paramref name="dropCount"/> least-significant decimal digits from the double-width value,
        /// returning the retained significand (which fits in a single limb). The most-significant removed digit is
        /// returned in <paramref name="roundDigit"/> for the rounding decision; all lower removed digits are folded
        /// into <paramref name="sticky"/>.
        /// </summary>
        private static TValue DropDigits<TValue>(ref TValue high, ref TValue low, int dropCount, ref bool sticky, out int roundDigit)
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            roundDigit = 0;

            if (dropCount == 0)
            {
                return low;
            }

            // For the 32-bit and 64-bit formats the (high, low) limb pair fits in a single wider C# integer
            // (ulong and UInt128 respectively), so the requested digits are dropped with one native division
            // by 10^dropCount rather than a per-digit long-division loop. The remainder holds the removed
            // low-order digits: its most-significant digit is the rounding digit and everything below it is
            // folded into the sticky bit. The 128-bit format has no wider native type and uses the generic
            // limb loop below.
            if (typeof(TValue) == typeof(uint))
            {
                ulong wide = ((ulong)uint.CreateTruncating(high) << 32) | uint.CreateTruncating(low);

                ulong scale = 1;
                for (int i = 1; i < dropCount; i++)
                {
                    scale *= 10;
                }
                ulong power = scale * 10;

                (ulong quotient, ulong remainder) = ulong.DivRem(wide, power);
                roundDigit = int.CreateTruncating(remainder / scale);
                sticky |= (remainder % scale) != 0;

                high = TValue.Zero;
                low = TValue.CreateTruncating(quotient);
                return low;
            }
            else if (typeof(TValue) == typeof(ulong))
            {
                UInt128 wide = ((UInt128)ulong.CreateTruncating(high) << 64) | ulong.CreateTruncating(low);

                UInt128 scale = UInt128.One;
                for (int i = 1; i < dropCount; i++)
                {
                    scale *= 10;
                }
                UInt128 power = scale * 10;

                (UInt128 quotient, UInt128 remainder) = UInt128.DivRem(wide, power);
                roundDigit = int.CreateTruncating(remainder / scale);
                sticky |= (remainder % scale) != UInt128.Zero;

                high = TValue.Zero;
                low = TValue.CreateTruncating(quotient);
                return low;
            }

            for (int i = 0; i < dropCount; i++)
            {
                if (i > 0)
                {
                    sticky |= roundDigit != 0;
                }

                roundDigit = WideDivideByTen(ref high, ref low);
            }

            Debug.Assert(TValue.IsZero(high));
            return low;
        }

        /// <summary>
        /// Rounds the exact double-width coefficient (<paramref name="high"/>, <paramref name="low"/>) with value
        /// <c>±coefficient·10^<paramref name="exponent"/></c> (plus an optional sub-unit <paramref name="sticky"/> tail)
        /// to the nearest representable IEEE 754 decimal value using round-to-nearest, ties-to-even, and returns its
        /// BID bit pattern. This is the integer-based counterpart of <see cref="NumberToDecimalIeee754Bits{TDecimal, TValue}"/>
        /// used by the arithmetic operators; it mirrors the same exponent-range handling without materializing a digit string.
        /// </summary>
        private static TValue NumberToDecimalIeee754BitsFromWide<TDecimal, TValue>(bool sign, TValue high, TValue low, int exponent, bool sticky)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TValue.IsZero(high) && TValue.IsZero(low))
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, TValue.Zero, exponent);
            }

            int precision = TDecimal.Precision;

            if (exponent > TDecimal.MaxExponent)
            {
                return sign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            int digitsCount = WideDigitCount<TDecimal, TValue>(high, low);

            if (exponent > TDecimal.MaxAdjustedExponent)
            {
                // The least-significant digit already sits above the largest representable quantum. The value is
                // representable only if the coefficient can be padded with trailing zeros without exceeding the
                // precision; otherwise it overflows to infinity.
                int numberZeroDigits = exponent - TDecimal.MaxAdjustedExponent;

                if (digitsCount + numberZeroDigits > precision)
                {
                    return sign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }

                TValue paddedSignificand = low * TDecimal.Power10(numberZeroDigits);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, paddedSignificand, TDecimal.MaxAdjustedExponent);
            }

            if (exponent < TDecimal.MinAdjustedExponent)
            {
                int numberDigitsRemove = TDecimal.MinAdjustedExponent - exponent;

                if (numberDigitsRemove > digitsCount)
                {
                    return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, TValue.Zero, TDecimal.MinAdjustedExponent);
                }
                else if (numberDigitsRemove < digitsCount)
                {
                    int numberDigitsRemain = digitsCount - numberDigitsRemove;

                    if (numberDigitsRemain > precision)
                    {
                        // Still above the format precision after shifting to the minimum quantum, so the value is
                        // actually normal. Round to the precision in a single step to avoid a double rounding.
                        numberDigitsRemain = precision;
                    }

                    return RoundWideToSignificand<TDecimal, TValue>(sign, high, low, digitsCount, exponent, numberDigitsRemain, sticky);
                }
                else
                {
                    return RoundWideToZeroOrEpsilon<TDecimal, TValue>(sign, high, low, digitsCount, sticky);
                }
            }

            if (digitsCount > precision)
            {
                int numberDigitsRemove = digitsCount - precision;

                if (exponent + numberDigitsRemove > TDecimal.MaxAdjustedExponent)
                {
                    return sign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }

                return RoundWideToSignificand<TDecimal, TValue>(sign, high, low, digitsCount, exponent, precision, sticky);
            }

            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, low, exponent);
        }

        /// <summary>
        /// Rounds the double-width coefficient to <paramref name="numberDigitsRemain"/> significant digits using
        /// round-to-nearest, ties-to-even, and encodes the result. This is the integer-based counterpart of
        /// <see cref="DecimalIeee754Rounding{TDecimal, TValue}"/>.
        /// </summary>
        private static TValue RoundWideToSignificand<TDecimal, TValue>(bool sign, TValue high, TValue low, int digitsCount, int exponent, int numberDigitsRemain, bool sticky)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            int dropCount = digitsCount - numberDigitsRemain;
            TValue significand = DropDigits(ref high, ref low, dropCount, ref sticky, out int roundDigit);
            int resultExponent = exponent + dropCount;

            bool roundUp = (roundDigit > 5)
                || ((roundDigit == 5) && (sticky || TValue.IsOddInteger(significand)));

            if (!roundUp)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, significand, resultExponent);
            }

            if (significand == TDecimal.MaxSignificand)
            {
                resultExponent += 1;

                if (resultExponent > TDecimal.MaxAdjustedExponent)
                {
                    return sign ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
                }

                significand = TDecimal.Power10(TDecimal.Precision - 1);
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, significand, resultExponent);
            }

            significand += TValue.One;
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, significand, resultExponent);
        }

        /// <summary>
        /// Rounds a double-width coefficient whose value lies below the minimum quantum to either zero or the
        /// smallest representable subnormal (epsilon), using round-to-nearest, ties-to-even. This is the
        /// integer-based counterpart of <see cref="RoundToZeroOrEpsilon{TDecimal, TValue}"/>.
        /// </summary>
        private static TValue RoundWideToZeroOrEpsilon<TDecimal, TValue>(bool sign, TValue high, TValue low, int digitsCount, bool sticky)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue lead = DropDigits(ref high, ref low, digitsCount - 1, ref sticky, out int roundDigit);
            bool restNonZero = sticky || (roundDigit != 0);
            int leadDigit = int.CreateTruncating(lead);

            TValue significand = ((leadDigit > 5) || ((leadDigit == 5) && restNonZero)) ? TValue.One : TValue.Zero;
            return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, significand, TDecimal.MinAdjustedExponent);
        }

        internal static TValue AbsDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return decimalBits & ~TDecimal.SignMask;
        }

        /// <summary>
        /// Classifies a value as a non-integer (<c>-1</c>), an even integer (<c>0</c>), or an odd integer (<c>1</c>).
        /// A finite value is an integer when its significand is evenly divisible by the power of ten implied by a
        /// negative exponent; parity is that of the resulting integer coefficient.
        /// </summary>
        private static int ClassifyIntegerParityDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!TDecimal.IsFinite(decimalBits))
            {
                return -1;
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);
            TValue significand = decoded.Significand;

            if (TValue.IsZero(significand))
            {
                // Zero is an even integer.
                return 0;
            }

            int exponent = decoded.UnbiasedExponent;

            if (exponent >= 1)
            {
                // significand * 10^exponent carries a factor of ten and is therefore even.
                return 0;
            }

            if (exponent == 0)
            {
                return int.CreateTruncating(significand & TValue.One);
            }

            // A negative exponent is an integer only when the significand is evenly divisible by 10^(-exponent).
            // The significand has at most Precision digits, so dropping Precision or more digits never divides evenly.
            int dropCount = -exponent;

            if (dropCount >= TDecimal.Precision)
            {
                return -1;
            }

            (TValue quotient, TValue remainder) = TDecimal.DivRemPow10(significand, dropCount);

            if (!TValue.IsZero(remainder))
            {
                return -1;
            }

            return int.CreateTruncating(quotient & TValue.One);
        }

        internal static bool IsIntegerDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return ClassifyIntegerParityDecimalIeee754<TDecimal, TValue>(decimalBits) >= 0;
        }

        internal static bool IsEvenIntegerDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return ClassifyIntegerParityDecimalIeee754<TDecimal, TValue>(decimalBits) == 0;
        }

        internal static bool IsOddIntegerDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return ClassifyIntegerParityDecimalIeee754<TDecimal, TValue>(decimalBits) == 1;
        }

        internal static bool IsZeroDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!TDecimal.IsFinite(decimalBits))
            {
                return false;
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);
            return TValue.IsZero(decoded.Significand);
        }

        // This code is based on `bid32_isCanonical`, `bid64_isCanonical`, and `bid128_isCanonical`
        // from Intel(R) Decimal Floating-Point Math Library
        // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text
        internal static bool IsCanonicalDecimalIeee754<TDecimal, TValue>(TValue decimalBits, TValue nanReservedMask, TValue nanPayloadMask, TValue maxNaNPayload)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(decimalBits))
            {
                // A canonical NaN leaves the reserved bits clear and carries a payload within range.
                if ((decimalBits & nanReservedMask) != TValue.Zero)
                {
                    return false;
                }
                return (decimalBits & nanPayloadMask) <= maxNaNPayload;
            }

            if (TDecimal.IsInfinity(decimalBits))
            {
                // A canonical infinity leaves every bit outside the sign and NaN/infinity indicator clear.
                return (decimalBits & ~(TDecimal.SignMask | TDecimal.NaNMask)) == TValue.Zero;
            }

            // A finite value is canonical when its raw coefficient does not exceed the maximum
            // representable significand. The `11` steering form implies the most significant bit,
            // whose combination always overflows the maximum for `Decimal128`.
            TValue significand;

            if ((decimalBits & TDecimal.G0G1Mask) == TDecimal.G0G1Mask)
            {
                significand = (decimalBits & TDecimal.GwPlus4SignificandMask) | TDecimal.MostSignificantBitOfSignificandMask;
            }
            else
            {
                significand = decimalBits & TDecimal.GwPlus2ToGwPlus4SignificandMask;
            }

            return significand <= TDecimal.MaxSignificand;
        }

        /// <summary>
        /// Determines whether a finite non-zero value has an adjusted exponent at or above the minimum normal
        /// exponent. Zero, infinity, and NaN are never normal.
        /// </summary>
        internal static bool IsNormalDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!TDecimal.IsFinite(decimalBits))
            {
                return false;
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);

            if (TValue.IsZero(decoded.Significand))
            {
                return false;
            }

            int adjustedExponent = decoded.UnbiasedExponent + TDecimal.CountDigits(decoded.Significand) - 1;
            return adjustedExponent >= TDecimal.MinExponent;
        }

        /// <summary>
        /// Determines whether a finite non-zero value has an adjusted exponent below the minimum normal exponent.
        /// Zero, infinity, and NaN are never subnormal.
        /// </summary>
        internal static bool IsSubnormalDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!TDecimal.IsFinite(decimalBits))
            {
                return false;
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);

            if (TValue.IsZero(decoded.Significand))
            {
                return false;
            }

            int adjustedExponent = decoded.UnbiasedExponent + TDecimal.CountDigits(decoded.Significand) - 1;
            return adjustedExponent < TDecimal.MinExponent;
        }

        // The magnitude helpers match the IEEE 754:2019 maximumMagnitude/minimumMagnitude family. The *Number
        // variants do not propagate NaN; both treat +0 as greater than -0. Comparisons are performed on the
        // absolute (sign-cleared) bit patterns using the existing ordering helpers.

        internal static TValue MaxMagnitudeDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ax = AbsDecimalIeee754<TDecimal, TValue>(x);
            TValue ay = AbsDecimalIeee754<TDecimal, TValue>(y);

            if (GreaterThanDecimalIeee754<TDecimal, TValue>(ax, ay) || TDecimal.IsNaN(ax))
            {
                return CanonicalizeIfNaN<TDecimal, TValue>(x);
            }

            if (EqualsDecimalIeee754<TDecimal, TValue>(ax, ay))
            {
                return TDecimal.IsNegative(x) ? y : x;
            }

            return CanonicalizeIfNaN<TDecimal, TValue>(y);
        }

        internal static TValue MinMagnitudeDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ax = AbsDecimalIeee754<TDecimal, TValue>(x);
            TValue ay = AbsDecimalIeee754<TDecimal, TValue>(y);

            if (LessThanDecimalIeee754<TDecimal, TValue>(ax, ay) || TDecimal.IsNaN(ax))
            {
                return CanonicalizeIfNaN<TDecimal, TValue>(x);
            }

            if (EqualsDecimalIeee754<TDecimal, TValue>(ax, ay))
            {
                return TDecimal.IsNegative(x) ? x : y;
            }

            return CanonicalizeIfNaN<TDecimal, TValue>(y);
        }

        internal static TValue MaxMagnitudeNumberDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ax = AbsDecimalIeee754<TDecimal, TValue>(x);
            TValue ay = AbsDecimalIeee754<TDecimal, TValue>(y);

            if (GreaterThanDecimalIeee754<TDecimal, TValue>(ax, ay) || TDecimal.IsNaN(ay))
            {
                return CanonicalizeIfNaN<TDecimal, TValue>(x);
            }

            if (EqualsDecimalIeee754<TDecimal, TValue>(ax, ay))
            {
                return TDecimal.IsNegative(x) ? y : x;
            }

            return y;
        }

        internal static TValue MinMagnitudeNumberDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ax = AbsDecimalIeee754<TDecimal, TValue>(x);
            TValue ay = AbsDecimalIeee754<TDecimal, TValue>(y);

            if (LessThanDecimalIeee754<TDecimal, TValue>(ax, ay) || TDecimal.IsNaN(ay))
            {
                return CanonicalizeIfNaN<TDecimal, TValue>(x);
            }

            if (EqualsDecimalIeee754<TDecimal, TValue>(ax, ay))
            {
                return TDecimal.IsNegative(x) ? x : y;
            }

            return y;
        }

        // The Max/Min helpers match the IEEE 754:2019 maximum/minimum family. Max/Min propagate NaN; the *Native
        // variants mirror the greater-than/less-than operators (NaN never compares greater or less, so the second
        // operand wins); the *Number variants drop NaN inputs. All treat +0 as greater than -0.

        internal static TValue MaxDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(x))
            {
                return PropagateNaN<TDecimal, TValue>(x, x);
            }

            if (TDecimal.IsNaN(y))
            {
                return PropagateNaN<TDecimal, TValue>(y, y);
            }

            if (!EqualsDecimalIeee754<TDecimal, TValue>(x, y))
            {
                return GreaterThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y;
            }

            return TDecimal.IsNegative(y) ? x : y;
        }

        internal static TValue MinDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(x))
            {
                return PropagateNaN<TDecimal, TValue>(x, x);
            }

            if (TDecimal.IsNaN(y))
            {
                return PropagateNaN<TDecimal, TValue>(y, y);
            }

            if (!EqualsDecimalIeee754<TDecimal, TValue>(x, y))
            {
                return LessThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y;
            }

            return TDecimal.IsNegative(x) ? x : y;
        }

        internal static TValue MaxNativeDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(GreaterThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y);
        }

        internal static TValue MinNativeDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return CanonicalizeIfNaN<TDecimal, TValue>(LessThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y);
        }

        internal static TValue MaxNumberDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!EqualsDecimalIeee754<TDecimal, TValue>(x, y))
            {
                if (!TDecimal.IsNaN(y))
                {
                    return LessThanDecimalIeee754<TDecimal, TValue>(y, x) ? x : y;
                }

                return CanonicalizeIfNaN<TDecimal, TValue>(x);
            }

            return TDecimal.IsNegative(y) ? x : y;
        }

        internal static TValue MinNumberDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (!EqualsDecimalIeee754<TDecimal, TValue>(x, y))
            {
                if (!TDecimal.IsNaN(y))
                {
                    return LessThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y;
                }

                return CanonicalizeIfNaN<TDecimal, TValue>(x);
            }

            return TDecimal.IsNegative(x) ? x : y;
        }

        internal static TValue CopySignDecimalIeee754<TDecimal, TValue>(TValue value, TValue sign)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            // This method must work for all inputs, including NaN, so it operates on the raw bits: clear the sign of
            // value, keep only the sign of sign, then combine them.
            return (value & ~TDecimal.SignMask) | (sign & TDecimal.SignMask);
        }

        internal static int SignDecimalIeee754<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(decimalBits))
            {
                throw new ArithmeticException(SR.Arithmetic_NaN);
            }

            if (TDecimal.IsFinite(decimalBits) && TValue.IsZero(UnpackDecimalIeee754<TDecimal, TValue>(decimalBits).Significand))
            {
                return 0;
            }

            return ((decimalBits & TDecimal.SignMask) != TValue.Zero) ? -1 : +1;
        }

        // ==================================================================================================
        // Conversions
        // ==================================================================================================

        /// <summary>
        /// Classifies a decimal value for conversion to an integer type: whether it is NaN, an infinity, or a
        /// finite value. For finite values it also returns the sign and the magnitude truncated toward zero.
        /// </summary>
        internal enum DecimalIeee754ToIntegerStatus
        {
            Finite,
            NaN,
            PositiveInfinity,
            NegativeInfinity,
        }

        /// <summary>
        /// Converts the decimal <paramref name="decimalBits"/> to the integer magnitude obtained by truncating
        /// toward zero (the fractional part is discarded). Returns whether the value is finite, an infinity, or
        /// NaN. For finite values, <paramref name="isNegative"/> carries the sign, <paramref name="magnitude"/>
        /// the truncated absolute value, and <paramref name="exceedsUInt128"/> is set when that magnitude does
        /// not fit in <see cref="UInt128"/> (and therefore in no integer type).
        /// </summary>
        private static DecimalIeee754ToIntegerStatus DecimalIeee754ToIntegerMagnitude<TDecimal, TValue>(TValue decimalBits, out bool isNegative, out UInt128 magnitude, out bool exceedsUInt128)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            isNegative = (decimalBits & TDecimal.SignMask) != TValue.Zero;
            magnitude = UInt128.Zero;
            exceedsUInt128 = false;

            if (TDecimal.IsNaN(decimalBits))
            {
                return DecimalIeee754ToIntegerStatus.NaN;
            }

            if (TDecimal.IsInfinity(decimalBits))
            {
                return isNegative ? DecimalIeee754ToIntegerStatus.NegativeInfinity : DecimalIeee754ToIntegerStatus.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);
            isNegative = decoded.Signed;

            UInt128 significand = UInt128.CreateTruncating(decoded.Significand);
            int exponent = decoded.UnbiasedExponent;

            if (significand == UInt128.Zero)
            {
                return DecimalIeee754ToIntegerStatus.Finite;
            }

            if (exponent >= 0)
            {
                // magnitude = significand * 10^exponent. UInt128 holds at most 39 digits, so any exponent that
                // would push the product past that bound overflows every integer type and saturates/throws.
                for (int i = 0; (i < exponent) && !exceedsUInt128; i++)
                {
                    if (significand > UInt128.MaxValue / 10)
                    {
                        exceedsUInt128 = true;
                        break;
                    }
                    significand *= 10;
                }

                magnitude = exceedsUInt128 ? UInt128.Zero : significand;
            }
            else
            {
                // magnitude = significand / 10^(-exponent), truncated toward zero. Once the divisor has more
                // digits than the significand the quotient is zero.
                int drop = -exponent;

                for (int i = 0; (i < drop) && (significand != UInt128.Zero); i++)
                {
                    significand /= 10;
                }

                magnitude = significand;
            }

            return DecimalIeee754ToIntegerStatus.Finite;
        }

        /// <summary>
        /// Converts a decimal value to an integer type, truncating toward zero. When <paramref name="isChecked"/>
        /// is <c>true</c>, NaN, infinities, and out-of-range values throw <see cref="OverflowException"/>;
        /// otherwise the result saturates (NaN maps to zero, out-of-range values clamp to the type's bounds),
        /// matching the behavior of the binary floating-point to integer conversions.
        /// </summary>
        internal static TInteger ConvertDecimalIeee754ToInteger<TDecimal, TValue, TInteger>(TValue decimalBits, bool isChecked)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
            where TInteger : IBinaryInteger<TInteger>, IMinMaxValue<TInteger>
        {
            DecimalIeee754ToIntegerStatus status = DecimalIeee754ToIntegerMagnitude<TDecimal, TValue>(decimalBits, out bool isNegative, out UInt128 magnitude, out bool exceedsUInt128);

            bool isUnsigned = TInteger.IsZero(TInteger.MinValue);
            UInt128 maxMagnitude = UInt128.CreateTruncating(TInteger.MaxValue);
            // For a two's-complement signed type the most-negative value has magnitude MaxValue + 1.
            UInt128 minMagnitude = isUnsigned ? UInt128.Zero : maxMagnitude + UInt128.One;

            switch (status)
            {
                case DecimalIeee754ToIntegerStatus.NaN:
                {
                    if (isChecked)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return TInteger.Zero;
                }

                case DecimalIeee754ToIntegerStatus.PositiveInfinity:
                {
                    if (isChecked)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return TInteger.MaxValue;
                }

                case DecimalIeee754ToIntegerStatus.NegativeInfinity:
                {
                    if (isChecked)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return TInteger.MinValue;
                }

                default:
                {
                    if (!isNegative)
                    {
                        if (exceedsUInt128 || (magnitude > maxMagnitude))
                        {
                            if (isChecked)
                            {
                                ThrowHelper.ThrowOverflowException();
                            }
                            return TInteger.MaxValue;
                        }

                        return TInteger.CreateTruncating(magnitude);
                    }

                    // Negative magnitude.
                    if (isUnsigned)
                    {
                        if ((magnitude != UInt128.Zero) && isChecked)
                        {
                            ThrowHelper.ThrowOverflowException();
                        }
                        return TInteger.Zero;
                    }

                    if (exceedsUInt128 || (magnitude > minMagnitude))
                    {
                        if (isChecked)
                        {
                            ThrowHelper.ThrowOverflowException();
                        }
                        return TInteger.MinValue;
                    }

                    if (magnitude == minMagnitude)
                    {
                        return TInteger.MinValue;
                    }

                    return TInteger.Zero - TInteger.CreateTruncating(magnitude);
                }
            }
        }

        /// <summary>
        /// Converts an integer value of type <typeparamref name="TInteger"/> to the decimal format, rounding to
        /// the format precision when the integer has more significant digits than the format can represent. The
        /// result uses the preferred exponent of zero (quantum one) whenever the value fits exactly.
        /// </summary>
        internal static TValue ConvertIntegerToDecimalIeee754<TDecimal, TValue, TInteger>(TInteger value)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
            where TInteger : IBinaryInteger<TInteger>
        {
            bool isNegative = TInteger.IsNegative(value);
            UInt128 bits = UInt128.CreateTruncating(value);

            // For a negative value CreateTruncating sign-extends the two's-complement pattern to 128 bits, so a
            // width-independent negate recovers the true magnitude (including the most-negative value).
            UInt128 magnitude = isNegative ? (~bits) + UInt128.One : bits;

            return DecimalIeee754FromMagnitude<TDecimal, TValue>(isNegative, magnitude, 0);
        }

        /// <summary>
        /// Builds the decimal encoding for the value <c>(-1)^<paramref name="sign"/> * <paramref name="magnitude"/> * 10^<paramref name="exponent"/></c>,
        /// rendering the magnitude to its decimal digits and running the shared rounding pipeline (which rounds
        /// to the format precision and handles the subnormal and overflow ranges).
        /// </summary>
        private static TValue DecimalIeee754FromMagnitude<TDecimal, TValue>(bool sign, UInt128 magnitude, int exponent)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (magnitude == UInt128.Zero)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(sign, TValue.Zero, exponent);
            }

            // A UInt128 has at most 39 decimal digits; leave room for the terminating null. The NumberBuffer
            // constructor rewrites Digits[0] as part of initialization, so the digits must be written into the
            // buffer's span after construction rather than before.
            Span<byte> digits = stackalloc byte[UInt128NumberBufferLength + 1];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits);

            int digitsCount = WriteDecimalDigits(magnitude, number.Digits);
            number.DigitsCount = digitsCount;
            number.Scale = digitsCount + exponent;
            number.IsNegative = sign;
            number.CheckConsistency();

            return NumberToDecimalIeee754Bits<TDecimal, TValue>(ref number);
        }

        /// <summary>
        /// Writes the decimal digits of the non-zero <paramref name="magnitude"/> into <paramref name="digits"/>
        /// (most-significant digit first, no leading zeros) followed by a terminating null, returning the digit count.
        /// </summary>
        private static int WriteDecimalDigits(UInt128 magnitude, Span<byte> digits)
        {
            Debug.Assert(magnitude != UInt128.Zero);

            // Emit least-significant digit first into the tail of a scratch buffer, then compact to the front.
            Span<byte> scratch = stackalloc byte[UInt128NumberBufferLength];
            int index = scratch.Length;

            while (magnitude != UInt128.Zero)
            {
                (magnitude, UInt128 digit) = UInt128.DivRem(magnitude, 10);
                scratch[--index] = (byte)('0' + (int)digit);
            }

            int count = scratch.Length - index;
            scratch.Slice(index, count).CopyTo(digits);
            digits[count] = (byte)'\0';
            return count;
        }

        /// <summary>
        /// Converts a decimal value from the source format to the target format. Widening conversions are exact;
        /// narrowing conversions round to the target precision. NaN and infinities are propagated with their sign.
        /// </summary>
        internal static TTargetValue ConvertDecimalIeee754<TSourceDecimal, TSourceValue, TTargetDecimal, TTargetValue>(TSourceValue decimalBits)
            where TSourceDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TSourceDecimal, TSourceValue>
            where TSourceValue : unmanaged, IBinaryInteger<TSourceValue>
            where TTargetDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TTargetDecimal, TTargetValue>
            where TTargetValue : unmanaged, IBinaryInteger<TTargetValue>
        {
            bool isNegative = (decimalBits & TSourceDecimal.SignMask) != TSourceValue.Zero;

            if (TSourceDecimal.IsNaN(decimalBits))
            {
                return isNegative ? (TTargetDecimal.NaN | TTargetDecimal.SignMask) : TTargetDecimal.NaN;
            }

            if (TSourceDecimal.IsInfinity(decimalBits))
            {
                return isNegative ? TTargetDecimal.NegativeInfinity : TTargetDecimal.PositiveInfinity;
            }

            DecodedDecimalIeee754<TSourceValue> decoded = UnpackDecimalIeee754<TSourceDecimal, TSourceValue>(decimalBits);

            if (TSourceValue.IsZero(decoded.Significand))
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TTargetDecimal, TTargetValue>(decoded.Signed, TTargetValue.Zero, decoded.UnbiasedExponent);
            }

            UInt128 magnitude = UInt128.CreateTruncating(decoded.Significand);
            return DecimalIeee754FromMagnitude<TTargetDecimal, TTargetValue>(decoded.Signed, magnitude, decoded.UnbiasedExponent);
        }

        /// <summary>
        /// Converts a decimal value to the binary floating-point type <typeparamref name="TFloat"/>, correctly
        /// rounded. NaN and infinities propagate with their sign; finite values are rendered to their decimal
        /// digits and run through the shared binary parsing pipeline, which produces the correctly rounded result.
        /// </summary>
        internal static TFloat ConvertDecimalIeee754ToFloat<TDecimal, TValue, TFloat>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            bool isNegative = (decimalBits & TDecimal.SignMask) != TValue.Zero;

            if (TDecimal.IsNaN(decimalBits))
            {
                return isNegative ? -TFloat.NaN : TFloat.NaN;
            }

            if (TDecimal.IsInfinity(decimalBits))
            {
                return isNegative ? TFloat.NegativeInfinity : TFloat.PositiveInfinity;
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);

            if (TValue.IsZero(decoded.Significand))
            {
                return decoded.Signed ? -TFloat.Zero : TFloat.Zero;
            }

            // The NumberBuffer constructor rewrites Digits[0] as part of initialization, so the digits must be
            // written into the buffer's span after construction rather than before.
            UInt128 magnitude = UInt128.CreateTruncating(decoded.Significand);
            Span<byte> digits = stackalloc byte[UInt128NumberBufferLength + 1];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits);

            int digitsCount = WriteDecimalDigits(magnitude, number.Digits);
            number.DigitsCount = digitsCount;
            number.Scale = digitsCount + decoded.UnbiasedExponent;
            number.IsNegative = decoded.Signed;

            return NumberToFloat<TFloat>(ref number);
        }

        /// <summary>
        /// Converts a binary floating-point value to the decimal format, correctly rounded. NaN and infinities
        /// propagate with their sign; finite values are expanded to their exact decimal representation via Dragon4
        /// and rounded once to the target precision (IEEE convertFormat rounds the exact value, not the shortest
        /// round-trippable string).
        /// </summary>
        internal static TValue ConvertFloatToDecimalIeee754<TFloat, TDecimal, TValue>(TFloat value)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            bool isNegative = TFloat.IsNegative(value);

            if (TFloat.IsNaN(value))
            {
                return isNegative ? (TDecimal.NaN | TDecimal.SignMask) : TDecimal.NaN;
            }

            if (TFloat.IsInfinity(value))
            {
                return isNegative ? TDecimal.NegativeInfinity : TDecimal.PositiveInfinity;
            }

            if (value == TFloat.Zero)
            {
                return DecimalIeee754FiniteNumberBinaryEncoding<TDecimal, TValue>(isNegative, TValue.Zero, 0);
            }

            // Produce the exact decimal expansion of the finite value. Passing a length-based cutoff of int.MaxValue
            // ensures the buffer size is the limiting factor, and NumberBufferLength is large enough to hold the full
            // expansion (so the result is exact and a single rounding to precision follows).
            Span<byte> digits = stackalloc byte[TFloat.NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits);
            Dragon4<TFloat>(value, cutoffNumber: int.MaxValue, isSignificantDigits: false, ref number);
            number.IsNegative = isNegative;

            // IEEE convertFormat delivers the preferred (quantum) exponent: for an exact result it is the
            // representable exponent closest to zero from below. Dragon4 strips trailing zeros, which can push the
            // exponent above zero (e.g. 1000 -> digits "1", Scale 4, exponent 3). Re-materialize those trailing zeros
            // to bring the exponent down to zero so integer-valued inputs keep quantum one (matching the decimal parse
            // path); the shared pipeline then rounds when the coefficient exceeds the target precision.
            int preferredZeros = number.Scale - number.DigitsCount;
            if (preferredZeros > 0)
            {
                int end = number.DigitsCount + preferredZeros;
                digits.Slice(number.DigitsCount, preferredZeros).Fill((byte)'0');
                digits[end] = (byte)'\0';
                number.DigitsCount = end;
            }

            number.CheckConsistency();

            return NumberToDecimalIeee754Bits<TDecimal, TValue>(ref number);
        }

        /// <summary>
        /// Converts a decimal value to <see cref="decimal"/> (System.Decimal). NaN and infinities cannot be
        /// represented and throw <see cref="OverflowException"/>; finite values are rendered to their decimal
        /// digits and run through the shared System.Decimal conversion pipeline, which rounds to the System.Decimal
        /// precision and throws <see cref="OverflowException"/> when the value is outside the System.Decimal range.
        /// </summary>
        internal static decimal ConvertDecimalIeee754ToDecimal<TDecimal, TValue>(TValue decimalBits)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            if (TDecimal.IsNaN(decimalBits) || TDecimal.IsInfinity(decimalBits))
            {
                throw new OverflowException(SR.Overflow_Decimal);
            }

            DecodedDecimalIeee754<TValue> decoded = UnpackDecimalIeee754<TDecimal, TValue>(decimalBits);

            if (TValue.IsZero(decoded.Significand))
            {
                // System.Decimal has no infinite quantum range: clamp the preferred exponent into the [0, -28]
                // scale range the way the parse pipeline does for a zero coefficient.
                return new decimal(0, 0, 0, decoded.Signed, (byte)Math.Clamp(-decoded.UnbiasedExponent, 0, 28));
            }

            UInt128 magnitude = UInt128.CreateTruncating(decoded.Significand);
            Span<byte> digits = stackalloc byte[UInt128NumberBufferLength + 1];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits);

            int digitsCount = WriteDecimalDigits(magnitude, number.Digits);
            number.DigitsCount = digitsCount;
            number.Scale = digitsCount + decoded.UnbiasedExponent;
            number.IsNegative = decoded.Signed;
            number.CheckConsistency();

            decimal result = default;

            if (!TryNumberToDecimal(ref number, ref result))
            {
                throw new OverflowException(SR.Overflow_Decimal);
            }

            return result;
        }

        /// <summary>
        /// Converts a <see cref="decimal"/> (System.Decimal) value to the decimal format, correctly rounded. The
        /// System.Decimal digits and scale are rendered into a NumberBuffer and run through the shared rounding
        /// pipeline, which rounds to the format precision and preserves the source quantum (IEEE convertFormat).
        /// </summary>
        internal static TValue ConvertDecimalToDecimalIeee754<TDecimal, TValue>(decimal value)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            Span<byte> digits = stackalloc byte[DecimalNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits);

            DecimalToNumber(ref value, ref number);

            return NumberToDecimalIeee754Bits<TDecimal, TValue>(ref number);
        }
    }
}
