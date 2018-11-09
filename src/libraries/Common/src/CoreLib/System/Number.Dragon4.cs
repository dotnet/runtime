// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static partial class Number
    {
        private static unsafe void Dragon4(double value, int precision, ref NumberBuffer number)
        {
            const double Log10V2 = 0.30102999566398119521373889472449;

            // DriftFactor = 1 - Log10V2 - epsilon (a small number account for drift of floating point multiplication)
            const double DriftFactor = 0.69;

            // ========================================================================================================================================
            // This implementation is based on the paper: https://www.cs.indiana.edu/~dyb/pubs/FP-Printing-PLDI96.pdf
            // Besides the paper, some of the code and ideas are modified from http://www.ryanjuckett.com/programming/printing-floating-point-numbers/
            // You must read these two materials to fully understand the code.
            //
            // Note: we only support fixed format input.
            // ======================================================================================================================================== 
            //
            // Overview:
            //
            // The input double number can be represented as:
            // value = f * 2^e = r / s.
            //
            // f: the output mantissa. Note: f is not the 52 bits mantissa of the input double number. 
            // e: biased exponent.
            // r: numerator.
            // s: denominator.
            // k: value = d0.d1d2 . . . dn * 10^k

            // Step 1:
            // Extract meta data from the input double value.
            //
            // Refer to IEEE double precision floating point format.
            ulong f = (ulong)(ExtractFractionAndBiasedExponent(value, out int e));
            int mantissaHighBitIndex = (e == -1074) ? (int)(BigInteger.LogBase2(f)) : 52;

            // Step 2:
            // Estimate k. We'll verify it and fix any error later.
            //
            // This is an improvement of the estimation in the original paper.
            // Inspired by http://www.ryanjuckett.com/programming/printing-floating-point-numbers/
            //
            // LOG10V2 = 0.30102999566398119521373889472449
            // DRIFT_FACTOR = 0.69 = 1 - log10V2 - epsilon (a small number account for drift of floating point multiplication)
            int k = (int)(Math.Ceiling(((mantissaHighBitIndex + e) * Log10V2) - DriftFactor));

            // Step 3:
            // Store the input double value in BigInteger format.
            //
            // To keep the precision, we represent the double value as r/s.
            // We have several optimization based on following table in the paper.
            //
            //     ----------------------------------------------------------------------------------------------------------
            //     |               e >= 0                   |                         e < 0                                 |
            //     ----------------------------------------------------------------------------------------------------------
            //     |  f != b^(P - 1)  |  f = b^(P - 1)      | e = min exp or f != b^(P - 1) | e > min exp and f = b^(P - 1) |
            // --------------------------------------------------------------------------------------------------------------
            // | r |  f * b^e * 2     |  f * b^(e + 1) * 2  |          f * 2                |            f * b * 2          |
            // --------------------------------------------------------------------------------------------------------------
            // | s |        2         |        b * 2        |          b^(-e) * 2           |            b^(-e + 1) * 2     |
            // --------------------------------------------------------------------------------------------------------------  
            //
            // Note, we do not need m+ and m- because we only support fixed format input here.
            // m+ and m- are used for free format input, which need to determine the exact range of values 
            // that would round to value when input so that we can generate the shortest correct number.digits.
            //
            // In our case, we just output number.digits until reaching the expected precision.

            var r = new BigInteger(f);
            var s = new BigInteger(0);

            if (e >= 0)
            {
                // When f != b^(P - 1):
                // r = f * b^e * 2
                // s = 2
                // value = r / s = f * b^e * 2 / 2 = f * b^e / 1
                //
                // When f = b^(P - 1):
                // r = f * b^(e + 1) * 2
                // s = b * 2
                // value = r / s =  f * b^(e + 1) * 2 / b * 2 = f * b^e / 1
                //
                // Therefore, we can simply say that when e >= 0:
                // r = f * b^e = f * 2^e
                // s = 1

                r.ShiftLeft((uint)(e));
                s.SetUInt64(1);
            }
            else
            {
                // When e = min exp or f != b^(P - 1):
                // r = f * 2
                // s = b^(-e) * 2
                // value = r / s = f * 2 / b^(-e) * 2 = f / b^(-e)
                //
                // When e > min exp and f = b^(P - 1):
                // r = f * b * 2
                // s = b^(-e + 1) * 2
                // value = r / s =  f * b * 2 / b^(-e + 1) * 2 = f / b^(-e)
                //
                // Therefore, we can simply say that when e < 0:
                // r = f
                // s = b^(-e) = 2^(-e)

                BigInteger.ShiftLeft(1, (uint)(-e), ref s);
            }

            // According to the paper, we should use k >= 0 instead of k > 0 here.
            // However, if k = 0, both r and s won't be changed, we don't need to do any operation.
            //
            // Following are the Scheme code from the paper:
            // --------------------------------------------------------------------------------
            // (if (>= est 0)
            // (fixup r (* s (exptt B est)) m+ m− est B low-ok? high-ok? )
            // (let ([scale (exptt B (− est))])
            // (fixup (* r scale) s (* m+ scale) (* m− scale) est B low-ok? high-ok? ))))
            // --------------------------------------------------------------------------------
            //
            // If est is 0, (* s (exptt B est)) = s, (* r scale) = (* r (exptt B (− est)))) = r.
            //
            // So we just skip when k = 0.

            if (k > 0)
            {
                s.MultiplyPow10((uint)(k));
            }
            else if (k < 0)
            {
                r.MultiplyPow10((uint)(-k));
            }

            if (BigInteger.Compare(ref r, ref s) >= 0)
            {
                // The estimation was incorrect. Fix the error by increasing 1.
                k += 1;
            }
            else
            {
                r.Multiply10();
            }

            number.Scale = (k - 1);

            // This the prerequisite of calling BigInteger.HeuristicDivide().
            BigInteger.PrepareHeuristicDivide(ref r, ref s);

            // Step 4:
            // Calculate number.digits.
            //
            // Output number.digits until reaching the last but one precision or the numerator becomes zero.

            int digitsNum = 0;
            int currentDigit = 0;

            while (true)
            {
                currentDigit = (int)(BigInteger.HeuristicDivide(ref r, ref s));

                if (r.IsZero() || ((digitsNum + 1) == precision))
                {
                    break;
                }

                number.Digits[digitsNum] = (byte)('0' + currentDigit);
                digitsNum++;

                r.Multiply10();
            }

            // Step 5:
            // Set the last digit.
            //
            // We round to the closest digit by comparing value with 0.5:
            //  compare( value, 0.5 )
            //  = compare( r / s, 0.5 )
            //  = compare( r, 0.5 * s)
            //  = compare(2 * r, s)
            //  = compare(r << 1, s)

            r.ShiftLeft(1);
            int compareResult = BigInteger.Compare(ref r, ref s);
            bool isRoundDown = compareResult < 0;

            // We are in the middle, round towards the even digit (i.e. IEEE rouding rules)
            if (compareResult == 0)
            {
                isRoundDown = (currentDigit & 1) == 0;
            }

            if (isRoundDown)
            {
                number.Digits[digitsNum] = (byte)('0' + currentDigit);
                digitsNum++;
            }
            else
            {
                byte* pCurrentDigit = (number.GetDigitsPointer() + digitsNum);

                // Rounding up for 9 is special.
                if (currentDigit == 9)
                {
                    // find the first non-nine prior digit
                    while (true)
                    {
                        // If we are at the first digit
                        if (pCurrentDigit == number.GetDigitsPointer())
                        {
                            // Output 1 at the next highest exponent
                            *pCurrentDigit = (byte)('1');
                            digitsNum++;
                            number.Scale += 1;
                            break;
                        }

                        pCurrentDigit--;
                        digitsNum--;

                        if (*pCurrentDigit != '9')
                        {
                            // increment the digit
                            *pCurrentDigit += 1;
                            digitsNum++;
                            break;
                        }
                    }
                }
                else
                {
                    // It's simple if the digit is not 9.
                    *pCurrentDigit = (byte)('0' + currentDigit + 1);
                    digitsNum++;
                }
            }

            while (digitsNum < precision)
            {
                number.Digits[digitsNum] = (byte)('0');
                digitsNum++;
            }

            number.Digits[precision] = (byte)('\0');

            number.Scale++;
            number.IsNegative = double.IsNegative(value);
        }
    }
}
