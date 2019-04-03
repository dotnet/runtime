// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Numerics;
using Internal.Runtime.CompilerServices;

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

            uint mantissaHighBitIdx = 0;
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

        public static unsafe void Dragon4Single(float value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
        {
            float v = float.IsNegative(value) ? -value : value;

            Debug.Assert(v > 0);
            Debug.Assert(float.IsFinite(v));

            uint mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            uint mantissaHighBitIdx = 0;
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

            // Compute the initial state in integral form such that
            //      value     = scaledValue / scale
            //      marginLow = scaledMarginLow / scale

            BigInteger scale;           // positive scale applied to value and margin such that they can be represented as whole numbers
            BigInteger scaledValue;     // scale * mantissa
            BigInteger scaledMarginLow; // scale * 0.5 * (distance between this floating-point number and its immediate lower value)

            // For normalized IEEE floating-point values, each time the exponent is incremented the margin also doubles.
            // That creates a subset of transition numbers where the high margin is twice the size of the low margin.
            BigInteger* pScaledMarginHigh;
            BigInteger optionalMarginHigh;

            if (hasUnequalMargins)
            {
                if (exponent > 0)   // We have no fractional component
                {
                    // 1) Expand the input value by multiplying out the mantissa and exponent.
                    //    This represents the input value in its whole number representation.
                    // 2) Apply an additional scale of 2 such that later comparisons against the margin values are simplified.
                    // 3) Set the margin value to the loweset mantissa bit's scale.

                    // scaledValue      = 2 * 2 * mantissa * 2^exponent
                    scaledValue = new BigInteger(4 * mantissa);
                    scaledValue.ShiftLeft((uint)(exponent));

                    // scale            = 2 * 2 * 1
                    scale = new BigInteger(4);

                    // scaledMarginLow  = 2 * 2^(exponent - 1)
                    BigInteger.Pow2((uint)(exponent), out scaledMarginLow);

                    // scaledMarginHigh = 2 * 2 * 2^(exponent + 1)
                    BigInteger.Pow2((uint)(exponent + 1), out optionalMarginHigh);
                }
                else                // We have a fractional exponent
                {
                    // In order to track the mantissa data as an integer, we store it as is with a large scale

                    // scaledValue      = 2 * 2 * mantissa
                    scaledValue = new BigInteger(4 * mantissa);

                    // scale            = 2 * 2 * 2^(-exponent)
                    BigInteger.Pow2((uint)(-exponent + 2), out scale);

                    // scaledMarginLow  = 2 * 2^(-1)
                    scaledMarginLow = new BigInteger(1);

                    // scaledMarginHigh = 2 * 2 * 2^(-1)
                    optionalMarginHigh = new BigInteger(2);
                }

                // The high and low margins are different
                pScaledMarginHigh = &optionalMarginHigh;
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
                    scaledValue = new BigInteger(2 * mantissa);
                    scaledValue.ShiftLeft((uint)(exponent));

                    // scale           = 2 * 1
                    scale = new BigInteger(2);

                    // scaledMarginLow = 2 * 2^(exponent-1)
                    BigInteger.Pow2((uint)(exponent), out scaledMarginLow);
                }
                else                // We have a fractional exponent
                {
                    // In order to track the mantissa data as an integer, we store it as is with a large scale

                    // scaledValue     = 2 * mantissa
                    scaledValue = new BigInteger(2 * mantissa);

                    // scale           = 2 * 2^(-exponent)
                    BigInteger.Pow2((uint)(-exponent + 1), out scale);

                    // scaledMarginLow = 2 * 2^(-1)
                    scaledMarginLow = new BigInteger(1);
                }

                // The high and low margins are equal
                pScaledMarginHigh = &scaledMarginLow;
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
            int digitExponent = (int)(Math.Ceiling(((int)(mantissaHighBitIdx) + exponent) * Log10V2 - 0.69));

            // Divide value by 10^digitExponent.
            if (digitExponent > 0)
            {
                // The exponent is positive creating a division so we multiply up the scale.
                scale.MultiplyPow10((uint)(digitExponent));
            }
            else if (digitExponent < 0)
            {
                // The exponent is negative creating a multiplication so we multiply up the scaledValue, scaledMarginLow and scaledMarginHigh.

                BigInteger.Pow10((uint)(-digitExponent), out BigInteger pow10);

                scaledValue.Multiply(ref pow10);
                scaledMarginLow.Multiply(ref pow10);

                if (pScaledMarginHigh != &scaledMarginLow)
                {
                    BigInteger.Multiply(ref scaledMarginLow, 2, ref *pScaledMarginHigh);
                }
            }

            // If (value >= 1), our estimate for digitExponent was too low
            if (BigInteger.Compare(ref scaledValue, ref scale) >= 0)
            {
                // The exponent estimate was incorrect.
                // Increment the exponent and don't perform the premultiply needed for the first loop iteration.
                digitExponent = digitExponent + 1;
            }
            else
            {
                // The exponent estimate was correct.
                // Multiply larger by the output base to prepare for the first loop iteration.
                scaledValue.Multiply10();
                scaledMarginLow.Multiply10();

                if (pScaledMarginHigh != &scaledMarginLow)
                {
                    BigInteger.Multiply(ref scaledMarginLow, 2, ref *pScaledMarginHigh);
                }
            }

            // Compute the cutoff exponent (the exponent of the final digit to print).
            // Default to the maximum size of the output buffer.
            int cutoffExponent = digitExponent - buffer.Length;

            if (cutoffNumber != -1)
            {
                int desiredCutoffExponent = 0;

                if (isSignificantDigits)
                {
                    // We asked for a specific number of significant digits.
                    Debug.Assert(cutoffNumber > 0);
                    desiredCutoffExponent = digitExponent - cutoffNumber;
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
            decimalExponent = digitExponent - 1;

            // In preparation for calling BigInteger.HeuristicDivie(), we need to scale up our values such that the highest block of the denominator is greater than or equal to 8.
            // We also need to guarantee that the numerator can never have a length greater than the denominator after each loop iteration.
            // This requires the highest block of the denominator to be less than or equal to 429496729 which is the highest number that can be multiplied by 10 without overflowing to a new block.

            Debug.Assert(scale.GetLength() > 0);
            uint hiBlock = scale.GetBlock((uint)(scale.GetLength() - 1));

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

                scale.ShiftLeft(shift);
                scaledValue.ShiftLeft(shift);
                scaledMarginLow.ShiftLeft(shift);

                if (pScaledMarginHigh != &scaledMarginLow)
                {
                    BigInteger.Multiply(ref scaledMarginLow, 2, ref *pScaledMarginHigh);
                }
            }

            // These values are used to inspect why the print loop terminated so we can properly round the final digit.
            bool low;            // did the value get within marginLow distance from zero
            bool high;           // did the value get within marginHigh distance from one
            uint outputDigit;    // current digit being output

            if (cutoffNumber == -1)
            {
                Debug.Assert(isSignificantDigits);

                // For the unique cutoff mode, we will try to print until we have reached a level of precision that uniquely distinguishes this value from its neighbors.
                // If we run out of space in the output buffer, we terminate early.

                while (true)
                {
                    digitExponent = digitExponent - 1;

                    // divide out the scale to extract the digit
                    outputDigit = BigInteger.HeuristicDivide(ref scaledValue, ref scale);
                    Debug.Assert(outputDigit < 10);

                    // update the high end of the value
                    BigInteger.Add(ref scaledValue, ref *pScaledMarginHigh, out BigInteger scaledValueHigh);

                    // stop looping if we are far enough away from our neighboring values or if we have reached the cutoff digit
                    low = BigInteger.Compare(ref scaledValue, ref scaledMarginLow) < 0;
                    high = BigInteger.Compare(ref scaledValueHigh, ref scale) > 0;

                    if (low || high || (digitExponent == cutoffExponent))
                    {
                        break;
                    }

                    // store the output digit
                    buffer[curDigit] = (byte)('0' + outputDigit);
                    curDigit += 1;

                    // multiply larger by the output base
                    scaledValue.Multiply10();
                    scaledMarginLow.Multiply10();

                    if (pScaledMarginHigh != &scaledMarginLow)
                    {
                        BigInteger.Multiply(ref scaledMarginLow, 2, ref *pScaledMarginHigh);
                    }
                }
            }
            else
            {
                Debug.Assert((cutoffNumber > 0) || ((cutoffNumber == 0) && !isSignificantDigits));

                // For length based cutoff modes, we will try to print until we have exhausted all precision (i.e. all remaining digits are zeros) or until we reach the desired cutoff digit.
                low = false;
                high = false;

                while (true)
                {
                    digitExponent = digitExponent - 1;

                    // divide out the scale to extract the digit
                    outputDigit = BigInteger.HeuristicDivide(ref scaledValue, ref scale);
                    Debug.Assert(outputDigit < 10);

                    if (scaledValue.IsZero() || (digitExponent <= cutoffExponent))
                    {
                        break;
                    }

                    // store the output digit
                    buffer[curDigit] = (byte)('0' + outputDigit);
                    curDigit += 1;

                    // multiply larger by the output base
                    scaledValue.Multiply10();
                }
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
                scaledValue.Multiply(2);
                int compare = BigInteger.Compare(ref scaledValue, ref scale);
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
                curDigit += 1;
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
                        curDigit += 1;
                        decimalExponent += 1;

                        break;
                    }

                    curDigit -= 1;

                    if (buffer[curDigit] != '9')
                    {
                        // increment the digit

                        buffer[curDigit] += 1;
                        curDigit += 1;

                        break;
                    }
                }
            }
            else
            {
                // values in the range [0,8] can perform a simple round up
                buffer[curDigit] = (byte)('0' + outputDigit + 1);
                curDigit += 1;
            }

            // return the number of digits output
            uint outputLen = (uint)(curDigit);
            Debug.Assert(outputLen <= buffer.Length);
            return outputLen;
        }
    }
}
