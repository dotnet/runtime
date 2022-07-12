// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System
{
    // This is a port of the `Dragon4` implementation here: http://www.ryanjuckett.com/programming/printing-floating-point-numbers/part-2/
    // The backing algorithm and the proofs behind it are described in more detail here:  https://www.cs.indiana.edu/~dyb/pubs/FP-Printing-PLDI96.pdf
    internal static partial class Number
    {
        public static void Dragon4Double(double value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
        {
            double v = double.IsNegative(value) ? -value : value;

            Debug.Assert(v > 0);
            Debug.Assert(double.IsFinite(v));

            ulong mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            uint mantissaHighBitIdx;
            bool hasUnequalMargins = false;

            if ((mantissa >> DiyFp.DoubleImplicitBitIndex) != 0)
            {
                mantissaHighBitIdx = DiyFp.DoubleImplicitBitIndex;
                hasUnequalMargins = (mantissa == (1UL << DiyFp.DoubleImplicitBitIndex));
            }
            else
            {
                Debug.Assert(mantissa != 0);
                mantissaHighBitIdx = (uint)BitOperations.Log2(mantissa);
            }

            int length = (int)(Dragon4(mantissa, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out int decimalExponent));

            number.Scale = decimalExponent + 1;
            number.Digits[length] = (byte)('\0');
            number.DigitsCount = length;
        }

        public static unsafe void Dragon4Half(Half value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
        {
            Half v = Half.IsNegative(value) ? Half.Negate(value) : value;

            Debug.Assert((double)v > 0.0);
            Debug.Assert(Half.IsFinite(v));

            ushort mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            uint mantissaHighBitIdx;
            bool hasUnequalMargins = false;

            if ((mantissa >> DiyFp.HalfImplicitBitIndex) != 0)
            {
                mantissaHighBitIdx = DiyFp.HalfImplicitBitIndex;
                hasUnequalMargins = (mantissa == (1U << DiyFp.HalfImplicitBitIndex));
            }
            else
            {
                Debug.Assert(mantissa != 0);
                mantissaHighBitIdx = (uint)BitOperations.Log2(mantissa);
            }

            int length = (int)(Dragon4(mantissa, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out int decimalExponent));

            number.Scale = decimalExponent + 1;
            number.Digits[length] = (byte)('\0');
            number.DigitsCount = length;
        }

        public static unsafe void Dragon4Single(float value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
        {
            float v = float.IsNegative(value) ? -value : value;

            Debug.Assert(v > 0);
            Debug.Assert(float.IsFinite(v));

            uint mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            uint mantissaHighBitIdx;
            bool hasUnequalMargins = false;

            if ((mantissa >> DiyFp.SingleImplicitBitIndex) != 0)
            {
                mantissaHighBitIdx = DiyFp.SingleImplicitBitIndex;
                hasUnequalMargins = (mantissa == (1U << DiyFp.SingleImplicitBitIndex));
            }
            else
            {
                Debug.Assert(mantissa != 0);
                mantissaHighBitIdx = (uint)BitOperations.Log2(mantissa);
            }

            int length = (int)(Dragon4(mantissa, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out int decimalExponent));

            number.Scale = decimalExponent + 1;
            number.Digits[length] = (byte)('\0');
            number.DigitsCount = length;
        }

        public static (uint low, uint mid, uint high, uint decimalScale) Dragon4DoubleToDecimal(double value, int cutoffNumber, bool isSignificantDigits) // TODO I don't think we need cutoff number, maybe even isSignificantDigits
        {
            double v = double.IsNegative(value) ? -value : value;

            Debug.Assert(v > 0);
            Debug.Assert(double.IsFinite(v));

            ulong mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            uint mantissaHighBitIdx;
            bool hasUnequalMargins = false;

            if ((mantissa >> DiyFp.DoubleImplicitBitIndex) != 0)
            {
                mantissaHighBitIdx = DiyFp.DoubleImplicitBitIndex;
                hasUnequalMargins = (mantissa == (1UL << DiyFp.DoubleImplicitBitIndex));
            }
            else
            {
                Debug.Assert(mantissa != 0);
                mantissaHighBitIdx = (uint)BitOperations.Log2(mantissa);
            }

            Dragon4State state = Dragon4GetState(mantissa, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits);

            // At this point, here is the state
            // scaledValue / scale =  value * 10 / 10^digitExponent
            // scaledValue * 10^(digitExponent - 1) / scale =  value
            //
            // value = decimalMantissa / 10^decimalExponent
            //
            // scaledValue * 10^(digitExponent - 1) / scale = decimalMantissa / 10^decimalExponent
            //
            // scaledValue * 10^(digitExponent - 1) * 10^decimalExponent / scale = decimalMantissa
            //
            // Given that we have 29 whole-number digits to work with in decimal, we can choose decimalExponent based on
            // digitExponent, by doing (29 - digitExponent)

            int maxDecimalDigits = 29;
            int maxDecimalScale = 28;

            int decimalExponent = maxDecimalDigits - state.digitExponent;

            // We must now adjust the decialExponent to fit within the limits of (0, 28)
            if (decimalExponent < 0)
            {
                // if decimalExponent is negative, we should normalize it to 0.
                // Note: for decimalExponent to be negative, digitExponent needs to be greater than 29, which is out of range
                // for numbers we can represent as decimal. This case should never be hit.
                decimalExponent = 0;
                ThrowOverflowException(TypeCode.Decimal);
            }
            else if (decimalExponent > maxDecimalScale)
            {
                // If decimalExponent is more than 28, we should normalize it to 28.
                decimalExponent = maxDecimalScale;
            }

            // Compute the numerator or denomonator by multiplying scaledValue by 10^(digitExponent - 1) and 10^decimalExponent
            int totalExtraScale = state.digitExponent + decimalExponent - 1;
            if (totalExtraScale >= 0)
            {
                state.scaledValue.MultiplyPow10((uint)totalExtraScale);
            }
            else
            {
                state.scale.MultiplyPow10((uint)-totalExtraScale);
            }

            // Divide scaledValue by scale to get our final result, decimalMantissa
            BigInteger.DivRem(ref state.scaledValue, ref state.scale, out BigInteger decimalMantissa, out BigInteger rem);

            // Check if we should round up
            BigInteger.Multiply(ref rem, 2, out BigInteger rem2);
            BigInteger.SetUInt32(out BigInteger one, 1);

            if (BigInteger.Compare(ref rem2, ref state.scale) > 0)
            {
                // Round up
                BigInteger.Add(ref decimalMantissa, ref one, out decimalMantissa);
            }
            else if (BigInteger.Compare(ref rem2, ref state.scale) == 0)
            {
                // Round to even
                // Isolate the smallest digit of decimalMantissa, if it is odd, round up to even
                BigInteger.SetUInt32(out BigInteger ten, 10);
                BigInteger.DivRem(ref decimalMantissa, ref ten, out _, out BigInteger smallestDigit);
                uint smallestDigitUint = smallestDigit.GetBlock(0);
                if (smallestDigitUint % 2 == 1)
                {
                    // Round up
                    BigInteger.Add(ref decimalMantissa, ref one, out decimalMantissa);
                }
            }

            // There is an edge case where some numbers utilizing all 29 decimal digits for thier mantissa will overflow past 96 bits.
            // In these cases, we should scale them down by 10 and adjust the decimalExponent accordingly,
            // representing the same value with one less sigfig.
            if (decimalMantissa.GetLength() == 4)
            {
                BigInteger.SetUInt32(out BigInteger ten, 10);
                BigInteger.DivRem(ref decimalMantissa, ref ten, out decimalMantissa, out BigInteger smallestDigit);
                decimalExponent--;

                // Round up if needed
                uint smallestDigitUint = smallestDigit.GetBlock(0);
                if (smallestDigitUint > 5)
                {
                    // Round up
                    BigInteger.Add(ref decimalMantissa, ref one, out decimalMantissa);
                }
                else if (smallestDigitUint == 5)
                {
                    // Check if we have trailing digits using rem
                    if (rem.IsZero())
                    {
                        // No trailing digits, round to even
                        if (smallestDigitUint % 2 == 1)
                        {
                            // Round up
                            BigInteger.Add(ref decimalMantissa, ref one, out decimalMantissa);
                        }
                    }
                    else
                    {
                        // Trailing digits, round up
                        BigInteger.Add(ref decimalMantissa, ref one, out decimalMantissa);
                    }
                }
            }

            uint low = 0;
            uint mid = 0;
            uint high = 0;

            int len = decimalMantissa.GetLength();
            Debug.Assert(len < 4);

            switch (len)
            {
                case 0:
                    {
                        break;
                    }
                case 3:
                    {
                        high = decimalMantissa.GetBlock(2);
                        goto case 2;
                    }
                case 2:
                    {
                        mid = decimalMantissa.GetBlock(1);
                        goto case 1;
                    }
                case 1:
                    {
                        low = decimalMantissa.GetBlock(0);
                        break;
                    }
                default:
                    {
                        ThrowOverflowException(TypeCode.Decimal);
                        break;
                    }
            }

            Debug.Assert(decimalExponent <= maxDecimalScale && decimalExponent >= 0);
            return (low, mid, high, (uint)decimalExponent);

        }

        private unsafe ref struct Dragon4State
        {
            public BigInteger scale;           // positive scale applied to value and margin such that they can be represented as whole numbers
            public BigInteger scaledValue;     // scale * mantissa
            public BigInteger scaledMarginLow; // scale * 0.5 * (distance between this floating-point number and its immediate lower value)

            // For normalized IEEE floating-point values, each time the exponent is incremented the margin also doubles.
            // That creates a subset of transition numbers where the high margin is twice the size of the low margin.
            public BigInteger* pScaledMarginHigh;
            public BigInteger optionalMarginHigh;

            // Other state set by Dragon4GetState that is used by the second half of the algorithm
            public int digitExponent;
            public bool isEven;
        }

        // This is an implementation of the Dragon4 algorithm to convert a binary number in floating-point format to a decimal number in string format.
        // The function returns the number of digits written to the output buffer and the output is not NUL terminated.
        //
        // The floating point input value is (mantissa * 2^exponent).
        //
        // See the following papers for more information on the algorithm:
        //  "How to Print Floating-Point Numbers Accurately"
        //    Steele and White
        //    http://kurtstephens.com/files/p372-steele.pdf
        //  "Printing Floating-Point Numbers Quickly and Accurately"
        //    Burger and Dybvig
        //    http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.72.4656&rep=rep1&type=pdf
        private static unsafe uint Dragon4(ulong mantissa, int exponent, uint mantissaHighBitIdx, bool hasUnequalMargins, int cutoffNumber, bool isSignificantDigits, Span<byte> buffer, out int decimalExponent)
        {
            int curDigit = 0;

            Debug.Assert(buffer.Length > 0);

            // We deviate from the original algorithm and just assert that the mantissa
            // is not zero. Comparing to zero is fine since the caller should have set
            // the implicit bit of the mantissa, meaning it would only ever be zero if
            // the extracted exponent was also zero. And the assertion is fine since we
            // require that the DoubleToNumber handle zero itself.
            Debug.Assert(mantissa != 0);

            Dragon4State state = Dragon4GetState(mantissa, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits);

            // Compute the cutoff exponent (the exponent of the final digit to print).
            // Default to the maximum size of the output buffer.
            int cutoffExponent = state.digitExponent - buffer.Length;

            if (cutoffNumber != -1)
            {
                int desiredCutoffExponent = 0;

                if (isSignificantDigits)
                {
                    // We asked for a specific number of significant digits.
                    Debug.Assert(cutoffNumber > 0);
                    desiredCutoffExponent = state.digitExponent - cutoffNumber;
                }
                else
                {
                    // We asked for a specific number of fractional digits.
                    Debug.Assert(cutoffNumber >= 0);
                    desiredCutoffExponent = -cutoffNumber;
                }

                if (desiredCutoffExponent > cutoffExponent)
                {
                    // Only select the new cutoffExponent if it won't overflow the destination buffer.
                    cutoffExponent = desiredCutoffExponent;
                }
            }

            // Output the exponent of the first digit we will print
            decimalExponent = --state.digitExponent;

            // In preparation for calling BigInteger.HeuristicDivide(), we need to scale up our values such that the highest block of the denominator is greater than or equal to 8.
            // We also need to guarantee that the numerator can never have a length greater than the denominator after each loop iteration.
            // This requires the highest block of the denominator to be less than or equal to 429496729 which is the highest number that can be multiplied by 10 without overflowing to a new block.

            Debug.Assert(state.scale.GetLength() > 0);
            uint hiBlock = state.scale.GetBlock((uint)(state.scale.GetLength() - 1));

            if ((hiBlock < 8) || (hiBlock > 429496729))
            {
                // Perform a bit shift on all values to get the highest block of the denominator into the range [8,429496729].
                // We are more likely to make accurate quotient estimations in BigInteger.HeuristicDivide() with higher denominator values so we shift the denominator to place the highest bit at index 27 of the highest block.
                // This is safe because (2^28 - 1) = 268435455 which is less than 429496729.
                // This means that all values with a highest bit at index 27 are within range.
                Debug.Assert(hiBlock != 0);
                uint hiBlockLog2 = (uint)BitOperations.Log2(hiBlock);
                Debug.Assert((hiBlockLog2 < 3) || (hiBlockLog2 > 27));
                uint shift = (32 + 27 - hiBlockLog2) % 32;

                state.scale.ShiftLeft(shift);
                state.scaledValue.ShiftLeft(shift);
                state.scaledMarginLow.ShiftLeft(shift);

                if (state.pScaledMarginHigh != &state.scaledMarginLow)
                {
                    BigInteger.Multiply(ref state.scaledMarginLow, 2, out *state.pScaledMarginHigh);
                }
            }

            // These values are used to inspect why the print loop terminated so we can properly round the final digit.
            bool low;            // did the value get within marginLow distance from zero
            bool high;           // did the value get within marginHigh distance from one
            uint outputDigit;    // current digit being output

            if (cutoffNumber == -1)
            {
                Debug.Assert(isSignificantDigits);
                Debug.Assert(state.digitExponent >= cutoffExponent);

                // For the unique cutoff mode, we will try to print until we have reached a level of precision that uniquely distinguishes this value from its neighbors.
                // If we run out of space in the output buffer, we terminate early.

                while (true)
                {
                    // divide out the scale to extract the digit
                    outputDigit = BigInteger.HeuristicDivide(ref state.scaledValue, ref state.scale);
                    Debug.Assert(outputDigit < 10);

                    // update the high end of the value
                    BigInteger.Add(ref state.scaledValue, ref *state.pScaledMarginHigh, out BigInteger scaledValueHigh);

                    // stop looping if we are far enough away from our neighboring values or if we have reached the cutoff digit
                    int cmpLow = BigInteger.Compare(ref state.scaledValue, ref state.scaledMarginLow);
                    int cmpHigh = BigInteger.Compare(ref scaledValueHigh, ref state.scale);

                    if (state.isEven)
                    {
                        low = (cmpLow <= 0);
                        high = (cmpHigh >= 0);
                    }
                    else
                    {
                        low = (cmpLow < 0);
                        high = (cmpHigh > 0);
                    }

                    if (low || high || (state.digitExponent == cutoffExponent))
                    {
                        break;
                    }

                    // store the output digit
                    buffer[curDigit] = (byte)('0' + outputDigit);
                    curDigit++;

                    // multiply larger by the output base
                    state.scaledValue.Multiply10();
                    state.scaledMarginLow.Multiply10();

                    if (state.pScaledMarginHigh != &state.scaledMarginLow)
                    {
                        BigInteger.Multiply(ref state.scaledMarginLow, 2, out *state.pScaledMarginHigh);
                    }

                    state.digitExponent--;
                }
            }
            else if (state.digitExponent >= cutoffExponent)
            {
                Debug.Assert((cutoffNumber > 0) || ((cutoffNumber == 0) && !isSignificantDigits));

                // For length based cutoff modes, we will try to print until we have exhausted all precision (i.e. all remaining digits are zeros) or until we reach the desired cutoff digit.
                low = false;
                high = false;

                while (true)
                {
                    // divide out the scale to extract the digit
                    outputDigit = BigInteger.HeuristicDivide(ref state.scaledValue, ref state.scale);
                    Debug.Assert(outputDigit < 10);

                    if (state.scaledValue.IsZero() || (state.digitExponent <= cutoffExponent))
                    {
                        break;
                    }

                    // store the output digit
                    buffer[curDigit] = (byte)('0' + outputDigit);
                    curDigit++;

                    // multiply larger by the output base
                    state.scaledValue.Multiply10();
                    state.digitExponent--;
                }
            }
            else
            {
                // In the scenario where the first significant digit is after the cutoff, we want to treat that
                // first significant digit as the rounding digit. If the first significant would cause the next
                // digit to round, we will increase the decimalExponent by one and set the previous digit to one.
                // This  ensures we correctly handle the case where the first significant digit is exactly one after
                // the cutoff, it is a 4, and the subsequent digit would round that to 5 inducing a double rounding
                // bug when NumberToString does its own rounding checks. However, if the first significant digit
                // would not cause the next one to round, we preserve that digit as is.

                // divide out the scale to extract the digit
                outputDigit = BigInteger.HeuristicDivide(ref state.scaledValue, ref state.scale);
                Debug.Assert((0 < outputDigit) && (outputDigit < 10));

                if ((outputDigit > 5) || ((outputDigit == 5) && !state.scaledValue.IsZero()))
                {
                    decimalExponent++;
                    outputDigit = 1;
                }

                buffer[curDigit] = (byte)('0' + outputDigit);
                curDigit++;

                // return the number of digits output
                return (uint)curDigit;
            }

            // round off the final digit
            // default to rounding down if value got too close to 0
            bool roundDown = low;

            if (low == high)    // is it legal to round up and down
            {
                // round to the closest digit by comparing value with 0.5.
                //
                // To do this we need to convert the inequality to large integer values.
                //      compare(value, 0.5)
                //      compare(scale * value, scale * 0.5)
                //      compare(2 * scale * value, scale)
                state.scaledValue.ShiftLeft(1); // Multiply by 2
                int compare = BigInteger.Compare(ref state.scaledValue, ref state.scale);
                roundDown = compare < 0;

                // if we are directly in the middle, round towards the even digit (i.e. IEEE rouding rules)
                if (compare == 0)
                {
                    roundDown = (outputDigit & 1) == 0;
                }
            }

            // print the rounded digit
            if (roundDown)
            {
                buffer[curDigit] = (byte)('0' + outputDigit);
                curDigit++;
            }
            else if (outputDigit == 9)      // handle rounding up
            {
                // find the first non-nine prior digit
                while (true)
                {
                    // if we are at the first digit
                    if (curDigit == 0)
                    {
                        // output 1 at the next highest exponent

                        buffer[curDigit] = (byte)('1');
                        curDigit++;
                        decimalExponent++;

                        break;
                    }

                    curDigit--;

                    if (buffer[curDigit] != '9')
                    {
                        // increment the digit

                        buffer[curDigit]++;
                        curDigit++;

                        break;
                    }
                }
            }
            else
            {
                // values in the range [0,8] can perform a simple round up
                buffer[curDigit] = (byte)('0' + outputDigit + 1);
                curDigit++;
            }

            // return the number of digits output
            uint outputLen = (uint)curDigit;
            Debug.Assert(outputLen <= buffer.Length);
            return outputLen;
        }

        private static unsafe Dragon4State Dragon4GetState(ulong mantissa, int exponent, uint mantissaHighBitIdx, bool hasUnequalMargins, int cutoffNumber, bool isSignificantDigits)
        {
            // Compute the initial state in integral form such that
            //      value     = scaledValue / scale
            //      marginLow = scaledMarginLow / scale
            Dragon4State state = default(Dragon4State);

            if (hasUnequalMargins)
            {
                if (exponent > 0)   // We have no fractional component
                {
                    // 1) Expand the input value by multiplying out the mantissa and exponent.
                    //    This represents the input value in its whole number representation.
                    // 2) Apply an additional scale of 2 such that later comparisons against the margin values are simplified.
                    // 3) Set the margin value to the loweset mantissa bit's scale.

                    // scaledValue      = 2 * 2 * mantissa * 2^exponent
                    BigInteger.SetUInt64(out state.scaledValue, 4 * mantissa);
                    state.scaledValue.ShiftLeft((uint)(exponent));

                    // scale            = 2 * 2 * 1
                    BigInteger.SetUInt32(out state.scale, 4);

                    // scaledMarginLow  = 2 * 2^(exponent - 1)
                    BigInteger.Pow2((uint)(exponent), out state.scaledMarginLow);

                    // scaledMarginHigh = 2 * 2 * 2^(exponent + 1)
                    BigInteger.Pow2((uint)(exponent + 1), out state.optionalMarginHigh);
                }
                else                // We have a fractional exponent
                {
                    // In order to track the mantissa data as an integer, we store it as is with a large scale

                    // scaledValue      = 2 * 2 * mantissa
                    BigInteger.SetUInt64(out state.scaledValue, 4 * mantissa);

                    // scale            = 2 * 2 * 2^(-exponent)
                    BigInteger.Pow2((uint)(-exponent + 2), out state.scale);

                    // scaledMarginLow  = 2 * 2^(-1)
                    BigInteger.SetUInt32(out state.scaledMarginLow, 1);

                    // scaledMarginHigh = 2 * 2 * 2^(-1)
                    BigInteger.SetUInt32(out state.optionalMarginHigh, 2);
                }

                // The high and low margins are different
                state.pScaledMarginHigh = &state.optionalMarginHigh;
            }
            else
            {
                if (exponent > 0)   // We have no fractional component
                {
                    // 1) Expand the input value by multiplying out the mantissa and exponent.
                    //    This represents the input value in its whole number representation.
                    // 2) Apply an additional scale of 2 such that later comparisons against the margin values are simplified.
                    // 3) Set the margin value to the lowest mantissa bit's scale.

                    // scaledValue     = 2 * mantissa*2^exponent
                    BigInteger.SetUInt64(out state.scaledValue, 2 * mantissa);
                    state.scaledValue.ShiftLeft((uint)(exponent));

                    // scale           = 2 * 1
                    BigInteger.SetUInt32(out state.scale, 2);

                    // scaledMarginLow = 2 * 2^(exponent-1)
                    BigInteger.Pow2((uint)(exponent), out state.scaledMarginLow);
                }
                else                // We have a fractional exponent
                {
                    // In order to track the mantissa data as an integer, we store it as is with a large scale

                    // scaledValue     = 2 * mantissa
                    BigInteger.SetUInt64(out state.scaledValue, 2 * mantissa);

                    // scale           = 2 * 2^(-exponent)
                    BigInteger.Pow2((uint)(-exponent + 1), out state.scale);

                    // scaledMarginLow = 2 * 2^(-1)
                    BigInteger.SetUInt32(out state.scaledMarginLow, 1);
                }

                // The high and low margins are equal
                state.pScaledMarginHigh = &state.scaledMarginLow;
            }

            // Compute an estimate for digitExponent that will be correct or undershoot by one.
            //
            // This optimization is based on the paper "Printing Floating-Point Numbers Quickly and Accurately" by Burger and Dybvig http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.72.4656&rep=rep1&type=pdf
            //
            // We perform an additional subtraction of 0.69 to increase the frequency of a failed estimate because that lets us take a faster branch in the code.
            // 0.69 is chosen because 0.69 + log10(2) is less than one by a reasonable epsilon that will account for any floating point error.
            //
            // We want to set digitExponent to floor(log10(v)) + 1
            //      v = mantissa * 2^exponent
            //      log2(v) = log2(mantissa) + exponent;
            //      log10(v) = log2(v) * log10(2)
            //      floor(log2(v)) = mantissaHighBitIdx + exponent;
            //      log10(v) - log10(2) < (mantissaHighBitIdx + exponent) * log10(2) <= log10(v)
            //      log10(v) < (mantissaHighBitIdx + exponent) * log10(2) + log10(2) <= log10(v) + log10(2)
            //      floor(log10(v)) < ceil((mantissaHighBitIdx + exponent) * log10(2)) <= floor(log10(v)) + 1
            const double Log10V2 = 0.30102999566398119521373889472449;
            state.digitExponent = (int)(Math.Ceiling(((int)(mantissaHighBitIdx) + exponent) * Log10V2 - 0.69));

            // Divide value by 10^digitExponent.
            if (state.digitExponent > 0)
            {
                // The exponent is positive creating a division so we multiply up the scale.
                state.scale.MultiplyPow10((uint)(state.digitExponent));
            }
            else if (state.digitExponent < 0)
            {
                // The exponent is negative creating a multiplication so we multiply up the scaledValue, scaledMarginLow and scaledMarginHigh.

                BigInteger.Pow10((uint)(-state.digitExponent), out BigInteger pow10);

                state.scaledValue.Multiply(ref pow10);
                state.scaledMarginLow.Multiply(ref pow10);

                if (state.pScaledMarginHigh != &state.scaledMarginLow)
                {
                    BigInteger.Multiply(ref state.scaledMarginLow, 2, out *state.pScaledMarginHigh);
                }
            }

            state.isEven = (mantissa % 2) == 0;
            bool estimateTooLow = false;

            if (cutoffNumber == -1)
            {
                // When printing the shortest possible string, we want to
                // take IEEE unbiased rounding into account so we can return
                // shorter strings for various edge case values like 1.23E+22

                BigInteger.Add(ref state.scaledValue, ref *state.pScaledMarginHigh, out BigInteger scaledValueHigh);
                int cmpHigh = BigInteger.Compare(ref scaledValueHigh, ref state.scale);
                estimateTooLow = state.isEven ? (cmpHigh >= 0) : (cmpHigh > 0);
            }
            else
            {
                estimateTooLow = BigInteger.Compare(ref state.scaledValue, ref state.scale) >= 0;
            }

            // Was our estimate for digitExponent too low?
            if (estimateTooLow)
            {
                // The exponent estimate was incorrect.
                // Increment the exponent and don't perform the premultiply needed for the first loop iteration.
                state.digitExponent++;
            }
            else
            {
                // The exponent estimate was correct.
                // Multiply larger by the output base to prepare for the first loop iteration.
                state.scaledValue.Multiply10();
                state.scaledMarginLow.Multiply10();

                if (state.pScaledMarginHigh != &state.scaledMarginLow)
                {
                    BigInteger.Multiply(ref state.scaledMarginLow, 2, out *state.pScaledMarginHigh);
                }
            }

            return state;
        }
    }
}
