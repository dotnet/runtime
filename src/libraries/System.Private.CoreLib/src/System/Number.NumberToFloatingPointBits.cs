// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    internal unsafe partial class Number
    {
        public readonly struct FloatingPointInfo
        {
            public static readonly FloatingPointInfo Double = new FloatingPointInfo(
                denormalMantissaBits: 52,
                exponentBits: 11,
                maxBinaryExponent: 1023,
                exponentBias: 1023,
                infinityBits: 0x7FF00000_00000000
            );

            public static readonly FloatingPointInfo Single = new FloatingPointInfo(
                denormalMantissaBits: 23,
                exponentBits: 8,
                maxBinaryExponent: 127,
                exponentBias: 127,
                infinityBits: 0x7F800000
            );

            public static readonly FloatingPointInfo Half = new FloatingPointInfo(
                denormalMantissaBits: 10,
                exponentBits: 5,
                maxBinaryExponent: 15,
                exponentBias: 15,
                infinityBits: 0x7C00
            );

            public ulong ZeroBits { get; }
            public ulong InfinityBits { get; }

            public ulong NormalMantissaMask { get; }
            public ulong DenormalMantissaMask { get; }

            public int MinBinaryExponent { get; }
            public int MaxBinaryExponent { get; }

            public int ExponentBias { get; }
            public int OverflowDecimalExponent { get; }

            public ushort NormalMantissaBits { get; }
            public ushort DenormalMantissaBits { get; }

            public ushort ExponentBits { get; }

            public FloatingPointInfo(ushort denormalMantissaBits, ushort exponentBits, int maxBinaryExponent, int exponentBias, ulong infinityBits)
            {
                ExponentBits = exponentBits;

                DenormalMantissaBits = denormalMantissaBits;
                NormalMantissaBits = (ushort)(denormalMantissaBits + 1); // we get an extra (hidden) bit for normal mantissas

                OverflowDecimalExponent = (maxBinaryExponent + 2 * NormalMantissaBits) / 3;
                ExponentBias = exponentBias;

                MaxBinaryExponent = maxBinaryExponent;
                MinBinaryExponent = 1 - maxBinaryExponent;

                DenormalMantissaMask = (1UL << denormalMantissaBits) - 1;
                NormalMantissaMask = (1UL << NormalMantissaBits) - 1;

                InfinityBits = infinityBits;
                ZeroBits = 0;
            }
        }

        private static readonly float[] s_Pow10SingleTable = new float[]
        {
            1e0f,   // 10^0
            1e1f,   // 10^1
            1e2f,   // 10^2
            1e3f,   // 10^3
            1e4f,   // 10^4
            1e5f,   // 10^5
            1e6f,   // 10^6
            1e7f,   // 10^7
            1e8f,   // 10^8
            1e9f,   // 10^9
            1e10f,  // 10^10
        };

        private static readonly double[] s_Pow10DoubleTable = new double[]
        {
            1e0,    // 10^0
            1e1,    // 10^1
            1e2,    // 10^2
            1e3,    // 10^3
            1e4,    // 10^4
            1e5,    // 10^5
            1e6,    // 10^6
            1e7,    // 10^7
            1e8,    // 10^8
            1e9,    // 10^9
            1e10,   // 10^10
            1e11,   // 10^11
            1e12,   // 10^12
            1e13,   // 10^13
            1e14,   // 10^14
            1e15,   // 10^15
            1e16,   // 10^16
            1e17,   // 10^17
            1e18,   // 10^18
            1e19,   // 10^19
            1e20,   // 10^20
            1e21,   // 10^21
            1e22,   // 10^22
        };

        private static void AccumulateDecimalDigitsIntoBigInteger(ref NumberBuffer number, uint firstIndex, uint lastIndex, out BigInteger result)
        {
            BigInteger.SetZero(out result);

            byte* src = number.GetDigitsPointer() + firstIndex;
            uint remaining = lastIndex - firstIndex;

            while (remaining != 0)
            {
                uint count = Math.Min(remaining, 9);
                uint value = DigitsToUInt32(src, (int)(count));

                result.MultiplyPow10(count);
                result.Add(value);

                src += count;
                remaining -= count;
            }
        }

        private static ulong AssembleFloatingPointBits(in FloatingPointInfo info, ulong initialMantissa, int initialExponent, bool hasZeroTail)
        {
            // number of bits by which we must adjust the mantissa to shift it into the
            // correct position, and compute the resulting base two exponent for the
            // normalized mantissa:
            uint initialMantissaBits = BigInteger.CountSignificantBits(initialMantissa);
            int normalMantissaShift = info.NormalMantissaBits - (int)(initialMantissaBits);
            int normalExponent = initialExponent - normalMantissaShift;

            ulong mantissa = initialMantissa;
            int exponent = normalExponent;

            if (normalExponent > info.MaxBinaryExponent)
            {
                // The exponent is too large to be represented by the floating point
                // type; report the overflow condition:
                return info.InfinityBits;
            }
            else if (normalExponent < info.MinBinaryExponent)
            {
                // The exponent is too small to be represented by the floating point
                // type as a normal value, but it may be representable as a denormal
                // value.  Compute the number of bits by which we need to shift the
                // mantissa in order to form a denormal number.  (The subtraction of
                // an extra 1 is to account for the hidden bit of the mantissa that
                // is not available for use when representing a denormal.)
                int denormalMantissaShift = normalMantissaShift + normalExponent + info.ExponentBias - 1;

                // Denormal values have an exponent of zero, so the debiased exponent is
                // the negation of the exponent bias:
                exponent = -info.ExponentBias;

                if (denormalMantissaShift < 0)
                {
                    // Use two steps for right shifts:  for a shift of N bits, we first
                    // shift by N-1 bits, then shift the last bit and use its value to
                    // round the mantissa.
                    mantissa = RightShiftWithRounding(mantissa, -denormalMantissaShift, hasZeroTail);

                    // If the mantissa is now zero, we have underflowed:
                    if (mantissa == 0)
                    {
                        return info.ZeroBits;
                    }

                    // When we round the mantissa, the result may be so large that the
                    // number becomes a normal value.  For example, consider the single
                    // precision case where the mantissa is 0x01ffffff and a right shift
                    // of 2 is required to shift the value into position. We perform the
                    // shift in two steps:  we shift by one bit, then we shift again and
                    // round using the dropped bit.  The initial shift yields 0x00ffffff.
                    // The rounding shift then yields 0x007fffff and because the least
                    // significant bit was 1, we add 1 to this number to round it.  The
                    // final result is 0x00800000.
                    //
                    // 0x00800000 is 24 bits, which is more than the 23 bits available
                    // in the mantissa.  Thus, we have rounded our denormal number into
                    // a normal number.
                    //
                    // We detect this case here and re-adjust the mantissa and exponent
                    // appropriately, to form a normal number:
                    if (mantissa > info.DenormalMantissaMask)
                    {
                        // We add one to the denormal_mantissa_shift to account for the
                        // hidden mantissa bit (we subtracted one to account for this bit
                        // when we computed the denormal_mantissa_shift above).
                        exponent = initialExponent - (denormalMantissaShift + 1) - normalMantissaShift;
                    }
                }
                else
                {
                    mantissa <<= denormalMantissaShift;
                }
            }
            else
            {
                if (normalMantissaShift < 0)
                {
                    // Use two steps for right shifts:  for a shift of N bits, we first
                    // shift by N-1 bits, then shift the last bit and use its value to
                    // round the mantissa.
                    mantissa = RightShiftWithRounding(mantissa, -normalMantissaShift, hasZeroTail);

                    // When we round the mantissa, it may produce a result that is too
                    // large.  In this case, we divide the mantissa by two and increment
                    // the exponent (this does not change the value).
                    if (mantissa > info.NormalMantissaMask)
                    {
                        mantissa >>= 1;
                        exponent++;

                        // The increment of the exponent may have generated a value too
                        // large to be represented.  In this case, report the overflow:
                        if (exponent > info.MaxBinaryExponent)
                        {
                            return info.InfinityBits;
                        }
                    }
                }
                else if (normalMantissaShift > 0)
                {
                    mantissa <<= normalMantissaShift;
                }
            }

            // Unset the hidden bit in the mantissa and assemble the floating point value
            // from the computed components:
            mantissa &= info.DenormalMantissaMask;

            Debug.Assert((info.DenormalMantissaMask & (1UL << info.DenormalMantissaBits)) == 0);
            ulong shiftedExponent = ((ulong)(exponent + info.ExponentBias)) << info.DenormalMantissaBits;
            Debug.Assert((shiftedExponent & info.DenormalMantissaMask) == 0);
            Debug.Assert((mantissa & ~info.DenormalMantissaMask) == 0);
            Debug.Assert((shiftedExponent & ~(((1UL << info.ExponentBits) - 1) << info.DenormalMantissaBits)) == 0); // exponent fits in its place

            return shiftedExponent | mantissa;
        }

        private static ulong ConvertBigIntegerToFloatingPointBits(ref BigInteger value, in FloatingPointInfo info, uint integerBitsOfPrecision, bool hasNonZeroFractionalPart)
        {
            int baseExponent = info.DenormalMantissaBits;

            // When we have 64-bits or less of precision, we can just get the mantissa directly
            if (integerBitsOfPrecision <= 64)
            {
                return AssembleFloatingPointBits(in info, value.ToUInt64(), baseExponent, !hasNonZeroFractionalPart);
            }

            (uint topBlockIndex, uint topBlockBits) = Math.DivRem(integerBitsOfPrecision, 32);
            uint middleBlockIndex = topBlockIndex - 1;
            uint bottomBlockIndex = middleBlockIndex - 1;

            ulong mantissa;
            int exponent = baseExponent + ((int)(bottomBlockIndex) * 32);
            bool hasZeroTail = !hasNonZeroFractionalPart;

            // When the top 64-bits perfectly span two blocks, we can get those blocks directly
            if (topBlockBits == 0)
            {
                mantissa = ((ulong)(value.GetBlock(middleBlockIndex)) << 32) + value.GetBlock(bottomBlockIndex);
            }
            else
            {
                // Otherwise, we need to read three blocks and combine them into a 64-bit mantissa

                int bottomBlockShift = (int)(topBlockBits);
                int topBlockShift = 64 - bottomBlockShift;
                int middleBlockShift = topBlockShift - 32;

                exponent += (int)(topBlockBits);

                uint bottomBlock = value.GetBlock(bottomBlockIndex);
                uint bottomBits = bottomBlock >> bottomBlockShift;

                ulong middleBits = (ulong)(value.GetBlock(middleBlockIndex)) << middleBlockShift;
                ulong topBits = (ulong)(value.GetBlock(topBlockIndex)) << topBlockShift;

                mantissa = topBits + middleBits + bottomBits;

                uint unusedBottomBlockBitsMask = (1u << (int)(topBlockBits)) - 1;
                hasZeroTail &= (bottomBlock & unusedBottomBlockBitsMask) == 0;
            }

            for (uint i = 0; i != bottomBlockIndex; i++)
            {
                hasZeroTail &= (value.GetBlock(i) == 0);
            }

            return AssembleFloatingPointBits(in info, mantissa, exponent, hasZeroTail);
        }

        // get 32-bit integer from at most 9 digits
        private static uint DigitsToUInt32(byte* p, int count)
        {
            Debug.Assert((1 <= count) && (count <= 9));

            byte* end = (p + count);
            uint res = (uint)(p[0] - '0');

            for (p++; p < end; p++)
            {
                res = (10 * res) + p[0] - '0';
            }

            return res;
        }

        // get 64-bit integer from at most 19 digits
        private static ulong DigitsToUInt64(byte* p, int count)
        {
            Debug.Assert((1 <= count) && (count <= 19));

            byte* end = (p + count);
            ulong res = (ulong)(p[0] - '0');

            for (p++; p < end; p++)
            {
                res = (10 * res) + p[0] - '0';
            }

            return res;
        }

        private static ulong NumberToDoubleFloatingPointBits(ref NumberBuffer number, in FloatingPointInfo info)
        {
            Debug.Assert(info.DenormalMantissaBits == 52);

            Debug.Assert(number.GetDigitsPointer()[0] != '0');

            Debug.Assert(number.Scale <= FloatingPointMaxExponent);
            Debug.Assert(number.Scale >= FloatingPointMinExponent);

            Debug.Assert(number.DigitsCount != 0);

            // The input is of the form 0.Mantissa x 10^Exponent, where 'Mantissa' are
            // the decimal digits of the mantissa and 'Exponent' is the decimal exponent.
            // We decompose the mantissa into two parts: an integer part and a fractional
            // part.  If the exponent is positive, then the integer part consists of the
            // first 'exponent' digits, or all present digits if there are fewer digits.
            // If the exponent is zero or negative, then the integer part is empty.  In
            // either case, the remaining digits form the fractional part of the mantissa.

            uint totalDigits = (uint)(number.DigitsCount);
            uint positiveExponent = (uint)(Math.Max(0, number.Scale));

            uint integerDigitsPresent = Math.Min(positiveExponent, totalDigits);
            uint fractionalDigitsPresent = totalDigits - integerDigitsPresent;

            uint fastExponent = (uint)(Math.Abs(number.Scale - integerDigitsPresent - fractionalDigitsPresent));

            // When the number of significant digits is less than or equal to 15 and the
            // scale is less than or equal to 22, we can take some shortcuts and just rely
            // on floating-point arithmetic to compute the correct result. This is
            // because each floating-point precision values allows us to exactly represent
            // different whole integers and certain powers of 10, depending on the underlying
            // formats exact range. Additionally, IEEE operations dictate that the result is
            // computed to the infinitely precise result and then rounded, which means that
            // we can rely on it to produce the correct result when both inputs are exact.

            byte* src = number.GetDigitsPointer();

            if ((totalDigits <= 15) && (fastExponent <= 22))
            {
                double result = DigitsToUInt64(src, (int)(totalDigits));
                double scale = s_Pow10DoubleTable[fastExponent];

                if (fractionalDigitsPresent != 0)
                {
                    result /= scale;
                }
                else
                {
                    result *= scale;
                }

                return (ulong)(BitConverter.DoubleToInt64Bits(result));
            }

            return NumberToFloatingPointBitsSlow(ref number, in info, positiveExponent, integerDigitsPresent, fractionalDigitsPresent);
        }

        private static ushort NumberToHalfFloatingPointBits(ref NumberBuffer number, in FloatingPointInfo info)
        {
            Debug.Assert(info.DenormalMantissaBits == 10);

            Debug.Assert(number.GetDigitsPointer()[0] != '0');

            Debug.Assert(number.Scale <= FloatingPointMaxExponent);
            Debug.Assert(number.Scale >= FloatingPointMinExponent);

            Debug.Assert(number.DigitsCount != 0);

            // The input is of the form 0.Mantissa x 10^Exponent, where 'Mantissa' are
            // the decimal digits of the mantissa and 'Exponent' is the decimal exponent.
            // We decompose the mantissa into two parts: an integer part and a fractional
            // part.  If the exponent is positive, then the integer part consists of the
            // first 'exponent' digits, or all present digits if there are fewer digits.
            // If the exponent is zero or negative, then the integer part is empty.  In
            // either case, the remaining digits form the fractional part of the mantissa.

            uint totalDigits = (uint)(number.DigitsCount);
            uint positiveExponent = (uint)(Math.Max(0, number.Scale));

            uint integerDigitsPresent = Math.Min(positiveExponent, totalDigits);
            uint fractionalDigitsPresent = totalDigits - integerDigitsPresent;

            uint fastExponent = (uint)(Math.Abs(number.Scale - integerDigitsPresent - fractionalDigitsPresent));

            // When the number of significant digits is less than or equal to 15 and the
            // scale is less than or equal to 22, we can take some shortcuts and just rely
            // on floating-point arithmetic to compute the correct result. This is
            // because each floating-point precision values allows us to exactly represent
            // different whole integers and certain powers of 10, depending on the underlying
            // formats exact range. Additionally, IEEE operations dictate that the result is
            // computed to the infinitely precise result and then rounded, which means that
            // we can rely on it to produce the correct result when both inputs are exact.

            byte* src = number.GetDigitsPointer();

            if ((totalDigits <= 7) && (fastExponent <= 10))
            {
                // It is only valid to do this optimization for half and single-precision floating-point
                // values since we can lose some of the mantissa bits and would return the
                // wrong value when upcasting to double.

                float result = DigitsToUInt32(src, (int)(totalDigits));
                float scale = s_Pow10SingleTable[fastExponent];

                if (fractionalDigitsPresent != 0)
                {
                    result /= scale;
                }
                else
                {
                    result *= scale;
                }

                return (ushort)(BitConverter.HalfToInt16Bits((Half)result));
            }

            if ((totalDigits <= 15) && (fastExponent <= 22))
            {
                double result = DigitsToUInt64(src, (int)(totalDigits));
                double scale = s_Pow10DoubleTable[fastExponent];

                if (fractionalDigitsPresent != 0)
                {
                    result /= scale;
                }
                else
                {
                    result *= scale;
                }

                return (ushort)(BitConverter.HalfToInt16Bits((Half)(result)));
            }

            return (ushort)NumberToFloatingPointBitsSlow(ref number, in info, positiveExponent, integerDigitsPresent, fractionalDigitsPresent);
        }

        private static uint NumberToSingleFloatingPointBits(ref NumberBuffer number, in FloatingPointInfo info)
        {
            Debug.Assert(info.DenormalMantissaBits == 23);

            Debug.Assert(number.GetDigitsPointer()[0] != '0');

            Debug.Assert(number.Scale <= FloatingPointMaxExponent);
            Debug.Assert(number.Scale >= FloatingPointMinExponent);

            Debug.Assert(number.DigitsCount != 0);

            // The input is of the form 0.Mantissa x 10^Exponent, where 'Mantissa' are
            // the decimal digits of the mantissa and 'Exponent' is the decimal exponent.
            // We decompose the mantissa into two parts: an integer part and a fractional
            // part.  If the exponent is positive, then the integer part consists of the
            // first 'exponent' digits, or all present digits if there are fewer digits.
            // If the exponent is zero or negative, then the integer part is empty.  In
            // either case, the remaining digits form the fractional part of the mantissa.

            uint totalDigits = (uint)(number.DigitsCount);
            uint positiveExponent = (uint)(Math.Max(0, number.Scale));

            uint integerDigitsPresent = Math.Min(positiveExponent, totalDigits);
            uint fractionalDigitsPresent = totalDigits - integerDigitsPresent;

            uint fastExponent = (uint)(Math.Abs(number.Scale - integerDigitsPresent - fractionalDigitsPresent));

            // When the number of significant digits is less than or equal to 15 and the
            // scale is less than or equal to 22, we can take some shortcuts and just rely
            // on floating-point arithmetic to compute the correct result. This is
            // because each floating-point precision values allows us to exactly represent
            // different whole integers and certain powers of 10, depending on the underlying
            // formats exact range. Additionally, IEEE operations dictate that the result is
            // computed to the infinitely precise result and then rounded, which means that
            // we can rely on it to produce the correct result when both inputs are exact.

            byte* src = number.GetDigitsPointer();

            if ((totalDigits <= 7) && (fastExponent <= 10))
            {
                // It is only valid to do this optimization for single-precision floating-point
                // values since we can lose some of the mantissa bits and would return the
                // wrong value when upcasting to double.

                float result = DigitsToUInt32(src, (int)(totalDigits));
                float scale = s_Pow10SingleTable[fastExponent];

                if (fractionalDigitsPresent != 0)
                {
                    result /= scale;
                }
                else
                {
                    result *= scale;
                }

                return (uint)(BitConverter.SingleToInt32Bits(result));
            }

            if ((totalDigits <= 15) && (fastExponent <= 22))
            {
                double result = DigitsToUInt64(src, (int)(totalDigits));
                double scale = s_Pow10DoubleTable[fastExponent];

                if (fractionalDigitsPresent != 0)
                {
                    result /= scale;
                }
                else
                {
                    result *= scale;
                }

                return (uint)(BitConverter.SingleToInt32Bits((float)(result)));
            }

            return (uint)NumberToFloatingPointBitsSlow(ref number, in info, positiveExponent, integerDigitsPresent, fractionalDigitsPresent);
        }

        private static ulong NumberToFloatingPointBitsSlow(ref NumberBuffer number, in FloatingPointInfo info, uint positiveExponent, uint integerDigitsPresent, uint fractionalDigitsPresent)
        {
            // To generate an N bit mantissa we require N + 1 bits of precision.  The
            // extra bit is used to correctly round the mantissa (if there are fewer bits
            // than this available, then that's totally okay; in that case we use what we
            // have and we don't need to round).
            uint requiredBitsOfPrecision = (uint)(info.NormalMantissaBits + 1);

            uint totalDigits = (uint)(number.DigitsCount);
            uint integerDigitsMissing = positiveExponent - integerDigitsPresent;

            const uint IntegerFirstIndex = 0;
            uint integerLastIndex = integerDigitsPresent;

            uint fractionalFirstIndex = integerLastIndex;
            uint fractionalLastIndex = totalDigits;

            // First, we accumulate the integer part of the mantissa into a big_integer:
            AccumulateDecimalDigitsIntoBigInteger(ref number, IntegerFirstIndex, integerLastIndex, out BigInteger integerValue);

            if (integerDigitsMissing > 0)
            {
                if (integerDigitsMissing > info.OverflowDecimalExponent)
                {
                    return info.InfinityBits;
                }

                integerValue.MultiplyPow10(integerDigitsMissing);
            }

            // At this point, the integer_value contains the value of the integer part
            // of the mantissa.  If either [1] this number has more than the required
            // number of bits of precision or [2] the mantissa has no fractional part,
            // then we can assemble the result immediately:
            uint integerBitsOfPrecision = BigInteger.CountSignificantBits(ref integerValue);

            if ((integerBitsOfPrecision >= requiredBitsOfPrecision) || (fractionalDigitsPresent == 0))
            {
                return ConvertBigIntegerToFloatingPointBits(
                    ref integerValue,
                    in info,
                    integerBitsOfPrecision,
                    fractionalDigitsPresent != 0
                );
            }

            // Otherwise, we did not get enough bits of precision from the integer part,
            // and the mantissa has a fractional part.  We parse the fractional part of
            // the mantissa to obtain more bits of precision.  To do this, we convert
            // the fractional part into an actual fraction N/M, where the numerator N is
            // computed from the digits of the fractional part, and the denominator M is
            // computed as the power of 10 such that N/M is equal to the value of the
            // fractional part of the mantissa.

            uint fractionalDenominatorExponent = fractionalDigitsPresent;

            if (number.Scale < 0)
            {
                fractionalDenominatorExponent += (uint)(-number.Scale);
            }

            if ((integerBitsOfPrecision == 0) && (fractionalDenominatorExponent - (int)(totalDigits)) > info.OverflowDecimalExponent)
            {
                // If there were any digits in the integer part, it is impossible to
                // underflow (because the exponent cannot possibly be small enough),
                // so if we underflow here it is a true underflow and we return zero.
                return info.ZeroBits;
            }

            AccumulateDecimalDigitsIntoBigInteger(ref number, fractionalFirstIndex, fractionalLastIndex, out BigInteger fractionalNumerator);

            if (fractionalNumerator.IsZero())
            {
                return ConvertBigIntegerToFloatingPointBits(
                    ref integerValue,
                    in info,
                    integerBitsOfPrecision,
                    fractionalDigitsPresent != 0
                );
            }

            BigInteger.Pow10(fractionalDenominatorExponent, out BigInteger fractionalDenominator);

            // Because we are using only the fractional part of the mantissa here, the
            // numerator is guaranteed to be smaller than the denominator.  We normalize
            // the fraction such that the most significant bit of the numerator is in
            // the same position as the most significant bit in the denominator.  This
            // ensures that when we later shift the numerator N bits to the left, we
            // will produce N bits of precision.
            uint fractionalNumeratorBits = BigInteger.CountSignificantBits(ref fractionalNumerator);
            uint fractionalDenominatorBits = BigInteger.CountSignificantBits(ref fractionalDenominator);

            uint fractionalShift = 0;

            if (fractionalDenominatorBits > fractionalNumeratorBits)
            {
                fractionalShift = fractionalDenominatorBits - fractionalNumeratorBits;
            }

            if (fractionalShift > 0)
            {
                fractionalNumerator.ShiftLeft(fractionalShift);
            }

            uint requiredFractionalBitsOfPrecision = requiredBitsOfPrecision - integerBitsOfPrecision;
            uint remainingBitsOfPrecisionRequired = requiredFractionalBitsOfPrecision;

            if (integerBitsOfPrecision > 0)
            {
                // If the fractional part of the mantissa provides no bits of precision
                // and cannot affect rounding, we can just take whatever bits we got from
                // the integer part of the mantissa.  This is the case for numbers like
                // 5.0000000000000000000001, where the significant digits of the fractional
                // part start so far to the right that they do not affect the floating
                // point representation.
                //
                // If the fractional shift is exactly equal to the number of bits of
                // precision that we require, then no fractional bits will be part of the
                // result, but the result may affect rounding.  This is e.g. the case for
                // large, odd integers with a fractional part greater than or equal to .5.
                // Thus, we need to do the division to correctly round the result.
                if (fractionalShift > remainingBitsOfPrecisionRequired)
                {
                    return ConvertBigIntegerToFloatingPointBits(
                        ref integerValue,
                        in info,
                        integerBitsOfPrecision,
                        fractionalDigitsPresent != 0
                    );
                }

                remainingBitsOfPrecisionRequired -= fractionalShift;
            }

            // If there was no integer part of the mantissa, we will need to compute the
            // exponent from the fractional part.  The fractional exponent is the power
            // of two by which we must multiply the fractional part to move it into the
            // range [1.0, 2.0).  This will either be the same as the shift we computed
            // earlier, or one greater than that shift:
            uint fractionalExponent = fractionalShift;

            if (BigInteger.Compare(ref fractionalNumerator, ref fractionalDenominator) < 0)
            {
                fractionalExponent++;
            }

            fractionalNumerator.ShiftLeft(remainingBitsOfPrecisionRequired);

            BigInteger.DivRem(ref fractionalNumerator, ref fractionalDenominator, out BigInteger bigFractionalMantissa, out BigInteger fractionalRemainder);
            ulong fractionalMantissa = bigFractionalMantissa.ToUInt64();
            bool hasZeroTail = !number.HasNonZeroTail && fractionalRemainder.IsZero();

            // We may have produced more bits of precision than were required.  Check,
            // and remove any "extra" bits:
            uint fractionalMantissaBits = BigInteger.CountSignificantBits(fractionalMantissa);

            if (fractionalMantissaBits > requiredFractionalBitsOfPrecision)
            {
                int shift = (int)(fractionalMantissaBits - requiredFractionalBitsOfPrecision);
                hasZeroTail = hasZeroTail && (fractionalMantissa & ((1UL << shift) - 1)) == 0;
                fractionalMantissa >>= shift;
            }

            // Compose the mantissa from the integer and fractional parts:
            ulong integerMantissa = integerValue.ToUInt64();
            ulong completeMantissa = (integerMantissa << (int)(requiredFractionalBitsOfPrecision)) + fractionalMantissa;

            // Compute the final exponent:
            // * If the mantissa had an integer part, then the exponent is one less than
            //   the number of bits we obtained from the integer part.  (It's one less
            //   because we are converting to the form 1.11111, with one 1 to the left
            //   of the decimal point.)
            // * If the mantissa had no integer part, then the exponent is the fractional
            //   exponent that we computed.
            // Then, in both cases, we subtract an additional one from the exponent, to
            // account for the fact that we've generated an extra bit of precision, for
            // use in rounding.
            int finalExponent = (integerBitsOfPrecision > 0) ? (int)(integerBitsOfPrecision) - 2 : -(int)(fractionalExponent) - 1;

            return AssembleFloatingPointBits(in info, completeMantissa, finalExponent, hasZeroTail);
        }

        private static ulong RightShiftWithRounding(ulong value, int shift, bool hasZeroTail)
        {
            // If we'd need to shift further than it is possible to shift, the answer
            // is always zero:
            if (shift >= 64)
            {
                return 0;
            }

            ulong extraBitsMask = (1UL << (shift - 1)) - 1;
            ulong roundBitMask = (1UL << (shift - 1));
            ulong lsbBitMask = 1UL << shift;

            bool lsbBit = (value & lsbBitMask) != 0;
            bool roundBit = (value & roundBitMask) != 0;
            bool hasTailBits = !hasZeroTail || (value & extraBitsMask) != 0;

            return (value >> shift) + (ShouldRoundUp(lsbBit, roundBit, hasTailBits) ? 1UL : 0);
        }

        private static bool ShouldRoundUp(bool lsbBit, bool roundBit, bool hasTailBits)
        {
            // If there are insignificant set bits, we need to round to the
            // nearest; there are two cases:
            // we round up if either [1] the value is slightly greater than the midpoint
            // between two exactly representable values or [2] the value is exactly the
            // midpoint between two exactly representable values and the greater of the
            // two is even (this is "round-to-even").
            return roundBit && (hasTailBits || lsbBit);
        }
    }
}
