// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    internal static partial class Number
    {
        // Rounds the exact value represented by `value` to `digits` fractional decimal digits
        // using the specified `mode`, returning the nearest representable result.
        //
        // Unlike scaling the input by a power of 10 and rounding, this operates on the exact
        // value of the input using arbitrary precision arithmetic and so produces the correctly
        // rounded result for all finite inputs, including those that would otherwise appear to be
        // a midpoint after an inexact scaling (e.g. `655.924999999999954525...` scaling to exactly
        // `65592.5` when multiplied by `100`).
        //
        // The caller is responsible for handling values which cannot have a fractional portion at
        // the requested number of digits (namely non-finite values and values whose magnitude is at
        // or above the point where all representable values are integers).
        public static unsafe TNumber RoundToDecimalDigits<TNumber>(TNumber value, int digits, MidpointRounding mode)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            Debug.Assert(TNumber.IsFinite(value));
            Debug.Assert((uint)digits <= 15);

            bool isNegative = TNumber.IsNegative(value);

            // Decompose the input into `mantissa * 2^exponent`, giving us the exact value.
            ulong mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            // We want the nearest integer to `|value| * 10^digits`, which is `numerator / denominator`
            // where both are computed exactly. The `2^exponent` term stays in the numerator when the
            // exponent is non-negative and moves to the denominator otherwise.

            BigInteger.SetUInt64(out BigInteger numerator, mantissa);
            BigInteger denominator;

            if (exponent >= 0)
            {
                numerator.ShiftLeft(exponent);
                numerator.MultiplyPow10((uint)digits);
                BigInteger.SetUInt32(out denominator, 1);
            }
            else
            {
                numerator.MultiplyPow10((uint)digits);
                BigInteger.Pow2(-exponent, out denominator);
            }

            BigInteger.DivRem(ref numerator, ref denominator, out BigInteger quotient, out BigInteger remainder);

            // The fractional portion we are rounding is `remainder / denominator`. Comparing
            // `2 * remainder` against `denominator` tells us whether it is below, exactly at, or
            // above the midpoint without any loss of precision.

            bool hasRemainder = !remainder.IsZero();
            remainder.Multiply(2);
            int midpointComparison = BigInteger.Compare(ref remainder, ref denominator);

            bool roundUp;

            switch (mode)
            {
                // Rounds to the nearest value; if the number falls midway, it is rounded to the
                // nearest value with an even least significant digit.
                case MidpointRounding.ToEven:
                    roundUp = (midpointComparison > 0) || ((midpointComparison == 0) && !quotient.IsZero() && ((quotient.GetBlock(0) & 1) != 0));
                    break;

                // Rounds to the nearest value; if the number falls midway, it is rounded to the
                // nearest value away from zero.
                case MidpointRounding.AwayFromZero:
                    roundUp = midpointComparison >= 0;
                    break;

                // Directed rounding: round toward zero.
                case MidpointRounding.ToZero:
                    roundUp = false;
                    break;

                // Directed rounding: round toward negative infinity.
                case MidpointRounding.ToNegativeInfinity:
                    roundUp = isNegative && hasRemainder;
                    break;

                // Directed rounding: round toward positive infinity.
                case MidpointRounding.ToPositiveInfinity:
                    roundUp = !isNegative && hasRemainder;
                    break;

                default:
                    ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
                    return default;
            }

            if (roundUp)
            {
                quotient.Add(1);
            }

            // `quotient` is now the exactly rounded integer value of `|value| * 10^digits`. The final
            // result is the nearest representable value to `quotient * 10^-digits`, which we obtain by
            // materializing the decimal digits and letting the shared conversion perform the correctly
            // rounded decimal-to-binary step.

            byte* pDigits = stackalloc byte[TNumber.NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, TNumber.NumberBufferLength);
            number.IsNegative = isNegative;

            Span<byte> buffer = number.Digits;
            int digitCount = 0;

            if (!quotient.IsZero())
            {
                BigInteger.SetUInt32(out BigInteger ten, 10);

                // Extract the digits least-significant first.
                do
                {
                    BigInteger.DivRem(ref quotient, ref ten, out BigInteger newQuotient, out BigInteger digit);
                    uint digitValue = digit.IsZero() ? 0 : digit.GetBlock(0);
                    buffer[digitCount++] = (byte)('0' + digitValue);
                    BigInteger.SetValue(out quotient, ref newQuotient);
                }
                while (!quotient.IsZero());

                // The decimal point sits `digits` places to the left of the least significant digit,
                // so the scale (number of digits to the left of the decimal point) is `digitCount - digits`.
                number.Scale = digitCount - digits;

                // Reorder to most-significant first, as expected by NumberBuffer.
                buffer.Slice(0, digitCount).Reverse();

                // Trailing zeros carry no value and are not stored in a NumberBuffer.
                while ((digitCount > 0) && (buffer[digitCount - 1] == '0'))
                {
                    digitCount--;
                }
            }

            buffer[digitCount] = (byte)'\0';
            number.DigitsCount = digitCount;

            return NumberToFloat<TNumber>(ref number);
        }
    }
}
