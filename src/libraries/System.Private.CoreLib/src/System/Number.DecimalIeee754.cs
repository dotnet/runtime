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
                return x;
            }

            if (EqualsDecimalIeee754<TDecimal, TValue>(ax, ay))
            {
                return TDecimal.IsNegative(x) ? y : x;
            }

            return y;
        }

        internal static TValue MinMagnitudeDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ax = AbsDecimalIeee754<TDecimal, TValue>(x);
            TValue ay = AbsDecimalIeee754<TDecimal, TValue>(y);

            if (LessThanDecimalIeee754<TDecimal, TValue>(ax, ay) || TDecimal.IsNaN(ax))
            {
                return x;
            }

            if (EqualsDecimalIeee754<TDecimal, TValue>(ax, ay))
            {
                return TDecimal.IsNegative(x) ? x : y;
            }

            return y;
        }

        internal static TValue MaxMagnitudeNumberDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            TValue ax = AbsDecimalIeee754<TDecimal, TValue>(x);
            TValue ay = AbsDecimalIeee754<TDecimal, TValue>(y);

            if (GreaterThanDecimalIeee754<TDecimal, TValue>(ax, ay) || TDecimal.IsNaN(ay))
            {
                return x;
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
                return x;
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
                return x;
            }

            if (TDecimal.IsNaN(y))
            {
                return y;
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
                return x;
            }

            if (TDecimal.IsNaN(y))
            {
                return y;
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
            return GreaterThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y;
        }

        internal static TValue MinNativeDecimalIeee754<TDecimal, TValue>(TValue x, TValue y)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            return LessThanDecimalIeee754<TDecimal, TValue>(x, y) ? x : y;
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

                return x;
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

                return x;
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
    }
}
